using System.Text.Json.Serialization;

namespace StravaStats.BusinessObjects;

public class MetricSummary
{
    [JsonIgnore]
    public double MaxValue => maxValue == double.MinValue ? 0 : maxValue;

    [JsonIgnore]
    public double MinValue => minValue == double.MaxValue ? 0 : minValue;

    [JsonIgnore]
    public double Average => count == 0 ? 0 : totalValue / count;

    [JsonInclude]
    private double maxValue = double.MinValue;
    public Coordinate MaxPosition { get; set; }

    [JsonInclude]
    private double minValue = double.MaxValue;
    public Coordinate MinPosition { get; set; }

    [JsonInclude]
    private double totalValue { get; set; }
    [JsonInclude]
    private int count { get; set; }
    public void AddMetric(double? value, Coordinate position)
    {
        if (value is null) return;
        count++;
        totalValue += value.Value;
        if (maxValue < value.Value)
        {
            maxValue = value.Value;
            MaxPosition = position;
        }
        if (minValue > value.Value)
        {
            minValue = value.Value;
            MinPosition = position;
        }
    }

    public void AddMetric(MetricSummary metric)
    {
        if (metric is null) return;
        count += metric.count;
        totalValue += metric.totalValue;
        if (maxValue < metric.maxValue)
        {
            maxValue = metric.maxValue;
            MaxPosition = metric.MaxPosition;
        }
        if (minValue > metric.minValue)
        {
            minValue = metric.minValue;
            MinPosition = metric.MinPosition;
        }
    }
}
