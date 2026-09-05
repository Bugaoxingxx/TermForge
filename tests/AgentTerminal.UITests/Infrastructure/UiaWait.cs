namespace AgentTerminal.UITests.Infrastructure;

public static class UiaWait
{
    public static T Until<T>(Func<T?> getter, TimeSpan? timeout = null, TimeSpan? interval = null, string? message = null) where T : class
    {
        var limit = timeout ?? TimeSpan.FromSeconds(5);
        var step = interval ?? TimeSpan.FromMilliseconds(200);
        var end = DateTime.UtcNow + limit;

        while (DateTime.UtcNow < end)
        {
            var result = getter();
            if (result != null)
            {
                return result;
            }
            Thread.Sleep(step);
        }

        throw new TimeoutException(message ?? $"Timed out waiting for element or condition after {limit.TotalSeconds}s");
    }

    public static void UntilTrue(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? interval = null, string? message = null)
    {
        var limit = timeout ?? TimeSpan.FromSeconds(5);
        var step = interval ?? TimeSpan.FromMilliseconds(200);
        var end = DateTime.UtcNow + limit;

        while (DateTime.UtcNow < end)
        {
            if (condition())
            {
                return;
            }
            Thread.Sleep(step);
        }

        throw new TimeoutException(message ?? $"Timed out waiting for condition to become true after {limit.TotalSeconds}s");
    }
}
