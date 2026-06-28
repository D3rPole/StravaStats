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
    // Make properties readonly to prevent accidental mutation while used as dictionary keys
    public Coordinate StartNodeKey { get; }
    public Coordinate EndNodeKey { get; }

    public EdgeKey(Coordinate startNodeKey, Coordinate endNodeKey)
    {
        StartNodeKey = startNodeKey;
        EndNodeKey = endNodeKey;
    }

    // 1. High-performance, strongly-typed Equals (No boxing)
    public bool Equals(EdgeKey other)
    {
        // For an undirected graph: (A -> B) is the same as (B -> A)
        return (StartNodeKey.Equals(other.StartNodeKey) && EndNodeKey.Equals(other.EndNodeKey)) ||
               (StartNodeKey.Equals(other.EndNodeKey) && EndNodeKey.Equals(other.StartNodeKey));
    }

    // 2. Required object override
    public override bool Equals(object? obj)
    {
        return obj is EdgeKey other && Equals(other);
    }

    // 3. Critically Important: HashCode must be direction-agnostic!
    // Since (A -> B) == (B -> A), we sort or combine them in a way 
    // that order doesn't matter (e.g., using an XOR or addition, or sorting by hash)
    public override int GetHashCode()
    {
        int h1 = StartNodeKey.GetHashCode();
        int h2 = EndNodeKey.GetHashCode();

        // XOR (^) is commutative, meaning h1 ^ h2 gives the exact same result as h2 ^ h1.
        // This perfectly matches your bidirectional Equals logic.
        return h1 ^ h2;
    }

    // 4. Cleaned up operator overloads (Structs are not inherently nullable)
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
    public Coordinate StartNodeKey { get; set; }
    public Coordinate EndNodeKey { get; set; }
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

    public double DistanceToPoint(Node node, Dictionary<Coordinate, Node> nodes)
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
