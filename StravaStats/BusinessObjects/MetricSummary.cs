using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace StravaStats.BusinessObjects;

public class MetricSummary
{
    [JsonIgnore, NotMapped] 
    public double MaxValue => maxValue == double.MinValue ? 0 : maxValue;

    [JsonIgnore, NotMapped]
    public double MinValue => minValue == double.MaxValue ? 0 : minValue;

    [JsonIgnore, NotMapped]
    public double Average => count == 0 ? 0 : Total / count;

    [JsonInclude]
    private double maxValue = double.MinValue;
    public Coordinate MaxPosition { get; set; }

    [JsonInclude]
    private double minValue = double.MaxValue;
    public Coordinate MinPosition { get; set; }

    [JsonInclude]
    public double Total { get; set; }
    [JsonInclude]
    private int count { get; set; }
    public void AddValue(double? value, Coordinate position)
    {
        if (value is null) return;
        count++;
        Total += value.Value;
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
    public void AddValue(double? value)
    {
        if (value is null) return;
        count++;
        Total += value.Value;
        if (maxValue < value.Value)
            maxValue = value.Value;

        if (minValue > value.Value)
            minValue = value.Value;
    }

    public void AddMetric(MetricSummary metric)
    {
        if (metric is null) return;
        count += metric.count;
        Total += metric.Total;
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
