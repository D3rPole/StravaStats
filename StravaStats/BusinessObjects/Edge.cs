using ProtoBuf;
using StravaStats.Helper;
using System.Text.Json.Serialization;

namespace StravaStats.BusinessObjects;

[ProtoContract]
public struct EdgeKey : IEquatable<EdgeKey>
{
    [ProtoMember(1)]
    public Coordinate StartNodeKey { get; set; }

    [ProtoMember(2)]
    public Coordinate EndNodeKey { get; set; }

    public EdgeKey() { }

    public EdgeKey(Coordinate startNodeKey, Coordinate endNodeKey)
    {
        StartNodeKey = startNodeKey;
        EndNodeKey = endNodeKey;
    }

    public bool Equals(EdgeKey other)
    {
        return (StartNodeKey.Equals(other.StartNodeKey) && EndNodeKey.Equals(other.EndNodeKey))
            || (StartNodeKey.Equals(other.EndNodeKey) && EndNodeKey.Equals(other.StartNodeKey));
    }

    public override bool Equals(object? obj)
    {
        return obj is EdgeKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StartNodeKey.GetHashCode() ^ EndNodeKey.GetHashCode();
    }

    public double GetDirection()
    {
        return StartNodeKey.GetDirection(new Coordinate(1,0));
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

[ProtoContract]
public class Edge
{
    [ProtoMember(1)]
    public EdgeKey EdgeKey { get; set; }

    [ProtoMember(2)]
    public HashSet<long> ActivityIds { get; set; } = new();

    [ProtoMember(3)]
    public Metrics AllMetrics { get; set; } = new();

    [ProtoMember(4)]
    public Metrics UphillMetrics { get; set; } = new();

    [ProtoMember(5)]
    public Metrics DownhillMetrics { get; set; } = new();

    public void AddDataPoint(TrackingPoint previousPoint, TrackingPoint trackingPoint)
    {
        var dir = previousPoint.Coordinate.GetDirection(trackingPoint.Coordinate);
        AllMetrics.AddDataPoint(trackingPoint);
        if (trackingPoint.Grade > 0) // Split be east and west direction
        {
            UphillMetrics.AddDataPoint(trackingPoint);
        }
        else
        {
            DownhillMetrics.AddDataPoint(trackingPoint);
        }
    }

    public void AddEdge(Edge edge)
    {
        ActivityIds.UnionWith(edge.ActivityIds);
        AllMetrics.AddMetrics(edge.AllMetrics);
        UphillMetrics.AddMetrics(edge.UphillMetrics);
        DownhillMetrics.AddMetrics(edge.DownhillMetrics);
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
