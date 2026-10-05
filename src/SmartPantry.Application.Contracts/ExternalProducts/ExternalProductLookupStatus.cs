namespace SmartPantry.ExternalProducts;

/// <summary>
/// Resultado propio de la consulta al catálogo externo.
/// El valor 0 no se usa a propósito: un resultado sin asignar no se confunde con "encontrado".
/// </summary>
public enum ExternalProductLookupStatus
{
    /// <summary>El proveedor devolvió el producto.</summary>
    Found = 1,

    /// <summary>El proveedor respondió, pero no conoce ese código.</summary>
    NotFound = 2,

    /// <summary>El proveedor limitó temporalmente las consultas (HTTP 429).</summary>
    RateLimited = 3,

    /// <summary>El proveedor no respondió, tardó demasiado o devolvió un error.</summary>
    ServiceUnavailable = 4
}
