using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using Dapper;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Datastore.Converters
{
    public class CookieConverter : SqlMapper.TypeHandler<IDictionary<string, string>>
    {
        protected readonly JsonSerializerOptions SerializerSettings;

        public CookieConverter()
        {
            var serializerSettings = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                IgnoreNullValues = true,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            SerializerSettings = serializerSettings;
        }

        public override void SetValue(IDbDataParameter parameter, IDictionary<string, string> value)
        {
            parameter.Value = JsonSerializer.Serialize(value, SerializerSettings);
        }

        public override IDictionary<string, string> Parse(object value)
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>((string)value, SerializerSettings);
        }
    }
}
