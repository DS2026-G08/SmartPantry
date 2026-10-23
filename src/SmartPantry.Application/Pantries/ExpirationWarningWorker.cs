using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace SmartPantry.Pantries;

/// <summary>
/// RF-15, tarea periódica. El worker sólo decide cuándo se procesan los vencimientos;
/// qué advertencias crear o actualizar lo decide <see cref="ExpirationWarningManager"/>,
/// que se prueba sin esperar el temporizador.
/// </summary>
public class ExpirationWarningWorker : AsyncPeriodicBackgroundWorkerBase
{
    // Una vez por hora alcanza: la regla trabaja con días calendario.
    public const int PeriodInMilliseconds = 60 * 60 * 1000;

    public ExpirationWarningWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory)
        : base(timer, serviceScopeFactory)
    {
        Timer.Period = PeriodInMilliseconds;
        // La primera ejecución ocurre al iniciar la aplicación, sin esperar un período completo.
        Timer.RunOnStart = true;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        try
        {
            // workerContext.ServiceProvider pertenece a un alcance creado para esta ejecución:
            // el manager y sus repositorios (scoped) se resuelven acá y se descartan al terminar.
            var unitOfWorkManager = workerContext.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            var expirationWarningManager = workerContext.ServiceProvider.GetRequiredService<ExpirationWarningManager>();

            var today = ArgentinaCalendar.GetToday(DateTime.UtcNow);

            // Todas las escrituras de una ejecución se confirman o se descartan juntas.
            using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            var result = await expirationWarningManager.ProcessAsync(today);
            await uow.CompleteAsync();

            Logger.LogInformation(
                "Advertencias de vencimiento procesadas para {Today}: {Created} creadas, {Updated} actualizadas, {Reactivated} reactivadas, {Deactivated} desactivadas.",
                today, result.Created, result.Updated, result.Reactivated, result.Deactivated);
        }
        catch (Exception exception)
        {
            // Se registra y se sigue: un error no debe detener las próximas ejecuciones. Si dos
            // ejecuciones chocan contra el índice único, la siguiente corrida completa el trabajo.
            Logger.LogError(exception, "Falló el procesamiento de advertencias de vencimiento.");
        }
    }
}
