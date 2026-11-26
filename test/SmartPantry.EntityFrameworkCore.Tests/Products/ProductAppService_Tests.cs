using NSubstitute;
using Shouldly;
using SmartPantry.EntityFrameworkCore;
using SmartPantry.ExternalProducts;
using Volo.Abp;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Xunit;

namespace SmartPantry.Products;

public class ProductAppService_Tests : SmartPantryEntityFrameworkCoreTestBase
{
    private readonly IProductAppService _productAppService;

    public ProductAppService_Tests()
    {
        _productAppService = GetRequiredService<IProductAppService>();
    }

    // ---------- Pruebas del TP05 ----------

    [Fact]
    public async Task Should_Create_A_Valid_Product()
    {
        var input = new CreateProductDto
        {
            Name = " Yerba Mate ",
            Brand = " Playadito ",
            Barcode = "7791234567890"
        };

        var result = await _productAppService.CreateAsync(input);

        result.Id.ShouldNotBe(Guid.Empty);
        result.Name.ShouldBe("Yerba Mate");
        result.Brand.ShouldBe("Playadito");
        result.Barcode.ShouldBe("7791234567890");
    }

    [Fact]
    public async Task Should_Get_An_Existing_Product()
    {
        var createdProduct = await _productAppService.CreateAsync(new CreateProductDto
        {
            Name = "Arroz",
            Brand = "Gallo",
            Barcode = "1234567890"
        });

        var retrievedProduct = await _productAppService.GetAsync(createdProduct.Id);

        retrievedProduct.ShouldNotBeNull();
        retrievedProduct.Id.ShouldBe(createdProduct.Id);
        retrievedProduct.Name.ShouldBe("Arroz");
        retrievedProduct.Brand.ShouldBe("Gallo");
    }

    [Fact]
    public async Task Should_Throw_Exception_When_Product_Not_Found()
    {
        var nonExistentId = Guid.NewGuid();

        await Should.ThrowAsync<EntityNotFoundException>(async () =>
        {
            await _productAppService.GetAsync(nonExistentId);
        });
    }

    // ---------- Pruebas nuevas del TP06 ----------

    [Fact]
    public async Task Should_Register_List_Update_Get_And_Delete_A_Product()
    {
        // Registrar
        var created = await _productAppService.CreateAsync(new CreateProductDto
        {
            Name = "Leche",
            Brand = "La Serenisima",
            Barcode = "7790742000001"
        });

        // Listar
        var list = await _productAppService.GetListAsync(new PagedAndSortedResultRequestDto
        {
            Sorting = "Name",
            SkipCount = 0,
            MaxResultCount = 10
        });
        list.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
        list.Items.ShouldContain(p => p.Id == created.Id);

        // Modificar
        var updated = await _productAppService.UpdateAsync(created.Id, new UpdateProductDto
        {
            Name = "  Leche Descremada  ",
            Brand = "La Serenisima",
            Barcode = "   "
        });
        updated.Name.ShouldBe("Leche Descremada");
        updated.Barcode.ShouldBeNull();

        // Consultar
        var fetched = await _productAppService.GetAsync(created.Id);
        fetched.Name.ShouldBe("Leche Descremada");
        fetched.Brand.ShouldBe("La Serenisima");

        // Eliminar
        await _productAppService.DeleteAsync(created.Id);

        // Consultar el eliminado -> no existe
        await Should.ThrowAsync<EntityNotFoundException>(async () =>
        {
            await _productAppService.GetAsync(created.Id);
        });
    }

    [Fact]
    public async Task Should_Return_A_Paged_And_Sorted_List()
    {
        await _productAppService.CreateAsync(new CreateProductDto { Name = "Cafe", Brand = "La Virginia" });
        await _productAppService.CreateAsync(new CreateProductDto { Name = "Azucar", Brand = "Ledesma" });
        await _productAppService.CreateAsync(new CreateProductDto { Name = "Birome", Brand = "Bic" });

        var page = await _productAppService.GetListAsync(new PagedAndSortedResultRequestDto
        {
            Sorting = "Name",
            SkipCount = 0,
            MaxResultCount = 2
        });

        page.TotalCount.ShouldBe(3);              // total real
        page.Items.Count.ShouldBe(2);             // sólo la página pedida
        page.Items.Select(p => p.Name).ShouldBe(new[] { "Azucar", "Birome" }); // ordenado
    }

    // ---------- Entrega final: código único y guardado de productos externos (RF-08) ----------

    private const string ExternalBarcode = "7790742363206";

    private IExternalProductCatalogClient CatalogClient => GetRequiredService<IExternalProductCatalogClient>();

    private void CatalogReturns(ExternalProductDto? product)
    {
        CatalogClient.GetByBarcodeAsync(ExternalBarcode).Returns(Task.FromResult(product));
    }

    [Fact]
    public async Task Should_Reject_A_Second_Product_With_The_Same_Barcode()
    {
        await _productAppService.CreateAsync(new CreateProductDto { Name = "Leche", Barcode = "7790742000001" });

        var ex = await Should.ThrowAsync<BusinessException>(() =>
            _productAppService.CreateAsync(new CreateProductDto { Name = "Otra leche", Barcode = " 7790742000001 " }));

        ex.Code.ShouldBe(SmartPantryDomainErrorCodes.DuplicateBarcode);
    }

    [Fact]
    public async Task Should_Reject_Updating_A_Product_To_A_Barcode_In_Use_But_Allow_Keeping_Its_Own()
    {
        await _productAppService.CreateAsync(new CreateProductDto { Name = "Leche", Barcode = "7790742000001" });
        var rice = await _productAppService.CreateAsync(new CreateProductDto { Name = "Arroz", Barcode = "7790742000002" });

        // Conservar su propio código no es un duplicado.
        var updated = await _productAppService.UpdateAsync(rice.Id,
            new UpdateProductDto { Name = "Arroz largo fino", Barcode = "7790742000002" });
        updated.Name.ShouldBe("Arroz largo fino");

        var ex = await Should.ThrowAsync<BusinessException>(() =>
            _productAppService.UpdateAsync(rice.Id, new UpdateProductDto { Name = "Arroz", Barcode = "7790742000001" }));
        ex.Code.ShouldBe(SmartPantryDomainErrorCodes.DuplicateBarcode);
    }

    [Fact]
    public async Task Should_Allow_Several_Products_Without_Barcode_Or_Brand()
    {
        await _productAppService.CreateAsync(new CreateProductDto { Name = "Pan casero" });
        var second = await _productAppService.CreateAsync(new CreateProductDto { Name = "Tomates de la huerta", Brand = " " });

        second.Brand.ShouldBeNull();
        second.Barcode.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Save_An_External_Product_With_Its_Catalog_Details()
    {
        CatalogReturns(new ExternalProductDto
        {
            Barcode = ExternalBarcode,
            Name = "Leche entera",
            Brand = "La Serenísima",
            ImageUrl = "https://images.example.org/leche.jpg",
            Categories = "Lácteos",
            Ingredients = "Leche entera, vitaminas A y D",
            AllergenTags = new() { "en:milk", "es:Contiene leche" },
            NutriScore = "unknown",
            NovaGroup = 1
        });

        var saved = await _productAppService.SaveFromExternalAsync(new SaveExternalProductDto { Barcode = ExternalBarcode });

        saved.Name.ShouldBe("Leche entera");
        saved.Brand.ShouldBe("La Serenísima");
        saved.Barcode.ShouldBe(ExternalBarcode);
        saved.Allergens.ShouldBe(new[] { "en:milk" });
        saved.NutriScore.ShouldBeNull();
        saved.NovaGroup.ShouldBe(1);

        // Persistido: se puede volver a leer con todos sus datos.
        var read = await _productAppService.GetAsync(saved.Id);
        read.Categories.ShouldBe("Lácteos");
        read.Allergens.ShouldBe(new[] { "en:milk" });
    }

    [Fact]
    public async Task Should_Not_Duplicate_When_Saving_The_Same_External_Product_Twice()
    {
        CatalogReturns(new ExternalProductDto { Barcode = ExternalBarcode, Name = "Leche entera" });

        var first = await _productAppService.SaveFromExternalAsync(new SaveExternalProductDto { Barcode = ExternalBarcode });
        var second = await _productAppService.SaveFromExternalAsync(new SaveExternalProductDto { Barcode = ExternalBarcode });

        second.Id.ShouldBe(first.Id);
        (await _productAppService.GetListAsync(new PagedAndSortedResultRequestDto())).TotalCount.ShouldBe(1);
        // La segunda vez se reutiliza el producto guardado: no se vuelve a consultar al proveedor.
        await CatalogClient.Received(1).GetByBarcodeAsync(ExternalBarcode);
    }

    [Fact]
    public async Task Should_Use_The_Name_Written_By_The_User_Only_When_The_Catalog_Has_None()
    {
        CatalogReturns(new ExternalProductDto { Barcode = ExternalBarcode, Name = null });

        var ex = await Should.ThrowAsync<BusinessException>(() =>
            _productAppService.SaveFromExternalAsync(new SaveExternalProductDto { Barcode = ExternalBarcode }));
        ex.Code.ShouldBe(SmartPantryDomainErrorCodes.ExternalProductNameRequired);

        var saved = await _productAppService.SaveFromExternalAsync(
            new SaveExternalProductDto { Barcode = ExternalBarcode, Name = " Leche de la góndola " });
        saved.Name.ShouldBe("Leche de la góndola");
        saved.Brand.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Fail_With_A_Clear_Error_When_The_External_Product_Cannot_Be_Obtained()
    {
        CatalogReturns(null);
        var notFound = await Should.ThrowAsync<BusinessException>(() =>
            _productAppService.SaveFromExternalAsync(new SaveExternalProductDto { Barcode = ExternalBarcode }));
        notFound.Code.ShouldBe(SmartPantryDomainErrorCodes.ExternalProductNotFound);

        CatalogClient.GetByBarcodeAsync(ExternalBarcode).Returns(
            Task.FromException<ExternalProductDto?>(new ExternalCatalogUnavailableException("caído")));
        var unavailable = await Should.ThrowAsync<BusinessException>(() =>
            _productAppService.SaveFromExternalAsync(new SaveExternalProductDto { Barcode = ExternalBarcode }));
        unavailable.Code.ShouldBe(SmartPantryDomainErrorCodes.ExternalCatalogUnavailable);

        (await _productAppService.GetListAsync(new PagedAndSortedResultRequestDto())).TotalCount.ShouldBe(0);
    }
}