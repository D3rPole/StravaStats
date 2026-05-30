using Dynastream.Fit;
using StravaStats.BusinessObjects;
using System.IO.Compression;

namespace StravaStats.Services
{
    public class ActivityService(
        IConfiguration configuration)
    {
        private readonly IConfiguration _configuration = configuration;

        private List<BusinessObjects.Activity> _activities;

        public async Task<List<BusinessObjects.Activity>> GetActivities()
        {
            if(_activities is not null && _activities.Count > 0)
                return _activities;
            var activitiesPath = _configuration["ActivitiesPath"];
            if (string.IsNullOrWhiteSpace(activitiesPath))
                throw new ArgumentNullException(nameof(activitiesPath));

            if (!Directory.Exists(activitiesPath))
            {
                Directory.CreateDirectory(activitiesPath);
                return [];
            }

            var files = Directory.GetFiles(activitiesPath, "*.gz");
            List<BusinessObjects.Activity> activities = [];
            foreach (var file in files)
            {
                BusinessObjects.Activity activity = new();
                using var fileStream = System.IO.File.OpenRead(file);
                using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);

                var fitStream = new MemoryStream();
                await gzipStream.CopyToAsync(fitStream);
                fitStream.Position = 0;

                Decode decoder = new();
                MesgBroadcaster broadcaster = new();
                decoder.MesgEvent += broadcaster.OnMesg;
                decoder.MesgDefinitionEvent += broadcaster.OnMesgDefinition;

                broadcaster.RecordMesgEvent += (object sender, MesgEventArgs e) =>
                {
                    if (e.mesg is not RecordMesg record)
                        return;

                    TrackingPoint trackingPoint = new()
                    {
                        TimeStamp = record.GetTimestamp()?.GetDateTime(),
                        HeartRate = record.GetHeartRate(),
                        Speed = record.GetSpeed(),
                        Latitude = record.GetPositionLat() / 11930465.0,
                        Longitude = record.GetPositionLong() / 11930465.0
                    };
                    activity.TrackingPoints.Add(trackingPoint);
                };
                decoder.Read(fitStream);
                activities.Add(activity);
            }
            _activities = activities;
            return activities;
        }

        public async Task<ActivityGrid> GetActivityGrid()
        {
            var activities = await GetActivities();
            var grid = new ActivityGrid(activities);
            return grid;
        }
    }
}
