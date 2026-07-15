<template>
  <div class="so-aggregate-table-wrap sell-order-item-stock-out-notify-tab-table">
    <el-table v-if="items.length > 0" :data="items" size="small" stripe @row-dblclick="onRowDblClick">
      <el-table-column type="index" width="50" label="#" />
      <el-table-column :label="t('stockOutNotifyList.columns.status')" width="110" align="center">
        <template #default="{ row }">
          <span :class="['status-badge', `status-${row.status}`]">{{ statusLabel(row.status) }}</span>
        </template>
      </el-table-column>
      <el-table-column :label="t('stockOutNotifyList.columns.customsStatus')" width="120" align="center">
        <template #default="{ row }">{{ customsStatusLabel(row.customsStatus) }}</template>
      </el-table-column>
      <el-table-column :label="t('stockOutNotifyList.columns.stockOutType')" width="140" align="center">
        <template #default="{ row }">
          <StockBizTypeTag
            biz="out"
            :type="row.stockOutType"
            :customs-declaration-id="row.customsDeclarationId"
            :customs-declaration-code="row.customsDeclarationCode"
          />
        </template>
      </el-table-column>
      <el-table-column
        :label="t('stockOutNotifyList.columns.materialModel')"
        prop="materialModel"
        width="180"
        show-overflow-tooltip
      />
      <el-table-column
        :label="t('stockOutNotifyList.columns.brand')"
        prop="brand"
        width="140"
        show-overflow-tooltip
      />
      <el-table-column :label="t('stockOutNotifyList.columns.outQuantity')" width="110" align="right">
        <template #default="{ row }">{{ row.outQuantity }}</template>
      </el-table-column>
      <el-table-column :label="t('stockOutNotifyList.columns.regionType')" width="100" align="center">
        <template #default="{ row }">{{ regionTypeLabel(row) }}</template>
      </el-table-column>
      <el-table-column :label="t('stockOutNotifyList.columns.shipmentMethod')" width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ shipmentMethodDisplay(row.shipmentMethod) }}</template>
      </el-table-column>
      <el-table-column :label="t('stockOutNotifyList.columns.expressCompany')" width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ expressCompanyDisplay(row.expressCompany) }}</template>
      </el-table-column>
      <el-table-column :label="t('stockOutNotifyList.columns.packingCode')" width="150" show-overflow-tooltip>
        <template #default="{ row }">
          <router-link
            v-if="row.packingId?.trim() && row.packingCode?.trim()"
            class="so-tab-link"
            :to="`/inventory/packing/${row.packingId.trim()}`"
            @click.stop
          >
            {{ row.packingCode.trim() }}
          </router-link>
          <span v-else-if="row.packingCode?.trim()">{{ row.packingCode.trim() }}</span>
          <span v-else>—</span>
        </template>
      </el-table-column>
      <el-table-column :label="t('stockOutNotifyList.columns.requestDate')" width="170">
        <template #default="{ row }">{{ formatRequestDateTime(row.requestDate) }}</template>
      </el-table-column>
      <el-table-column :label="t('stockOutNotifyList.columns.salesUserName')" width="130" show-overflow-tooltip>
        <template #default="{ row }">{{ row.salesUserName?.trim() || '—' }}</template>
      </el-table-column>
      <el-table-column
        :label="t('stockOutNotifyList.columns.customer')"
        prop="customerName"
        min-width="180"
        show-overflow-tooltip
      />
      <el-table-column :label="t('stockOutNotifyList.columns.remark')" prop="remark" min-width="180" show-overflow-tooltip />
      <el-table-column :label="t('stockOutNotifyList.columns.requestCode')" width="190" show-overflow-tooltip>
        <template #default="{ row }">
          <span class="notify-code-cell">
            <span class="notify-code-text">{{ row.requestCode?.trim() || '—' }}</span>
            <el-tooltip
              v-if="isCustomsNotify(row) && salesNotifyTooltip(row)"
              :content="salesNotifyTooltip(row)"
              placement="top"
              :hide-after="0"
            >
              <span class="customs-notify-tag">{{ t('stockOutNotifyList.customsNotifyTag') }}</span>
            </el-tooltip>
          </span>
        </template>
      </el-table-column>
      <el-table-column :label="t('stockOutNotifyList.columns.salesOrderCode')" width="160" show-overflow-tooltip>
        <template #default="{ row }">
          <router-link
            v-if="row.salesOrderId?.trim() && row.salesOrderCode?.trim()"
            class="so-tab-link"
            :to="`/sales-orders/${row.salesOrderId.trim()}`"
            @click.stop
          >
            {{ row.salesOrderCode.trim() }}
          </router-link>
          <span v-else>{{ row.salesOrderCode?.trim() || '—' }}</span>
        </template>
      </el-table-column>
      <el-table-column :label="t('stockOutNotifyList.columns.createTime')" width="170">
        <template #default="{ row }">{{ formatRequestDateTime(row.createTime) }}</template>
      </el-table-column>
      <el-table-column :label="t('stockOutNotifyList.columns.createUser')" width="140" show-overflow-tooltip>
        <template #default="{ row }">{{ row.requestUserName?.trim() || '—' }}</template>
      </el-table-column>
    </el-table>
    <DetailListPanelEmpty v-else size="low" :description="emptyText ?? t('sellOrderItemStockOutNotifyTab.empty')" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { formatDate as formatDateTimeZh } from '@/utils/date'
import type { SellOrderItemStockOutNotifyTabRow } from '@/api/salesOrder'
import type { StockOutRequestDto } from '@/api/stockOut'
import { normalizeRegionType, REGION_TYPE_OVERSEAS } from '@/constants/regionType'
import { STOCK_OUT_REQUEST_STATUS } from '@/constants/stockOutRequestStatus'
import { STOCK_OUT_NOTIFY_CUSTOMS_STATUS } from '@/constants/stockOutNotifyCustomsStatus'
import { StockOutTypeCode } from '@/constants/stockOutType'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import DetailListPanelEmpty from '@/components/Common/DetailListPanelEmpty.vue'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'

withDefaults(
  defineProps<{
    items: SellOrderItemStockOutNotifyTabRow[]
    emptyText?: string
  }>(),
  {
    items: () => []
  }
)

const { t } = useI18n()
const router = useRouter()
const { ensureLoaded: ensureLogisticsDict, shipmentArrivalOptions, expressOptions } = useLogisticsFormDict()

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

function formatRequestDateTime(v?: string | null) {
  if (v == null || v === '') return '—'
  return formatDateTimeZh(v, 'YYYY-MM-DD HH:mm')
}

function statusLabel(s: number) {
  if (s === STOCK_OUT_REQUEST_STATUS.PendingCustoms) return t('stockOutNotifyList.status.pendingCustoms')
  if (s === STOCK_OUT_REQUEST_STATUS.PendingPacking) return t('stockOutNotifyList.status.pendingPacking')
  if (s === STOCK_OUT_REQUEST_STATUS.Packed) return t('stockOutNotifyList.status.packed')
  if (s === STOCK_OUT_REQUEST_STATUS.StockedOut) return t('stockOutNotifyList.status.stockedOut')
  if (s === STOCK_OUT_REQUEST_STATUS.Cancelled) return t('stockOutNotifyList.status.cancelled')
  return t('stockOutNotifyList.status.unknown')
}

function customsStatusLabel(code?: number | null): string {
  const n = Number(code ?? 0)
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.NotRequired) return '—'
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.PendingCustoms) return t('stockOutNotifyList.customsStatus.pendingCustoms')
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.InCustoms) return t('stockOutNotifyList.customsStatus.inCustoms')
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.Completed) return t('stockOutNotifyList.customsStatus.completed')
  return '—'
}

function resolveNotifyStockOutType(v: unknown): number {
  const n = Number(v)
  if (
    n === StockOutTypeCode.Sales ||
    n === StockOutTypeCode.Customs ||
    n === StockOutTypeCode.Return ||
    n === StockOutTypeCode.Scrap
  ) {
    return n
  }
  return StockOutTypeCode.Sales
}

function isCustomsNotify(row: StockOutRequestDto): boolean {
  return resolveNotifyStockOutType(row.stockOutType) === StockOutTypeCode.Customs
}

function salesNotifyTooltip(row: StockOutRequestDto): string {
  const code = String(row.salesStockOutNotifyCode ?? '').trim()
  if (!code) return ''
  return t('stockOutNotifyList.salesNotifyCodeTooltip', { code })
}

function regionTypeLabel(row: SellOrderItemStockOutNotifyTabRow) {
  const n = normalizeRegionType(row.regionType)
  return n === REGION_TYPE_OVERSEAS ? t('inventoryList.warehouse.regionOverseas') : t('inventoryList.warehouse.regionDomestic')
}

function shipmentMethodDisplay(code?: string | null): string {
  const c = String(code ?? '').trim()
  if (!c) return '—'
  return arrivalLabelByCode.value.get(c.toLowerCase()) ?? c
}

function expressCompanyDisplay(code?: string | null): string {
  const c = String(code ?? '').trim()
  if (!c) return '—'
  return expressLabelByCode.value.get(c.toLowerCase()) ?? c
}

function onRowDblClick(row: SellOrderItemStockOutNotifyTabRow) {
  const id = String(row?.id ?? '').trim()
  if (!id) return
  void router.push({ name: 'StockOutNotifyDetail', params: { id } })
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

.notify-code-cell {
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
  &.status-5 {
    background: rgba(156, 89, 182, 0.18);
    color: #9c59b6;
  }
  &.status-10 {
    background: rgba(255, 193, 7, 0.15);
    color: #ffc107;
  }
  &.status-20 {
    background: rgba(0, 212, 255, 0.15);
    color: $cyan-primary;
  }
  &.status-100 {
    background: rgba(70, 191, 145, 0.18);
    color: #46bf91;
  }
  &.status--1 {
    background: rgba(201, 87, 69, 0.18);
    color: #c95745;
  }
}
</style>
