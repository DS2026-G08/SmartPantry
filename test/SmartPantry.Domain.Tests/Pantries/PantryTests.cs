using System;
using Shouldly;
using Volo.Abp.Domain.Entities;
using Xunit;

namespace SmartPantry.Pantries;

public class PantryTests
{
    private static readonly DateOnly SomeDate = new(2026, 11, 10);

    [Fact]
    public void UpdateItem_Valido_Aplica_Normalizaciones()
    {
        var (pantry, item) = NewPantryWithItem();

        pantry.UpdateItem(item.Id, SomeDate.AddDays(1), 0, "   ");

        item.ExpirationDate.ShouldBe(SomeDate.AddDays(1));
        item.Quantity.ShouldBe(0);      // cero es válido
        item.Unit.ShouldBeNull();       // unidad vacía se guarda como null
    }

    [Fact]
    public void UpdateItem_Con_Cantidad_Negativa_Es_Rechazado_Y_No_Deja_Cambios_Parciales()
    {
        var (pantry, item) = NewPantryWithItem();

        Should.Throw<ArgumentException>(() => pantry.UpdateItem(item.Id, SomeDate.AddDays(5), -1, "kg"));

        item.ExpirationDate.ShouldBe(SomeDate);
        item.Quantity.ShouldBe(2);
        item.Unit.ShouldBe("l");
    }

    [Fact]
    public void UpdateItem_Con_Unidad_Demasiado_Larga_Es_Rechazado()
    {
        var (pantry, item) = NewPantryWithItem();

        Should.Throw<ArgumentException>(() =>
            pantry.UpdateItem(item.Id, SomeDate, 1, new string('u', PantryItemConsts.MaxUnitLength + 1)));

        item.Unit.ShouldBe("l");
    }

    [Fact]
    public void ConsumeItem_Conserva_El_Item_Como_Historial()
    {
        var (pantry, item) = NewPantryWithItem();

        pantry.ConsumeItem(item.Id);

        item.Status.ShouldBe(PantryItemStatus.Consumed);
        pantry.Items.ShouldContain(item);
    }

    [Fact]
    public void RemoveItem_Quita_El_Item_Y_Rechaza_Un_Item_Desconocido()
    {
        var (pantry, item) = NewPantryWithItem();

        pantry.RemoveItem(item.Id);

        pantry.Items.ShouldBeEmpty();
        Should.Throw<EntityNotFoundException>(() => pantry.RemoveItem(item.Id));
    }

    private static (Pantry Pantry, PantryItem Item) NewPantryWithItem()
    {
        var pantry = new Pantry(Guid.NewGuid(), Guid.NewGuid(), "Despensa de prueba");
        var item = pantry.AddItem(Guid.NewGuid(), Guid.NewGuid(), SomeDate, 2, "l");
        return (pantry, item);
    }
}
