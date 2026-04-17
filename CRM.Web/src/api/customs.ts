import apiClient from './client'

/** 报关公司服务方向：10 深圳、20 香港（与库列 Type 一致）。 */
export const CustomsBrokerRegionType = {
  Shenzhen: 10,
  HongKong: 20
} as const

export interface CustomsBrokerDto {
  id: string
  brokerCode: string
  cname: string
  ename?: string | null
  type: number
  status: number
  remark?: string | null
  createTime?: string
}

export interface CustomsDeclarationListItemDto {
  id: string
  declarationCode: string
  stockOutRequestId: string
  customsBrokerId: string
  customsBrokerName?: string | null
  declarationType: number
  internalStatus: number
  customsClearanceStatus: number
  declareDate: string
  totalTaxAmount: number
  remark?: string | null
  createTime: string
  createByUserId?: string | null
  createUserDisplay?: string | null
}

export interface CustomsDeclarationItemListItemDto {
  id: string
  declarationId: string
  declarationCode: string
  declareDate: string
  lineNo: number
  stockOutRequestId: string
  customerId?: string | null
  customerName?: string | null
  salesUserId?: string | null
  salesUserName?: string | null
  sellOrderItemCode?: string | null
  purchasePn?: string | null
  purchaseBrand?: string | null
  declareQty: number
  declareUnitPrice: number
  dutyAmount: number
  vatAmount: number
  customsPaymentGoods: number
  customsAgencyFee: number
  otherFee: number
  inspectionFee: number
  totalValueTax: number
  taxIncludedUnitPrice: number
  createTime: string
  createUserDisplay?: string | null
}

export interface StockTransferListItemDto {
  id: string
  transferCode: string
  bizScene: string
  customsDeclarationId: string
  declarationCode?: string | null
  status: number
  confirmedTime?: string | null
  confirmedByUserId?: string | null
  fromWarehouseId: string
  toWarehouseId: string
  fromWarehouseName?: string | null
  toWarehouseName?: string | null
  createTime: string
  createUserDisplay?: string | null
  isConfirmed: boolean
}

export interface CustomsDeclarationDetailDto {
  id: string
  declarationCode: string
  stockOutRequestId: string
  customsBrokerId: string
  declarationType: number
  internalStatus: number
  customsClearanceStatus: number
  declareDate: string
  exchangeRate: number
  totalTaxAmount: number
  fromWarehouseId: string
  toWarehouseId: string
  remark?: string | null
  createTime: string
  items?: Array<Record<string, unknown>>
}

export async function fetchCustomsBrokersAdmin(): Promise<CustomsBrokerDto[]> {
  return apiClient.get<CustomsBrokerDto[]>('/api/v1/customs-brokers', { params: { all: true } })
}

export async function createCustomsBroker(body: {
  cname: string
  ename?: string | null
  type: number
  remark?: string | null
}): Promise<CustomsBrokerDto> {
  return apiClient.post<CustomsBrokerDto>('/api/v1/customs-brokers', body)
}

export async function updateCustomsBroker(
  id: string,
  body: { cname: string; ename?: string | null; type: number; remark?: string | null }
): Promise<CustomsBrokerDto> {
  return apiClient.put<CustomsBrokerDto>(`/api/v1/customs-brokers/${encodeURIComponent(id)}`, body)
}

/** 1=启用，0=停用 */
export async function patchCustomsBrokerStatus(id: string, status: 0 | 1): Promise<CustomsBrokerDto> {
  return apiClient.patch<CustomsBrokerDto>(`/api/v1/customs-brokers/${encodeURIComponent(id)}/status`, {
    status
  })
}

export async function deleteCustomsBroker(id: string): Promise<void> {
  await apiClient.delete(`/api/v1/customs-brokers/${encodeURIComponent(id)}`)
}

export async function fetchCustomsDeclarations(params: Record<string, unknown>): Promise<CustomsDeclarationListItemDto[]> {
  return apiClient.get<CustomsDeclarationListItemDto[]>('/api/v1/customs-declarations', { params })
}

export async function fetchCustomsDeclarationById(id: string): Promise<CustomsDeclarationDetailDto> {
  return apiClient.get<CustomsDeclarationDetailDto>(`/api/v1/customs-declarations/${encodeURIComponent(id)}`)
}

export async function patchCustomsClearanceStatus(id: string, customsClearanceStatus: number): Promise<void> {
  await apiClient.patch(`/api/v1/customs-declarations/${encodeURIComponent(id)}/customs-clearance-status`, {
    customsClearanceStatus
  })
}

export async function completeCustomsDeclaration(id: string): Promise<void> {
  await apiClient.post(`/api/v1/customs-declarations/${encodeURIComponent(id)}/complete`, {})
}

export async function fetchCustomsDeclarationItems(
  params: Record<string, unknown>
): Promise<CustomsDeclarationItemListItemDto[]> {
  return apiClient.get<CustomsDeclarationItemListItemDto[]>('/api/v1/customs-declaration-items', { params })
}

export async function fetchStockTransfers(params: Record<string, unknown>): Promise<StockTransferListItemDto[]> {
  return apiClient.get<StockTransferListItemDto[]>('/api/v1/stock-transfers', { params })
}

export async function confirmStockTransfer(id: string): Promise<void> {
  await apiClient.patch(`/api/v1/stock-transfers/${encodeURIComponent(id)}/confirm`, {})
}
