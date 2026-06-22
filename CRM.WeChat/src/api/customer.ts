/**
 * 客户 API - 微信端（仅核心功能）
 */
import apiClient from './client'

export interface CustomerListQuery {
  keyword?: string
  page?: number
  pageSize?: number
}

export interface CustomerListItem {
  id: string
  customerName: string
  customerShortName: string
  customerType: number
  customerLevel: number
  contactPerson: string
  contactPhone: string
  salesUserName: string
  createTime: string
}

export interface CustomerDetail {
  id: string
  customerName: string
  customerShortName: string
  customerType: number
  customerTypeLabel: string
  customerLevel: number
  customerLevelLabel: string
  unifiedSocialCreditCode: string
  contactPerson: string
  contactPhone: string
  contactEmail: string
  address: string
  salesUserName: string
  salesUserPhone: string
  remark: string
  createTime: string
  updateTime: string
}

export const customerApi = {
  /** 客户列表 */
  getList(query: CustomerListQuery): Promise<{ items: CustomerListItem[]; total: number }> {
    return apiClient.get('/api/v1/customers', query as Record<string, any>)
  },

  /** 客户详情 */
  getDetail(id: string): Promise<CustomerDetail> {
    return apiClient.get(`/api/v1/customers/${id}`)
  },
}
