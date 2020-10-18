using Nancy;
using Nancy.Responses.Negotiation;

namespace Prowlarr.Http
{
    public abstract class RadarrModule : NancyModule
    {
        protected RadarrModule(string resource)
        : base(resource)
        {
        }

        protected Negotiator ResponseWithCode(object model, HttpStatusCode statusCode)
        {
            return Negotiate.WithModel(model).WithStatusCode(statusCode);
        }
    }
}
