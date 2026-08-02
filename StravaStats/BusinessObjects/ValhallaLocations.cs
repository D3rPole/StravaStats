using Newtonsoft.Json;
using PolylinerNet;
using StravaStats.CustomPolyliner;

namespace StravaStats.BusinessObjects;

public class ValhallaLocations
{
    [JsonProperty("verbose")]
    public bool Verbose { get; set; } = true;

    [JsonProperty("locations")]
    public List<ValhallaLocation> Locations { get; set; }
}

public class ValhallaLocation
{
    [JsonProperty("lat")]
    public double Lat { get; set; }

    [JsonProperty("lon")]
    public double Lon { get; set; }
}

public class ValhallaLocationsResponse
{
    [JsonProperty("input_lat")]
    public double InputLat { get; set; }

    [JsonProperty("input_lon")]
    public double InputLon { get; set; }

    [JsonProperty("edges")]
    public List<ValhallaLocationsEdge> Edges { get; set; }
}

public class ValhallaLocationsEdge
{
    [JsonProperty("correlated_lon")]
    public double CorrelatedLat { get; set; }
    [JsonProperty("correlated_lon")]
    public double CorrelatedLon { get; set; }

    [JsonProperty("linear_reference")]
    public string LinearReference { get; set; }


    [JsonIgnore]
    public List<PolylinePoint> NodeCoords
    {
        get
        {
            if (string.IsNullOrWhiteSpace(LinearReference))
                return [];

            if (field is null)
            {
                field = new ValhallaPolyliner().Decode(LinearReference);
            }

            return field;
        }
    }
}
