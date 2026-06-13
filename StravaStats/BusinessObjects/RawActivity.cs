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
