import { mapEnumToOptions } from '@abp/ng.core';

export enum PreferenceEvaluationStatus {
  NoPreferences = 0,
  NoMatch = 1,
  Match = 2,
  NotVerifiable = 3,
}

export const preferenceEvaluationStatusOptions = mapEnumToOptions(PreferenceEvaluationStatus);
