using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Download
{
    public interface IValidateNzbs
    {
        void Validate(byte[] fileContent);
    }

    public class NzbValidationService : IValidateNzbs
    {
        public void Validate(byte[] fileContent)
        {
            var reader = new StreamReader(new MemoryStream(fileContent));

            using (var xmlTextReader = XmlReader.Create(reader, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, IgnoreComments = true }))
            {
                var xDoc = XDocument.Load(xmlTextReader);
                var nzb = xDoc.Root;

                if (nzb == null)
                {
                    throw new InvalidNzbException("Invalid NZB: No Root element");
                }

                // nZEDb has an bug in their error reporting code spitting out invalid http status codes
                if (nzb.Name.LocalName.Equals("error") &&
                    nzb.TryGetAttributeValue("code", out var code) &&
                    nzb.TryGetAttributeValue("description", out var description))
                {
                    throw new InvalidNzbException("Invalid NZB: Contains indexer error: {0} - {1}", code, description);
                }

                if (!nzb.Name.LocalName.Equals("nzb"))
                {
                    throw new InvalidNzbException("Invalid NZB: Unexpected root element. Expected 'nzb' found '{0}'", nzb.Name.LocalName);
                }

                var ns = nzb.Name.Namespace;
                var files = nzb.Elements(ns + "file").ToList();

                if (files.Empty())
                {
                    throw new InvalidNzbException("Invalid NZB: No files");
                }
            }
        }
    }
}
