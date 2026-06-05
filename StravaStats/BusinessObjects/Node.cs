namespace StravaStats.BusinessObjects
{
    public class Node
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string GetKey()
        {
            return $"{Latitude.ToString("F6")},{Longitude.ToString("F6")}";
        }
    }
}
