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
}
