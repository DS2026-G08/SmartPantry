using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.Domain.Entities.Auditing;

namespace SmartPantry.Preferences;

/// <summary>
/// Advertencia informativa sobre un ítem de despensa según las preferencias de su dueño (RF-12).
/// Hay como máximo una por ítem (índice único en PantryItemId): igual que en el TP08, la misma fila
/// se actualiza, se desactiva y se reactiva, nunca se crea una segunda.
/// Es una entidad aparte de PantryItemWarning porque aquélla exige una fecha de vencimiento.
/// </summary>
public class PreferenceWarning : AuditedAggregateRoot<Guid>
{
    private const char Separator = '\n';

    public Guid PantryItemId { get; private set; }

    /// <summary>Match o NotVerifiable: los otros estados no generan advertencia.</summary>
    public PreferenceEvaluationStatus Status { get; private set; }

    public string MatchedAllergens { get; private set; } = string.Empty;
    public string MatchedIngredients { get; private set; } = string.Empty;
    public bool MissingAllergens { get; private set; }
    public bool MissingIngredients { get; private set; }
    public bool IsActive { get; private set; }

    // Constructor privado para Entity Framework Core
    private PreferenceWarning()
    {
    }

    public PreferenceWarning(Guid id, Guid pantryItemId, PreferenceEvaluation evaluation)
        : base(id)
    {
        PantryItemId = pantryItemId;
        Refresh(evaluation);
    }

    /// <summary>
    /// Deja la advertencia activa con el resultado vigente. Devuelve false si ya estaba así,
    /// para no escribir cambios innecesarios.
    /// </summary>
    public bool Refresh(PreferenceEvaluation evaluation)
    {
        if (!evaluation.RequiresWarning)
        {
            throw new ArgumentException("Sólo una coincidencia o un dato faltante generan advertencia.", nameof(evaluation));
        }

        var matchedAllergens = Join(evaluation.MatchedAllergens);
        var matchedIngredients = Join(evaluation.MatchedIngredients);

        if (IsActive
            && Status == evaluation.Status
            && MatchedAllergens == matchedAllergens
            && MatchedIngredients == matchedIngredients
            && MissingAllergens == evaluation.MissingAllergens
            && MissingIngredients == evaluation.MissingIngredients)
        {
            return false;
        }

        Status = evaluation.Status;
        MatchedAllergens = matchedAllergens;
        MatchedIngredients = matchedIngredients;
        MissingAllergens = evaluation.MissingAllergens;
        MissingIngredients = evaluation.MissingIngredients;
        IsActive = true;
        return true;
    }

    /// <summary>Desactiva la advertencia cuando deja de corresponder. Devuelve false si ya estaba inactiva.</summary>
    public bool Deactivate()
    {
        if (!IsActive)
        {
            return false;
        }

        IsActive = false;
        return true;
    }

    public IReadOnlyList<string> GetMatchedAllergens() => Split(MatchedAllergens);

    public IReadOnlyList<string> GetMatchedIngredients() => Split(MatchedIngredients);

    private static string Join(IEnumerable<string> values) => string.Join(Separator, values);

    private static IReadOnlyList<string> Split(string value)
    {
        return string.IsNullOrEmpty(value)
            ? Array.Empty<string>()
            : value.Split(Separator, StringSplitOptions.RemoveEmptyEntries).ToArray();
    }
}
