<template>
  <div class="customer-extend-cell" :class="{ 'is-expanded': expanded }">
    <template v-if="masked">
      <span class="customer-extend-cell__value">—</span>
    </template>
    <template v-else-if="expanded">
      <div
        class="customer-extend-cell__cols"
        :style="{ gridTemplateColumns: subColGridTemplateColumns }"
      >
        <span
          v-for="f in fieldKeys"
          :key="f"
          class="customer-extend-cell__col"
          :title="displayValue(f)"
        >
          {{ displayValue(f) }}
        </span>
      </div>
    </template>
    <template v-else>
      <span class="customer-extend-cell__value customer-extend-cell__value--single" :title="displayValue(activeField)">
        {{ displayValue(activeField) }}
      </span>
    </template>
  </div>
</template>

<script setup lang="ts">
import {
  CUSTOMER_EXTEND_FIELD_KEYS,
  type CustomerExtendFieldKey,
  type CustomerExtendRowSlice
} from '@/constants/listCustomerExtendColumnSpec'
import { pickCustomerExtendFieldValue, useCustomerExtendColumn } from '@/composables/useCustomerExtendColumn'

const props = defineProps<{
  row: CustomerExtendRowSlice
  activeField: CustomerExtendFieldKey
  masked?: boolean
  emptyText?: string
}>()

const fieldKeys = CUSTOMER_EXTEND_FIELD_KEYS
const { expanded, subColGridTemplateColumns } = useCustomerExtendColumn()

function displayValue(field: CustomerExtendFieldKey): string {
  const raw = pickCustomerExtendFieldValue(props.row, field)
  if (raw) return raw
  return props.emptyText ?? '—'
}
</script>
