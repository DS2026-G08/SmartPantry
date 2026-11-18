import { mapEnumToOptions } from '@abp/ng.core';

export enum ExternalProductLookupStatus {
  Found = 1,
  NotFound = 2,
  RateLimited = 3,
  ServiceUnavailable = 4,
}

export const externalProductLookupStatusOptions = mapEnumToOptions(ExternalProductLookupStatus);
