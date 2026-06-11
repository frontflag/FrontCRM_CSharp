<template>
  <div class="vendor-extend-cell" :class="{ 'is-expanded': expanded }">
    <template v-if="masked">
      <span class="vendor-extend-cell__value">—</span>
    </template>
    <template v-else-if="expanded">
      <div
        class="vendor-extend-cell__cols"
        :style="{ gridTemplateColumns: subColGridTemplateColumns }"
      >
        <span
          v-for="f in fieldKeys"
          :key="f"
          class="vendor-extend-cell__col"
          :title="displayValue(f)"
        >
          {{ displayValue(f) }}
        </span>
      </div>
    </template>
    <template v-else>
      <span class="vendor-extend-cell__value vendor-extend-cell__value--single" :title="displayValue(activeField)">
        {{ displayValue(activeField) }}
      </span>
    </template>
  </div>
</template>

<script setup lang="ts">
import {
  VENDOR_EXTEND_FIELD_KEYS,
  type VendorExtendFieldKey,
  type VendorExtendRowSlice
} from '@/constants/listVendorExtendColumnSpec'
import { pickVendorExtendFieldValue, useVendorExtendColumn } from '@/composables/useVendorExtendColumn'

const props = defineProps<{
  row: VendorExtendRowSlice
  activeField: VendorExtendFieldKey
  masked?: boolean
  emptyText?: string
}>()

const fieldKeys = VENDOR_EXTEND_FIELD_KEYS
const { expanded, subColGridTemplateColumns } = useVendorExtendColumn()

function displayValue(field: VendorExtendFieldKey): string {
  const raw = pickVendorExtendFieldValue(props.row, field)
  if (raw) return raw
  return props.emptyText ?? '—'
}
</script>
