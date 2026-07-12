using StravaStats.Enums;
using System.Text.Json.Serialization;

namespace StravaStats.BusinessObjects
{
    public class RawActivity
    {
        [JsonPropertyName("altitude")]
        public DataList? Altitude { get; set; }

        [JsonPropertyName("latlng")]
        public DataList? LatLng { get; set; }

        [JsonPropertyName("moving")]
        public DataList? Moving { get; set; }

        [JsonPropertyName("velocity_smooth")]
        public DataList? Velocity { get; set; }

        [JsonPropertyName("time")]
        public DataList? Time { get; set; }

        [JsonPropertyName("heartrate")]
        public DataList? HeartRate { get; set; }

        [JsonPropertyName("grade_smooth")]
        public DataList? Grade { get; set; }

        [JsonPropertyName("distance")]
        public DataList Distance { get; set; }

        [JsonPropertyName("ActivityHeader")]
        public ActivityHeader ActivityHeader { get; set; }
    }

    public class ActivityHeader
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("distance")]
        public double? Distance { get; set; }

        [JsonPropertyName("moving_time")]
        public double? MovingTime { get; set; }

        [JsonPropertyName("elapsed_time")]
        public double? ElapsedTime { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("sport_type")]
        public string SportType { get; set; }

        [JsonPropertyName("workout_type")]
        public int? WorkoutType { get; set; }

        [JsonPropertyName("start_date")]
        public DateTime? StartDate { get; set; }

        [JsonPropertyName("start_local_date")]
        public DateTime? StartLocalDate { get; set; }

        [JsonPropertyName("timezone")]
        public string TimeZone { get; set; }

        [JsonPropertyName("location_city")]
        public string LocationCity { get; set; }

        [JsonPropertyName("location_country")]
        public string LocationCountry { get; set; }

        [JsonPropertyName("average_speed")]
        public double? AverageSpeed { get; set; }

        [JsonPropertyName("max_speed")]
        public double? MaxSpeed { get; set; }

        [JsonPropertyName("average_watt")]
        public double? AverageWatt { get; set; }

        [JsonPropertyName("kilojoules")]
        public double? Kilojoules { get; set; }

        [JsonPropertyName("average_heartrate")]
        public double? AverageHeartRate { get; set; }

        [JsonPropertyName("max_heartrate")]
        public double? MaxHeartRate { get; set; }

        [JsonIgnore]
        public ActivityType ActivityType => Enums.ActivityTypeExtensions.FromString(Type);
    }

    public class DataList
    {
        [JsonPropertyName("data")]
        public List<object> Data { get; set; }

        [JsonPropertyName("series_type")]
        public string SeriesType { get; set; }

        [JsonPropertyName("original_size")]
        public int Size { get; set; }

        [JsonPropertyName("resolution")]
        public string Resolution { get; set; }
    }
}
