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
}
