namespace SmartPantry.Preferences;

/// <summary>
/// Resultado de comparar un producto con las preferencias del usuario (RF-12). Es informativo:
/// ningún estado significa "apto" ni "seguro" (reglas 1 y 2).
/// </summary>
public enum PreferenceEvaluationStatus
{
    /// <summary>El usuario no vigila ningún alérgeno ni ingrediente.</summary>
    NoPreferences = 0,

    /// <summary>Sin coincidencias, y estaban todos los datos necesarios para comparar.</summary>
    NoMatch = 1,

    /// <summary>Algún alérgeno o ingrediente vigilado aparece en el producto.</summary>
    Match = 2,

    /// <summary>Sin coincidencias, pero el producto no informa algún dato necesario para comparar.</summary>
    NotVerifiable = 3
}
