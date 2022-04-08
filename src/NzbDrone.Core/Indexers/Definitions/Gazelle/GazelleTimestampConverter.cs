using System;
using Newtonsoft.Json;

namespace NzbDrone.Core.Indexers.Gazelle
{
    public class GazelleTimestampConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(value);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(long))
            {
                return Convert.ToInt64(reader.Value);
            }

            if (objectType == typeof(string))
            {
                var date = DateTimeOffset.Parse(reader.Value.ToString());
                return date.ToUnixTimeSeconds();
            }

            throw new JsonSerializationException("Can't convert type " + existingValue.GetType().FullName + " to timestamp");
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(long) || objectType == typeof(string);
        }
    }
}
