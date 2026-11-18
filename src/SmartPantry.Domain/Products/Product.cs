using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace SmartPantry.Products;

public class Product : AggregateRoot<Guid>
{
    public string Name { get; private set; } = null!;

    // RF-09: muchos productos externos no informan la marca, así que es opcional.
    public string? Brand { get; private set; }

    public string? Barcode { get; private set; }

    // Datos del catálogo externo (RF-07). Todos opcionales: si el proveedor no los informa, quedan en null.
    public string? ImageUrl { get; private set; }
    public string? Categories { get; private set; }
    public string? Ingredients { get; private set; }

    /// <summary>
    /// Alérgenos como etiquetas de Open Food Facts separadas por coma (por ejemplo "en:milk,en:nuts").
    /// Se guardan las etiquetas y no un texto traducido para poder compararlas con las preferencias del usuario.
    /// </summary>
    public string? Allergens { get; private set; }

    /// <summary>Nota del Nutri-Score, de "a" a "e".</summary>
    public string? NutriScore { get; private set; }

    /// <summary>Grupo NOVA, de 1 a 4.</summary>
    public int? NovaGroup { get; private set; }

    // Constructor privado para Entity Framework Core
    private Product()
    {
    }

    // Constructor público: aplica las mismas reglas que Update
    public Product(Guid id, string name, string? brand, string? barcode = null)
        : base(id)
    {
        Update(name, brand, barcode);
    }

    /// <summary>
    /// Modifica los datos del producto. Primero valida y normaliza todos los valores;
    /// sólo si todos son válidos cambia el estado. Una entrada inválida no deja cambios parciales.
    /// </summary>
    public void Update(string name, string? brand, string? barcode)
    {
        // 1) Validar y normalizar (sin tocar la entidad todavía)
        var normalizedName = NormalizeRequired(name, nameof(name), ProductConsts.MaxNameLength);
        var normalizedBrand = NormalizeOptional(brand, nameof(brand), ProductConsts.MaxBrandLength);
        var normalizedBarcode = NormalizeOptional(barcode, nameof(barcode), ProductConsts.MaxBarcodeLength);

        // 2) Recién ahora cambiar el estado
        Name = normalizedName;
        Brand = normalizedBrand;
        Barcode = normalizedBarcode;
    }

    /// <summary>
    /// Reemplaza los datos que provienen del catálogo externo (RF-07).
    /// Los textos largos se recortan y los valores fuera de rango se descartan: son datos de terceros
    /// y un dato dudoso se informa como ausente en lugar de rechazar todo el producto.
    /// </summary>
    public void SetCatalogDetails(
        string? imageUrl,
        string? categories,
        string? ingredients,
        IEnumerable<string>? allergenTags,
        string? nutriScore,
        int? novaGroup)
    {
        var allergens = CatalogValues.NormalizeAllergenTags(allergenTags);

        ImageUrl = CatalogValues.NormalizeText(imageUrl, ProductConsts.MaxImageUrlLength);
        Categories = CatalogValues.NormalizeText(categories, ProductConsts.MaxCategoriesLength);
        Ingredients = CatalogValues.NormalizeText(ingredients, ProductConsts.MaxIngredientsLength);
        Allergens = JoinTags(allergens);
        NutriScore = CatalogValues.NormalizeNutriScore(nutriScore);
        NovaGroup = CatalogValues.NormalizeNovaGroup(novaGroup);
    }

    /// <summary>Alérgenos como lista de etiquetas. Lista vacía si no hay ninguno informado.</summary>
    public IReadOnlyList<string> GetAllergenTags()
    {
        return string.IsNullOrEmpty(Allergens)
            ? Array.Empty<string>()
            : Allergens.Split(',', StringSplitOptions.RemoveEmptyEntries);
    }

    private static string NormalizeRequired(string value, string parameterName, int maxLength)
    {
        Check.NotNullOrWhiteSpace(value, parameterName);
        var trimmed = value.Trim();
        Check.Length(trimmed, parameterName, maxLength);
        return trimmed;
    }

    private static string? NormalizeOptional(string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        Check.Length(trimmed, parameterName, maxLength);
        return trimmed;
    }

    // Se agregan etiquetas completas mientras entren en la columna: nunca se guarda una etiqueta cortada.
    private static string? JoinTags(IReadOnlyList<string> tags)
    {
        var kept = new List<string>();
        var length = 0;
        foreach (var tag in tags)
        {
            var added = length == 0 ? tag.Length : tag.Length + 1;
            if (length + added > ProductConsts.MaxAllergensLength)
            {
                break;
            }

            kept.Add(tag);
            length += added;
        }

        return kept.Count == 0 ? null : string.Join(',', kept);
    }
}
