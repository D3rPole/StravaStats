using OpenLayers.Blazor;
using StravaStats.Helper;
using System.Text.Json.Serialization;

namespace StravaStats.BusinessObjects
{
    public class QuadTree
    {
        [JsonIgnore]
        const int maxEdges = 80;
        [JsonIgnore]
        const int maxDepth = 30;
        
        public Extent Extent { get; set; }

        public int CurrentDepth { get; set; } = 0;
        public QuadTree? UpperLeft { get; set; }
        public QuadTree? UpperRight { get; set; }
        public QuadTree? DownLeft { get; set; }
        public QuadTree? DownRight { get; set; }
        public List<Edge> Edges  { get; set; } = [];

        [JsonIgnore]
        private bool isSplit => UpperLeft is not null;

        public QuadTree(Extent extent, int currentDepth)
        {
            Extent = extent;
            this.CurrentDepth = currentDepth;
        }

        public void AddEdge(Edge edge, Dictionary<Coordinate, Node> nodes)
        {
            // Reject edges that don't touch this extent at all
            if (!Extent.ContainsNode(nodes[edge.EdgeKey.StartNodeKey]) &&
                !Extent.ContainsNode(nodes[edge.EdgeKey.EndNodeKey]))
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

                if (Edges.Count > maxEdges && CurrentDepth < maxDepth)
                    Split(nodes);
            }
        }

        private void Split(Dictionary<Coordinate, Node> nodes)
        {
            double midX = (Extent.X1 + Extent.X2) / 2;
            double midY = (Extent.Y1 + Extent.Y2) / 2;

            UpperLeft = new(new Extent(Extent.X1, Extent.Y1, midX, midY), CurrentDepth + 1);
            UpperRight = new(new Extent(midX, Extent.Y1, Extent.X2, midY), CurrentDepth + 1);
            DownLeft = new(new Extent(Extent.X1, midY, midX, Extent.Y2), CurrentDepth + 1);
            DownRight = new(new Extent(midX, midY, Extent.X2, Extent.Y2), CurrentDepth + 1);

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
                if (UpperLeft?.Extent.ContainsCoords(lat, lon) == true)
                    return UpperLeft.GetClosestEdge(lat, lon, nodes);
                if (UpperRight?.Extent.ContainsCoords(lat, lon) == true)
                    return UpperRight.GetClosestEdge(lat, lon, nodes);
                if (DownLeft?.Extent.ContainsCoords(lat, lon) == true)
                    return DownLeft.GetClosestEdge(lat, lon, nodes);
                if (DownRight?.Extent.ContainsCoords(lat, lon) == true)
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
                    .Where(q => q?.Extent.ContainsCoords(lat, lon) == true)
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
    }
}
