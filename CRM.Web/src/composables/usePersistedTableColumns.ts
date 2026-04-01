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

function mergeLayout(defs: CrmTableColumnDef[], saved: Partial<PersistedTableLayout> | null): PersistedTableLayout {
  const mk = middleKeys(defs)
  const savedMid = (saved?.middleOrder ?? []).filter((k) => mk.includes(k))
  const mergedMid = [...savedMid, ...mk.filter((k) => !savedMid.includes(k))]

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

  return { middleOrder: mergedMid, hiddenKeys: [...rawHidden] }
}

function loadRaw(tableKey: string): Partial<PersistedTableLayout> | null {
  try {
    const raw = localStorage.getItem(STORAGE_PREFIX + tableKey)
    if (!raw) return null
    const p = JSON.parse(raw) as Partial<PersistedTableLayout>
    if (!p || typeof p !== 'object') return null
    return {
      middleOrder: Array.isArray(p.middleOrder) ? p.middleOrder.filter((x) => typeof x === 'string') : undefined,
      hiddenKeys: Array.isArray(p.hiddenKeys) ? p.hiddenKeys.filter((x) => typeof x === 'string') : undefined
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
 * 表格列顺序 / 显隐持久化（localStorage），与 CrmDataTable 的 columnLayoutKey 配合使用。
 * tableKey 为空或未传列定义时不读写存储。
 */
export function usePersistedTableColumns(tableKey: MaybeRef<string | undefined | null>, columnDefs: Ref<CrmTableColumnDef[]>) {
  const middleOrder = ref<string[]>([])
  const hiddenKeys = ref<string[]>([])

  const storageKey = computed(() => String(unref(tableKey) ?? '').trim())
  const enabled = computed(() => storageKey.value.length > 0 && columnDefs.value.length > 0)

  function applyMerged() {
    if (!enabled.value) {
      middleOrder.value = []
      hiddenKeys.value = []
      return
    }
    const merged = mergeLayout(columnDefs.value, loadRaw(storageKey.value))
    middleOrder.value = merged.middleOrder
    hiddenKeys.value = merged.hiddenKeys
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
    hiddenKeys: [...hiddenKeys.value]
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

    return [...start.filter(vis), ...mid.filter(vis), ...end.filter(vis)]
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

  return {
    middleOrder,
    hiddenKeys,
    orderedVisibleColumns,
    settingsMiddleColumns,
    setMiddleOrder,
    toggleHidden,
    setColumnVisible,
    isHidden,
    resetToDefault
  }
}
