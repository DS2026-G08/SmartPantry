using System;
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

    // Constructor privado para Entity Framework Core
    private PantryItem()
    {
    }

    internal PantryItem(Guid id, Guid pantryId, Guid productId, DateOnly? expirationDate)
        : base(id)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("El ítem debe referenciar un producto.", nameof(productId));
        }

        PantryId = pantryId;
        ProductId = productId;
        Status = PantryItemStatus.Active;
        ExpirationDate = expirationDate;
    }

    internal void ChangeExpirationDate(DateOnly? expirationDate)
    {
        ExpirationDate = expirationDate;
    }

    internal void MarkAsConsumed()
    {
        Status = PantryItemStatus.Consumed;
    }
}
