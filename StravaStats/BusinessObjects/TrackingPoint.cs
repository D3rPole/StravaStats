using ProtoBuf;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace StravaStats.BusinessObjects;

[ProtoContract]
public class TrackingPoint
{
    [ProtoMember(1)]
    public int Time { get; set; }
    
    [ProtoMember(2)]
    public double? HeartRate { get; set; }

    [ProtoMember(3)]
    public double VelocitySmooth { get; set; }

    [ProtoMember(4)]
    public double? Grade { get; set; }

    [ProtoMember(5)]
    public double? Watt { get; set; }

    [ProtoMember(6)]
    public double? Acceleration { get; set; }

    [ProtoMember(7)]
    public bool? Moving { get; set; }

    [ProtoMember(8)]
    public double? Altitude { get; set; }

    [ProtoMember(9)]
    public double Latitude { get; set; }

    [ProtoMember(10)]
    public double Longitude { get; set; }

    [ProtoMember(11)]
    public double Distance { get; set; }

    [JsonInclude, ProtoMember(12)]
    private Coordinate? coordinate { get; set; }

    [JsonIgnore]
    public double SpeedKmh => VelocitySmooth * 3.6;
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
