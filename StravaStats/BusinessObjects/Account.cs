using System.Text.Json.Serialization;

namespace StravaStats.BusinessObjects
{
    public class Account
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("client_id")]
        public string ClientId { get; set; }

        [JsonPropertyName("client_secret")]
        public string ClientSecret { get; set; }

        [JsonPropertyName("token")]
        public Token Token { get; set; }

        [JsonIgnore]
        public List<Activity> Activities { get; set; } = [];

        [JsonIgnore]
        public Graph FullGraph { get; set; }

        [JsonIgnore]
        public Graph LOD1Graph { get; set; }

        [JsonIgnore]
        public Graph LOD2Graph { get; set; }

        [JsonIgnore]
        public Graph LOD3Graph { get; set; }

        [JsonIgnore]
        public string AccountDirectory { get; set; }

        public async Task BuildGraphs()
        {
            long ticks = DateTime.Now.Ticks;

            FullGraph = new(Activities.Select(a => a.Graph).ToList());
            var task1 = Task.Run(() => LOD1Graph = new(FullGraph, 40));
            var task2 = Task.Run(() => LOD2Graph = new(FullGraph, 80));
            var task3 = Task.Run(() => LOD3Graph = new(FullGraph, 160));
            await Task.WhenAll(task1, task2, task3);

            double ms = (double)(DateTime.Now.Ticks - ticks) / TimeSpan.TicksPerMillisecond;
            Console.WriteLine(Name + $": {ms:F2} ms");
        }
    }

    public class Token
    {
        [JsonPropertyName("token_type")]
        public string Type { get; set; }

        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("expires_at")]
        public int ExpiresAt { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonPropertyName("scope")]
        public string Scope { get; set; }

        [JsonIgnore]
        public DateTime ExpiresAtDateTime => DateTimeOffset.FromUnixTimeSeconds(ExpiresAt).UtcDateTime;
    }
}
