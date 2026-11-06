using System;
using Volo.Abp.Application.Dtos;

namespace SmartPantry.Pantries;

public class PantryItemDto : EntityDto<Guid>
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string ProductBrand { get; set; } = null!;
    public PantryItemStatus Status { get; set; }

    // Los datos opcionales quedan en null cuando no se informan (RF-09): la UI no inventa valores.
    public DateOnly? ExpirationDate { get; set; }
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
}
