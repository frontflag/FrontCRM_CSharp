/**
 * customerApi.ts — FrontCRM Customer API Service
 * Connects to .NET C# backend at http://localhost:5000/api/v1
 */

const BASE_URL = "http://localhost:5000/api/v1";

// ─── Types ────────────────────────────────────────────────────────────────────

export interface CustomerContact {
  id: string;
  customerId: string;
  name?: string;
  contactName?: string;
  cName?: string;
  eName?: string;
  gender?: number;
  title?: string;
  position?: string;
  department?: string;
  mobile?: string;
  mobilePhone?: string;
  tel?: string;
  phone?: string;
  email?: string;
  qq?: string;
  weChat?: string;
  address?: string;
  isMain: boolean;
  isDefault: boolean;
  remark?: string;
  createTime?: string;
}

export interface CustomerAddress {
  id: string;
  customerId: string;
  addressType: number;
  country?: number;
  province?: string;
  city?: string;
  area?: string;
  address?: string;
  contactName?: string;
  contactPhone?: string;
  isDefault: boolean;
  createTime?: string;
}

export interface CustomerBankInfo {
  id: string;
  customerId: string;
  bankName?: string;
  bankAccount?: string;
  accountNumber?: string;
  accountName?: string;
  bankBranch?: string;
  bankCode?: string;
  currency?: number;
  isDefault: boolean;
  remark?: string;
  createTime?: string;
}

export interface CustomerContactHistory {
  id: string;
  customerId: string;
  type: string;
  subject?: string;
  content?: string;
  contactPerson?: string;
  time: string;
  nextFollowUpTime?: string;
  result?: string;
  operatorId?: string;
  createTime?: string;
}

export interface Customer {
  id: string;
  customerCode: string;
  officialName?: string;
  customerName?: string;
  nickName?: string;
  customerShortName?: string;
  level: number;
  customerLevel?: string;
  type?: number;
  customerType?: number;
  industry?: string;
  product?: string;
  email?: string;
  phone?: string;
  salesUserId?: string;
  remark?: string;
  remarks?: string;
  creditLine: number;
  creditLimit: number;
  payment?: number;
  paymentTerms?: number;
  tradeCurrency?: number;
  currency?: number;
  creditCode?: string;
  unifiedSocialCreditCode?: string;
  status: number;
  isActive: boolean;
  isDeleted: boolean;
  blackList?: boolean;
  blackListReason?: string;
  blackListAt?: string;
  blackListByUserId?: string;
  blackListByUserName?: string;
  deleteReason?: string;
  taxRate?: number;
  invoiceType?: number;
  contacts?: CustomerContact[];
  addresses?: CustomerAddress[];
  bankAccounts?: CustomerBankInfo[];
  createTime?: string;
  modifyTime?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errorCode?: number;
}

// ─── Request Types ─────────────────────────────────────────────────────────────

export interface CreateCustomerRequest {
  customerName?: string;
  customerShortName?: string;
  customerLevel?: string;
  customerType?: number;
  industry?: string;
  product?: string;
  email?: string;
  phone?: string;
  salesPersonId?: string;
  remarks?: string;
  creditLimit?: number;
  paymentTerms?: number;
  currency?: number;
  unifiedSocialCreditCode?: string;
}

export interface UpdateCustomerRequest extends CreateCustomerRequest {}

export interface AddContactRequest {
  contactName?: string;
  gender?: number;
  department?: string;
  position?: string;
  mobile?: string;
  tel?: string;
  email?: string;
  fax?: string;
  isDefault?: boolean;
}

export interface AddAddressRequest {
  addressType: number;
  country?: number;
  province?: string;
  city?: string;
  area?: string;
  address?: string;
  contactName?: string;
  contactPhone?: string;
  isDefault?: boolean;
}

export interface AddBankRequest {
  bankName?: string;
  accountNumber?: string;
  accountName?: string;
  bankCode?: string;
  currency?: number;
  isDefault?: boolean;
  remark?: string;
}

export interface AddContactHistoryRequest {
  contactType?: string;
  subject?: string;
  content?: string;
  contactPerson?: string;
  contactTime?: string;
  nextFollowUpTime?: string;
  result?: string;
}

// ─── API Helper ────────────────────────────────────────────────────────────────

async function request<T>(
  path: string,
  options?: RequestInit
): Promise<ApiResponse<T>> {
  const res = await fetch(`${BASE_URL}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...options?.headers,
    },
    ...options,
  });

  const text = await res.text();
  if (!text) {
    return { success: res.ok, message: res.ok ? "OK" : `HTTP ${res.status}` };
  }

  try {
    return JSON.parse(text) as ApiResponse<T>;
  } catch {
    return { success: false, message: text };
  }
}

// ─── Customer CRUD ─────────────────────────────────────────────────────────────

export const customerApi = {
  /** 分页获取客户列表 */
  list(params: {
    pageNumber?: number;
    pageSize?: number;
    searchTerm?: string;
    customerLevel?: number;
    customerType?: number;
    isActive?: boolean;
  } = {}) {
    const q = new URLSearchParams();
    if (params.pageNumber) q.set("pageNumber", String(params.pageNumber));
    if (params.pageSize) q.set("pageSize", String(params.pageSize));
    if (params.searchTerm) q.set("searchTerm", params.searchTerm);
    if (params.customerLevel != null) q.set("customerLevel", String(params.customerLevel));
    if (params.customerType != null) q.set("customerType", String(params.customerType));
    if (params.isActive != null) q.set("isActive", String(params.isActive));
    return request<PagedResult<Customer>>(`/customers?${q.toString()}`);
  },

  /** 获取客户详情 */
  get(id: string) {
    return request<Customer>(`/customers/${id}`);
  },

  /** 创建客户 */
  create(data: CreateCustomerRequest) {
    return request<Customer>("/customers", {
      method: "POST",
      body: JSON.stringify(data),
    });
  },

  /** 更新客户 */
  update(id: string, data: UpdateCustomerRequest) {
    return request<Customer>(`/customers/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    });
  },

  /** 删除客户（软删除） */
  delete(id: string) {
    return request<void>(`/customers/${id}`, { method: "DELETE" });
  },

  /** 激活客户 */
  activate(id: string) {
    return request<Customer>(`/customers/${id}/activate`, { method: "POST" });
  },

  /** 停用客户 */
  deactivate(id: string) {
    return request<Customer>(`/customers/${id}/deactivate`, { method: "POST" });
  },

  // ─── Contacts ────────────────────────────────────────────────────────────────

  /** 获取联系人列表 */
  getContacts(customerId: string) {
    return request<CustomerContact[]>(`/customers/${customerId}/contacts`);
  },

  /** 添加联系人 */
  addContact(customerId: string, data: AddContactRequest) {
    return request<CustomerContact>(`/customers/${customerId}/contacts`, {
      method: "POST",
      body: JSON.stringify(data),
    });
  },

  /** 更新联系人 */
  updateContact(customerId: string, contactId: string, data: AddContactRequest) {
    return request<CustomerContact>(`/customers/${customerId}/contacts/${contactId}`, {
      method: "PUT",
      body: JSON.stringify(data),
    });
  },

  /** 删除联系人 */
  deleteContact(customerId: string, contactId: string) {
    return request<void>(`/customers/${customerId}/contacts/${contactId}`, {
      method: "DELETE",
    });
  },

  /** 设置默认联系人 */
  setDefaultContact(customerId: string, contactId: string) {
    return request<void>(`/customers/${customerId}/contacts/${contactId}/set-default`, {
      method: "POST",
    });
  },

  // ─── Addresses ───────────────────────────────────────────────────────────────

  /** 获取地址列表 */
  getAddresses(customerId: string) {
    return request<CustomerAddress[]>(`/customers/${customerId}/addresses`);
  },

  /** 添加地址 */
  addAddress(customerId: string, data: AddAddressRequest) {
    return request<CustomerAddress>(`/customers/${customerId}/addresses`, {
      method: "POST",
      body: JSON.stringify(data),
    });
  },

  /** 更新地址 */
  updateAddress(customerId: string, addressId: string, data: AddAddressRequest) {
    return request<CustomerAddress>(`/customers/${customerId}/addresses/${addressId}`, {
      method: "PUT",
      body: JSON.stringify(data),
    });
  },

  /** 删除地址 */
  deleteAddress(customerId: string, addressId: string) {
    return request<void>(`/customers/${customerId}/addresses/${addressId}`, {
      method: "DELETE",
    });
  },

  // ─── Banks ───────────────────────────────────────────────────────────────────

  /** 获取银行账户列表 */
  getBanks(customerId: string) {
    return request<CustomerBankInfo[]>(`/customers/${customerId}/banks`);
  },

  /** 添加银行账户 */
  addBank(customerId: string, data: AddBankRequest) {
    return request<CustomerBankInfo>(`/customers/${customerId}/banks`, {
      method: "POST",
      body: JSON.stringify(data),
    });
  },

  /** 更新银行账户 */
  updateBank(customerId: string, bankId: string, data: AddBankRequest) {
    return request<CustomerBankInfo>(`/customers/${customerId}/banks/${bankId}`, {
      method: "PUT",
      body: JSON.stringify(data),
    });
  },

  /** 删除银行账户 */
  deleteBank(customerId: string, bankId: string) {
    return request<void>(`/customers/${customerId}/banks/${bankId}`, {
      method: "DELETE",
    });
  },

  // ─── Contact History ─────────────────────────────────────────────────────────

  /** 获取联系历史 */
  getContactHistory(customerId: string) {
    return request<CustomerContactHistory[]>(`/customers/${customerId}/contact-history`);
  },

  /** 添加联系历史 */
  addContactHistory(customerId: string, data: AddContactHistoryRequest) {
    return request<CustomerContactHistory>(`/customers/${customerId}/contact-history`, {
      method: "POST",
      body: JSON.stringify(data),
    });
  },

  /** 更新联系历史 */
  updateContactHistory(customerId: string, historyId: string, data: AddContactHistoryRequest) {
    return request<CustomerContactHistory>(`/customers/${customerId}/contact-history/${historyId}`, {
      method: "PUT",
      body: JSON.stringify(data),
    });
  },

  /** 删除联系历史 */
  deleteContactHistory(customerId: string, historyId: string) {
    return request<void>(`/customers/${customerId}/contact-history/${historyId}`, {
      method: "DELETE",
    });
  },

  /** 将客户加入黑名单（旧，无理由） */
  blacklist(id: string) {
    return request<void>(`/customers/${id}/blacklist`, { method: "POST" });
  },

  /** 删除客户（带理由） */
  deleteWithReason(id: string, reason?: string) {
    return request<void>(`/customers/${id}/with-reason`, {
      method: "DELETE",
      body: JSON.stringify({ reason }),
    });
  },

  /** 将客户加入黑名单（带理由） */
  setBlacklist(id: string, reason: string) {
    return request<void>(`/customers/${id}/blacklist`, {
      method: "POST",
      body: JSON.stringify({ reason }),
    });
  },

  /** 移出黑名单 */
  removeBlacklist(id: string) {
    return request<void>(`/customers/${id}/remove-blacklist`, { method: "POST" });
  },

  /** 恢复已删除的客户 */
  restoreCustomer(id: string) {
    return request<void>(`/customers/${id}/restore`, { method: "POST" });
  },

  /** 获取回收站列表 */
  getRecycleBin(page = 1, pageSize = 10, keyword?: string) {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (keyword) params.set("keyword", keyword);
    return request<{ items: Customer[]; totalCount: number; pageNumber: number; pageSize: number; totalPages: number }>(`/customers/recycle-bin?${params}`);
  },

  /** 获取黑名单列表 */
  getBlacklist(page = 1, pageSize = 10, keyword?: string) {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (keyword) params.set("keyword", keyword);
     return request<{ items: Customer[]; totalCount: number; pageNumber: number; pageSize: number; totalPages: number }>(`/customers/blacklist?${params}`);
  },

  /** 获取客户操作日志 */
  getOperationLogs(id: string) {
    return request<{ id: string; customerId: string; operationType: string; operationDesc: string; operatorUserId: string; operatorUserName: string; operationTime: string; remark?: string }[]>(`/customers/${id}/operation-logs`);
  },

  /** 获取客户变更日志 */
  getChangeLogs(id: string) {
    return request<{ id: string; customerId: string; fieldName: string; fieldLabel: string; oldValue: string; newValue: string; changedByUserId: string; changedByUserName: string; changedAt: string }[]>(`/customers/${id}/change-logs`);
  },

  /** 获取客户统计 */
  getStats() {
    return request<{
      totalCustomers: number;
      activeCustomers: number;
      newThisMonth: number;
      totalBalance: number;
      byLevel: Record<string, number>;
      byIndustry: Record<string, number>;
    }>("/customers/statistics");
  },
};
