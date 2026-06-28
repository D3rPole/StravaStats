using StravaStats.BusinessObjects;
using System.Text.Json;
using System.Text.Json.Serialization;

public class CoordinateDictionaryConverter<TValue> : JsonConverter<Dictionary<Coordinate, TValue>>
{
    public override Dictionary<Coordinate, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected the JSON to start with an object.");
        }

        var dictionary = new Dictionary<Coordinate, TValue>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return dictionary;
            }

            string rawKey = reader.GetString() ?? throw new JsonException("Key cannot be null.");

            Coordinate key = Coordinate.FromString(rawKey);

            // Advance reader to the value
            reader.Read();

            // Deserialize the value (your Edge object)
            TValue value = JsonSerializer.Deserialize<TValue>(ref reader, options)!;

            dictionary.Add(key, value);
        }

        throw new JsonException("Expected EndObject token.");
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<Coordinate, TValue> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (KeyValuePair<Coordinate, TValue> kvp in value)
        {
            writer.WritePropertyName(kvp.Key.ToString());

            // Serialize the value
            JsonSerializer.Serialize(writer, kvp.Value, options);
        }

        writer.WriteEndObject();
    }
}