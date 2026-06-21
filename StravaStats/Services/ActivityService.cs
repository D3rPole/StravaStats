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
            foreach (string stravaFile in Directory.EnumerateFiles(stravaActivitiesPath))
            {
                var task = Task.Run(async () =>
                {
                    string activityCachedFile = Path.Combine(activityCache, Path.GetFileNameWithoutExtension(stravaFile) + ".json");
                    if (File.Exists(activityCachedFile))
                    {
                        using var fileStream = File.OpenRead(activityCachedFile);
                        Activity? act = JsonSerializer.Deserialize<Activity>(fileStream);

                        if (act is not null)
                        {
                            activities.Add(act);
                            return;
                        }
                    }
                    using var stravaFileStream = File.OpenRead(stravaFile);
                    var rawActivity = JsonSerializer.Deserialize<RawActivity>(stravaFileStream);
                    if (rawActivity is null)
                    {
                        logger.LogError($"Couldn't load Activity: {stravaFile}");
                        return;
                    }
                    if (rawActivity.Distance is null)
                        return;
                    var activity = new Activity(rawActivity);
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
