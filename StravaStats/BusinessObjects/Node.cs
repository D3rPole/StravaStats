namespace StravaStats.BusinessObjects;

public struct Coordinate : IEquatable<Coordinate>
{
    // 1. Make fields/properties readonly if possible. 
    // Dictionaries love immutable keys. If a key changes its internal state, 
    // its HashCode changes, and the dictionary breaks.
    public double Latitude { get; }
    public double Longitude { get; }

    public Coordinate(double latitude, double longitude)
    {
        Latitude = Math.Round(latitude, 6);
        Longitude = Math.Round(longitude, 6);
    }

    // 2. High-performance Equality check (No boxing)
    public bool Equals(Coordinate other)
    {
        return Latitude == other.Latitude && Longitude == other.Longitude;
    }

    // 3. Required override for object compatibility
    public override bool Equals(object? obj)
    {
        return obj is Coordinate other && Equals(other);
    }

    // 4. Fast, collision-resistant HashCode generation
    public override int GetHashCode()
    {
        return HashCode.Combine(Latitude, Longitude);
    }

    // 5. Operator overloads (Highly recommended for structs)
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
}

public class Node
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public Coordinate Key;

    public Node(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
        Key = new(Latitude, Longitude);
    }
}
