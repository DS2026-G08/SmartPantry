using System.ComponentModel.DataAnnotations;
using SmartPantry.ExternalProducts;

namespace SmartPantry.Products;

/// <summary>
/// RF-08: guardar un producto del catálogo externo. Sólo se envía el código: el servidor vuelve a
/// consultar el catálogo y guarda lo que obtiene, sin confiar en datos armados por el navegador.
/// </summary>
public class SaveExternalProductDto
{
    [Required]
    [RegularExpression(ExternalProductLookupInputDto.BarcodePattern, ErrorMessage = "El código de barras debe contener entre 8 y 14 dígitos.")]
    public string Barcode { get; set; } = null!;

    /// <summary>
    /// Nombre que escribe el usuario. Sólo se usa si el catálogo externo no informa uno (RF-09:
    /// el sistema no lo inventa, lo pide).
    /// </summary>
    [StringLength(ProductConsts.MaxNameLength)]
    public string? Name { get; set; }
}
