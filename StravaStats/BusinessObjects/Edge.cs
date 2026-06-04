namespace StravaStats.BusinessObjects
{
    public class Edge
    {
        public Node Start { get; set; }
        public Node End { get; set; }
        public double Length { get; set; }
        public int PassedAmount { get; set; }
        public double TotalHeartRate { get; set; }
        public double TotalSpeed { get; set; }
        public double AverageHeartRate => PassedAmount > 0 ? TotalHeartRate / PassedAmount : 0;
        public double AverageSpeed => PassedAmount > 0 ? TotalSpeed / PassedAmount : 0;
    }
}
