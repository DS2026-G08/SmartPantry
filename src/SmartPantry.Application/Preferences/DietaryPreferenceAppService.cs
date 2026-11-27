using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace SmartPantry.Preferences;

/// <summary>
/// RF-03: preferencias del usuario autenticado. No recibe un id de usuario: siempre son las de
/// ICurrentUser, así no hay forma de leer ni cambiar las de otro (regla 3).
/// </summary>
[Authorize]
public class DietaryPreferenceAppService : SmartPantryAppService, IDietaryPreferenceAppService
{
    private readonly IRepository<DietaryPreference, Guid> _preferenceRepository;
    private readonly PreferenceWarningManager _warningManager;

    public DietaryPreferenceAppService(
        IRepository<DietaryPreference, Guid> preferenceRepository,
        PreferenceWarningManager warningManager)
    {
        _preferenceRepository = preferenceRepository;
        _warningManager = warningManager;
    }

    public async Task<DietaryPreferenceDto> GetAsync()
    {
        var userId = CurrentUser.GetId();
        return PreferenceMapping.ToDto(await _preferenceRepository.FindAsync(p => p.UserId == userId));
    }

    public async Task<DietaryPreferenceDto> UpdateAsync(UpdateDietaryPreferenceDto input)
    {
        var userId = CurrentUser.GetId();
        var preference = await _preferenceRepository.FindAsync(p => p.UserId == userId);
        var isNew = preference == null;
        preference ??= new DietaryPreference(GuidGenerator.Create(), userId);

        // El dominio valida y normaliza; un valor inválido responde un mensaje localizado sin guardar nada.
        preference.Update(input.WatchedAllergens, input.WatchedIngredients, input.ShowAlerts);

        if (isNew)
        {
            await _preferenceRepository.InsertAsync(preference, autoSave: true);
        }
        else
        {
            await _preferenceRepository.UpdateAsync(preference, autoSave: true);
        }

        // RF-12: las advertencias guardadas de la despensa siguen a las preferencias nuevas.
        await _warningManager.RefreshForOwnerAsync(userId, preference);

        return PreferenceMapping.ToDto(preference);
    }
}
