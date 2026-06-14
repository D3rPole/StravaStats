using System.Globalization;

namespace StravaStats.BusinessObjects
{
    public class Node
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public Node(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public string GetKey()
        {
            return $"{Latitude.ToString("F6", CultureInfo.InvariantCulture)},{Longitude.ToString("F6", CultureInfo.InvariantCulture)}";
        }
    }
}
