using NetTopologySuite.GeometriesGraph;
using OpenLayers.Blazor;

namespace StravaStats.Helper
{
    public static class Extentions
    {
        public static bool ContainsNode(this Extent extent, BusinessObjects.Node node)
        {
            return ContainsCoords(extent, node.Coordinate.Latitude, node.Coordinate.Longitude);
        }

        public static bool ContainsCoords(this Extent extent, double lat, double lon)
        {
            return 
                lon >= extent.X1 &&
                lon <= extent.X2 &&
                lat >= extent.Y1 &&
                lat <= extent.Y2;
        }
    }
}
