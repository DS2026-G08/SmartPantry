using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartPantry.Pantries;
using SmartPantry.Products;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace SmartPantry.Preferences;

/// <summary>
/// Crea, actualiza, reactiva o desactiva las advertencias de preferencias (RF-12), con el mismo patrón
/// que <see cref="ExpirationWarningManager"/>: una sola fila por ítem, nunca una segunda.
/// No usa ICurrentUser: quien llama indica de quién son los ítems. Las escrituras se confirman en la
/// unidad de trabajo de quien llama.
/// </summary>
public class PreferenceWarningManager : DomainService
{
    private readonly IRepository<Pantry, Guid> _pantryRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<PreferenceWarning, Guid> _warningRepository;

    public PreferenceWarningManager(
        IRepository<Pantry, Guid> pantryRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<PreferenceWarning, Guid> warningRepository)
    {
        _pantryRepository = pantryRepository;
        _productRepository = productRepository;
        _warningRepository = warningRepository;
    }

    /// <summary>
    /// Al agregar un producto a la despensa: evalúa ese ítem con las preferencias de su dueño.
    /// Devuelve la advertencia activa, o null si no corresponde ninguna.
    /// </summary>
    public async Task<PreferenceWarning?> RefreshForItemAsync(Guid pantryItemId, Product product, DietaryPreference? preference)
    {
        var existing = await _warningRepository.FindAsync(w => w.PantryItemId == pantryItemId);
        var (_, warning) = await ApplyAsync(pantryItemId, PreferenceMatcher.Evaluate(preference, product), existing);
        return warning is { IsActive: true } ? warning : null;
    }

    /// <summary>Al consumir un ítem: deja de tener sentido avisar sobre él.</summary>
    public async Task DeactivateForItemAsync(Guid pantryItemId)
    {
        var existing = await _warningRepository.FindAsync(w => w.PantryItemId == pantryItemId);
        if (existing != null && existing.Deactivate())
        {
            await _warningRepository.UpdateAsync(existing);
        }
    }

    /// <summary>
    /// Al cambiar las preferencias: recalcula los ítems disponibles del usuario y desactiva las
    /// advertencias de los consumidos. Es idempotente: con los mismos datos no escribe nada.
    /// </summary>
    public async Task<PreferenceWarningProcessResult> RefreshForOwnerAsync(Guid ownerId, DietaryPreference? preference)
    {
        var pantries = await _pantryRepository.GetQueryableAsync();
        var products = await _productRepository.GetQueryableAsync();

        var itemQuery =
            from pantry in pantries
            where pantry.OwnerId == ownerId
            from item in pantry.Items
            join product in products on item.ProductId equals product.Id
            select new { item.Id, item.Status, product.Allergens, product.Ingredients };

        var items = await AsyncExecuter.ToListAsync(itemQuery);
        var itemIds = items.Select(i => i.Id).ToList();

        var warnings = itemIds.Count == 0
            ? new List<PreferenceWarning>()
            : await _warningRepository.GetListAsync(w => itemIds.Contains(w.PantryItemId));
        var warningsByItem = warnings.ToDictionary(w => w.PantryItemId);

        var result = new PreferenceWarningProcessResult();
        foreach (var item in items)
        {
            warningsByItem.TryGetValue(item.Id, out var existing);

            var evaluation = item.Status == PantryItemStatus.Active
                ? PreferenceMatcher.Evaluate(preference, SplitTags(item.Allergens), item.Ingredients)
                : PreferenceEvaluation.NoPreferences; // consumido: no se avisa

            var (change, _) = await ApplyAsync(item.Id, evaluation, existing);
            result.Add(change);
        }

        return result;
    }

    private async Task<(PreferenceWarningChange Change, PreferenceWarning? Warning)> ApplyAsync(
        Guid pantryItemId,
        PreferenceEvaluation evaluation,
        PreferenceWarning? existing)
    {
        if (!evaluation.RequiresWarning)
        {
            if (existing != null && existing.Deactivate())
            {
                await _warningRepository.UpdateAsync(existing);
                return (PreferenceWarningChange.Deactivated, existing);
            }

            return (PreferenceWarningChange.None, existing);
        }

        if (existing == null)
        {
            var created = await _warningRepository.InsertAsync(
                new PreferenceWarning(GuidGenerator.Create(), pantryItemId, evaluation));
            return (PreferenceWarningChange.Created, created);
        }

        var wasActive = existing.IsActive;
        if (!existing.Refresh(evaluation))
        {
            return (PreferenceWarningChange.None, existing);
        }

        await _warningRepository.UpdateAsync(existing);
        return (wasActive ? PreferenceWarningChange.Updated : PreferenceWarningChange.Reactivated, existing);
    }

    // Product.Allergens guarda las etiquetas separadas por coma (ver Product.GetAllergenTags).
    private static IEnumerable<string> SplitTags(string? allergens)
    {
        return string.IsNullOrEmpty(allergens)
            ? Array.Empty<string>()
            : allergens.Split(',', StringSplitOptions.RemoveEmptyEntries);
    }
}

public enum PreferenceWarningChange
{
    None,
    Created,
    Updated,
    Reactivated,
    Deactivated
}

public class PreferenceWarningProcessResult
{
    public int Created { get; private set; }
    public int Updated { get; private set; }
    public int Reactivated { get; private set; }
    public int Deactivated { get; private set; }

    internal void Add(PreferenceWarningChange change)
    {
        switch (change)
        {
            case PreferenceWarningChange.Created: Created++; break;
            case PreferenceWarningChange.Updated: Updated++; break;
            case PreferenceWarningChange.Reactivated: Reactivated++; break;
            case PreferenceWarningChange.Deactivated: Deactivated++; break;
        }
    }
}
