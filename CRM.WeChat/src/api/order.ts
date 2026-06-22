/**
 * 订单 API - 微信端（销售订单 + 采购订单核心查询）
 */
import apiClient from './client'

export interface OrderListQuery {
  keyword?: string
  status?: number
  page?: number
  pageSize?: number
}

export interface SalesOrderListItem {
  id: string
  orderCode: string
  customerName: string
  totalAmount: number
  currency: number
  status: number
  statusLabel: string
  salesUserName: string
  createTime: string
}

export interface PurchaseOrderListItem {
  id: string
  orderCode: string
  vendorName: string
  totalAmount: number
  currency: number
  status: number
  statusLabel: string
  purchaseUserName: string
  createTime: string
}

export interface OrderDetail {
  id: string
  orderCode: string
  status: number
  statusLabel: string
  customerName?: string
  vendorName?: string
  items: OrderItem[]
  totalAmount: number
  currency: number
  currencyLabel: string
  remark: string
  createTime: string
  createUser: string
}

export interface OrderItem {
  id: string
  itemCode: string
  materialName: string
  materialModel: string
  quantity: number
  unitPrice: number
  totalPrice: number
}

export const orderApi = {
  /** 销售订单列表 */
  getSalesOrders(query: OrderListQuery): Promise<{ items: SalesOrderListItem[]; total: number }> {
    return apiClient.get('/api/v1/sales-orders', query as Record<string, any>)
  },

  /** 销售订单详情 */
  getSalesOrderDetail(id: string): Promise<OrderDetail> {
    return apiClient.get(`/api/v1/sales-orders/${id}`)
  },

  /** 采购订单列表 */
  getPurchaseOrders(query: OrderListQuery): Promise<{ items: PurchaseOrderListItem[]; total: number }> {
    return apiClient.get('/api/v1/purchase-orders', query as Record<string, any>)
  },

  /** 采购订单详情 */
  getPurchaseOrderDetail(id: string): Promise<OrderDetail> {
    return apiClient.get(`/api/v1/purchase-orders/${id}`)
  },
}
