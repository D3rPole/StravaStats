using System.Text.Json;
using System.Text.Json.Serialization;

namespace StravaStats.Services
{
    public class ActivityResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
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
    public class StravaService(IConfiguration configuration, ILogger<StravaService> logger) : IHostedService
    {
        private Token? token;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            LoadToken();
            await DownloadActivities();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }

        private void LoadToken()
        {
            string tokenFilePath = Path.Combine(AppData.DataDirectory, "Tokens.json");
            if (!File.Exists(tokenFilePath))
            {
                logger.LogError("Token File missing. Strava Api unavailable.");
                return;
            }

            string jsonText = File.ReadAllText(tokenFilePath);

            token = JsonSerializer.Deserialize<Token>(jsonText);

            if (token is null)
            {
                logger.LogError("Failed to read Tokens. Strava Api unavailable.");
                return;
            }
            logger.LogInformation("Token loaded");
        }

        public async Task<bool> RefreshToken()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://www.strava.com/");

            var parameters = new Dictionary<string, string>
            {
                { "client_id", configuration["StravaClientId"] ?? string.Empty },
                { "client_secret", configuration["StravaClientSecret"] ?? string.Empty },
                { "refresh_token", token?.RefreshToken ?? string.Empty },
                { "grant_type", "refresh_token" }
            };

            var content = new FormUrlEncodedContent(parameters);

            var response = await httpClient.PostAsync("oauth/token", content);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Token refresh error: " + response.ReasonPhrase);
                return false;
            }

            var responseString = await response.Content.ReadAsStringAsync();
            token = JsonSerializer.Deserialize<Token>(responseString);
            if (token is not null)
            {
                string tokenFilePath = Path.Combine(AppData.DataDirectory, "Tokens.json");
                File.WriteAllText(tokenFilePath, responseString);
                logger.LogInformation("Token refreshed.");
                return true;
            }

            logger.LogError("Token turned out to be null, reloading old token. Refresh failed");
            LoadToken(); // just reload old token, try again later
            return false;
        }

        public async Task DownloadActivities()
        {
            if (token is null)
            {
                logger.LogError("No Token set, skipping Activities download");
                return;
            }

            if (token.ExpiresAtDateTime < DateTime.Now)
            {
                bool refreshed = await RefreshToken();
                if (!refreshed)
                    return;
            }

            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://www.strava.com/api/v3/");
            httpClient.DefaultRequestHeaders.Authorization = new("Bearer", token.AccessToken);

            List<ActivityResponse> activities = [];
            List<ActivityResponse>? currentRequestActivities = null;
            int page = 1;
            do
            {
                var response = await httpClient.GetAsync($"activities?page={page}&per_page=200");
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError(response.ReasonPhrase);
                    return;
                }
                var str = await response.Content.ReadAsStringAsync();
                currentRequestActivities = JsonSerializer.Deserialize<List<ActivityResponse>>(str);
                activities.AddRange(currentRequestActivities ?? []);
                page++;
            } while (currentRequestActivities is not null && currentRequestActivities.Count > 0);
            int i = 0;
            // Get Activity:
            // https://www.strava.com/api/v3/activities/18894098488/streams?keys=time,distance,latlng,heartrate&key_by_type=true

            string activitiesDirectory = Path.Combine(AppData.DataDirectory, "Activities");
            if (!Directory.Exists(activitiesDirectory))
                Directory.CreateDirectory(activitiesDirectory);

            foreach (ActivityResponse activity in activities)
            {
                string activityPath = Path.Combine(activitiesDirectory, activity.Id + ".json");
                if (File.Exists(activityPath))
                    continue;

                var response = await httpClient.GetAsync($"https://www.strava.com/api/v3/activities/{activity.Id}/streams?keys=time,distance,latlng,altitude,velocity_smooth,heartrate,cadence,watts,moving,grade_smooth&key_by_type=true");
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError($"Couldn't retrieve data for activity ({activity.Name} : {activity.Id}) {response.ReasonPhrase}");
                    continue;
                }

                var stringResponse = await response.Content.ReadAsStringAsync();
                File.WriteAllText(activityPath, stringResponse);
            }
        }
    }
}
