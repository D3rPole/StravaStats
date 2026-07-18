namespace StravaStats.BusinessObjects;

public class Metrics
{
    public MetricSummary HeartRate { get; set; } = new();
    public MetricSummary Speed { get; set; } = new();
    public MetricSummary Grade { get; set; } = new();
    public MetricSummary Wattage { get; set; } = new();
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
