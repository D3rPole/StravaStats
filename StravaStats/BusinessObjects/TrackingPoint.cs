namespace StravaStats.BusinessObjects
{
    public class TrackingPoint
    {
        public DateTime? TimeStamp { get; set; }
        public double? HeartRate { get; set; }
        public double? Speed { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Distance { get; set; }
    }
}
