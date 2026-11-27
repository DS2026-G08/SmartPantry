import type { PreferenceEvaluationStatus } from './preference-evaluation-status.enum';

export interface DietaryPreferenceDto {
  watchedAllergens?: string[];
  watchedIngredients?: string[];
  showAlerts?: boolean;
}

export interface PreferenceEvaluationDto {
  status?: PreferenceEvaluationStatus;
  matchedAllergens?: string[];
  matchedIngredients?: string[];
  missingAllergens?: boolean;
  missingIngredients?: boolean;
}

export interface UpdateDietaryPreferenceDto {
  watchedAllergens?: string[];
  watchedIngredients?: string[];
  showAlerts?: boolean;
}
