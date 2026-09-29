using Shouldly;
using SmartPantry.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
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

    [Fact]
    public async Task Should_Create_A_Valid_Product()
    {
        // Arrange
        var input = new CreateProductDto
        {
            Name = " Yerba Mate ",
            Brand = " Playadito ",
            Barcode = "7791234567890"
        };

        // Act
        var result = await _productAppService.CreateAsync(input);

        // Assert
        result.Id.ShouldNotBe(Guid.Empty);
        result.Name.ShouldBe("Yerba Mate");
        result.Brand.ShouldBe("Playadito");
        result.Barcode.ShouldBe("7791234567890");
    }

    [Fact]
    public async Task Should_Get_An_Existing_Product()
    {
        // Arrange
        var createdProduct = await _productAppService.CreateAsync(new CreateProductDto
        {
            Name = "Arroz",
            Brand = "Gallo",
            Barcode = "1234567890"
        });

        // Act
        var retrievedProduct = await _productAppService.GetAsync(createdProduct.Id);

        // Assert
        retrievedProduct.ShouldNotBeNull();
        retrievedProduct.Id.ShouldBe(createdProduct.Id);
        retrievedProduct.Name.ShouldBe("Arroz");
        retrievedProduct.Brand.ShouldBe("Gallo");
    }

    [Fact]
    public async Task Should_Throw_Exception_When_Product_Not_Found()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Should.ThrowAsync<EntityNotFoundException>(async () =>
        {
            await _productAppService.GetAsync(nonExistentId);
        });
    }
}
