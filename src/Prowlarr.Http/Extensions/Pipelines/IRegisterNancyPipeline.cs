using Nancy.Bootstrapper;

namespace Prowlarr.Http.Extensions.Pipelines
{
    public interface IRegisterNancyPipeline
    {
        int Order { get; }

        void Register(IPipelines pipelines);
    }
}
