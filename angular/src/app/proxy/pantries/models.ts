import type { EntityDto, PagedResultRequestDto } from '@abp/ng.core';
import type { PantryItemStatus } from './pantry-item-status.enum';

export interface CreatePantryItemDto extends UpdatePantryItemDto {
  productId?: string;
}

export interface GetPantryItemListDto extends PagedResultRequestDto {
  includeConsumed?: boolean;
}

export interface PantryItemDto extends EntityDto<string> {
  productId?: string;
  productName?: string;
  productBrand?: string;
  status?: PantryItemStatus;
  expirationDate?: string | null;
  quantity?: number | null;
  unit?: string | null;
}

export interface UpdatePantryItemDto {
  quantity?: number | null;
  unit?: string | null;
  expirationDate?: string | null;
}
