namespace SmartPantry.Pantries;

/// <summary>
/// Tipo de advertencia. Junto con el ítem forma la clave lógica de la advertencia:
/// como máximo una por ítem y tipo.
/// </summary>
public enum PantryItemWarningType
{
    /// <summary>El ítem vence dentro del umbral o ya venció (RF-15).</summary>
    Expiration = 1
}
