using System.Text.Json.Serialization;

namespace StravaStats.BusinessObjects
{
    public class ValhallaResponse
    {

        [JsonPropertyName("edges")]
        public List<ValhallaEdge> Edges { get; set; } = [];

        [JsonPropertyName("matched_points")]
        public List<VallhallaMatchedPoint> MatchedPoints { get; set; } = [];

        [JsonPropertyName("shape")]
        public string Shape { get; set; } = string.Empty;

        [JsonPropertyName("units")]
        public string Units { get; set; } = string.Empty;
    }

    public class ValhallaEdge
    {
        [JsonPropertyName("end_node")]
        public ValhallaEdgeNode EndNode { get; set; } = new();

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("length")]
        public double Length { get; set; }

        [JsonPropertyName("source_percent_along")]
        public double SourcePercentAlong { get; set; }

        [JsonPropertyName("target_percent_along")]
        public double TargetPercentAlong { get; set; }

        [JsonPropertyName("way_id")]
        public long WayId { get; set; }
    }

    public class ValhallaEdgeNode
    {
        [JsonPropertyName("elapsed_cost")]
        public double ElapsedCost { get; set; }

        [JsonPropertyName("elapsed_time")]
        public double ElapsedTime { get; set; }
    }

    public class VallhallaMatchedPoint
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lon")]
        public double Lon { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("edge_index")]
        public ulong EdgeIndex { get; set; }

        [JsonPropertyName("distance_along_edge")]
        public double DistanceAlongEdge { get; set; }
    }
}
