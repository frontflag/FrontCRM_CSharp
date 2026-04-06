import apiClient from './client';

export type DictionaryItemDto = { code: string; label: string };
export type VendorFormDictResponse = Record<string, DictionaryItemDto[]>;

export const dictionaryApi = {
  async fetchVendorForm(): Promise<VendorFormDictResponse> {
    return apiClient.get<VendorFormDictResponse>('/api/v1/dictionaries/vendor-form');
  },
  async fetchCustomerForm(): Promise<VendorFormDictResponse> {
    return apiClient.get<VendorFormDictResponse>('/api/v1/dictionaries/customer-form');
  },

  /** 物料「生产日期」MaterialProductionDate */
  async fetchMaterialForm(): Promise<VendorFormDictResponse> {
    return apiClient.get<VendorFormDictResponse>('/api/v1/dictionaries/material-form');
  },

  /** 物流「来货方式」「快递方式」LogisticsArrivalMethod / LogisticsExpressMethod */
  async fetchLogisticsForm(): Promise<VendorFormDictResponse> {
    return apiClient.get<VendorFormDictResponse>('/api/v1/dictionaries/logistics-form');
  },

  /** 含已禁用项，用于历史编码在表单中回显 */
  async lookup(category: string, code: string): Promise<DictionaryItemDto | null> {
    const row = await apiClient.get<DictionaryItemDto | null>('/api/v1/dictionaries/lookup', {
      params: { category, code }
    });
    return row ?? null;
  }
};
