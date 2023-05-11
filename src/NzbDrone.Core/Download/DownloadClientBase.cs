using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download
{
    public abstract class DownloadClientBase<TSettings> : IDownloadClient
        where TSettings : IProviderConfig, new()
    {
        protected readonly IConfigService _configService;
        protected readonly IDiskProvider _diskProvider;
        protected readonly Logger _logger;

        public abstract string Name { get; }

        public Type ConfigContract => typeof(TSettings);

        public virtual ProviderMessage Message => null;

        public IEnumerable<ProviderDefinition> DefaultDefinitions => new List<ProviderDefinition>();

        public ProviderDefinition Definition { get; set; }

        public virtual object RequestAction(string action, IDictionary<string, string> query)
        {
            return null;
        }

        protected TSettings Settings => (TSettings)Definition.Settings;

        protected DownloadClientBase(IConfigService configService,
            IDiskProvider diskProvider,
            Logger logger)
        {
            _configService = configService;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public override string ToString()
        {
            return GetType().Name;
        }

        protected List<DownloadClientCategory> Categories => ((DownloadClientDefinition)Definition).Categories;
        public abstract bool SupportsCategories { get; }

        public abstract DownloadProtocol Protocol
        {
            get;
        }

        public abstract Task<string> Download(ReleaseInfo release, bool redirect, IIndexer indexer);

        protected string GetCategoryForRelease(ReleaseInfo release)
        {
            var categories = ((DownloadClientDefinition)Definition).Categories;
            if (categories.Count == 0)
            {
                return null;
            }

            // Check for direct mapping
            var category = categories.FirstOrDefault(x => x.Categories.Intersect(release.Categories.Select(c => c.Id)).Any())?.ClientCategory;

            // Check for parent mapping
            if (category == null)
            {
                foreach (var cat in categories)
                {
                    var mappedCat = NewznabStandardCategory.AllCats.Where(x => cat.Categories.Contains(x.Id));
                    var subCats = mappedCat.SelectMany(x => x.SubCategories);

                    if (subCats.Intersect(release.Categories).Any())
                    {
                        category = cat.ClientCategory;
                        break;
                    }
                }
            }

            return category;
        }

        protected virtual void ValidateCategories(List<ValidationFailure> failures)
        {
            foreach (var category in ((DownloadClientDefinition)Definition).Categories)
            {
                if (category.ClientCategory.IsNullOrWhiteSpace())
                {
                    failures.AddIfNotNull(new ValidationFailure(string.Empty, "Category can not be empty"));
                }
            }
        }

        public ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            try
            {
                ValidateCategories(failures);
                Test(failures);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Test aborted due to exception");
                failures.Add(new ValidationFailure(string.Empty, "Test was aborted due to an error: " + ex.Message));
            }

            return new ValidationResult(failures);
        }

        protected abstract void Test(List<ValidationFailure> failures);

        protected ValidationFailure TestFolder(string folder, string propertyName, bool mustBeWritable = true)
        {
            if (!_diskProvider.FolderExists(folder))
            {
                return new NzbDroneValidationFailure(propertyName, "Folder does not exist")
                {
                    DetailedDescription = string.Format("The folder you specified does not exist or is inaccessible. Please verify the folder permissions for the user account '{0}', which is used to execute Prowlarr.", Environment.UserName)
                };
            }

            if (mustBeWritable && !_diskProvider.FolderWritable(folder))
            {
                _logger.Error("Folder '{0}' is not writable.", folder);
                return new NzbDroneValidationFailure(propertyName, "Unable to write to folder")
                {
                    DetailedDescription = string.Format("The folder you specified is not writable. Please verify the folder permissions for the user account '{0}', which is used to execute Prowlarr.", Environment.UserName)
                };
            }

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
