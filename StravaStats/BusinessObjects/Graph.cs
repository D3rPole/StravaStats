using PolylinerNet;
using StravaStats.Helper;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace StravaStats.BusinessObjects
{
    public class Graph
    {
        [JsonConverter(typeof(TupleKeyDictionaryConverter<Edge>))]
        public Dictionary<(string, string), Edge> Edges { get; set; } = [];
        public Dictionary<string, List<string>> AdjacencyList { get; set; } = [];
        public Dictionary<string, Node> Nodes { get; set; } = [];
        public QuadTree QuadTree { get; set; } = new(new OpenLayers.Blazor.Extent(-180, -90, 180, 90), 0);
        public Metrics Metrics { get; set; } = new();

        public Graph() { }

        public Graph(List<Graph> graphs)
        {
            foreach (var graph in graphs)
            {
                if (graph is null)
                    continue;

                foreach (var node in graph.Nodes)
                {
                    AddNode(node.Value);
                }
                foreach (var edge in graph.Edges)
                {
                    AddEdge(edge.Value);
                }
            }
        }

        public Graph(List<Activity> activities)
        {
            var configiration = AppData.GetService<IConfiguration>();
            double maxNodeDistance = double.Parse(configiration["MaxNodeDistance"]);
            int e = 0;
            foreach (var activity in activities)
            {
                var valhallaResponse = activity.ValhallaResponse;
                if (valhallaResponse is null)
                    continue;
                foreach (var node in valhallaResponse.NodeCoords)
                {
                    AddNode(new Node(node.Latitude, node.Longitude));
                }

                foreach (var edge in valhallaResponse.Edges)
                {
                    var crossedNodes = valhallaResponse.NodeCoords.Skip(edge.BeginShapeIndex).Take(edge.EndShapeIndex - edge.BeginShapeIndex + 1).ToList();

                    for (int i = 1; i < crossedNodes.Count; i++)
                    {
                        var startNodeKey = GetNodeKey(crossedNodes[i - 1]);
                        var endNodeKey = GetNodeKey(crossedNodes[i]);

                        var startNode = Nodes[startNodeKey];
                        var endNode = Nodes[endNodeKey];

                        double nodeDistance = GeoUtils.CalculateDistance(startNode, endNode);

                        if (nodeDistance < maxNodeDistance)
                        {
                            AddEdge(startNode, endNode);
                        }
                        else
                        {
                            int subDevisionCount = (int)Math.Ceiling(nodeDistance / maxNodeDistance);
                            double step = 1.0 / subDevisionCount;
                            var currentNode = startNode;

                            for (int j = 1; j < subDevisionCount; j++)
                            {
                                var newNode = GeoUtils.Interpolate(startNode, endNode, step * j);

                                AddNode(newNode);

                                AddEdge(currentNode, newNode);
                                currentNode = newNode;
                            }

                            AddEdge(currentNode, endNode);
                        }
                    }
                }

                foreach (var point in activity.TrackingPoints)
                {
                    var closestEdge = QuadTree.GetClosestEdge(point.Latitude, point.Longitude, Nodes);
                    if (closestEdge is null)
                    {
                        e++;
                        continue;
                    }

                    closestEdge.AddDataPoint(point);
                    closestEdge.ActivityIds.Add(activity.ActivityHeader.Id.ToString());
                }
            }
        }

        public List<Dictionary<(string, string), Edge>> GetWays()
        {
            var intersections = AdjacencyList.Where(t => t.Value.Count > 2).ToDictionary();
            var visitedEdges = new HashSet<(string, string)>();
            var ways = new List<Dictionary<(string, string), Edge>>();
            foreach (var intersection in intersections)
            {
                foreach (var adjacentNodeKey in intersection.Value)
                {
                    if (visitedEdges.Contains((intersection.Key, adjacentNodeKey)) || visitedEdges.Contains((adjacentNodeKey, intersection.Key)))
                        continue;
                    var way = new Dictionary<(string, string), Edge>();
                    var currentNodeKey = adjacentNodeKey;
                    var previousNodeKey = intersection.Key;
                    while (true)
                    {
                        var edge = GetEdge(previousNodeKey, currentNodeKey);
                        if (edge is null)
                            break;
                        way.Add((edge.StartNodeKey, edge.EndNodeKey), edge);
                        visitedEdges.Add((previousNodeKey, currentNodeKey));
                        if (AdjacencyList[currentNodeKey].Count != 2) // stop at next intersection
                            break;
                        // move to the next node
                        var nextNodeKeys = AdjacencyList[currentNodeKey].Where(k => k != previousNodeKey).ToList();
                        if (nextNodeKeys.Count == 0)
                            break; // dead end
                        previousNodeKey = currentNodeKey;
                        currentNodeKey = nextNodeKeys[0];
                    }
                    if (way.Count > 0)
                        ways.Add(way);
                }
            }
            return ways;
        }

        public void AddNode(double lat, double lon)
        {
            AddNode(new Node(lat, lon));
        }

        public void AddNode(Node node)
        {
            if (Nodes.ContainsKey(node.GetKey()))
                return;
            Nodes.Add(node.GetKey(), node);
        }

        public Edge AddEdge(Node startNode, Node endNode)
        {
            return AddEdge(startNode.GetKey(), endNode.GetKey());
        }

        public Edge AddEdge(string startNodeKey, string endNodeKey)
        {
            if (Edges.ContainsKey((startNodeKey, endNodeKey)) || Edges.ContainsKey((endNodeKey, startNodeKey)))
                return GetEdge(startNodeKey, endNodeKey);

            var edge = new Edge()
            {
                StartNodeKey = startNodeKey,
                EndNodeKey = endNodeKey
            };

            Edges.Add((startNodeKey, endNodeKey), edge);
            QuadTree.AddEdge(edge, Nodes);

            if (AdjacencyList.ContainsKey(startNodeKey))
                AdjacencyList[startNodeKey].Add(endNodeKey);
            else
                AdjacencyList.Add(startNodeKey, new List<string> { endNodeKey });

            if (AdjacencyList.ContainsKey(endNodeKey))
                AdjacencyList[endNodeKey].Add(startNodeKey);
            else
                AdjacencyList.Add(endNodeKey, new List<string> { startNodeKey });

            return edge;

        }

        public void AddEdge(Edge edge)
        {
            Edge e = AddEdge(edge.StartNodeKey, edge.EndNodeKey);
            Metrics.AddEdge(e);
            e?.AddEdge(edge);
        }

        public Edge? GetEdge(Node nodeA, Node nodeB)
        {
            return GetEdge(nodeA.GetKey(), nodeB.GetKey());
        }

        public Edge? GetEdge(string nodeAKey, string nodeBKey)
        {
            if (Edges.ContainsKey((nodeAKey, nodeBKey)))
                return Edges[(nodeAKey, nodeBKey)];
            else if (Edges.ContainsKey((nodeBKey, nodeAKey)))
                return Edges[(nodeBKey, nodeAKey)];
            else
                return null;
        }

        public Edge? FindEdge(double lat, double lon)
        {
            return QuadTree.GetClosestEdge(lat, lon, Nodes);
        }

        public List<Edge> GetEdgesForNode(string nodeKey)
        {
            List<Edge> edges = [];
            if (AdjacencyList.ContainsKey(nodeKey))
            {
                foreach (var adjacentNodeKey in AdjacencyList[nodeKey])
                {
                    if (Edges.ContainsKey((nodeKey, adjacentNodeKey)))
                        edges.Add(Edges[(nodeKey, adjacentNodeKey)]);
                    else if (Edges.ContainsKey((adjacentNodeKey, nodeKey)))
                        edges.Add(Edges[(adjacentNodeKey, nodeKey)]);
                }
            }
            return edges;
        }

        private string GetNodeKey(PolylinePoint node)
        {
            return $"{node.Latitude.ToString("F6", CultureInfo.InvariantCulture)},{node.Longitude.ToString("F6", CultureInfo.InvariantCulture)}";
        }
    }
}
