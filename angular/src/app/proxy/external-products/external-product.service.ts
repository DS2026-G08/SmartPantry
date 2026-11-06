import type { ExternalProductLookupInputDto, ExternalProductLookupResultDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class ExternalProductService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  getByBarcode = (input: ExternalProductLookupInputDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ExternalProductLookupResultDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/external-product/by-barcode',
      params: { barcode: input.barcode },
    },
    { apiName: this.apiName,...config });
}