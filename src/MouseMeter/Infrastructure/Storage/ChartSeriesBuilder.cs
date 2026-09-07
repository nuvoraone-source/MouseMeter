using MouseMeter.Infrastructure;

namespace MouseMeter.Infrastructure.Storage;

/// <summary>
/// Baut die Datenreihe für das Diagramm: einen Balken pro Stunde (Tag), Tag
/// (Woche/Monat) oder pro Monat (Jahr), für die im Dashboard wählbare
/// Kennzahl (Gesamtkonzept Abschnitt 19-22). "Tag" wertet dabei seit v1.1.0
/// die stundengenau erfassten Werte aus <see cref="DailyStats.Hours"/> aus.
/// </summary>
public static class ChartSeriesBuilder
{
    private static readonly string[] WeekdayNames = { "Mo", "Di", "Mi", "Do", "Fr", "Sa", "So" };
    private static readonly string[] MonthNames =
    {
        "Jan", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez",
    };

    public static List<ChartDataPoint> Build(
        AppState state, DateOnly today, DashboardPeriod period, DailyStats liveToday, ChartMetric metric)
    {
        return period switch
        {
            DashboardPeriod.Day => BuildDay(liveToday, metric),
            DashboardPeriod.Week => BuildWeek(state, today, liveToday, metric),
            DashboardPeriod.Month => BuildMonth(state, today, liveToday, metric),
            DashboardPeriod.Year => BuildYear(state, today, liveToday, metric),
            _ => new List<ChartDataPoint>(),
        };
    }

    private static DailyStats? GetDay(AppState state, DateOnly day, DateOnly today, DailyStats liveToday) =>
        day == today ? liveToday : state.Days.GetValueOrDefault(day.ToString("yyyy-MM-dd"));

    private static double MetricValue(DailyStats stats, ChartMetric metric) => metric switch
    {
        ChartMetric.Distance => stats.DistanceMeters,
        ChartMetric.Clicks => stats.LeftClicks + stats.RightClicks + stats.MiddleClicks,
        ChartMetric.Keystrokes => stats.Keystrokes,
        ChartMetric.Speed => stats.ActiveSeconds > 0 ? stats.DistanceMeters / stats.ActiveSeconds : 0,
        _ => 0,
    };

    private static double MetricValue(HourlyStats stats, ChartMetric metric) => metric switch
    {
        ChartMetric.Distance => stats.DistanceMeters,
        ChartMetric.Clicks => stats.LeftClicks + stats.RightClicks + stats.MiddleClicks,
        ChartMetric.Keystrokes => stats.Keystrokes,
        ChartMetric.Speed => stats.ActiveSeconds > 0 ? stats.DistanceMeters / stats.ActiveSeconds : 0,
        _ => 0,
    };

    private static List<ChartDataPoint> BuildDay(DailyStats liveToday, ChartMetric metric)
    {
        var points = new List<ChartDataPoint>(24);
        int currentHour = DateTime.Now.Hour;

        for (int hour = 0; hour < 24; hour++)
        {
            double value = hour <= currentHour ? MetricValue(liveToday.Hours[hour], metric) : 0;
            string label = hour % 3 == 0 ? $"{hour}" : string.Empty;
            points.Add(new ChartDataPoint(label, value, $"{hour:00}-{hour + 1:00} Uhr - {Formatting.MetricValue(metric, value)}"));
        }

        return points;
    }

    private static List<ChartDataPoint> BuildWeek(AppState state, DateOnly today, DailyStats liveToday, ChartMetric metric)
    {
        DateOnly start = PeriodAggregator.GetStart(DashboardPeriod.Week, today);
        var points = new List<ChartDataPoint>(7);

        for (int i = 0; i < 7; i++)
        {
            DateOnly day = start.AddDays(i);
            DailyStats? stats = day <= today ? GetDay(state, day, today, liveToday) : null;
            double value = stats is null ? 0 : MetricValue(stats, metric);
            points.Add(new ChartDataPoint(WeekdayNames[i], value, $"{WeekdayNames[i]} {day:dd.MM.} - {Formatting.MetricValue(metric, value)}"));
        }

        return points;
    }

    private static List<ChartDataPoint> BuildMonth(AppState state, DateOnly today, DailyStats liveToday, ChartMetric metric)
    {
        var start = new DateOnly(today.Year, today.Month, 1);
        int daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        var points = new List<ChartDataPoint>(daysInMonth);

        for (int i = 0; i < daysInMonth; i++)
        {
            DateOnly day = start.AddDays(i);
            DailyStats? stats = day <= today ? GetDay(state, day, today, liveToday) : null;
            double value = stats is null ? 0 : MetricValue(stats, metric);

            // Nicht jeden Tag beschriften - bei bis zu 31 Balken wuerden sich
            // die Zahlen sonst gegenseitig ueberlappen.
            bool showLabel = i == 0 || i == daysInMonth - 1 || (i + 1) % 5 == 0;
            string label = showLabel ? (i + 1).ToString() : string.Empty;

            points.Add(new ChartDataPoint(label, value, $"{day:dd.MM.} - {Formatting.MetricValue(metric, value)}"));
        }

        return points;
    }

    private static List<ChartDataPoint> BuildYear(AppState state, DateOnly today, DailyStats liveToday, ChartMetric metric)
    {
        var points = new List<ChartDataPoint>(12);

        for (int month = 1; month <= 12; month++)
        {
            string name = MonthNames[month - 1];

            if (month > today.Month)
            {
                points.Add(new ChartDataPoint(name, 0, $"{name} - {Formatting.MetricValue(metric, 0)}"));
                continue;
            }

            var monthStart = new DateOnly(today.Year, month, 1);
            var monthEnd = month == today.Month ? today : new DateOnly(today.Year, month, DateTime.DaysInMonth(today.Year, month));

            double sum = 0;
            double sumDistance = 0;
            double sumActiveSeconds = 0;

            for (DateOnly day = monthStart; day <= monthEnd; day = day.AddDays(1))
            {
                DailyStats? stats = GetDay(state, day, today, liveToday);
                if (stats is null)
                {
                    continue;
                }

                if (metric == ChartMetric.Speed)
                {
                    sumDistance += stats.DistanceMeters;
                    sumActiveSeconds += stats.ActiveSeconds;
                }
                else
                {
                    sum += MetricValue(stats, metric);
                }
            }

            double value = metric == ChartMetric.Speed
                ? (sumActiveSeconds > 0 ? sumDistance / sumActiveSeconds : 0)
                : sum;

            points.Add(new ChartDataPoint(name, value, $"{name} - {Formatting.MetricValue(metric, value)}"));
        }

        return points;
    }
}
