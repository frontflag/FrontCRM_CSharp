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
import { StockInTypeCode } from '@/constants/stockInType'
import { StockOutTypeCode } from '@/constants/stockOutType'
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

const normalizedType = computed(() => {
  const n = Number(props.type)
  if (props.biz === 'in') {
    if (n === 1) return StockInTypeCode.Purchase
    if (n === 2) return StockInTypeCode.Return
    if (n === 4) return StockInTypeCode.Scrap
    if (
      n === StockInTypeCode.Purchase ||
      n === StockInTypeCode.Customs ||
      n === StockInTypeCode.Return ||
      n === StockInTypeCode.Scrap
    ) {
      return n
    }
    return StockInTypeCode.Purchase
  }
  if (
    n === StockOutTypeCode.Sales ||
    n === StockOutTypeCode.Customs ||
    n === StockOutTypeCode.Return ||
    n === StockOutTypeCode.Scrap
  ) {
    return n
  }
  return StockOutTypeCode.Sales
})

const tagClass = computed(() => `biz-type-tag--${normalizedType.value}`)

const showCustomsIcon = computed(() => {
  const n = normalizedType.value
  const isCustomsType =
    props.biz === 'in' ? n === StockInTypeCode.Customs : n === StockOutTypeCode.Customs
  return isCustomsType && !!(props.customsDeclarationId || '').trim()
})

const displayLabel = computed(() => {
  const n = normalizedType.value
  if (props.biz === 'in') {
    if (n === StockInTypeCode.Customs) return t('stockInList.stockInTypeLabels.customs')
    if (n === StockInTypeCode.Return) return t('stockInList.stockInTypeLabels.return')
    if (n === StockInTypeCode.Scrap) return t('stockInList.stockInTypeLabels.scrap')
    return t('stockInList.stockInTypeLabels.purchase')
  }
  if (n === StockOutTypeCode.Customs) return t('stockOutList.stockOutTypeLabels.customs')
  if (n === StockOutTypeCode.Return) return t('stockOutList.stockOutTypeLabels.return')
  if (n === StockOutTypeCode.Scrap) return t('stockOutList.stockOutTypeLabels.scrap')
  return t('stockOutList.stockOutTypeLabels.sales')
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
</style>
