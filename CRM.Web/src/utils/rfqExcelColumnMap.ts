import { mapPriceCurrency } from '@/utils/entityParseSchema'
import { DEFAULT_SETTLEMENT_CURRENCY_CODE } from '@/constants/currency'
import type { ParsedRfqItemFields } from '@/utils/entityParseSchema'
import { parseAiJsonObject } from '@/utils/aiJson'

/** Excel 明细行最大条数（不含表头） */
export const RFQ_EXCEL_MAX_DATA_ROWS = 500

/** 默认可选表头行上限（1-based 展示，0-based 存储） */
export const RFQ_EXCEL_MAX_HEADER_ROW_OPTIONS = 10

export type RfqExcelItemFieldKey =
  | 'customer_mpn'
  | 'mpn'
  | 'customer_brand'
  | 'brand'
  | 'quantity'
  | 'target_price'
  | 'price_currency'
  | 'min_package_qty'
  | 'min_order_qty'
  | 'alternative_materials'
  | 'production_date'
  | 'expiry_date'
  | 'remark'

export type RfqExcelMappingSource = 'rule' | 'ai' | 'manual'

export type RfqExcelFieldMeta = {
  key: RfqExcelItemFieldKey
  label: string
  required: boolean
  example: string
  note: string
}

export const RFQ_EXCEL_FIELD_METAS: RfqExcelFieldMeta[] = [
  { key: 'customer_mpn', label: '客户物料型号', required: false, example: 'ABC-123', note: '客户自己的物料编号' },
  { key: 'mpn', label: '物料型号(MPN)', required: true, example: 'STM32F103C8T6', note: '标准物料型号，必填' },
  { key: 'customer_brand', label: '客户品牌', required: false, example: 'ST', note: '供应品牌为空时用于品牌匹配' },
  { key: 'brand', label: '供应品牌', required: false, example: 'STMicroelectronics', note: '优先匹配；支持中英文名/别名' },
  { key: 'quantity', label: '数量', required: true, example: '1000', note: '需求数量，必填，正整数' },
  { key: 'target_price', label: '目标价', required: false, example: '2.5', note: '目标单价' },
  { key: 'price_currency', label: '货币', required: false, example: 'USD', note: 'USD/RMB/HKD/EUR，默认 USD' },
  { key: 'min_package_qty', label: '最小包装量', required: false, example: '100', note: '最小包装数量' },
  { key: 'min_order_qty', label: '最小起订量', required: false, example: '500', note: 'MOQ' },
  { key: 'alternative_materials', label: '可替代料', required: false, example: 'STM32F103CBT6', note: '多个用逗号分隔' },
  { key: 'production_date', label: '生产日期', required: false, example: '2024-01', note: '批次/生产日期要求' },
  { key: 'expiry_date', label: '有效期', required: false, example: '2026-12', note: '过期/有效期要求' },
  { key: 'remark', label: '备注', required: false, example: '需2年内产品', note: '行备注' }
]

export const RFQ_EXCEL_ITEM_FIELD_KEYS: RfqExcelItemFieldKey[] = RFQ_EXCEL_FIELD_METAS.map((m) => m.key)

const FIELD_META_BY_KEY = new Map(RFQ_EXCEL_FIELD_METAS.map((m) => [m.key, m]))

const VALID_FIELD_KEYS = new Set<string>(RFQ_EXCEL_ITEM_FIELD_KEYS)

/** 表头同义词 → 标准字段（归一化后的 key） */
const HEADER_SYNONYMS: Record<string, RfqExcelItemFieldKey> = {
  客户物料型号: 'customer_mpn',
  客户型号: 'customer_mpn',
  客户料号: 'customer_mpn',
  客户mpn: 'customer_mpn',
  customermpn: 'customer_mpn',
  customerpn: 'customer_mpn',
  customerpartno: 'customer_mpn',
  customerpartnumber: 'customer_mpn',
  custmpn: 'customer_mpn',
  custpn: 'customer_mpn',
  物料型号: 'mpn',
  物料型号mpn: 'mpn',
  mpn: 'mpn',
  pn: 'mpn',
  partno: 'mpn',
  partnumber: 'mpn',
  型号: 'mpn',
  料号: 'mpn',
  materialmodel: 'mpn',
  materialpn: 'mpn',
  客户品牌: 'customer_brand',
  customerbrand: 'customer_brand',
  custbrand: 'customer_brand',
  供应品牌: 'brand',
  品牌: 'brand',
  厂商: 'brand',
  制造商: 'brand',
  brand: 'brand',
  manufacturer: 'brand',
  mfr: 'brand',
  supplybrand: 'brand',
  vendorbrand: 'brand',
  数量: 'quantity',
  需求数量: 'quantity',
  qty: 'quantity',
  quantity: 'quantity',
  qyt: 'quantity',
  目标价: 'target_price',
  目标单价: 'target_price',
  单价: 'target_price',
  targetprice: 'target_price',
  unitprice: 'target_price',
  price: 'target_price',
  货币: 'price_currency',
  币种: 'price_currency',
  currency: 'price_currency',
  pricecurrency: 'price_currency',
  结算币别: 'price_currency',
  最小包装量: 'min_package_qty',
  包装量: 'min_package_qty',
  minpackageqty: 'min_package_qty',
  packqty: 'min_package_qty',
  spq: 'min_package_qty',
  最小起订量: 'min_order_qty',
  起订量: 'min_order_qty',
  moq: 'min_order_qty',
  minorderqty: 'min_order_qty',
  minqty: 'min_order_qty',
  可替代料: 'alternative_materials',
  替代料: 'alternative_materials',
  替代型号: 'alternative_materials',
  alternatives: 'alternative_materials',
  alternativematerials: 'alternative_materials',
  substitute: 'alternative_materials',
  生产日期: 'production_date',
  批次日期: 'production_date',
  productiondate: 'production_date',
  dc: 'production_date',
  datecode: 'production_date',
  有效期: 'expiry_date',
  过期日期: 'expiry_date',
  expirydate: 'expiry_date',
  expiredate: 'expiry_date',
  shelflife: 'expiry_date',
  备注: 'remark',
  说明: 'remark',
  行备注: 'remark',
  remark: 'remark',
  remarks: 'remark',
  notes: 'remark',
  comment: 'remark'
}

export type RfqExcelColumnMappingRow = {
  colIndex: number
  colLetter: string
  headerText: string
  fieldKey: RfqExcelItemFieldKey | null
  fieldLabel: string
  required: boolean
  matched: boolean
  mappingSource?: RfqExcelMappingSource
  confidence?: number | null
}

export type RfqExcelParseRowResult = {
  excelRow: number
  item: ParsedRfqItemFields
  error?: string
}

export type RfqExcelParseResult = {
  columnMappings: RfqExcelColumnMappingRow[]
  rows: RfqExcelParseRowResult[]
  skippedEmptyRows: number
  dataRowCount: number
  hasRequiredColumns: boolean
  missingRequiredFields: string[]
  headerRowIndex: number
}

export type RfqExcelAiColumnMapColumn = {
  colIndex: number
  fieldKey: RfqExcelItemFieldKey | null
  confidence?: number | null
}

export type RfqExcelAiColumnMapResult = {
  headerRowIndex: number
  columns: RfqExcelAiColumnMapColumn[]
}

export type RfqExcelParseOptions = {
  headerRowIndex?: number
  columnMappings?: RfqExcelColumnMappingRow[]
}

function toHalfWidth(s: string): string {
  return s.replace(/[\uFF01-\uFF5E]/g, (ch) => String.fromCharCode(ch.charCodeAt(0) - 0xfee0)).replace(/\u3000/g, ' ')
}

/** 归一化表头文本用于同义词匹配 */
export function normalizeExcelHeader(raw: unknown): string {
  let s = toHalfWidth(String(raw ?? '').trim().toLowerCase())
  s = s.replace(/\*+/g, '')
  s = s.replace(/[（(][^）)]*[）)]/g, '')
  s = s.replace(/[：:，,、/\\|·\-_\s]+/g, '')
  return s
}

export function excelColLetter(index: number): string {
  let n = index
  let s = ''
  do {
    s = String.fromCharCode(65 + (n % 26)) + s
    n = Math.floor(n / 26) - 1
  } while (n >= 0)
  return s
}

export function fieldMetaLabel(key: RfqExcelItemFieldKey | null): string {
  if (!key) return '—'
  return FIELD_META_BY_KEY.get(key)?.label ?? key
}

export function resolveHeaderFieldKey(header: unknown): RfqExcelItemFieldKey | null {
  const norm = normalizeExcelHeader(header)
  if (!norm) return null
  return HEADER_SYNONYMS[norm] ?? null
}

function coerceFieldKey(raw: unknown): RfqExcelItemFieldKey | null {
  let s = String(raw ?? '').trim()
  if (!s || s.toLowerCase() === 'null') return null
  if (s.includes('|')) s = s.split('|')[0].trim()
  if (VALID_FIELD_KEYS.has(s)) return s as RfqExcelItemFieldKey
  const byLabel = LABEL_TO_FIELD_KEY.get(normalizeExcelHeader(s))
  return byLabel ?? null
}

const LABEL_TO_FIELD_KEY = new Map<string, RfqExcelItemFieldKey>(
  RFQ_EXCEL_FIELD_METAS.flatMap((m) => {
    const entries: Array<[string, RfqExcelItemFieldKey]> = [[normalizeExcelHeader(m.label), m.key]]
    if (m.key === 'mpn') entries.push([normalizeExcelHeader('物料型号MPN'), m.key])
    return entries
  })
)

function extractColumnsArray(raw: Record<string, unknown>): unknown[] | null {
  const candidates = [raw.columns, raw.column_mappings, raw.mappings, raw.mapping, raw.column_map]
  for (const c of candidates) {
    if (Array.isArray(c)) return c
  }
  return null
}

function columnsFromFieldIndexObject(
  raw: Record<string, unknown>,
  headerCount: number
): RfqExcelAiColumnMapColumn[] {
  const columns: RfqExcelAiColumnMapColumn[] = []
  for (const [key, value] of Object.entries(raw)) {
    if (['header_row_index', 'headerRowIndex', 'columns', 'column_mappings', 'mappings'].includes(key)) {
      continue
    }
    const fieldKey = coerceFieldKey(key)
    if (!fieldKey) continue
    const colIndex = Number(value)
    if (!Number.isFinite(colIndex) || colIndex < 0 || colIndex >= headerCount) continue
    columns.push({ colIndex: Math.floor(colIndex), fieldKey, confidence: null })
  }
  return columns
}

function columnsFromHeaderTextMap(
  items: unknown[],
  headers: unknown[]
): RfqExcelAiColumnMapColumn[] {
  const columns: RfqExcelAiColumnMapColumn[] = []
  for (const item of items) {
    if (!item || typeof item !== 'object' || Array.isArray(item)) continue
    const rec = item as Record<string, unknown>
    const headerText = String(rec.header ?? rec.header_text ?? rec.headerText ?? rec.excel_header ?? '').trim()
    if (!headerText) continue
    const colIndex = headers.findIndex((h) => String(h ?? '').trim() === headerText)
    if (colIndex < 0) continue
    const fieldKey = coerceFieldKey(rec.field ?? rec.field_key ?? rec.target_field ?? rec.mapped_field)
    columns.push({ colIndex, fieldKey, confidence: null })
  }
  return columns
}

function parseColumnEntries(items: unknown[]): RfqExcelAiColumnMapColumn[] {
  const columns: RfqExcelAiColumnMapColumn[] = []
  for (const item of items) {
    if (!item || typeof item !== 'object' || Array.isArray(item)) continue
    const rec = item as Record<string, unknown>
    const colIndex = Number(rec.col_index ?? rec.colIndex ?? rec.index ?? rec.column_index)
    if (!Number.isFinite(colIndex) || colIndex < 0) continue
    const fieldKey = coerceFieldKey(rec.field ?? rec.field_key ?? rec.target_field ?? rec.mapped_field)
    const confidenceRaw = rec.confidence
    const confidence =
      confidenceRaw == null || confidenceRaw === ''
        ? null
        : Number.isFinite(Number(confidenceRaw))
          ? Number(confidenceRaw)
          : null
    columns.push({ colIndex: Math.floor(colIndex), fieldKey, confidence })
  }
  return columns
}

export function normalizeAiColumnMapResult(
  raw: Record<string, unknown>,
  fallbackHeaderRowIndex = 0,
  headers: unknown[] = []
): RfqExcelAiColumnMapResult | null {
  const headerRowIndexRaw = raw.header_row_index ?? raw.headerRowIndex
  let headerRowIndex = fallbackHeaderRowIndex
  if (headerRowIndexRaw != null && headerRowIndexRaw !== '') {
    const n = Number(headerRowIndexRaw)
    if (Number.isFinite(n) && n >= 0) headerRowIndex = Math.floor(n)
  }

  let columns: RfqExcelAiColumnMapColumn[] = []
  const columnsRaw = extractColumnsArray(raw)
  if (columnsRaw) {
    columns = parseColumnEntries(columnsRaw)
    if (!columns.length) {
      columns = columnsFromHeaderTextMap(columnsRaw, headers)
    }
  }

  if (!columns.length) {
    columns = columnsFromFieldIndexObject(raw, Math.max(headers.length, 32))
  }

  if (!columns.length) return null

  return { headerRowIndex, columns }
}

/** 解析 AI invoke 返回（兼容 data / content 多种 JSON 形态） */
export function parseAiColumnMapResponse(
  data: unknown,
  content: string,
  headers: unknown[],
  fallbackHeaderRowIndex = 0
): RfqExcelAiColumnMapResult | null {
  const tryNormalize = (raw: unknown) => {
    if (!raw || typeof raw !== 'object' || Array.isArray(raw)) return null
    return normalizeAiColumnMapResult(raw as Record<string, unknown>, fallbackHeaderRowIndex, headers)
  }

  const fromData = tryNormalize(data)
  if (fromData) return fromData

  const fromContentObj = tryNormalize(parseAiJsonObject(null, content))
  if (fromContentObj) return fromContentObj

  return null
}

function mappingRowFromHeader(
  colIndex: number,
  headerText: string,
  fieldKey: RfqExcelItemFieldKey | null,
  mappingSource: RfqExcelMappingSource,
  confidence?: number | null
): RfqExcelColumnMappingRow {
  const meta = fieldKey ? FIELD_META_BY_KEY.get(fieldKey) : undefined
  return {
    colIndex,
    colLetter: excelColLetter(colIndex),
    headerText,
    fieldKey,
    fieldLabel: meta?.label ?? '—',
    required: meta?.required ?? false,
    matched: !!fieldKey,
    mappingSource,
    confidence: confidence ?? null
  }
}

function isExcelCellEmpty(cell: unknown): boolean {
  if (cell == null) return true
  if (typeof cell === 'number') return !Number.isFinite(cell)
  return String(cell).trim() === ''
}

/** 数据区（表头行之后）该列是否至少有一个非空单元格 */
export function columnHasDataInDataRows(
  rows: unknown[][],
  headerRowIndex: number,
  colIndex: number
): boolean {
  return rows.slice(headerRowIndex + 1).some((row) => !isExcelCellEmpty(row?.[colIndex]))
}

/** 过滤掉数据区完全为空的列（仅影响展示与映列，无数据列不参与映射） */
export function filterColumnMappingsWithData(
  mappings: RfqExcelColumnMappingRow[],
  rows: unknown[][],
  headerRowIndex: number
): RfqExcelColumnMappingRow[] {
  if (!rows.length) return mappings
  return mappings.filter((m) => columnHasDataInDataRows(rows, headerRowIndex, m.colIndex))
}

/** 规则同义词映列 */
export function buildRuleColumnMappings(
  headers: unknown[],
  options?: { rows?: unknown[][]; headerRowIndex?: number }
): RfqExcelColumnMappingRow[] {
  const mappings: RfqExcelColumnMappingRow[] = []
  const usedFields = new Set<RfqExcelItemFieldKey>()
  const rows = options?.rows
  const headerRowIndex = options?.headerRowIndex ?? 0

  for (let i = 0; i < headers.length; i++) {
    const headerText = String(headers[i] ?? '').trim()
    if (!headerText) continue
    if (rows?.length && !columnHasDataInDataRows(rows, headerRowIndex, i)) continue

    let fieldKey = resolveHeaderFieldKey(headerText)
    if (fieldKey && usedFields.has(fieldKey)) fieldKey = null
    if (fieldKey) usedFields.add(fieldKey)

    mappings.push(mappingRowFromHeader(i, headerText, fieldKey, 'rule'))
  }
  return mappings
}

/** @deprecated 使用 buildRuleColumnMappings */
export function buildColumnMappings(headers: unknown[]): RfqExcelColumnMappingRow[] {
  return buildRuleColumnMappings(headers)
}

export function applyFieldAssignments(
  headers: unknown[],
  assignments: Array<{
    colIndex: number
    fieldKey: RfqExcelItemFieldKey | null
    mappingSource: RfqExcelMappingSource
    confidence?: number | null
  }>
): RfqExcelColumnMappingRow[] {
  const usedFields = new Set<RfqExcelItemFieldKey>()
  const rows: RfqExcelColumnMappingRow[] = []

  for (const a of assignments) {
    const headerText = String(headers[a.colIndex] ?? '').trim()
    if (!headerText) continue

    let fieldKey = a.fieldKey
    if (fieldKey && usedFields.has(fieldKey)) fieldKey = null
    if (fieldKey) usedFields.add(fieldKey)

    rows.push(mappingRowFromHeader(a.colIndex, headerText, fieldKey, a.mappingSource, a.confidence))
  }
  return rows
}

/** 将 AI 结果合并到规则映列：仅填充规则未识别的列 */
export function mergeRuleAndAiMappings(
  ruleMappings: RfqExcelColumnMappingRow[],
  aiResult: RfqExcelAiColumnMapResult
): RfqExcelColumnMappingRow[] {
  const usedFields = new Set(
    ruleMappings.map((m) => m.fieldKey).filter((k): k is RfqExcelItemFieldKey => !!k)
  )
  const aiByCol = new Map(aiResult.columns.map((c) => [c.colIndex, c]))

  return ruleMappings.map((row) => {
    if (row.fieldKey) return row
    const aiCol = aiByCol.get(row.colIndex)
    if (!aiCol?.fieldKey || usedFields.has(aiCol.fieldKey)) return row
    usedFields.add(aiCol.fieldKey)
    return {
      ...row,
      fieldKey: aiCol.fieldKey,
      fieldLabel: fieldMetaLabel(aiCol.fieldKey),
      required: FIELD_META_BY_KEY.get(aiCol.fieldKey)?.required ?? false,
      matched: true,
      mappingSource: 'ai' as const,
      confidence: aiCol.confidence ?? null
    }
  })
}

function buildFieldToCol(mappings: RfqExcelColumnMappingRow[]): Map<RfqExcelItemFieldKey, number> {
  const fieldToCol = new Map<RfqExcelItemFieldKey, number>()
  for (const m of mappings) {
    if (m.fieldKey != null) fieldToCol.set(m.fieldKey, m.colIndex)
  }
  return fieldToCol
}

function computeMissingRequired(fieldToCol: Map<RfqExcelItemFieldKey, number>): string[] {
  return RFQ_EXCEL_FIELD_METAS.filter((m) => m.required && !fieldToCol.has(m.key)).map((m) => m.label)
}

function cellText(row: unknown[], colIndex: number): string {
  const v = row[colIndex]
  if (v == null) return ''
  return String(v).trim()
}

function cellNumberOrNull(row: unknown[], colIndex: number): number | null {
  const t = cellText(row, colIndex)
  if (!t) return null
  const n = Number(t.replace(/,/g, ''))
  return Number.isFinite(n) ? n : null
}

function getFieldValue(row: unknown[], fieldToCol: Map<RfqExcelItemFieldKey, number>, key: RfqExcelItemFieldKey): string {
  const col = fieldToCol.get(key)
  if (col == null) return ''
  return cellText(row, col)
}

function parseItemRow(row: unknown[], fieldToCol: Map<RfqExcelItemFieldKey, number>): ParsedRfqItemFields {
  const customerMpn = getFieldValue(row, fieldToCol, 'customer_mpn')
  const mpn = getFieldValue(row, fieldToCol, 'mpn')
  const customerBrand = getFieldValue(row, fieldToCol, 'customer_brand')
  const brand = getFieldValue(row, fieldToCol, 'brand')
  const quantityRaw = fieldToCol.has('quantity') ? row[fieldToCol.get('quantity')!] : null
  const targetPrice = fieldToCol.has('target_price')
    ? cellNumberOrNull(row, fieldToCol.get('target_price')!)
    : null
  const currencyRaw = fieldToCol.has('price_currency') ? row[fieldToCol.get('price_currency')!] : null
  const minPackageQty = fieldToCol.has('min_package_qty')
    ? cellNumberOrNull(row, fieldToCol.get('min_package_qty')!)
    : null
  const minOrderQty = fieldToCol.has('min_order_qty')
    ? cellNumberOrNull(row, fieldToCol.get('min_order_qty')!)
    : null
  const alternativeMaterials = getFieldValue(row, fieldToCol, 'alternative_materials')
  const productionDate = getFieldValue(row, fieldToCol, 'production_date')
  const expiryDate = getFieldValue(row, fieldToCol, 'expiry_date')
  const remark = getFieldValue(row, fieldToCol, 'remark')

  const quantity = quantityRaw === '' || quantityRaw == null ? null : Number(String(quantityRaw).replace(/,/g, ''))

  return {
    customerMpn,
    customerBrand,
    mpn,
    brand,
    targetPrice,
    priceCurrency: mapPriceCurrency(currencyRaw) ?? DEFAULT_SETTLEMENT_CURRENCY_CODE,
    quantity: Number.isFinite(quantity as number) ? (quantity as number) : null,
    productionDate,
    expiryDate,
    minPackageQty,
    minOrderQty,
    alternativeMaterials,
    remark
  }
}

function validateItem(item: ParsedRfqItemFields): string | undefined {
  if (!item.mpn.trim()) return '缺少MPN'
  const qty = item.quantity
  if (qty == null || !Number.isFinite(qty) || qty <= 0) return '数量无效'
  return undefined
}

export function parseRfqExcelRows(rows: unknown[][], options: RfqExcelParseOptions = {}): RfqExcelParseResult {
  const headerRowIndex = options.headerRowIndex ?? 0

  if (!rows.length || headerRowIndex >= rows.length) {
    return {
      columnMappings: [],
      rows: [],
      skippedEmptyRows: 0,
      dataRowCount: 0,
      hasRequiredColumns: false,
      missingRequiredFields: ['物料型号(MPN)', '数量'],
      headerRowIndex
    }
  }

  const headers = rows[headerRowIndex] ?? []
  const columnMappings =
    options.columnMappings ??
    buildRuleColumnMappings(headers, { rows, headerRowIndex })
  const fieldToCol = buildFieldToCol(columnMappings)
  const missingRequiredFields = computeMissingRequired(fieldToCol)
  const hasRequiredColumns = missingRequiredFields.length === 0

  const dataRows = rows.slice(headerRowIndex + 1)
  let skippedEmptyRows = 0
  const parsedRows: RfqExcelParseRowResult[] = []

  for (let i = 0; i < dataRows.length; i++) {
    const row = dataRows[i] ?? []
    const isEmpty = row.every((cell) => cell === '' || cell == null)
    if (isEmpty) {
      skippedEmptyRows++
      continue
    }

    const item = parseItemRow(row, fieldToCol)
    const error = hasRequiredColumns ? validateItem(item) : '缺少必填列映射'
    parsedRows.push({
      excelRow: headerRowIndex + i + 2,
      item,
      error
    })
  }

  return {
    columnMappings,
    rows: parsedRows,
    skippedEmptyRows,
    dataRowCount: parsedRows.length,
    hasRequiredColumns,
    missingRequiredFields,
    headerRowIndex
  }
}

export function countNonEmptyDataRows(rows: unknown[][], headerRowIndex: number): number {
  return rows
    .slice(headerRowIndex + 1)
    .filter((row) => !row.every((cell) => cell === '' || cell == null)).length
}

export function buildAiColumnMapInput(headers: unknown[]): {
  headersJson: string
  targetFieldsJson: string
} {
  const headerTexts = headers.map((h) => String(h ?? '').trim())
  return {
    headersJson: JSON.stringify(headerTexts),
    targetFieldsJson: JSON.stringify(RFQ_EXCEL_ITEM_FIELD_KEYS)
  }
}

export function mappingSourceLabel(
  source: RfqExcelMappingSource | undefined,
  t: (key: string) => string
): string {
  if (source === 'ai') return t('rfqExcelImport.mappingSourceAi')
  if (source === 'manual') return t('rfqExcelImport.mappingSourceManual')
  return t('rfqExcelImport.mappingSourceRule')
}
