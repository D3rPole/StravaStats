using StravaStats.Helper;

namespace StravaStats.BusinessObjects
{
    public class Edge
    {
        public string StartNodeKey { get; set; }
        public string EndNodeKey { get; set; }
        public long WayId { get; set; }
        public double Length { get; set; }
        public HashSet<string> ActivityFileNames { get; set; } = new();
        public int DataPoints { get; set; }
        public double TotalHeartRate { get; set; }
        public double TotalSpeed { get; set; }
        public double AverageHeartRate => DataPoints > 0 ? TotalHeartRate / DataPoints : 0;
        public double AverageSpeed => DataPoints > 0 ? TotalSpeed / DataPoints : 0;

        public void AddDataPoint(TrackingPoint trackingPoint)
        {
            TotalHeartRate += trackingPoint.HeartRate ?? 0;
            TotalSpeed += trackingPoint.Speed ?? 0;
            DataPoints++;
        }

        public void AddEdge(Edge edge)
        {
            ActivityFileNames.UnionWith(edge.ActivityFileNames);
            TotalHeartRate += edge.TotalHeartRate;
            TotalSpeed += edge.TotalSpeed;
            DataPoints += edge.DataPoints;
        }

        public double DistanceToPoint(Node node, Dictionary<string, Node> nodes)
        {
            var startNode = nodes[StartNodeKey];
            var endNode = nodes[EndNodeKey];
            return Math.Min(GeoUtils.CalculateDistance(node, startNode),
                            GeoUtils.CalculateDistance(node, endNode));
        }
    }
}
