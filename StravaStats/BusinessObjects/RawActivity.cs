using ProtoBuf;
using StravaStats.Enums;
using System.Text.Json.Serialization;

namespace StravaStats.BusinessObjects
{
    public class RawActivity
    {
        [JsonPropertyName("altitude"), ProtoMember(1)]
        public DataList? Altitude { get; set; }

        [JsonPropertyName("latlng"), ProtoMember(2)]
        public DataList? LatLng { get; set; }

        [JsonPropertyName("moving"), ProtoMember(3)]
        public DataList? Moving { get; set; }

        [JsonPropertyName("velocity_smooth"), ProtoMember(4)]
        public DataList? Velocity { get; set; }

        [JsonPropertyName("time"), ProtoMember(5)]
        public DataList? Time { get; set; }

        [JsonPropertyName("heartrate"), ProtoMember(6)]
        public DataList? HeartRate { get; set; }

        [JsonPropertyName("grade_smooth"), ProtoMember(7)]
        public DataList? Grade { get; set; }

        [JsonPropertyName("distance"), ProtoMember(8)]
        public DataList Distance { get; set; }

        [JsonPropertyName("ActivityHeader"), ProtoMember(9)]
        public ActivityHeader ActivityHeader { get; set; }
    }

    [ProtoContract]
    public class ActivityHeader
    {
        [JsonPropertyName("id"), ProtoMember(1)]
        public long Id { get; set; }

        [JsonPropertyName("name"), ProtoMember(2)]
        public string Name { get; set; }

        [JsonPropertyName("distance"), ProtoMember(3)]
        public double? Distance { get; set; }

        [JsonPropertyName("moving_time"), ProtoMember(4)]
        public double? MovingTime { get; set; }

        [JsonPropertyName("elapsed_time"), ProtoMember(5)]
        public double? ElapsedTime { get; set; }

        [JsonPropertyName("type"), ProtoMember(6)]
        public string Type { get; set; }

        [JsonPropertyName("sport_type"), ProtoMember(7)]
        public string SportType { get; set; }

        [JsonPropertyName("workout_type"), ProtoMember(8)]
        public int? WorkoutType { get; set; }

        [JsonPropertyName("start_date"), ProtoMember(9)]
        public DateTime? StartDate { get; set; }

        [JsonPropertyName("start_local_date"), ProtoMember(10)]
        public DateTime? StartLocalDate { get; set; }

        [JsonPropertyName("timezone"), ProtoMember(11)]
        public string TimeZone { get; set; }

        [JsonPropertyName("location_city"), ProtoMember(12)]
        public string LocationCity { get; set; }

        [JsonPropertyName("location_country"), ProtoMember(13)]
        public string LocationCountry { get; set; }

        [JsonPropertyName("average_speed"), ProtoMember(14)]
        public double? AverageSpeed { get; set; }

        [JsonPropertyName("max_speed"), ProtoMember(15)]
        public double? MaxSpeed { get; set; }

        [JsonPropertyName("average_watt"), ProtoMember(16)]
        public double? AverageWatt { get; set; }

        [JsonPropertyName("kilojoules"), ProtoMember(17)]
        public double? Kilojoules { get; set; }

        [JsonPropertyName("average_heartrate"), ProtoMember(18)]
        public double? AverageHeartRate { get; set; }

        [JsonPropertyName("max_heartrate"), ProtoMember(19)]
        public double? MaxHeartRate { get; set; }

        [JsonIgnore]
        public ActivityType ActivityType => Enums.ActivityTypeExtensions.FromString(Type);
    }

    public class DataList
    {
        [JsonPropertyName("data"), ProtoMember(1)]
        public List<object> Data { get; set; }

        [JsonPropertyName("series_type"), ProtoMember(2)]
        public string SeriesType { get; set; }

        [JsonPropertyName("original_size"), ProtoMember(3)]
        public int Size { get; set; }

        [JsonPropertyName("resolution"), ProtoMember(4)]
        public string Resolution { get; set; }
    }
}
