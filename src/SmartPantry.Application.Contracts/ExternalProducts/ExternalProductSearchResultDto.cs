using System.Collections.Generic;

namespace SmartPantry.ExternalProducts;

/// <summary>Respuesta propia de la búsqueda por texto (RF-06).</summary>
public class ExternalProductSearchResultDto
{
    /// <summary>
    /// Found si hay al menos un resultado, NotFound si no hay coincidencias,
    /// RateLimited o ServiceUnavailable si el proveedor falló (en ese caso no hay ítems).
    /// </summary>
    public ExternalProductLookupStatus Status { get; set; }

    public List<ExternalProductSummaryDto> Items { get; set; } = new();

    /// <summary>Total de coincidencias, con un máximo navegable (ver ExternalProductAppService).</summary>
    public long TotalCount { get; set; }
}

/// <summary>Un resultado de la búsqueda por texto. El detalle completo se pide por código.</summary>
public class ExternalProductSummaryDto
{
    public string Barcode { get; set; } = null!;
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? ImageUrl { get; set; }
}
