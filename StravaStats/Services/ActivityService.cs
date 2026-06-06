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

            var files = Directory.GetFiles(activitiesPath, "*.fit.gz");
            List<BusinessObjects.Activity> activities = [];
            foreach (var file in files)
            {
                BusinessObjects.Activity activity = new();
                activity.FileName = Path.GetFileNameWithoutExtension(file);
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

                    double? lat = record.GetPositionLat();
                    double? lon = record.GetPositionLong();

                    if (lat is null || lon is null)
                        return;

                    object hrValue = record.GetFieldValue(3);
                    byte heartRate = 0;

                    if (hrValue != null)
                    {
                        heartRate = Convert.ToByte(hrValue);
                    }

                    TrackingPoint trackingPoint = new()
                    {
                        TimeStamp = record.GetTimestamp()?.GetDateTime(),
                        HeartRate = heartRate == 0 ? null : (float)heartRate,
                        Speed = record.GetSpeed() is null ? 0 : record.GetSpeed() * 3.6,
                        Latitude = lat.Value / 11930465.0,
                        Longitude = lon.Value / 11930465.0,
                        Distance = record.GetDistance() ?? 0
                    };
                    activity.TrackingPoints.Add(trackingPoint);
                };
                decoder.Read(fitStream);
                await activity.MatchRoads();
                activities.Add(activity);
            }
            _activities = activities;
            return _activities;
        }
    }
}
