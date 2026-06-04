/** 出库通知 stockout_notify.CustomsStatus，与后端 StockOutNotifyCustomsStatusCode 一致 */
export const STOCK_OUT_NOTIFY_CUSTOMS_STATUS = {
  Unknown: 0,
  NotRequired: 10,
  PendingCustoms: 20,
  InCustoms: 30,
  Completed: 100
} as const

export type StockOutNotifyCustomsStatusValue =
  (typeof STOCK_OUT_NOTIFY_CUSTOMS_STATUS)[keyof typeof STOCK_OUT_NOTIFY_CUSTOMS_STATUS]
