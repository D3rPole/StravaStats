namespace StravaStats.Helper;

public static class ActionExtensions
{
    public static Action Debounce(this Action action, TimeSpan delay)
    {
        CancellationTokenSource? cancellationTokenSource = null;

        return () =>
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new();

            Task
                .Delay(delay, cancellationTokenSource.Token)
                .ContinueWith(task =>
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        action();
                    }
                }, TaskScheduler.Default);
        };
    }
}
