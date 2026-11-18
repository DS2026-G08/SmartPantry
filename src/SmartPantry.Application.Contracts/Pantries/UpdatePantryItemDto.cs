using System;
using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Pantries;

// Validación en servidor. La UI repite estas reglas por usabilidad, pero no las reemplaza.
public class UpdatePantryItemDto
{
    [Range(typeof(decimal), "0", "999999")]
    public decimal? Quantity { get; set; }

    [StringLength(PantryItemConsts.MaxUnitLength)]
    public string? Unit { get; set; }

    // Una fecha con formato inválido la rechaza el model binding (400) antes de llegar al servicio.
    public DateOnly? ExpirationDate { get; set; }
}
