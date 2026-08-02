using System.Text.Json.Serialization;
using PolylinerNet;
using StravaStats.CustomPolyliner;

namespace StravaStats.BusinessObjects
{
    public class ValhallaTraceAttributesResponse
    {

        [JsonPropertyName("edges")]
        public List<ValhallaEdge> Edges { get; set; } = [];

        [JsonPropertyName("matched_points")]
        public List<VallhallaMatchedPoint> MatchedPoints { get; set; } = [];

        [JsonPropertyName("shape")]
        public string Shape { get; set; } = string.Empty;

        [JsonPropertyName("units")]
        public string Units { get; set; } = string.Empty;

        [JsonIgnore]
        public List<PolylinePoint> NodeCoords
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Shape))
                    return [];

                if (field is null)
                {
                    field = new ValhallaPolyliner().Decode(Shape);
                    field.RemoveAt(0);
                    field.RemoveAt(field.Count - 1);
                    /*if (ValhallaLocationsResponse is not null && ValhallaLocationsResponse.Count == 2 && field.Count > 2)
                    {
                        var startEdge = ValhallaLocationsResponse[0].Edges.FirstOrDefault(e =>
                        {
                            var first = e.NodeCoords[0];
                            var second = e.NodeCoords[1];
                            return ValhallaPolyliner.Equals(field[1], first) || ValhallaPolyliner.Equals(field[1], second);
                        });
                        var firstStart = startEdge.NodeCoords[0];
                        var firstEnd = startEdge.NodeCoords[1];

                        var endEdge = ValhallaLocationsResponse[1].Edges.FirstOrDefault(e =>
                        {
                            var first = e.NodeCoords[0];
                            var second = e.NodeCoords[1];
                            return ValhallaPolyliner.Equals(field[^2], first) || ValhallaPolyliner.Equals(field[^2], second);
                        });
                        var secondStart = endEdge.NodeCoords[0];
                        var secondEnd = endEdge.NodeCoords[1];

                        if (ValhallaPolyliner.Equals(field[1], firstStart))
                        {
                            field[0] = firstEnd;
                        }
                        if (ValhallaPolyliner.Equals(field[1], firstEnd))
                        {
                            field[0] = firstStart;
                        }

                        if (ValhallaPolyliner.Equals(field[^2], secondStart))
                        {
                            field[^1] = secondEnd;
                        }
                        if (ValhallaPolyliner.Equals(field[^2], secondEnd))
                        {
                            field[^1] = secondStart;
                        }
                    }*/
                }

                return field;
            }
        }

        public List<ValhallaLocationsResponse>? ValhallaLocationsResponse { get; set; }
    }

    public class ValhallaEdge
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("length")]
        public double Length { get; set; }

        [JsonPropertyName("end_shape_index")]
        public int EndShapeIndex { get; set; }

        [JsonPropertyName("begin_shape_index")]
        public int BeginShapeIndex { get; set; }

        [JsonPropertyName("source_percent_along")]
        public double SourcePercentAlong { get; set; }

        [JsonPropertyName("target_percent_along")]
        public double TargetPercentAlong { get; set; }

        [JsonPropertyName("way_id")]
        public long WayId { get; set; }
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
