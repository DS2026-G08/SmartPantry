// Datos del catálogo externo que la interfaz necesita interpretar (RF-07).

/** Mismo formato que valida el BackEnd: sólo dígitos, entre 8 y 14. */
export const BARCODE_PATTERN = /^[0-9]{8,14}$/;

/**
 * Alérgenos de declaración obligatoria, con el nombre de su etiqueta en Open Food Facts ("en:milk" → "milk").
 * Cada uno tiene una clave de localización "Allergen:<nombre>". Las etiquetas que no están en la lista
 * se muestran con su texto original.
 */
export const KNOWN_ALLERGENS = [
  'gluten',
  'crustaceans',
  'eggs',
  'fish',
  'peanuts',
  'soybeans',
  'milk',
  'nuts',
  'celery',
  'mustard',
  'sesame-seeds',
  'sulphur-dioxide-and-sulphites',
  'lupin',
  'molluscs',
] as const;

export interface AllergenLabel {
  /** Clave de localización si el alérgeno es conocido. */
  key?: string;
  /** Texto legible si no lo es. */
  text: string;
}

export function allergenLabel(tag: string): AllergenLabel {
  const name = tag.replace(/^en:/, '');
  const known = (KNOWN_ALLERGENS as readonly string[]).includes(name);
  return {
    key: known ? `::Allergen:${name}` : undefined,
    text: name.replace(/-/g, ' '),
  };
}
