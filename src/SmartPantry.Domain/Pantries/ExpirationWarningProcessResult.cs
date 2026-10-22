namespace SmartPantry.Pantries;

/// <summary>
/// Cambios que produjo una ejecución. Repetir el proceso sobre los mismos datos debe dar todo en cero.
/// </summary>
public record ExpirationWarningProcessResult(int Created, int Updated, int Reactivated, int Deactivated)
{
    public bool HasChanges => Created + Updated + Reactivated + Deactivated > 0;
}
