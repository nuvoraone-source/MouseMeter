namespace MouseMeter.Infrastructure.Storage;

/// <summary>Ein einzelner Balken im Diagramm: Beschriftung, Wert und ausführlicher Tooltip-Text.</summary>
public sealed record ChartDataPoint(string Label, double Value, string Tooltip);
