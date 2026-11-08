using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace SmartPantry.Pantries;

/// <summary>
/// Despensa de un usuario. TP08 incorporó el modelo mínimo para el worker de vencimientos
/// (RF-15); TP10 agrega la administración de los ítems propios (RF-10, RF-11).
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

    public PantryItem AddItem(
        Guid itemId,
        Guid productId,
        DateOnly? expirationDate,
        decimal? quantity = null,
        string? unit = null)
    {
        var item = new PantryItem(itemId, Id, productId, expirationDate, quantity, unit);
        Items.Add(item);
        return item;
    }

    public void ChangeItemExpirationDate(Guid itemId, DateOnly? expirationDate)
    {
        GetItem(itemId).ChangeExpirationDate(expirationDate);
    }

    public void UpdateItem(Guid itemId, DateOnly? expirationDate, decimal? quantity, string? unit)
    {
        GetItem(itemId).Update(expirationDate, quantity, unit);
    }

    // Regla de negocio 6: consumir no elimina el ítem; queda como historial.
    public void ConsumeItem(Guid itemId)
    {
        GetItem(itemId).MarkAsConsumed();
    }

    public void RemoveItem(Guid itemId)
    {
        Items.Remove(GetItem(itemId));
    }

    public PantryItem GetItem(Guid itemId)
    {
        return Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new EntityNotFoundException(typeof(PantryItem), itemId);
    }
}
