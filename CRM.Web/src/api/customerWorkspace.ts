import apiClient from './client'

export type CustomerWorkspaceSource =
  | 'rfq'
  | 'rfqItem'
  | 'sellOrder'
  | 'sellOrderItem'
  | 'stockOutRequest'
  | 'packing'
  | 'packingItem'
  | 'stockOut'
  | 'stockOutItem'
  | 'financeReceipt'
  | 'financeSellInvoice'
  | 'financeReceivable'

export type CustomerWorkspace = {
  hasCustomer: boolean
  canViewFull: boolean
  customerId?: string | null
  customerCode?: string | null
  salesUserName?: string | null
  chineseName?: string | null
  englishName?: string | null
  customerType?: number | null
  customerLevel?: string | null
  industry?: string | null
  region?: string | null
  creditLimit?: number | null
  paymentTerms?: number | null
  settlementCurrency?: number | null
  taxRate?: number | null
  invoiceType?: number | null
}

export const customerWorkspaceApi = {
  async get(source: CustomerWorkspaceSource, id: string): Promise<CustomerWorkspace> {
    return apiClient.get<CustomerWorkspace>('/api/v1/customer-workspace', {
      params: { source, id }
    })
  }
}
