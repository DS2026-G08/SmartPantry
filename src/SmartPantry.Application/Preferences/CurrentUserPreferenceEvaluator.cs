using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace SmartPantry.Preferences;

public class CurrentUserPreferenceEvaluator : IPreferenceEvaluator, ITransientDependency
{
    private readonly ICurrentUser _currentUser;
    private readonly IRepository<DietaryPreference, Guid> _preferenceRepository;

    public CurrentUserPreferenceEvaluator(
        ICurrentUser currentUser,
        IRepository<DietaryPreference, Guid> preferenceRepository)
    {
        _currentUser = currentUser;
        _preferenceRepository = preferenceRepository;
    }

    public async Task<PreferenceEvaluationDto?> EvaluateForCurrentUserAsync(
        IEnumerable<string> allergenTags,
        string? ingredients)
    {
        var userId = _currentUser.GetId();
        var preference = await _preferenceRepository.FindAsync(p => p.UserId == userId);

        if (preference is { ShowAlerts: false })
        {
            return null;
        }

        return PreferenceMapping.ToDto(PreferenceMatcher.Evaluate(preference, allergenTags, ingredients));
    }
}
