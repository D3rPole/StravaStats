using NetTopologySuite.Index.KdTree;
using PolylinerNet;
using StravaStats.Helper;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace StravaStats.BusinessObjects
{
    public class Graph
    {
        [JsonConverter(typeof(EdgeKeyDictionaryConverter<Edge>))]
        public Dictionary<EdgeKey, Edge> Edges { get; set; } = [];
        [JsonConverter(typeof(CoordinateDictionaryConverter<List<Coordinate>>))]
        public Dictionary<Coordinate, List<Coordinate>> AdjacencyList { get; set; } = [];
        [JsonConverter(typeof(CoordinateDictionaryConverter<Node>))]
        public Dictionary<Coordinate, Node> Nodes { get; set; } = [];
        public QuadTree QuadTree { get; set; }
        public Metrics AllMetrics { get; set; } = new();
        public Metrics UphillMetrics { get; set; } = new();
        public Metrics DownhillMetrics { get; set; } = new();

        public Graph() {}

        public Graph(List<Graph> graphs)
        {
            QuadTree = new(new BoundingBox(-180, -90, 180, 90), 0, 200, 0);
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
                    Edge e = AddEdge(edge.Value.EdgeKey.StartNodeKey, edge.Value.EdgeKey.EndNodeKey);
                    AllMetrics.AddMetrics(edge.Value.AllMetrics);
                    UphillMetrics.AddMetrics(edge.Value.UphillMetrics);
                    DownhillMetrics.AddMetrics(edge.Value.DownhillMetrics);
                    e?.AddEdge(edge.Value);
                }
            }
        }

        public Graph(List<Activity> activities)
        {
            QuadTree = new(new BoundingBox(-180, -90, 180, 90), 0);
            var configiration = AppData.GetService<IConfiguration>();
            double maxNodeDistance = double.Parse(configiration["MaxNodeDistance"]);
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

                PopulateGraph(activity);
            }
        }

        private void PopulateGraph(Activity activity)
        {
            for (int i = 1; i < activity.TrackingPoints.Count; i++)
            {
                var previousPoint = activity.TrackingPoints[i - 1];
                var point = activity.TrackingPoints[i];

                var closestEdge = QuadTree.GetClosestEdge(point.Latitude, point.Longitude, Nodes);
                if (closestEdge is null)
                    continue;

                closestEdge.AddDataPoint(previousPoint, point);
                closestEdge.ActivityIds.Add(activity.ActivityHeader.Id);
            }
        }

        public List<Dictionary<(Coordinate, Coordinate), Edge>> GetWays()
        {
            var intersections = AdjacencyList.Where(t => t.Value.Count > 2).ToDictionary();
            var visitedEdges = new HashSet<(Coordinate, Coordinate)>();
            var ways = new List<Dictionary<(Coordinate, Coordinate), Edge>>();
            foreach (var intersection in intersections)
            {
                foreach (var adjacentNodeKey in intersection.Value)
                {
                    if (visitedEdges.Contains((intersection.Key, adjacentNodeKey)) || visitedEdges.Contains((adjacentNodeKey, intersection.Key)))
                        continue;
                    var way = new Dictionary<(Coordinate, Coordinate), Edge>();
                    var currentNodeKey = adjacentNodeKey;
                    var previousNodeKey = intersection.Key;
                    while (true)
                    {
                        EdgeKey edgeKey = new(previousNodeKey, currentNodeKey);
                        var edge = GetEdge(edgeKey);
                        if (edge is null)
                            break;
                        way.Add((edge.EdgeKey.StartNodeKey, edge.EdgeKey.EndNodeKey), edge);
                        visitedEdges.Add((previousNodeKey, currentNodeKey));
                        if (AdjacencyList[currentNodeKey].Count != 2) // stop at next intersection
                            break;
                        // move to the next node
                        var nextNodeKeys = AdjacencyList[currentNodeKey].Where(k => !k.Equals(previousNodeKey)).ToList();
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
            if (Nodes.ContainsKey(node.Coordinate))
                return;
            Nodes.Add(node.Coordinate, node);
        }

        public Edge AddEdge(Node startNode, Node endNode)
        {
            return AddEdge(startNode.Coordinate, endNode.Coordinate);
        }

        public Edge AddEdge(Coordinate startNodeKey, Coordinate endNodeKey)
        {
            EdgeKey edgeKey = new(startNodeKey, endNodeKey);
            if (Edges.ContainsKey(edgeKey))
                return GetEdge(edgeKey);

            var edge = new Edge()
            {
                EdgeKey = new(startNodeKey, endNodeKey)
            };

            Edges.Add(edgeKey, edge);
            QuadTree.AddEdge(edge, Nodes);

            if (AdjacencyList.ContainsKey(startNodeKey))
                AdjacencyList[startNodeKey].Add(endNodeKey);
            else
                AdjacencyList.Add(startNodeKey, new List<Coordinate> { endNodeKey });

            if (AdjacencyList.ContainsKey(endNodeKey))
                AdjacencyList[endNodeKey].Add(startNodeKey);
            else
                AdjacencyList.Add(endNodeKey, new List<Coordinate> { startNodeKey });

            return edge;
        }

        public Edge? GetEdge(Node nodeA, Node nodeB)
        {
            EdgeKey edgeKey = new(nodeA.Coordinate, nodeB.Coordinate);
            return GetEdge(edgeKey);
        }

        public Edge? GetEdge(EdgeKey edgeKey)
        {
            if (Edges.ContainsKey(edgeKey))
                return Edges[edgeKey];
            else
                return null;
        }

        public Edge? FindEdge(double lat, double lon)
        {
            return QuadTree.GetClosestEdge(lat, lon, Nodes);
        }

        private Coordinate GetNodeKey(PolylinePoint node)
        {
            return new(node.Latitude, node.Longitude);
        }
    }
}
