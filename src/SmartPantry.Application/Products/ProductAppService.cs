using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SmartPantry.ExternalProducts;
using SmartPantry.Localization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace SmartPantry.Products;

// RF-08 (guardado interno). TP09: requiere un usuario autenticado; cualquier usuario común puede consultar y guardar.
[Authorize]
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
    private readonly IExternalProductCatalogClient _catalogClient;

    public ProductAppService(
        IRepository<Product, Guid> repository,
        IExternalProductCatalogClient catalogClient)
        : base(repository)
    {
        _catalogClient = catalogClient;
        LocalizationResource = typeof(SmartPantryResource);
    }

    public async Task<ProductDto> SaveFromExternalAsync(SaveExternalProductDto input)
    {
        var barcode = input.Barcode.Trim();

        // Idempotente: si el código ya está guardado, se reutiliza ese producto (regla de negocio 4).
        var existing = await Repository.FirstOrDefaultAsync(p => p.Barcode == barcode);
        if (existing != null)
        {
            return await MapToGetOutputDtoAsync(existing);
        }

        ExternalProductDto? external;
        try
        {
            external = await _catalogClient.GetByBarcodeAsync(barcode);
        }
        catch (Exception ex) when (ex is ExternalCatalogRateLimitException or ExternalCatalogUnavailableException)
        {
            throw new BusinessException(SmartPantryDomainErrorCodes.ExternalCatalogUnavailable);
        }

        if (external == null)
        {
            throw new BusinessException(SmartPantryDomainErrorCodes.ExternalProductNotFound)
                .WithData("Barcode", barcode);
        }

        // RF-09: si el catálogo no informa el nombre, se usa el que escribió el usuario; nunca se inventa.
        var name = FirstWithValue(external.Name, input.Name)
            ?? throw new BusinessException(SmartPantryDomainErrorCodes.ExternalProductNameRequired);

        // Son datos de terceros: un texto más largo que la columna se recorta en vez de rechazar el producto.
        var product = new Product(
            GuidGenerator.Create(),
            CatalogValues.NormalizeText(name, ProductConsts.MaxNameLength)!,
            CatalogValues.NormalizeText(external.Brand, ProductConsts.MaxBrandLength),
            barcode);
        product.SetCatalogDetails(
            external.ImageUrl,
            external.Categories,
            external.Ingredients,
            external.AllergenTags,
            external.NutriScore,
            external.NovaGroup);

        await Repository.InsertAsync(product, autoSave: true);
        return await MapToGetOutputDtoAsync(product);
    }

    // CREAR (adaptado): en vez de mapear el DTO a la entidad automáticamente,
    // usamos el constructor de Product, que aplica las reglas del dominio.
    protected override async Task<Product> MapToEntityAsync(CreateProductDto createInput)
    {
        var product = new Product(
            GuidGenerator.Create(),
            createInput.Name,
            createInput.Brand,
            createInput.Barcode);

        await CheckBarcodeIsFreeAsync(product.Barcode, product.Id);
        return product;
    }

    // MODIFICAR (adaptado): en vez de copiar propiedades una por una,
    // delegamos en Product.Update, que valida todo antes de cambiar el estado.
    protected override async Task MapToEntityAsync(UpdateProductDto updateInput, Product entity)
    {
        entity.Update(updateInput.Name, updateInput.Brand, updateInput.Barcode);
        await CheckBarcodeIsFreeAsync(entity.Barcode, entity.Id);
    }

    // LISTAR (adaptado): orden por defecto cuando el cliente no manda "Sorting".
    // Product no tiene CreationTime, así que ABP no sabe por qué ordenar si no se lo decimos.
    protected override IQueryable<Product> ApplyDefaultSorting(IQueryable<Product> query)
    {
        return query.OrderBy(p => p.Name);
    }

    // Los alérgenos se guardan como texto y se exponen como lista.
    protected override Task<ProductDto> MapToGetOutputDtoAsync(Product entity)
    {
        return Task.FromResult(ProductMapping.ToDto(entity));
    }

    protected override Task<ProductDto> MapToGetListOutputDtoAsync(Product entity)
    {
        return Task.FromResult(ProductMapping.ToDto(entity));
    }

    // Regla de negocio 4: el índice único de la base es la última garantía; esta validación
    // da un mensaje comprensible en lugar de un error de base de datos.
    private async Task CheckBarcodeIsFreeAsync(string? barcode, Guid productId)
    {
        if (barcode == null)
        {
            return;
        }

        if (await Repository.AnyAsync(p => p.Barcode == barcode && p.Id != productId))
        {
            throw new BusinessException(SmartPantryDomainErrorCodes.DuplicateBarcode)
                .WithData("Barcode", barcode);
        }
    }

    private static string? FirstWithValue(params string?[] values)
    {
        return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
    }
}
