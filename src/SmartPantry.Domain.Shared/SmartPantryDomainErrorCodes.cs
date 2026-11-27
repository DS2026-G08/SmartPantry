namespace SmartPantry;

public static class SmartPantryDomainErrorCodes
{
    // Las claves de localización usan el mismo texto que el código (ver es.json / en.json).

    /// <summary>Ya existe un producto con ese código de barras (regla de negocio 4).</summary>
    public const string DuplicateBarcode = "SmartPantry:Products:DuplicateBarcode";

    /// <summary>El catálogo externo no conoce el código que se quiso guardar.</summary>
    public const string ExternalProductNotFound = "SmartPantry:Products:ExternalProductNotFound";

    /// <summary>El catálogo externo no respondió o limitó las consultas al intentar guardar.</summary>
    public const string ExternalCatalogUnavailable = "SmartPantry:Products:ExternalCatalogUnavailable";

    /// <summary>El producto externo no informa nombre y el usuario tampoco lo indicó.</summary>
    public const string ExternalProductNameRequired = "SmartPantry:Products:ExternalProductNameRequired";

    /// <summary>La etiqueta no está entre los 14 alérgenos que se pueden vigilar.</summary>
    public const string UnknownAllergen = "SmartPantry:Preferences:UnknownAllergen";

    /// <summary>Palabra de ingrediente vacía, muy corta o muy larga.</summary>
    public const string InvalidIngredient = "SmartPantry:Preferences:InvalidIngredient";

    /// <summary>La misma palabra de ingrediente aparece dos veces (sin distinguir mayúsculas ni acentos).</summary>
    public const string DuplicateIngredient = "SmartPantry:Preferences:DuplicateIngredient";

    /// <summary>Se superó la cantidad máxima de palabras de ingredientes.</summary>
    public const string TooManyIngredients = "SmartPantry:Preferences:TooManyIngredients";
}
