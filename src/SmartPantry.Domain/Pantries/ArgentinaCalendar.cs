using System;

namespace SmartPantry.Pantries;

/// <summary>
/// Convierte un instante UTC en el día calendario de Argentina.
/// Criterio del TP08: el servidor guarda y recibe instantes en UTC, pero los vencimientos
/// son días locales. Entre las 21:00 y las 23:59 UTC, en Argentina sigue siendo el día anterior.
/// </summary>
public static class ArgentinaCalendar
{
    private static readonly TimeZoneInfo TimeZone = FindTimeZone();

    public static DateOnly GetToday(DateTime utcNow)
    {
        if (utcNow.Kind == DateTimeKind.Local)
        {
            throw new ArgumentException("Se espera un instante en UTC.", nameof(utcNow));
        }

        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utcNow, TimeZone));
    }

    private static TimeZoneInfo FindTimeZone()
    {
        // Linux (CI) y Windows reconocen el identificador IANA. Si no estuviera disponible,
        // Argentina usa UTC-3 fijo, sin horario de verano.
        return TimeZoneInfo.TryFindSystemTimeZoneById(ExpirationWarningConsts.TimeZoneId, out var timeZone)
            ? timeZone
            : TimeZoneInfo.CreateCustomTimeZone(ExpirationWarningConsts.TimeZoneId, TimeSpan.FromHours(-3), "Argentina", "Argentina");
    }
}
