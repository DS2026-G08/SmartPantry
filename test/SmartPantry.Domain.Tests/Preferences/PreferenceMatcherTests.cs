using System;
using Shouldly;
using Xunit;

namespace SmartPantry.Preferences;

// Regla de RF-12 con casos fijos: no usa la base ni el usuario actual.
public class PreferenceMatcherTests
{
    private static readonly string[] None = Array.Empty<string>();

    [Fact]
    public void Without_Preferences_Returns_NoPreferences()
    {
        var result = PreferenceMatcher.Evaluate(None, None, new[] { "en:milk" }, "leche");

        result.Status.ShouldBe(PreferenceEvaluationStatus.NoPreferences);
        result.RequiresWarning.ShouldBeFalse();
    }

    [Fact]
    public void Matches_An_Allergen_By_Its_Exact_Tag()
    {
        var result = PreferenceMatcher.Evaluate(
            new[] { "en:milk", "en:gluten" }, None, new[] { "en:milk", "en:nuts" }, null);

        result.Status.ShouldBe(PreferenceEvaluationStatus.Match);
        result.MatchedAllergens.ShouldBe(new[] { "en:milk" });
        result.MatchedIngredients.ShouldBeEmpty();
    }

    [Fact]
    public void Ignores_Free_Text_Allergens_That_Are_Not_Canonical_Tags()
    {
        // "es:Contiene leche" no es una etiqueta canónica: no cuenta como dato de alérgenos.
        var result = PreferenceMatcher.Evaluate(new[] { "en:milk" }, None, new[] { "es:Contiene leche" }, null);

        result.Status.ShouldBe(PreferenceEvaluationStatus.NotVerifiable);
        result.MissingAllergens.ShouldBeTrue();
    }

    [Theory]
    [InlineData("azúcar", "AZUCAR, harina de trigo")]
    [InlineData("AZUCAR", "Azúcar de caña")]
    [InlineData("harina de trigo", "Harina   de\ntrigo, sal")]
    public void Matches_An_Ingredient_Word_Ignoring_Case_Accents_And_Spaces(string word, string ingredients)
    {
        var result = PreferenceMatcher.Evaluate(None, new[] { word }, null, ingredients);

        result.Status.ShouldBe(PreferenceEvaluationStatus.Match);
        result.MatchedIngredients.ShouldBe(new[] { word }); // se informa la palabra tal como la escribió el usuario
    }

    [Fact]
    public void Without_Ingredients_A_Watched_Word_Is_Not_Verifiable()
    {
        var result = PreferenceMatcher.Evaluate(None, new[] { "azúcar" }, new[] { "en:milk" }, "  ");

        result.Status.ShouldBe(PreferenceEvaluationStatus.NotVerifiable);
        result.MissingIngredients.ShouldBeTrue();
        result.MissingAllergens.ShouldBeFalse();
    }

    [Fact]
    public void Without_Allergens_A_Watched_Allergen_Is_Not_Verifiable()
    {
        // Open Food Facts devuelve la lista vacía tanto si no tiene como si nadie la cargó (regla 2).
        var result = PreferenceMatcher.Evaluate(new[] { "en:milk" }, None, None, "agua, sal");

        result.Status.ShouldBe(PreferenceEvaluationStatus.NotVerifiable);
        result.MissingAllergens.ShouldBeTrue();
        result.MissingIngredients.ShouldBeFalse();
    }

    [Fact]
    public void A_Match_Wins_Even_When_Other_Data_Is_Missing()
    {
        var result = PreferenceMatcher.Evaluate(new[] { "en:milk" }, new[] { "azúcar" }, None, "Leche, azúcar");

        result.Status.ShouldBe(PreferenceEvaluationStatus.Match);
        result.MatchedIngredients.ShouldBe(new[] { "azúcar" });
        result.MissingAllergens.ShouldBeTrue(); // se aclara qué no se pudo verificar
    }

    [Fact]
    public void With_All_Data_And_No_Coincidences_Returns_NoMatch()
    {
        var result = PreferenceMatcher.Evaluate(new[] { "en:gluten" }, new[] { "maní" }, new[] { "en:milk" }, "leche, azúcar");

        result.Status.ShouldBe(PreferenceEvaluationStatus.NoMatch);
        result.RequiresWarning.ShouldBeFalse();
        result.MissingAllergens.ShouldBeFalse();
        result.MissingIngredients.ShouldBeFalse();
    }
}
