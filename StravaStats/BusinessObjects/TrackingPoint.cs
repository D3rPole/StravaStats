namespace StravaStats.BusinessObjects
{
    public class TrackingPoint
    {
        public int Time { get; set; }
        public double? HeartRate { get; set; }
        public double? Velocity { get; set; }
        public double? Grade { get; set; }
        public double? Watt { get; set; }
        public double? Acceleration { get; set; }
        public bool? Moving { get; set; }
        public double? Altitude { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Distance { get; set; }
    }
}
