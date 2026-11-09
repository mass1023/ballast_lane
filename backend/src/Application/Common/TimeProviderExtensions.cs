namespace RecordLabel.Application.Common;

public static class TimeProviderExtensions
{
    public static DateTime UtcNowInMicroseconds(this TimeProvider timeProvider)
    {
        var ticks = timeProvider.GetUtcNow().UtcDateTime.Ticks;
        return new DateTime(ticks / TimeSpan.TicksPerMicrosecond * TimeSpan.TicksPerMicrosecond, DateTimeKind.Utc);
    }
}
