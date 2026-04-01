<template>
  <div
    v-if="!props.embedded"
    class="table-wrapper crm-items-table crm-data-table"
    :class="wrapperClass"
    :style="wrapperStyle"
  >
    <el-table v-bind="tableAttrs" ref="innerTableRef" :border="props.border" style="width: 100%">
      <slot />
    </el-table>
  </div>
  <div
    v-else
    class="crm-items-table crm-data-table crm-data-table--embedded"
    :class="wrapperClass"
    :style="wrapperStyle"
  >
    <el-table v-bind="tableAttrs" ref="innerTableRef" :border="props.border" style="width: 100%">
      <slot />
    </el-table>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, useAttrs, type StyleValue } from 'vue'

/**
 * 项目统一列表表格：与 .crm-items-table 视觉一致；默认 `border` 以支持表头拖拽调列宽。
 * 列间竖线由全局 `.crm-data-table` 隐藏（见 crm-unified-list.scss）。
 */
defineOptions({ name: 'CrmDataTable', inheritAttrs: false })

const props = withDefaults(
  defineProps<{
    border?: boolean
    embedded?: boolean
  }>(),
  { border: true, embedded: false }
)

const attrs = useAttrs()

const tableAttrs = computed(() => {
  const a = { ...attrs } as Record<string, unknown>
  delete a.class
  delete a.style
  return a
})

const wrapperClass = computed(() => attrs.class as string | Record<string, boolean> | Array<string> | undefined)
const wrapperStyle = computed(() => attrs.style as StyleValue | undefined)

/** Element Plus Table 实例（用于 clearSelection 等） */
const innerTableRef = ref<{
  clearSelection: () => void
  toggleRowSelection: (row: unknown, selected?: boolean) => void
  setCurrentRow: (row?: unknown) => void
  getSelectionRows?: () => unknown[]
} | null>(null)

/** 父组件 ref 可调用 clearSelection 等，与 el-table 一致 */
defineExpose({
  clearSelection: () => innerTableRef.value?.clearSelection(),
  toggleRowSelection: (row: unknown, selected?: boolean) =>
    innerTableRef.value?.toggleRowSelection(row, selected),
  setCurrentRow: (row?: unknown) => innerTableRef.value?.setCurrentRow(row),
  getSelectionRows: () => innerTableRef.value?.getSelectionRows?.()
})
</script>
