using Volo.Abp.Application.Dtos;

namespace SmartPantry.Pantries;

public class GetPantryItemListDto : PagedResultRequestDto
{
    // Regla de negocio 6: los consumidos se conservan como historial y se muestran sólo si se piden.
    public bool IncludeConsumed { get; set; }
}
