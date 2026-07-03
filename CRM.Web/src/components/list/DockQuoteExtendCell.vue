<template>
  <div class="customer-extend-cell dock-quote-extend-cell" :class="{ 'is-expanded': expanded }">
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
          :title="fieldTitle(f)"
        >
          <template v-if="f === 'freeShipping'">
            <span
              v-if="freeShippingState === 'yes'"
              class="dock-quote-free-ship-icon dock-quote-free-ship-icon--yes"
              :aria-label="t('common.dockQuoteExtendCol.freeShipping.yes')"
            >
              <el-icon><CircleCheck /></el-icon>
            </span>
            <span
              v-else-if="freeShippingState === 'no'"
              class="dock-quote-free-ship-icon dock-quote-free-ship-icon--no"
              :aria-label="t('common.dockQuoteExtendCol.freeShipping.no')"
            >
              <el-icon><CircleClose /></el-icon>
            </span>
            <span v-else class="dock-quote-free-ship-icon dock-quote-free-ship-icon--empty">{{ emptyText }}</span>
          </template>
          <template v-else>{{ originDisplay(f) }}</template>
        </span>
      </div>
    </template>
    <template v-else>
      <span
        class="customer-extend-cell__value customer-extend-cell__value--single"
        :title="fieldTitle(activeField)"
      >
        <template v-if="activeField === 'freeShipping'">
          <span
            v-if="freeShippingState === 'yes'"
            class="dock-quote-free-ship-icon dock-quote-free-ship-icon--yes"
            :aria-label="t('common.dockQuoteExtendCol.freeShipping.yes')"
          >
            <el-icon><CircleCheck /></el-icon>
          </span>
          <span
            v-else-if="freeShippingState === 'no'"
            class="dock-quote-free-ship-icon dock-quote-free-ship-icon--no"
            :aria-label="t('common.dockQuoteExtendCol.freeShipping.no')"
          >
            <el-icon><CircleClose /></el-icon>
          </span>
          <span v-else class="dock-quote-free-ship-icon dock-quote-free-ship-icon--empty">{{ emptyText }}</span>
        </template>
        <template v-else>{{ originDisplay(activeField) }}</template>
      </span>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { CircleCheck, CircleClose } from '@element-plus/icons-vue'
import {
  DOCK_QUOTE_EXTEND_FIELD_KEYS,
  DOCK_QUOTE_EXTEND_TOGGLE_RESERVE_PX,
  type DockQuoteExtendFieldKey
} from '@/constants/listDockQuoteExtendColumnSpec'
import { useDockQuoteExtendColumn } from '@/composables/useDockQuoteExtendColumn'
import {
  formatQuoteProductOrigin,
  parseQuoteFreeShippingState,
  parseQuoteProductOriginState,
  resolveQuoteFreeShipping,
  resolveQuotePackageOrigin,
  resolveQuoteWaferOrigin
} from '@/utils/quoteProductOrigin'

const props = defineProps<{
  row: Record<string, unknown>
  activeField: DockQuoteExtendFieldKey
  emptyText?: string
}>()

const { t } = useI18n()
const fieldKeys = DOCK_QUOTE_EXTEND_FIELD_KEYS
const { expanded, subColGridTemplateColumns } = useDockQuoteExtendColumn()

const toggleSpacerStyle = {
  flex: `0 0 ${DOCK_QUOTE_EXTEND_TOGGLE_RESERVE_PX}px`,
  width: `${DOCK_QUOTE_EXTEND_TOGGLE_RESERVE_PX}px`,
  minWidth: `${DOCK_QUOTE_EXTEND_TOGGLE_RESERVE_PX}px`
}

const emptyText = computed(() => props.emptyText ?? '—')

const originLabels = computed(() => ({
  us: t('common.dockQuoteExtendCol.origin.us'),
  nonUs: t('common.dockQuoteExtendCol.origin.nonUs'),
  pending: t('common.dockQuoteExtendCol.origin.pending'),
  na: emptyText.value
}))

const freeShippingState = computed(() =>
  parseQuoteFreeShippingState(resolveQuoteFreeShipping(props.row))
)

function resolveOriginRaw(field: 'waferOrigin' | 'packageOrigin'): unknown {
  return field === 'waferOrigin'
    ? resolveQuoteWaferOrigin(props.row)
    : resolveQuotePackageOrigin(props.row)
}

/** 列表展示：待确定 → "-" */
function originDisplay(field: DockQuoteExtendFieldKey): string {
  if (field === 'freeShipping') return ''
  const state = parseQuoteProductOriginState(resolveOriginRaw(field))
  if (state === 2) return '-'
  return formatQuoteProductOrigin(resolveOriginRaw(field), originLabels.value)
}

function fieldTitle(field: DockQuoteExtendFieldKey): string {
  if (field === 'freeShipping') {
    if (freeShippingState.value === 'yes') return t('common.dockQuoteExtendCol.freeShipping.yes')
    if (freeShippingState.value === 'no') return t('common.dockQuoteExtendCol.freeShipping.no')
    return emptyText.value
  }
  const state = parseQuoteProductOriginState(resolveOriginRaw(field))
  if (state === 2) return t('common.dockQuoteExtendCol.origin.pending')
  return originDisplay(field)
}
</script>

<style scoped lang="scss">
.dock-quote-free-ship-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  line-height: 1;
  font-size: 15px;
  vertical-align: middle;
}

.dock-quote-free-ship-icon--yes {
  color: #22c55e;
}

.dock-quote-free-ship-icon--no {
  color: #909399;
}

.dock-quote-free-ship-icon--empty {
  color: inherit;
  font-size: 12px;
}
</style>
