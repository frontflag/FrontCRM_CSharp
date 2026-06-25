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
      <span
        class="vendor-extend-cell__value vendor-extend-cell__value--single"
        :title="displayValue(activeField)"
      >
        {{ displayValue(activeField) }}
      </span>
    </template>
  </div>
</template>

<script setup lang="ts">
import {
  VENDOR_RECEIVING_BANK_EXTEND_FIELD_KEYS,
  type VendorReceivingBankExtendFieldKey,
  type VendorReceivingBankExtendRowSlice
} from '@/constants/listVendorReceivingBankExtendColumnSpec'
import {
  pickVendorReceivingBankExtendFieldValue,
  useVendorReceivingBankExtendColumn
} from '@/composables/useVendorReceivingBankExtendColumn'

const props = defineProps<{
  row: VendorReceivingBankExtendRowSlice
  activeField: VendorReceivingBankExtendFieldKey
  masked?: boolean
  emptyText?: string
}>()

const fieldKeys = VENDOR_RECEIVING_BANK_EXTEND_FIELD_KEYS
const { expanded, subColGridTemplateColumns } = useVendorReceivingBankExtendColumn()

function displayValue(field: VendorReceivingBankExtendFieldKey): string {
  const raw = pickVendorReceivingBankExtendFieldValue(props.row, field)
  if (raw) return raw
  return props.emptyText ?? '—'
}
</script>
