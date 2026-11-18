using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartPantry.ExternalProducts;

// Clases internas: sólo sirven para leer los campos necesarios del JSON de Open Food Facts.
// No se exponen como DTOs del endpoint propio.
internal class OpenFoodFactsProductResponse
{
    [JsonPropertyName("product")]
    public OpenFoodFactsProduct? Product { get; set; }
}

internal class OpenFoodFactsProduct
{
    [JsonPropertyName("product_name_es")]
    public string? ProductNameEs { get; set; }

    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }

    [JsonPropertyName("brands")]
    public string? Brands { get; set; }

    [JsonPropertyName("image_front_url")]
    public string? ImageFrontUrl { get; set; }

    [JsonPropertyName("categories")]
    public string? Categories { get; set; }

    [JsonPropertyName("ingredients_text_es")]
    public string? IngredientsTextEs { get; set; }

    [JsonPropertyName("ingredients_text")]
    public string? IngredientsText { get; set; }

    [JsonPropertyName("allergens_tags")]
    public List<string>? AllergensTags { get; set; }

    [JsonPropertyName("nutriscore_grade")]
    public string? NutriscoreGrade { get; set; }

    // El proveedor lo envía como número, pero se acepta también como texto.
    [JsonPropertyName("nova_group")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int? NovaGroup { get; set; }
}

// Search-a-licious (búsqueda por texto, RF-06).
internal class SearchALiciousResponse
{
    [JsonPropertyName("hits")]
    public List<SearchALiciousHit>? Hits { get; set; }

    [JsonPropertyName("count")]
    public long Count { get; set; }
}

internal class SearchALiciousHit
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("product_name_es")]
    public string? ProductNameEs { get; set; }

    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }

    // Search-a-licious devuelve las marcas como lista; se acepta también un texto por las dudas.
    [JsonPropertyName("brands")]
    public JsonElement Brands { get; set; }

    [JsonPropertyName("image_front_url")]
    public string? ImageFrontUrl { get; set; }

    public string? GetBrands()
    {
        return Brands.ValueKind switch
        {
            JsonValueKind.String => Brands.GetString(),
            JsonValueKind.Array => string.Join(", ", Brands.EnumerateArray()
                .Where(b => b.ValueKind == JsonValueKind.String)
                .Select(b => b.GetString())
                .Where(b => !string.IsNullOrWhiteSpace(b))),
            _ => null
        };
    }
}
