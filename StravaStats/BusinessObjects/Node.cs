namespace StravaStats.BusinessObjects;

public struct Coordinate : IEquatable<Coordinate>
{
    public double Latitude { get; set; }
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

    public static bool operator ==(Coordinate left, Coordinate right) => left.Equals(right);
    public static bool operator !=(Coordinate left, Coordinate right) => !left.Equals(right);

    public override string ToString()
    {
        return $"{Latitude:F6};{Longitude:F6}";
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

public class Node
{
    public Coordinate Coordinate { get; set; }

    public Node() { }

    public Node(double latitude, double longitude)
    {
        Coordinate = new(latitude, longitude);
    }
}
