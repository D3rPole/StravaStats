using StravaStats.BusinessObjects;
using System.Text.Json;
using System.Text.Json.Serialization;

public class EdgeKeyDictionaryConverter<TValue> : JsonConverter<Dictionary<EdgeKey, TValue>>
{
    private const string Delimiter = ";"; // Choose a delimiter that won't appear in your keys

    public override Dictionary<EdgeKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected the JSON to start with an object.");
        }

        var dictionary = new Dictionary<EdgeKey, TValue>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return dictionary;
            }

            string rawKey = reader.GetString() ?? throw new JsonException("Key cannot be null.");

            EdgeKey key = EdgeKey.FromString(rawKey);

            // Advance reader to the value
            reader.Read();

            // Deserialize the value (your Edge object)
            TValue value = JsonSerializer.Deserialize<TValue>(ref reader, options)!;

            dictionary.Add(key, value);
        }

        throw new JsonException("Expected EndObject token.");
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<EdgeKey, TValue> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (KeyValuePair<EdgeKey, TValue> kvp in value)
        {
            writer.WritePropertyName(kvp.Key.ToString());

            // Serialize the value
            JsonSerializer.Serialize(writer, kvp.Value, options);
        }

        writer.WriteEndObject();
    }
}