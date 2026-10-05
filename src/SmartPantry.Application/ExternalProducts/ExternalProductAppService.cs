using System.Threading.Tasks;

namespace SmartPantry.ExternalProducts;

// El acceso anónimo viene de SmartPantryAppService (decisión transitoria de esta etapa).
public class ExternalProductAppService : SmartPantryAppService, IExternalProductAppService
{
    private readonly IExternalProductCatalogClient _catalogClient;

    // Depende sólo de la interfaz: no conoce HttpClient, rutas, encabezados ni el JSON del proveedor.
    public ExternalProductAppService(IExternalProductCatalogClient catalogClient)
    {
        _catalogClient = catalogClient;
    }

    public async Task<ExternalProductLookupResultDto> GetByBarcodeAsync(ExternalProductLookupInputDto input)
    {
        var barcode = input.Barcode.Trim();

        try
        {
            var product = await _catalogClient.GetByBarcodeAsync(barcode);

            if (product == null)
            {
                return WithoutProduct(barcode, ExternalProductLookupStatus.NotFound);
            }

            return new ExternalProductLookupResultDto
            {
                Status = ExternalProductLookupStatus.Found,
                Barcode = barcode,
                Name = NullIfBlank(product.Name),
                Brand = NullIfBlank(product.Brand),
                ImageUrl = NullIfBlank(product.ImageUrl)
            };
        }
        catch (ExternalCatalogRateLimitException)
        {
            return WithoutProduct(barcode, ExternalProductLookupStatus.RateLimited);
        }
        catch (ExternalCatalogUnavailableException)
        {
            return WithoutProduct(barcode, ExternalProductLookupStatus.ServiceUnavailable);
        }
    }

    private static ExternalProductLookupResultDto WithoutProduct(string barcode, ExternalProductLookupStatus status)
    {
        return new ExternalProductLookupResultDto
        {
            Status = status,
            Barcode = barcode
        };
    }

    // RF-09: un dato vacío se informa como ausente (null), nunca se completa con un valor inventado.
    private static string? NullIfBlank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
