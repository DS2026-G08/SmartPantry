using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartPantry.ExternalProducts;

/// <summary>
/// Consulta a Open Food Facts: Product Opener API v3 para el código de barras y Search-a-licious
/// para la búsqueda por texto. Es la única clase que conoce las rutas, los códigos HTTP y el JSON del proveedor.
/// </summary>
public class OpenFoodFactsProductCatalogClient : IExternalProductCatalogClient
{
    // Sólo se piden los campos que el sistema usa (RF-07).
    private const string ProductFields =
        "product_name,product_name_es,brands,image_front_url,categories," +
        "ingredients_text,ingredients_text_es,allergens_tags,nutriscore_grade,nova_group";

    private const string SearchFields = "code,product_name,product_name_es,brands,image_front_url";

    /// <summary>
    /// Search-a-licious es un servicio aparte del de productos. La URL es absoluta, así que no usa
    /// la dirección base del HttpClient (que apunta a Product Opener).
    /// </summary>
    public const string SearchUrl = "https://search.openfoodfacts.org/search";

    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenFoodFactsProductCatalogClient> _logger;

    // El HttpClient lo entrega IHttpClientFactory ya configurado (URL base, timeout, User-Agent).
    public OpenFoodFactsProductCatalogClient(
        HttpClient httpClient,
        ILogger<OpenFoodFactsProductCatalogClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ExternalProductDto?> GetByBarcodeAsync(string barcode)
    {
        var path = $"product/{Uri.EscapeDataString(barcode)}?fields={Uri.EscapeDataString(ProductFields)}";
        var call = $"código {barcode}";

        var body = await SendAsync<OpenFoodFactsProductResponse>(path, call, allowNotFound: true);
        if (body?.Product == null)
        {
            return null;
        }

        return ToExternalProduct(barcode, body.Product);
    }

    public async Task<ExternalProductSearchPage> SearchAsync(string text, int page, int pageSize)
    {
        var query = SanitizeQuery(text);
        if (query.Length == 0)
        {
            // Sólo había signos: no hay nada que buscar y no se consulta al proveedor.
            return new ExternalProductSearchPage();
        }

        var url = $"{SearchUrl}?q={Uri.EscapeDataString(query)}&langs=es&page={page}&page_size={pageSize}" +
                  $"&fields={Uri.EscapeDataString(SearchFields)}";

        // No se registra el texto buscado: puede contener datos que el usuario no quiere dejar en un log.
        var body = await SendAsync<SearchALiciousResponse>(url, $"búsqueda por texto, página {page}", allowNotFound: false);

        return new ExternalProductSearchPage
        {
            TotalCount = body?.Count ?? 0,
            Items = (body?.Hits ?? new())
                .Where(h => !string.IsNullOrWhiteSpace(h.Code))
                .Select(h => new ExternalProductDto
                {
                    Barcode = h.Code!.Trim(),
                    Name = FirstWithValue(h.ProductNameEs, h.ProductName),
                    Brand = FirstWithValue(h.GetBrands()),
                    ImageUrl = FirstWithValue(h.ImageFrontUrl)
                })
                .ToList()
        };
    }

    /// <summary>
    /// Hace la llamada y traduce los errores del proveedor a las excepciones propias.
    /// Devuelve null sólo cuando <paramref name="allowNotFound"/> es true y el proveedor responde 404.
    /// </summary>
    private async Task<T?> SendAsync<T>(string requestUri, string call, bool allowNotFound)
        where T : class
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await _httpClient.GetAsync(requestUri);

            if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound)
            {
                LogCall(call, "no encontrado", stopwatch);
                return null;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                LogCall(call, "límite de solicitudes", stopwatch);
                throw new ExternalCatalogRateLimitException();
            }

            if (!response.IsSuccessStatusCode)
            {
                LogCall(call, $"HTTP {(int)response.StatusCode}", stopwatch);
                throw new ExternalCatalogUnavailableException(
                    $"Open Food Facts devolvió HTTP {(int)response.StatusCode}.");
            }

            var body = await response.Content.ReadFromJsonAsync<T>();
            LogCall(call, "correcta", stopwatch);
            return body;
        }
        catch (TaskCanceledException ex)
        {
            LogCall(call, "tiempo de espera agotado", stopwatch);
            throw new ExternalCatalogUnavailableException("La consulta a Open Food Facts excedió el tiempo de espera.", ex);
        }
        catch (HttpRequestException ex)
        {
            LogCall(call, "sin conexión", stopwatch);
            throw new ExternalCatalogUnavailableException("No se pudo conectar con Open Food Facts.", ex);
        }
        catch (JsonException ex)
        {
            LogCall(call, "respuesta no interpretable", stopwatch);
            throw new ExternalCatalogUnavailableException("Open Food Facts devolvió una respuesta que no pudo interpretarse.", ex);
        }
    }

    // Traduce el JSON externo al DTO interno. Lo que el proveedor no informa queda en null.
    private static ExternalProductDto ToExternalProduct(string barcode, OpenFoodFactsProduct product)
    {
        return new ExternalProductDto
        {
            Barcode = barcode,
            Name = FirstWithValue(product.ProductNameEs, product.ProductName),
            Brand = FirstWithValue(product.Brands),
            ImageUrl = FirstWithValue(product.ImageFrontUrl),
            Categories = FirstWithValue(product.Categories),
            Ingredients = FirstWithValue(product.IngredientsTextEs, product.IngredientsText),
            AllergenTags = product.AllergensTags ?? new(),
            NutriScore = FirstWithValue(product.NutriscoreGrade),
            NovaGroup = product.NovaGroup
        };
    }

    /// <summary>
    /// Search-a-licious interpreta una sintaxis de consulta (campos, comillas, operadores). El texto del
    /// usuario se reduce a letras, números y espacios para que siempre sea una búsqueda simple.
    /// </summary>
    private static string SanitizeQuery(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            builder.Append(char.IsLetterOrDigit(c) ? c : ' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? FirstWithValue(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    // Observabilidad: tipo de llamada, resultado y duración. No se registra el contenido de la respuesta.
    private void LogCall(string call, string result, Stopwatch stopwatch)
    {
        _logger.LogInformation(
            "Open Food Facts - {Call}: {Result} ({ElapsedMilliseconds} ms)",
            call,
            result,
            stopwatch.ElapsedMilliseconds);
    }
}
