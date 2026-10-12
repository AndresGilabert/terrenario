namespace Terrenario.Api.Application.Dashboard;

/// <summary>Extremo de una recta de tendencia: fecha y rendimiento en L/100kg (RN-013).</summary>
public sealed record TrendPoint(DateOnly Date, decimal Value);

/// <summary>Recta de regresión, entregada como sus dos extremos para que el cliente solo la dibuje.</summary>
public sealed record TrendLine(TrendPoint From, TrendPoint To);

/// <summary>
/// PRD-101 — Regresión lineal por mínimos cuadrados sobre la fecha. Todos los puntos pesan igual: el
/// rendimiento graso no depende de los kilos del día (decisión del PO), que solo ponderan el valor de
/// un día con varias cosechas.
/// </summary>
public static class YieldRegression
{
    /// <summary>
    /// Recta entre la primera y la última fecha de los puntos, o <c>null</c> si hay menos de dos fechas
    /// distintas: con una sola no hay pendiente que calcular y dibujarla sería inventarla.
    /// </summary>
    public static TrendLine? Fit(IReadOnlyCollection<(DateOnly Date, decimal Value)> points)
    {
        var minDay = points.Count == 0 ? 0 : points.Min(p => p.Date.DayNumber);
        var maxDay = points.Count == 0 ? 0 : points.Max(p => p.Date.DayNumber);
        if (minDay == maxDay) return null;

        var meanX = points.Average(p => (double)p.Date.DayNumber);
        var meanY = points.Average(p => (double)p.Value);
        var sxx = points.Sum(p => Math.Pow(p.Date.DayNumber - meanX, 2));
        var sxy = points.Sum(p => (p.Date.DayNumber - meanX) * ((double)p.Value - meanY));
        var slope = sxy / sxx;

        TrendPoint At(int day) => new(
            DateOnly.FromDayNumber(day),
            decimal.Round((decimal)(meanY + slope * (day - meanX)), 2, MidpointRounding.AwayFromZero));

        return new TrendLine(At(minDay), At(maxDay));
    }
}
