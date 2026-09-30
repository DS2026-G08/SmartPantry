using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace SmartPantry.Products;

public class Product : AggregateRoot<Guid>
{
    public string Name { get; private set; } = null!;
    public string Brand { get; private set; } = null!;
    public string? Barcode { get; private set; }

    // Constructor privado para Entity Framework Core
    private Product()
    {
    }

    // Constructor público: aplica las mismas reglas que Update
    public Product(Guid id, string name, string brand, string? barcode = null)
        : base(id)
    {
        Update(name, brand, barcode);
    }

    /// <summary>
    /// Modifica los datos del producto. Primero valida y normaliza todos los valores;
    /// sólo si todos son válidos cambia el estado. Una entrada inválida no deja cambios parciales.
    /// </summary>
    public void Update(string name, string brand, string? barcode)
    {
        // 1) Validar y normalizar (sin tocar la entidad todavía)
        var normalizedName = NormalizeRequired(name, nameof(name), ProductConsts.MaxNameLength);
        var normalizedBrand = NormalizeRequired(brand, nameof(brand), ProductConsts.MaxBrandLength);
        var normalizedBarcode = NormalizeBarcode(barcode);

        // 2) Recién ahora cambiar el estado
        Name = normalizedName;
        Brand = normalizedBrand;
        Barcode = normalizedBarcode;
    }

    private static string NormalizeRequired(string value, string parameterName, int maxLength)
    {
        Check.NotNullOrWhiteSpace(value, parameterName);
        var trimmed = value.Trim();
        Check.Length(trimmed, parameterName, maxLength);
        return trimmed;
    }

    private static string? NormalizeBarcode(string? barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }

        var trimmed = barcode.Trim();
        Check.Length(trimmed, nameof(barcode), ProductConsts.MaxBarcodeLength);
        return trimmed;
    }
}