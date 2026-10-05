using System.Threading.Tasks;

namespace SmartPantry.ExternalProducts;

/// <summary>
/// Representa la necesidad de consultar un catálogo externo de productos.
/// No conoce HttpClient ni Open Food Facts: la implementación concreta vive en HttpApi.Host.
/// </summary>
public interface IExternalProductCatalogClient
{
    /// <summary>
    /// Devuelve el producto, o null si el catálogo no conoce ese código.
    /// Lanza <see cref="ExternalCatalogRateLimitException"/> si el proveedor limita las consultas
    /// y <see cref="ExternalCatalogUnavailableException"/> si no está disponible.
    /// </summary>
    Task<ExternalProductDto?> GetByBarcodeAsync(string barcode);
}
