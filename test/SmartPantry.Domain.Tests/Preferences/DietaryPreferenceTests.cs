using System;
using System.Linq;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace SmartPantry.Preferences;

public class DietaryPreferenceTests
{
    private static DietaryPreference NewPreference() => new(Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Starts_Empty_With_Alerts_On()
    {
        var preference = NewPreference();

        preference.GetWatchedAllergens().ShouldBeEmpty();
        preference.GetWatchedIngredients().ShouldBeEmpty();
        preference.ShowAlerts.ShouldBeTrue();
    }

    [Fact]
    public void Update_Normalizes_Tags_And_Words()
    {
        var preference = NewPreference();

        preference.Update(
            new[] { " EN:MILK ", "en:gluten", "en:milk" },
            new[] { "  azúcar  ", "harina   de trigo" },
            showAlerts: false);

        // Etiquetas sin repetir, en el orden de la lista de 14.
        preference.GetWatchedAllergens().ShouldBe(new[] { "en:gluten", "en:milk" });
        preference.GetWatchedIngredients().ShouldBe(new[] { "azúcar", "harina de trigo" });
        preference.ShowAlerts.ShouldBeFalse();
    }

    [Fact]
    public void Rejects_An_Allergen_Outside_The_List_Without_Partial_Changes()
    {
        var preference = NewPreference();
        preference.Update(new[] { "en:milk" }, new[] { "azúcar" }, true);

        var ex = Should.Throw<BusinessException>(() =>
            preference.Update(new[] { "en:gluten", "en:chocolate" }, new[] { "sal" }, false));

        ex.Code.ShouldBe(SmartPantryDomainErrorCodes.UnknownAllergen);
        preference.GetWatchedAllergens().ShouldBe(new[] { "en:milk" });
        preference.GetWatchedIngredients().ShouldBe(new[] { "azúcar" });
        preference.ShowAlerts.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a")]
    public void Rejects_Empty_Or_Too_Short_Words(string word)
    {
        var ex = Should.Throw<BusinessException>(() => NewPreference().Update(null, new[] { word }, true));
        ex.Code.ShouldBe(SmartPantryDomainErrorCodes.InvalidIngredient);
    }

    [Fact]
    public void Rejects_Too_Long_Words()
    {
        var word = new string('x', DietaryPreferenceConsts.MaxIngredientLength + 1);

        var ex = Should.Throw<BusinessException>(() => NewPreference().Update(null, new[] { word }, true));
        ex.Code.ShouldBe(SmartPantryDomainErrorCodes.InvalidIngredient);
    }

    [Fact]
    public void Rejects_Repeated_Words_Ignoring_Case_And_Accents()
    {
        var ex = Should.Throw<BusinessException>(() =>
            NewPreference().Update(null, new[] { "Azúcar", "azucar" }, true));
        ex.Code.ShouldBe(SmartPantryDomainErrorCodes.DuplicateIngredient);
    }

    [Fact]
    public void Rejects_Too_Many_Words()
    {
        var words = Enumerable.Range(1, DietaryPreferenceConsts.MaxWatchedIngredients + 1).Select(i => $"palabra {i}");

        var ex = Should.Throw<BusinessException>(() => NewPreference().Update(null, words, true));
        ex.Code.ShouldBe(SmartPantryDomainErrorCodes.TooManyIngredients);
    }
}
