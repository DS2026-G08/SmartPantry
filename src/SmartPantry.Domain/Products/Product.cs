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

    // Constructor público con las reglas de negocio
    public Product(Guid id, string name, string brand, string? barcode = null)
        : base(id)
    {
        SetName(name);
        SetBrand(brand);
        Barcode = barcode?.Trim();
    }

    public void SetName(string name)
    {
        // Valida que no sea nulo ni puros espacios
        Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: ProductConsts.MaxNameLength);
        Name = name.Trim();
    }

    public void SetBrand(string brand)
    {
        Check.NotNullOrWhiteSpace(brand, nameof(brand), maxLength: ProductConsts.MaxBrandLength);
        Brand = brand.Trim();
    }
}