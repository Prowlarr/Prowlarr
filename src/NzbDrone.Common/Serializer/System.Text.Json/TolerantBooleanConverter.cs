using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NzbDrone.Common.Serializer
{
    /// <summary>
    /// Reads booleans that indexers publish in inconsistent shapes, such as true/false, 0/1,
    /// "1"/"0", "" and null. One unexpected value therefore cannot fail a whole response.
    /// </summary>
    public class TolerantBooleanConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                case JsonTokenType.Null:
                    return false;
                case JsonTokenType.Number:
                    return reader.GetInt64() != 0;
                case JsonTokenType.String:
                    return ParseString(reader.GetString());
                default:
                    reader.Skip();
                    return false;
            }
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }

        private static bool ParseString(string value)
        {
            var trimmed = value?.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                return false;
            }

            if (bool.TryParse(trimmed, out var parsed))
            {
                return parsed;
            }

            return trimmed == "1";
        }
    }
}
