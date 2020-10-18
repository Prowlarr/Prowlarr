using System;
using NzbDrone.Core.Datastore;
using Prowlarr.Http.REST;
using Prowlarr.Http.Validation;

namespace Prowlarr.Http
{
    public abstract class ProwlarrRestModule<TResource> : RestModule<TResource>
        where TResource : RestResource, new()
    {
        protected string Resource { get; private set; }

        private static string BaseUrl()
        {
            var isV1 = typeof(TResource).Namespace.Contains(".V1.");
            if (isV1)
            {
                return "/api/v1/";
            }

            return "/api/";
        }

        private static string ResourceName()
        {
            return new TResource().ResourceName.Trim('/').ToLower();
        }

        protected ProwlarrRestModule()
            : this(ResourceName())
        {
        }

        protected ProwlarrRestModule(string resource)
            : base(BaseUrl() + resource.Trim('/').ToLower())
        {
            Resource = resource;
            PostValidator.RuleFor(r => r.Id).IsZero();
            PutValidator.RuleFor(r => r.Id).ValidId();
        }

        protected PagingResource<TResource> ApplyToPage<TModel>(Func<PagingSpec<TModel>, PagingSpec<TModel>> function, PagingSpec<TModel> pagingSpec, Converter<TModel, TResource> mapper)
        {
            pagingSpec = function(pagingSpec);

            return new PagingResource<TResource>
            {
                Page = pagingSpec.Page,
                PageSize = pagingSpec.PageSize,
                SortDirection = pagingSpec.SortDirection,
                SortKey = pagingSpec.SortKey,
                TotalRecords = pagingSpec.TotalRecords,
                Records = pagingSpec.Records.ConvertAll(mapper)
            };
        }
    }
}
