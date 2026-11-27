using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartPantry.Preferences;

/// <summary>
/// Compara datos de un producto con las preferencias del usuario actual (RF-12, "al abrir").
/// Está detrás de una interfaz para que <see cref="ExternalProducts.ExternalProductAppService"/>
/// siga probándose sin base de datos ni usuario.
/// </summary>
public interface IPreferenceEvaluator
{
    /// <summary>Null si el usuario apagó "Mostrar alertas".</summary>
    Task<PreferenceEvaluationDto?> EvaluateForCurrentUserAsync(IEnumerable<string> allergenTags, string? ingredients);
}
