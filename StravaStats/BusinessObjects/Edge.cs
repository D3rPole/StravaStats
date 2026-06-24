using StravaStats.Helper;
using System.Text.Json.Serialization;

namespace StravaStats.BusinessObjects
{
    public class MetricSummary
    {
        [JsonIgnore]
        public double MaxValue => maxValue == double.MinValue ? 0 : maxValue;

        [JsonIgnore]
        public double Average => count == 0 ? 0 : totalValue / count;

        [JsonInclude]
        private double maxValue = double.MinValue;

        [JsonInclude]
        private double totalValue { get; set; }
        [JsonInclude]
        private int count { get; set; }
        public void AddMetric(double? value)
        {
            if (value is null) return;
            count++;
            totalValue += value.Value;
            if (maxValue < value.Value)
                maxValue = value.Value;
        }

        public void AddMetric(MetricSummary metric)
        {
            if (metric is null) return;
            count += metric.count;
            totalValue += metric.totalValue;
            if (maxValue < metric.maxValue)
                maxValue = metric.maxValue;
        }
    }
    public class Metrics
    {
        public MetricSummary HeartRate { get; set; } = new();
        public MetricSummary Speed { get; set; } = new();
        public MetricSummary Grade { get; set; } = new();
        public MetricSummary Wattage { get; set; } = new();
        public MetricSummary Acceleration { get; set; } = new();

        public void AddDataPoint(TrackingPoint trackingPoint)
        {
            HeartRate.AddMetric(trackingPoint.HeartRate);
            Speed.AddMetric(trackingPoint.SpeedKmh);
            Grade.AddMetric(trackingPoint.Grade);
            Wattage.AddMetric(trackingPoint.Watt);
            Acceleration.AddMetric(trackingPoint.Acceleration);
        }

        public void AddEdge(Edge edge)
        {
            HeartRate.AddMetric(edge.Metrics.HeartRate);
            Speed.AddMetric(edge.Metrics.Speed);
            Grade.AddMetric(edge.Metrics.Grade);
            Wattage.AddMetric(edge.Metrics.Wattage);
            Acceleration.AddMetric(edge.Metrics.Acceleration);
        }
    }
    public class Edge
    {
        public string StartNodeKey { get; set; }
        public string EndNodeKey { get; set; }
        public long WayId { get; set; }
        public double Length { get; set; }
        public HashSet<string> ActivityIds { get; set; } = new();

        public Metrics Metrics { get; set; } = new();

        public void AddDataPoint(TrackingPoint trackingPoint)
        {
            Metrics.AddDataPoint(trackingPoint);
        }

        public void AddEdge(Edge edge)
        {
            ActivityIds.UnionWith(edge.ActivityIds);
            Metrics.AddEdge(edge);
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
