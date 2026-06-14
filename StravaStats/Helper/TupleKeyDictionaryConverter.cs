using System.Text.Json;
using System.Text.Json.Serialization;

public class TupleKeyDictionaryConverter<TValue> : JsonConverter<Dictionary<(string, string), TValue>>
{
    private const string Delimiter = ";"; // Choose a delimiter that won't appear in your keys

    public override Dictionary<(string, string), TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected the JSON to start with an object.");
        }

        var dictionary = new Dictionary<(string, string), TValue>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return dictionary;
            }

            // Get the string key (e.g., "NodeA,NodeB")
            string rawKey = reader.GetString() ?? throw new JsonException("Key cannot be null.");

            // Split it back into the tuple halves
            string[] parts = rawKey.Split(Delimiter);
            if (parts.Length != 2)
            {
                throw new JsonException($"Invalid key format. Expected 'string{Delimiter}string'.");
            }

            var keyTuple = (parts[0], parts[1]);

            // Advance reader to the value
            reader.Read();

            // Deserialize the value (your Edge object)
            TValue value = JsonSerializer.Deserialize<TValue>(ref reader, options)!;

            dictionary.Add(keyTuple, value);
        }

        throw new JsonException("Expected EndObject token.");
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<(string, string), TValue> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (KeyValuePair<(string, string), TValue> kvp in value)
        {
            // Combine the tuple into a single string key
            string combinedKey = $"{kvp.Key.Item1}{Delimiter}{kvp.Key.Item2}";
            writer.WritePropertyName(combinedKey);

            // Serialize the value
            JsonSerializer.Serialize(writer, kvp.Value, options);
        }

        writer.WriteEndObject();
    }
}