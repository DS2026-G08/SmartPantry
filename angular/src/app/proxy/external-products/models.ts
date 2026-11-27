import type { ExternalProductLookupStatus } from './external-product-lookup-status.enum';
import type { PreferenceEvaluationDto } from '../preferences/models';

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
  preferences?: PreferenceEvaluationDto | null;
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
