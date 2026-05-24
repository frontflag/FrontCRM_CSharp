/** 出库通知 stockoutrequest.Status，与后端 StockOutRequestStatusCode 一致 */
export const STOCK_OUT_REQUEST_STATUS = {
  PendingPacking: 10,
  Packed: 20,
  StockedOut: 100,
  Cancelled: -1
} as const

export type StockOutRequestStatusValue =
  (typeof STOCK_OUT_REQUEST_STATUS)[keyof typeof STOCK_OUT_REQUEST_STATUS]
