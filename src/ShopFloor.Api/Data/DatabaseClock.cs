namespace ShopFloor.Api.Data;

public static class DatabaseClock
{
    /// <summary>
    /// Current UTC time truncated to microseconds. PostgreSQL stores timestamps with microsecond
    /// precision, so values returned right after a save match what later reads return.
    /// </summary>
    public static DateTimeOffset GetUtcNowForDatabase(this TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
    }
}
