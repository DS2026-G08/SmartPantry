using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartPantry.ExternalProducts;

/// <summary>
/// Consulta por código de barras a Open Food Facts (Product Opener API v3).
/// Es la única clase que conoce la ruta, los códigos HTTP y el JSON del proveedor.
/// </summary>
public class OpenFoodFactsProductCatalogClient : IExternalProductCatalogClient
{
    // Sólo se piden los campos que el sistema usa.
    private const string Fields = "product_name,product_name_es,brands,image_front_url";

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
        var path = $"product/{Uri.EscapeDataString(barcode)}?fields={Uri.EscapeDataString(Fields)}";
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await _httpClient.GetAsync(path);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                LogCall(barcode, "no encontrado", stopwatch);
                return null;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                LogCall(barcode, "límite de solicitudes", stopwatch);
                throw new ExternalCatalogRateLimitException();
            }

            if (!response.IsSuccessStatusCode)
            {
                LogCall(barcode, $"HTTP {(int)response.StatusCode}", stopwatch);
                throw new ExternalCatalogUnavailableException(
                    $"Open Food Facts devolvió HTTP {(int)response.StatusCode}.");
            }

            var body = await response.Content.ReadFromJsonAsync<OpenFoodFactsProductResponse>();
            if (body?.Product == null)
            {
                LogCall(barcode, "no encontrado", stopwatch);
                return null;
            }

            LogCall(barcode, "encontrado", stopwatch);
            return ToExternalProduct(barcode, body.Product);
        }
        catch (TaskCanceledException ex)
        {
            LogCall(barcode, "tiempo de espera agotado", stopwatch);
            throw new ExternalCatalogUnavailableException("La consulta a Open Food Facts excedió el tiempo de espera.", ex);
        }
        catch (HttpRequestException ex)
        {
            LogCall(barcode, "sin conexión", stopwatch);
            throw new ExternalCatalogUnavailableException("No se pudo conectar con Open Food Facts.", ex);
        }
        catch (JsonException ex)
        {
            LogCall(barcode, "respuesta no interpretable", stopwatch);
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
            ImageUrl = FirstWithValue(product.ImageFrontUrl)
        };
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
    private void LogCall(string barcode, string result, Stopwatch stopwatch)
    {
        _logger.LogInformation(
            "Open Food Facts - consulta por código {Barcode}: {Result} ({ElapsedMilliseconds} ms)",
            barcode,
            result,
            stopwatch.ElapsedMilliseconds);
    }
}
