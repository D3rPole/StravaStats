using ProtoBuf;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace StravaStats.BusinessObjects;

[ProtoContract]
public class MetricSummary
{
    [JsonIgnore]
    public double MaxValue => maxValue == double.MinValue ? 0 : maxValue;

    [JsonIgnore]
    public double MinValue => minValue == double.MaxValue ? 0 : minValue;

    [JsonIgnore]
    public double Average => count == 0 ? 0 : Total / count;

    [JsonInclude, ProtoMember(1)]
    private double maxValue = double.MinValue;

    [JsonInclude, ProtoMember(2)]
    public Coordinate MaxPosition { get; set; }

    [JsonInclude, ProtoMember(3)]
    private double minValue = double.MaxValue;

    [JsonInclude, ProtoMember(4)]
    public Coordinate MinPosition { get; set; }

    [JsonInclude, ProtoMember(5)]
    public double Total { get; set; }

    [JsonInclude, ProtoMember(6)]
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
