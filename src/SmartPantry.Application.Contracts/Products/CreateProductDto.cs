using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Products;

public class CreateProductDto
{
    [Required]
    [StringLength(ProductConsts.MaxNameLength)]
    public string Name { get; set; } = null!;

    // RF-09: la marca es opcional.
    [StringLength(ProductConsts.MaxBrandLength)]
    public string? Brand { get; set; }

    [StringLength(ProductConsts.MaxBarcodeLength)]
    public string? Barcode { get; set; }
}