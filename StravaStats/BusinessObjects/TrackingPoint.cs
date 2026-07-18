using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace StravaStats.BusinessObjects
{
    public class TrackingPoint
    {
        public int Time { get; set; }
        public double? HeartRate { get; set; }
        public double SpeedKmh => VelocitySmooth * 3.6;
        public double VelocitySmooth { get; set; }
        public double? Grade { get; set; }
        public double? Watt { get; set; }
        public double? Acceleration { get; set; }
        public bool? Moving { get; set; }
        public double? Altitude { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Distance { get; set; }

        [JsonInclude]
        private Coordinate? coordinate { get; set; }

        [JsonIgnore, NotMapped]
        public Coordinate Coordinate
        {
            get
            {
                if (coordinate is not null)
                    return coordinate.Value;

                coordinate = new(Latitude, Longitude);
                return coordinate.Value;
            }
        }
    }
}
