using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace SmartPantry.Pantries;

/// <summary>
/// Decide qué advertencias de vencimiento crear, actualizar, reactivar o desactivar (RF-15).
/// No sabe cuándo se ejecuta: eso lo decide el worker. Recibe "hoy" como parámetro para que
/// las pruebas usen una fecha controlada, y no usa ICurrentUser: procesa los ítems de todos
/// los propietarios a partir de los datos persistidos.
/// </summary>
public class ExpirationWarningManager : DomainService
{
    private readonly IRepository<Pantry, Guid> _pantryRepository;
    private readonly IRepository<PantryItemWarning, Guid> _warningRepository;

    public ExpirationWarningManager(
        IRepository<Pantry, Guid> pantryRepository,
        IRepository<PantryItemWarning, Guid> warningRepository)
    {
        _pantryRepository = pantryRepository;
        _warningRepository = warningRepository;
    }

    /// <summary>
    /// Es idempotente: con los mismos datos y la misma fecha, una segunda ejecución no cambia nada.
    /// Las escrituras se confirman en la unidad de trabajo de quien llama.
    /// </summary>
    public async Task<ExpirationWarningProcessResult> ProcessAsync(DateOnly today)
    {
        var limit = ExpirationRule.GetWarningLimit(today);

        // 1) Ítems que hoy requieren advertencia. La base filtra por el límite y la regla lo confirma.
        var pantries = await _pantryRepository.GetQueryableAsync();
        var candidateQuery = pantries
            .SelectMany(p => p.Items)
            .Where(i => i.Status == PantryItemStatus.Active
                && i.ExpirationDate != null
                && i.ExpirationDate <= limit)
            .Select(i => new { i.Id, i.Status, i.ExpirationDate });

        var candidates = (await AsyncExecuter.ToListAsync(candidateQuery))
            .Where(i => ExpirationRule.RequiresWarning(i.Status, i.ExpirationDate, today))
            .ToDictionary(i => i.Id, i => i.ExpirationDate!.Value);

        // 2) Advertencias existentes: las de los candidatos y las activas que quizá dejaron de corresponder.
        var candidateIds = candidates.Keys.ToList();
        var warnings = await _warningRepository.GetListAsync(w =>
            w.Type == PantryItemWarningType.Expiration
            && (w.IsActive || candidateIds.Contains(w.PantryItemId)));
        var warningsByItem = warnings.ToDictionary(w => w.PantryItemId);

        int created = 0, updated = 0, reactivated = 0, deactivated = 0;

        // 3) Crear, actualizar o reactivar la advertencia de cada candidato.
        foreach (var (itemId, expirationDate) in candidates)
        {
            if (!warningsByItem.TryGetValue(itemId, out var warning))
            {
                await _warningRepository.InsertAsync(new PantryItemWarning(
                    GuidGenerator.Create(), itemId, PantryItemWarningType.Expiration, expirationDate));
                created++;
                continue;
            }

            var wasActive = warning.IsActive;
            if (warning.Refresh(expirationDate))
            {
                await _warningRepository.UpdateAsync(warning);
                if (wasActive)
                {
                    updated++;
                }
                else
                {
                    reactivated++;
                }
            }
        }

        // 4) Desactivar las que ya no corresponden: fuera del umbral, sin fecha o consumidos.
        foreach (var warning in warnings.Where(w => !candidates.ContainsKey(w.PantryItemId)))
        {
            if (warning.Deactivate())
            {
                await _warningRepository.UpdateAsync(warning);
                deactivated++;
            }
        }

        return new ExpirationWarningProcessResult(created, updated, reactivated, deactivated);
    }
}
