import type { DietaryPreferenceDto, UpdateDietaryPreferenceDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class DietaryPreferenceService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  get = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, DietaryPreferenceDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/dietary-preference',
    },
    { apiName: this.apiName,...config });
  

  update = (input: UpdateDietaryPreferenceDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, DietaryPreferenceDto>({
      method: 'PUT',
      headers: { Accept: 'application/json' },
      url: '/api/app/dietary-preference',
      body: input,
    },
    { apiName: this.apiName,...config });
}