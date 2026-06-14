using NetTopologySuite.Geometries;
using StravaStats.Helper;
using System.Text.Json.Serialization;

namespace StravaStats.BusinessObjects
{
    public class MetricSummary
    {
        [JsonIgnore]
        public double Average => count == 0 ? 0 : totalValue / count;
        [JsonInclude]
        private double totalValue { get; set; }
        [JsonInclude]
        private int count { get; set; }
        public void AddMetric(double? value)
        {
            if (value is null) return;
            count++;
            totalValue += value.Value;
        }

        public void AddMetric(MetricSummary metric)
        {
            if (metric is null) return;
            count += metric.count;
            totalValue += metric.totalValue;
        }
    }
    public class Edge
    {
        public string StartNodeKey { get; set; }
        public string EndNodeKey { get; set; }
        public long WayId { get; set; }
        public double Length { get; set; }
        public HashSet<string> ActivityFileNames { get; set; } = new();

        public MetricSummary HeartRate { get; set; } = new();
        public MetricSummary Velocity { get; set; } = new();
        public MetricSummary Grade { get; set; } = new();
        public MetricSummary Wattage { get; set; } = new();
        public MetricSummary Acceleration { get; set; } = new();

        public void AddDataPoint(TrackingPoint trackingPoint)
        {
            HeartRate.AddMetric(trackingPoint.HeartRate);
            Velocity.AddMetric(trackingPoint.Velocity);
            Grade.AddMetric(trackingPoint.Grade);
            Wattage.AddMetric(trackingPoint.Watt);
            Acceleration.AddMetric(trackingPoint.Acceleration);
        }

        public void AddEdge(Edge edge)
        {
            ActivityFileNames.UnionWith(edge.ActivityFileNames);
            HeartRate.AddMetric(edge.HeartRate);
            Velocity.AddMetric(edge.Velocity);
            Grade.AddMetric(edge.Grade);
            Wattage.AddMetric(edge.Wattage);
            Acceleration.AddMetric(edge.Acceleration);
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
