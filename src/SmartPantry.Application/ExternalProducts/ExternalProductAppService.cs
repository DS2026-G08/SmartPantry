using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SmartPantry.Products;

namespace SmartPantry.ExternalProducts;

// RF-05, RF-06 y RF-07 (consulta externa). TP09: requiere un usuario autenticado.
[Authorize]
public class ExternalProductAppService : SmartPantryAppService, IExternalProductAppService
{
    /// <summary>
    /// Máximo de resultados navegables de la búsqueda por texto. El proveedor informa hasta 10.000
    /// coincidencias, pero nadie recorre tantas páginas y las páginas muy profundas fallan.
    /// </summary>
    public const int MaxNavigableResults = 1000;

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
                ImageUrl = NullIfBlank(product.ImageUrl),
                Categories = NullIfBlank(product.Categories),
                Ingredients = NullIfBlank(product.Ingredients),
                Allergens = CatalogValues.NormalizeAllergenTags(product.AllergenTags),
                NutriScore = CatalogValues.NormalizeNutriScore(product.NutriScore),
                NovaGroup = CatalogValues.NormalizeNovaGroup(product.NovaGroup)
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

    public async Task<ExternalProductSearchResultDto> GetListAsync(ExternalProductSearchInputDto input)
    {
        var text = input.Text.Trim();
        var page = input.SkipCount / input.MaxResultCount + 1;

        // Más allá del máximo navegable no se consulta al proveedor.
        if (input.SkipCount >= MaxNavigableResults)
        {
            return new ExternalProductSearchResultDto { Status = ExternalProductLookupStatus.NotFound };
        }

        try
        {
            var result = await _catalogClient.SearchAsync(text, page, input.MaxResultCount);

            var items = result.Items
                .Where(p => !string.IsNullOrWhiteSpace(p.Barcode))
                .Select(p => new ExternalProductSummaryDto
                {
                    Barcode = p.Barcode.Trim(),
                    Name = NullIfBlank(p.Name),
                    Brand = NullIfBlank(p.Brand),
                    ImageUrl = NullIfBlank(p.ImageUrl)
                })
                .ToList();

            return new ExternalProductSearchResultDto
            {
                Status = items.Count > 0 ? ExternalProductLookupStatus.Found : ExternalProductLookupStatus.NotFound,
                Items = items,
                TotalCount = Math.Min(result.TotalCount, MaxNavigableResults)
            };
        }
        catch (ExternalCatalogRateLimitException)
        {
            return new ExternalProductSearchResultDto { Status = ExternalProductLookupStatus.RateLimited };
        }
        catch (ExternalCatalogUnavailableException)
        {
            return new ExternalProductSearchResultDto { Status = ExternalProductLookupStatus.ServiceUnavailable };
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
