namespace SmartPantry.ExternalProducts;

/// <summary>
/// Respuesta propia de SmartPantry para la búsqueda por código (RF-05).
/// No copia la estructura de Open Food Facts: sólo expone lo que el sistema necesita.
/// </summary>
public class ExternalProductLookupResultDto
{
    public ExternalProductLookupStatus Status { get; set; }

    /// <summary>Código consultado. Es el dato de consulta, no un identificador de usuario.</summary>
    public string Barcode { get; set; } = null!;

    // RF-09: si el proveedor no informa un dato, queda en null. No se inventa un valor.
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? ImageUrl { get; set; }
}
