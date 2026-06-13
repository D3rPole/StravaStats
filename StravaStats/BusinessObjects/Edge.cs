using NetTopologySuite.Geometries;
using StravaStats.Helper;

namespace StravaStats.BusinessObjects
{
    public class Edge
    {
        public string StartNodeKey { get; set; }
        public string EndNodeKey { get; set; }
        public long WayId { get; set; }
        public double Length { get; set; }
        public HashSet<string> ActivityFileNames { get; set; } = new();
        public int TotalSpeedDataPoints { get; set; }
        public int TotalHeartRateDataPoints { get; set; }
        public double TotalHeartRate { get; set; }
        public double TotalSpeed { get; set; }
        public double AverageHeartRate => TotalHeartRateDataPoints > 0 ? TotalHeartRate / TotalHeartRateDataPoints : 0;
        public double AverageSpeed => TotalSpeedDataPoints > 0 ? TotalSpeed / TotalSpeedDataPoints : 0;

        public void AddDataPoint(TrackingPoint trackingPoint)
        {
            if (trackingPoint.HeartRate.HasValue)
            {
                TotalHeartRate += trackingPoint.HeartRate ?? 0;
                TotalHeartRateDataPoints++;
            }
            if (trackingPoint.Velocity.HasValue)
            {
                TotalSpeed += trackingPoint.Velocity ?? 0;
                TotalSpeedDataPoints++;
            }
        }

        public void AddEdge(Edge edge)
        {
            ActivityFileNames.UnionWith(edge.ActivityFileNames);
            TotalHeartRate += edge.TotalHeartRate;
            TotalSpeed += edge.TotalSpeed;
            TotalSpeedDataPoints += edge.TotalSpeedDataPoints;
        }

        public double DistanceToPoint(Node node, Dictionary<string, Node> nodes)
        {
            var startNode = nodes[StartNodeKey];
            var endNode = nodes[EndNodeKey];
            double x = node.Longitude;
            double y = node.Latitude;
            double x1 = startNode.Longitude;
            double y1 = startNode.Latitude;
            double x2 = endNode.Longitude;
            double y2 = endNode.Latitude;

            double dx = x2 - x1;
            double dy = y2 - y1;

            if (dx == 0 && dy == 0)
            {
                return GeoUtils.CalculateDistance(node, startNode);
            }

            double t = ((x - x1) * dx + (y - y1) * dy) / (dx * dx + dy * dy);

            t = Math.Max(0, Math.Min(1, t));

            double closestLon = x1 + t * dx;
            double closestLat = y1 + t * dy;

            Node closestPointOnSegment = new Node(closestLat, closestLon);

            return GeoUtils.CalculateDistance(node, closestPointOnSegment);
        }

        public OpenLayers.Blazor.Coordinate GetCenter(Graph graph)
        {
            var center = GeoUtils.Interpolate(graph.Nodes[StartNodeKey], graph.Nodes[EndNodeKey], 0.5);
            return new(center.Longitude, center.Latitude);
        }
    }
}
