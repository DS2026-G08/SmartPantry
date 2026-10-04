using System.Text.Json.Serialization;

namespace SmartPantry.ExternalProducts
{
    // Clases internas exclusivas de la capa de infraestructura
    internal class OpenFoodFactsResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; } 

        [JsonPropertyName("product")]
        public OpenFoodFactsProductData? Product { get; set; }
    }

    internal class OpenFoodFactsProductData
    {
        [JsonPropertyName("product_name")]
        public string? ProductName { get; set; }

        [JsonPropertyName("brands")]
        public string? Brands { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }
    }
}