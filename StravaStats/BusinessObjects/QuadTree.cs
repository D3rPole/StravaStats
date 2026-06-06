using OpenLayers.Blazor;
using StravaStats.Helper;

namespace StravaStats.BusinessObjects
{
    public class QuadTree
    {
        const int maxEdges = 200;
        const int maxDepth = 20;
        Extent Extent;

        int currentDepth = 0;
        public QuadTree? UpperLeft;
        public QuadTree? UpperRight;
        public QuadTree? DownLeft;
        public QuadTree? DownRight;
        public List<Edge> Edges = [];
        private bool isSplit => UpperLeft is not null;

        public QuadTree(Extent extent, int currentDepth)
        {
            Extent = extent;
            this.currentDepth = currentDepth;
        }

        public void AddEdge(Edge edge, Dictionary<string, Node> nodes)
        {
            // Reject edges that don't touch this extent at all
            if (!Extent.ContainsNode(nodes[edge.StartNodeKey]) &&
                !Extent.ContainsNode(nodes[edge.EndNodeKey]))
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

                if (Edges.Count > maxEdges && currentDepth < maxDepth)
                    Split(nodes);
            }
        }

        private void Split(Dictionary<string, Node> nodes)
        {
            double midX = (Extent.X1 + Extent.X2) / 2;
            double midY = (Extent.Y1 + Extent.Y2) / 2;

            UpperLeft = new(new Extent(Extent.X1, Extent.Y1, midX, midY), currentDepth + 1);
            UpperRight = new(new Extent(midX, Extent.Y1, Extent.X2, midY), currentDepth + 1);
            DownLeft = new(new Extent(Extent.X1, midY, midX, Extent.Y2), currentDepth + 1);
            DownRight = new(new Extent(midX, midY, Extent.X2, Extent.Y2), currentDepth + 1);

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

        public Edge? GetClosestEdge(double lat, double lon, Dictionary<string, Node> nodes)
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

        private IEnumerable<Edge> GetLeafEdges(double lat, double lon, Dictionary<string, Node> nodes)
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
    }
}
