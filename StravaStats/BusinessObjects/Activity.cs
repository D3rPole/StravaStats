using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using PolylinerNet;
using System.Text.Json;

namespace StravaStats.BusinessObjects
{
    public class Activity
    {
        public string FileName { get; set; }
        public List<TrackingPoint> TrackingPoints { get; set; } = [];
        public List<TrackingPoint> SimplifiedTrackingPoint { get; set; } = [];
        public ValhallaResponse ValhallaResponse { get; set; }

        public void Simplify()
        {
            SimplifiedTrackingPoint = DouglasPeucker(TrackingPoints, 0.00001);
        }

        public async Task MatchRoads()
        {
            var configuration = AppServices.GetService<IConfiguration>();
            string? activitiesPath = configuration["ActivitiesPath"];
            string cacheDir = Path.Combine(activitiesPath, "cache");
            string cacheFilePath = Path.Combine(cacheDir, $"{FileName}.json");

            if(File.Exists(cacheFilePath))
            {
                string cachedContent = await File.ReadAllTextAsync(cacheFilePath);
                ValhallaResponse = JsonSerializer.Deserialize<ValhallaResponse>(cachedContent);
                return;
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
                Console.WriteLine($"Valhalla request failed with status code: {response.StatusCode}");
                Console.WriteLine(await response.Content.ReadAsStringAsync());
                return;
            }
            ValhallaResponse = await response.Content.ReadFromJsonAsync<ValhallaResponse>();

            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            string json = JsonSerializer.Serialize(ValhallaResponse);
            File.WriteAllText(cacheFilePath, json);

        }


        private List<TrackingPoint> DouglasPeucker(List<TrackingPoint> points, double tolerance)
        {
            if (points.Count < 3)
                return points;
            var start = points[0];
            var end = points[^1];
            double maxPerpendicularDistance = 0;
            int index = 0;
            for (int i = 1; i < points.Count - 1; i++)
            {
                double perpendicularDistance = PerpendicularDistance(points[i], start, end);
                if (perpendicularDistance > maxPerpendicularDistance)
                {
                    maxPerpendicularDistance = perpendicularDistance;
                    index = i;
                }
            }
            if (maxPerpendicularDistance > tolerance)
            {
                var left = DouglasPeucker(points.GetRange(0, index + 1), tolerance);
                var right = DouglasPeucker(points.GetRange(index, points.Count - index), tolerance);
                return left.Take(left.Count - 1).Concat(right).ToList();
            }
            /*else if (Distance(start, end) > 1)
            {
                // Force a split right down the middle index of the array to break up the straight line evenly
                int midIndex = points.Count / 2;

                var left = DouglasPeucker(points.GetRange(0, midIndex + 1), tolerance);
                var right = DouglasPeucker(points.GetRange(midIndex, points.Count - midIndex), tolerance);
                return left.Take(left.Count - 1).Concat(right).ToList();
            }*/
            else
            {
                return new List<TrackingPoint> { start, end };
            }
        }

        private double PerpendicularDistance(TrackingPoint point, TrackingPoint lineStart, TrackingPoint lineEnd)
        {
            double dx = lineEnd.Longitude - lineStart.Longitude;
            double dy = lineEnd.Latitude - lineStart.Latitude;
            if (dx == 0 && dy == 0)
                return Math.Sqrt(Math.Pow(point.Longitude - lineStart.Longitude, 2) + Math.Pow(point.Latitude - lineStart.Latitude, 2));
            double t = ((point.Longitude - lineStart.Longitude) * dx + (point.Latitude - lineStart.Latitude) * dy) / (dx * dx + dy * dy);
            if (t < 0)
                return Math.Sqrt(Math.Pow(point.Longitude - lineStart.Longitude, 2) + Math.Pow(point.Latitude - lineStart.Latitude, 2));
            else if (t > 1)
                return Math.Sqrt(Math.Pow(point.Longitude - lineEnd.Longitude, 2) + Math.Pow(point.Latitude - lineEnd.Latitude, 2));
            else
            {
                double projX = lineStart.Longitude + t * dx;
                double projY = lineStart.Latitude + t * dy;
                return Math.Sqrt(Math.Pow(point.Longitude - projX, 2) + Math.Pow(point.Latitude - projY, 2));
            }
        }

        private double Distance(TrackingPoint p1, TrackingPoint p2)
        {
            double dx = p1.Longitude - p2.Longitude;
            double dy = p1.Latitude - p2.Latitude;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
