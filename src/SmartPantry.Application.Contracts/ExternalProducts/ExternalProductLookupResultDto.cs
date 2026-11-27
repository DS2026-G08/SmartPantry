using System.Collections.Generic;
using SmartPantry.Preferences;

namespace SmartPantry.ExternalProducts;

/// <summary>
/// Respuesta propia de SmartPantry para la búsqueda por código (RF-05) y el detalle (RF-07).
/// No copia la estructura de Open Food Facts: sólo expone lo que el sistema necesita.
/// </summary>
public class ExternalProductLookupResultDto
{
    public ExternalProductLookupStatus Status { get; set; }

    /// <summary>Código consultado. Es el dato de consulta, no un identificador de usuario.</summary>
    public string Barcode { get; set; } = null!;

    // RF-09: si el proveedor no informa un dato, queda en null (o en una lista vacía). No se inventa un valor.
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? ImageUrl { get; set; }
    public string? Categories { get; set; }
    public string? Ingredients { get; set; }

    /// <summary>Etiquetas de Open Food Facts, por ejemplo "en:milk". Vacía si no informa alérgenos.</summary>
    public List<string> Allergens { get; set; } = new();

    /// <summary>"a" a "e", o null si no está informado.</summary>
    public string? NutriScore { get; set; }

    /// <summary>1 a 4, o null si no está informado.</summary>
    public int? NovaGroup { get; set; }

    /// <summary>
    /// RF-12, "al abrir": comparación con las preferencias del usuario actual. Se calcula en el momento
    /// y no se guarda. Es null si el producto no se encontró o si el usuario apagó "Mostrar alertas".
    /// </summary>
    public PreferenceEvaluationDto? Preferences { get; set; }
}
