using System.Collections.Generic;

namespace SmartPantry.Preferences;

/// <summary>Preferencias del usuario actual (RF-03). No lleva id de usuario: siempre son las propias.</summary>
public class DietaryPreferenceDto
{
    /// <summary>Etiquetas canónicas de Open Food Facts, por ejemplo "en:milk".</summary>
    public List<string> WatchedAllergens { get; set; } = new();

    /// <summary>Palabras libres que se buscan en el texto de ingredientes.</summary>
    public List<string> WatchedIngredients { get; set; } = new();

    public bool ShowAlerts { get; set; } = true;
}
