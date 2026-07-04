<template>
  <div class="customer-extend-cell" :class="{ 'is-expanded': expanded }">
    <template v-if="expanded">
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
  WRITE_OFF_RECEIPT_DATE_FIELD_KEYS,
  type WriteOffReceiptDateFieldKey,
  type WriteOffReceiptDateRowSlice
} from '@/constants/writeOffReceiptDateExtendColumnSpec'
import { useWriteOffReceiptDateExtendColumn } from '@/composables/useWriteOffReceiptDateExtendColumn'

const props = defineProps<{
  row: WriteOffReceiptDateRowSlice
  activeField: WriteOffReceiptDateFieldKey
  formatDate: (value?: string | null) => string
}>()

const fieldKeys = WRITE_OFF_RECEIPT_DATE_FIELD_KEYS
const { expanded, subColGridTemplateColumns } = useWriteOffReceiptDateExtendColumn()

function displayValue(field: WriteOffReceiptDateFieldKey): string {
  const raw = field === 'earliest' ? props.row.earliestReceiptDate : props.row.latestReceiptDate
  return props.formatDate(raw ?? undefined)
}
</script>
