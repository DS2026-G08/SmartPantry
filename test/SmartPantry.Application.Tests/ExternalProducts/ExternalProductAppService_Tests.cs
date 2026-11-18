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

    // ---------- Entrega final: detalle (RF-07) y búsqueda por texto (RF-06) ----------

    [Fact]
    public async Task Should_Return_The_Product_Detail_And_Normalize_Provider_Values()
    {
        _catalogClient.GetByBarcodeAsync(Barcode).Returns(Task.FromResult<ExternalProductDto?>(
            new ExternalProductDto
            {
                Barcode = Barcode,
                Name = "Nutella",
                Categories = "Untables de cacao y avellanas",
                Ingredients = "azúcar, aceite de palma, avellanas, leche descremada en polvo",
                AllergenTags = new() { "en:nuts", "es:Contiene leche", "en:milk" },
                NutriScore = "E",
                NovaGroup = 4
            }));

        var result = await _appService.GetByBarcodeAsync(new ExternalProductLookupInputDto { Barcode = Barcode });

        result.Categories.ShouldBe("Untables de cacao y avellanas");
        result.Ingredients.ShouldStartWith("azúcar");
        result.Allergens.ShouldBe(new[] { "en:nuts", "en:milk" }); // el texto libre se descarta
        result.NutriScore.ShouldBe("e");
        result.NovaGroup.ShouldBe(4);
    }

    [Fact]
    public async Task Should_Report_Unknown_NutriScore_And_Nova_As_Missing()
    {
        _catalogClient.GetByBarcodeAsync(Barcode).Returns(Task.FromResult<ExternalProductDto?>(
            new ExternalProductDto { Barcode = Barcode, Name = "Leche", NutriScore = "unknown", NovaGroup = 0 }));

        var result = await _appService.GetByBarcodeAsync(new ExternalProductLookupInputDto { Barcode = Barcode });

        result.NutriScore.ShouldBeNull();
        result.NovaGroup.ShouldBeNull();
        result.Allergens.ShouldBeEmpty();
        result.Categories.ShouldBeNull();
        result.Ingredients.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Search_By_Text_And_Translate_Paging()
    {
        _catalogClient.SearchAsync("leche", 3, 10).Returns(Task.FromResult(new ExternalProductSearchPage
        {
            TotalCount = 25,
            Items = new()
            {
                new ExternalProductDto { Barcode = "7790742363206", Name = "Leche entera", Brand = "La Serenísima" },
                new ExternalProductDto { Barcode = "7790742000001", Name = "  ", Brand = null }
            }
        }));

        var result = await _appService.GetListAsync(new ExternalProductSearchInputDto
        {
            Text = " leche ",
            SkipCount = 20, // tercera página de 10
            MaxResultCount = 10
        });

        result.Status.ShouldBe(ExternalProductLookupStatus.Found);
        result.TotalCount.ShouldBe(25);
        result.Items.Count.ShouldBe(2);
        result.Items[0].Name.ShouldBe("Leche entera");
        result.Items[1].Name.ShouldBeNull(); // RF-09: sin nombre informado
        await _catalogClient.Received(1).SearchAsync("leche", 3, 10);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Search_Has_No_Results()
    {
        _catalogClient.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(Task.FromResult(new ExternalProductSearchPage()));

        var result = await _appService.GetListAsync(new ExternalProductSearchInputDto { Text = "xyzxyz" });

        result.Status.ShouldBe(ExternalProductLookupStatus.NotFound);
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Return_RateLimited_And_ServiceUnavailable_When_Search_Fails()
    {
        _catalogClient.SearchAsync("leche", Arg.Any<int>(), Arg.Any<int>()).Returns(
            Task.FromException<ExternalProductSearchPage>(new ExternalCatalogRateLimitException()));
        _catalogClient.SearchAsync("arroz", Arg.Any<int>(), Arg.Any<int>()).Returns(
            Task.FromException<ExternalProductSearchPage>(new ExternalCatalogUnavailableException("caído")));

        var limited = await _appService.GetListAsync(new ExternalProductSearchInputDto { Text = "leche" });
        var unavailable = await _appService.GetListAsync(new ExternalProductSearchInputDto { Text = "arroz" });

        limited.Status.ShouldBe(ExternalProductLookupStatus.RateLimited);
        limited.Items.ShouldBeEmpty();
        unavailable.Status.ShouldBe(ExternalProductLookupStatus.ServiceUnavailable);
        unavailable.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Cap_Search_Results_And_Not_Call_Provider_Beyond_The_Limit()
    {
        _catalogClient.SearchAsync(Arg.Any<string>(), 1, 10).Returns(Task.FromResult(new ExternalProductSearchPage
        {
            TotalCount = 10000,
            Items = new() { new ExternalProductDto { Barcode = "7790742363206", Name = "Leche" } }
        }));

        var first = await _appService.GetListAsync(new ExternalProductSearchInputDto { Text = "leche" });
        var beyond = await _appService.GetListAsync(new ExternalProductSearchInputDto
        {
            Text = "leche",
            SkipCount = ExternalProductAppService.MaxNavigableResults,
            MaxResultCount = 10
        });

        first.TotalCount.ShouldBe(ExternalProductAppService.MaxNavigableResults);
        beyond.Items.ShouldBeEmpty();
        await _catalogClient.Received(1).SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>());
    }

    [Theory]
    [InlineData("leche", 0, 10, true)]
    [InlineData("l", 0, 10, false)]    // texto muy corto
    [InlineData("leche", 5, 10, false)] // el salto no es múltiplo del tamaño de página
    [InlineData("leche", 0, 51, false)] // página demasiado grande
    public void Should_Validate_Search_Input(string text, int skipCount, int maxResultCount, bool expectedValid)
    {
        var input = new ExternalProductSearchInputDto { Text = text, SkipCount = skipCount, MaxResultCount = maxResultCount };

        var isValid = Validator.TryValidateObject(
            input,
            new ValidationContext(input),
            new List<ValidationResult>(),
            validateAllProperties: true);

        isValid.ShouldBe(expectedValid);
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
