using System.ComponentModel.DataAnnotations;

namespace SmartPantry.ExternalProducts
{
    public class GetExternalProductInputDto
    {
        [Required(ErrorMessage = "El código de barras es obligatorio.")]
        [StringLength(14, MinimumLength = 8, ErrorMessage = "El código de barras debe tener entre 8 y 14 caracteres.")]
        public string Barcode { get; set; } = null!;
    }
}