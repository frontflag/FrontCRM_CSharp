import apiClient from './client';
import type {
  Customer,
  CustomerContactInfo,
  CustomerAddress,
  CustomerBankInfo,
  CustomerSearchRequest,
  CustomerSearchResponse,
  CustomerStatistics,
  CreateCustomerRequest,
  UpdateCustomerRequest,
  CreateContactRequest,
  CreateAddressRequest,
  CreateBankInfoRequest
} from '@/types/customer';

/**
 * P2 修复：将前端 customerLevel 字符串统一映射到后端数字枚举
 * 后端枚举: D=1, C=2, B=3, BPO=4, VIP=5, VPO=6
 * 兼容旧前端字符串值: Normal->3(B), Important->5(VIP), Lead->1(D)
 */
export function mapCustomerLevelToInt(level: string | number | undefined): number {
  if (level === undefined || level === null) return 3; // 默认 B(Normal)
  if (typeof level === 'number') return level > 0 ? level : 3;
  const map: Record<string, number> = {
    'D': 1, 'C': 2, 'B': 3, 'BPO': 4, 'VIP': 5, 'VPO': 6,
    // 兼容旧前端字符串值
    'Normal': 3, 'Important': 5, 'Lead': 1
  };
  return map[level] ?? 3;
}

/**
 * P3 修复：将后端 customerType 数字映射到前端显示标签
 * 后端枚举: OEM=1, ODM=2, EndUser=3, IDH=4, Trader=5, Agent=6
 */
export function mapCustomerTypeToLabel(type: number | undefined): string {
  const map: Record<number, string> = {
    1: 'OEM', 2: 'ODM', 3: '终端用户', 4: 'IDH', 5: '贸易商', 6: '代理商'
  };
  return map[type ?? 0] ?? '未知';
}

// 客户API
export const customerApi = {
  // 搜索客户列表
  async searchCustomers(params: CustomerSearchRequest): Promise<CustomerSearchResponse> {
    const queryParams = new URLSearchParams();
    if (params.pageNumber) queryParams.append('pageNumber', params.pageNumber.toString());
    if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString());
    if (params.searchTerm) queryParams.append('searchTerm', params.searchTerm);
    if (params.customerType !== undefined && params.customerType > 0) queryParams.append('customerType', params.customerType.toString());
    if (params.customerLevel) queryParams.append('customerLevel', params.customerLevel);
    if (params.industry) queryParams.append('industry', params.industry);
    if (params.region) queryParams.append('region', params.region);
    if (params.isActive !== undefined) queryParams.append('isActive', params.isActive.toString());
    if (params.sortBy) queryParams.append('sortBy', params.sortBy);
    if (params.sortDescending !== undefined) queryParams.append('sortDescending', params.sortDescending.toString());

    const response = await apiClient.get<any>(`/api/v1/customers?${queryParams.toString()}`);
    return response || { items: [], totalCount: 0 };
  },

  // 获取客户详情
  async getCustomerById(id: string): Promise<Customer> {
    return await apiClient.get<Customer>(`/api/v1/customers/${id}`);
  },

  // 创建客户
  async createCustomer(data: CreateCustomerRequest): Promise<Customer> {
    const backendData: any = {
      ...data,
      officialName: data.customerName || (data as any).officialName,
      nickName: data.customerShortName || (data as any).nickName,
      // P2 修复：使用统一映射函数
      level: mapCustomerLevelToInt(data.customerLevel || (data as any).level),
      // P3 修复：customerType 前端已是数字，直接传递（默认值从 0 改为 1）
      type: (data.customerType && data.customerType > 0) ? data.customerType : ((data as any).type || 1),
      salesUserId: data.salesPersonId || (data as any).salesUserId,
      remark: data.remarks || (data as any).remark,
      creditLine: data.creditLimit ?? (data as any).creditLine,
      payment: data.paymentTerms ?? (data as any).payment,
      tradeCurrency: data.currency ?? (data as any).tradeCurrency,
      creditCode: data.unifiedSocialCreditCode || (data as any).creditCode,
      // P1 修复：新建时包含联系人数组
      contacts: (data as any).contacts || []
    };
    return await apiClient.post<Customer>('/api/v1/customers', backendData);
  },

  // 更新客户
  async updateCustomer(id: string, data: UpdateCustomerRequest): Promise<Customer> {
    const backendData: any = {
      ...data,
      officialName: data.customerName || (data as any).officialName,
      nickName: data.customerShortName || (data as any).nickName,
      // P2 修复：使用统一映射函数
      level: mapCustomerLevelToInt(data.customerLevel || (data as any).level),
      // P3 修复：customerType 直接传递
      type: (data.customerType && data.customerType > 0) ? data.customerType : ((data as any).type || 1),
      salesUserId: data.salesPersonId || (data as any).salesUserId,
      remark: data.remarks || (data as any).remark,
      creditLine: data.creditLimit ?? (data as any).creditLine,
      payment: data.paymentTerms ?? (data as any).payment,
      tradeCurrency: data.currency ?? (data as any).tradeCurrency,
      creditCode: data.unifiedSocialCreditCode || (data as any).creditCode,
      // P1 修复：更新时包含联系人数组
      contacts: (data as any).contacts || []
    };
    return await apiClient.put<Customer>(`/api/v1/customers/${id}`, backendData);
  },

  // 删除客户
  async deleteCustomer(id: string): Promise<void> {
    await apiClient.delete(`/api/v1/customers/${id}`);
  },

  // 激活客户
  async activateCustomer(id: string): Promise<void> {
    await apiClient.post(`/api/v1/customers/${id}/activate`);
  },

  // 停用客户
  async deactivateCustomer(id: string): Promise<void> {
    await apiClient.post(`/api/v1/customers/${id}/deactivate`);
  },

  // 获取客户统计信息
  async getCustomerStatistics(): Promise<CustomerStatistics> {
    return await apiClient.get<CustomerStatistics>('/api/v1/customers/statistics');
  },

  // 获取客户联系历史
  async getCustomerContactHistory(customerId: string): Promise<any[]> {
    return await apiClient.get<any[]>(`/api/v1/customers/${customerId}/contact-history`);
  },

  // 添加联系记录
  async addContactHistory(customerId: string, data: any): Promise<any> {
    return await apiClient.post<any>(`/api/v1/customers/${customerId}/contact-history`, data);
  }
};

// 客户联系人API
export const customerContactApi = {
  // 获取客户联系人列表
  async getContactsByCustomerId(customerId: string): Promise<CustomerContactInfo[]> {
    return await apiClient.get<CustomerContactInfo[]>(`/api/v1/customers/${customerId}/contacts`);
  },

  // 创建联系人
  async createContact(customerId: string, data: CreateContactRequest): Promise<CustomerContactInfo> {
    return await apiClient.post<CustomerContactInfo>(`/api/v1/customers/${customerId}/contacts`, data);
  },

  // 更新联系人
  async updateContact(contactId: string, data: Partial<CreateContactRequest>): Promise<CustomerContactInfo> {
    return await apiClient.put<CustomerContactInfo>(`/api/v1/contacts/${contactId}`, data);
  },

  // 删除联系人
  async deleteContact(contactId: string): Promise<void> {
    await apiClient.delete(`/api/v1/contacts/${contactId}`);
  },

  // 设置默认联系人
  async setDefaultContact(contactId: string): Promise<void> {
    await apiClient.post(`/api/v1/contacts/${contactId}/set-default`);
  }
};

// 客户地址API
export const customerAddressApi = {
  // 获取客户地址列表
  async getAddressesByCustomerId(customerId: string): Promise<CustomerAddress[]> {
    return await apiClient.get<CustomerAddress[]>(`/api/v1/customers/${customerId}/addresses`);
  },

  // 创建地址
  async createAddress(customerId: string, data: CreateAddressRequest): Promise<CustomerAddress> {
    return await apiClient.post<CustomerAddress>(`/api/v1/customers/${customerId}/addresses`, data);
  },

  // 更新地址
  async updateAddress(addressId: string, data: Partial<CreateAddressRequest>): Promise<CustomerAddress> {
    return await apiClient.put<CustomerAddress>(`/api/v1/addresses/${addressId}`, data);
  },

  // 删除地址
  async deleteAddress(addressId: string): Promise<void> {
    await apiClient.delete(`/api/v1/addresses/${addressId}`);
  },

  // 设置默认地址
  async setDefaultAddress(addressId: string): Promise<void> {
    await apiClient.post(`/api/v1/addresses/${addressId}/set-default`);
  }
};

// 客户银行信息API
export const customerBankApi = {
  // 获取客户银行信息列表
  async getBanksByCustomerId(customerId: string): Promise<CustomerBankInfo[]> {
    return await apiClient.get<CustomerBankInfo[]>(`/api/v1/customers/${customerId}/banks`);
  },

  // 创建银行信息
  async createBank(customerId: string, data: CreateBankInfoRequest): Promise<CustomerBankInfo> {
    return await apiClient.post<CustomerBankInfo>(`/api/v1/customers/${customerId}/banks`, data);
  },

  // 更新银行信息
  async updateBank(bankId: string, data: Partial<CreateBankInfoRequest>): Promise<CustomerBankInfo> {
    return await apiClient.put<CustomerBankInfo>(`/api/v1/banks/${bankId}`, data);
  },

  // 删除银行信息
  async deleteBank(bankId: string): Promise<void> {
    await apiClient.delete(`/api/v1/banks/${bankId}`);
  },

  // 设置默认银行
  async setDefaultBank(bankId: string): Promise<void> {
    await apiClient.post(`/api/v1/banks/${bankId}/set-default`);
  }
};
