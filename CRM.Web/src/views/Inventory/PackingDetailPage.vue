<template>
  <div class="packing-detail-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">箱</div>
          <h1 class="page-title">{{ t('packingDetail.title') }}</h1>
          <span v-if="detail?.code" class="count-badge">{{ detail.code }}</span>
        </div>
      </div>
      <div class="header-right">
        <button type="button" class="btn-secondary" @click="goBack">{{ t('packingDetail.back') }}</button>
      </div>
    </div>

    <el-skeleton v-if="loading" :rows="8" animated />
    <template v-else-if="detail">
      <div class="detail-card">
        <h3 class="section-title">{{ t('packingDetail.sectionHeader') }}</h3>
        <el-descriptions :column="2" border>
          <el-descriptions-item :label="t('packingList.columns.packingCode')">{{ detail.code || '—' }}</el-descriptions-item>
          <el-descriptions-item :label="t('packingList.columns.status')">{{ packingStatusLabel(detail.status) }}</el-descriptions-item>
          <el-descriptions-item :label="t('packingList.columns.customerName')">
            {{ maskSaleSensitiveFields ? '—' : (detail.customerName?.trim() || '—') }}
          </el-descriptions-item>
          <el-descriptions-item :label="t('packingList.columns.salesUserName')">
            {{ maskSaleSensitiveFields ? '—' : (detail.salesUserName?.trim() || '—') }}
          </el-descriptions-item>
          <el-descriptions-item :label="t('packingDetail.stockOutType')">
            <StockBizTypeTag biz="out" :type="detail.stockOutType" />
          </el-descriptions-item>
          <el-descriptions-item :label="t('packingDetail.materialType')">{{ packingMaterialTypeLabel(detail.materialType) }}</el-descriptions-item>
          <el-descriptions-item :label="t('packingList.columns.itemRows')">{{ detail.itemRows }}</el-descriptions-item>
          <el-descriptions-item :label="t('packingDetail.scheduleShipDate')">{{ formatTime(detail.scheduleShipDate) }}</el-descriptions-item>
          <el-descriptions-item :label="t('packingList.columns.createTime')">{{ formatTime(detail.createTime) }}</el-descriptions-item>
          <el-descriptions-item :label="t('packingList.columns.shipmentMethod')">{{ shipmentMethodDisplay(detail.shipmentMethod) }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.detail.expressCompany')">{{ expressCompanyDisplay(detail.expressCompany) }}</el-descriptions-item>
          <el-descriptions-item v-if="detail.comment?.trim()" :label="t('packingDetail.comment')" :span="2">
            {{ detail.comment }}
          </el-descriptions-item>
        </el-descriptions>
      </div>

      <div class="detail-card packing-extend-card">
        <h3 class="section-title">{{ t('packingDetail.sectionExtend') }}</h3>
        <el-tabs v-model="packingExtendTab" type="border-card" class="packing-extend-tabs">
          <el-tab-pane :label="t('packingDetail.tabs.shipAddress')" name="ship">
            <el-descriptions :column="2" border class="packing-kv-descriptions">
              <el-descriptions-item
                v-for="row in shipAddressKv"
                :key="row.key"
                :label="row.label"
              >
                {{ row.value }}
              </el-descriptions-item>
            </el-descriptions>
          </el-tab-pane>
          <el-tab-pane :label="t('packingDetail.tabs.billAddress')" name="bill">
            <el-descriptions :column="2" border class="packing-kv-descriptions">
              <el-descriptions-item
                v-for="row in billAddressKv"
                :key="row.key"
                :label="row.label"
              >
                {{ row.value }}
              </el-descriptions-item>
            </el-descriptions>
          </el-tab-pane>
          <el-tab-pane :label="t('packingDetail.tabs.deliveryReq')" name="deliveryReq">
            <div class="delivery-req-block">
              <div class="delivery-req-label">{{ t('packingDetail.deliveryReq') }}</div>
              <div class="delivery-req-value">{{ detail.deliveryReq?.trim() || '—' }}</div>
            </div>
          </el-tab-pane>
          <el-tab-pane :label="t('packingDetail.tabs.boxParams')" name="box">
            <el-descriptions :column="2" border class="packing-kv-descriptions">
              <el-descriptions-item
                v-for="row in boxParamsKv"
                :key="row.key"
                :label="row.label"
              >
                {{ row.value }}
              </el-descriptions-item>
            </el-descriptions>
          </el-tab-pane>
        </el-tabs>
      </div>

      <div class="detail-card">
        <h3 class="section-title">{{ t('packingDetail.sectionLines') }}</h3>
        <p class="section-hint">{{ t('packingDetail.itemExtendHint') }}</p>
        <el-table
          :data="detail.items"
          border
          class="lines-table packing-items-table"
          size="small"
          :empty-text="t('packingDetail.linesEmpty')"
          :row-class-name="packingItemRowClassName"
          @row-click="onPackingItemRowClick"
        >
          <el-table-column
            :label="t('packingDetail.itemCode')"
            prop="itemCode"
            min-width="148"
            show-overflow-tooltip
          >
            <template #default="{ row }">{{ row.itemCode?.trim() || '—' }}</template>
          </el-table-column>
          <el-table-column :label="t('packingItemList.columns.pn')" prop="pn" min-width="140" show-overflow-tooltip />
          <el-table-column :label="t('packingItemList.columns.brand')" prop="brand" min-width="120" show-overflow-tooltip />
          <el-table-column :label="t('packingItemList.columns.qty')" prop="qty" width="88" align="right" />
          <el-table-column :label="t('packingDetail.unit')" prop="unit" width="72" />
          <el-table-column :label="t('packingItemList.columns.sellOrderCode')" min-width="140" show-overflow-tooltip>
            <template #default="{ row }">{{ row.sellOrderCode || '—' }}</template>
          </el-table-column>
          <el-table-column :label="t('packingItemList.columns.sellOrderItemCode')" min-width="140" show-overflow-tooltip>
            <template #default="{ row }">{{ row.sellOrderItemCode || '—' }}</template>
          </el-table-column>
          <el-table-column :label="t('packingDetail.comment')" prop="comment" min-width="120" show-overflow-tooltip />
        </el-table>
      </div>

      <div class="detail-card">
        <h3 class="section-title">{{ t('packingDetail.sectionItemExtend') }}</h3>
        <p v-if="selectedPackingItemId" class="section-hint section-hint--muted">
          {{ selectedPackingItemLabel }}
        </p>
        <el-table
          :data="selectedItemExtends"
          border
          class="lines-table packing-item-extend-table"
          size="small"
          :empty-text="selectedPackingItemId ? t('packingDetail.itemExtendEmpty') : t('packingDetail.itemExtendNoSelection')"
        >
          <el-table-column :label="t('packingDetail.extendColumns.customerSo')" prop="customerSo" min-width="120" show-overflow-tooltip />
          <el-table-column :label="t('packingDetail.extendColumns.customerPn')" prop="customerPn" min-width="120" show-overflow-tooltip />
          <el-table-column :label="t('packingDetail.extendColumns.customerBrand')" prop="customerBrand" min-width="110" show-overflow-tooltip />
          <el-table-column :label="t('packingDetail.extendColumns.price')" width="110" align="right">
            <template #default="{ row }">
              <span v-if="row.price != null">{{ row.price }}</span>
              <span v-else>—</span>
            </template>
          </el-table-column>
          <el-table-column :label="t('packingDetail.extendColumns.priceCurrency')" width="80" align="center">
            <template #default="{ row }">
              {{ row.priceCurrency != null ? currencyLabel(row.priceCurrency) : '—' }}
            </template>
          </el-table-column>
          <el-table-column :label="t('packingDetail.extendColumns.priceConvertPrice')" width="110" align="right">
            <template #default="{ row }">
              <span v-if="row.priceConvertPrice != null">{{ row.priceConvertPrice }}</span>
              <span v-else>—</span>
            </template>
          </el-table-column>
          <el-table-column :label="t('packingDetail.extendColumns.sellOrderCode')" min-width="140" show-overflow-tooltip>
            <template #default="{ row }">{{ row.sellOrderCode?.trim() || '—' }}</template>
          </el-table-column>
          <el-table-column :label="t('packingDetail.extendColumns.sellOrderItemCode')" min-width="150" show-overflow-tooltip>
            <template #default="{ row }">{{ row.sellOrderItemCode?.trim() || '—' }}</template>
          </el-table-column>
          <el-table-column :label="t('packingDetail.extendColumns.customerName')" min-width="140" show-overflow-tooltip>
            <template #default="{ row }">{{ displayCustomerName(row.customerName) }}</template>
          </el-table-column>
          <el-table-column :label="t('packingDetail.extendColumns.salesUserName')" min-width="120" show-overflow-tooltip>
            <template #default="{ row }">{{ displaySalesUserName(row.salesUserName) }}</template>
          </el-table-column>
        </el-table>
      </div>

      <div class="detail-card">
        <h3 class="section-title">{{ t('packingDetail.sectionStockOutNotify') }}</h3>
        <p v-if="selectedPackingItemId" class="section-hint section-hint--muted">
          {{ selectedPackingItemLabel }}
        </p>
        <p v-else class="section-hint">{{ t('packingDetail.stockOutNotifyHint') }}</p>
        <el-table
          :data="selectedStockOutNotifyRows"
          border
          class="lines-table packing-stock-out-notify-table"
          size="small"
          row-key="id"
          :empty-text="stockOutNotifyEmptyText"
          @row-dblclick="onStockOutNotifyRowDblClick"
        >
          <el-table-column :label="t('stockOutNotifyList.columns.status')" width="110" align="center">
            <template #default="{ row }">
              <span :class="['status-badge', `status-${row.status}`]">
                {{ stockOutNotifyStatusLabel(row.status) }}
              </span>
            </template>
          </el-table-column>
          <el-table-column
            :label="t('stockOutNotifyList.columns.requestCode')"
            prop="requestCode"
            min-width="140"
            show-overflow-tooltip
          />
          <el-table-column
            :label="t('stockOutNotifyList.columns.materialModel')"
            prop="materialModel"
            min-width="140"
            show-overflow-tooltip
          />
          <el-table-column :label="t('stockOutNotifyList.columns.brand')" prop="brand" min-width="120" show-overflow-tooltip />
          <el-table-column :label="t('stockOutNotifyList.columns.outQuantity')" prop="outQuantity" width="100" align="right" />
          <el-table-column :label="t('stockOutNotifyList.columns.regionType')" width="100" align="center">
            <template #default="{ row }">{{ stockOutNotifyRegionLabel(row) }}</template>
          </el-table-column>
          <el-table-column :label="t('stockOutNotifyList.columns.salesOrderCode')" min-width="130" show-overflow-tooltip>
            <template #default="{ row }">{{ row.salesOrderCode || '—' }}</template>
          </el-table-column>
          <el-table-column :label="t('stockOutNotifyList.columns.customer')" min-width="160" show-overflow-tooltip>
            <template #default="{ row }">{{ displayCustomerName(row.customerName) }}</template>
          </el-table-column>
          <el-table-column :label="t('stockOutNotifyList.columns.salesUserName')" min-width="110" show-overflow-tooltip>
            <template #default="{ row }">{{ displaySalesUserName(row.salesUserName) }}</template>
          </el-table-column>
          <el-table-column :label="t('stockOutNotifyList.columns.requestDate')" min-width="150">
            <template #default="{ row }">{{ formatTime(row.requestDate) }}</template>
          </el-table-column>
          <el-table-column :label="t('stockOutNotifyList.columns.remark')" prop="remark" min-width="140" show-overflow-tooltip />
        </el-table>
      </div>

      <div v-loading="loadingPickPage" class="detail-card">
        <h3 class="section-title">{{ t('packingDetail.sectionPickingLines') }}</h3>
        <p v-if="selectedPackingItemId" class="section-hint section-hint--muted">
          {{ selectedPackingItemLabel }}
          <template v-if="pickPage?.pickingTask?.taskCode">
            · {{ t('packingDetail.pickingTaskCode', { code: pickPage.pickingTask.taskCode }) }}
          </template>
        </p>
        <p v-else class="section-hint">{{ t('packingDetail.pickingLinesHint') }}</p>
        <el-table
          :data="selectedPickingLines"
          border
          class="lines-table packing-picking-lines-table"
          size="small"
          row-key="id"
          :empty-text="pickingLinesEmptyText"
        >
          <el-table-column :label="t('pickingSlip.detail.itemCode')" min-width="140" show-overflow-tooltip>
            <template #default="{ row }">{{ row.itemCode?.trim() || '—' }}</template>
          </el-table-column>
          <el-table-column :label="t('packingDetail.pickingColumns.stockItemCode')" min-width="140" show-overflow-tooltip>
            <template #default="{ row }">{{ pickingLineStockItemCode(row) }}</template>
          </el-table-column>
          <el-table-column :label="t('packingDetail.pickingColumns.stockInItemCode')" min-width="140" show-overflow-tooltip>
            <template #default="{ row }">{{ pickingLineStockInItemCode(row) }}</template>
          </el-table-column>
          <el-table-column :label="t('inventoryList.columns.stockType')" width="100" align="center">
            <template #default="{ row }">{{ pickingLineStockTypeLabel(row) }}</template>
          </el-table-column>
          <el-table-column :label="t('packingDetail.pickingColumns.planQty')" width="88" align="right">
            <template #default="{ row }">{{ row.planQty }}</template>
          </el-table-column>
          <el-table-column :label="t('packingDetail.pickingColumns.pickedQty')" width="88" align="right">
            <template #default="{ row }">{{ row.pickedQty }}</template>
          </el-table-column>
          <el-table-column :label="t('packingDetail.pickingColumns.source')" width="110" align="center">
            <template #default="{ row }">
              <span v-if="pickingLineIsStocking(row)" class="picking-source-stocking">
                {{ t('inventoryList.stockTypes.stocking') }}
              </span>
              <span v-else class="picking-source-normal">{{ t('inventoryList.stockTypes.customer') }}</span>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </template>
    <el-empty v-else :description="t('packingDetail.notFound')" />
  </div>
</template>


<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import {
  packingApi,
  packingMaterialTypeLabel,
  packingStatusLabel,
  currencyLabel,
  type PackingDetail,
  type PackingDetailLine,
  type PackingStockOutNotifyRow
} from '@/api/packing'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import {
  inventoryCenterApi,
  type PickPageByPacking,
  type PickingTaskLine
} from '@/api/inventoryCenter'
import { STOCK_OUT_REQUEST_STATUS } from '@/constants/stockOutRequestStatus'
import { normalizeRegionType, REGION_TYPE_OVERSEAS } from '@/constants/regionType'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const { ensureLoaded: ensureLogisticsDict, shipmentArrivalOptions, expressOptions } = useLogisticsFormDict()

function shipmentMethodDisplay(code?: string | null): string {
  const c = String(code ?? '').trim()
  if (!c) return '—'
  const hit = shipmentArrivalOptions.value.find((o) => String(o.value) === c)
  return hit?.label ?? c
}

function expressCompanyDisplay(code?: string | null): string {
  const c = String(code ?? '').trim()
  if (!c) return '—'
  const hit = expressOptions.value.find((o) => String(o.value) === c)
  return hit?.label ?? c
}

function displayCustomerName(name?: string | null): string {
  if (maskSaleSensitiveFields.value) return '—'
  return name?.trim() || '—'
}

function displaySalesUserName(name?: string | null): string {
  if (maskSaleSensitiveFields.value) return '—'
  return name?.trim() || '—'
}

const loading = ref(false)
const loadingPickPage = ref(false)
const detail = ref<PackingDetail | null>(null)
const pickPage = ref<PickPageByPacking | null>(null)
const selectedPackingItemId = ref<string | null>(null)
const packingExtendTab = ref<'ship' | 'bill' | 'deliveryReq' | 'box'>('ship')

type KvRow = { key: string; label: string; value: string }

function kvValue(v?: string | null): string {
  const s = v?.trim()
  return s || '—'
}

function kvNumber(v?: number | null): string {
  return v != null ? String(v) : '—'
}

const shipAddressKv = computed<KvRow[]>(() => {
  const d = detail.value
  if (!d) return []
  return [
    { key: 'shipCompany', label: t('packingDetail.shipCompany'), value: kvValue(d.shipCompany) },
    { key: 'shipAddress', label: t('packingDetail.shipAddress'), value: kvValue(d.shipAddress) },
    { key: 'shipAttn', label: t('packingDetail.shipAttn'), value: kvValue(d.shipAttn) },
    { key: 'shipTel', label: t('packingDetail.shipTel'), value: kvValue(d.shipTel) }
  ]
})

const billAddressKv = computed<KvRow[]>(() => {
  const d = detail.value
  if (!d) return []
  return [
    { key: 'billCompany', label: t('packingDetail.billCompany'), value: kvValue(d.billCompany) },
    { key: 'billAddress', label: t('packingDetail.billAddress'), value: kvValue(d.billAddress) },
    { key: 'billAttn', label: t('packingDetail.billAttn'), value: kvValue(d.billAttn) },
    { key: 'billTel', label: t('packingDetail.billTel'), value: kvValue(d.billTel) }
  ]
})

const boxParamsKv = computed<KvRow[]>(() => {
  const d = detail.value
  if (!d) return []
  return [
    { key: 'nw', label: t('packingDetail.boxNw'), value: kvNumber(d.boxNw) },
    { key: 'gw', label: t('packingDetail.boxGw'), value: kvNumber(d.boxGw) },
    { key: 'dim', label: t('packingDetail.boxDim'), value: kvValue(d.boxDim) },
    { key: 'ctns', label: t('packingDetail.boxCtns'), value: kvNumber(d.boxCtns) }
  ]
})

const selectedItemExtends = computed(() => {
  const d = detail.value
  const itemId = selectedPackingItemId.value
  if (!d || !itemId) return []
  return d.itemExtends.filter((e) => e.packingItemId === itemId)
})

const selectedStockOutNotifyRows = computed((): PackingStockOutNotifyRow[] => {
  const d = detail.value
  const itemId = selectedPackingItemId.value
  if (!d || !itemId) return []

  const line = d.items.find((x) => x.id === itemId)
  if (!line) return []

  const all = d.stockOutNotifies ?? []
  const notifyId = line.stockOutNotifyId?.trim()
  if (notifyId) {
    const byId = all.find((n) => n.id === notifyId)
    return byId ? [byId] : []
  }

  const sellItemId = line.sellOrderItemId?.trim()
  if (sellItemId) {
    const bySoItem = all.find((n) => (n.salesOrderItemId?.trim() || '') === sellItemId)
    return bySoItem ? [bySoItem] : []
  }

  return []
})

const stockOutNotifyEmptyText = computed(() => {
  if (!selectedPackingItemId.value) return t('packingDetail.stockOutNotifyNoSelection')
  return t('packingDetail.stockOutNotifyEmpty')
})

const selectedPickingLines = computed((): PickingTaskLine[] => {
  const itemId = selectedPackingItemId.value?.trim()
  if (!itemId || !pickPage.value?.lines?.length) return []
  const pl = pickPage.value.lines.find((l) => l.packingItemId === itemId)
  return pl?.pickingItems ?? []
})

const pickingLinesEmptyText = computed(() => {
  if (!selectedPackingItemId.value) return t('packingDetail.pickingLinesNoSelection')
  return t('packingDetail.pickingLinesNotPicked')
})

function pickingLineStockItemCode(line: PickingTaskLine) {
  const v = line.stockItemCode ?? (line as unknown as Record<string, unknown>).StockItemCode
  const s = String(v ?? '').trim()
  return s || '—'
}

function pickingLineStockInItemCode(line: PickingTaskLine) {
  const v = line.stockInItemCode ?? (line as unknown as Record<string, unknown>).StockInItemCode
  const s = String(v ?? '').trim()
  return s || '—'
}

function pickingLineStockTypeLabel(line: PickingTaskLine) {
  const x = line as unknown as Record<string, unknown>
  const n = line.stockType ?? x.StockType
  if (n == null || n === '') return t('inventoryList.stockTypes.unknown')
  const num = Number(n)
  const m: Record<number, string> = {
    1: t('inventoryList.stockTypes.customer'),
    2: t('inventoryList.stockTypes.stocking'),
    3: t('inventoryList.stockTypes.sample')
  }
  return Number.isFinite(num) ? (m[num] ?? t('inventoryList.stockTypes.unknown')) : t('inventoryList.stockTypes.unknown')
}

function pickingLineIsStocking(line: PickingTaskLine) {
  const x = line as unknown as Record<string, unknown>
  return Boolean(line.isStockingSupplement ?? x.IsStockingSupplement)
}

const selectedPackingItemLabel = computed(() => {
  const d = detail.value
  const itemId = selectedPackingItemId.value
  if (!d || !itemId) return ''
  const line = d.items.find((x) => x.id === itemId)
  if (!line) return itemId
  const pn = line.pn?.trim() || '—'
  const brand = line.brand?.trim() || '—'
  return `${pn} / ${brand} · ${line.qty}${line.unit ? ` ${line.unit}` : ''}`
})

function onPackingItemRowClick(row: PackingDetailLine) {
  const id = String(row?.id || '').trim()
  if (!id) return
  selectedPackingItemId.value = id
}

function packingItemRowClassName({ row }: { row: PackingDetailLine }) {
  return row.id === selectedPackingItemId.value ? 'packing-item-row--active' : ''
}

function stockOutNotifyStatusLabel(s: number) {
  if (s === STOCK_OUT_REQUEST_STATUS.PendingPacking) return t('stockOutNotifyList.status.pendingPacking')
  if (s === STOCK_OUT_REQUEST_STATUS.Packed) return t('stockOutNotifyList.status.packed')
  if (s === STOCK_OUT_REQUEST_STATUS.StockedOut) return t('stockOutNotifyList.status.stockedOut')
  if (s === STOCK_OUT_REQUEST_STATUS.Cancelled) return t('stockOutNotifyList.status.cancelled')
  return t('stockOutNotifyList.status.unknown')
}

function stockOutNotifyRegionLabel(row: PackingStockOutNotifyRow) {
  const n = normalizeRegionType(row.regionType)
  return n === REGION_TYPE_OVERSEAS ? t('inventoryList.warehouse.regionOverseas') : t('inventoryList.warehouse.regionDomestic')
}

function onStockOutNotifyRowDblClick(row: PackingStockOutNotifyRow) {
  const id = String(row?.id || '').trim()
  if (!id) return
  router.push({ name: 'StockOutNotifyDetail', params: { id } })
}

function syncDefaultSelectedItem() {
  const items = detail.value?.items ?? []
  if (!items.length) {
    selectedPackingItemId.value = null
    return
  }
  const current = selectedPackingItemId.value
  if (!current || !items.some((x) => x.id === current)) {
    selectedPackingItemId.value = items[0].id
  }
}

function formatTime(v?: string | null) {
  return v ? formatDisplayDateTime(v) : '—'
}

function goBack() {
  router.push({ name: 'PackingList' })
}

async function loadPickPage(packingId: string) {
  loadingPickPage.value = true
  try {
    pickPage.value = await inventoryCenterApi.getPickPageByPacking(packingId)
  } catch (e) {
    console.error(e)
    pickPage.value = null
  } finally {
    loadingPickPage.value = false
  }
}

async function loadDetail() {
  const id = String(route.params.id || '').trim()
  if (!id) {
    detail.value = null
    pickPage.value = null
    return
  }
  loading.value = true
  pickPage.value = null
  try {
    detail.value = await packingApi.getById(id)
    syncDefaultSelectedItem()
    await loadPickPage(id)
  } catch (e) {
    console.error(e)
    detail.value = null
    pickPage.value = null
    ElMessage.error(e instanceof Error ? e.message : t('packingDetail.loadFailed'))
  } finally {
    loading.value = false
  }
}

watch(
  () => route.params.id,
  () => {
    void loadDetail()
  }
)

onMounted(() => {
  void ensureLogisticsDict()
  void loadDetail()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.packing-detail-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
}

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
}

.page-icon {
  width: 36px;
  height: 36px;
  background: rgba(0, 212, 255, 0.1);
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: $cyan-primary;
  font-size: 14px;
  font-weight: 600;
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
}

.count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}

.btn-secondary {
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  cursor: pointer;
  border: 1px solid $border-panel;
  background: rgba(255, 255, 255, 0.05);
  color: $text-secondary;
}

.detail-card {
  margin-bottom: 16px;
  padding: 16px;
  border-radius: 10px;
  border: 1px solid $border-panel;
  background: $layer-2;
}

.section-title {
  margin: 0 0 12px;
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
}

.section-hint {
  margin: 0 0 10px;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.5;
}

.section-hint--muted {
  color: rgba(0, 212, 255, 0.75);
}

.lines-table {
  width: 100%;
}

.packing-items-table {
  cursor: pointer;
}

:deep(.packing-items-table .el-table__body tr.packing-item-row--active > td) {
  background: rgba(0, 212, 255, 0.12) !important;
}

:deep(.packing-items-table .el-table__body tr.packing-item-row--active > td.el-table__cell) {
  box-shadow: inset 3px 0 0 $cyan-primary;
}

.packing-extend-tabs {
  --el-tabs-header-height: 40px;
}

.packing-extend-tabs :deep(.el-tabs__content) {
  padding: 12px 4px 4px;
}

.packing-kv-descriptions {
  width: 100%;
}

.packing-kv-descriptions :deep(.el-descriptions__label) {
  width: 140px;
  font-weight: 500;
  color: $text-muted;
}

.packing-kv-descriptions :deep(.el-descriptions__content) {
  color: $text-primary;
}

.delivery-req-block {
  padding: 4px 2px;
}

.delivery-req-label {
  font-size: 12px;
  font-weight: 500;
  color: $text-muted;
  margin-bottom: 8px;
}

.delivery-req-value {
  font-size: 13px;
  line-height: 1.65;
  color: $text-primary;
  white-space: pre-wrap;
  word-break: break-word;
}

.packing-stock-out-notify-table {
  cursor: default;
}

.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;

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

.picking-source-stocking {
  color: #e6a23c;
  font-size: 12px;
}

.picking-source-normal {
  font-size: 12px;
  color: $text-secondary;
}
</style>
