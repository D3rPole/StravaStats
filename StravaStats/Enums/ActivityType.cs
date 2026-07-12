namespace StravaStats.Enums;

public enum ActivityType
{
    All = 0,
    Bike = 1,
    Run = 2,
    Walk = 4,
    Swim = 8,
}

public static class ActivityTypeExtensions
{
    public static string GetIcon(this ActivityType activityType) => activityType switch
    {
        ActivityType.All => MudBlazor.FontIcons.MaterialIcons.Filled.FitnessCenter,
        ActivityType.Bike => MudBlazor.FontIcons.MaterialIcons.Filled.DirectionsBike,
        ActivityType.Run => MudBlazor.FontIcons.MaterialIcons.Filled.DirectionsRun,
        ActivityType.Walk => MudBlazor.FontIcons.MaterialIcons.Filled.DirectionsWalk,
        ActivityType.Swim => MudBlazor.FontIcons.MaterialIcons.Filled.Pool,
        _ => ActivityType.All.GetIcon()
    };

    public static ActivityType FromString(string activityType) => activityType switch
    {
        "All" => ActivityType.All,
        "Ride" => ActivityType.Bike,
        "Run" => ActivityType.Run,
        "Walk" => ActivityType.Walk,
        "Swim" => ActivityType.Swim,
        _ => ActivityType.All
    };
}
