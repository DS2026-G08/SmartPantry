using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace SmartPantry.Pantries;

/// <summary>
/// Modelo mínimo del TP08: lo necesario para que el worker de vencimientos (RF-15)
/// procese ítems propios de un usuario. La administración de la despensa (RF-10, RF-11)
/// llegará con Seguridad.
/// </summary>
public class Pantry : AggregateRoot<Guid>
{
    public string Name { get; private set; } = null!;

    // Propietario persistido. El worker lo toma de la base, nunca del usuario HTTP actual.
    public Guid OwnerId { get; private set; }

    public ICollection<PantryItem> Items { get; private set; } = new List<PantryItem>();

    // Constructor privado para Entity Framework Core
    private Pantry()
    {
    }

    public Pantry(Guid id, Guid ownerId, string name)
        : base(id)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("La despensa debe tener un propietario.", nameof(ownerId));
        }

        Check.NotNullOrWhiteSpace(name, nameof(name));
        var trimmedName = name.Trim();
        Check.Length(trimmedName, nameof(name), PantryConsts.MaxNameLength);

        OwnerId = ownerId;
        Name = trimmedName;
    }

    public PantryItem AddItem(Guid itemId, Guid productId, DateOnly? expirationDate)
    {
        var item = new PantryItem(itemId, Id, productId, expirationDate);
        Items.Add(item);
        return item;
    }

    public void ChangeItemExpirationDate(Guid itemId, DateOnly? expirationDate)
    {
        GetItem(itemId).ChangeExpirationDate(expirationDate);
    }

    public void ConsumeItem(Guid itemId)
    {
        GetItem(itemId).MarkAsConsumed();
    }

    private PantryItem GetItem(Guid itemId)
    {
        return Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new EntityNotFoundException(typeof(PantryItem), itemId);
    }
}
