using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace SmartPantry.Pantries;

/// <summary>
/// Ítem de una despensa. Sólo se crea y modifica a través de <see cref="Pantry"/>.
/// </summary>
public class PantryItem : Entity<Guid>
{
    public Guid PantryId { get; private set; }
    public Guid ProductId { get; private set; }
    public PantryItemStatus Status { get; private set; }

    // Día calendario sin hora; null cuando el producto no informa vencimiento.
    public DateOnly? ExpirationDate { get; private set; }

    // TP10: cantidad y unidad opcionales; null cuando no se informan (RF-09).
    public decimal? Quantity { get; private set; }
    public string? Unit { get; private set; }

    // Constructor privado para Entity Framework Core
    private PantryItem()
    {
    }

    internal PantryItem(
        Guid id,
        Guid pantryId,
        Guid productId,
        DateOnly? expirationDate,
        decimal? quantity = null,
        string? unit = null)
        : base(id)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("El ítem debe referenciar un producto.", nameof(productId));
        }

        PantryId = pantryId;
        ProductId = productId;
        Status = PantryItemStatus.Active;
        Update(expirationDate, quantity, unit);
    }

    internal void ChangeExpirationDate(DateOnly? expirationDate)
    {
        ExpirationDate = expirationDate;
    }

    /// <summary>
    /// Primero valida y normaliza todos los valores; sólo si son válidos cambia el estado.
    /// </summary>
    internal void Update(DateOnly? expirationDate, decimal? quantity, string? unit)
    {
        if (quantity < 0)
        {
            throw new ArgumentException("La cantidad no puede ser negativa.", nameof(quantity));
        }

        var normalizedUnit = NormalizeUnit(unit);

        ExpirationDate = expirationDate;
        Quantity = quantity;
        Unit = normalizedUnit;
    }

    internal void MarkAsConsumed()
    {
        Status = PantryItemStatus.Consumed;
    }

    private static string? NormalizeUnit(string? unit)
    {
        if (string.IsNullOrWhiteSpace(unit))
        {
            return null;
        }

        var trimmed = unit.Trim();
        Check.Length(trimmed, nameof(unit), PantryItemConsts.MaxUnitLength);
        return trimmed;
    }
}
