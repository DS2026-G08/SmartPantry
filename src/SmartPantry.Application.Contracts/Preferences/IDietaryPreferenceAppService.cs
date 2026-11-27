using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SmartPantry.Preferences;

/// <summary>RF-03: preferencias alimentarias del usuario autenticado.</summary>
public interface IDietaryPreferenceAppService : IApplicationService
{
    /// <summary>Preferencias propias; vacías (con ShowAlerts encendido) si nunca se guardaron.</summary>
    Task<DietaryPreferenceDto> GetAsync();

    /// <summary>Crea o reemplaza las preferencias propias y recalcula las advertencias de la despensa.</summary>
    Task<DietaryPreferenceDto> UpdateAsync(UpdateDietaryPreferenceDto input);
}
