namespace StravaStats.BusinessObjects
{
    public class OsrmMatchResponse
    {
        public TracePoint[] tracepoints { get; set; }
        public Matching[] matchings { get; set; }
    }

    public class TracePoint
    {
        public int waypoint_index { get; set; }
        public int matchings_index { get; set; }
        public double[] location { get; set; }
    }

    public class Matching
    {
        public string geometry { get; set; }
        public Leg[] legs { get; set; }
    }

    public class Leg
    {
        public Annotation annotation { get; set; }
    }

    public class Annotation
    {
        public long[] nodes { get; set; }
        public double[] distances { get; set; }
    }
}
