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
        HeartRate.AddMetric(trackingPoint.HeartRate, trackingPoint.Coordinate);
        Speed.AddMetric(trackingPoint.SpeedKmh, trackingPoint.Coordinate);
        Grade.AddMetric(trackingPoint.Grade, trackingPoint.Coordinate);
        Wattage.AddMetric(trackingPoint.Watt, trackingPoint.Coordinate);
        Acceleration.AddMetric(trackingPoint.Acceleration, trackingPoint.Coordinate);
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

    public Metrics AddMetrics(IEnumerable<Metrics> metricsList)
    {
        foreach (var metrics in metricsList)
        {
            AddMetrics(metrics);
        }
        return this;
    }
}
