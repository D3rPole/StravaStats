namespace StravaStats.BusinessObjects.Cities
{
    public class City : BaseRegion
    {
        public string Name { get; set; }
        public string ParentId{ get; set; }
    }
}
