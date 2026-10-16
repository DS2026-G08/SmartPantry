using System;

namespace SmartPantry.ExternalProducts;

/// <summary>El catálogo externo no respondió, tardó demasiado o devolvió una respuesta inválida.</summary>
public class ExternalCatalogUnavailableException : Exception
{
    public ExternalCatalogUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
