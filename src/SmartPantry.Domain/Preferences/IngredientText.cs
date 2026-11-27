using System.Globalization;
using System.Text;

namespace SmartPantry.Preferences;

/// <summary>
/// Forma comparable de un texto de ingredientes: sin acentos, en minúsculas y con los espacios
/// reducidos a uno. Así "azúcar" encuentra "AZUCAR" y "Azúcar  de caña".
/// </summary>
public static class IngredientText
{
    public static string Normalize(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var lastWasSpace = true; // evita un espacio al principio

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue; // la tilde de "á" queda separada de la "a" en FormD
            }

            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace)
                {
                    builder.Append(' ');
                    lastWasSpace = true;
                }

                continue;
            }

            builder.Append(char.ToLowerInvariant(c));
            lastWasSpace = false;
        }

        return builder.ToString().TrimEnd().Normalize(NormalizationForm.FormC);
    }
}
