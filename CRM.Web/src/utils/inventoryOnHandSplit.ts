/** 库存中心在库汇总：拆分维度偏好（localStorage，可复选）。 */

const SPLIT_KEY = 'crm.inventory-on-hand.split'

export type InventoryOnHandSplitState = {
  stockType: boolean
  warehouse: boolean
}

const DEFAULT_SPLIT: InventoryOnHandSplitState = { stockType: false, warehouse: false }

function isBool(v: unknown): v is boolean {
  return v === true || v === false
}

export function readInventoryOnHandSplit(): InventoryOnHandSplitState {
  try {
    const raw = localStorage.getItem(SPLIT_KEY)
    if (!raw) return { ...DEFAULT_SPLIT }
    const parsed = JSON.parse(raw) as Partial<InventoryOnHandSplitState>
    return {
      stockType: isBool(parsed.stockType) ? parsed.stockType : false,
      warehouse: isBool(parsed.warehouse) ? parsed.warehouse : false
    }
  } catch {
    return { ...DEFAULT_SPLIT }
  }
}

export function writeInventoryOnHandSplit(state: InventoryOnHandSplitState): void {
  try {
    localStorage.setItem(SPLIT_KEY, JSON.stringify({
      stockType: !!state.stockType,
      warehouse: !!state.warehouse
    }))
  } catch {
    /* ignore quota / private mode */
  }
}
