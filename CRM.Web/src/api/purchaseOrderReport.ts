import apiClient from './client'

export interface SendPurchaseOrderReportEmailPayload {
  to: string
  pdfBase64: string
  fileName?: string
  subject?: string
  body?: string
}

export async function sendPurchaseOrderReportEmail(
  purchaseOrderId: string,
  payload: SendPurchaseOrderReportEmailPayload
): Promise<void> {
  // 上传 Base64 PDF + SMTP 发信常超过默认 10s，单独放宽
  await apiClient.post(
    `/api/v1/purchase-orders/${purchaseOrderId}/report/send-email`,
    payload,
    { timeout: 120_000 }
  )
}
