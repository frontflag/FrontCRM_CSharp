<template>
  <div
    class="crm-data-table-root"
    :class="[
      props.embedded ? 'crm-items-table crm-data-table crm-data-table--embedded' : 'table-wrapper crm-items-table crm-data-table',
      wrapperClass
    ]"
    :style="wrapperStyle"
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
    <el-table v-bind="tableAttrs" ref="innerTableRef" :border="props.border" style="width: 100%">
      <template v-if="configMode">
        <el-table-column
          v-for="col in orderedVisibleColumns"
          :key="col.key"
          :type="col.type"
          :prop="col.prop"
          :label="col.label"
          :width="col.width"
          :min-width="col.minWidth"
          :fixed="col.fixed"
          :align="col.align"
          :sortable="col.sortable"
          :formatter="col.formatter"
          :show-overflow-tooltip="col.showOverflowTooltip"
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
        </el-table-column>
      </template>
      <slot v-else />
    </el-table>

    <el-drawer
      v-if="configMode"
      v-model="settingsOpen"
      title="列显示与顺序"
      direction="rtl"
      size="min(360px, 92vw)"
      append-to-body
      class="crm-data-table-column-drawer"
    >
      <p class="crm-data-table__drawer-hint">勾选控制显示；拖拽调整顺序。设置保存在本机，下次打开仍有效。</p>
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
import { computed, ref, toRef, useAttrs, useSlots, type StyleValue } from 'vue'
import { ElMessage } from 'element-plus'
import { RefreshLeft, Setting } from '@element-plus/icons-vue'
import { usePersistedTableColumns, type CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

type CheckboxValue = boolean | string | number

/**
 * 项目统一列表表格：与 .crm-items-table 视觉一致；默认 `border` 以支持表头拖拽调列宽。
 * 可选：传入 `columnLayoutKey` + `columns` 启用列显隐、顺序与 localStorage 持久化。
 */
defineOptions({ name: 'CrmDataTable', inheritAttrs: false })

const props = withDefaults(
  defineProps<{
    border?: boolean
    embedded?: boolean
    columnLayoutKey?: string
    columns?: CrmTableColumnDef[]
    showColumnSettings?: boolean
  }>(),
  { border: true, embedded: false, showColumnSettings: true }
)

const attrs = useAttrs()
const slots = useSlots()

const tableAttrs = computed(() => {
  const a = { ...attrs } as Record<string, unknown>
  delete a.class
  delete a.style
  delete a.columns
  delete a.columnLayoutKey
  delete a.showColumnSettings
  return a
})

const wrapperClass = computed(() => attrs.class as string | Record<string, boolean> | Array<string> | undefined)
const wrapperStyle = computed(() => attrs.style as StyleValue | undefined)

const columnsRef = computed<CrmTableColumnDef[]>(() => props.columns ?? [])

const configMode = computed(() => !!(props.columnLayoutKey?.trim() && props.columns?.length))

const persist = usePersistedTableColumns(toRef(props, 'columnLayoutKey'), columnsRef)

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
  ElMessage.success('已恢复默认列布局')
}

const innerTableRef = ref<{
  clearSelection: () => void
  toggleRowSelection: (row: unknown, selected?: boolean) => void
  setCurrentRow: (row?: unknown) => void
  getSelectionRows?: () => unknown[]
} | null>(null)

defineExpose({
  clearSelection: () => innerTableRef.value?.clearSelection(),
  toggleRowSelection: (row: unknown, selected?: boolean) =>
    innerTableRef.value?.toggleRowSelection(row, selected),
  setCurrentRow: (row?: unknown) => innerTableRef.value?.setCurrentRow(row),
  getSelectionRows: () => innerTableRef.value?.getSelectionRows?.(),
  resetColumnLayout: () => persist.resetToDefault(),
  /** 外部触发打开「列设置」抽屉（如放到表格底栏按钮） */
  openColumnSettings: () => {
    if (!configMode.value) return
    settingsOpen.value = true
  }
})
</script>

<style scoped lang="scss">
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
