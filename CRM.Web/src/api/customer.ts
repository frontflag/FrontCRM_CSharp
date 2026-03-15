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

// 客户API
export const customerApi = {
  // 搜索客户列表
  async searchCustomers(params: CustomerSearchRequest): Promise<CustomerSearchResponse> {
    const queryParams = new URLSearchParams();
    if (params.pageNumber) queryParams.append('pageNumber', params.pageNumber.toString());
    if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString());
    if (params.searchTerm) queryParams.append('searchTerm', params.searchTerm);
    if (params.customerType !== undefined) queryParams.append('customerType', params.customerType.toString());
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
    // 转换字段名以匹配后端
    const backendData: any = {
      ...data,
      officialName: data.customerName || data.officialName,
      nickName: data.customerShortName || data.nickName,
      level: data.customerLevel ? (['', 'D', 'C', 'B', 'BPO', 'VIP', 'VPO'].indexOf(data.customerLevel)) : (data.level || 1),
      type: data.customerType ?? data.type,
      salesUserId: data.salesPersonId || data.salesUserId,
      remark: data.remarks || data.remark,
      creditLine: data.creditLimit ?? data.creditLine,
      payment: data.paymentTerms ?? data.payment,
      tradeCurrency: data.currency ?? data.tradeCurrency,
      creditCode: data.unifiedSocialCreditCode || data.creditCode
    };
    return await apiClient.post<Customer>('/api/v1/customers', backendData);
  },

  // 更新客户
  async updateCustomer(id: string, data: UpdateCustomerRequest): Promise<Customer> {
    // 转换字段名以匹配后端
    const backendData: any = {
      ...data,
      officialName: data.customerName || data.officialName,
      nickName: data.customerShortName || data.nickName,
      level: data.customerLevel ? (['', 'D', 'C', 'B', 'BPO', 'VIP', 'VPO'].indexOf(data.customerLevel)) : data.level,
      type: data.customerType ?? data.type,
      salesUserId: data.salesPersonId || data.salesUserId,
      remark: data.remarks || data.remark,
      creditLine: data.creditLimit ?? data.creditLine,
      payment: data.paymentTerms ?? data.payment,
      tradeCurrency: data.currency ?? data.tradeCurrency,
      creditCode: data.unifiedSocialCreditCode || data.creditCode
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
