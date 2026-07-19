export interface QcCreateStockInDisabledHintContent {
  summary: string
  details: string[]
  nextStep: string
}

type TranslateFn = (key: string, params?: Record<string, unknown>) => string

/** 是否已生成入库（列表行或 aggregates 任一侧有记录即视为已完成）。 */
export function qcCreateStockInCompleted(
  row: Record<string, unknown>,
  hasStockInAggregate: boolean
): boolean {
  const stockInId = String(row.stockInId ?? row.StockInId ?? '').trim()
  return stockInId.length > 0 || hasStockInAggregate
}

/** 有物流写权限时，生成入库按钮是否应置灰（与列表 canCreateStockIn 口径一致并含合格数量）。 */
export function qcCreateStockInButtonDisabled(row: Record<string, unknown>): boolean {
  const status = Number(row.status ?? row.Status ?? 0)
  if (status === -1) return true
  const stockInId = String(row.stockInId ?? row.StockInId ?? '').trim()
  if (stockInId.length > 0) return true
  const passQty = Number(row.passQty ?? row.PassQty ?? 0)
  return !Number.isFinite(passQty) || passQty <= 0
}

/** 构建生成入库禁用/只读提示（与销售明细操作面板 hint 结构一致）。 */
export function buildQcCreateStockInDisabledHintContent(
  row: Record<string, unknown>,
  canWriteLogistics: boolean,
  hasStockInAggregate: boolean,
  t: TranslateFn
): QcCreateStockInDisabledHintContent | null {
  if (qcCreateStockInCompleted(row, hasStockInAggregate)) return null

  if (!canWriteLogistics) {
    return {
      summary: t('qcList.opsPanel.createStockInHintNoPermission'),
      details: [],
      nextStep: t('qcList.opsPanel.createStockInNextNoPermission')
    }
  }

  if (!qcCreateStockInButtonDisabled(row)) return null

  const status = Number(row.status ?? row.Status ?? 0)
  if (status === -1) {
    return {
      summary: t('qcList.opsPanel.createStockInHintRejected'),
      details: [],
      nextStep: t('qcList.opsPanel.createStockInNextRejected')
    }
  }

  const passQty = Number(row.passQty ?? row.PassQty ?? 0)
  if (!Number.isFinite(passQty) || passQty <= 0) {
    return {
      summary: t('qcList.opsPanel.createStockInHintNoPassQty'),
      details: [],
      nextStep: t('qcList.opsPanel.createStockInNextNoPassQty')
    }
  }

  return null
}
