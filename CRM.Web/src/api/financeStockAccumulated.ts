import apiClient from './client'

export interface FinanceStockAccumulatedSearchOptions {
  years: string[]
}

export interface FinanceStockAccumulatedMonthRow {
  yearMonth: string
  prvAmountTotal: number | null
  currentStockInAmountTotal: number | null
  currentStockOutAmountTotal: number | null
  balanceAmountTotal: number | null
  prvStockQty: number
  stockInQty: number
  stockOutQty: number
  balanceStockQty: number
}

export interface FinanceStockAccumulatedList {
  year: string
  maskAmounts: boolean
  items: FinanceStockAccumulatedMonthRow[]
}

export interface FinanceStockAccumulatedItemQuery {
  month?: string
  queryKeywords?: string
  pn?: string
  stockInCode?: string
  stockInTimeStart?: string
  stockInTimeEnd?: string
  page?: number
  pageSize?: number
}

export interface FinanceStockAccumulatedItemRow {
  stockInItemId: string
  billCode: string
  pn?: string | null
  stockInTime: string
  stockInQty: number
  stockOutQty: number
  prvQty: number
  balanceQty: number
  prvAmountTotal: number | null
  currentStockInAmountTotal: number | null
  currentStockOutAmountTotal: number | null
  balanceAmountTotal: number | null
}

export interface FinanceStockAccumulatedItemPage {
  maskAmounts: boolean
  items: FinanceStockAccumulatedItemRow[]
  total: number
  page: number
  pageSize: number
}

const BASE = '/api/v1/finance/accumulated'

export const financeStockAccumulatedApi = {
  getSearchOptions() {
    return apiClient.get<FinanceStockAccumulatedSearchOptions>(`${BASE}/search-options`)
  },
  getStockSummary(year: string) {
    return apiClient.get<FinanceStockAccumulatedList>(`${BASE}/stock`, { params: { year } })
  },
  getStockItems(query: FinanceStockAccumulatedItemQuery) {
    return apiClient.get<FinanceStockAccumulatedItemPage>(`${BASE}/stock-items`, { params: query })
  }
}
