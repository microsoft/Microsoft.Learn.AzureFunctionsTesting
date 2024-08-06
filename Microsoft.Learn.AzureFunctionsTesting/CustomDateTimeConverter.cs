using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Learn.AzureFunctionsTesting
{
    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        private readonly string _format;

        public CustomDateTimeConverter(string format)
        {
            _format = format;
        }

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? dateString = reader.GetString();
            if (dateString != null)
            {
                return DateTime.ParseExact(dateString, _format, null);
            }
            else
            {
                return default;
            }
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_format));
        }
    }
}
