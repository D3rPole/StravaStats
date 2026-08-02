using NetTopologySuite.Index.KdTree;
using PolylinerNet;
using ProtoBuf;
using StravaStats.Helper;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace StravaStats.BusinessObjects
{
    [ProtoContract]
    public class Graph
    {
        [JsonConverter(typeof(EdgeKeyDictionaryConverter<Edge>)), ProtoMember(1)]
        public Dictionary<EdgeKey, Edge> Edges { get; set; } = [];

        [JsonConverter(typeof(CoordinateDictionaryConverter<List<Coordinate>>)), ProtoMember(2)]
        public Dictionary<Coordinate, List<Coordinate>> AdjacencyList { get; set; } = [];

        [JsonConverter(typeof(CoordinateDictionaryConverter<Node>)), ProtoMember(3)]
        public Dictionary<Coordinate, Node> Nodes { get; set; } = [];

        [ProtoMember(4)]
        public QuadTree QuadTree { get; set; }


        [ProtoMember(5)]
        public Metrics AllMetrics { get; set; } = new();

        [ProtoMember(6)]
        public Metrics UphillMetrics { get; set; } = new();

        [ProtoMember(7)]
        public Metrics DownhillMetrics { get; set; } = new();

        [ProtoMember(8)]
        public MetricSummary Distance { get; set; } = new();

        [ProtoMember(9)]
        public MetricSummary ActiveTime { get; set; } = new();

        [ProtoMember(10)]
        public MetricSummary TotalTime { get; set; } = new();

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
                Distance.AddMetric(graph.Distance);
                ActiveTime.AddMetric(graph.ActiveTime);
                TotalTime.AddMetric(graph.TotalTime);
            }
        }

        public Graph(List<Activity> activities)
        {
            QuadTree = new(new BoundingBox(-180, -90, 180, 90), 0);
            var configiration = AppData.GetService<IConfiguration>();
            double maxNodeDistance = double.Parse(configiration["MaxNodeDistance"]);
            foreach (var activity in activities)
            {
                var valhallaResponse = activity.ValhallaTraceResponse;
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
                var point = activity.TrackingPoints[i];

                var closestEdge = QuadTree.GetClosestEdge(point.Latitude, point.Longitude, Nodes);
                if (closestEdge is null)
                    continue;

                closestEdge.AddDataPoint(point);
                closestEdge.ActivityIds.Add(activity.ActivityHeader.Id);
            }
            Distance.AddValue(activity.ActivityHeader.Distance.GetValueOrDefault());
            ActiveTime.AddValue(activity.ActivityHeader.MovingTime.GetValueOrDefault());
            TotalTime.AddValue(activity.ActivityHeader.ElapsedTime.GetValueOrDefault());
            CleanGraph();
        }

        private void CleanGraph()
        {
            // Removes edges with no data
            foreach(var edge in Edges)
            {
                if(edge.Value.ActivityIds.Count == 0)
                {
                    RemoveEdge(edge.Value);
                }
            }
        }

        private void RemoveEdge(Edge edge)
        {
            // Remove references from QuadTree, Edge list and Edgeless nodes left behind
            QuadTree.RemoveEdge(edge);
            Edges.Remove(edge.EdgeKey);

            bool startNodeHasEdges = Edges.Any(keyValuePair => keyValuePair.Key.EndNodeKey == edge.EdgeKey.StartNodeKey || keyValuePair.Key.StartNodeKey == edge.EdgeKey.StartNodeKey);
            if (!startNodeHasEdges)
                Nodes.Remove(edge.EdgeKey.StartNodeKey);

            bool endNodeHasEdges = Edges.Any(keyValuePair => keyValuePair.Key.EndNodeKey == edge.EdgeKey.EndNodeKey || keyValuePair.Key.StartNodeKey == edge.EdgeKey.EndNodeKey);
            if (!endNodeHasEdges)
                Nodes.Remove(edge.EdgeKey.EndNodeKey);
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
