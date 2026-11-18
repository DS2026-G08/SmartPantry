using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SmartPantry.Products;

public interface IProductAppService :
    ICrudAppService<
        ProductDto,                      // lo que devuelve (nunca la entidad)
        Guid,                            // tipo del Id
        PagedAndSortedResultRequestDto,  // entrada de la lista: paginado + orden
        CreateProductDto,                // entrada para crear
        UpdateProductDto>                // entrada para modificar
{
    /// <summary>
    /// RF-08: guarda un producto del catálogo externo. Si ya hay un producto con ese código,
    /// lo devuelve sin crear otro (la operación se puede repetir sin duplicar).
    /// </summary>
    Task<ProductDto> SaveFromExternalAsync(SaveExternalProductDto input);
}
