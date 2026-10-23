using SmartPantry.Localization;
using Volo.Abp.Application.Services;

namespace SmartPantry;

/* Inherit your application services from this class.
 */
// TP09: ya no permite acceso anónimo. Cada AppService declara su propia autorización con [Authorize].
public abstract class SmartPantryAppService : ApplicationService
{
    protected SmartPantryAppService()
    {
        LocalizationResource = typeof(SmartPantryResource);
    }
}
