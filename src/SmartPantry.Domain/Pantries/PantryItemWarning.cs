using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace SmartPantry.Pantries;

/// <summary>
/// Advertencia sobre un ítem de despensa. Su clave lógica es (PantryItemId, Type) y la
/// base la refuerza con un índice único: en lugar de crear filas nuevas, la misma
/// advertencia se actualiza, se desactiva y se reactiva.
/// </summary>
public class PantryItemWarning : AuditedAggregateRoot<Guid>
{
    public Guid PantryItemId { get; private set; }
    public PantryItemWarningType Type { get; private set; }

    // Fecha de vencimiento del ítem al momento de la última evaluación.
    public DateOnly ExpirationDate { get; private set; }

    public bool IsActive { get; private set; }

    // Constructor privado para Entity Framework Core
    private PantryItemWarning()
    {
    }

    public PantryItemWarning(Guid id, Guid pantryItemId, PantryItemWarningType type, DateOnly expirationDate)
        : base(id)
    {
        PantryItemId = pantryItemId;
        Type = type;
        ExpirationDate = expirationDate;
        IsActive = true;
    }

    /// <summary>
    /// Deja la advertencia activa con la fecha vigente del ítem.
    /// Devuelve false si ya estaba así, para no escribir cambios innecesarios.
    /// </summary>
    public bool Refresh(DateOnly expirationDate)
    {
        if (IsActive && ExpirationDate == expirationDate)
        {
            return false;
        }

        ExpirationDate = expirationDate;
        IsActive = true;
        return true;
    }

    /// <summary>
    /// Desactiva la advertencia cuando deja de corresponder. Devuelve false si ya estaba inactiva.
    /// </summary>
    public bool Deactivate()
    {
        if (!IsActive)
        {
            return false;
        }

        IsActive = false;
        return true;
    }
}
