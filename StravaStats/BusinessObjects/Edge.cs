using StravaStats.Helper;
using System.Text.Json.Serialization;

namespace StravaStats.BusinessObjects;

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

public struct EdgeKey : IEquatable<EdgeKey>
{
    public Coordinate StartNodeKey { get; set; }
    public Coordinate EndNodeKey { get; set; }

    public EdgeKey() { }

    public EdgeKey(Coordinate startNodeKey, Coordinate endNodeKey)
    {
        StartNodeKey = startNodeKey;
        EndNodeKey = endNodeKey;
    }

    public bool Equals(EdgeKey other)
    {
        return (StartNodeKey.Equals(other.StartNodeKey) && EndNodeKey.Equals(other.EndNodeKey)) ||
               (StartNodeKey.Equals(other.EndNodeKey) && EndNodeKey.Equals(other.StartNodeKey));
    }

    public override bool Equals(object? obj)
    {
        return obj is EdgeKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        int h1 = StartNodeKey.GetHashCode();
        int h2 = EndNodeKey.GetHashCode();

        return h1 ^ h2;
    }

    public static bool operator ==(EdgeKey left, EdgeKey right) => left.Equals(right);
    public static bool operator !=(EdgeKey left, EdgeKey right) => !left.Equals(right);

    public override string ToString()
    {
        return $"{StartNodeKey.Latitude:F6};{StartNodeKey.Longitude:F6};{EndNodeKey.Latitude:F6};{EndNodeKey.Longitude:F6}";
    }

    public static EdgeKey FromString(string value)
    {
        var values = value.Split(';');
        if (values.Length != 4)
            throw new Exception($"Invalid string {value}");

        double startLat = double.Parse(values[0]);
        double startLon = double.Parse(values[1]);
        double endLat = double.Parse(values[2]);
        double endLon = double.Parse(values[3]);
        return new(new(startLat, startLon), new(endLat, endLon));
    }
}

public class Edge
{
    public EdgeKey EdgeKey { get; set; }
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

    public double DistanceToPoint(Node node, Dictionary<Coordinate, Node> nodes)
    {
        var startNode = nodes[EdgeKey.StartNodeKey];
        var endNode = nodes[EdgeKey.EndNodeKey];
        double x = node.Coordinate.Longitude;
        double y = node.Coordinate.Latitude;
        double x1 = startNode.Coordinate.Longitude;
        double y1 = startNode.Coordinate.Latitude;
        double x2 = endNode.Coordinate.Longitude;
        double y2 = endNode.Coordinate.Latitude;

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
        var center = GeoUtils.Interpolate(
            graph.Nodes[EdgeKey.StartNodeKey],
            graph.Nodes[EdgeKey.EndNodeKey],
            0.5);
        return new(center.Coordinate.Longitude, center.Coordinate.Latitude);
    }
}
