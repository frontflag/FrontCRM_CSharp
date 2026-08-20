<template>
  <div
    ref="rootRef"
    class="crm-data-table-root"
    :class="[
      props.embedded ? 'crm-items-table crm-data-table crm-data-table--embedded' : 'table-wrapper crm-items-table crm-data-table',
      rowDensityClass,
      wrapperClass,
      { 'is-col-resizing': colResizeDragging }
    ]"
    :style="wrapperStyle"
    @pointermove="onColResizePointerMove"
    @pointerleave="onColResizePointerLeave"
  >
    <div
      v-if="configMode && props.showColumnSettings"
      class="crm-data-table__toolbar"
      :class="{ 'crm-data-table__toolbar--embedded': props.embedded }"
    >
      <el-button type="primary" link @click="settingsOpen = true">
        <el-icon class="crm-data-table__toolbar-icon"><Setting /></el-icon>
        列设置
      </el-button>
    </div>
    <Teleport v-if="showDensityControls && densityTeleportTarget" :to="densityTeleportTarget">
      <div class="crm-row-density-toggle-group" role="group" aria-label="列表行高">
        <el-tooltip content="紧密" placement="top" :hide-after="0"
          ><el-button
            class="list-settings-btn crm-row-density-settings-btn"
            :class="{ 'is-row-density-muted': rowDensity !== 'compact' }"
            link
            type="primary"
            aria-label="紧密"
            :aria-pressed="rowDensity === 'compact'"
            @click="setRowDensity('compact')"
          >
            <span class="crm-row-density-icon-wrap" aria-hidden="true">
              <svg class="crm-row-density-icon" viewBox="0 0 20 20" width="18" height="18">
                <rect x="2" y="4.5" width="16" height="1.5" rx="0.5" fill="currentColor" />
                <rect x="2" y="9.25" width="16" height="1.5" rx="0.5" fill="currentColor" />
                <rect x="2" y="14" width="16" height="1.5" rx="0.5" fill="currentColor" />
              </svg>
            </span>
          </el-button></el-tooltip><el-tooltip content="适中" placement="top" :hide-after="0"
          ><el-button
            class="list-settings-btn crm-row-density-settings-btn"
            :class="{ 'is-row-density-muted': rowDensity !== 'medium' }"
            link
            type="primary"
            aria-label="适中"
            :aria-pressed="rowDensity === 'medium'"
            @click="setRowDensity('medium')"
          >
            <span class="crm-row-density-icon-wrap" aria-hidden="true">
              <svg class="crm-row-density-icon" viewBox="0 0 20 20" width="18" height="18">
                <rect x="2" y="7.5" width="16" height="1.5" rx="0.5" fill="currentColor" />
                <rect x="2" y="12.5" width="16" height="1.5" rx="0.5" fill="currentColor" />
              </svg>
            </span>
          </el-button></el-tooltip>
      </div>
    </Teleport>

    <el-table
      v-bind="tableAttrs"
      ref="innerTableRef"
      :border="props.border"
      :row-class-name="mergedRowClassName"
      style="width: 100%"
      @row-click="onInternalRowClick"
      @header-dragend="onInternalHeaderDragend"
      :header-cell-class-name="mergedHeaderCellClassName"
    >
      <template v-if="configMode">
        <el-table-column
          v-for="col in orderedVisibleColumns"
          :key="col.key"
          :column-key="col.key"
          :type="col.type"
          :prop="col.prop"
          :label="col.label"
          :width="col.width"
          :min-width="col.minWidth"
          :fixed="col.fixed"
          :align="col.align"
          :sortable="col.sortable"
          :formatter="col.formatter"
          :show-overflow-tooltip="isCrmListCopyableColumn(col) ? false : col.showOverflowTooltip"
          :class-name="col.className"
          :label-class-name="col.labelClassName"
          :resizable="col.resizable !== false && col.type !== 'selection'"
          :reserve-selection="col.type === 'selection' ? col.reserveSelection : undefined"
        >
          <template v-if="slots[headerSlotName(col)]" #header>
            <slot :name="headerSlotName(col)" />
          </template>
          <template v-if="col.type !== 'selection' && col.type !== 'index' && slots[slotName(col)]" #default="scope">
            <slot :name="slotName(col)" v-bind="scope" />
          </template>
          <template
            v-else-if="col.type !== 'selection' && col.type !== 'index' && isCrmListCopyableColumn(col)"
            #default="scope"
          >
            <CrmListCopyableTextCell
              :text="resolveCrmListCopyableCellValue(scope.row as Record<string, unknown>, col)"
            />
          </template>
        </el-table-column>
      </template>
      <slot v-else />
      <template v-if="slots.empty" #empty>
        <slot name="empty" />
      </template>
    </el-table>

    <div
      v-show="colResizeGuideVisible"
      class="crm-col-resize-hit"
      :class="{ 'is-dragging': colResizeDragging, 'is-hot': colResizeHot }"
      :style="colResizeHitStyle"
      @pointerdown="onColResizeHitDown"
    >
      <span class="crm-col-resize-hit__line" aria-hidden="true" />
    </div>

    <el-drawer
      v-if="configMode"
      v-model="settingsOpen"
      title="列显示与顺序"
      direction="rtl"
      size="min(360px, 92vw)"
      append-to-body
      class="crm-data-table-column-drawer"
    >
      <p class="crm-data-table__drawer-hint">勾选控制显示；拖拽调整顺序。拖表头可调列宽。列显示、顺序与列宽保存在本机，下次打开仍有效；恢复默认时一并清除。</p>
      <div v-if="pinnedStartDefs.length" class="crm-data-table__drawer-section">
        <div class="crm-data-table__drawer-section-title">固定在前</div>
        <ul class="crm-data-table__drawer-list">
          <li v-for="c in pinnedStartDefs" :key="c.key" class="crm-data-table__drawer-row is-static">
            <span class="crm-data-table__drawer-label">{{ drawerColumnLabel(c) }}</span>
            <el-tag size="small" type="info" effect="plain">固定</el-tag>
          </li>
        </ul>
      </div>
      <div class="crm-data-table__drawer-section">
        <div class="crm-data-table__drawer-section-title">数据列（可排序）</div>
        <ul class="crm-data-table__drawer-list">
          <li
            v-for="(c, idx) in settingsRows"
            :key="c.key"
            class="crm-data-table__drawer-row"
            :class="{ 'is-dragging': dragIndex === idx, 'is-locked': c.reorderable === false }"
            :draggable="c.reorderable !== false"
            @dragstart="onDragStart(idx, c)"
            @dragend="dragIndex = null"
            @drop="onDrop(idx)"
            @dragover.prevent
          >
            <span class="crm-data-table__drawer-grip" aria-hidden="true">⋮⋮</span>
            <el-checkbox
              v-if="c.hideable !== false"
              :model-value="!persist.isHidden(c.key)"
              @update:model-value="(v: CheckboxValue) => persist.setColumnVisible(c.key, c, v === true)"
            />
            <span v-else class="crm-data-table__drawer-checkbox-spacer" />
            <span class="crm-data-table__drawer-label">{{ drawerColumnLabel(c) }}</span>
            <el-tag v-if="c.hideable === false" size="small" type="info" effect="plain">必选</el-tag>
          </li>
        </ul>
      </div>
      <div v-if="pinnedEndDefs.length" class="crm-data-table__drawer-section">
        <div class="crm-data-table__drawer-section-title">固定在后</div>
        <ul class="crm-data-table__drawer-list">
          <li v-for="c in pinnedEndDefs" :key="c.key" class="crm-data-table__drawer-row is-static">
            <span class="crm-data-table__drawer-label">{{ drawerColumnLabel(c) }}</span>
            <el-tag size="small" type="info" effect="plain">固定</el-tag>
          </li>
        </ul>
      </div>
      <template #footer>
        <div class="crm-data-table__drawer-footer">
          <el-button @click="settingsOpen = false">关闭</el-button>
          <el-button type="warning" plain @click="onResetColumns">
            <el-icon class="crm-data-table__toolbar-icon"><RefreshLeft /></el-icon>
            恢复默认
          </el-button>
        </div>
      </template>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, toRef, useAttrs, useSlots, watch, type StyleValue } from 'vue'
import { ElMessage } from 'element-plus'
import { RefreshLeft, Setting } from '@element-plus/icons-vue'
import { usePersistedTableColumns, type CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import {
  readPersistedRowDensity,
  writePersistedRowDensity,
  type CrmTableRowDensity
} from '@/utils/crmTableRowDensityStorage'
import CrmListCopyableTextCell from '@/components/CrmListCopyableTextCell.vue'
import {
  isCrmListCopyableColumn,
  resolveCrmListCopyableCellValue
} from '@/utils/crmListCopyableField'
import {
  mergeCrmListRowClassName,
  resolveCrmTableRowKey,
  type CrmTableRowKeyProp
} from '@/utils/crmListClickedRow'
import {
  CRM_COL_RESIZE_HIT_PX,
  clampColumnResizeWidth,
  isHeaderColumnResizable,
  isHeaderResizeControlTarget,
  isPointerOverTableHeader,
  pickNearestBoundaryIndex,
  resolveHeaderResizeMinWidth,
  type HeaderResizeBoundary
} from '@/utils/crmTableHeaderResizeGuide'

type CheckboxValue = boolean | string | number

/**
 * 项目统一列表表格：与 .crm-items-table 视觉一致；默认 `border` 以支持表头拖拽调列宽。
 * 可选：传入 `columnLayoutKey` + `columns` 启用列显隐、顺序、用户拖过的列宽与 localStorage 持久化。
 */
defineOptions({ name: 'CrmDataTable', inheritAttrs: false })

const props = withDefaults(
  defineProps<{
    border?: boolean
    embedded?: boolean
    columnLayoutKey?: string
    columns?: CrmTableColumnDef[]
    showColumnSettings?: boolean
    /** 无 columnLayoutKey 时用于行高密度 localStorage 的独立键 */
    rowDensityStorageKey?: string
    /** 为 false 时隐藏「紧密 / 适中」切换（仍按 storageKey 应用已保存密度） */
    showRowDensityToggle?: boolean
    /** 页脚锚点（列设置旁），「紧密 / 适中」切换 Teleport 到此；不传则不显示切换（仍按 storageKey 应用已保存密度） */
    densityToggleAnchorEl?: HTMLElement | null
  }>(),
  { border: true, embedded: false, showColumnSettings: true, showRowDensityToggle: true }
)

const storageKey = computed(() =>
  String(props.columnLayoutKey ?? props.rowDensityStorageKey ?? '')
    .trim()
)

const rowDensity = ref<CrmTableRowDensity>('compact')

watch(
  storageKey,
  (k) => {
    rowDensity.value = readPersistedRowDensity(k)
  },
  { immediate: true }
)

function setRowDensity(d: CrmTableRowDensity) {
  rowDensity.value = d
  writePersistedRowDensity(storageKey.value, d)
}

const rowDensityClass = computed(() =>
  rowDensity.value === 'compact' ? 'crm-items-table--density-compact' : 'crm-items-table--density-medium'
)

const showDensityControls = computed(
  () =>
    storageKey.value.length > 0 &&
    props.showRowDensityToggle !== false &&
    props.densityToggleAnchorEl != null
)

const densityTeleportTarget = computed(() => props.densityToggleAnchorEl ?? null)

const attrs = useAttrs()
const slots = useSlots()

const clickedRowKey = ref<string | null>(null)

const rowKeyProp = computed<CrmTableRowKeyProp>(() => {
  const raw = attrs.rowKey ?? attrs['row-key']
  if (typeof raw === 'function') return raw as (row: Record<string, unknown>) => string
  if (typeof raw === 'string') return raw
  return undefined
})

const userRowClassName = computed(
  () => attrs.rowClassName ?? attrs['row-class-name']
)

watch(
  () => attrs.data,
  (data) => {
    if (!clickedRowKey.value) return
    const rows = Array.isArray(data) ? data : []
    const stillThere = rows.some(
      (r) => resolveCrmTableRowKey(r as Record<string, unknown>, rowKeyProp.value) === clickedRowKey.value
    )
    if (!stillThere) clickedRowKey.value = null
  },
  { deep: false }
)

function mergedRowClassName(ctx: { row: Record<string, unknown>; rowIndex: number }): string {
  return mergeCrmListRowClassName(
    userRowClassName.value as string | ((c: { row: Record<string, unknown>; rowIndex: number }) => string) | undefined,
    ctx,
    clickedRowKey.value,
    rowKeyProp.value
  )
}

function onInternalRowClick(row: Record<string, unknown>, column: unknown, event: Event) {
  const key = resolveCrmTableRowKey(row, rowKeyProp.value)
  clickedRowKey.value = key || null
  const handler = attrs.onRowClick as ((...args: unknown[]) => void) | undefined
  handler?.(row, column, event)
}

const tableAttrs = computed(() => {
  const a = { ...attrs } as Record<string, unknown>
  delete a.class
  delete a.style
  delete a.columns
  delete a.columnLayoutKey
  delete a.showColumnSettings
  delete a.rowDensityStorageKey
  delete a.showRowDensityToggle
  delete a.densityToggleAnchorEl
  delete a.rowClassName
  delete a['row-class-name']
  delete a.onRowClick
  delete a.onHeaderDragend
  delete a.headerCellClassName
  delete a['header-cell-class-name']
  return a
})

const wrapperClass = computed(() => attrs.class as string | Record<string, boolean> | Array<string> | undefined)
const wrapperStyle = computed(() => attrs.style as StyleValue | undefined)

const columnsRef = computed<CrmTableColumnDef[]>(() => props.columns ?? [])

const configMode = computed(() => !!(props.columnLayoutKey?.trim() && props.columns?.length))

const persist = usePersistedTableColumns(toRef(props, 'columnLayoutKey'), columnsRef)

function onInternalHeaderDragend(
  newWidth: number,
  oldWidth: number,
  column: { columnKey?: string; property?: string },
  event: MouseEvent
) {
  if (configMode.value) persist.applyHeaderDragWidth(column, newWidth)
  const handler = attrs.onHeaderDragend as ((...args: unknown[]) => void) | undefined
  handler?.(newWidth, oldWidth, column, event)
}

function mergedHeaderCellClassName(ctx: {
  column: { columnKey?: string; property?: string }
  columnIndex: number
  rowIndex: number
}) {
  const user = attrs.headerCellClassName ?? attrs['header-cell-class-name']
  let extra = ''
  if (typeof user === 'function') extra = String(user(ctx) ?? '')
  else if (typeof user === 'string') extra = user
  const key = ctx.column?.columnKey || ctx.column?.property
  const ours = key ? `crm-col-key-${key}` : ''
  return [ours, extra].filter(Boolean).join(' ')
}

const rootRef = ref<HTMLElement | null>(null)
const colResizeGuideVisible = ref(false)
const colResizeHot = ref(false)
const colResizeDragging = ref(false)
const colResizeHitStyle = ref<{ left: string; top: string; height: string; width: string } | undefined>()
const activeBoundary = ref<HeaderResizeBoundary | null>(null)

type ColResizeDragState = {
  key: string
  property?: string
  startWidth: number
  minWidth: number
  startX: number
  startRight: number
  top: number
  height: number
}

let colResizeDrag: ColResizeDragState | null = null

function collectResizeBoundaries(): HeaderResizeBoundary[] {
  const root = rootRef.value
  if (!root || props.border === false) return []
  const fixedRight = root.querySelector('.el-table__fixed-right') as HTMLElement | null
  const clipRight = fixedRight?.getBoundingClientRect().left ?? Number.POSITIVE_INFINITY
  const defs = configMode.value ? persist.orderedVisibleColumns.value : []
  const out: HeaderResizeBoundary[] = []

  const pushTh = (th: HTMLElement, def: CrmTableColumnDef | undefined, fallbackKey: string) => {
    if (th.classList.contains('gutter') || th.classList.contains('el-table-column--selection')) return
    if (th.classList.contains('op-col')) return
    if (def && !isHeaderColumnResizable(def)) return
    if (!def && th.classList.contains('el-table-column--selection')) return
    const rect = th.getBoundingClientRect()
    if (rect.width < 12 || rect.height < 8) return
    const right = Math.min(rect.right, clipRight)
    if (right - rect.left < 20) return
    out.push({
      key: def?.key ?? fallbackKey,
      property: def?.prop,
      minWidth: resolveHeaderResizeMinWidth(def, rect.width),
      startWidth: rect.width,
      right,
      top: rect.top,
      height: rect.height
    })
  }

  if (defs.length) {
    for (const def of defs) {
      if (!isHeaderColumnResizable(def)) continue
      const nodes = [
        ...root.querySelectorAll<HTMLElement>(`th.crm-col-key-${CSS.escape(def.key)}`)
      ]
      if (!nodes.length) continue
      const th =
        nodes.find((n) => n.closest('.el-table__header-wrapper') && !n.closest('.el-table__fixed')) ??
        nodes[0]!
      pushTh(th, def, def.key)
    }
    return out
  }

  const ths = [
    ...root.querySelectorAll<HTMLElement>('.el-table__header-wrapper thead th.el-table__cell')
  ].filter((th) => !th.classList.contains('gutter'))
  ths.forEach((th, i) => pushTh(th, undefined, `idx-${i}`))
  return out
}

function placeHitFromBoundary(b: HeaderResizeBoundary, rootRect: DOMRect, hot: boolean) {
  colResizeHitStyle.value = {
    left: `${b.right - rootRect.left - CRM_COL_RESIZE_HIT_PX}px`,
    top: `${b.top - rootRect.top}px`,
    height: `${b.height}px`,
    width: `${CRM_COL_RESIZE_HIT_PX}px`
  }
  colResizeHot.value = hot
  colResizeGuideVisible.value = true
  activeBoundary.value = b
}

function hideColResizeGuide() {
  if (colResizeDragging.value) return
  colResizeGuideVisible.value = false
  colResizeHot.value = false
  activeBoundary.value = null
}

function onColResizePointerMove(e: PointerEvent) {
  if (colResizeDragging.value) return
  if (props.border === false) return
  if (isHeaderResizeControlTarget(e.target)) {
    hideColResizeGuide()
    return
  }
  if (!isPointerOverTableHeader(e.target)) {
    hideColResizeGuide()
    return
  }
  const root = rootRef.value
  if (!root) return
  const boundaries = collectResizeBoundaries()
  const idx = pickNearestBoundaryIndex(
    boundaries.map((b) => b.right),
    e.clientX
  )
  if (idx < 0) {
    hideColResizeGuide()
    return
  }
  const b = boundaries[idx]!
  const hot = e.clientX <= b.right && b.right - e.clientX <= CRM_COL_RESIZE_HIT_PX
  placeHitFromBoundary(b, root.getBoundingClientRect(), hot)
}

function onColResizePointerLeave(e: PointerEvent) {
  const next = e.relatedTarget
  if (next instanceof Node && rootRef.value?.contains(next)) return
  hideColResizeGuide()
}

function onColResizeHitDown(e: PointerEvent) {
  if (e.button !== 0) return
  const b = activeBoundary.value
  const root = rootRef.value
  if (!b || !root) return
  e.preventDefault()
  e.stopPropagation()
  colResizeDragging.value = true
  colResizeHot.value = true
  colResizeDrag = {
    key: b.key,
    property: b.property,
    startWidth: b.startWidth,
    minWidth: b.minWidth,
    startX: e.clientX,
    startRight: b.right,
    top: b.top,
    height: b.height
  }
  document.body.style.cursor = 'col-resize'
  document.body.style.userSelect = 'none'
  document.addEventListener('pointermove', onColResizeDocMove)
  document.addEventListener('pointerup', onColResizeDocUp)
}

function onColResizeDocMove(e: PointerEvent) {
  const drag = colResizeDrag
  const root = rootRef.value
  if (!drag || !root) return
  const width = clampColumnResizeWidth(drag.startWidth + (e.clientX - drag.startX), drag.minWidth)
  const right = drag.startRight + (width - drag.startWidth)
  const rootRect = root.getBoundingClientRect()
  colResizeHitStyle.value = {
    left: `${right - rootRect.left - CRM_COL_RESIZE_HIT_PX}px`,
    top: `${drag.top - rootRect.top}px`,
    height: `${drag.height}px`,
    width: `${CRM_COL_RESIZE_HIT_PX}px`
  }
}

function onColResizeDocUp(e: PointerEvent) {
  const drag = colResizeDrag
  document.removeEventListener('pointermove', onColResizeDocMove)
  document.removeEventListener('pointerup', onColResizeDocUp)
  document.body.style.cursor = ''
  document.body.style.userSelect = ''
  colResizeDragging.value = false
  colResizeDrag = null
  if (!drag) return
  const newWidth = clampColumnResizeWidth(drag.startWidth + (e.clientX - drag.startX), drag.minWidth)
  onInternalHeaderDragend(
    newWidth,
    drag.startWidth,
    { columnKey: drag.key, property: drag.property },
    e as unknown as MouseEvent
  )
  void nextTick(() => innerTableRef.value?.doLayout?.())
  hideColResizeGuide()
}

onBeforeUnmount(() => {
  document.removeEventListener('pointermove', onColResizeDocMove)
  document.removeEventListener('pointerup', onColResizeDocUp)
  document.body.style.cursor = ''
  document.body.style.userSelect = ''
})

const orderedVisibleColumns = computed(() => {
  if (!configMode.value) return []
  return persist.orderedVisibleColumns.value
})

function isPinnedStart(c: CrmTableColumnDef) {
  return c.pinned === 'start' || c.type === 'selection'
}
function isPinnedEnd(c: CrmTableColumnDef) {
  return c.pinned === 'end' || c.fixed === 'right'
}

const pinnedStartDefs = computed(() => (props.columns ?? []).filter(isPinnedStart))
const pinnedEndDefs = computed(() => (props.columns ?? []).filter(isPinnedEnd))

const settingsRows = computed(() => persist.settingsMiddleColumns.value)

const settingsOpen = ref(false)
const dragIndex = ref<number | null>(null)

function slotName(col: CrmTableColumnDef) {
  return `col-${col.key}` as const
}

function headerSlotName(col: CrmTableColumnDef) {
  return `col-${col.key}-header` as const
}

function drawerColumnLabel(c: CrmTableColumnDef) {
  if (c.label != null && String(c.label).trim() !== '') return c.label
  if (c.type === 'selection') return '勾选列'
  return c.key
}

function onDragStart(idx: number, c: CrmTableColumnDef) {
  if (c.reorderable === false) return
  dragIndex.value = idx
}

function onDrop(targetIdx: number) {
  const from = dragIndex.value
  dragIndex.value = null
  if (from === null || from === targetIdx) return
  const order = [...persist.middleOrder.value]
  const [moved] = order.splice(from, 1)
  let insertAt = targetIdx
  if (from < targetIdx) insertAt = targetIdx - 1
  order.splice(insertAt, 0, moved)
  persist.setMiddleOrder(order)
}

function onResetColumns() {
  persist.resetToDefault()
  void nextTick(() => innerTableRef.value?.doLayout?.())
  ElMessage.success('已恢复默认列布局')
}

const innerTableRef = ref<{
  clearSelection: () => void
  toggleRowSelection: (row: unknown, selected?: boolean) => void
  setCurrentRow: (row?: unknown) => void
  getSelectionRows?: () => unknown[]
  doLayout?: () => void
} | null>(null)

defineExpose({
  clearSelection: () => innerTableRef.value?.clearSelection(),
  toggleRowSelection: (row: unknown, selected?: boolean) =>
    innerTableRef.value?.toggleRowSelection(row, selected),
  setCurrentRow: (row?: unknown) => innerTableRef.value?.setCurrentRow(row),
  getSelectionRows: () => innerTableRef.value?.getSelectionRows?.(),
  resetColumnLayout: () => {
    persist.resetToDefault()
    void nextTick(() => innerTableRef.value?.doLayout?.())
  },
  clearClickedRow: () => {
    clickedRowKey.value = null
  },
  /** 外部触发打开「列设置」抽屉（如放到表格底栏按钮） */
  openColumnSettings: () => {
    if (!configMode.value) return
    settingsOpen.value = true
  },
  rowDensity,
  setRowDensity
})
</script>

<style scoped lang="scss">
.crm-data-table-root {
  position: relative;
}

.crm-col-resize-hit {
  position: absolute;
  z-index: 8;
  cursor: col-resize;
  touch-action: none;

  &:not(.is-hot):not(.is-dragging) {
    pointer-events: none;
  }
}

.crm-col-resize-hit__line {
  position: absolute;
  top: 10px;
  right: 0;
  bottom: 10px;
  width: 2px;
  height: auto;
  background: #ced1d1;
  opacity: 0.9;
  pointer-events: none;
}

.crm-col-resize-hit.is-hot .crm-col-resize-hit__line,
.crm-col-resize-hit.is-dragging .crm-col-resize-hit__line {
  opacity: 1;
}

.crm-data-table-root.is-col-resizing {
  cursor: col-resize;
  user-select: none;
}

.crm-data-table__toolbar {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  padding: 0 0 8px;
  min-height: 32px;
}

.crm-data-table__toolbar--embedded {
  padding-top: 4px;
}

.crm-data-table__toolbar-icon {
  margin-right: 4px;
  vertical-align: middle;
}

.crm-data-table__drawer-hint {
  margin: 0 0 16px;
  font-size: 12px;
  line-height: 1.5;
  color: var(--crm-text-secondary, rgba(100, 116, 139, 0.9));
}

.crm-data-table__drawer-section {
  margin-bottom: 18px;
}

.crm-data-table__drawer-section-title {
  font-size: 12px;
  font-weight: 600;
  color: var(--crm-text-muted, #64748b);
  margin-bottom: 8px;
  letter-spacing: 0.02em;
}

.crm-data-table__drawer-list {
  list-style: none;
  margin: 0;
  padding: 0;
  border: 1px solid var(--crm-border-panel, #e2e8f0);
  border-radius: 8px;
  overflow: hidden;
  background: var(--crm-layer-2, #fff);
}

.crm-data-table__drawer-row {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  border-bottom: 1px solid var(--crm-chrome-border, #e2e8f0);
  cursor: grab;
  font-size: 13px;
  color: var(--crm-text-primary, #0f172a);

  &:last-child {
    border-bottom: none;
  }

  &.is-static {
    cursor: default;
    background: var(--crm-layer-3, #f8fafc);
  }

  &.is-dragging {
    opacity: 0.55;
  }

  &.is-locked {
    cursor: default;
    opacity: 0.9;
  }
}

.crm-data-table__drawer-grip {
  font-size: 10px;
  letter-spacing: -2px;
  color: var(--crm-text-muted, #94a3b8);
  user-select: none;
  width: 22px;
  flex-shrink: 0;
}

.crm-data-table__drawer-checkbox-spacer {
  width: 14px;
  flex-shrink: 0;
}

.crm-data-table__drawer-label {
  flex: 1;
  min-width: 0;
}

.crm-data-table__drawer-footer {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  flex-wrap: wrap;
}

</style>

<!-- Teleport 到表格外时 scoped 不生效；与列表页「列设置」齿轮同款 link primary + list-settings-btn -->
<style lang="scss">
.crm-row-density-toggle-group {
  display: inline-flex;
  align-items: center;
  gap: 0;
  vertical-align: middle;

  & > .el-tooltip + .el-tooltip {
    margin-left: 0;
  }
}

.crm-row-density-toggle-group .list-settings-btn {
  padding: 4px 6px !important;
  min-width: 28px;
}

.crm-row-density-toggle-group .list-settings-btn.crm-row-density-settings-btn {
  padding: 4px 4px !important;
  min-width: 0;
}

.crm-row-density-icon-wrap {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  line-height: 0;
}

.crm-row-density-icon {
  display: block;
}

.crm-row-density-settings-btn.is-row-density-muted {
  opacity: 0.42;
}

.crm-row-density-settings-btn:not(.is-row-density-muted):hover {
  opacity: 1;
}
</style>
