namespace SmartPantry.Products;

public static class ProductConsts
{
    public const int MaxNameLength = 128;
    public const int MaxBrandLength = 64;
    public const int MaxBarcodeLength = 32;

    // Datos del catálogo externo (RF-07). Los textos más largos se recortan al guardarlos.
    public const int MaxImageUrlLength = 512;
    public const int MaxCategoriesLength = 512;
    public const int MaxIngredientsLength = 4000;
    public const int MaxAllergensLength = 512;
    public const int MinNovaGroup = 1;
    public const int MaxNovaGroup = 4;

    /// <summary>Notas válidas del Nutri-Score. Cualquier otro valor ("unknown", "not-applicable") se guarda como null.</summary>
    public static readonly string[] NutriScoreGrades = { "a", "b", "c", "d", "e" };
}
