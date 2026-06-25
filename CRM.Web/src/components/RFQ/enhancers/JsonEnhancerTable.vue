<template>
  <el-table :data="rows" size="small" stripe border :class="tableClass" style="width: 100%">
    <el-table-column
      v-for="col in columns"
      :key="col.prop"
      :prop="col.prop"
      :label="columnLabel(col.labelKey)"
      :width="col.width"
      :min-width="col.minWidth"
    />
  </el-table>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { EnhancerColumnDef } from '@/utils/materialIntelJsonEnhancers'
import { resolveColumnLabel } from '@/utils/jsonLabels'

const props = defineProps<{
  rows: Record<string, unknown>[]
  columns: EnhancerColumnDef[]
  tableClass?: string
}>()

const { t, te, locale } = useI18n()

const tableClass = computed(() => props.tableClass ?? '')

function columnLabel(labelKey: string): string {
  return resolveColumnLabel(labelKey, t, te, locale.value)
}
</script>
