<template>
  <div class="so-aggregate-table-wrap sell-order-item-stock-in-tab-table">
    <el-table v-if="items.length > 0" :data="items" size="small" stripe @row-dblclick="onRowDblClick">
      <el-table-column type="index" width="50" label="#" />
      <el-table-column :label="t('stockInList.columns.status')" width="110" align="center">
        <template #default="{ row }">
          <span :class="['status-badge', `status-${row.status}`]">{{ statusLabel(row.status) }}</span>
        </template>
      </el-table-column>
      <el-table-column :label="t('stockInList.columns.stockInType')" width="140" align="center">
        <template #default="{ row }">
          <StockBizTypeTag
            biz="in"
            :type="row.stockInType"
            :customs-declaration-id="row.customsDeclarationId"
            :customs-declaration-code="row.customsDeclarationCode"
          />
        </template>
      </el-table-column>
      <el-table-column :label="t('stockInList.columns.materialModel')" min-width="140" show-overflow-tooltip>
        <template #default="{ row }">{{ materialModelText(row) || t('quoteList.na') }}</template>
      </el-table-column>
      <el-table-column :label="t('stockInList.columns.brand')" min-width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ materialBrandText(row) || t('quoteList.na') }}</template>
      </el-table-column>
      <el-table-column :label="t('stockInList.columns.warehouse')" min-width="160" show-overflow-tooltip>
        <template #default="{ row }">{{ warehouseNameOf(row.warehouseId) }}</template>
      </el-table-column>
      <el-table-column
        :label="t('common.vendorExtendCol.columnTitle')"
        :min-width="vendorExtendColMinWidth"
        :width="vendorExtendColWidth"
        show-overflow-tooltip
        class-name="vendor-extend-col"
        label-class-name="vendor-extend-col"
      >
        <template #header>
          <VendorExtendColumnHeader
            :active-field="vendorExtendActiveField"
            @set-active-field="setVendorExtendActiveField"
          />
        </template>
        <template #default="{ row }">
          <VendorExtendCell
            :row="row"
            :active-field="vendorExtendActiveField"
            :masked="maskPurchaseSensitiveFields"
            :empty-text="t('quoteList.na')"
          />
        </template>
      </el-table-column>
      <el-table-column :label="t('stockInList.columns.stockInDate')" width="160">
        <template #default="{ row }">
          <span class="text-secondary">{{ formatDate(row.stockInDate) }}</span>
        </template>
      </el-table-column>
      <el-table-column :label="t('stockInList.columns.totalQuantity')" width="110" align="right">
        <template #default="{ row }">{{ formatNum(row.totalQuantity) }}</template>
      </el-table-column>
      <el-table-column :label="t('stockInList.columns.hasBatchEntered')" width="120" align="center">
        <template #default="{ row }">
          <span :class="row.hasBatchEntered ? 'batch-flag batch-flag--yes' : 'batch-flag batch-flag--no'">
            {{ row.hasBatchEntered ? t('stockInList.hasBatchEntered.yes') : t('stockInList.hasBatchEntered.no') }}
          </span>
        </template>
      </el-table-column>
      <el-table-column :label="t('stockInList.columns.totalAmount')" width="130" align="right">
        <template #default="{ row }">
          <span v-if="maskPurchaseSensitiveFields">—</span>
          <span v-else>
            {{ formatMoney(row.totalAmount) }}
            <template v-if="stockInCurrencyLabel(row)">
              <span class="text-secondary"> {{ stockInCurrencyLabel(row) }}</span>
            </template>
          </span>
        </template>
      </el-table-column>
      <el-table-column :label="t('stockInList.columns.remark')" prop="remark" min-width="160" show-overflow-tooltip />
      <el-table-column :label="t('stockInList.columns.stockInCode')" width="160" show-overflow-tooltip>
        <template #default="{ row }">
          <span class="stock-in-code-cell">
            <span>{{ row.stockInCode?.trim() || t('quoteList.na') }}</span>
            <el-tooltip
              v-if="isCustomsStockIn(row) && arrivalNotifyTooltip(row)"
              :content="arrivalNotifyTooltip(row)"
              placement="top"
              :hide-after="0"
            >
              <span class="customs-notify-tag">{{ t('stockInList.customsNotifyTag') }}</span>
            </el-tooltip>
          </span>
        </template>
      </el-table-column>
      <el-table-column
        :label="t('stockInList.columns.sourceCode')"
        prop="sourceDisplayNo"
        width="160"
        show-overflow-tooltip
      />
      <el-table-column
        :label="t('stockInList.columns.purchaseOrderCode')"
        prop="purchaseOrderCode"
        min-width="160"
        show-overflow-tooltip
      />
      <el-table-column
        :label="t('common.freightForwarderOrderNo')"
        prop="freightForwarderOrderNo"
        min-width="160"
        show-overflow-tooltip
      />
      <el-table-column :label="t('stockInList.columns.salesOrderCode')" min-width="170" show-overflow-tooltip>
        <template #default="{ row }">{{ row.salesOrderCode?.trim() || t('quoteList.na') }}</template>
      </el-table-column>
      <el-table-column :label="t('stockInList.columns.createTime')" width="160">
        <template #default="{ row }">{{ formatDate(row.createTime) }}</template>
      </el-table-column>
      <el-table-column :label="t('stockInList.columns.createUser')" width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ row.createUserName?.trim() || t('quoteList.na') }}</template>
      </el-table-column>
    </el-table>
    <DetailListPanelEmpty v-else size="low" :description="emptyText ?? t('sellOrderItemStockInTab.empty')" />
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import type { StockInListItemDto } from '@/api/stockIn'
import { inventoryCenterApi, type WarehouseInfo } from '@/api/inventoryCenter'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import { StockInTypeCode } from '@/constants/stockInType'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import VendorExtendColumnHeader from '@/components/list/VendorExtendColumnHeader.vue'
import VendorExtendCell from '@/components/list/VendorExtendCell.vue'
import DetailListPanelEmpty from '@/components/Common/DetailListPanelEmpty.vue'
import { useVendorExtendColumn } from '@/composables/useVendorExtendColumn'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'

withDefaults(
  defineProps<{
    items: StockInListItemDto[]
    emptyText?: string
  }>(),
  {
    items: () => []
  }
)

const { t } = useI18n()
const router = useRouter()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const {
  activeField: vendorExtendActiveField,
  colWidth: vendorExtendColWidth,
  colMinWidth: vendorExtendColMinWidth,
  setActiveField: setVendorExtendActiveField
} = useVendorExtendColumn()

const warehouses = ref<WarehouseInfo[]>([])

function formatDate(v?: string | null) {
  return v != null && String(v).length > 0 ? formatDisplayDateTime(String(v)) : t('quoteList.na')
}

function formatNum(v?: number | null) {
  return v == null || Number.isNaN(Number(v)) ? t('quoteList.na') : Number(v).toLocaleString()
}

function formatMoney(v?: number | null) {
  return v == null || Number.isNaN(Number(v)) ? t('quoteList.na') : Number(v).toFixed(2)
}

function materialModelText(row: StockInListItemDto) {
  return String(row.materialModelSummary ?? '').trim()
}

function materialBrandText(row: StockInListItemDto) {
  return String(row.materialBrandSummary ?? '').trim()
}

function stockInCurrencyLabel(row: StockInListItemDto) {
  const raw = row.currencyCode
  if (raw == null) return ''
  const n = Number(raw)
  if (Number.isNaN(n)) return ''
  return CURRENCY_CODE_TO_TEXT[n] ?? String(n)
}

function warehouseNameOf(warehouseId?: string) {
  if (!warehouseId) return t('quoteList.na')
  const byId = warehouses.value.find((w) => w.id === warehouseId)
  if (byId?.warehouseName) return byId.warehouseName
  const byCode = warehouses.value.find((w) => (w.warehouseCode || '').trim() === warehouseId.trim())
  return byCode?.warehouseName || warehouseId
}

function statusLabel(s: number) {
  switch (s) {
    case 0:
      return t('stockInList.status.draft')
    case 1:
      return t('stockInList.status.pending')
    case 2:
      return t('stockInList.status.done')
    case 3:
      return t('stockInList.status.cancelled')
    default:
      return t('rfqDetail.unknown')
  }
}

function isCustomsStockIn(row: StockInListItemDto): boolean {
  return Number(row.stockInType) === StockInTypeCode.Customs
}

function arrivalNotifyTooltip(row: StockInListItemDto): string {
  const code = String(row.sourceDisplayNo ?? '').trim()
  if (!code) return ''
  return t('stockInList.arrivalNotifyCodeTooltip', { code })
}

function onRowDblClick(row: StockInListItemDto) {
  const id = String(row?.id ?? '').trim()
  if (!id) return
  void router.push({ name: 'StockInDetail', params: { id } })
}

onMounted(() => {
  void inventoryCenterApi.getWarehouses().then((list) => {
    warehouses.value = list
  }).catch(() => {
    warehouses.value = []
  })
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

.text-secondary {
  color: $text-muted;
}

.stock-in-code-cell {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  max-width: 100%;
}

.customs-notify-tag {
  flex: 0 0 auto;
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
}

.batch-flag {
  font-size: 12px;
  &--yes {
    color: #46bf91;
  }
  &--no {
    color: $text-muted;
  }
}
</style>
