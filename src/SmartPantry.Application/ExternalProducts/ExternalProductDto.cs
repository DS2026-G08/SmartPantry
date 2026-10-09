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
}
