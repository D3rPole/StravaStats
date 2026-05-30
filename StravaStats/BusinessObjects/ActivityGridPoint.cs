using H3.Model;
using NetTopologySuite.Geometries;

namespace StravaStats.BusinessObjects
{
    public class ActivityGridPoint
    {
        public LatLng CenterPoint { get; set; }
        public Polygon CellBoundary { get; set; }
        public List<OpenLayers.Blazor.Coordinate> CoordinateList
        {
            get
            {
                if (CellBoundary == null)
                    return [];
                var coordinates = CellBoundary.Coordinates;
                List<OpenLayers.Blazor.Coordinate> coordinateList = [];
                foreach (var coordinate in coordinates)
                {
                    coordinateList.Add(new OpenLayers.Blazor.Coordinate(coordinate.X, coordinate.Y));
                }
                return coordinateList;
            }
        }
        public int Count { get; set; }
        public float SpeedSum { get; set; }
        public float HeartRateSum { get; set; }
        public float AverageSpeed => Count > 0 ? SpeedSum / Count : 0;
        public float AverageHeartRate => Count > 0 ? HeartRateSum / Count : 0;

        public void Increment(TrackingPoint trackingPoint)
        {
            Count++;
            SpeedSum += trackingPoint.Speed.HasValue ? (float)trackingPoint.Speed.Value : 0;
            HeartRateSum += trackingPoint.HeartRate.HasValue ? (float)trackingPoint.HeartRate.Value : 0;
        }
    }
}
