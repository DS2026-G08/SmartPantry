using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Products;

public class CreateProductDto
{
    [Required]
    [StringLength(ProductConsts.MaxNameLength)]
    public string Name { get; set; } = null!;

    [Required]
    [StringLength(ProductConsts.MaxBrandLength)]
    public string Brand { get; set; } = null!;

    [StringLength(ProductConsts.MaxBarcodeLength)]
    public string? Barcode { get; set; }
}