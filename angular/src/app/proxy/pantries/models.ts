import type { EntityDto, PagedResultRequestDto } from '@abp/ng.core';
import type { PantryItemStatus } from './pantry-item-status.enum';
import type { PreferenceEvaluationDto } from '../preferences/models';

export interface CreatePantryItemDto extends UpdatePantryItemDto {
  productId?: string;
}

export interface GetPantryItemListDto extends PagedResultRequestDto {
  includeConsumed?: boolean;
}

export interface PantryItemDto extends EntityDto<string> {
  productId?: string;
  productName?: string;
  productBrand?: string | null;
  status?: PantryItemStatus;
  expirationDate?: string | null;
  quantity?: number | null;
  unit?: string | null;
  preferenceWarning?: PreferenceEvaluationDto | null;
}

export interface UpdatePantryItemDto {
  quantity?: number | null;
  unit?: string | null;
  expirationDate?: string | null;
}
