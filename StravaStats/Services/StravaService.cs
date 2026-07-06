using StravaStats.BusinessObjects;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StravaStats.Services
{
    public class StravaService(IConfiguration configuration, ILogger<StravaService> logger)
    {
        public async Task<bool> RefreshToken(Account account)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://www.strava.com/");

            var parameters = new Dictionary<string, string>
            {
                { "client_id", account.ClientId ?? string.Empty },
                { "client_secret", account.ClientSecret ?? string.Empty },
                { "refresh_token", account.Token.RefreshToken ?? string.Empty },
                { "grant_type", "refresh_token" }
            };

            var content = new FormUrlEncodedContent(parameters);

            var response = await httpClient.PostAsync("oauth/token", content);
            if (!response.IsSuccessStatusCode)
            {
                var r = await response.Content.ReadAsStringAsync();
                logger.LogError("Token refresh error: " + response.ReasonPhrase);
                return false;
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<Token>(responseString);
            if (token is not null)
            {
                string tokenFilePath = Path.Combine(account.AccountDirectory, "Account.json");
                account.Token = token;
                File.WriteAllText(tokenFilePath, JsonSerializer.Serialize(account));
                logger.LogInformation("Token refreshed.");
                return true;
            }

            logger.LogError("Token turned out to be null, Refresh failed");
            return false;
        }

        public async Task DownloadActivities(Account account, string activitiesPath)
        {
            if (account.Token is null)
            {
                logger.LogError("No Token set, skipping Activities download");
                return;
            }

            if (account.Token.ExpiresAtDateTime < DateTime.Now)
            {
                bool refreshed = await RefreshToken(account);
                if (!refreshed)
                    return;
            }

            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://www.strava.com/api/v3/");
            httpClient.DefaultRequestHeaders.Authorization = new("Bearer", account.Token.AccessToken);

            List<ActivityHeader> activities = [];
            List<ActivityHeader>? currentRequestActivities = null;
            int page = 1;
            do
            {
                var response = await httpClient.GetAsync($"activities?page={page}&per_page=200");
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError(response.ReasonPhrase);
                    return;
                }
                var str = await response.Content.ReadAsStreamAsync();
                currentRequestActivities = JsonSerializer.Deserialize<List<ActivityHeader>>(str);
                activities.AddRange(currentRequestActivities ?? []);
                page++;
            } while (currentRequestActivities is not null && currentRequestActivities.Count > 0);

            logger.LogInformation($"Found {activities.Count} Activities for {account.Name}");

            // Get Activity:
            // https://www.strava.com/api/v3/activities/18894098488/streams?keys=time,distance,latlng,heartrate&key_by_type=true

            string targetDirectory = Path.Combine(activitiesPath, AppData.ActivitiesStravaFileLocation);

            if (!Directory.Exists(targetDirectory))
                Directory.CreateDirectory(targetDirectory);

            int loaded = 0;
            foreach (ActivityHeader activity in activities)
            {
                string activityPath = Path.Combine(targetDirectory, activity.Id + ".json");
                if (File.Exists(activityPath))
                {
                    loaded++;
                    logger.LogInformation($"Activity {loaded} / {activities.Count} for {account.Name} already Cached");
                    continue;
                }

                var response = await httpClient.GetAsync($"https://www.strava.com/api/v3/activities/{activity.Id}/streams?keys=time,distance,latlng,altitude,velocity_smooth,heartrate,cadence,watts,moving,grade_smooth&key_by_type=true");
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError($"Couldn't retrieve data for activity ({activity.Name} : {activity.Id}) {response.ReasonPhrase}");
                    continue;
                }

                var streamResponse = await response.Content.ReadAsStreamAsync();
                var rawActivity = JsonSerializer.Deserialize<RawActivity>(streamResponse);
                rawActivity.ActivityHeader = activity;
                File.WriteAllText(activityPath, JsonSerializer.Serialize(rawActivity));
                loaded++;
                logger.LogInformation($"Downloaded {loaded} / {activities.Count} Activities for {account.Name}");
            }
        }
    }
}
