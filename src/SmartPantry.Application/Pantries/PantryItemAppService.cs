using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SmartPantry.Products;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace SmartPantry.Pantries;

/// <summary>
/// RF-10 y RF-11: ítems de la despensa del usuario autenticado.
/// El propietario se obtiene siempre de ICurrentUser, nunca de la petición. Un ítem de otro
/// usuario se trata igual que uno inexistente (404), así la API no revela que existe.
/// </summary>
[Authorize]
public class PantryItemAppService : SmartPantryAppService, IPantryItemAppService
{
    private readonly IRepository<Pantry, Guid> _pantryRepository;
    private readonly IRepository<Product, Guid> _productRepository;

    public PantryItemAppService(
        IRepository<Pantry, Guid> pantryRepository,
        IRepository<Product, Guid> productRepository)
    {
        _pantryRepository = pantryRepository;
        _productRepository = productRepository;
    }

    public async Task<PagedResultDto<PantryItemDto>> GetListAsync(GetPantryItemListDto input)
    {
        var ownerId = CurrentUser.GetId();
        var pantries = await _pantryRepository.GetQueryableAsync();
        var products = await _productRepository.GetQueryableAsync();

        var query =
            from pantry in pantries
            where pantry.OwnerId == ownerId
            from item in pantry.Items
            where input.IncludeConsumed || item.Status == PantryItemStatus.Active
            join product in products on item.ProductId equals product.Id
            select new { item, product };

        var totalCount = await AsyncExecuter.CountAsync(query);

        // Primero los activos; dentro de cada estado, los que vencen antes y al final los sin fecha.
        var page = await AsyncExecuter.ToListAsync(query
            .OrderBy(x => x.item.Status)
            .ThenBy(x => x.item.ExpirationDate == null)
            .ThenBy(x => x.item.ExpirationDate)
            .ThenBy(x => x.product.Name)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount));

        return new PagedResultDto<PantryItemDto>(
            totalCount,
            page.Select(x => MapToDto(x.item, x.product)).ToList());
    }

    public async Task<PantryItemDto> GetAsync(Guid id)
    {
        var pantry = await GetOwnPantryOfItemAsync(id);
        return await MapToDtoAsync(pantry.GetItem(id));
    }

    public async Task<PantryItemDto> CreateAsync(CreatePantryItemDto input)
    {
        var ownerId = CurrentUser.GetId();
        var product = await _productRepository.GetAsync(input.ProductId);

        // El ítem va a la primera despensa del usuario; si todavía no tiene, se crea.
        var ownPantries = (await _pantryRepository.WithDetailsAsync())
            .Where(p => p.OwnerId == ownerId)
            .OrderBy(p => p.Name);
        var pantry = await AsyncExecuter.FirstOrDefaultAsync(ownPantries);
        var isNewPantry = pantry is null;
        pantry ??= new Pantry(GuidGenerator.Create(), ownerId, PantryConsts.DefaultName);

        var item = pantry.AddItem(GuidGenerator.Create(), product.Id, input.ExpirationDate, input.Quantity, input.Unit);

        if (isNewPantry)
        {
            await _pantryRepository.InsertAsync(pantry, autoSave: true);
        }
        else
        {
            await _pantryRepository.UpdateAsync(pantry, autoSave: true);
        }

        return MapToDto(item, product);
    }

    public async Task<PantryItemDto> UpdateAsync(Guid id, UpdatePantryItemDto input)
    {
        var pantry = await GetOwnPantryOfItemAsync(id);
        pantry.UpdateItem(id, input.ExpirationDate, input.Quantity, input.Unit);
        await _pantryRepository.UpdateAsync(pantry, autoSave: true);

        return await MapToDtoAsync(pantry.GetItem(id));
    }

    public async Task<PantryItemDto> ConsumeAsync(Guid id)
    {
        var pantry = await GetOwnPantryOfItemAsync(id);
        pantry.ConsumeItem(id);
        await _pantryRepository.UpdateAsync(pantry, autoSave: true);

        return await MapToDtoAsync(pantry.GetItem(id));
    }

    public async Task DeleteAsync(Guid id)
    {
        var pantry = await GetOwnPantryOfItemAsync(id);
        pantry.RemoveItem(id);
        await _pantryRepository.UpdateAsync(pantry, autoSave: true);
    }

    // Busca la despensa del usuario actual que contiene el ítem. Si el ítem no existe o es
    // de otro usuario, el resultado es el mismo: EntityNotFoundException, que ABP traduce a 404.
    private async Task<Pantry> GetOwnPantryOfItemAsync(Guid itemId)
    {
        var ownerId = CurrentUser.GetId();
        var pantry = await _pantryRepository.FindAsync(p =>
            p.OwnerId == ownerId && p.Items.Any(i => i.Id == itemId));

        return pantry ?? throw new EntityNotFoundException(typeof(PantryItem), itemId);
    }

    private async Task<PantryItemDto> MapToDtoAsync(PantryItem item)
    {
        var product = await _productRepository.GetAsync(item.ProductId);
        return MapToDto(item, product);
    }

    private static PantryItemDto MapToDto(PantryItem item, Product product)
    {
        return new PantryItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = product.Name,
            ProductBrand = product.Brand,
            Status = item.Status,
            ExpirationDate = item.ExpirationDate,
            Quantity = item.Quantity,
            Unit = item.Unit
        };
    }
}
