using System.Collections.Generic;

namespace SmartPantry.ExternalProducts;

/// <summary>
/// DTO interno del grupo: lo que el cliente externo le entrega al AppService.
/// No es el JSON del proveedor ni el contrato público del endpoint.
/// </summary>
public class ExternalProductDto
{
    public string Barcode { get; set; } = null!;
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? ImageUrl { get; set; }

    // Detalle (RF-07). En los resultados de la búsqueda por texto sólo vienen los datos de arriba.
    public string? Categories { get; set; }
    public string? Ingredients { get; set; }
    public List<string> AllergenTags { get; set; } = new();
    public string? NutriScore { get; set; }
    public int? NovaGroup { get; set; }
}
