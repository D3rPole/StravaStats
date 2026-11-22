using Microsoft.Data.Sqlite;
using StravaStats.BusinessObjects;
using System.Diagnostics;
using System.Text.Json;
using Activity = StravaStats.BusinessObjects.Activity;

namespace StravaStats.Services
{
    public class ActivityService
    {
        private SqliteConnectionStringBuilder? connectionStringBuilder;
        private readonly ILogger<ActivityService> logger;
        private readonly string citiesFilePath;

        public ActivityService(ILogger<ActivityService> logger, IConfiguration configuration)
        {
            citiesFilePath = configuration.GetValue<string>("CacheFile") ?? "";
            if (string.IsNullOrWhiteSpace(citiesFilePath))
                throw new Exception("CacheFile is not configured!!");

            this.logger = logger;
        }

        public async Task InitializeAsync()
        {
            connectionStringBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = citiesFilePath,
                Mode = SqliteOpenMode.ReadWriteCreate
            };

            using var conn = new SqliteConnection(connectionStringBuilder.ToString());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS store (
                account TEXT NOT NULL,
                key     TEXT NOT NULL,
                value   BLOB NOT NULL,
                PRIMARY KEY (account, key)
            ) WITHOUT ROWID;";
            cmd.ExecuteNonQuery();

            connectionStringBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = citiesFilePath,
                Mode = SqliteOpenMode.ReadWrite // create isnt needed at this point
            };
        }

        public async Task<List<Activity>> GetActivities(string activitiesPath, string accountName)
        {
            string stravaActivitiesPath = Path.Combine(activitiesPath, AppData.ActivitiesStravaFileLocation);
            string activityCache = Path.Combine(activitiesPath, AppData.ActivitiesCacheLocation);

            if (!Directory.Exists(activitiesPath))
                Directory.CreateDirectory(activitiesPath);

            if (!Directory.Exists(activityCache))
                Directory.CreateDirectory(activityCache);

            if (!Directory.Exists(stravaActivitiesPath))
                Directory.CreateDirectory(stravaActivitiesPath);

            var cachedActivities = await LoadActivities(accountName);
            logger.LogInformation($"Loaded {cachedActivities.Count} Activities for {accountName} from DB");

            List<Task> tasks = [];
            List<Activity> activities = [];
            List<Activity> activitiesToSave = [];
            List<string> stravaFiles = Directory.EnumerateFiles(stravaActivitiesPath).ToList();
            List<string> stravaFilesToCreate = [];
            foreach (string stravaFile in stravaFiles)
            {
                if (!long.TryParse(Path.GetFileNameWithoutExtension(stravaFile), out long activityId))
                    continue;
                if (cachedActivities.Any(a => a.ActivityId == activityId))
                    continue;
                stravaFilesToCreate.Add(stravaFile);
            }
            logger.LogInformation($"Found {stravaFiles.Count} Activities for {accountName}. {stravaFilesToCreate.Count} Need to be Parsed.");
            int loaded = 0;
            foreach (string stravaFile in stravaFilesToCreate)
            {
                if (!long.TryParse(Path.GetFileNameWithoutExtension(stravaFile), out long activityId))
                    continue;
                if (cachedActivities.Any(a => a.ActivityId == activityId))
                    continue;
                var task = Task.Run(async () =>
                {
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
                    loaded++;
                    logger.LogInformation($"Created cached Activity for {accountName}: {activity.ActivityHeader.Id} ({loaded} / {stravaFilesToCreate.Count})");
                    activitiesToSave.Add(activity);
                });
                tasks.Add(task);
            }
            Task.WaitAll(tasks);
            await SaveActivities(accountName, activitiesToSave);
            return activitiesToSave.Concat(cachedActivities).ToList();
        }

        private async Task<List<Activity>> LoadActivities(string account)
        {
            try
            {
                using var conn = new SqliteConnection(connectionStringBuilder.ToString());
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT key, value FROM store where account = $a;";
                var pAccount = cmd.Parameters.Add("$a", SqliteType.Text);
                pAccount.Value = account;
                using var reader = await cmd.ExecuteReaderAsync();
                List<Activity> activities = [];
                while (reader.Read())
                {
                    if (reader.IsDBNull(0) || reader.IsDBNull(1))
                        continue;

                    string key = reader.GetString(0);
                    Stream value = reader.GetStream(1);
                    Activity? activity = ProtoBuf.Serializer.Deserialize<Activity>(value);
                    if (activity is not null)
                        activities.Add(activity);
                }
                return activities;
            }
            catch (Exception ex)
            {
                logger.LogError("Couldn't load activities from database\n{0}", ex.ToString());
                return [];
            }
        }

        private async Task SaveActivities(string account, List<BusinessObjects.Activity> activities)
        {
            SqliteTransaction? tx = null;
            try
            {
                using var conn = new SqliteConnection(connectionStringBuilder.ToString());
                conn.Open();
                tx = conn.BeginTransaction();
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT OR REPLACE INTO store (account, key, value) VALUES ($a, $k, $v)";

                var pAccount = cmd.Parameters.Add("$a", SqliteType.Text);
                var pKey = cmd.Parameters.Add("$k", SqliteType.Text);
                var pVal = cmd.Parameters.Add("$v", SqliteType.Blob);
                cmd.Prepare();

                pAccount.Value = account;
                using var stream = new MemoryStream();
                foreach (var activity in activities)
                {
                    stream.SetLength(0);
                    ProtoBuf.Serializer.Serialize(stream, activity);

                    pKey.Value = activity.ActivityId;
                    pVal.Value = stream.ToArray();
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch (Exception ex)
            {
                if (tx is not null)
                    tx.Rollback();
                logger.LogError("Failed to save activities to database\n{0}", ex.ToString());
            }
            finally
            {
                if (tx is not null)
                    await tx.DisposeAsync();
            }
        }
    }
}
