using System;
using SmartPantry.Preferences;
using Volo.Abp.Application.Dtos;

namespace SmartPantry.Pantries;

public class PantryItemDto : EntityDto<Guid>
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? ProductBrand { get; set; }
    public PantryItemStatus Status { get; set; }

    // Los datos opcionales quedan en null cuando no se informan (RF-09): la UI no inventa valores.
    public DateOnly? ExpirationDate { get; set; }
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }

    /// <summary>
    /// RF-12: advertencia activa del ítem según las preferencias (Match o NotVerifiable). Es null si no
    /// hay advertencia o si el usuario apagó "Mostrar alertas".
    /// </summary>
    public PreferenceEvaluationDto? PreferenceWarning { get; set; }
}
