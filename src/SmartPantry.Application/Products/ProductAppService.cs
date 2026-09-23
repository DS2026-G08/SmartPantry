using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace SmartPantry.Products;

public class ProductAppService : SmartPantryAppService, IProductAppService
{
    private readonly IRepository<Product, Guid> _productRepository;

    public ProductAppService(IRepository<Product, Guid> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto input)
    {
        var product = new Product(
            GuidGenerator.Create(),
            input.Name,
            input.Brand,
            input.Barcode
        );

        await _productRepository.InsertAsync(product);

        return ObjectMapper.Map<Product, ProductDto>(product);
    }

    public async Task<ProductDto> GetAsync(Guid id)
    {
        var product = await _productRepository.GetAsync(id);

        return ObjectMapper.Map<Product, ProductDto>(product);
    }
}
