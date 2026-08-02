using ProtoBuf;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StravaStats.BusinessObjects;

[ProtoContract]
public class Activity
{
    [ProtoMember(1)]
    public long ActivityId { get; set; }

    [ProtoMember(2)]
    public ActivityHeader ActivityHeader { get; set; }

    [ProtoMember(3)]
    public List<TrackingPoint> TrackingPoints { get; set; } = [];

    [ProtoMember(4)]
    public Graph Graph { get; set; }

    [JsonIgnore]
    public ValhallaTraceAttributesResponse ValhallaTraceResponse { get; set; }

    [JsonIgnore]
    ILogger<Activity> logger = AppData.GetService<ILogger<Activity>>();

    public Activity() { }

    public Activity(RawActivity rawActivity)
    {
        if (rawActivity.ActivityHeader is not null)
        {
            this.ActivityHeader = rawActivity.ActivityHeader;
            ActivityId = rawActivity.ActivityHeader.Id;
        }
        else
        {
            throw new InvalidOperationException("Header is null");
        }

        for (int i = 0; i < rawActivity.Distance.Size; i++)
        {
            TrackingPoint trackingPoint = new TrackingPoint();
            trackingPoint.Distance = ((JsonElement)rawActivity.Distance.Data[i]).GetDouble();
            if (rawActivity.Time is not null)
                trackingPoint.Time = ((JsonElement)rawActivity.Time.Data[i]).GetInt32();
            if (rawActivity.HeartRate is not null)
                trackingPoint.HeartRate = ((JsonElement)rawActivity.HeartRate.Data[i]).GetDouble();
            if (rawActivity.Moving is not null)
                trackingPoint.Moving = ((JsonElement)rawActivity.Moving.Data[i]).GetBoolean();
            if (rawActivity.Altitude is not null)
                trackingPoint.Altitude = ((JsonElement)rawActivity.Altitude.Data[i]).GetDouble();
            if (rawActivity.Time is not null)
                trackingPoint.Time = ((JsonElement)rawActivity.Time.Data[i]).GetInt32();
            if (rawActivity.Velocity is not null)
                trackingPoint.VelocitySmooth = ((JsonElement)rawActivity.Velocity.Data[i]).GetDouble();
            if (rawActivity.Grade is not null)
            {
                if (trackingPoint.VelocitySmooth < 0.8)
                {
                    trackingPoint.Grade = 0;
                }
                else
                {
                    trackingPoint.Grade = ((JsonElement)rawActivity.Grade.Data[i]).GetDouble();
                }
            }
            if (rawActivity.LatLng is not null)
            {
                var obj = (JsonElement)rawActivity.LatLng.Data[i];

                if (obj[0] is JsonElement lat)
                    trackingPoint.Latitude = lat.GetDouble();
                if (obj[1] is JsonElement lon)
                    trackingPoint.Longitude = lon.GetDouble();
            }

            TrackingPoints.Add(trackingPoint);
        }

        const int range = 10;

        var window = new Queue<double>();

        for (int i = 0; i < TrackingPoints.Count; i++)
        {
            var trackingPoint = TrackingPoints[i];
            if (trackingPoint.Grade is null)
            {
                window.Clear();
                continue;
            }
            window.Enqueue(trackingPoint.Grade.Value);
            if (window.Count > range)
            {
                window.Dequeue();
            }

            trackingPoint.Grade = window.Average();
        }

        for (int i = 1; i < TrackingPoints.Count; i++)
        {
            var trackingPoint = TrackingPoints[i];
            var lastTrackingPoint = TrackingPoints[i - 1];
            var lastTrackingPoints = TrackingPoints
                .Where((p, index) => index >= i - range && index < i)
                .ToList();

            var deltaTime = trackingPoint.Time - lastTrackingPoint.Time;
            var deltaSpeed = trackingPoint.VelocitySmooth - lastTrackingPoint.VelocitySmooth;
            if (deltaSpeed > 20 || deltaTime == 0)
                continue;
            double rawAcceleration = deltaSpeed / deltaTime;
            if (rawAcceleration > 6)
                continue;
            double accelerationSum = lastTrackingPoints.Sum(p => p.Acceleration ?? 0) + rawAcceleration;
            trackingPoint.Acceleration = accelerationSum / (lastTrackingPoints.Count + 1);
        }

        foreach (var trackingPoint in TrackingPoints)
        {
            CalculatePower(trackingPoint);
        }
    }

    public void CalculatePower(TrackingPoint trackingPoint)
    {
        if (trackingPoint.Grade is null || trackingPoint.Acceleration is null || trackingPoint.VelocitySmooth <= 0.1)
        {
            trackingPoint.Watt = 0;
            return;
        }

        double mass = 100;
        double velocity = trackingPoint.VelocitySmooth;
        double grade = trackingPoint.Grade.Value / 100.0;

        double denominator = Math.Sqrt(1 + grade * grade);
        double sinGrade = grade / denominator;
        double cosGrade = 1 / denominator;

        double acceleration = trackingPoint.Acceleration.Value;
        double gravity = 9.81;
        double rollingResistanceCoefficent = 0.005;
        double airDensity = 1.225;
        double dragCoefficient = 0.32;
        double driveTrainEfficiency = 0.95;

        double gravityPower = mass * gravity * sinGrade * velocity;
        double powerDrag = 0.5 * airDensity * dragCoefficient * Math.Pow(velocity, 3);
        double powerRolling = rollingResistanceCoefficent * mass * gravity * cosGrade * velocity;
        double powerAcceleration = mass * acceleration * velocity;

        double totalWheelPower = gravityPower + powerDrag + powerRolling + powerAcceleration;

        double riderPower = totalWheelPower / driveTrainEfficiency;

        trackingPoint.Watt = riderPower;
    }

    public async Task MatchRoads(string activitiesPath)
    {
        var configuration = AppData.GetService<IConfiguration>();
        string cacheDir = Path.Combine(activitiesPath, AppData.ActivitiesValhallaFileLocation);
        string cacheFilePath = Path.Combine(cacheDir, $"{ActivityHeader.Id}.json");

        if (File.Exists(cacheFilePath))
        {
            using var fileStream = File.OpenRead(cacheFilePath);
            ValhallaTraceResponse = JsonSerializer.Deserialize<ValhallaTraceAttributesResponse>(fileStream);

            if (ValhallaTraceResponse is not null)
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
        ValhallaTraceResponse = await response.Content.ReadFromJsonAsync<ValhallaTraceAttributesResponse>();
        if (ValhallaTraceResponse is null)
        {
            logger.LogError("Couldn't parse Valhalla response");
            return;
        }
        logger.LogInformation("Retrieved Valhalla response");
        MatchTrackingPointsToValhallaResponse();

        var request = new ValhallaLocations()
        {
            Locations = [
                new ValhallaLocation() { Lat = TrackingPoints[0].Latitude, Lon = TrackingPoints[0].Longitude},
                new ValhallaLocation() { Lat = TrackingPoints[^1].Latitude, Lon = TrackingPoints[^1].Longitude}
            ]
        };
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        string uri = $"{configuration["ValhallaServer"]}/locate?json={JsonSerializer.Serialize(request, options)}";
        response = await client.GetAsync(uri);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError($"Valhalla locations request failed with status code: {response.StatusCode}");
        }
        else
        {
            ValhallaTraceResponse.ValhallaLocationsResponse = await response.Content.ReadFromJsonAsync<List<ValhallaLocationsResponse>>(options);
            logger.LogInformation("Retrieved Valhalla location response");
        }

        if (!Directory.Exists(cacheDir))
        {
            Directory.CreateDirectory(cacheDir);
        }
        string json = JsonSerializer.Serialize(ValhallaTraceResponse);
        File.WriteAllText(cacheFilePath, json);
    }

    private void MatchTrackingPointsToValhallaResponse()
    {
        int a = 0;
        for (int i = 0; i < TrackingPoints.Count; i++)
        {
            Coordinate coordinate = new(ValhallaTraceResponse.MatchedPoints[i].Lat, ValhallaTraceResponse.MatchedPoints[i].Lon);
            TrackingPoints[i].Latitude = coordinate.Latitude;
            TrackingPoints[i].Longitude = coordinate.Longitude;
        }
        Graph = new Graph([this]);
    }
}
