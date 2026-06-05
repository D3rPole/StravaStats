using PolylinerNet;
using StravaStats.Helper;
using System.Globalization;
using System.Security.Cryptography;

namespace StravaStats.BusinessObjects
{
    public class Graph
    {
        public Dictionary<(string, string), Edge> Edges = [];
        public Dictionary<string, List<string>> AdjacencyList = [];
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
                    AddNode(new Node { Latitude = node.Latitude, Longitude = node.Longitude });
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

                /*List<long> edgesCrossed = [];
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
                }*/
            }
        }

        public Graph(Graph other, double nodeDistance)
        {
            var ways = other.GetWays();
            var intersections = other.AdjacencyList.Where(t => t.Value.Count > 2).ToDictionary();

            foreach (var way in ways)
            {
                var edgeList = way.Values.ToList();
                if (edgeList.Count == 0)
                    continue;

                var startNode = other.Nodes[intersections.Where(i => edgeList.Any(e => e.StartNodeKey == i.Key || e.EndNodeKey == i.Key)).First().Key];
                var visitedNodes = new HashSet<string> { startNode.GetKey() };
                var edge = edgeList.Where(e => e.StartNodeKey == startNode.GetKey() || e.EndNodeKey == startNode.GetKey()).First();
                double dist = 0;
                while (true)
                {
                    visitedNodes.Add(edge.StartNodeKey);
                    visitedNodes.Add(edge.EndNodeKey);

                    var nodeA = edge.StartNodeKey;
                    var nodeB = edge.EndNodeKey;
                    double length = GeoUtils.CalculateEdgeLength(edge, other.Nodes);
                    dist += length;

                    edge = edgeList.Where(e => (
                        (
                            (
                            edge.StartNodeKey == e.StartNodeKey ||
                            edge.StartNodeKey == e.EndNodeKey)
                            && !visitedNodes.Contains(e.StartNodeKey)
                        )
                        ||
                        (
                            (
                            edge.EndNodeKey == e.StartNodeKey ||
                            edge.EndNodeKey == e.EndNodeKey)
                            && !visitedNodes.Contains(e.EndNodeKey)
                        )
                    )).FirstOrDefault();

                    if (edge is null)
                        break;

                    if (dist < nodeDistance || visitedNodes.Count == edgeList.Count + 1)
                        continue;
                    dist = 0;

                    if(edge.EndNodeKey == nodeA || edge.StartNodeKey == nodeA)
                    {
                        AddNode(startNode);
                        AddNode(other.Nodes[nodeA]);
                        AddEdge(startNode.GetKey(), nodeA);
                        startNode = other.Nodes[nodeA];
                    }
                    else if (edge.EndNodeKey == nodeB || edge.StartNodeKey == nodeB)
                    {
                        AddNode(startNode);
                        AddNode(other.Nodes[nodeB]);
                        AddEdge(startNode.GetKey(), nodeB);
                        startNode = other.Nodes[nodeB];
                    }
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
            AddNode(new Node()
            {
                Latitude = lat,
                Longitude = lon
            });
        }

        public void AddNode(Node node)
        {
            if (Nodes.ContainsKey(node.GetKey()))
                return;
            Nodes.Add(node.GetKey(), node);
        }

        public void AddEdge(Node startNode, Node endNode)
        {
            AddEdge(startNode.GetKey(), endNode.GetKey());
        }

        public void AddEdge(string startNodeKey, string endNodeKey)
        {
            if (Edges.ContainsKey((startNodeKey, endNodeKey)) || Edges.ContainsKey((endNodeKey, startNodeKey)))
                return;
            Edges.Add((startNodeKey, endNodeKey), new Edge()
            {
                StartNodeKey = startNodeKey,
                EndNodeKey = endNodeKey
            });

            if (AdjacencyList.ContainsKey(startNodeKey))
                AdjacencyList[startNodeKey].Add(endNodeKey);
            else
                AdjacencyList.Add(startNodeKey, new List<string> { endNodeKey });

            if (AdjacencyList.ContainsKey(endNodeKey))
                AdjacencyList[endNodeKey].Add(startNodeKey);
            else
                AdjacencyList.Add(endNodeKey, new List<string> { startNodeKey });
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

        private string GetNodeKey(Node node)
        {
            return $"{node.Latitude.ToString("F6")},{node.Longitude.ToString("F6")}";
        }

        private string GetNodeKey(PolylinePoint node)
        {
            return $"{node.Latitude.ToString("F6")},{node.Longitude.ToString("F6")}";
        }
    }
}
