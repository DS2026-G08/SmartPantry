// Interpretación en pantalla de la comparación con las preferencias (RF-12).
import { PreferenceEvaluationDto } from '../proxy/preferences';
import { allergenLabel, AllergenLabel, KNOWN_ALLERGENS } from './catalog';

/** Mismos límites que DietaryPreferenceConsts en el BackEnd. */
export const MAX_WATCHED_INGREDIENTS = 20;
export const MIN_INGREDIENT_LENGTH = 2;
export const MAX_INGREDIENT_LENGTH = 50;

/** Etiqueta canónica de Open Food Facts de un alérgeno de la lista ("milk" → "en:milk"). */
export function allergenTag(name: (typeof KNOWN_ALLERGENS)[number] | string): string {
  return `en:${name}`;
}

/**
 * Coincidencias para mostrar: los alérgenos con su nombre localizado y las palabras tal como las
 * escribió el usuario.
 */
export function preferenceMatches(evaluation: PreferenceEvaluationDto | null | undefined): AllergenLabel[] {
  if (!evaluation) {
    return [];
  }

  return [
    ...(evaluation.matchedAllergens ?? []).map(allergenLabel),
    ...(evaluation.matchedIngredients ?? []).map(text => ({ text })),
  ];
}

/**
 * Forma comparable de una palabra: sin acentos, en minúsculas y con un solo espacio entre palabras.
 * Es el mismo criterio que usa el BackEnd para detectar palabras repetidas.
 */
export function normalizeIngredient(word: string): string {
  return word
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .replace(/\s+/g, ' ')
    .trim();
}
