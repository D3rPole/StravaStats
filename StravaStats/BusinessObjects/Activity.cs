using Microsoft.AspNetCore.Mvc.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StravaStats.BusinessObjects
{
    public class Activity
    {
        public ActivityHeader ActivityHeader { get; set; }

        public List<TrackingPoint> TrackingPoints { get; set; } = [];

        public Graph Graph { get; set; }

        [JsonIgnore]
        public ValhallaResponse ValhallaResponse { get; set; }

        [JsonIgnore]
        ILogger<Activity> logger = AppData.GetService<ILogger<Activity>>();

        public Activity() { }

        public Activity(RawActivity rawActivity)
        {
            if (rawActivity.ActivityHeader is not null)
                this.ActivityHeader = rawActivity.ActivityHeader;

            for (int i = 0; i < rawActivity.Distance.Size; i++)
            {
                TrackingPoint trackingPoint = new TrackingPoint();
                trackingPoint.Distance = ((JsonElement)rawActivity.Distance.Data[i]).GetDouble();
                if (rawActivity.Time is not null)
                    trackingPoint.Time = ((JsonElement)rawActivity.Time.Data[i]).GetInt32();
                if (rawActivity.HeartRate is not null)
                    trackingPoint.HeartRate = ((JsonElement)rawActivity.HeartRate.Data[i]).GetDouble();
                if (rawActivity.Grade is not null)
                    trackingPoint.Grade = ((JsonElement)rawActivity.Grade.Data[i]).GetDouble();
                if (rawActivity.Moving is not null)
                    trackingPoint.Moving = ((JsonElement)rawActivity.Moving.Data[i]).GetBoolean();
                if (rawActivity.Altitude is not null)
                    trackingPoint.Altitude = ((JsonElement)rawActivity.Altitude.Data[i]).GetDouble();
                if (rawActivity.Time is not null)
                    trackingPoint.Time = ((JsonElement)rawActivity.Time.Data[i]).GetInt32();
                if (rawActivity.LatLng is not null)
                {
                    var obj = (JsonElement)rawActivity.LatLng.Data[i];

                    if (obj[0] is JsonElement lat)
                        trackingPoint.Latitude = lat.GetDouble();
                    if (obj[1] is JsonElement lon)
                        trackingPoint.Longitude = lon.GetDouble();
                }
                if(rawActivity.Velocity is not null && rawActivity.Time is not null)
                {
                    if (i > 0)
                    {
                        var beforeTrackingPoint = TrackingPoints[^1];
                        double deltaTime = trackingPoint.Time - beforeTrackingPoint.Time;
                        if(deltaTime > 0)
                        {
                            double deltaDistance = trackingPoint.Distance - beforeTrackingPoint.Distance;
                            trackingPoint.Velocity = deltaDistance / deltaTime;

                            double deltaVelocity = trackingPoint.Velocity - beforeTrackingPoint.Velocity;
                            trackingPoint.Acceleration = deltaVelocity / deltaTime;
                        }
                        else
                        {
                            // just use last known values
                            trackingPoint.Velocity = beforeTrackingPoint.Velocity;
                            trackingPoint.Acceleration = 0; // 0 makes more sense for constant velocity
                        }
                    }
                    else
                    {
                        trackingPoint.Velocity = 0;
                        trackingPoint.Acceleration = 0;
                    }
                }
                else
                {
                    trackingPoint.Velocity = 0;
                    trackingPoint.Acceleration = 0;
                }

                TrackingPoints.Add(trackingPoint);
            }
        }

        public async Task MatchRoads(string activitiesPath)
        {
            var configuration = AppData.GetService<IConfiguration>();
            string cacheDir = Path.Combine(activitiesPath, AppData.ActivitiesValhallaFileLocation);
            string cacheFilePath = Path.Combine(cacheDir, $"{ActivityHeader.Id}.json");

            if (File.Exists(cacheFilePath))
            {
                using var fileStream = File.OpenRead(cacheFilePath);
                ValhallaResponse = JsonSerializer.Deserialize<ValhallaResponse>(fileStream);

                if (ValhallaResponse is not null)
                {
                    MatchTrackingPointsToValhallaResponse();
                    return;
                }
            }

            HttpClient client = new();
            var response = await client.PostAsJsonAsync($"{configuration["ValhallaServer"]}/trace_attributes", new
            {
                shape = TrackingPoints.Select(tp => new { lat = tp.Latitude, lon = tp.Longitude }).ToArray(),
                costing = "pedestrian",
                shape_match = "map_snap",
                filters = new
                {
                    attributes = new string[] {
                        "edge.way_id",
                        "edge.length",
                        "edge.id",
                        "edge.elapsed_time",
                        "matched.point",
                        "matched.type",
                        "matched.edge_index",
                        "matched.distance_along_edge",
                        "shape",
                        "edge.begin_shape_index",
                        "edge.end_shape_index",
                        "edge.begin_node_id",
                        "edge.end_node_id",
                    },
                    action = "include"
                }
            });

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError($"Valhalla request failed with status code: {response.StatusCode}");
                return;
            }
            ValhallaResponse = await response.Content.ReadFromJsonAsync<ValhallaResponse>();
            if (ValhallaResponse is null)
            {
                logger.LogError("Couldn't parse Valhalla response");
                return;
            }
            logger.LogInformation("Retrieved Valhalla response");
            MatchTrackingPointsToValhallaResponse();

            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }
            string json = JsonSerializer.Serialize(ValhallaResponse);
            File.WriteAllText(cacheFilePath, json);
        }

        private void MatchTrackingPointsToValhallaResponse()
        {
            int a = 0;
            for (int i = 0; i < TrackingPoints.Count; i++)
            {
                TrackingPoints[i].Latitude = ValhallaResponse.MatchedPoints[i].Lat;
                TrackingPoints[i].Longitude = ValhallaResponse.MatchedPoints[i].Lon;
            }
            Graph = new Graph([this]);
        }
    }
}
