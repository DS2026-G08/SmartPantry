using System;

namespace SmartPantry.ExternalProducts;

/// <summary>El catálogo externo limitó temporalmente las consultas.</summary>
public class ExternalCatalogRateLimitException : Exception
{
    public ExternalCatalogRateLimitException()
        : base("El catálogo externo limitó temporalmente las consultas.")
    {
    }
}
