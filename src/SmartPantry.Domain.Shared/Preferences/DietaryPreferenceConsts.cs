using System.Collections.Generic;

namespace SmartPantry.Preferences;

public static class DietaryPreferenceConsts
{
    /// <summary>
    /// Los 14 alérgenos de declaración obligatoria, con la etiqueta canónica de Open Food Facts.
    /// Es la misma lista que KNOWN_ALLERGENS en Angular (angular/src/app/shared/catalog.ts): las
    /// preferencias usan las mismas etiquetas que el producto para que la comparación sea exacta.
    /// </summary>
    public static readonly IReadOnlyList<string> KnownAllergenTags = new[]
    {
        "en:gluten",
        "en:crustaceans",
        "en:eggs",
        "en:fish",
        "en:peanuts",
        "en:soybeans",
        "en:milk",
        "en:nuts",
        "en:celery",
        "en:mustard",
        "en:sesame-seeds",
        "en:sulphur-dioxide-and-sulphites",
        "en:lupin",
        "en:molluscs"
    };

    public const int MaxWatchedIngredients = 20;
    public const int MinIngredientLength = 2;
    public const int MaxIngredientLength = 50;

    // Columnas: las etiquetas se guardan separadas por coma y las palabras, una por línea.
    public const int MaxWatchedAllergensLength = 512;
    public const int MaxWatchedIngredientsLength = 1024;
}
