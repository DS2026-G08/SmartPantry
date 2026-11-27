using System;
using System.Collections.Generic;

namespace SmartPantry.Preferences;

/// <summary>
/// Resultado de <see cref="PreferenceMatcher"/>. Las coincidencias se informan con los valores que
/// vigila el usuario (la etiqueta "en:milk" o la palabra tal como la escribió).
/// </summary>
public class PreferenceEvaluation
{
    public static readonly PreferenceEvaluation NoPreferences =
        new(PreferenceEvaluationStatus.NoPreferences, Array.Empty<string>(), Array.Empty<string>(), false, false);

    public PreferenceEvaluationStatus Status { get; }
    public IReadOnlyList<string> MatchedAllergens { get; }
    public IReadOnlyList<string> MatchedIngredients { get; }

    /// <summary>Se vigila algún alérgeno y el producto no informa ninguno.</summary>
    public bool MissingAllergens { get; }

    /// <summary>Se vigila alguna palabra y el producto no informa ingredientes.</summary>
    public bool MissingIngredients { get; }

    public PreferenceEvaluation(
        PreferenceEvaluationStatus status,
        IReadOnlyList<string> matchedAllergens,
        IReadOnlyList<string> matchedIngredients,
        bool missingAllergens,
        bool missingIngredients)
    {
        Status = status;
        MatchedAllergens = matchedAllergens;
        MatchedIngredients = matchedIngredients;
        MissingAllergens = missingAllergens;
        MissingIngredients = missingIngredients;
    }

    /// <summary>Sólo una coincidencia o un dato faltante justifican guardar una advertencia.</summary>
    public bool RequiresWarning =>
        Status is PreferenceEvaluationStatus.Match or PreferenceEvaluationStatus.NotVerifiable;
}
