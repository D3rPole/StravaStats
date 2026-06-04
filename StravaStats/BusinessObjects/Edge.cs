namespace StravaStats.BusinessObjects
{
    public class Edge
    {
        public string StartNodeKey { get; set; }
        public string EndNodeKey { get; set; }
        public long WayId { get; set; }
        public double Length { get; set; }
        public int PassedAmount { get; set; }
        public int DataPoints { get; set; }
        public double TotalHeartRate { get; set; }
        public double TotalSpeed { get; set; }
        public double AverageHeartRate => DataPoints > 0 ? TotalHeartRate / DataPoints : 0;
        public double AverageSpeed => DataPoints > 0 ? TotalSpeed / DataPoints : 0;
    }
}
