using System.Collections.Generic;

namespace SmartPantry.ExternalProducts;

/// <summary>Una página de resultados de la búsqueda por texto en el catálogo externo.</summary>
public class ExternalProductSearchPage
{
    public List<ExternalProductDto> Items { get; set; } = new();

    /// <summary>Cantidad total de coincidencias que informa el proveedor (puede ser aproximada).</summary>
    public long TotalCount { get; set; }
}
