namespace InvoiceFlow.Application.Common;

public static class TimeProviderExtensions
{
    private static readonly TimeZoneInfo Lisbon = TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon");

    /// <summary>
    /// The current calendar date in Portugal. Document dates are legal dates, so "today" must not
    /// depend on the server's time zone (containers usually run in UTC).
    /// </summary>
    public static DateOnly TodayInPortugal(this TimeProvider timeProvider) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), Lisbon).DateTime);
}
