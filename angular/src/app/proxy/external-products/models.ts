import type { ExternalProductLookupStatus } from './external-product-lookup-status.enum';

export interface ExternalProductLookupInputDto {
  barcode: string;
}

export interface ExternalProductLookupResultDto {
  status?: ExternalProductLookupStatus;
  barcode?: string;
  name?: string | null;
  brand?: string | null;
  imageUrl?: string | null;
  categories?: string | null;
  ingredients?: string | null;
  allergens?: string[];
  nutriScore?: string | null;
  novaGroup?: number | null;
}

export interface ExternalProductSearchInputDto {
  text: string;
  skipCount?: number;
  maxResultCount?: number;
}

export interface ExternalProductSearchResultDto {
  status?: ExternalProductLookupStatus;
  items?: ExternalProductSummaryDto[];
  totalCount?: number;
}

export interface ExternalProductSummaryDto {
  barcode?: string;
  name?: string | null;
  brand?: string | null;
  imageUrl?: string | null;
}
