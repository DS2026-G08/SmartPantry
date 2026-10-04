using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using SmartPantry.ExternalProducts;

namespace SmartPantry.ExternalProducts
{
    public class OpenFoodFactsProductCatalogClient : IExternalProductCatalogClient
    {
        private readonly HttpClient _httpClient;

        // Se inyecta HttpClient provisto por IHttpClientFactory
        public OpenFoodFactsProductCatalogClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ExternalProductDto?> GetByBarcodeAsync(string barcode)
        {
            // Ejecutamos la petición GET armando la URL con el código
            var response = await _httpClient.GetAsync($"{barcode}.json");

            if (!response.IsSuccessStatusCode)
            {
                // Si la API devuelve 404 u otro error, asumimos que no se encontró o falló
                return null;
            }

            // Deserializamos usando nuestras clases internas
            var externalData = await response.Content.ReadFromJsonAsync<OpenFoodFactsResponse>();

            if (externalData == null || externalData.Status != "success" || externalData.Product == null)
            {
                return null;
            }

            // Mapeamos los datos externos hacia nuestro contrato propio (ExternalProductDto)
            return new ExternalProductDto
            {
                Barcode = barcode,
                Name = externalData.Product.ProductName,
                Brand = externalData.Product.Brands,
                ImageUrl = externalData.Product.ImageUrl
            };
        }
    }
}