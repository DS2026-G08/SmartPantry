using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace SmartPantry.Products;

public class ProductDto : EntityDto<Guid>
{
    public string Name { get; set; } = null!;

    // RF-09: los datos no informados quedan en null (o en una lista vacía).
    public string? Brand { get; set; }
    public string? Barcode { get; set; }
    public string? ImageUrl { get; set; }
    public string? Categories { get; set; }
    public string? Ingredients { get; set; }

    /// <summary>Etiquetas de Open Food Facts, por ejemplo "en:milk".</summary>
    public List<string> Allergens { get; set; } = new();

    public string? NutriScore { get; set; }
    public int? NovaGroup { get; set; }
}
