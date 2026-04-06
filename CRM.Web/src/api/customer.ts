import apiClient from './client';
import { parseApiBoolean } from '@/utils/parseApiBoolean';
import { industryCellToStorageLabel } from '@/utils/customerIndustryStorage';
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

/** 客户 ID 可能含需编码字符，统一用于路径段 */
function customersPath(id: string, suffix: string): string {
  return `/api/v1/customers/${encodeURIComponent(id)}${suffix}`;
}

/** 不在前端展示客户「归属」(ascriptionType：专属/公海)，从接口对象上剥离。 */
function stripCustomerAscriptionType(obj: Record<string, unknown> | null | undefined): void {
  if (!obj || typeof obj !== 'object') return;
  delete obj.ascriptionType;
  delete obj.AscriptionType;
}

/**
 * 将前端 customerLevel 字符串映射到后端 Level：D=1 … VPO=6
 */
export function mapCustomerLevelToInt(level: string | number | undefined): number {
  if (level === undefined || level === null) return 3;
  if (typeof level === 'number') return level > 0 ? level : 3;
  const map: Record<string, number> = {
    D: 1,
    C: 2,
    B: 3,
    BPO: 4,
    VIP: 5,
    VPO: 6
  };
  return map[level] ?? 3;
}

/**
 * 将后端 customerType（Type）数字映射为中文标签（无 i18n 场景）
 * 1:历史 OEM；2–6 沿用；7–11 为扩展类型
 */
export function mapCustomerTypeToLabel(type: number | undefined): string {
  const map: Record<number, string> = {
    1: 'OEM',
    2: 'ODM',
    3: '终端',
    4: 'IDH',
    5: '贸易商',
    6: '代理商',
    7: 'EMS',
    8: '非行业',
    9: '科研机构',
    10: '供应链',
    11: '原厂'
  };
  return map[type ?? 0] ?? '未知';
}

/**
 * 客户地址：前端下拉值 ↔ 后端 customeraddress.AddressType（short）
 * 1 收货 2 账单；3/4 为前端扩展（办公、注册），与实体注释可并存。
 */
export const ADDRESS_TYPE_UI_TO_SHORT: Record<string, number> = {
  Shipping: 1,
  Billing: 2,
  Office: 3,
  Registered: 4
};

export const ADDRESS_TYPE_SHORT_TO_UI: Record<number, string> = {
  1: 'Shipping',
  2: 'Billing',
  3: 'Office',
  4: 'Registered'
};

export function mapAddressUiTypeToShort(type: string | number | undefined): number {
  if (type === undefined || type === null) return 3;
  if (typeof type === 'number' && Number.isFinite(type)) return Math.trunc(type);
  return ADDRESS_TYPE_UI_TO_SHORT[String(type)] ?? 3;
}

/** 后端 AddAddressRequest（camelCase JSON） */
export function mapCreateAddressRequestToAddBody(data: CreateAddressRequest) {
  return {
    addressType: mapAddressUiTypeToShort(data.addressType),
    province: data.province || undefined,
    city: data.city || undefined,
    area: data.district || undefined,
    address: data.streetAddress || undefined,
    contactName: data.contactPerson || undefined,
    contactPhone: data.contactPhone || undefined,
    isDefault: !!data.isDefault
  };
}

/**
 * 后端 UpdateAddressRequest（camelCase）。
 * 仅序列化传入的字段；字符串传空串以便清空（C# 中 null 表示不更新）。
 */
export function mapPartialCreateAddressToUpdateBody(data: Partial<CreateAddressRequest>): Record<string, unknown> {
  const o: Record<string, unknown> = {};
  if (data.addressType !== undefined) o.addressType = mapAddressUiTypeToShort(data.addressType);
  if (data.province !== undefined) o.province = data.province ?? '';
  if (data.city !== undefined) o.city = data.city ?? '';
  if (data.district !== undefined) o.area = data.district ?? '';
  if (data.streetAddress !== undefined) o.address = data.streetAddress ?? '';
  if (data.contactPerson !== undefined) o.contactName = data.contactPerson ?? '';
  if (data.contactPhone !== undefined) o.contactPhone = data.contactPhone ?? '';
  if (data.isDefault !== undefined) o.isDefault = data.isDefault;
  return o;
}

/** 将接口返回的地址行统一为前端 CustomerAddress（兼容 address/area/contactName 与数字 addressType） */
export function normalizeCustomerAddressFromApi(raw: unknown): CustomerAddress {
  const r = raw as Record<string, unknown> | null;
  if (!r || typeof r !== 'object') {
    return {
      id: '',
      customerId: '',
      addressType: 'Office',
      streetAddress: '',
      isDefault: false
    };
  }
  const id = String(r.id ?? r.addressId ?? '');
  const typeRaw = r.addressType;
  let addressType: string;
  if (typeof typeRaw === 'number') {
    addressType = ADDRESS_TYPE_SHORT_TO_UI[typeRaw] ?? String(typeRaw);
  } else if (typeof typeRaw === 'string' && ADDRESS_TYPE_UI_TO_SHORT[typeRaw] !== undefined) {
    addressType = typeRaw;
  } else {
    addressType = 'Office';
  }
  const street = (r.streetAddress ?? r.address ?? '') as string;
  const district = (r.district ?? r.area ?? '') as string;
  const contact = (r.contactPerson ?? r.contactName ?? '') as string;
  return {
    id,
    customerId: String(r.customerId ?? ''),
    addressType,
    country: r.country != null && r.country !== '' ? String(r.country) : undefined,
    province: (r.province as string) || undefined,
    city: (r.city as string) || undefined,
    district,
    streetAddress: street,
    zipCode: (r.zipCode as string) || undefined,
    contactPerson: contact,
    contactPhone: (r.contactPhone as string) || undefined,
    isDefault: Boolean(r.isDefault),
    remark: r.remark as string | undefined,
    createdAt: (r.createdAt ?? r.createTime) as string | undefined,
    updatedAt: (r.updatedAt ?? r.modifyTime) as string | undefined
  };
}

/**
 * 客户联系人：与「编辑联系人」表单一致。
 * - 姓名：兼容 API 的 contactName / name（NotMapped 别名序列化差异）
 * - 性别：0=保密、1=男、2=女（兼容字符串数字）
 * - 手机：兼容 mobilePhone / mobile
 */
export function normalizeCustomerContactFromApi(raw: unknown): CustomerContactInfo {
  const r = raw as Record<string, unknown> | null;
  if (!r || typeof r !== 'object') {
    return {
      id: '',
      customerId: '',
      contactName: '',
      gender: 0,
      mobilePhone: '',
      isDecisionMaker: false,
      isDefault: false
    };
  }
  const name = String(r.contactName ?? r.name ?? r.cName ?? '').trim();
  const gRaw = r.gender;
  const gNum = gRaw === null || gRaw === undefined || gRaw === '' ? NaN : Number(gRaw);
  const gender = gNum === 0 || gNum === 1 || gNum === 2 ? gNum : 0;
  const mobile = String(r.mobilePhone ?? r.mobile ?? '').trim();
  return {
    id: String(r.id ?? ''),
    customerId: String(r.customerId ?? ''),
    contactName: name,
    gender,
    position: (r.position ?? r.title) as string | undefined,
    department: r.department as string | undefined,
    mobilePhone: mobile,
    phone: (r.phone ?? r.tel) as string | undefined,
    fax: r.fax as string | undefined,
    email: r.email as string | undefined,
    socialAccount: (r.socialAccount ?? r.qq ?? r.weChat) as string | undefined,
    isDecisionMaker: parseApiBoolean(r.isDecisionMaker),
    isDefault: parseApiBoolean(r.isDefault ?? r.isMain),
    remarks: (r.remarks ?? r.remark) as string | undefined,
    createdAt: (r.createdAt ?? r.createTime) as string | undefined,
    updatedAt: (r.updatedAt ?? r.modifyTime) as string | undefined
  };
}

export function formatCustomerAddressTypeLabel(type: string | number | undefined): string {
  const labels: Record<string, string> = {
    Office: '办公地址',
    Billing: '开票地址',
    Shipping: '收货地址',
    Registered: '注册地址'
  };
  if (typeof type === 'number') {
    const ui = ADDRESS_TYPE_SHORT_TO_UI[type];
    return (ui && labels[ui]) || String(type);
  }
  return labels[String(type)] || String(type ?? '');
}

/** 将接口返回的银行行规范为前端 CustomerBankInfo（兼容 bankAccount 等字段） */
export function normalizeCustomerBankFromApi(raw: unknown): CustomerBankInfo {
  const r = raw as Record<string, unknown> | null;
  if (!r || typeof r !== 'object') {
    return {
      id: '',
      customerId: '',
      bankName: '',
      accountNumber: '',
      accountName: '',
      bankBranch: '',
      currency: 1,
      isDefault: false
    };
  }
  const acct = r.accountNumber ?? r.bankAccount ?? '';
  return {
    id: String(r.id ?? ''),
    customerId: String(r.customerId ?? ''),
    bankName: String(r.bankName ?? ''),
    accountNumber: typeof acct === 'string' ? acct : String(acct ?? ''),
    accountName: String(r.accountName ?? ''),
    bankBranch: String(r.bankBranch ?? ''),
    currency: typeof r.currency === 'number' && Number.isFinite(r.currency) ? r.currency : Number(r.currency) || 1,
    swiftCode: r.swiftCode as string | undefined,
    isDefault: parseApiBoolean(r.isDefault ?? r['IsDefault']),
    remark: r.remark as string | undefined,
    createdAt: (r.createdAt ?? r.createTime) as string | undefined,
    updatedAt: (r.updatedAt ?? r.modifyTime) as string | undefined
  };
}

/**
 * 客户详情 JSON 中银行为 bankAccounts（后端 CustomerInfo.BankAccounts），前端表格使用 banks。
 */
export function banksFromCustomerApiPayload(raw: Record<string, unknown>): CustomerBankInfo[] {
  const list = (raw.banks ?? raw.bankAccounts) as unknown[] | undefined;
  if (!Array.isArray(list)) return [];
  return list.map((b) => normalizeCustomerBankFromApi(b));
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
    if (params.customerLevel) {
      const lv = mapCustomerLevelToInt(params.customerLevel);
      queryParams.append('customerLevel', String(lv));
    }
    if (params.industry) queryParams.append('industry', industryCellToStorageLabel(params.industry));
    if (params.region) queryParams.append('region', params.region);
    if (params.salesPersonId) queryParams.append('salesUserId', params.salesPersonId);
    if (params.createdFrom) queryParams.append('createdFrom', params.createdFrom);
    if (params.createdTo) queryParams.append('createdTo', params.createdTo);
    if (params.status !== undefined && params.status !== null && !Number.isNaN(Number(params.status))) {
      queryParams.append('status', String(params.status));
    } else if (params.isActive !== undefined) {
      queryParams.append('isActive', params.isActive.toString());
    }
    if (params.sortBy) queryParams.append('sortBy', params.sortBy);
    if (params.sortDescending !== undefined) queryParams.append('sortDescending', params.sortDescending.toString());

    const response = await apiClient.get<any>(`/api/v1/customers?${queryParams.toString()}`);
    const data = response || { items: [], totalCount: 0 };
    if (Array.isArray(data.items)) {
      for (const it of data.items) {
        stripCustomerAscriptionType(it as Record<string, unknown>);
      }
    }
    return data;
  },

  // 获取客户详情
  async getCustomerById(id: string): Promise<Customer> {
    const raw = (await apiClient.get<Record<string, unknown>>(`/api/v1/customers/${id}`)) as Record<string, unknown>;
    stripCustomerAscriptionType(raw);
    const banks = banksFromCustomerApiPayload(raw);
    const { bankAccounts: _bankAccounts, ...rest } = raw;
    const customer = { ...(rest as unknown as Customer), banks };
    if (Array.isArray(customer.contacts)) {
      customer.contacts = customer.contacts.map((c) => normalizeCustomerContactFromApi(c));
    }
    return customer;
  },

  /** Excel 批量导入（解析后的结构化数据） */
  async importCustomersBatch(payload: {
    items: Array<{
      customer: Record<string, unknown>;
      contacts: Array<Record<string, unknown>>;
    }>;
  }): Promise<{
    successCount: number;
    failCount: number;
    items: Array<{
      index: number;
      success: boolean;
      customerCode?: string;
      customerId?: string;
      error?: string;
    }>;
  }> {
    return await apiClient.post('/api/v1/customers/import/batch', payload);
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
    delete backendData.ascriptionType;
    if (backendData.invoiceType === '' || backendData.invoiceType === 0) {
      backendData.invoiceType = undefined;
    }
    if (backendData.industry != null && String(backendData.industry).trim() !== '') {
      backendData.industry = industryCellToStorageLabel(String(backendData.industry));
    }
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
    delete backendData.ascriptionType;
    if (backendData.invoiceType === '' || backendData.invoiceType === 0) {
      backendData.invoiceType = undefined;
    }
    if (backendData.industry != null && String(backendData.industry).trim() !== '') {
      backendData.industry = industryCellToStorageLabel(String(backendData.industry));
    }
    return await apiClient.put<Customer>(`/api/v1/customers/${id}`, backendData);
  },

  // 删除客户（带理由，软删除到回收站）
  async deleteCustomer(id: string, reason?: string): Promise<void> {
    await apiClient.delete(`/api/v1/customers/${id}`, { data: { reason } });
  },

  // 加入黑名单
  async addToBlacklist(id: string, reason: string): Promise<void> {
    await apiClient.post(`/api/v1/customers/${id}/blacklist`, { reason });
  },

  // 移出黑名单（需原因，与后端 POST .../remove-blacklist 一致）
  async removeFromBlacklist(id: string, reason: string): Promise<void> {
    await apiClient.post(`/api/v1/customers/${id}/remove-blacklist`, { reason });
  },

  /** 冻结客户（需原因，写入操作日志） */
  async freezeCustomer(id: string, reason: string): Promise<void> {
    await apiClient.post(customersPath(id, '/freeze'), { reason });
  },

  /** 启用客户/解除冻结（需原因，写入操作日志） */
  async unfreezeCustomer(id: string, reason: string): Promise<void> {
    await apiClient.post(customersPath(id, '/unfreeze'), { reason });
  },

  // 获取回收站列表
  async getRecycleBin(params: { pageIndex?: number; pageSize?: number; keyword?: string } = {}): Promise<any> {
    const q = new URLSearchParams();
    q.append('pageIndex', String(params.pageIndex ?? 1));
    q.append('pageSize', String(params.pageSize ?? 20));
    if (params.keyword) q.append('keyword', params.keyword);
    const res = await apiClient.get<any>(`/api/v1/customers/recycle-bin?${q.toString()}`);
    if (Array.isArray(res?.items)) {
      for (const it of res.items) stripCustomerAscriptionType(it as Record<string, unknown>);
    }
    return res;
  },

  // 从回收站恢复客户
  async restoreCustomer(id: string): Promise<void> {
    await apiClient.post(`/api/v1/customers/${id}/restore`);
  },

  // 获取黑名单列表
  async getBlacklist(params: { pageIndex?: number; pageSize?: number; keyword?: string } = {}): Promise<any> {
    const q = new URLSearchParams();
    q.append('pageIndex', String(params.pageIndex ?? 1));
    q.append('pageSize', String(params.pageSize ?? 20));
    if (params.keyword) q.append('keyword', params.keyword);
    const res = await apiClient.get<any>(`/api/v1/customers/blacklist?${q.toString()}`);
    if (Array.isArray(res?.items)) {
      for (const it of res.items) stripCustomerAscriptionType(it as Record<string, unknown>);
    }
    return res;
  },

  // 获取冻结客户列表
  async getFrozen(params: { pageIndex?: number; pageSize?: number; keyword?: string } = {}): Promise<any> {
    const q = new URLSearchParams();
    q.append('page', String(params.pageIndex ?? 1));
    q.append('pageSize', String(params.pageSize ?? 20));
    if (params.keyword) q.append('keyword', params.keyword);
    const res = await apiClient.get<any>(`/api/v1/customers/frozen?${q.toString()}`);
    if (Array.isArray(res?.items)) {
      for (const it of res.items) stripCustomerAscriptionType(it as Record<string, unknown>);
    }
    return res;
  },

  // 获取操作日志
  async getOperationLogs(customerId: string): Promise<any[]> {
    const res = await apiClient.get<any>(`/api/v1/customers/${customerId}/operation-logs`);
    return Array.isArray(res) ? res : [];
  },

  // 获取字段变更日志（后端路由为 change-logs）
  async getFieldChangeLogs(customerId: string): Promise<any[]> {
    const res = await apiClient.get<any>(`/api/v1/customers/${customerId}/change-logs`);
    return Array.isArray(res) ? res : [];
  },

  // 更新联系历史
  async updateContactHistory(customerId: string, historyId: string, data: any): Promise<any> {
    return await apiClient.put<any>(`/api/v1/customers/${customerId}/contact-history/${historyId}`, data);
  },

  // 删除联系历史
  async deleteContactHistory(customerId: string, historyId: string): Promise<void> {
    await apiClient.delete(`/api/v1/customers/${customerId}/contact-history/${historyId}`);
  },

  // 激活客户
  async activateCustomer(id: string): Promise<void> {
    await apiClient.post(`/api/v1/customers/${id}/activate`);
  },

  // 停用客户
  async deactivateCustomer(id: string): Promise<void> {
    await apiClient.post(`/api/v1/customers/${id}/deactivate`);
  },

  // 提交审核（新建 -> 待审核）
  async submitAudit(id: string): Promise<void> {
    await apiClient.post(`/api/v1/customers/${id}/submit-audit`);
  },

  // 获取客户统计信息
  async getCustomerStatistics(): Promise<CustomerStatistics> {
    const raw = (await apiClient.get<Partial<CustomerStatistics>>('/api/v1/customers/statistics')) as Partial<CustomerStatistics>
    return {
      totalCustomers: raw.totalCustomers ?? 0,
      activeCustomers: raw.activeCustomers ?? 0,
      newThisMonth: raw.newThisMonth ?? 0,
      newLast30Days: raw.newLast30Days ?? 0,
      customersWithDeals: raw.customersWithDeals ?? 0,
      totalBalance: raw.totalBalance ?? 0,
      receivableGoodsAmount: raw.receivableGoodsAmount ?? 0,
      receivableCustomerCount: raw.receivableCustomerCount ?? 0,
      pendingOutboundAmount: raw.pendingOutboundAmount ?? 0,
      pendingOutboundCustomerCount: raw.pendingOutboundCustomerCount ?? 0,
      byLevel: raw.byLevel ?? {},
      byIndustry: raw.byIndustry ?? {}
    }
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
  normalizeContactPayload(data: Partial<CreateContactRequest> & Record<string, any>) {
    /** 0=保密、1=男、2=女（后端 Gender 存 short） */
    const normalizedGender = (() => {
      const raw: unknown = data.gender
      if (raw === null || raw === undefined || raw === '') return 0
      const n = Number(raw)
      if (!Number.isFinite(n)) return 0
      if (n === 0 || n === 1 || n === 2) return n
      return 0
    })()

    const name = data.contactName ?? data.name ?? ''
    const mobile = data.mobilePhone ?? data.mobile ?? ''
    const o: Record<string, unknown> = {
      name,
      contactName: name,
      gender: normalizedGender,
      department: data.department ?? '',
      position: data.position ?? '',
      phone: data.phone ?? data.tel ?? '',
      mobile,
      mobilePhone: mobile,
      email: data.email ?? '',
      fax: data.fax ?? '',
      isDefault: Boolean(data.isDefault),
      tel: data.phone ?? data.tel ?? ''
    }
    if (data.isDecisionMaker !== undefined) {
      o.isDecisionMaker = Boolean(data.isDecisionMaker)
    }
    return o
  },

  // 获取客户联系人列表
  async getContactsByCustomerId(customerId: string): Promise<CustomerContactInfo[]> {
    const raw = await apiClient.get<unknown>(`/api/v1/customers/${encodeURIComponent(customerId)}/contacts`);
    return Array.isArray(raw) ? (raw as CustomerContactInfo[]) : [];
  },

  // 创建联系人
  async createContact(customerId: string, data: CreateContactRequest): Promise<CustomerContactInfo> {
    const backendData = this.normalizeContactPayload(data as Partial<CreateContactRequest> & Record<string, any>);
    return await apiClient.post<CustomerContactInfo>(`/api/v1/customers/${customerId}/contacts`, backendData);
  },

  // 更新联系人
  async updateContact(contactId: string, data: Partial<CreateContactRequest>): Promise<CustomerContactInfo> {
    const backendData = this.normalizeContactPayload(data as Partial<CreateContactRequest> & Record<string, any>);
    return await apiClient.put<CustomerContactInfo>(`/api/v1/contacts/${contactId}`, backendData);
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
    const body = mapCreateAddressRequestToAddBody(data);
    return await apiClient.post<CustomerAddress>(`/api/v1/customers/${customerId}/addresses`, body);
  },

  // 更新地址
  async updateAddress(addressId: string, data: Partial<CreateAddressRequest>): Promise<CustomerAddress> {
    const body = mapPartialCreateAddressToUpdateBody(data);
    return await apiClient.put<CustomerAddress>(`/api/v1/addresses/${addressId}`, body);
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
    const list = await apiClient.get<unknown[]>(`/api/v1/customers/${customerId}/banks`);
    return Array.isArray(list) ? list.map((b) => normalizeCustomerBankFromApi(b)) : [];
  },

  // 创建银行信息
  async createBank(customerId: string, data: CreateBankInfoRequest): Promise<CustomerBankInfo> {
    const raw = await apiClient.post<unknown>(`/api/v1/customers/${customerId}/banks`, data);
    return normalizeCustomerBankFromApi(raw);
  },

  // 更新银行信息
  async updateBank(bankId: string, data: Partial<CreateBankInfoRequest>): Promise<CustomerBankInfo> {
    const raw = await apiClient.put<unknown>(`/api/v1/banks/${bankId}`, data);
    return normalizeCustomerBankFromApi(raw);
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
