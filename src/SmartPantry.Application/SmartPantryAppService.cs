using SmartPantry.Localization;
using Volo.Abp.Application.Services;
using Microsoft.AspNetCore.Authorization;

namespace SmartPantry;

/* Inherit your application services from this class.
 */
// Decisión técnica temporal: acceso anónimo habilitado para verificar la operación en Swagger sin login previo
[AllowAnonymous]
public abstract class SmartPantryAppService : ApplicationService
{
    protected SmartPantryAppService()
    {
        LocalizationResource = typeof(SmartPantryResource);
    }
}
