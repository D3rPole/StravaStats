using Newtonsoft.Json;

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
}
