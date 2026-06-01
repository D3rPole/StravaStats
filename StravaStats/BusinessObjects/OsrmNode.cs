namespace StravaStats.BusinessObjects
{
    public class OsrmNode
    {
        public string geometry { get; set; }
        public int[] nodes { get; set; }
        public double[] distances { get; set; }
    }
}
