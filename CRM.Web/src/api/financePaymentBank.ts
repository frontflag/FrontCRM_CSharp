import apiClient from './client'

export interface FinancePaymentBankDto {
  id: string
  bankName: string
  sortOrder: number
  isDisabled: boolean
  createTimeUtc: string
  modifyTimeUtc?: string | null
}

export const financePaymentBankApi = {
  /** 已启用项，供请款「供应商银行」等下拉使用（需登录）。 */
  async listOptions(): Promise<FinancePaymentBankDto[]> {
    return apiClient.get<FinancePaymentBankDto[]>('/api/v1/finance/payment-banks/options')
  },

  async list(): Promise<FinancePaymentBankDto[]> {
    return apiClient.get<FinancePaymentBankDto[]>('/api/v1/finance/payment-banks')
  },

  async create(body: { bankName: string; sortOrder?: number | null }): Promise<FinancePaymentBankDto> {
    return apiClient.post<FinancePaymentBankDto>('/api/v1/finance/payment-banks', body)
  },

  async update(
    id: string,
    body: { bankName: string; sortOrder: number; isDisabled: boolean }
  ): Promise<FinancePaymentBankDto> {
    return apiClient.put<FinancePaymentBankDto>(`/api/v1/finance/payment-banks/${id}`, body)
  }
}
