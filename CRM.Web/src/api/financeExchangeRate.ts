import apiClient from './client'

export interface FinanceExchangeRateDto {
  usdToCny: number
  usdToHkd: number
  usdToEur: number
  modifyTimeUtc?: string | null
  modifyUserId?: string | null
  modifyUserName?: string | null
}

export interface FinanceExchangeRateChangeLogDto {
  id: string
  changeTimeUtc: string
  changeUserId?: string | null
  changeUserName?: string | null
  usdToCny: number
  usdToHkd: number
  usdToEur: number
  changeSummary?: string | null
}

export interface ChangeLogPage {
  items: FinanceExchangeRateChangeLogDto[]
  totalCount: number
  pageIndex: number
  pageSize: number
}

export const financeExchangeRateApi = {
  async getCurrent(): Promise<FinanceExchangeRateDto> {
    return apiClient.get<FinanceExchangeRateDto>('/api/v1/finance/exchange-rates/current')
  },

  async save(body: { usdToCny: number; usdToHkd: number; usdToEur: number }): Promise<FinanceExchangeRateDto> {
    return apiClient.put<FinanceExchangeRateDto>('/api/v1/finance/exchange-rates/current', body)
  },

  async getChangeLog(page = 1, pageSize = 20): Promise<ChangeLogPage> {
    return apiClient.get<ChangeLogPage>('/api/v1/finance/exchange-rates/change-log', {
      params: { page, pageSize }
    })
  }
}
