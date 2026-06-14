using StravaStats.BusinessObjects;
using System.Text.Json;

namespace StravaStats.Services
{
    public class ActivityService(ILogger<ActivityService> logger)
    {
        public async Task<List<Activity>> GetActivities(string activitiesPath)
        {
            string stravaActivitiesPath = Path.Combine(activitiesPath, AppData.ActivitiesStravaFileLocation);
            string activityCache = Path.Combine(activitiesPath, AppData.ActivitiesCacheLocation);

            if (!Directory.Exists(activitiesPath))
                Directory.CreateDirectory(activitiesPath);

            if (!Directory.Exists(activityCache))
                Directory.CreateDirectory(activityCache);

            if (!Directory.Exists(stravaActivitiesPath))
                Directory.CreateDirectory(stravaActivitiesPath);

            List<Task> tasks = [];
            List<Activity> activities = [];
            foreach (string file in Directory.EnumerateFiles(stravaActivitiesPath))
            {
                var task = Task.Run(async () =>
                {
                    string activityCachedFile = Path.Combine(activityCache, Path.GetFileNameWithoutExtension(file) + ".json");
                    if (File.Exists(activityCachedFile))
                    {
                        string actJson = await File.ReadAllTextAsync(activityCachedFile);
                        Activity? act = JsonSerializer.Deserialize<Activity>(actJson);

                        if (act is not null)
                        {
                            activities.Add(act);
                            return;
                        }
                    }

                    string jsonString = await File.ReadAllTextAsync(file);
                    var rawActivity = JsonSerializer.Deserialize<RawActivity>(jsonString);
                    if (rawActivity is null)
                    {
                        logger.LogError($"Couldn't load Activity: {file}");
                        return;
                    }
                    if (rawActivity.Distance is null)
                        return;
                    var activity = new Activity(rawActivity, Path.GetFileNameWithoutExtension(file));
                    await activity.MatchRoads(activitiesPath);
                    string activityJson = JsonSerializer.Serialize(activity);
                    File.WriteAllText(activityCachedFile, activityJson);
                    activities.Add(activity);
                });
                tasks.Add(task);
            }
            Task.WaitAll(tasks);
            return activities;
        }
    }
}
