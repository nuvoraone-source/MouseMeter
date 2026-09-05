namespace MouseMeter.Infrastructure.Storage;

/// <summary>
/// Fasst mehrere Tagesstatistiken zu einer Woche, einem Monat oder einem
/// Jahr zusammen (Gesamtkonzept Abschnitt 26: höhere Zeiträume werden aus
/// den Tagesdaten berechnet, nicht separat erfasst).
///
/// Wochen beginnen mit Montag, Monate und Jahre folgen dem Kalender.
/// Zukünftige Tage innerhalb eines Zeitraums existieren naturgemäß nicht und
/// tragen nichts bei; der heutige Tag wird nicht aus dem (bis zu 5 Sekunden
/// alten) Speicherstand gelesen, sondern über <paramref name="today"/> mit
/// den aktuell laufenden Live-Werten übergeben.
/// </summary>
public static class PeriodAggregator
{
    /// <summary>Der erste Tag des Zeitraums, der den angegebenen Tag enthält.</summary>
    public static DateOnly GetStart(DashboardPeriod period, DateOnly containingDay) => period switch
    {
        DashboardPeriod.Week => containingDay.AddDays(-(((int)containingDay.DayOfWeek + 6) % 7)),
        DashboardPeriod.Month => new DateOnly(containingDay.Year, containingDay.Month, 1),
        DashboardPeriod.Year => new DateOnly(containingDay.Year, 1, 1),
        _ => containingDay,
    };

    public static DailyStats Aggregate(AppState state, DateOnly today, DashboardPeriod period, DailyStats liveToday)
    {
        if (period == DashboardPeriod.Day)
        {
            return liveToday;
        }

        DateOnly start = GetStart(period, today);
        var total = new DailyStats();

        for (DateOnly day = start; day <= today; day = day.AddDays(1))
        {
            DailyStats? entry = day == today
                ? liveToday
                : state.Days.GetValueOrDefault(day.ToString("yyyy-MM-dd"));

            if (entry is null)
            {
                continue;
            }

            total.DistanceMeters += entry.DistanceMeters;
            total.ActiveSeconds += entry.ActiveSeconds;
            total.LeftClicks += entry.LeftClicks;
            total.RightClicks += entry.RightClicks;
            total.MiddleClicks += entry.MiddleClicks;
            total.Keystrokes += entry.Keystrokes;
            total.ScrollForward += entry.ScrollForward;
            total.ScrollBackward += entry.ScrollBackward;
        }

        return total;
    }
}
