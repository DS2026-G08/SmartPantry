using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SmartPantry.Localization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace SmartPantry.Products;

// Decisión temporal del TP05: acceso anónimo para probar en Swagger sin login.
[AllowAnonymous]
public class ProductAppService :
    CrudAppService<
        Product,
        ProductDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateProductDto,
        UpdateProductDto>,
    IProductAppService
{
    public ProductAppService(IRepository<Product, Guid> repository)
        : base(repository)
    {
        LocalizationResource = typeof(SmartPantryResource);
    }

    // CREAR (adaptado): en vez de mapear el DTO a la entidad automáticamente,
    // usamos el constructor de Product, que aplica las reglas del dominio.
    protected override Task<Product> MapToEntityAsync(CreateProductDto createInput)
    {
        var product = new Product(
            GuidGenerator.Create(),
            createInput.Name,
            createInput.Brand,
            createInput.Barcode);

        return Task.FromResult(product);
    }

    // MODIFICAR (adaptado): en vez de copiar propiedades una por una,
    // delegamos en Product.Update, que valida todo antes de cambiar el estado.
    protected override Task MapToEntityAsync(UpdateProductDto updateInput, Product entity)
    {
        entity.Update(updateInput.Name, updateInput.Brand, updateInput.Barcode);
        return Task.CompletedTask;
    }

    // LISTAR (adaptado): orden por defecto cuando el cliente no manda "Sorting".
    // Product no tiene CreationTime, así que ABP no sabe por qué ordenar si no se lo decimos.
    protected override IQueryable<Product> ApplyDefaultSorting(IQueryable<Product> query)
    {
        return query.OrderBy(p => p.Name);
    }
}