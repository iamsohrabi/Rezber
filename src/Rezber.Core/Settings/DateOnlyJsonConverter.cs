using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rezber.Core.Settings;

public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string Format = "yyyy-MM-dd";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new InvalidOperationException($"Cannot get the value of a token type '{reader.TokenType}' as a string.");
        }

        string dateString = reader.GetString()
            ?? throw new JsonException("Date string cannot be null.");

        return DateOnly.ParseExact(dateString, Format, CultureInfo.InvariantCulture);
    }
    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}
