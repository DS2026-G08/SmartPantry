using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Preferences;

/// <summary>
/// Reemplaza las preferencias del usuario actual. El dominio valida cada valor (etiquetas de la lista
/// de 14, palabras de 2 a 50 caracteres, sin repetir) y responde un mensaje localizado.
/// </summary>
public class UpdateDietaryPreferenceDto
{
    [MaxLength(14)]
    public List<string> WatchedAllergens { get; set; } = new();

    [MaxLength(DietaryPreferenceConsts.MaxWatchedIngredients)]
    public List<string> WatchedIngredients { get; set; } = new();

    public bool ShowAlerts { get; set; } = true;
}
