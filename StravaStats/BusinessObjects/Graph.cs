using PolylinerNet;
using System.Globalization;
using System.Security.Cryptography;

namespace StravaStats.BusinessObjects
{
    public class Graph
    {
        public Dictionary<(string, string, int), Edge> Edges = [];
        public Dictionary<string, Node> Nodes = [];

        public Graph(List<Activity> activities)
        {
            var configiration = AppServices.GetService<IConfiguration>();
            double maxNodeDistance = double.Parse(configiration["MaxNodeDistance"]);

            int brokenEdgeCount = 0;
            foreach (var activity in activities)
            {
                var valhallaResponse = activity.ValhallaResponse;
                foreach (var node in valhallaResponse.NodeCoords)
                {
                    string key = GetNodeKey(node);
                    if (!Nodes.ContainsKey(key))
                        Nodes.Add(key, new Node { Latitude = node.Latitude, Longitude = node.Longitude });
                }

                foreach (var edge in valhallaResponse.Edges)
                {
                    var crossedNodes = valhallaResponse.NodeCoords.Skip(edge.BeginShapeIndex).Take(edge.EndShapeIndex - edge.BeginShapeIndex + 1).ToList();

                    for (int i = 1; i < crossedNodes.Count; i++)
                    {
                        var startNode = GetNodeKey(crossedNodes[i - 1]);
                        var endNode = GetNodeKey(crossedNodes[i]);

                        if (Edges.ContainsKey((startNode, endNode, 0)) || Edges.ContainsKey((endNode, startNode, 0)))
                            continue;

                        Edges.Add((startNode, endNode, 0), new Edge
                        {
                            StartNodeKey = startNode,
                            EndNodeKey = endNode,
                            WayId = edge.WayId
                        });
                    }
                }

                List<long> edgesCrossed = [];
                for (int i = 0; i < valhallaResponse.MatchedPoints.Count; i++)
                {
                    var match = valhallaResponse.MatchedPoints[i];
                    var edgeIndex = match.EdgeIndex;
                    if (edgeIndex == ulong.MaxValue)
                    {
                        brokenEdgeCount++;
                        continue;
                    }

                    var point = activity.TrackingPoints[i];

                    var id = valhallaResponse.Edges[(int)edgeIndex].WayId;

                    var edges = Edges.Where(e => e.Value.WayId == id).ToList();

                    bool passed = edgesCrossed.Any(t => t == id);
                    if(!passed)
                        edgesCrossed.Add(id);

                    foreach (var edge in edges)
                    {
                        edge.Value.TotalSpeed += point.Speed ?? 0;
                        edge.Value.TotalHeartRate += point.HeartRate ?? 0;
                        edge.Value.DataPoints++;
                        if(!passed)
                            edge.Value.PassedAmount++;
                    }
                }
            }
            Console.WriteLine(Edges.Where(e => e.Value.WayId == 30723352).Count());
            Console.WriteLine(brokenEdgeCount);
        }

        private string GetNodeKey(PolylinePoint node)
        {
            return $"{node.Latitude.ToString("F6")},{node.Longitude.ToString("F6")}";
        }
    }
}
