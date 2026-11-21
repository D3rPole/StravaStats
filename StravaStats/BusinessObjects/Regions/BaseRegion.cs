using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace StravaStats.BusinessObjects.Regions
{
    public abstract class BaseRegion
    {
        public Geometry Geometry { get; set; }
        public string Name { get; set; }
        public string Id { get; set; }
    }
}
