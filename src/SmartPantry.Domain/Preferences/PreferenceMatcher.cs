using System.Collections.Generic;
using System.Linq;
using SmartPantry.Products;

namespace SmartPantry.Preferences;

/// <summary>
/// Regla de RF-12: compara las preferencias con los datos disponibles del producto. Es pura (no usa
/// la base ni el usuario actual), así que se prueba con casos fijos.
///
/// - Alérgenos: etiquetas exactas ("en:milk" = "en:milk"). Sólo son verificables si el producto informa
///   al menos un alérgeno: Open Food Facts devuelve la lista vacía tanto si no tiene como si nadie la
///   cargó, y la regla 2 obliga a no suponer.
/// - Palabras: se buscan dentro del texto de ingredientes sin distinguir mayúsculas ni acentos. Sólo son
///   verificables si el producto tiene ingredientes.
/// - Una coincidencia gana: se informa Match aunque falten otros datos, y se aclara qué falta.
/// </summary>
public static class PreferenceMatcher
{
    public static PreferenceEvaluation Evaluate(
        IReadOnlyCollection<string> watchedAllergens,
        IReadOnlyCollection<string> watchedIngredients,
        IEnumerable<string>? productAllergenTags,
        string? productIngredients)
    {
        if (watchedAllergens.Count == 0 && watchedIngredients.Count == 0)
        {
            return PreferenceEvaluation.NoPreferences;
        }

        var productAllergens = CatalogValues.NormalizeAllergenTags(productAllergenTags);
        var matchedAllergens = watchedAllergens.Where(productAllergens.Contains).ToList();
        var missingAllergens = watchedAllergens.Count > 0 && productAllergens.Count == 0;

        var hasIngredients = !string.IsNullOrWhiteSpace(productIngredients);
        var ingredients = hasIngredients ? IngredientText.Normalize(productIngredients!) : string.Empty;
        var matchedIngredients = hasIngredients
            ? watchedIngredients.Where(w => ingredients.Contains(IngredientText.Normalize(w))).ToList()
            : new List<string>();
        var missingIngredients = watchedIngredients.Count > 0 && !hasIngredients;

        var status = matchedAllergens.Count > 0 || matchedIngredients.Count > 0
            ? PreferenceEvaluationStatus.Match
            : missingAllergens || missingIngredients
                ? PreferenceEvaluationStatus.NotVerifiable
                : PreferenceEvaluationStatus.NoMatch;

        return new PreferenceEvaluation(status, matchedAllergens, matchedIngredients, missingAllergens, missingIngredients);
    }

    public static PreferenceEvaluation Evaluate(DietaryPreference? preference, Product product)
    {
        return Evaluate(preference, product.GetAllergenTags(), product.Ingredients);
    }

    public static PreferenceEvaluation Evaluate(
        DietaryPreference? preference,
        IEnumerable<string>? productAllergenTags,
        string? productIngredients)
    {
        return preference == null
            ? PreferenceEvaluation.NoPreferences
            : Evaluate(
                preference.GetWatchedAllergens(),
                preference.GetWatchedIngredients(),
                productAllergenTags,
                productIngredients);
    }
}
