<template>
  <el-table-column v-bind="columnAttrs" :prop="prop">
    <template v-if="$slots.header" #header>
      <slot name="header" />
    </template>
    <template #default="scope">
      <slot v-bind="scope">
        <CrmListCopyableTextCell :text="cellText(scope.row as Record<string, unknown>)" :empty-text="emptyText" />
      </slot>
    </template>
  </el-table-column>
</template>

<script setup lang="ts">
import { computed, useAttrs } from 'vue'
import { pickCrmCopyableRowField } from '@/utils/crmListCopyableField'

defineOptions({ inheritAttrs: false })

const props = withDefaults(
  defineProps<{
    /** 行字段名（pn / brand / purchasePn / freightForwarderOrderNo 等） */
    prop: string
    emptyText?: string
  }>(),
  { emptyText: '—' }
)

const attrs = useAttrs()

const columnAttrs = computed(() => {
  const { showOverflowTooltip, 'show-overflow-tooltip': _sot, ...rest } = attrs as Record<string, unknown>
  void showOverflowTooltip
  void _sot
  return rest
})

function cellText(row: Record<string, unknown>): string {
  return pickCrmCopyableRowField(row, props.prop)
}
</script>
