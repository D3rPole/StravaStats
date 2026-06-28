namespace StravaStats.Helper;

public static class FuncExtentions
{
    public static Action Debounce(this Func<Task> action, TimeSpan delay)
    {
        CancellationTokenSource? cancellationTokenSource = null;

        return () =>
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new();

            Task
                .Delay(delay, cancellationTokenSource.Token)
                .ContinueWith(async task =>
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        await action();
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
        };
    }
}
