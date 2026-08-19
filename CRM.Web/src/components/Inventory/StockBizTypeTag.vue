<template>
  <span class="stock-biz-type-cell">
    <span :class="['biz-type-tag', tagClass]">{{ displayLabel }}</span>
    <CustomsDeclarationIconLink
      v-if="showCustomsIcon"
      :declaration-id="customsDeclarationId"
      :declaration-code="customsDeclarationCode"
    />
  </span>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { resolveStockInTypeLabelKey } from '@/constants/stockInType'
import { resolveStockOutTypeLabelKey } from '@/constants/stockOutType'
import CustomsDeclarationIconLink from '@/components/Customs/CustomsDeclarationIconLink.vue'

const props = defineProps<{
  /** 入库类型 / 出库类型 */
  biz: 'in' | 'out'
  type?: number | null
  /** 报关入库时关联报关单（有 ID 时在标签右侧显示海关图标） */
  customsDeclarationId?: string | null
  customsDeclarationCode?: string | null
}>()

const { t } = useI18n()

const inLabelKey = computed(() => resolveStockInTypeLabelKey(props.type))
const outLabelKey = computed(() => resolveStockOutTypeLabelKey(props.type))

const tagClass = computed(() => {
  const key = props.biz === 'in' ? inLabelKey.value : outLabelKey.value
  if (key === 'purchase' || key === 'sales') return 'biz-type-tag--10'
  if (key === 'customs') return 'biz-type-tag--20'
  if (key === 'return') return 'biz-type-tag--30'
  if (key === 'scrap') return 'biz-type-tag--40'
  if (key === 'transfer') return 'biz-type-tag--3'
  return 'biz-type-tag--unknown'
})

const showCustomsIcon = computed(() => {
  const isCustomsType =
    props.biz === 'in' ? inLabelKey.value === 'customs' : outLabelKey.value === 'customs'
  return isCustomsType && !!(props.customsDeclarationId || '').trim()
})

const displayLabel = computed(() => {
  if (props.biz === 'in') {
    return t(`stockInList.stockInTypeLabels.${inLabelKey.value}`)
  }
  return t(`stockOutList.stockOutTypeLabels.${outLabelKey.value}`)
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.stock-biz-type-cell {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  max-width: 100%;
  white-space: nowrap;
}

.biz-type-tag {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  line-height: 1.4;
  white-space: nowrap;
}

/* 10 销售/采购 */
.biz-type-tag--10 {
  background: rgba(0, 212, 255, 0.15);
  color: $cyan-primary;
}

/* 20 报关 */
.biz-type-tag--20 {
  background: rgba(255, 184, 77, 0.18);
  color: #ffb84d;
}

/* 30 退货 */
.biz-type-tag--30 {
  background: rgba(156, 89, 182, 0.18);
  color: #9c59b6;
}

/* 40 报废 */
.biz-type-tag--40 {
  background: rgba(201, 87, 69, 0.18);
  color: #c95745;
}

/* 3 移库 */
.biz-type-tag--3 {
  background: rgba(64, 158, 255, 0.16);
  color: #409eff;
}

.biz-type-tag--unknown {
  background: rgba(148, 163, 184, 0.22);
  color: #64748b;
}
</style>
