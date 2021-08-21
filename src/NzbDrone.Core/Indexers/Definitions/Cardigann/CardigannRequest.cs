using System.Collections.Generic;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Indexers.Cardigann
{
    public class CardigannRequest : IndexerRequest
    {
        public Dictionary<string, object> Variables { get; private set; }

        public CardigannRequest(string url, HttpAccept httpAccept, Dictionary<string, object> variables)
            : base(url, httpAccept)
        {
            Variables = variables;
        }

        public CardigannRequest(HttpRequest httpRequest, Dictionary<string, object> variables)
            : base(httpRequest)
        {
            Variables = variables;
        }
    }
}
