using System.Threading.Tasks;

namespace SmartPantry.ExternalProducts;

/// <summary>
/// Representa la necesidad de consultar un catálogo externo de productos.
/// No conoce HttpClient ni Open Food Facts: la implementación concreta vive en HttpApi.Host.
/// Ambos métodos lanzan <see cref="ExternalCatalogRateLimitException"/> si el proveedor limita las consultas
/// y <see cref="ExternalCatalogUnavailableException"/> si no está disponible.
/// </summary>
public interface IExternalProductCatalogClient
{
    /// <summary>Devuelve el producto con su detalle (RF-07), o null si el catálogo no conoce ese código.</summary>
    Task<ExternalProductDto?> GetByBarcodeAsync(string barcode);

    /// <summary>
    /// Busca por nombre, marca o categoría (RF-06). <paramref name="page"/> empieza en 1.
    /// Los resultados traen sólo código, nombre, marca e imagen.
    /// </summary>
    Task<ExternalProductSearchPage> SearchAsync(string text, int page, int pageSize);
}
