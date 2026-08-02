using OneOf.Types;
using ProtoBuf;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace StravaStats.BusinessObjects
{
    [ProtoContract(ImplicitFields = ImplicitFields.None)]
    public struct BoundingBox
    {
        [ProtoMember(1)]
        public double X1 { get; set; }

        [ProtoMember(2)]
        public double Y1 { get; set; }

        [ProtoMember(3)]
        public double X2 { get; set; }

        [ProtoMember(4)]
        public double Y2 { get; set; }

        public BoundingBox() { }
        public BoundingBox(double x1, double y1, double x2, double y2)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }

        public bool ContainsNode(BusinessObjects.Node node)
        {
            return ContainsCoords(node.Coordinate.Latitude, node.Coordinate.Longitude);
        }

        public bool ContainsCoords(double lat, double lon)
        {
            return
                lon >= X1 &&
                lon <= X2 &&
                lat >= Y1 &&
                lat <= Y2;
        }
    }

    [ProtoContract(ImplicitFields = ImplicitFields.None)]
    public class QuadTree
    {
        [JsonIgnore]
        public int MaxEdges { get; private set; } = 200;
        [JsonIgnore]
        public int MaxDepth { get; private set; } = 30;

        [ProtoMember(1)]
        public BoundingBox BoundingBox { get; set; }

        [ProtoMember(2)]
        public int CurrentDepth { get; set; } = 0;

        [ProtoMember(3)]
        public QuadTree? UpperLeft { get; set; }

        [ProtoMember(4)]
        public QuadTree? UpperRight { get; set; }

        [ProtoMember(5)]
        public QuadTree? DownLeft { get; set; }

        [ProtoMember(6)]
        public QuadTree? DownRight { get; set; }

        [ProtoMember(7)]
        public List<Edge> Edges { get; set; } = [];

        [JsonIgnore, NotMapped]
        private bool isSplit => UpperLeft is not null;

        public QuadTree() { }

        public QuadTree(BoundingBox boundingBox, int currentDepth)
        {
            BoundingBox = boundingBox;
            this.CurrentDepth = currentDepth;
        }

        public QuadTree(BoundingBox boundingBox, int currentDepth, int maxEdges, int maxDepth)
        {
            BoundingBox = boundingBox;
            this.CurrentDepth = currentDepth;
            MaxEdges = maxEdges;
            MaxDepth = maxDepth;
        }

        public void AddEdge(Edge edge, Dictionary<Coordinate, Node> nodes)
        {
            // Reject edges that don't touch this extent at all
            if (!BoundingBox.ContainsNode(nodes[edge.EdgeKey.StartNodeKey]) &&
                !BoundingBox.ContainsNode(nodes[edge.EdgeKey.EndNodeKey]))
                return;

            if (isSplit)
            {
                UpperLeft?.AddEdge(edge, nodes);
                UpperRight?.AddEdge(edge, nodes);
                DownLeft?.AddEdge(edge, nodes);
                DownRight?.AddEdge(edge, nodes);
            }
            else
            {
                Edges.Add(edge);

                if (Edges.Count > MaxEdges && CurrentDepth < MaxDepth)
                    Split(nodes);
            }
        }

        private void Split(Dictionary<Coordinate, Node> nodes)
        {
            double midX = (BoundingBox.X1 + BoundingBox.X2) / 2;
            double midY = (BoundingBox.Y1 + BoundingBox.Y2) / 2;

            UpperLeft = new(new BoundingBox(BoundingBox.X1, BoundingBox.Y1, midX, midY), CurrentDepth + 1, MaxEdges, MaxDepth);
            UpperRight = new(new BoundingBox(midX, BoundingBox.Y1, BoundingBox.X2, midY), CurrentDepth + 1, MaxEdges, MaxDepth);
            DownLeft = new(new BoundingBox(BoundingBox.X1, midY, midX, BoundingBox.Y2), CurrentDepth + 1, MaxEdges, MaxDepth);
            DownRight = new(new BoundingBox(midX, midY, BoundingBox.X2, BoundingBox.Y2), CurrentDepth + 1, MaxEdges, MaxDepth);

            var edgesToReassign = Edges.ToList();
            Edges.Clear();

            foreach (var edge in edgesToReassign)
            {
                UpperLeft.AddEdge(edge, nodes);
                UpperRight.AddEdge(edge, nodes);
                DownLeft.AddEdge(edge, nodes);
                DownRight.AddEdge(edge, nodes);
            }
        }

        public Edge? GetClosestEdge(double lat, double lon, Dictionary<Coordinate, Node> nodes)
        {
            if (isSplit)
            {
                if (UpperLeft?.BoundingBox.ContainsCoords(lat, lon) == true)
                    return UpperLeft.GetClosestEdge(lat, lon, nodes);
                if (UpperRight?.BoundingBox.ContainsCoords(lat, lon) == true)
                    return UpperRight.GetClosestEdge(lat, lon, nodes);
                if (DownLeft?.BoundingBox.ContainsCoords(lat, lon) == true)
                    return DownLeft.GetClosestEdge(lat, lon, nodes);
                if (DownRight?.BoundingBox.ContainsCoords(lat, lon) == true)
                    return DownRight.GetClosestEdge(lat, lon, nodes);
                return new[] { UpperLeft, UpperRight, DownLeft, DownRight }
                    .Where(q => q is not null)
                    .SelectMany(q => q!.GetLeafEdges(lat, lon, nodes))
                    .OrderBy(e => e.DistanceToPoint(new Node(lat, lon), nodes))
                    .FirstOrDefault();
            }
            else
            {
                return Edges
                    .OrderBy(e => e.DistanceToPoint(new Node(lat, lon), nodes))
                    .FirstOrDefault();
            }
        }

        private IEnumerable<Edge> GetLeafEdges(double lat, double lon, Dictionary<Coordinate, Node> nodes)
        {
            if (isSplit)
            {
                var matching = new[] { UpperLeft, UpperRight, DownLeft, DownRight }
                    .Where(q => q?.BoundingBox.ContainsCoords(lat, lon) == true)
                    .ToList();

                var source = matching.Count > 0
                    ? matching
                    : new[] { UpperLeft, UpperRight, DownLeft, DownRight }.Where(q => q is not null).ToList();

                return source.SelectMany(q => q!.GetLeafEdges(lat, lon, nodes));
            }
            else
            {
                return Edges;
            }
        }

        public List<QuadTree> GetAllLeaves()
        {
            if (isSplit)
            {
                var result = new List<QuadTree>();
                result.AddRange(UpperLeft.GetAllLeaves());
                result.AddRange(UpperRight.GetAllLeaves());
                result.AddRange(DownLeft.GetAllLeaves());
                result.AddRange(DownRight.GetAllLeaves());
                return result;
            }
            else
            {
                return [this];
            }
        }

        public void RemoveEdge(Edge edge)
        {
            if (isSplit)
            {
                UpperLeft?.RemoveEdge(edge);
                UpperRight?.RemoveEdge(edge);
                DownLeft?.RemoveEdge(edge);
                DownRight?.RemoveEdge(edge);
            }
            else
            {
                Edges.Remove(edge);
            }
        }
    }
}
