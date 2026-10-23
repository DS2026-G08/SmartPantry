using System;
using Shouldly;
using Xunit;

namespace SmartPantry.Pantries;

public class ExpirationRuleTests
{
    // Fecha controlada: la regla no depende del reloj.
    private static readonly DateOnly Today = new(2026, 10, 21);

    [Theory]
    [InlineData(1)]   // dentro del umbral
    [InlineData(0)]   // vence hoy
    [InlineData(3)]   // día límite (incluido)
    [InlineData(-2)]  // vencido
    public void Item_Activo_Dentro_Del_Umbral_O_Vencido_Requiere_Advertencia(int daysUntilExpiration)
    {
        ExpirationRule
            .RequiresWarning(PantryItemStatus.Active, Today.AddDays(daysUntilExpiration), Today)
            .ShouldBeTrue();
    }

    [Theory]
    [InlineData(4)]   // primer día fuera del umbral
    [InlineData(30)]
    public void Item_Fuera_Del_Umbral_No_Requiere_Advertencia(int daysUntilExpiration)
    {
        ExpirationRule
            .RequiresWarning(PantryItemStatus.Active, Today.AddDays(daysUntilExpiration), Today)
            .ShouldBeFalse();
    }

    [Fact]
    public void Item_Sin_Fecha_No_Requiere_Advertencia()
    {
        ExpirationRule.RequiresWarning(PantryItemStatus.Active, null, Today).ShouldBeFalse();
    }

    [Fact]
    public void Item_Consumido_No_Requiere_Advertencia()
    {
        ExpirationRule.RequiresWarning(PantryItemStatus.Consumed, Today.AddDays(1), Today).ShouldBeFalse();
    }

    [Fact]
    public void Limite_Es_Hoy_Mas_El_Umbral()
    {
        ExpirationRule.GetWarningLimit(Today).ShouldBe(new DateOnly(2026, 10, 24));
    }
}
