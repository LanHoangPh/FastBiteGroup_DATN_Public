using System.Text.Json;

namespace FastBiteGroupMCA.API.Converter
{
    public class UtcDateTimeConverter : System.Text.Json.Serialization.JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDateTime().ToUniversalTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Utc));
        }
    }
}
