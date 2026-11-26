using System.ComponentModel.DataAnnotations;

namespace SmartPantry.ExternalProducts;

/// <summary>Búsqueda por texto (RF-06): nombre, marca o categoría.</summary>
public class ExternalProductSearchInputDto : IValidatableObject
{
    public const int MinTextLength = 2;
    public const int MaxTextLength = 100;
    public const int MaxPageSize = 50;

    [Required]
    [StringLength(MaxTextLength, MinimumLength = MinTextLength)]
    public string Text { get; set; } = null!;

    [Range(0, int.MaxValue)]
    public int SkipCount { get; set; }

    [Range(1, MaxPageSize)]
    public int MaxResultCount { get; set; } = 10;

    // El proveedor pagina por número de página: el salto tiene que ser múltiplo del tamaño de página.
    public System.Collections.Generic.IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaxResultCount > 0 && SkipCount % MaxResultCount != 0)
        {
            yield return new ValidationResult(
                "SkipCount debe ser múltiplo de MaxResultCount.",
                new[] { nameof(SkipCount) });
        }
    }
}
