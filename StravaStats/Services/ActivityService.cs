using StravaStats.BusinessObjects;
using System.Text.Json;

namespace StravaStats.Services
{
    public class ActivityService(
        IConfiguration configuration,
        ILogger<ActivityService> logger)
    {
        private readonly IConfiguration _configuration = configuration;

        private List<BusinessObjects.Activity> activities = [];

        public async Task<List<BusinessObjects.Activity>> GetActivities()
        {
            if (activities.Count > 0)
                return activities;

            string activitiesDirectory = Path.Combine(AppData.DataDirectory, "Activities");
            if (!Directory.Exists(activitiesDirectory))
                return [];

            foreach(string file in Directory.EnumerateFiles(activitiesDirectory))
            {
                string jsonString = await File.ReadAllTextAsync(file);
                var rawActivity = JsonSerializer.Deserialize<RawActivity>(jsonString);
                if(rawActivity is null)
                {
                    logger.LogError($"Couldn't load Activity: {file}");
                    continue;
                }
                if (rawActivity.Distance is null)
                    continue;
                var activity = new Activity(rawActivity);
                activity.FileName = Path.GetFileName(file);
                await activity.MatchRoads();
                activities.Add(activity);
            }
            return activities;
        }
    }
}
