using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Processes;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.CustomScript
{
    public class CustomScript : NotificationBase<CustomScriptSettings>
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IProcessProvider _processProvider;
        private readonly Logger _logger;

        public CustomScript(IDiskProvider diskProvider, IProcessProvider processProvider, Logger logger)
        {
            _diskProvider = diskProvider;
            _processProvider = processProvider;
            _logger = logger;
        }

        public override string Name => "Custom Script";

        public override string Link => "https://wikijs.servarr.com/prowlarr/settings#connections";

        public override ProviderMessage Message => new ProviderMessage("Testing will execute the script with the EventType set to Test, ensure your script handles this correctly", ProviderMessageType.Warning);

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Prowlarr_EventType", "HealthIssue");
            environmentVariables.Add("Prowlarr_Health_Issue_Level", Enum.GetName(typeof(HealthCheckResult), healthCheck.Type));
            environmentVariables.Add("Prowlarr_Health_Issue_Message", healthCheck.Message);
            environmentVariables.Add("Prowlarr_Health_Issue_Type", healthCheck.Source.Name);
            environmentVariables.Add("Prowlarr_Health_Issue_Wiki", healthCheck.WikiUrl.ToString() ?? string.Empty);

            ExecuteScript(environmentVariables);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            if (!_diskProvider.FileExists(Settings.Path))
            {
                failures.Add(new NzbDroneValidationFailure("Path", "File does not exist"));
            }

            foreach (var systemFolder in SystemFolders.GetSystemFolders())
            {
                if (systemFolder.IsParentPath(Settings.Path))
                {
                    failures.Add(new NzbDroneValidationFailure("Path", $"Must not be a descendant of '{systemFolder}'"));
                }
            }

            if (failures.Empty())
            {
                try
                {
                    var environmentVariables = new StringDictionary();
                    environmentVariables.Add("Prowlarr_EventType", "Test");

                    var processOutput = ExecuteScript(environmentVariables);

                    if (processOutput.ExitCode != 0)
                    {
                        failures.Add(new NzbDroneValidationFailure(string.Empty, $"Script exited with code: {processOutput.ExitCode}"));
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                    failures.Add(new NzbDroneValidationFailure(string.Empty, ex.Message));
                }
            }

            return new ValidationResult(failures);
        }

        private ProcessOutput ExecuteScript(StringDictionary environmentVariables)
        {
            _logger.Debug("Executing external script: {0}", Settings.Path);

            var processOutput = _processProvider.StartAndCapture(Settings.Path, Settings.Arguments, environmentVariables);

            _logger.Debug("Executed external script: {0} - Status: {1}", Settings.Path, processOutput.ExitCode);
            _logger.Debug("Script Output: \r\n{0}", string.Join("\r\n", processOutput.Lines));

            return processOutput;
        }

        private bool ValidatePathParent(string possibleParent, string path)
        {
            return possibleParent.IsParentPath(path);
        }
    }
}
