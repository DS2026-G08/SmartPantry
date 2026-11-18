using System;
using Shouldly;
using Xunit;

namespace SmartPantry.Products;

public class ProductTests
{
    [Fact]
    public void Update_Valido_Aplica_Normalizaciones()
    {
        // Arrange
        var product = new Product(Guid.NewGuid(), "Arroz", "Gallo", "1234567890");

        // Act
        product.Update("  Arroz Largo Fino  ", "  Gallo Oro  ", "   ");

        // Assert
        product.Name.ShouldBe("Arroz Largo Fino");   // se recortan espacios
        product.Brand.ShouldBe("Gallo Oro");         // se recortan espacios
        product.Barcode.ShouldBeNull();              // código vacío se guarda como null
    }

    [Fact]
    public void Update_Invalido_Es_Rechazado_Y_No_Deja_Cambios_Parciales()
    {
        // Arrange
        var product = new Product(Guid.NewGuid(), "Arroz", "Gallo", "1234567890");

        // Act: marca y código válidos pero nombre vacío (inválido)
        Should.Throw<ArgumentException>(() =>
            product.Update("   ", "Otra Marca", "999"));

        // Assert: la entidad quedó exactamente como estaba
        product.Name.ShouldBe("Arroz");
        product.Brand.ShouldBe("Gallo");
        product.Barcode.ShouldBe("1234567890");
    }

    [Fact]
    public void Update_Con_Nombre_Demasiado_Largo_Es_Rechazado()
    {
        var product = new Product(Guid.NewGuid(), "Arroz", "Gallo");
        var nombreLargo = new string('a', ProductConsts.MaxNameLength + 1);

        Should.Throw<ArgumentException>(() =>
            product.Update(nombreLargo, "Gallo", null));

        product.Name.ShouldBe("Arroz");
    }

    // ---------- Entrega final: catálogo (RF-07, RF-09) ----------

    [Fact]
    public void Marca_Vacia_Se_Guarda_Como_Null_Sin_Inventar_Un_Valor()
    {
        var product = new Product(Guid.NewGuid(), "Producto sin marca", "   ", "7790742363206");

        product.Brand.ShouldBeNull();
    }

    [Fact]
    public void SetCatalogDetails_Guarda_Los_Datos_Validos_Del_Catalogo()
    {
        var product = new Product(Guid.NewGuid(), "Leche", "La Serenisima", "7790742363206");

        product.SetCatalogDetails(
            " https://images.example.org/leche.jpg ",
            "Lácteos, Leches",
            "Leche entera, vitaminas A y D",
            new[] { "en:milk", "EN:Gluten" },
            "B",
            1);

        product.ImageUrl.ShouldBe("https://images.example.org/leche.jpg");
        product.Categories.ShouldBe("Lácteos, Leches");
        product.Ingredients.ShouldBe("Leche entera, vitaminas A y D");
        product.GetAllergenTags().ShouldBe(new[] { "en:milk", "en:gluten" });
        product.NutriScore.ShouldBe("b");
        product.NovaGroup.ShouldBe(1);
    }

    [Fact]
    public void SetCatalogDetails_Informa_Como_Ausente_Lo_Que_No_Se_Puede_Interpretar()
    {
        var product = new Product(Guid.NewGuid(), "Leche", "La Serenisima", "7790742363206");

        // Así llegan algunos datos reales de Open Food Facts.
        product.SetCatalogDetails(
            "   ",
            null,
            "",
            new[] { "es:Contiene leche", "  ", "en:milk", "en:milk" },
            "unknown",
            7);

        product.ImageUrl.ShouldBeNull();
        product.Categories.ShouldBeNull();
        product.Ingredients.ShouldBeNull();
        product.GetAllergenTags().ShouldBe(new[] { "en:milk" }); // sólo etiquetas canónicas, sin repetir
        product.NutriScore.ShouldBeNull();                       // "unknown" no es una nota
        product.NovaGroup.ShouldBeNull();                        // fuera del rango 1-4
    }

    [Fact]
    public void SetCatalogDetails_Recorta_Los_Textos_Largos_En_Vez_De_Rechazar_El_Producto()
    {
        var product = new Product(Guid.NewGuid(), "Galletitas", null, "7790040000000");
        var ingredientesLargos = new string('a', ProductConsts.MaxIngredientsLength + 50);

        product.SetCatalogDetails(null, null, ingredientesLargos, null, null, null);

        product.Ingredients!.Length.ShouldBe(ProductConsts.MaxIngredientsLength);
        product.GetAllergenTags().ShouldBeEmpty();
    }
}
