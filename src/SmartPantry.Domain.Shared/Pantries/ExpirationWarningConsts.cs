namespace SmartPantry.Pantries;

public static class ExpirationWarningConsts
{
    /// <summary>
    /// Días de anticipación con los que se avisa un vencimiento. El día límite se incluye:
    /// con hoy = 21/10, un ítem que vence el 24/10 tiene advertencia y uno del 25/10 no.
    /// </summary>
    public const int ThresholdDays = 3;

    /// <summary>
    /// Zona horaria con la que se calcula "hoy". Las fechas de vencimiento son días
    /// calendario de Argentina, no instantes UTC.
    /// </summary>
    public const string TimeZoneId = "America/Argentina/Buenos_Aires";
}
