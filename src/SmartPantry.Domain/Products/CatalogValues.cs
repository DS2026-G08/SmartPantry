using System.Collections.Generic;
using System.Linq;

namespace SmartPantry.Products;

/// <summary>
/// Normaliza los datos que llegan del catálogo externo (RF-07, RF-09). Un valor que no se puede
/// interpretar se informa como ausente (null o lista vacía): nunca se inventa ni se completa.
/// Lo usan tanto el producto guardado como la respuesta de la búsqueda, para que muestren lo mismo.
/// </summary>
public static class CatalogValues
{
    /// <summary>
    /// Conserva sólo las etiquetas canónicas en inglés ("en:milk"). Open Food Facts a veces agrega
    /// texto libre cargado por usuarios (por ejemplo "es:Contiene leche") que no sirve para comparar.
    /// </summary>
    public static List<string> NormalizeAllergenTags(IEnumerable<string>? tags)
    {
        if (tags == null)
        {
            return new List<string>();
        }

        return tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.StartsWith("en:") && t.Length > 3 && !t.Contains(','))
            .Distinct()
            .ToList();
    }

    /// <summary>"a" a "e"; cualquier otro valor ("unknown", "not-applicable") es null.</summary>
    public static string? NormalizeNutriScore(string? value)
    {
        var grade = value?.Trim().ToLowerInvariant();
        return grade != null && ProductConsts.NutriScoreGrades.Contains(grade) ? grade : null;
    }

    /// <summary>1 a 4; fuera de ese rango es null.</summary>
    public static int? NormalizeNovaGroup(int? value)
    {
        return value is >= ProductConsts.MinNovaGroup and <= ProductConsts.MaxNovaGroup ? value : null;
    }

    /// <summary>Texto recortado; vacío es null; si excede el máximo se corta.</summary>
    public static string? NormalizeText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
