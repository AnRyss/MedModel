using System.Text.Json;
using System.Text.Json.Serialization;

namespace MedicalModel;


    public class NumberToTimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                double days = reader.GetDouble();
                return TimeSpan.FromDays(days);
            }

            return TimeSpan.Parse(reader.GetString()!);
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.TotalDays);
        }
    }
