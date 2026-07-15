<template>
  <div class="po-aggregate-table-wrap purchase-order-item-arrival-notice-tab-table">
    <el-table v-if="items.length > 0" :data="items" size="small" stripe>
      <el-table-column type="index" width="50" label="#" />
      <el-table-column :label="t('arrivalNoticeList.columns.status')" width="110" align="center">
        <template #default="{ row }">
          <el-tag effect="dark" :type="statusType(row.status)">{{ statusText(row.status) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column :label="t('arrivalNoticeList.columns.arrivalType')" width="140" align="center">
        <template #default="{ row }">
          <StockBizTypeTag
            biz="in"
            :type="row.stockInType"
            :customs-declaration-id="row.customsDeclarationId"
            :customs-declaration-code="row.customsDeclarationCode"
          />
        </template>
      </el-table-column>
      <el-table-column :label="t('arrivalNoticeList.columns.pn')" min-width="120" show-overflow-tooltip>
        <template #default="{ row }">
          <CrmListCopyableTextCell :text="rawPn(row)" />
        </template>
      </el-table-column>
      <el-table-column :label="t('arrivalNoticeList.columns.brand')" width="100" show-overflow-tooltip>
        <template #default="{ row }">
          <CrmListCopyableTextCell :text="rawBrand(row)" />
        </template>
      </el-table-column>
      <el-table-column :label="t('arrivalNoticeList.columns.expectedArrivalDate')" width="130" align="center">
        <template #default="{ row }">{{ formatExpected(row.expectedArrivalDate) }}</template>
      </el-table-column>
      <el-table-column :label="t('arrivalNoticeList.columns.expectedArrivalMethod')" width="136" show-overflow-tooltip>
        <template #default="{ row }">{{ shipmentMethodDisplay(pickShipmentMethod(row)) }}</template>
      </el-table-column>
      <el-table-column :label="t('arrivalNoticeList.columns.expectedArrivalExpressNo')" width="184" show-overflow-tooltip>
        <template #default="{ row }">{{ displayCourierTrackingNo(row) }}</template>
      </el-table-column>
      <el-table-column :label="t('arrivalNoticeList.columns.vendorName')" min-width="160" show-overflow-tooltip>
        <template #default="{ row }">
          <VendorNameReadonlyText
            :name-zh="row.vendorName"
            :name-en="row.vendorEnglishName"
            :masked="maskPurchaseSensitiveFields"
          />
        </template>
      </el-table-column>
      <el-table-column :label="t('arrivalNoticeList.columns.purchaseUserName')" width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ row.purchaseUserName?.trim() || '—' }}</template>
      </el-table-column>
      <el-table-column :label="t('arrivalNoticeList.columns.expectQty')" width="120" align="right">
        <template #default="{ row }">
          <span class="inv-list-qty">{{ formatQtyCell(expectQty(row)) }}</span>
        </template>
      </el-table-column>
      <el-table-column :label="t('arrivalNoticeList.columns.receiveQty')" width="120" align="right">
        <template #default="{ row }">
          <span class="inv-list-qty">{{ formatQtyCell(receiveQty(row)) }}</span>
        </template>
      </el-table-column>
      <el-table-column :label="t('arrivalNoticeList.columns.passedQty')" width="120" align="right">
        <template #default="{ row }">
          <span class="inv-list-qty">{{ formatQtyCell(passedQty(row)) }}</span>
        </template>
      </el-table-column>
      <el-table-column :label="t('arrivalNoticeList.columns.arrivalRegion')" width="100" align="center">
        <template #default="{ row }">{{ regionTypeLabel(row) }}</template>
      </el-table-column>
      <el-table-column :label="t('arrivalNoticeList.columns.noticeCode')" prop="noticeCode" width="170" show-overflow-tooltip />
      <el-table-column :label="t('arrivalNoticeList.columns.purchaseOrderCode')" prop="purchaseOrderCode" width="160" show-overflow-tooltip />
      <el-table-column :label="t('common.freightForwarderOrderNo')" prop="freightForwarderOrderNo" width="160" show-overflow-tooltip />
      <el-table-column :label="t('arrivalNoticeList.columns.createTime')" width="170">
        <template #default="{ row }">
          <template v-for="p in [formatDisplayDateTime2DigitYearParts(row.createTime)]" :key="'ct-' + row.id">
            <span v-if="p" class="crm-quote-create-time">
              <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
              <span class="crm-quote-create-time__hm">{{ p.time }}</span>
            </span>
            <span v-else class="inv-list-dash">—</span>
          </template>
        </template>
      </el-table-column>
      <el-table-column :label="t('arrivalNoticeList.columns.createUser')" width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ row.createUserName || row.createdBy || row.purchaseUserName || '—' }}</template>
      </el-table-column>
    </el-table>
    <DetailListPanelEmpty v-else size="low" :description="emptyText ?? t('purchaseOrderItemArrivalNoticeTab.empty')" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import type { StockInNotifyDto, StockInNotifyItemDto } from '@/api/logistics'
import { formatDisplayDate, formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { normalizeRegionType, REGION_TYPE_OVERSEAS } from '@/constants/regionType'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import VendorNameReadonlyText from '@/components/Vendor/VendorNameReadonlyText.vue'
import DetailListPanelEmpty from '@/components/Common/DetailListPanelEmpty.vue'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'

withDefaults(
  defineProps<{
    items: StockInNotifyDto[]
    emptyText?: string
  }>(),
  {
    items: () => []
  }
)

const { t } = useI18n()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { ensureLoaded: ensureLogisticsDict, shipmentArrivalOptions } = useLogisticsFormDict()

const arrivalLabelByCode = computed(() => {
  const m = new Map<string, string>()
  for (const o of shipmentArrivalOptions.value) {
    const k = String(o.value ?? '').trim()
    if (k) m.set(k.toLowerCase(), o.label)
  }
  return m
})

const num = (v: unknown) => Number(v ?? 0)

const qtyFromItems = (items: StockInNotifyItemDto[] | undefined, key: 'arrivedQty' | 'qty' | 'passedQty') =>
  Number((items || []).reduce((s, x) => s + num(x?.[key]), 0).toFixed(4))

const pickQty = (
  rowVal: number | undefined | null,
  items: StockInNotifyItemDto[] | undefined,
  itemKey: 'qty' | 'arrivedQty' | 'passedQty'
) => (rowVal != null && !Number.isNaN(Number(rowVal)) ? Number(rowVal) : qtyFromItems(items, itemKey))

function expectQty(row: StockInNotifyDto) {
  return pickQty(row.expectQty, row.items, 'qty')
}

function receiveQty(row: StockInNotifyDto) {
  return pickQty(row.receiveQty, row.items, 'arrivedQty')
}

function passedQty(row: StockInNotifyDto) {
  return pickQty(row.passedQty, row.items, 'passedQty')
}

function rawPn(row: StockInNotifyDto) {
  return (row.pn != null && row.pn !== '' ? row.pn : row.items?.[0]?.pn) || ''
}

function rawBrand(row: StockInNotifyDto) {
  return (row.brand != null && row.brand !== '' ? row.brand : row.items?.[0]?.brand) || ''
}

function formatQtyCell(v: unknown) {
  if (v == null || v === '') return '—'
  const n = Number(v)
  if (!Number.isFinite(n)) return '—'
  return n.toLocaleString('zh-CN')
}

function statusText(s: number) {
  const keyMap: Record<number, 'new' | 'notArrived' | 'pendingQc' | 'qcDone' | 'stocked'> = {
    1: 'new',
    10: 'notArrived',
    20: 'pendingQc',
    30: 'qcDone',
    100: 'stocked'
  }
  const k = keyMap[s]
  return k ? t(`arrivalNoticeList.status.${k}`) : t('arrivalNoticeList.statusUnknown')
}

function statusType(s: number) {
  return ({ 1: 'info', 10: 'warning', 20: 'primary', 30: 'success', 100: 'success' } as const)[s] || 'info'
}

function formatExpected(v?: string | null) {
  return v ? formatDisplayDate(v) : '—'
}

function pickShipmentMethod(row: StockInNotifyDto): string | null | undefined {
  const r = row as unknown as Record<string, unknown>
  return (r.shipmentMethod ?? r.ShipmentMethod) as string | null | undefined
}

function pickCourierTrackingNo(row: StockInNotifyDto): string | null | undefined {
  const r = row as unknown as Record<string, unknown>
  return (r.courierTrackingNo ?? r.CourierTrackingNo) as string | null | undefined
}

function shipmentMethodDisplay(code?: string | number | null): string {
  if (code === null || code === undefined || code === '') return '—'
  const c = String(code).trim()
  if (!c) return '—'
  return arrivalLabelByCode.value.get(c.toLowerCase()) ?? c
}

function displayCourierTrackingNo(row: StockInNotifyDto): string {
  const v = pickCourierTrackingNo(row)
  const s = String(v ?? '').trim()
  return s || '—'
}

function regionTypeLabel(row: StockInNotifyDto) {
  const r = row as unknown as Record<string, unknown>
  const n = normalizeRegionType(r.regionType ?? r.RegionType)
  return n === REGION_TYPE_OVERSEAS ? t('inventoryList.warehouse.regionOverseas') : t('inventoryList.warehouse.regionDomestic')
}

onMounted(() => {
  void ensureLogisticsDict()
})
</script>

<style scoped lang="scss">
.inv-list-qty {
  font-variant-numeric: tabular-nums;
}

.inv-list-dash {
  color: var(--crm-text-muted, #909399);
}
</style>
