import { computed, ref, unref, watch, type MaybeRef, type Ref } from 'vue'

const STORAGE_PREFIX = 'crm-table-columns:v1:'

export type ColumnPinned = 'start' | 'end' | 'none'

/** 声明式列（供 CrmDataTable 可配置模式） */
export interface CrmTableColumnDef {
  /** 稳定键，用于持久化，勿随文案改动 */
  key: string
  /** 表头文案；勾选列等可省略，列设置里显示「勾选列」等后备文案 */
  label?: string
  prop?: string
  type?: 'selection' | 'index' | 'expand'
  width?: number | string
  minWidth?: number | string
  fixed?: boolean | 'left' | 'right'
  align?: 'left' | 'center' | 'right'
  showOverflowTooltip?: boolean
  sortable?: boolean
  /** 默认不勾选「显示」 */
  defaultHidden?: boolean
  /** 不可在设置里隐藏（勾选列、操作列等） */
  hideable?: boolean
  /** 不可拖拽排序；pinned start/end 自动视为 false */
  reorderable?: boolean
  /** 固定在首段/尾段，不参与中间列排序 */
  pinned?: ColumnPinned
  className?: string
  labelClassName?: string
  formatter?: (row: unknown, column: unknown, cellValue: unknown, index: number) => string
  /** 表头拖拽调宽（Element Plus） */
  resizable?: boolean
  /** 仅 type=selection：翻页保留已选 */
  reserveSelection?: boolean
}

export interface PersistedTableLayout {
  /** 中间列（非 pinned）的顺序，仅存 key */
  middleOrder: string[]
  /** 被用户隐藏的列 key（仅 hideable 为 true 的列会生效） */
  hiddenKeys: string[]
  /**
   * 用户拖过的列宽（px），按稳定 `key` 存储。
   * 未出现的 key 继续用列定义默认宽；新插入的列不会让旧 key 失效。
   */
  columnWidths: Record<string, number>
}

const COLUMN_WIDTH_MIN_PX = 1
const COLUMN_WIDTH_MAX_PX = 4000

function classNameTokens(className: CrmTableColumnDef['className']): string {
  return typeof className === 'string' ? className : ''
}

/** 勾选 / 操作 / 扩展列等不走本表列宽记忆（操作列与展开收起冲突；扩展列有独立存储） */
export function isColumnWidthPersistable(col: CrmTableColumnDef): boolean {
  if (col.type === 'selection' || col.type === 'index' || col.type === 'expand') return false
  if (col.resizable === false) return false
  if (col.pinned === 'end' || col.fixed === 'right') return false
  const cn = classNameTokens(col.className)
  if (cn.split(/\s+/).includes('op-col')) return false
  if (cn.includes('extend-col')) return false
  return true
}

function clampPersistedWidth(px: number): number | null {
  if (!Number.isFinite(px)) return null
  const n = Math.round(px)
  if (n < COLUMN_WIDTH_MIN_PX || n > COLUMN_WIDTH_MAX_PX) return null
  return n
}

export function sanitizeColumnWidths(
  defs: CrmTableColumnDef[],
  raw: Record<string, unknown> | null | undefined
): Record<string, number> {
  if (!raw || typeof raw !== 'object') return {}
  const allowed = new Set(defs.filter(isColumnWidthPersistable).map((d) => d.key))
  const out: Record<string, number> = {}
  for (const [k, v] of Object.entries(raw)) {
    if (!allowed.has(k)) continue
    const n = clampPersistedWidth(typeof v === 'number' ? v : Number(v))
    if (n == null) continue
    out[k] = n
  }
  return out
}

export function resolveColumnKeyFromDrag(
  defs: CrmTableColumnDef[],
  column: { columnKey?: string; property?: string } | undefined
): string | undefined {
  if (!column) return undefined
  const byKey = new Map(defs.map((d) => [d.key, d]))
  const ck = typeof column.columnKey === 'string' ? column.columnKey.trim() : ''
  if (ck && byKey.has(ck)) return ck
  const prop = typeof column.property === 'string' ? column.property.trim() : ''
  if (prop && byKey.has(prop)) return prop
  if (prop) {
    const found = defs.find((d) => d.prop === prop)
    if (found) return found.key
  }
  return undefined
}

function isPinnedStart(c: CrmTableColumnDef) {
  return c.pinned === 'start' || c.type === 'selection'
}

function isPinnedEnd(c: CrmTableColumnDef) {
  return c.pinned === 'end' || c.fixed === 'right'
}

function middleKeys(defs: CrmTableColumnDef[]) {
  return defs.filter((d) => !isPinnedStart(d) && !isPinnedEnd(d)).map((d) => d.key)
}

/** 新列按默认顺序插入到相邻已存在列之间，避免一律追加到末尾 */
function mergeMiddleOrder(defaultKeys: string[], savedKeys: string[]): string[] {
  const saved = savedKeys.filter((k) => defaultKeys.includes(k))
  const missing = defaultKeys.filter((k) => !saved.includes(k))
  if (missing.length === 0) return saved

  const result = [...saved]
  for (const key of missing) {
    const defaultIdx = defaultKeys.indexOf(key)
    let insertAt = result.length
    for (let i = defaultIdx + 1; i < defaultKeys.length; i++) {
      const pos = result.indexOf(defaultKeys[i]!)
      if (pos !== -1) {
        insertAt = pos
        break
      }
    }
    if (insertAt === result.length) {
      for (let i = defaultIdx - 1; i >= 0; i--) {
        const pos = result.indexOf(defaultKeys[i]!)
        if (pos !== -1) {
          insertAt = pos + 1
          break
        }
      }
    }
    result.splice(insertAt, 0, key)
  }
  return result
}

export function mergeLayout(defs: CrmTableColumnDef[], saved: Partial<PersistedTableLayout> | null): PersistedTableLayout {
  const mk = middleKeys(defs)
  const savedMid = (saved?.middleOrder ?? []).filter((k) => mk.includes(k))
  const mergedMid = mergeMiddleOrder(mk, savedMid)

  const hideableKeys = new Set(defs.filter((d) => d.hideable !== false).map((d) => d.key))
  let rawHidden: Set<string>

  if (!saved) {
    rawHidden = new Set<string>()
    defs.forEach((d) => {
      if (d.hideable !== false && d.defaultHidden) rawHidden.add(d.key)
    })
  } else {
    rawHidden = new Set((saved.hiddenKeys ?? []).filter((k) => hideableKeys.has(k)))
    defs.forEach((d) => {
      if (d.hideable === false) rawHidden.delete(d.key)
    })
  }

  return {
    middleOrder: mergedMid,
    hiddenKeys: [...rawHidden],
    columnWidths: sanitizeColumnWidths(defs, saved?.columnWidths as Record<string, unknown> | undefined)
  }
}

function parseStoredColumnWidths(raw: unknown): Record<string, number> | undefined {
  if (!raw || typeof raw !== 'object' || Array.isArray(raw)) return undefined
  const out: Record<string, number> = {}
  for (const [k, v] of Object.entries(raw as Record<string, unknown>)) {
    if (typeof k !== 'string' || !k) continue
    const n = clampPersistedWidth(typeof v === 'number' ? v : Number(v))
    if (n == null) continue
    out[k] = n
  }
  return out
}

function loadRaw(tableKey: string): Partial<PersistedTableLayout> | null {
  try {
    const raw = localStorage.getItem(STORAGE_PREFIX + tableKey)
    if (!raw) return null
    const p = JSON.parse(raw) as Partial<PersistedTableLayout>
    if (!p || typeof p !== 'object') return null
    return {
      middleOrder: Array.isArray(p.middleOrder) ? p.middleOrder.filter((x) => typeof x === 'string') : undefined,
      hiddenKeys: Array.isArray(p.hiddenKeys) ? p.hiddenKeys.filter((x) => typeof x === 'string') : undefined,
      columnWidths: parseStoredColumnWidths(p.columnWidths)
    }
  } catch {
    return null
  }
}

function saveRaw(tableKey: string, layout: PersistedTableLayout) {
  try {
    localStorage.setItem(STORAGE_PREFIX + tableKey, JSON.stringify(layout))
  } catch {
    /* ignore */
  }
}

export function clearTableLayout(tableKey: string) {
  try {
    localStorage.removeItem(STORAGE_PREFIX + tableKey)
  } catch {
    /* ignore */
  }
}

/**
 * 表格列顺序 / 显隐 / 用户拖过的列宽 持久化（localStorage），与 CrmDataTable 的 columnLayoutKey 配合使用。
 * tableKey 为空或未传列定义时不读写存储。
 */
export function usePersistedTableColumns(tableKey: MaybeRef<string | undefined | null>, columnDefs: Ref<CrmTableColumnDef[]>) {
  const middleOrder = ref<string[]>([])
  const hiddenKeys = ref<string[]>([])
  const columnWidths = ref<Record<string, number>>({})

  const storageKey = computed(() => String(unref(tableKey) ?? '').trim())
  const enabled = computed(() => storageKey.value.length > 0 && columnDefs.value.length > 0)

  function applyMerged() {
    if (!enabled.value) {
      middleOrder.value = []
      hiddenKeys.value = []
      columnWidths.value = {}
      return
    }
    const merged = mergeLayout(columnDefs.value, loadRaw(storageKey.value))
    middleOrder.value = merged.middleOrder
    hiddenKeys.value = merged.hiddenKeys
    columnWidths.value = merged.columnWidths
  }

  watch(
    () => [storageKey.value, columnDefs.value.map((c) => c.key).join('\0')],
    () => {
      applyMerged()
    },
    { immediate: true }
  )

  const layout = computed<PersistedTableLayout>(() => ({
    middleOrder: [...middleOrder.value],
    hiddenKeys: [...hiddenKeys.value],
    columnWidths: { ...columnWidths.value }
  }))

  watch(
    layout,
    (v) => {
      if (!enabled.value) return
      saveRaw(storageKey.value, v)
    },
    { deep: true }
  )

  const defByKey = computed(() => {
    const m = new Map<string, CrmTableColumnDef>()
    columnDefs.value.forEach((d) => m.set(d.key, d))
    return m
  })

  /** 当前应渲染的列（含顺序与可见性） */
  const orderedVisibleColumns = computed(() => {
    const defs = columnDefs.value
    const start = defs.filter(isPinnedStart)
    const end = defs.filter(isPinnedEnd)
    const hidden = new Set(hiddenKeys.value)
    const mid = middleOrder.value.map((k) => defByKey.value.get(k)).filter(Boolean) as CrmTableColumnDef[]

    const vis = (c: CrmTableColumnDef) => c.hideable === false || !hidden.has(c.key)
    const widths = columnWidths.value

    function withPersistedWidth(c: CrmTableColumnDef): CrmTableColumnDef {
      const w = widths[c.key]
      if (w == null || !isColumnWidthPersistable(c)) return c
      return { ...c, width: w }
    }

    return [...start.filter(vis), ...mid.filter(vis), ...end.filter(vis)].map(withPersistedWidth)
  })

  /** 设置面板：中间列（可排序项） */
  const settingsMiddleColumns = computed(() => {
    return middleOrder.value.map((k) => defByKey.value.get(k)).filter(Boolean) as CrmTableColumnDef[]
  })

  function setMiddleOrder(next: string[]) {
    middleOrder.value = next.filter((k) => middleKeys(columnDefs.value).includes(k))
  }

  function toggleHidden(key: string, def: CrmTableColumnDef) {
    if (def.hideable === false) return
    const s = new Set(hiddenKeys.value)
    if (s.has(key)) s.delete(key)
    else s.add(key)
    hiddenKeys.value = [...s]
  }

  /** 将列设为显示/隐藏（供 el-checkbox 绑定） */
  function setColumnVisible(key: string, def: CrmTableColumnDef, visible: boolean) {
    if (def.hideable === false) return
    const s = new Set(hiddenKeys.value)
    if (visible) s.delete(key)
    else s.add(key)
    hiddenKeys.value = [...s]
  }

  function isHidden(key: string) {
    return hiddenKeys.value.includes(key)
  }

  function resetToDefault() {
    if (storageKey.value) clearTableLayout(storageKey.value)
    applyMerged()
  }

  function applyHeaderDragWidth(column: { columnKey?: string; property?: string } | undefined, newWidth: number) {
    const key = resolveColumnKeyFromDrag(columnDefs.value, column)
    if (!key) return
    const def = defByKey.value.get(key)
    if (!def || !isColumnWidthPersistable(def)) return
    const w = clampPersistedWidth(newWidth)
    if (w == null) return
    if (columnWidths.value[key] === w) return
    columnWidths.value = { ...columnWidths.value, [key]: w }
  }

  return {
    middleOrder,
    hiddenKeys,
    columnWidths,
    orderedVisibleColumns,
    settingsMiddleColumns,
    setMiddleOrder,
    toggleHidden,
    setColumnVisible,
    isHidden,
    applyHeaderDragWidth,
    resetToDefault
  }
}
