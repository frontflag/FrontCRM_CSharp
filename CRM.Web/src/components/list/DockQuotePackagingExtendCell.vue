<template>
  <div
    class="customer-extend-cell dock-quote-extend-cell dock-quote-packaging-extend-cell"
    :class="{ 'is-expanded': expanded }"
  >
    <template v-if="expanded">
      <span
        class="dock-quote-extend-cell__toggle-spacer"
        aria-hidden="true"
        :style="toggleSpacerStyle"
      />
      <div
        class="customer-extend-cell__cols dock-quote-extend-cell__cols"
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
      <span
        class="customer-extend-cell__value customer-extend-cell__value--single"
        :title="displayValue(activeField)"
      >
        {{ displayValue(activeField) }}
      </span>
    </template>
  </div>
</template>

<script setup lang="ts">
import {
  DOCK_QUOTE_PACKAGING_EXTEND_FIELD_KEYS,
  DOCK_QUOTE_PACKAGING_EXTEND_TOGGLE_RESERVE_PX,
  pickDockQuotePackagingField,
  type DockQuotePackagingExtendFieldKey
} from '@/constants/listDockQuotePackagingExtendColumnSpec'
import { useDockQuotePackagingExtendColumn } from '@/composables/useDockQuotePackagingExtendColumn'

const props = defineProps<{
  row: Record<string, unknown>
  activeField: DockQuotePackagingExtendFieldKey
  emptyText?: string
}>()

const fieldKeys = DOCK_QUOTE_PACKAGING_EXTEND_FIELD_KEYS
const { expanded, subColGridTemplateColumns } = useDockQuotePackagingExtendColumn()

const toggleSpacerStyle = {
  flex: `0 0 ${DOCK_QUOTE_PACKAGING_EXTEND_TOGGLE_RESERVE_PX}px`,
  width: `${DOCK_QUOTE_PACKAGING_EXTEND_TOGGLE_RESERVE_PX}px`,
  minWidth: `${DOCK_QUOTE_PACKAGING_EXTEND_TOGGLE_RESERVE_PX}px`
}

function displayValue(field: DockQuotePackagingExtendFieldKey): string {
  const v = pickDockQuotePackagingField(props.row, field)
  return v || (props.emptyText ?? '—')
}
</script>
