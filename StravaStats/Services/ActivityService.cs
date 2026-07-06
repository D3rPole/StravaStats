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
            var stravaFiles = Directory.EnumerateFiles(stravaActivitiesPath).ToList();
            logger.LogInformation($"Found {stravaFiles.Count} Activities");
            int loaded = 0;
            foreach (string stravaFile in stravaFiles)
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
                            loaded++;
                            logger.LogInformation($"Loaded cached Activity {loaded}/{stravaFiles.Count}: {act.ActivityHeader.Id}");
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
                    {
                        logger.LogError($"Activity {stravaFile} has no distance. Data is corrupted or gps data is missing.");
                        return;
                    }
                    var activity = new Activity(rawActivity);
                    await activity.MatchRoads(activitiesPath);
                    string activityJson = JsonSerializer.Serialize(activity);
                    File.WriteAllText(activityCachedFile, activityJson);
                    loaded++;
                    logger.LogInformation($"Created cached Activity {loaded}/{stravaFiles.Count}: {activity.ActivityHeader.Id}");
                    activities.Add(activity);
                });
                tasks.Add(task);
            }
            Task.WaitAll(tasks);
            return activities;
        }
    }
}
