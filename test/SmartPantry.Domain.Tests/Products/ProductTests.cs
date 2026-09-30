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

        // Act: nombre válido pero marca vacía (inválida)
        Should.Throw<ArgumentException>(() =>
            product.Update("Nombre Nuevo", "   ", "999"));

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
}
