using System;

namespace SmartPantry.Pantries;

/// <summary>
/// Regla de RF-15. Un ítem requiere advertencia de vencimiento cuando:
/// <list type="bullet">
/// <item>está activo (un ítem consumido ya no está en la despensa),</item>
/// <item>tiene fecha de vencimiento (sin fecha no hay nada que avisar), y</item>
/// <item>esa fecha es menor o igual a hoy + <see cref="ExpirationWarningConsts.ThresholdDays"/> (el día límite se incluye).</item>
/// </list>
/// Un ítem ya vencido sigue con advertencia activa hasta que se consuma o se corrija su fecha.
/// "Hoy" es la fecha calendario de Argentina: ver <see cref="ArgentinaCalendar"/>.
/// </summary>
public static class ExpirationRule
{
    public static DateOnly GetWarningLimit(DateOnly today)
    {
        return today.AddDays(ExpirationWarningConsts.ThresholdDays);
    }

    public static bool RequiresWarning(PantryItemStatus status, DateOnly? expirationDate, DateOnly today)
    {
        return status == PantryItemStatus.Active
            && expirationDate.HasValue
            && expirationDate.Value <= GetWarningLimit(today);
    }
}
