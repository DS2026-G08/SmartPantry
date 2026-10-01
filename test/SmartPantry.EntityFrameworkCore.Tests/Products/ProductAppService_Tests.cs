using Shouldly;
using SmartPantry.EntityFrameworkCore;
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
}