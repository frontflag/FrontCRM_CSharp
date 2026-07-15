<template>
  <div class="so-aggregate-table-wrap sell-order-item-stock-out-tab-table">
    <el-table v-if="items.length > 0" :data="items" size="small" stripe @row-dblclick="onRowDblClick">
      <el-table-column type="index" width="50" label="#" />
      <el-table-column
        :label="t('stockOutList.columns.stockOutCode')"
        min-width="190"
        show-overflow-tooltip
      >
        <template #default="{ row }">
          <span class="stock-out-code-cell">
            <router-link class="so-tab-link mono-cell" :to="`/inventory/stock-out/${row.id}`">
              {{ row.stockOutCode?.trim() || t('quoteList.na') }}
            </router-link>
            <el-tooltip
              v-if="isCustomsStockOut(row) && salesNotifyTooltip(row)"
              :content="salesNotifyTooltip(row)"
              placement="top"
              :hide-after="0"
            >
              <span class="customs-notify-tag">{{ t('stockOutList.customsNotifyTag') }}</span>
            </el-tooltip>
          </span>
        </template>
      </el-table-column>
      <el-table-column :label="t('stockOutList.columns.status')" width="110" align="center">
        <template #default="{ row }">
          <span :class="['status-badge', `status-${row.status}`]">{{ statusLabel(row.status) }}</span>
        </template>
      </el-table-column>
      <el-table-column :label="t('stockOutList.columns.stockOutType')" width="140" align="center">
        <template #default="{ row }">
          <StockBizTypeTag
            biz="out"
            :type="row.stockOutType"
            :customs-declaration-id="row.customsDeclarationId"
            :customs-declaration-code="row.customsDeclarationCode"
          />
        </template>
      </el-table-column>
      <el-table-column :label="t('stockOutList.columns.expectedStockOutDate')" width="130">
        <template #default="{ row }">
          <span class="text-secondary">{{ formatDate(row.expectedStockOutDate) }}</span>
        </template>
      </el-table-column>
      <el-table-column :label="t('stockOutList.columns.stockOutDate')" width="170">
        <template #default="{ row }">
          <span class="text-secondary">{{ formatDate(row.stockOutDate) }}</span>
        </template>
      </el-table-column>
      <el-table-column :label="t('stockOutList.columns.shipmentMethod')" width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ shipmentMethodDisplay(row.shipmentMethod) }}</template>
      </el-table-column>
      <el-table-column :label="t('stockOutList.columns.expressCompany')" width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ expressCompanyDisplay(row.expressCompany) }}</template>
      </el-table-column>
      <el-table-column :label="t('stockOutList.columns.courierTrackingNo')" width="140" show-overflow-tooltip>
        <template #default="{ row }">{{ row.courierTrackingNo?.trim() || t('quoteList.na') }}</template>
      </el-table-column>
      <el-table-column
        :label="t('common.customerExtendCol.columnTitle')"
        :min-width="customerExtendColMinWidth"
        :width="customerExtendColWidth"
        show-overflow-tooltip
        class-name="customer-extend-col"
        label-class-name="customer-extend-col"
      >
        <template #header>
          <CustomerExtendColumnHeader
            :active-field="customerExtendActiveField"
            @set-active-field="setCustomerExtendActiveField"
          />
        </template>
        <template #default="{ row }">
          <CustomerExtendCell
            :row="row"
            :active-field="customerExtendActiveField"
            :masked="maskSaleSensitiveFields"
            :empty-text="t('quoteList.na')"
          />
        </template>
      </el-table-column>
      <el-table-column :label="t('stockOutList.columns.salesUserName')" width="110" show-overflow-tooltip>
        <template #default="{ row }">
          {{ maskSaleSensitiveFields ? '—' : (row.salesUserName?.trim() || t('quoteList.na')) }}
        </template>
      </el-table-column>
      <el-table-column :label="t('stockOutList.columns.packingCodes')" width="160" show-overflow-tooltip>
        <template #default="{ row }">
          <span class="mono-cell">{{ row.packingCodes?.trim() || t('quoteList.na') }}</span>
        </template>
      </el-table-column>
      <el-table-column :label="t('common.freightForwarderOrderNo')" width="160" show-overflow-tooltip>
        <template #default="{ row }">{{ row.freightForwarderOrderNo?.trim() || t('quoteList.na') }}</template>
      </el-table-column>
      <el-table-column :label="t('stockOutList.columns.packingCount')" width="120" align="right">
        <template #default="{ row }">{{ formatPackingCount(row.packingCount) }}</template>
      </el-table-column>
      <el-table-column :label="t('stockOutList.columns.remark')" min-width="160" show-overflow-tooltip>
        <template #default="{ row }">{{ row.remark?.trim() || t('quoteList.na') }}</template>
      </el-table-column>
      <el-table-column :label="t('stockOutList.columns.createTime')" width="170">
        <template #default="{ row }">{{ formatDate(row.createTime) }}</template>
      </el-table-column>
      <el-table-column :label="t('stockOutList.columns.createUser')" width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ row.createUserName?.trim() || t('quoteList.na') }}</template>
      </el-table-column>
    </el-table>
    <DetailListPanelEmpty v-else size="low" :description="emptyText ?? t('sellOrderItemStockOutTab.empty')" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { SellOrderItemStockOutTabRow } from '@/api/salesOrder'
import type { StockOutDto } from '@/api/stockOut'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import CustomerExtendColumnHeader from '@/components/list/CustomerExtendColumnHeader.vue'
import CustomerExtendCell from '@/components/list/CustomerExtendCell.vue'
import DetailListPanelEmpty from '@/components/Common/DetailListPanelEmpty.vue'
import { useCustomerExtendColumn } from '@/composables/useCustomerExtendColumn'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { StockOutTypeCode } from '@/constants/stockOutType'

withDefaults(
  defineProps<{
    items: SellOrderItemStockOutTabRow[]
    emptyText?: string
  }>(),
  {
    items: () => []
  }
)

const { t } = useI18n()
const router = useRouter()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const { ensureLoaded: ensureLogisticsDict, shipmentArrivalOptions, expressOptions } = useLogisticsFormDict()
const {
  activeField: customerExtendActiveField,
  colWidth: customerExtendColWidth,
  colMinWidth: customerExtendColMinWidth,
  setActiveField: setCustomerExtendActiveField
} = useCustomerExtendColumn()

const arrivalLabelByCode = computed(() => {
  const m = new Map<string, string>()
  for (const o of shipmentArrivalOptions.value) {
    const k = String(o.value ?? '').trim()
    if (k) m.set(k.toLowerCase(), o.label)
  }
  return m
})

const expressLabelByCode = computed(() => {
  const m = new Map<string, string>()
  for (const o of expressOptions.value) {
    const k = String(o.value ?? '').trim()
    if (k) m.set(k.toLowerCase(), o.label)
  }
  return m
})

function formatDate(v?: string | null) {
  return v != null && String(v).length > 0 ? formatDisplayDateTime(String(v)) : t('quoteList.na')
}

function formatPackingCount(v?: number | null) {
  return v == null || Number.isNaN(Number(v)) ? t('quoteList.na') : String(Number(v))
}

function shipmentMethodDisplay(code?: string | number | null): string {
  if (code === null || code === undefined || code === '') return t('quoteList.na')
  const c = String(code).trim()
  if (!c) return t('quoteList.na')
  return arrivalLabelByCode.value.get(c.toLowerCase()) ?? c
}

function expressCompanyDisplay(code?: string | null): string {
  const c = String(code ?? '').trim()
  if (!c) return t('quoteList.na')
  return expressLabelByCode.value.get(c.toLowerCase()) ?? c
}

function isCustomsStockOut(row: StockOutDto): boolean {
  return Number(row.stockOutType) === StockOutTypeCode.Customs
}

function salesNotifyTooltip(row: StockOutDto): string {
  const code = String(row.salesStockOutNotifyCode ?? '').trim()
  if (!code) return ''
  return t('stockOutList.salesNotifyCodeTooltip', { code })
}

function statusLabel(s: number) {
  switch (s) {
    case 0:
      return t('stockOutList.status.draft')
    case 1:
      return t('stockOutList.status.pending')
    case 2:
      return t('stockOutList.status.done')
    case 3:
      return t('stockOutList.status.cancelled')
    case 4:
      return t('stockOutList.status.finished')
    default:
      return t('rfqDetail.unknown')
  }
}

function onRowDblClick(row: SellOrderItemStockOutTabRow) {
  const id = String(row?.id ?? '').trim()
  if (!id) return
  void router.push(`/inventory/stock-out/${id}`)
}

onMounted(() => {
  void ensureLogisticsDict()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.so-tab-link {
  color: $cyan-primary;
  text-decoration: none;
  font-weight: 500;
  &:hover {
    text-decoration: underline;
  }
}

.mono-cell {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace;
}

.text-secondary {
  color: $text-muted;
}

.stock-out-code-cell {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.customs-notify-tag {
  display: inline-flex;
  align-items: center;
  padding: 1px 6px;
  border-radius: 4px;
  font-size: 11px;
  line-height: 1.2;
  background: rgba(0, 212, 255, 0.12);
  color: $cyan-primary;
  white-space: nowrap;
}

.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  &.status-0 {
    background: rgba(255, 255, 255, 0.05);
    color: $text-muted;
  }
  &.status-1 {
    background: rgba(255, 193, 7, 0.15);
    color: #ffc107;
  }
  &.status-2 {
    background: rgba(70, 191, 145, 0.18);
    color: #46bf91;
  }
  &.status-3 {
    background: rgba(201, 87, 69, 0.18);
    color: #c95745;
  }
  &.status-4 {
    background: rgba(0, 212, 255, 0.18);
    color: $cyan-primary;
  }
}
</style>
