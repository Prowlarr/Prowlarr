using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public abstract class NotificationBase<TSettings> : INotification
        where TSettings : IProviderConfig, new()
    {
        protected const string RELEASE_GRABBED_TITLE = "Release Grabbed";
        protected const string HEALTH_ISSUE_TITLE = "Health Check Failure";
        protected const string HEALTH_RESTORED_TITLE = "Health Check Restored";
        protected const string APPLICATION_UPDATE_TITLE = "Application Updated";

        protected const string RELEASE_GRABBED_TITLE_BRANDED = "Prowlarr - " + RELEASE_GRABBED_TITLE;
        protected const string HEALTH_ISSUE_TITLE_BRANDED = "Prowlarr - " + HEALTH_ISSUE_TITLE;
        protected const string HEALTH_RESTORED_TITLE_BRANDED = "Prowlarr - " + HEALTH_RESTORED_TITLE;
        protected const string APPLICATION_UPDATE_TITLE_BRANDED = "Prowlarr - " + APPLICATION_UPDATE_TITLE;

        public abstract string Name { get; }

        public Type ConfigContract => typeof(TSettings);

        public virtual ProviderMessage Message => null;

        public IEnumerable<ProviderDefinition> DefaultDefinitions => new List<ProviderDefinition>();

        public ProviderDefinition Definition { get; set; }
        public abstract ValidationResult Test();

        public abstract string Link { get; }

        public virtual void OnGrab(GrabMessage grabMessage)
        {
        }

        public virtual void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
        }

        public virtual void OnHealthRestored(HealthCheck.HealthCheck previousCheck)
        {
        }

        public virtual void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
        }

        public virtual void ProcessQueue()
        {
        }

        public bool SupportsOnGrab => HasConcreteImplementation("OnGrab");
        public bool SupportsOnHealthIssue => HasConcreteImplementation("OnHealthIssue");
        public bool SupportsOnHealthRestored => HasConcreteImplementation("OnHealthRestored");
        public bool SupportsOnApplicationUpdate => HasConcreteImplementation("OnApplicationUpdate");

        protected TSettings Settings => (TSettings)Definition.Settings;

        public override string ToString()
        {
            return GetType().Name;
        }

        public virtual object RequestAction(string action, IDictionary<string, string> query)
        {
            return null;
        }

        private bool HasConcreteImplementation(string methodName)
        {
            var method = GetType().GetMethod(methodName);

            if (method == null)
            {
                throw new MissingMethodException(GetType().Name, Name);
            }

            return !method.DeclaringType.IsAbstract;
        }
    }
}
