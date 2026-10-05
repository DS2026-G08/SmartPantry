using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Xunit;

namespace SmartPantry.ExternalProducts;

// Pruebas unitarias del AppService: el catálogo externo se reemplaza por un mock,
// así que no dependen de Internet ni de la base de datos.
public class ExternalProductAppService_Tests
{
    private const string Barcode = "3017620422003";

    private readonly IExternalProductCatalogClient _catalogClient;
    private readonly ExternalProductAppService _appService;

    public ExternalProductAppService_Tests()
    {
        _catalogClient = Substitute.For<IExternalProductCatalogClient>();
        _appService = new ExternalProductAppService(_catalogClient);
    }

    [Fact]
    public async Task Should_Return_Found_When_Product_Exists()
    {
        _catalogClient.GetByBarcodeAsync(Barcode).Returns(Task.FromResult<ExternalProductDto?>(
            new ExternalProductDto
            {
                Barcode = Barcode,
                Name = "Nutella",
                Brand = "Ferrero",
                ImageUrl = "https://images.example.org/nutella.jpg"
            }));

        var result = await _appService.GetByBarcodeAsync(new ExternalProductLookupInputDto { Barcode = Barcode });

        result.Status.ShouldBe(ExternalProductLookupStatus.Found);
        result.Barcode.ShouldBe(Barcode);
        result.Name.ShouldBe("Nutella");
        result.Brand.ShouldBe("Ferrero");
        result.ImageUrl.ShouldBe("https://images.example.org/nutella.jpg");
        await _catalogClient.Received(1).GetByBarcodeAsync(Barcode);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        _catalogClient.GetByBarcodeAsync("0000000000000")
            .Returns(Task.FromResult<ExternalProductDto?>(null));

        var result = await _appService.GetByBarcodeAsync(new ExternalProductLookupInputDto { Barcode = "0000000000000" });

        result.Status.ShouldBe(ExternalProductLookupStatus.NotFound);
        result.Barcode.ShouldBe("0000000000000");
        result.Name.ShouldBeNull();
        result.Brand.ShouldBeNull();
        result.ImageUrl.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Keep_Missing_Data_As_Null_Without_Inventing_Values()
    {
        // El proveedor encontró el producto, pero no informa marca ni imagen (RF-09).
        _catalogClient.GetByBarcodeAsync(Barcode).Returns(Task.FromResult<ExternalProductDto?>(
            new ExternalProductDto
            {
                Barcode = Barcode,
                Name = "Producto sin marca",
                Brand = "   ",
                ImageUrl = null
            }));

        var result = await _appService.GetByBarcodeAsync(new ExternalProductLookupInputDto { Barcode = Barcode });

        result.Status.ShouldBe(ExternalProductLookupStatus.Found);
        result.Name.ShouldBe("Producto sin marca");
        result.Brand.ShouldBeNull();
        result.ImageUrl.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Return_RateLimited_When_Provider_Limits_Requests()
    {
        _catalogClient.GetByBarcodeAsync(Barcode).Returns(
            Task.FromException<ExternalProductDto?>(new ExternalCatalogRateLimitException()));

        var result = await _appService.GetByBarcodeAsync(new ExternalProductLookupInputDto { Barcode = Barcode });

        result.Status.ShouldBe(ExternalProductLookupStatus.RateLimited);
        result.Barcode.ShouldBe(Barcode);
        result.Name.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Return_ServiceUnavailable_When_Provider_Is_Down()
    {
        _catalogClient.GetByBarcodeAsync(Barcode).Returns(
            Task.FromException<ExternalProductDto?>(
                new ExternalCatalogUnavailableException("No se pudo conectar con Open Food Facts.")));

        var result = await _appService.GetByBarcodeAsync(new ExternalProductLookupInputDto { Barcode = Barcode });

        result.Status.ShouldBe(ExternalProductLookupStatus.ServiceUnavailable);
        result.Barcode.ShouldBe(Barcode);
        result.Name.ShouldBeNull();
    }

    // Validación del DTO de entrada (la misma que ABP aplica antes de llegar al AppService).
    [Theory]
    [InlineData("3017620422003", true)]   // EAN-13
    [InlineData("12345678", true)]        // EAN-8
    [InlineData("1234567", false)]        // muy corto
    [InlineData("123456789012345", false)] // muy largo
    [InlineData("30176204A2003", false)]  // con letras
    [InlineData("", false)]               // vacío
    public void Should_Validate_Barcode_Format(string barcode, bool expectedValid)
    {
        var input = new ExternalProductLookupInputDto { Barcode = barcode };

        var isValid = Validator.TryValidateObject(
            input,
            new ValidationContext(input),
            new List<ValidationResult>(),
            validateAllProperties: true);

        isValid.ShouldBe(expectedValid);
    }
}
