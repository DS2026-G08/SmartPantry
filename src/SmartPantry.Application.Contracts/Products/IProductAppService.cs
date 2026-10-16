using System;
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
}