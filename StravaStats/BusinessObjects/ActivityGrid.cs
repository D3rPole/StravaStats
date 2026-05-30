using Dynastream.Fit;
using H3;
using H3.Extensions;
using System.Text.Json;

namespace StravaStats.BusinessObjects
{
    public class ActivityGrid
    {
        public List<Activity> Activities { get; }
        public Dictionary<string, ActivityGridPoint> GridPoints { get; set; } = [];
        public const int GridResolution = 13;
        public ActivityGrid(List<Activity> activities)
        {
            Activities = activities;
            foreach (Activity activity in Activities) { 
                foreach(TrackingPoint trackingPoint in activity.TrackingPoints)
                {
                    if(trackingPoint.Latitude is null || trackingPoint.Longitude is null)
                        continue;
                    // 1. Convert your German degrees to Radians
                    double latRadians = trackingPoint.Latitude.Value * Math.PI / 180.0;
                    double lonRadians = trackingPoint.Longitude.Value * Math.PI / 180.0;

                    // 2. Pass the RADIANS into the LatLng constructor
                    var latLng = new H3.Model.LatLng(latRadians, lonRadians);

                    // 3. Get your index
                    H3Index index = H3Index.FromLatLng(latLng, GridResolution);
                    string cellId = index.ToString();

                    // Increment your pass counter
                    if (GridPoints.ContainsKey(cellId))
                    {
                        GridPoints[cellId].Increment(trackingPoint);
                    }
                    else
                    {
                        GridPoints[cellId] = new ActivityGridPoint();
                        GridPoints[cellId].Increment(trackingPoint);
                        GridPoints[cellId].CellBoundary = index.GetCellBoundary();
                        GridPoints[cellId].CenterPoint = index.ToLatLng();
                    }
                }
            }
        }

        public string GetHeatmapGeoJson()
        {
            var features = new List<object>();

            foreach (var gridPoint in GridPoints.Values)
            {
                var feature = new
                {
                    type = "Feature",
                    geometry = new
                    {
                        type = "Point",
                        coordinates = new double[] {gridPoint.CenterPoint.LongitudeDegrees, gridPoint.CenterPoint.LatitudeDegrees}
                    },
                    properties = new
                    {
                        weight = Math.Min(gridPoint.Count / 10.0,1.0),
                        count = gridPoint.Count
                    }
                };
                features.Add(feature);
            }

            var geoJsonStructure = new
            {
                type = "FeatureCollection",
                features = features
            };

            return JsonSerializer.Serialize(geoJsonStructure);
        }
    }
}
