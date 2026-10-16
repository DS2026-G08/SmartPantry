using System.ComponentModel.DataAnnotations;

namespace SmartPantry.ExternalProducts;

public class ExternalProductLookupInputDto
{
    // Mismo criterio que la consola de la cátedra: sólo dígitos, entre 8 y 14.
    public const string BarcodePattern = "^[0-9]{8,14}$";

    [Required]
    [RegularExpression(BarcodePattern, ErrorMessage = "El código de barras debe contener entre 8 y 14 dígitos.")]
    public string Barcode { get; set; } = null!;
}
