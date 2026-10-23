using System;
using Shouldly;
using Xunit;

namespace SmartPantry.Pantries;

public class PantryItemWarningTests
{
    private static readonly DateOnly ExpirationDate = new(2026, 10, 23);

    [Fact]
    public void Refresh_Con_La_Misma_Fecha_No_Produce_Cambios()
    {
        var warning = NewWarning();

        warning.Refresh(ExpirationDate).ShouldBeFalse();
        warning.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Refresh_Con_Otra_Fecha_Actualiza_La_Advertencia()
    {
        var warning = NewWarning();

        warning.Refresh(ExpirationDate.AddDays(-1)).ShouldBeTrue();
        warning.ExpirationDate.ShouldBe(ExpirationDate.AddDays(-1));
    }

    [Fact]
    public void Deactivate_Y_Refresh_Reactivan_La_Misma_Advertencia()
    {
        var warning = NewWarning();

        warning.Deactivate().ShouldBeTrue();
        warning.Deactivate().ShouldBeFalse();   // ya estaba inactiva
        warning.IsActive.ShouldBeFalse();

        warning.Refresh(ExpirationDate).ShouldBeTrue();
        warning.IsActive.ShouldBeTrue();
    }

    private static PantryItemWarning NewWarning()
    {
        return new PantryItemWarning(Guid.NewGuid(), Guid.NewGuid(), PantryItemWarningType.Expiration, ExpirationDate);
    }
}
