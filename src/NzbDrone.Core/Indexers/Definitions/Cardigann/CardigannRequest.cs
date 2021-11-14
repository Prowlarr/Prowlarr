using System.Collections.Generic;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Indexers.Cardigann
{
    public class CardigannRequest : IndexerRequest
    {
        public Dictionary<string, object> Variables { get; private set; }
        public SearchPathBlock SearchPath { get; private set; }

        public CardigannRequest(string url, HttpAccept httpAccept, Dictionary<string, object> variables, SearchPathBlock searchPath)
            : base(url, httpAccept)
        {
            Variables = variables;
            SearchPath = searchPath;
        }

        public CardigannRequest(HttpRequest httpRequest, Dictionary<string, object> variables, SearchPathBlock searchPath)
            : base(httpRequest)
        {
            Variables = variables;
            SearchPath = searchPath;
        }
    }
}
