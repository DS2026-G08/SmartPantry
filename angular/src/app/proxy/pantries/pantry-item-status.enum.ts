import { mapEnumToOptions } from '@abp/ng.core';

export enum PantryItemStatus {
  Active = 1,
  Consumed = 2,
}

export const pantryItemStatusOptions = mapEnumToOptions(PantryItemStatus);
