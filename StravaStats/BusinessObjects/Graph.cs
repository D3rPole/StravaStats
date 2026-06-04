namespace StravaStats.BusinessObjects
{
    public class Graph
    {
        List<Edge> edges = [];
        Dictionary<string, Node> nodes = [];

        public Graph(List<ValhallaResponse> valhallaResponses) 
        {
                foreach (var valhallaResponse in valhallaResponses)
                {
                    foreach (var edge in valhallaResponse.Edges)
                    {
                        var startNode = new Node
                        {
                            Latitude = edge.EndNode.ElapsedCost, // Placeholder, replace with actual latitude
                            Longitude = edge.EndNode.ElapsedTime // Placeholder, replace with actual longitude
                        };
                        var endNode = new Node
                        {
                            Latitude = edge.EndNode.ElapsedCost, // Placeholder, replace with actual latitude
                            Longitude = edge.EndNode.ElapsedTime // Placeholder, replace with actual longitude
                        };
    
                        /*if (!nodes.ContainsKey(edge.))
                            nodes[startNode.Id.ToString()] = startNode;
                        if (!nodes.ContainsKey(endNode.Id.ToString()))
                            nodes[endNode.Id.ToString()] = endNode;
    
                        edges.Add(new Edge
                        {
                            StartNodeId = startNode.Id,
                            EndNodeId = endNode.Id,
                            Length = edge.Length,
                            WayId = edge.WayId
                        });*/
                    }
            }
        }
    }
}
