using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Modularity;

namespace SmartPantry;

[DependsOn(
    typeof(SmartPantryApplicationModule),
    typeof(SmartPantryDomainTestModule)
)]
public class SmartPantryApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // TP08: el worker periódico no corre en las pruebas ni en CI. Las pruebas llaman
        // directamente a ExpirationWarningManager con una fecha controlada.
        Configure<AbpBackgroundWorkerOptions>(options =>
        {
            options.IsEnabled = false;
        });
    }
}
