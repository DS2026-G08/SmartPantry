using System.Collections.Generic;

namespace SmartPantry.Preferences;

/// <summary>
/// Comparación de un producto con las preferencias (RF-12). Es informativa: NoMatch significa
/// "sin coincidencias según los datos disponibles", nunca "apto" ni "seguro".
/// </summary>
public class PreferenceEvaluationDto
{
    public PreferenceEvaluationStatus Status { get; set; }

    /// <summary>Etiquetas vigiladas que aparecen en el producto, por ejemplo "en:milk".</summary>
    public List<string> MatchedAllergens { get; set; } = new();

    /// <summary>Palabras vigiladas que aparecen en los ingredientes, tal como las escribió el usuario.</summary>
    public List<string> MatchedIngredients { get; set; } = new();

    /// <summary>Se vigila algún alérgeno y el producto no informa alérgenos.</summary>
    public bool MissingAllergens { get; set; }

    /// <summary>Se vigila alguna palabra y el producto no informa ingredientes.</summary>
    public bool MissingIngredients { get; set; }
}
