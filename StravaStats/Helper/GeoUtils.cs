using StravaStats.BusinessObjects;
using System;

namespace StravaStats.Helper;

public class GeoUtils
{
    private const double EarthRadius = 6371000.0;

    public static double CalculateEdgeLength(Edge edge, Dictionary<string, Node> nodes)
    {
        var startNode = nodes[edge.StartNodeKey];
        var endNode = nodes[edge.EndNodeKey];
        return CalculateDistance(startNode, endNode);
    }

    public static double CalculateDistance(Node startNode, Node endNode)
    {
        double lat1 = startNode.Latitude;
        double lon1 = startNode.Longitude;

        double lat2 = endNode.Latitude;
        double lon2 = endNode.Longitude;

        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadius * c;
    }

    public static Node Interpolate(Node startNode, Node endNode, double fraction)
    {
        double lat1 = startNode.Latitude;
        double lon1 = startNode.Longitude;

        double lat2 = endNode.Latitude;
        double lon2 = endNode.Longitude;

        fraction = Math.Clamp(fraction, 0.0, 1.0);

        double lat = lat1 + (lat2 - lat1) * fraction;

        double dLon = lon2 - lon1;
        if (dLon > 180) dLon -= 360;
        if (dLon < -180) dLon += 360;

        double lon = lon1 + dLon * fraction;

        // Ensure longitude stays within -180 to 180 bounds
        if (lon > 180) lon -= 360;
        if (lon < -180) lon += 360;

        return new Node
        {
            Latitude = lat,
            Longitude = lon
        };
    }

    private static double ToRadians(double angle)
    {
        return Math.PI * angle / 180.0;
    }
}
