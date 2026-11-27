using System.Linq;

namespace SmartPantry.Preferences;

public static class PreferenceMapping
{
    public static DietaryPreferenceDto ToDto(DietaryPreference? preference)
    {
        // Sin preferencias guardadas: listas vacías y alertas encendidas (valor por defecto).
        return preference == null
            ? new DietaryPreferenceDto()
            : new DietaryPreferenceDto
            {
                WatchedAllergens = preference.GetWatchedAllergens().ToList(),
                WatchedIngredients = preference.GetWatchedIngredients().ToList(),
                ShowAlerts = preference.ShowAlerts
            };
    }

    public static PreferenceEvaluationDto ToDto(PreferenceEvaluation evaluation)
    {
        return new PreferenceEvaluationDto
        {
            Status = evaluation.Status,
            MatchedAllergens = evaluation.MatchedAllergens.ToList(),
            MatchedIngredients = evaluation.MatchedIngredients.ToList(),
            MissingAllergens = evaluation.MissingAllergens,
            MissingIngredients = evaluation.MissingIngredients
        };
    }

    public static PreferenceEvaluationDto ToDto(PreferenceWarning warning)
    {
        return new PreferenceEvaluationDto
        {
            Status = warning.Status,
            MatchedAllergens = warning.GetMatchedAllergens().ToList(),
            MatchedIngredients = warning.GetMatchedIngredients().ToList(),
            MissingAllergens = warning.MissingAllergens,
            MissingIngredients = warning.MissingIngredients
        };
    }
}
