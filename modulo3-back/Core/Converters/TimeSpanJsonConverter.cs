using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Core.Converters;

public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
{
    private static readonly Regex SecondsPattern = new(@"^(\d+(?:\.\d+)?)s$", RegexOptions.Compiled);

    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        if (string.IsNullOrWhiteSpace(value))
            return TimeSpan.Zero;

        if (TimeSpan.TryParse(value, out var result))
            return result;

        if (value.StartsWith("PT", StringComparison.OrdinalIgnoreCase))
        {
            try { return System.Xml.XmlConvert.ToTimeSpan(value); }
            catch {  }
        }

        var match = SecondsPattern.Match(value);
        if (match.Success && double.TryParse(match.Groups[1].Value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var seconds))
        {
            return TimeSpan.FromSeconds(seconds);
        }

        throw new JsonException($"Formato de TimeSpan não suportado: '{value}'. Formatos aceitos: 'hh:mm:ss', 'PT5M', '0.5s'.");
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(@"hh\:mm\:ss"));
}