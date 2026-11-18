using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SmartPantry.Pantries;

/// <summary>
/// Ítems de la despensa del usuario autenticado (RF-10, RF-11). Un ítem de otro usuario
/// se trata como inexistente: todas las operaciones responden 404.
/// </summary>
public interface IPantryItemAppService : IApplicationService
{
    Task<PagedResultDto<PantryItemDto>> GetListAsync(GetPantryItemListDto input);

    Task<PantryItemDto> GetAsync(Guid id);

    Task<PantryItemDto> CreateAsync(CreatePantryItemDto input);

    Task<PantryItemDto> UpdateAsync(Guid id, UpdatePantryItemDto input);

    Task<PantryItemDto> ConsumeAsync(Guid id);

    Task DeleteAsync(Guid id);
}
