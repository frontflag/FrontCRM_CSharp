import apiClient from './client';
import type {
  Vendor,
  VendorContactInfo,
  VendorAddress,
  VendorBankInfo,
  VendorSearchRequest,
  VendorSearchResponse,
  CreateVendorRequest,
  UpdateVendorRequest,
  AddVendorContactRequest,
  UpdateVendorContactRequest,
  VendorStatistics
} from '@/types/vendor';

// 供应商 API
export const vendorApi = {
  // 分页查询供应商列表
  async searchVendors(params: VendorSearchRequest): Promise<VendorSearchResponse> {
    const queryParams = new URLSearchParams();
    if (params.pageNumber != null) queryParams.append('pageNumber', params.pageNumber.toString());
    if (params.pageSize != null) queryParams.append('pageSize', params.pageSize.toString());
    if (params.keyword) queryParams.append('keyword', params.keyword);
    if (params.status != null) queryParams.append('status', params.status.toString());

    const response = await apiClient.get<any>(`/api/v1/vendors?${queryParams.toString()}`);
    if (response && typeof response === 'object' && 'data' in response && response.data)
      return response.data as VendorSearchResponse;
    return response || { items: [], totalCount: 0 };
  },

  async getVendorById(id: string): Promise<Vendor> {
    const response = await apiClient.get<any>(`/api/v1/vendors/${id}`);
    if (response && typeof response === 'object' && 'data' in response && response.data)
      return response.data as Vendor;
    return response as Vendor;
  },

  async createVendor(data: CreateVendorRequest): Promise<Vendor> {
    const payload: Record<string, unknown> = {
      code: data.code?.trim(),
      name: data.name?.trim(),
      officialName: data.officialName?.trim(),
      nickName: data.nickName?.trim(),
      industry: data.industry?.trim(),
      credit: data.credit,
      status: data.status,
      officeAddress: data.officeAddress?.trim(),
      website: data.website?.trim(),
      purchaserName: data.purchaserName?.trim(),
      tradeCurrency: data.tradeCurrency ?? data.currency,
      paymentMethod: data.paymentMethod?.trim(),
      paymentDays: data.paymentDays,
      creditCode: data.creditCode?.trim(),
      taxNumber: data.taxNumber?.trim(),
      companyInfo: data.companyInfo?.trim(),
      remark: data.remark?.trim()
    };
    Object.keys(payload).forEach((k) => {
      if (payload[k] === undefined) delete payload[k];
    });
    const response = await apiClient.post<any>('/api/v1/vendors', payload);
    if (response && typeof response === 'object' && 'data' in response && response.data)
      return response.data as Vendor;
    return response as Vendor;
  },

  async updateVendor(id: string, data: UpdateVendorRequest): Promise<Vendor> {
    const payload: Record<string, unknown> = {
      name: data.name?.trim(),
      nickName: data.nickName?.trim(),
      industry: data.industry?.trim(),
      product: data.product?.trim(),
      credit: data.credit,
      status: data.status,
      officeAddress: data.officeAddress?.trim(),
      website: data.website?.trim(),
      purchaserName: data.purchaserName?.trim(),
      level: data.level,
      tradeCurrency: data.tradeCurrency,
      paymentMethod: data.paymentMethod?.trim(),
      paymentDays: data.paymentDays ?? data.payment,
      creditCode: data.creditCode?.trim(),
      companyInfo: data.companyInfo?.trim(),
      remark: data.remark?.trim(),
      externalNumber: data.externalNumber?.trim()
    };
    Object.keys(payload).forEach((k) => {
      if (payload[k] === undefined) delete payload[k];
    });
    const response = await apiClient.put<any>(`/api/v1/vendors/${id}`, payload);
    if (response && typeof response === 'object' && 'data' in response && response.data)
      return response.data as Vendor;
    return response as Vendor;
  },

  async deleteVendor(id: string): Promise<void> {
    await apiClient.delete(`/api/v1/vendors/${id}`);
  },

  async activateVendor(id: string): Promise<void> {
    await apiClient.post(`/api/v1/vendors/${id}/activate`, {});
  },

  async deactivateVendor(id: string): Promise<void> {
    await apiClient.post(`/api/v1/vendors/${id}/deactivate`, {});
  },

  async submitAudit(id: string): Promise<void> {
    await apiClient.post(`/api/v1/vendors/${id}/submit-audit`, {});
  },

  async addToBlacklist(id: string, reason: string): Promise<void> {
    await apiClient.post(`/api/v1/vendors/${id}/blacklist`, { reason });
  },

  async removeFromBlacklist(id: string): Promise<void> {
    await apiClient.delete(`/api/v1/vendors/${id}/blacklist`);
  },

  async getBlacklist(params: { pageNumber?: number; pageSize?: number; keyword?: string } = {}): Promise<VendorSearchResponse> {
    const q = new URLSearchParams();
    q.append('pageNumber', String(params.pageNumber ?? 1));
    q.append('pageSize', String(params.pageSize ?? 20));
    if (params.keyword) q.append('keyword', params.keyword);
    const res = await apiClient.get<any>(`/api/v1/vendors/blacklist?${q.toString()}`);
    if (res && typeof res === 'object' && 'items' in res) return res as VendorSearchResponse;
    return res || { items: [], totalCount: 0 };
  },

  async deleteVendorSoft(id: string, reason?: string): Promise<void> {
    await apiClient.delete(`/api/v1/vendors/${id}`, { data: { reason } });
  },

  async getRecycleBin(params: { pageIndex?: number; pageSize?: number; keyword?: string } = {}): Promise<VendorSearchResponse> {
    const q = new URLSearchParams();
    q.append('pageNumber', String(params.pageIndex ?? 1));
    q.append('pageSize', String(params.pageSize ?? 20));
    if (params.keyword) q.append('keyword', params.keyword);
    const res = await apiClient.get<any>(`/api/v1/vendors/recycle-bin?${q.toString()}`);
    if (res && typeof res === 'object' && 'items' in res) return res as VendorSearchResponse;
    return res || { items: [], totalCount: 0 };
  },

  async restoreVendor(id: string): Promise<void> {
    await apiClient.post(`/api/v1/vendors/${id}/restore`, {});
  },

  async setMainContact(contactId: string): Promise<void> {
    await apiClient.post(`/api/v1/vendor-contacts/${contactId}/set-main`, {});
  },

  // 获取供应商统计信息
  async getVendorStatistics(): Promise<VendorStatistics> {
    return await apiClient.get<VendorStatistics>('/api/v1/vendors/statistics');
  },

  // 获取供应商联系历史
  async getVendorContactHistory(vendorId: string): Promise<any[]> {
    const res = await apiClient.get<any>(`/api/v1/vendors/${vendorId}/contact-history`);
    return Array.isArray(res) ? res : (res?.data ?? []);
  },

  // 添加供应商联系记录
  async addVendorContactHistory(vendorId: string, data: any): Promise<any> {
    return await apiClient.post<any>(`/api/v1/vendors/${vendorId}/contact-history`, data);
  },

  // 更新供应商联系记录
  async updateVendorContactHistory(vendorId: string, historyId: string, data: any): Promise<any> {
    return await apiClient.put<any>(`/api/v1/vendors/${vendorId}/contact-history/${historyId}`, data);
  },

  // 删除供应商联系记录
  async deleteVendorContactHistory(vendorId: string, historyId: string): Promise<void> {
    await apiClient.delete(`/api/v1/vendors/${vendorId}/contact-history/${historyId}`);
  },

  // 获取供应商操作日志
  async getOperationLogs(vendorId: string): Promise<any[]> {
    const res = await apiClient.get<any>(`/api/v1/vendors/${vendorId}/operation-logs`);
    return res?.data ?? res ?? [];
  },

  // 获取供应商字段变更日志
  async getFieldChangeLogs(vendorId: string): Promise<any[]> {
    const res = await apiClient.get<any>(`/api/v1/vendors/${vendorId}/field-change-logs`);
    return res?.data ?? res ?? [];
  }
};

// 供应商地址 API
export const vendorAddressApi = {
  async getAddressesByVendorId(vendorId: string): Promise<VendorAddress[]> {
    const res = await apiClient.get<any>(`/api/v1/vendors/${vendorId}/addresses`);
    if (res && typeof res === 'object' && 'data' in res && Array.isArray(res.data))
      return res.data as VendorAddress[];
    return Array.isArray(res) ? res : [];
  },

  async createAddress(vendorId: string, data: {
    addressType: number;
    country?: number;
    province?: string;
    city?: string;
    area?: string;
    address?: string;
    contactName?: string;
    contactPhone?: string;
    isDefault: boolean;
  }): Promise<VendorAddress> {
    const res = await apiClient.post<any>(`/api/v1/vendors/${vendorId}/addresses`, data);
    if (res && typeof res === 'object' && 'data' in res && res.data)
      return res.data as VendorAddress;
    return res as VendorAddress;
  },

  async updateAddress(addressId: string, data: Partial<{
    addressType: number;
    country?: number;
    province?: string;
    city?: string;
    area?: string;
    address?: string;
    contactName?: string;
    contactPhone?: string;
    isDefault: boolean;
  }>): Promise<VendorAddress> {
    const res = await apiClient.put<any>(`/api/v1/vendors/addresses/${addressId}`, data);
    if (res && typeof res === 'object' && 'data' in res && res.data)
      return res.data as VendorAddress;
    return res as VendorAddress;
  },

  async deleteAddress(addressId: string): Promise<void> {
    await apiClient.delete(`/api/v1/vendors/addresses/${addressId}`);
  },

  async setDefaultAddress(addressId: string): Promise<void> {
    await apiClient.post(`/api/v1/vendors/addresses/${addressId}/set-default`, {});
  }
};

// 供应商银行信息 API
export const vendorBankApi = {
  async getBanksByVendorId(vendorId: string): Promise<VendorBankInfo[]> {
    const res = await apiClient.get<any>(`/api/v1/vendors/${vendorId}/banks`);
    if (res && typeof res === 'object' && 'data' in res && Array.isArray(res.data))
      return res.data as VendorBankInfo[];
    return Array.isArray(res) ? res : [];
  },

  async createBank(vendorId: string, data: {
    accountName?: string;
    bankName?: string;
    bankBranch?: string;
    bankAccount?: string;
    currency?: number;
    isDefault: boolean;
    remark?: string;
  }): Promise<VendorBankInfo> {
    const res = await apiClient.post<any>(`/api/v1/vendors/${vendorId}/banks`, data);
    if (res && typeof res === 'object' && 'data' in res && res.data)
      return res.data as VendorBankInfo;
    return res as VendorBankInfo;
  },

  async updateBank(bankId: string, data: Partial<{
    accountName?: string;
    bankName?: string;
    bankBranch?: string;
    bankAccount?: string;
    currency?: number;
    isDefault: boolean;
    remark?: string;
  }>): Promise<VendorBankInfo> {
    const res = await apiClient.put<any>(`/api/v1/vendor-banks/${bankId}`, data);
    if (res && typeof res === 'object' && 'data' in res && res.data)
      return res.data as VendorBankInfo;
    return res as VendorBankInfo;
  },

  async deleteBank(bankId: string): Promise<void> {
    await apiClient.delete(`/api/v1/vendor-banks/${bankId}`);
  },

  async setDefaultBank(bankId: string): Promise<void> {
    await apiClient.post(`/api/v1/vendor-banks/${bankId}/set-default`, {});
  }
};

// 供应商联系人 API
export const vendorContactApi = {
  async getContactsByVendorId(vendorId: string): Promise<VendorContactInfo[]> {
    const response = await apiClient.get<any>(`/api/v1/vendors/${vendorId}/contacts`);
    if (response && typeof response === 'object' && 'data' in response && Array.isArray(response.data))
      return response.data as VendorContactInfo[];
    return Array.isArray(response) ? response : [];
  },

  async createContact(vendorId: string, data: AddVendorContactRequest): Promise<VendorContactInfo> {
    const payload = {
      cName: data.cName?.trim(),
      eName: data.eName?.trim(),
      title: data.title?.trim(),
      department: data.department?.trim(),
      mobile: data.mobile?.trim(),
      tel: data.tel?.trim(),
      email: data.email?.trim(),
      isMain: data.isMain ?? false,
      remark: data.remark?.trim()
    };
    const response = await apiClient.post<any>(`/api/v1/vendors/${vendorId}/contacts`, payload);
    if (response && typeof response === 'object' && 'data' in response && response.data)
      return response.data as VendorContactInfo;
    return response as VendorContactInfo;
  },

  async updateContact(contactId: string, data: UpdateVendorContactRequest): Promise<VendorContactInfo> {
    const payload = {
      cName: data.cName?.trim(),
      eName: data.eName?.trim(),
      title: data.title?.trim(),
      department: data.department?.trim(),
      mobile: data.mobile?.trim(),
      tel: data.tel?.trim(),
      email: data.email?.trim(),
      isMain: data.isMain,
      remark: data.remark?.trim()
    };
    const response = await apiClient.put<any>(`/api/v1/vendor-contacts/${contactId}`, payload);
    if (response && typeof response === 'object' && 'data' in response && response.data)
      return response.data as VendorContactInfo;
    return response as VendorContactInfo;
  },

  async deleteContact(contactId: string): Promise<void> {
    await apiClient.delete(`/api/v1/vendor-contacts/${contactId}`);
  }
};
