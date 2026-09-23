using System;
using Volo.Abp.Application.Dtos;

namespace SmartPantry.Products;

public class ProductDto : EntityDto<Guid>
{
    public string Name { get; set; } = null!;
    public string Brand { get; set; } = null!;
    public string? Barcode { get; set; }
}
