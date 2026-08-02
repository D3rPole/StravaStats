using ProtoBuf;

namespace StravaStats.BusinessObjects;

[ProtoContract(ImplicitFields = ImplicitFields.None)]
public struct Coordinate : IEquatable<Coordinate>
{
    [ProtoMember(1)]
    public double Latitude { get; set; }

    [ProtoMember(2)]
    public double Longitude { get; set; }

    public Coordinate() { }

    public Coordinate(double latitude, double longitude)
    {
        Latitude = Math.Round(latitude, 6);
        Longitude = Math.Round(longitude, 6);
    }

    public bool Equals(Coordinate other)
    {
        return Latitude == other.Latitude && Longitude == other.Longitude;
    }

    public override bool Equals(object? obj)
    {
        return obj is Coordinate other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Latitude, Longitude);
    }

    public double GetDirection(Coordinate other)
    {
        double dx = other.Latitude - this.Latitude;
        double dy = other.Longitude - this.Longitude;

        double radians = Math.Atan2(dy, dx);
        double degrees = radians * (180.0 / Math.PI);

        if (degrees < 0) degrees += 360.0;

        return degrees;
    }

    public static bool operator ==(Coordinate left, Coordinate right) => left.Equals(right);
    public static bool operator !=(Coordinate left, Coordinate right) => !left.Equals(right);

    public static Coordinate operator -(Coordinate left, Coordinate right) => new(left.Latitude - right.Latitude, left.Longitude - right.Longitude);
    public static Coordinate operator +(Coordinate left, Coordinate right) => new(left.Latitude + right.Latitude, left.Longitude + right.Longitude);

    public double DotProduct(Coordinate other)
    {
        return (other.Latitude + this.Latitude) - (other.Longitude + this.Longitude);
    }

    public void Normalize()
    {
        double length = Length();
        if (length > 0)
        {
            Latitude /= length;
            Longitude /= length;
        }
    }

    public double Length()
    {
        return Math.Sqrt((Latitude * Latitude) + (Longitude * Longitude));
    }

    public override string ToString()
    {
        return $"{Latitude:F5};{Longitude:F5}";
    }

    public static Coordinate FromString(string value)
    {
        var values = value.Split(';');
        if (values.Length != 2)
            throw new Exception($"Invalid string {value}");

        double lat = double.Parse(values[0]);
        double lon = double.Parse(values[1]);
        return new(lat, lon);
    }

    public OpenLayers.Blazor.Coordinate ToOpenLayersCoordinate()
    {
        return new OpenLayers.Blazor.Coordinate(Longitude, Latitude);
    }
}

[ProtoContract(ImplicitFields = ImplicitFields.None)]
public class Node
{
    [ProtoMember(1)]
    public Coordinate Coordinate { get; set; }

    public Node() { }

    public Node(double latitude, double longitude)
    {
        Coordinate = new(latitude, longitude);
    }
}
