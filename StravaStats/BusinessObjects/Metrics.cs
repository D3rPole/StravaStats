using ProtoBuf;

namespace StravaStats.BusinessObjects;

[ProtoContract(ImplicitFields = ImplicitFields.None)]
public class Metrics
{
    [ProtoMember(1)]
    public MetricSummary HeartRate { get; set; } = new();

    [ProtoMember(2)]
    public MetricSummary Speed { get; set; } = new();

    [ProtoMember(3)]
    public MetricSummary Grade { get; set; } = new();

    [ProtoMember(4)]
    public MetricSummary Wattage { get; set; } = new();

    [ProtoMember(5)]
    public MetricSummary Acceleration { get; set; } = new();

    public Metrics AddDataPoint(TrackingPoint trackingPoint)
    {
        HeartRate.AddValue(trackingPoint.HeartRate, trackingPoint.Coordinate);
        Speed.AddValue(trackingPoint.SpeedKmh, trackingPoint.Coordinate);
        Grade.AddValue(trackingPoint.Grade, trackingPoint.Coordinate);
        Wattage.AddValue(trackingPoint.Watt, trackingPoint.Coordinate);
        Acceleration.AddValue(trackingPoint.Acceleration, trackingPoint.Coordinate);
        return this;
    }

    public Metrics AddMetrics(Metrics metrics)
    {
        HeartRate.AddMetric(metrics.HeartRate);
        Speed.AddMetric(metrics.Speed);
        Grade.AddMetric(metrics.Grade);
        Wattage.AddMetric(metrics.Wattage);
        Acceleration.AddMetric(metrics.Acceleration);
        return this;
    }
}
