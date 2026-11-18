namespace SmartPantry.Pantries;

public static class PantryItemConsts
{
    public const int MaxUnitLength = 16;

    // Cantidad con hasta 3 decimales (por ejemplo 0,750 kg).
    public const int QuantityPrecision = 18;
    public const int QuantityScale = 3;
    public const double MaxQuantity = 999999;
}
