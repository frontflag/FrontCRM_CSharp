export const VENDOR_IMPORT_CHUNK_SIZE = 200
export const VENDOR_IMPORT_TIMEOUT_MS = 120_000

export const VENDOR_IMPORT_TIMEOUT_MESSAGE =
  '请求超时，请重新导入；已成功的供应商会自动跳过'

export type VendorImportFailureRow = {
  excelRow: number
  vendorName: string
  error: string
}

export type VendorImportItemResult = {
  index?: number
  excelRow?: number
  vendorName?: string
  customerName?: string
  success?: boolean
  skipped?: boolean
  error?: string
}

/** 复制到剪贴板的一行：Excel表第X行、供应商名称、失败原因，制表符分隔。 */
export function formatVendorImportFailureLine(excelRow: number, vendorName: string, error: string): string {
  const name = vendorName.trim() || '(未填写名称)'
  const reason = error.trim() || '导入失败'
  return `Excel表第${excelRow}行\t${name}\t${reason}`
}

export function formatVendorImportFailureText(rows: VendorImportFailureRow[]): string {
  return [...rows]
    .sort((a, b) => a.excelRow - b.excelRow)
    .map((row) => formatVendorImportFailureLine(row.excelRow, row.vendorName, row.error))
    .join('\n')
}

export function vendorImportRequestError(message: string, entityLabel = '供应商'): string {
  if (/timeout|超时/i.test(message))
    return `请求超时，请重新导入；已成功的${entityLabel}会自动跳过`
  const text = message.trim()
  return text || '请求失败'
}

export function isVendorImportCanceled(error: unknown): boolean {
  if (!error || typeof error !== 'object') return false
  const e = error as { code?: string; name?: string; message?: string }
  if (e.code === 'ERR_CANCELED' || e.name === 'CanceledError' || e.name === 'AbortError') return true
  const msg = e.message ?? ''
  if (/timeout|超时/i.test(msg)) return false
  return /canceled|cancelled|aborted/i.test(msg)
}

export function failureRowsFromBatch(
  items: VendorImportItemResult[] | undefined,
  fallbackNameByRow: Map<number, string>
): VendorImportFailureRow[] {
  const rows: VendorImportFailureRow[] = []
  for (const item of items ?? []) {
    if (item.success || item.skipped) continue
    const excelRow = item.excelRow ?? 0
    rows.push({
      excelRow,
      vendorName: (item.vendorName || item.customerName || fallbackNameByRow.get(excelRow) || '').trim(),
      error: (item.error || '导入失败').trim()
    })
  }
  return rows
}

export function failureRowsForRequestError(
  chunk: Array<{ excelRow: number; vendorName: string }>,
  message: string,
  entityLabel = '供应商'
): VendorImportFailureRow[] {
  const error = vendorImportRequestError(message, entityLabel)
  return chunk.map((row) => ({
    excelRow: row.excelRow,
    vendorName: row.vendorName,
    error
  }))
}

export function countNewVendorContacts(
  items: Array<{ excelRow: number; contacts: unknown[] }>,
  skippedExcelRows: number[]
): number {
  const skipped = new Set(skippedExcelRows)
  let contacts = 0
  for (const item of items) {
    if (skipped.has(item.excelRow)) continue
    contacts += item.contacts.length
  }
  return contacts
}
