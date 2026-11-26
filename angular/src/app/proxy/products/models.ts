import type { EntityDto } from '@abp/ng.core';

export interface CreateProductDto {
  name: string;
  brand?: string | null;
  barcode?: string | null;
}

export interface ProductDto extends EntityDto<string> {
  name?: string;
  brand?: string | null;
  barcode?: string | null;
  imageUrl?: string | null;
  categories?: string | null;
  ingredients?: string | null;
  allergens?: string[];
  nutriScore?: string | null;
  novaGroup?: number | null;
}

export interface SaveExternalProductDto {
  barcode: string;
  name?: string | null;
}

export interface UpdateProductDto {
  name: string;
  brand?: string | null;
  barcode?: string | null;
}
