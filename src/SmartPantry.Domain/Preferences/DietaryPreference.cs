using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace SmartPantry.Preferences;

/// <summary>
/// Preferencias alimentarias de un usuario (RF-03): los alérgenos e ingredientes que quiere vigilar
/// y si quiere ver las alertas en pantalla. Hay una sola por usuario (índice único en UserId).
/// </summary>
public class DietaryPreference : AuditedAggregateRoot<Guid>
{
    private const char AllergenSeparator = ',';
    private const char IngredientSeparator = '\n';

    public Guid UserId { get; private set; }

    /// <summary>Etiquetas canónicas ("en:milk") separadas por coma; sólo de la lista de 14.</summary>
    public string WatchedAllergens { get; private set; } = string.Empty;

    /// <summary>Palabras libres, una por línea, recortadas y sin repetir.</summary>
    public string WatchedIngredients { get; private set; } = string.Empty;

    public bool ShowAlerts { get; private set; } = true;

    // Constructor privado para Entity Framework Core
    private DietaryPreference()
    {
    }

    public DietaryPreference(Guid id, Guid userId)
        : base(id)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Las preferencias deben pertenecer a un usuario.", nameof(userId));
        }

        UserId = userId;
    }

    /// <summary>
    /// Reemplaza las preferencias. Primero valida y normaliza todo; si algo es inválido lanza una
    /// BusinessException con mensaje localizado y no deja cambios parciales.
    /// </summary>
    public void Update(IEnumerable<string>? allergens, IEnumerable<string>? ingredients, bool showAlerts)
    {
        var normalizedAllergens = NormalizeAllergens(allergens);
        var normalizedIngredients = NormalizeIngredients(ingredients);

        WatchedAllergens = string.Join(AllergenSeparator, normalizedAllergens);
        WatchedIngredients = string.Join(IngredientSeparator, normalizedIngredients);
        ShowAlerts = showAlerts;
    }

    public IReadOnlyList<string> GetWatchedAllergens() => Split(WatchedAllergens, AllergenSeparator);

    public IReadOnlyList<string> GetWatchedIngredients() => Split(WatchedIngredients, IngredientSeparator);

    private static IReadOnlyList<string> Split(string value, char separator)
    {
        return string.IsNullOrEmpty(value)
            ? Array.Empty<string>()
            : value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
    }

    // Marcar dos veces la misma casilla no es un error: se guarda una sola vez, en el orden de la lista de 14.
    private static List<string> NormalizeAllergens(IEnumerable<string>? allergens)
    {
        var requested = new HashSet<string>();
        foreach (var allergen in allergens ?? Enumerable.Empty<string>())
        {
            var tag = allergen?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!DietaryPreferenceConsts.KnownAllergenTags.Contains(tag))
            {
                throw new BusinessException(SmartPantryDomainErrorCodes.UnknownAllergen)
                    .WithData("Allergen", allergen ?? string.Empty);
            }

            requested.Add(tag);
        }

        return DietaryPreferenceConsts.KnownAllergenTags.Where(requested.Contains).ToList();
    }

    // Las palabras las escribe el usuario: una repetida es un error que tiene que ver, no se descarta en silencio.
    private static List<string> NormalizeIngredients(IEnumerable<string>? ingredients)
    {
        var result = new List<string>();
        var seen = new HashSet<string>();

        foreach (var ingredient in ingredients ?? Enumerable.Empty<string>())
        {
            // Espacios repetidos o saltos de línea internos se reducen a uno.
            var word = string.Join(' ', (ingredient ?? string.Empty)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

            if (word.Length < DietaryPreferenceConsts.MinIngredientLength
                || word.Length > DietaryPreferenceConsts.MaxIngredientLength)
            {
                throw new BusinessException(SmartPantryDomainErrorCodes.InvalidIngredient)
                    .WithData("Ingredient", word)
                    .WithData("Min", DietaryPreferenceConsts.MinIngredientLength)
                    .WithData("Max", DietaryPreferenceConsts.MaxIngredientLength);
            }

            if (!seen.Add(IngredientText.Normalize(word)))
            {
                throw new BusinessException(SmartPantryDomainErrorCodes.DuplicateIngredient)
                    .WithData("Ingredient", word);
            }

            result.Add(word);
        }

        if (result.Count > DietaryPreferenceConsts.MaxWatchedIngredients)
        {
            throw new BusinessException(SmartPantryDomainErrorCodes.TooManyIngredients)
                .WithData("Max", DietaryPreferenceConsts.MaxWatchedIngredients);
        }

        return result;
    }
}
