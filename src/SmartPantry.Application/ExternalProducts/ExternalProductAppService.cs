using System.Threading.Tasks;

namespace SmartPantry.ExternalProducts
{
    public class ExternalProductAppService : SmartPantryAppService, IExternalProductAppService
    {
        private readonly IExternalProductCatalogClient _externalCatalogClient;

        public ExternalProductAppService(IExternalProductCatalogClient externalCatalogClient)
        {
            _externalCatalogClient = externalCatalogClient;
        }

        public async Task<ExternalProductLookupResultDto> GetProductByBarcodeAsync(GetExternalProductInputDto input)
        {
            // Delegamos la búsqueda al cliente HTTP
            var product = await _externalCatalogClient.GetByBarcodeAsync(input.Barcode);

            // Si el cliente devuelve null, empaquetamos el error amigablemente
            if (product == null)
            {
                return new ExternalProductLookupResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = $"No se encontró ningún producto con el código de barras {input.Barcode}.",
                    Product = null
                };
            }

            // Si todo sale bien, devolvemos el estado exitoso junto con los datos
            return new ExternalProductLookupResultDto
            {
                IsSuccess = true,
                ErrorMessage = null,
                Product = product
            };
        }
    }
}