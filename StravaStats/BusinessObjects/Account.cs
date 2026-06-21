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
        public List<Activity> Activities
        {
            get => field; set
            {
                field = value;
                if (field is null || field.Count == 0)
                    return;
                StartDateRange = field.FirstOrDefault(a => a.ActivityHeader.StartDate is not null).ActivityHeader.StartDate ?? DateTime.MinValue;
                EndDateRange = StartDateRange;
                foreach (var activity in field)
                {
                    if(activity.ActivityHeader.StartDate is not null && activity.ActivityHeader.StartDate.Value < StartDateRange)
                        StartDateRange = activity.ActivityHeader.StartDate.Value;
                    if(activity.ActivityHeader.StartDate is not null && activity.ActivityHeader.StartDate.Value > EndDateRange)
                        EndDateRange = activity.ActivityHeader.StartDate.Value;
                }
            }
        }

        [JsonIgnore]
        public Graph FullGraph { get; set; }

        [JsonIgnore]
        public Graph Selection { get; set; }

        [JsonIgnore]
        public Graph LOD1Graph { get; set; }

        [JsonIgnore]
        public Graph LOD2Graph { get; set; }

        [JsonIgnore]
        public Graph LOD3Graph { get; set; }

        [JsonIgnore]
        public string AccountDirectory { get; set; }

        [JsonIgnore]
        public DateTime StartDateRange { get; set; }

        [JsonIgnore]
        public DateTime EndDateRange { get; set; }

        [JsonIgnore]
        public DateTime SelectedStart { get; set; }

        [JsonIgnore]
        public DateTime SelectedEnd { get; set; }

        public async Task BuildGraphs()
        {
            long ticks = DateTime.Now.Ticks;

            FullGraph = new(Activities.Select(a => a.Graph).ToList());
            Selection = FullGraph;

            double ms = (double)(DateTime.Now.Ticks - ticks) / TimeSpan.TicksPerMillisecond;
            Console.WriteLine(Name + $": {ms:F2} ms");
        }

        public void ResetSelection()
        {
            Selection = FullGraph;
            SelectedStart = StartDateRange;
            SelectedEnd = EndDateRange;
        }

        public void SelectRange(DateTime from, DateTime to)
        {
            if (from == SelectedStart && to == SelectedEnd)
                return;
            SelectedStart = from;
            SelectedEnd = to;
            var selection = Activities.Where(a =>
                a.ActivityHeader.StartDate is not null &&
                a.ActivityHeader.StartDate >= from &&
                a.ActivityHeader.StartDate <= to).Select(a => a.Graph).ToList();
            Selection = new(selection);
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
