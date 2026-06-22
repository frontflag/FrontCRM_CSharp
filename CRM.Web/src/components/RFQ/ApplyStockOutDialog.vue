<template>
  <el-dialog v-model="visible" width="960px" destroy-on-close @closed="onClosed">
    <template #header>
      <div class="apply-stockout-dialog-header">
        <span class="apply-stockout-dialog-header__title">{{ dialogTitle }}</span>
        <span
          v-if="isEdit && applyForm.requestCode?.trim()"
          class="apply-stockout-dialog-header__code"
        >{{ applyForm.requestCode.trim() }}</span>
      </div>
    </template>
    <div v-loading="loading">
      <el-alert
        v-if="applyForm.sellOrderItemId && zeroQtyBannerVisible"
        type="warning"
        :closable="false"
        show-icon
        class="apply-so-stock-alert"
        :title="t('salesOrderItemList.applyStockOutDialog.zeroApplyBanner')"
      />
      <el-alert
        v-if="applyForm.sellOrderItemId && !loading"
        type="info"
        :closable="false"
        show-icon
        class="apply-so-stock-purchasing-info"
        :class="{ 'apply-so-stock-purchasing-info--has-stock': purchasedStockingPurchasingHasQty }"
        :title="purchasedStockingPurchasingBarTitle"
      />
      <el-form :model="applyForm" label-width="140px">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item :label="t('stockOutNotifyList.applyDialog.regionType')">
              <el-select
                :model-value="normalizeRegionType(applyForm.regionType)"
                :teleported="false"
                disabled
                style="width: 100%"
                @update:model-value="(v: string | number) => { applyForm.regionType = normalizeRegionType(v) }"
              >
                <el-option :value="REGION_TYPE_DOMESTIC" :label="t('inventoryList.warehouse.regionDomestic')" />
                <el-option :value="REGION_TYPE_OVERSEAS" :label="t('inventoryList.warehouse.regionOverseas')" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('stockOutNotifyList.applyDialog.expectedShipDate')" required>
              <el-date-picker
                v-model="applyForm.requestDate"
                type="datetime"
                placeholder="选择日期与时间"
                format="YYYY-MM-DD HH:mm"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="客户">
              <el-input :model-value="maskSaleSensitiveFields ? '—' : orderContext.customerName || '--'" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="销售单号">
              <el-input :model-value="orderContext.sellOrderCode || '--'" disabled />
            </el-form-item>
          </el-col>
          <ShipmentExpressFields
            v-model:shipment-method="applyForm.shipmentMethod"
            v-model:express-company="applyForm.expressCompany"
            shipment-label="出货方式"
            placeholder="请选择"
          />
          <el-col :span="24">
            <el-form-item label="备注">
              <el-input v-model="applyForm.remark" type="textarea" :rows="2" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template v-if="applyForm.sellOrderItemId">
        <div class="apply-stock-lines items-table">
          <div class="apply-stock-lines__head">
            <span class="cell cell--idx">#</span>
            <span class="cell cell--pn">物料型号</span>
            <span class="cell cell--brand">品牌</span>
            <span class="cell cell--num">订单数量</span>
            <span class="cell cell--num">已占用通知</span>
            <span class="cell cell--num">尚可申请</span>
            <span class="cell cell--num">在库可用</span>
            <span class="cell cell--num">{{ t('salesOrderItemList.applyStockOutDialog.stockingOnHand') }}</span>
            <span class="cell cell--qty">本次数量</span>
          </div>
          <div class="apply-stock-lines__row">
            <span class="cell cell--idx">1</span>
            <span class="cell cell--pn">{{ applyForm.materialCode }}</span>
            <span class="cell cell--brand">{{ applyForm.materialName }}</span>
            <span class="cell cell--num">{{ salesOrderQtyText }}</span>
            <span class="cell cell--num">{{ alreadyNotifiedText }}</span>
            <span
              class="cell cell--num"
              :class="{ 'cell--num-zero': remainingNotifyZero }"
            >
              {{ remainingNotifyText }}
            </span>
            <span class="cell cell--num">{{ stockQtyText }}</span>
            <span class="cell cell--num">{{ purchasedStockQtyText }}</span>
            <span class="cell cell--qty">
              <span v-if="remainingNotifyZero" class="apply-qty-cannot-apply">
                {{ t('salesOrderItemList.applyStockOutDialog.cannotApply') }}
              </span>
              <el-input-number
                v-else
                v-model="applyForm.notifyQty"
                :min="0"
                :max="applyForm.maxQty"
                :precision="0"
                controls-position="right"
                style="width: 140px"
              />
            </span>
          </div>
        </div>
        <div
          v-if="normalizeRegionType(applyForm.regionType) === REGION_TYPE_DOMESTIC"
          class="apply-stock-inventory-panel"
        >
          <div class="apply-stock-inventory-panel__title">
            {{ t('salesOrderItemList.applyStockOutDialog.customerInventoryTitle') }}
          </div>
          <div class="apply-stock-inventory-panel__rows">
            <div
              v-for="row in customerInventoryRows"
              :key="`cust-${row.regionType}`"
              class="apply-stock-inventory-panel__row"
            >
              <span class="apply-stock-inventory-panel__label">{{ row.label }}</span>
              <span class="apply-stock-inventory-panel__value">{{ row.value }}</span>
            </div>
          </div>
          <div class="apply-stock-inventory-panel__title apply-stock-inventory-panel__title--sub">
            {{ t('salesOrderItemList.applyStockOutDialog.stockingAvailabilityTitle', {
              pn: applyForm.materialCode,
              brand: applyForm.materialName,
              qty: applyForm.notifyQty
            }) }}
          </div>
          <div class="apply-stock-inventory-panel__rows">
            <div
              v-for="row in stockingAvailabilityRows"
              :key="`stocking-${row.regionType}`"
              class="apply-stock-inventory-panel__row"
            >
              <span class="apply-stock-inventory-panel__label">{{ row.label }}</span>
              <span
                class="apply-stock-inventory-panel__value"
                :class="row.isAvailable ? 'apply-stock-inventory-panel__value--yes' : 'apply-stock-inventory-panel__value--no'"
              >
                {{ row.value }}
              </span>
            </div>
          </div>
          <template v-if="customsOptionVisible">
            <el-alert
              type="warning"
              :closable="false"
              show-icon
              class="apply-customs-hint-alert"
              :title="t('salesOrderItemList.applyStockOutDialog.customsCostHint')"
            />
            <div class="apply-customs-option-bar">
              <el-checkbox
                v-model="applyForm.useOverseasWarehouseAndCustoms"
                :disabled="customsOptionLocked"
                class="apply-customs-checkbox"
              >
                {{ t('salesOrderItemList.applyStockOutDialog.useOverseasWarehouseAndCustoms') }}
              </el-checkbox>
            </div>
          </template>
        </div>
      </template>
      <el-empty v-else description="请从上方明细行点击「申请出库」" :image-size="64" />
    </div>
    <template #footer>
      <el-button @click="visible = false">取消</el-button>
      <el-button
        type="primary"
        :loading="submitting"
        :disabled="confirmDisabled"
        @click="submit"
      >
        确定
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch, reactive } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import {
  stockOutApi,
  type StockOutApplyContextDto,
  type StockOutApplyRegionInventoryDto
} from '@/api/stockOut'
import { useAuthStore } from '@/stores/auth'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import ShipmentExpressFields from '@/components/Logistics/ShipmentExpressFields.vue'
import { REGION_TYPE_DOMESTIC, REGION_TYPE_OVERSEAS, normalizeRegionType } from '@/constants/regionType'
import { CurrencyCode } from '@/constants/currency'

export type ApplyStockOutLineInput = Record<string, unknown> & {
  id?: string
  Id?: string
  sellOrderItemId?: string
  pn?: string
  PN?: string
  brand?: string
  Brand?: string
  currency?: number
  Currency?: number
}

export type ApplyStockOutOrderContext = {
  salesOrderId: string
  customerId: string
  customerName: string
  sellOrderCode: string
}

const emit = defineEmits<{ success: [] }>()

const { t } = useI18n()
const authStore = useAuthStore()
const { ensureLoaded: ensureLogisticsDict } = useLogisticsFormDict()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const visible = ref(false)
const isEdit = ref(false)
const submitting = ref(false)
const loading = ref(false)

const orderContext = reactive<ApplyStockOutOrderContext>({
  salesOrderId: '',
  customerId: '',
  customerName: '',
  sellOrderCode: ''
})

const applyForm = ref({
  requestCode: '',
  requestDate: null as Date | null,
  shipmentMethod: '' as string,
  expressCompany: '' as string,
  regionType: REGION_TYPE_DOMESTIC as number,
  remark: '',
  sellOrderItemId: '',
  materialCode: '',
  materialName: '',
  salesOrderQty: 0,
  alreadyNotifiedQty: 0,
  remainingNotifyQty: 0,
  maxQty: 0,
  stockAvailableQty: 0,
  purchasedStockAvailableQty: 0,
  notifyQty: 0,
  customerOrderInventoryByRegion: [] as StockOutApplyRegionInventoryDto[],
  stockingAvailabilityByRegion: [] as StockOutApplyContextDto['stockingAvailabilityByRegion'],
  useOverseasWarehouseAndCustoms: false,
  customsOptionVisible: false,
  customsOptionLocked: false
})

const intQtyText = (v: unknown) => {
  const n = Math.trunc(Number(v) || 0)
  return String(Number.isFinite(n) ? n : 0)
}

const stockQtyText = computed(() => intQtyText(applyForm.value.stockAvailableQty ?? 0))
const purchasedStockQtyText = computed(() => intQtyText(applyForm.value.purchasedStockAvailableQty ?? 0))
const zeroQtyBannerVisible = computed(() => {
  const maxQ = Math.max(0, Math.trunc(Number(applyForm.value.maxQty) || 0))
  const stocking = Math.max(0, Math.trunc(Number(applyForm.value.purchasedStockAvailableQty) || 0))
  return maxQ <= 0 && stocking <= 0
})
const remainingNotifyZero = computed(
  () => Math.max(0, Math.trunc(Number(applyForm.value.remainingNotifyQty) || 0)) <= 0
)
const confirmDisabled = computed(
  () =>
    submitting.value ||
    loading.value ||
    (!!applyForm.value.sellOrderItemId &&
      (zeroQtyBannerVisible.value ||
        remainingNotifyZero.value ||
        !String(applyForm.value.shipmentMethod || '').trim()))
)
const purchasedStockingPurchasingBarTitle = computed(() => {
  const qty = Math.max(0, Math.trunc(Number(applyForm.value.purchasedStockAvailableQty) || 0))
  if (qty > 0)
    return t('salesOrderItemList.applyStockOutDialog.stockingPurchasingSummary', { qty })
  return t('salesOrderItemList.applyStockOutDialog.stockingPurchasingNone')
})
const purchasedStockingPurchasingHasQty = computed(
  () => Math.max(0, Math.trunc(Number(applyForm.value.purchasedStockAvailableQty) || 0)) > 0
)
const salesOrderQtyText = computed(() => intQtyText(applyForm.value.salesOrderQty))
const alreadyNotifiedText = computed(() => intQtyText(applyForm.value.alreadyNotifiedQty))
const remainingNotifyText = computed(() => intQtyText(applyForm.value.remainingNotifyQty))

const dialogTitle = computed(() =>
  isEdit.value
    ? t('stockOutNotifyList.applyDialog.editTitle')
    : t('stockOutNotifyList.applyDialog.createTitle')
)

function regionLabel(regionType: number): string {
  return normalizeRegionType(regionType) === REGION_TYPE_OVERSEAS
    ? t('inventoryList.warehouse.regionOverseas')
    : t('inventoryList.warehouse.regionDomestic')
}

const customerInventoryRows = computed(() => {
  const rows = applyForm.value.customerOrderInventoryByRegion ?? []
  return rows.map((row) => ({
    regionType: row.regionType,
    label: regionLabel(row.regionType),
    value: row.hasInventory
      ? t('salesOrderItemList.applyStockOutDialog.customerInventoryHasQty', {
          qty: intQtyText(row.availableQty)
        })
      : t('salesOrderItemList.applyStockOutDialog.customerInventoryNone')
  }))
})

const stockingAvailabilityRows = computed(() => {
  const rows = applyForm.value.stockingAvailabilityByRegion ?? []
  return rows.map((row) => ({
    regionType: row.regionType,
    isAvailable: row.isAvailable,
    label: regionLabel(row.regionType),
    value: row.isAvailable
      ? t('salesOrderItemList.applyStockOutDialog.stockingAvailableYes')
      : t('salesOrderItemList.applyStockOutDialog.stockingAvailableNo')
  }))
})

const customsOptionVisible = computed(() => applyForm.value.customsOptionVisible)
const customsOptionLocked = computed(() => applyForm.value.customsOptionLocked)

function applyContextToForm(ctx: StockOutApplyContextDto, resetQty = false) {
  const maxQ = Math.max(0, Math.trunc(Number(ctx.suggestedMaxQty) || 0))
  const stocking = Math.max(0, Math.trunc(Number(ctx.purchasedStockAvailableQty ?? 0) || 0))
  const customs = ctx.customsOption ?? { visible: false, defaultChecked: false, locked: false }
  applyForm.value = {
    ...applyForm.value,
    salesOrderQty: Number(ctx.salesOrderQty ?? 0),
    alreadyNotifiedQty: Number(ctx.alreadyNotifiedQty ?? 0),
    remainingNotifyQty: Number(ctx.remainingNotifyQty ?? 0),
    maxQty: maxQ,
    stockAvailableQty: Number(ctx.availableStockQty ?? 0),
    purchasedStockAvailableQty: stocking,
    notifyQty: resetQty ? maxQ : applyForm.value.notifyQty,
    customerOrderInventoryByRegion: ctx.customerOrderInventoryByRegion ?? [],
    stockingAvailabilityByRegion: ctx.stockingAvailabilityByRegion ?? [],
    customsOptionVisible: customs.visible,
    customsOptionLocked: customs.locked,
    useOverseasWarehouseAndCustoms: customs.visible
      ? customs.locked
        ? true
        : resetQty
          ? customs.defaultChecked
          : applyForm.value.useOverseasWarehouseAndCustoms
      : false
  }
}

let contextRefreshTimer: ReturnType<typeof setTimeout> | null = null
async function refreshContextForQty(qty: number) {
  if (!orderContext.salesOrderId || !applyForm.value.sellOrderItemId) return
  try {
    const ctx = await stockOutApi.getApplyContext(
      orderContext.salesOrderId,
      applyForm.value.sellOrderItemId,
      qty > 0 ? qty : undefined
    )
    applyContextToForm(ctx, false)
  } catch {
    // 数量联动刷新失败时不打断用户填写
  }
}

watch(
  () => applyForm.value.notifyQty,
  (qty) => {
    if (!visible.value || !applyForm.value.sellOrderItemId) return
    if (contextRefreshTimer) clearTimeout(contextRefreshTimer)
    contextRefreshTimer = setTimeout(() => {
      void refreshContextForQty(Math.trunc(Number(qty) || 0))
    }, 300)
  }
)

function resolveSellOrderItemId(line: ApplyStockOutLineInput): string {
  return String(line.sellOrderItemId ?? line.id ?? line.Id ?? '').trim()
}

async function open(ctx: ApplyStockOutOrderContext, line: ApplyStockOutLineInput, options?: { isEdit?: boolean }) {
  const sellOrderItemId = resolveSellOrderItemId(line)
  if (!ctx.salesOrderId?.trim()) {
    ElMessage.error('缺少销售订单 ID，无法申请出库')
    return
  }
  if (!sellOrderItemId) {
    ElMessage.error('销售订单明细缺少主键，无法申请出库')
    return
  }

  orderContext.salesOrderId = ctx.salesOrderId.trim()
  orderContext.customerId = ctx.customerId?.trim() || ''
  orderContext.customerName = ctx.customerName?.trim() || ''
  orderContext.sellOrderCode = ctx.sellOrderCode?.trim() || ''

  isEdit.value = options?.isEdit === true
  visible.value = true
  loading.value = true

  const lineCurrency = Number(line.currency ?? line.Currency ?? 0)
  const regionType = lineCurrency === CurrencyCode.RMB ? REGION_TYPE_DOMESTIC : REGION_TYPE_OVERSEAS

  applyForm.value = {
    requestCode: '',
    requestDate: new Date(),
    shipmentMethod: '',
    expressCompany: '',
    regionType,
    remark: '',
    sellOrderItemId,
    materialCode: String(line.pn ?? line.PN ?? '').trim(),
    materialName: String(line.brand ?? line.Brand ?? '').trim(),
    salesOrderQty: 0,
    alreadyNotifiedQty: 0,
    remainingNotifyQty: 0,
    maxQty: 0,
    stockAvailableQty: 0,
    purchasedStockAvailableQty: 0,
    notifyQty: 0,
    customerOrderInventoryByRegion: [],
    stockingAvailabilityByRegion: [],
    useOverseasWarehouseAndCustoms: false,
    customsOptionVisible: false,
    customsOptionLocked: false
  }

  try {
    await ensureLogisticsDict()
    const applyCtx = await stockOutApi.getApplyContext(orderContext.salesOrderId, sellOrderItemId)
    applyContextToForm(applyCtx, true)
  } catch (e: any) {
    ElMessage.error(e?.message || '加载出库申请数据失败')
    visible.value = false
  } finally {
    loading.value = false
  }
}

async function submit() {
  const rd = applyForm.value.requestDate
  if (!rd || !(rd instanceof Date) || Number.isNaN(rd.getTime())) {
    ElMessage.warning('请选择预计出货日期与时间')
    return
  }
  if (!applyForm.value.sellOrderItemId) {
    ElMessage.warning('请选择一条销售订单明细后再申请出库')
    return
  }
  if (!String(applyForm.value.shipmentMethod || '').trim()) {
    ElMessage.warning('请选择出货方式')
    return
  }
  const qty = Number(applyForm.value.notifyQty)
  if (!(qty > 0)) {
    ElMessage.warning('出库通知数量必须大于 0')
    return
  }
  const maxAllowed = Math.max(0, Math.trunc(Number(applyForm.value.maxQty) || 0))
  if (qty > maxAllowed) {
    ElMessage.warning(t('salesOrderItemList.messages.applyStockOutExceedsSuggestedMax', { max: maxAllowed }))
    return
  }

  submitting.value = true
  try {
    await stockOutApi.createRequest({
      requestCode: applyForm.value.requestCode?.trim() || undefined,
      salesOrderId: orderContext.salesOrderId,
      salesOrderItemId: applyForm.value.sellOrderItemId,
      materialCode: applyForm.value.materialCode,
      materialName: applyForm.value.materialName,
      quantity: qty,
      customerId: orderContext.customerId,
      requestUserId: (authStore.user as { id?: string } | null)?.id || '',
      requestDate: rd.toISOString(),
      remark: applyForm.value.remark || undefined,
      shipmentMethod: applyForm.value.shipmentMethod.trim(),
      expressCompany: applyForm.value.expressCompany?.trim() || undefined,
      regionType: normalizeRegionType(applyForm.value.regionType),
      useOverseasWarehouseAndCustoms: applyForm.value.customsOptionVisible
        ? applyForm.value.useOverseasWarehouseAndCustoms
        : undefined
    })
    visible.value = false
    ElMessage.success('申请出库成功')
    emit('success')
  } catch (e: any) {
    ElMessage.error(e?.message || '申请出库失败')
  } finally {
    submitting.value = false
  }
}

function onClosed() {
  if (contextRefreshTimer) {
    clearTimeout(contextRefreshTimer)
    contextRefreshTimer = null
  }
}

defineExpose({ open })
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.apply-stockout-dialog-header {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
}

.apply-stockout-dialog-header__title {
  font-size: 16px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.apply-stockout-dialog-header__code {
  font-size: 16px;
  font-weight: 600;
  color: #e6a23c;
}

.apply-so-stock-purchasing-info {
  margin-bottom: 10px;
}

.apply-so-stock-purchasing-info--has-stock {
  :deep(.el-alert__content),
  :deep(.el-alert__title),
  :deep(.el-alert__icon) {
    color: $success-color !important;
  }
}

.apply-so-stock-alert {
  margin-bottom: 12px;
}

.apply-stock-inventory-panel {
  margin: 12px 0 4px;
  padding: 12px 14px;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  background: $layer-2;
}

.apply-stock-inventory-panel__title {
  font-size: 13px;
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 8px;

  &--sub {
    margin-top: 12px;
  }
}

.apply-stock-inventory-panel__rows {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.apply-stock-inventory-panel__row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  font-size: 13px;
}

.apply-stock-inventory-panel__label {
  color: $text-secondary;
}

.apply-stock-inventory-panel__value {
  color: $text-primary;
  font-variant-numeric: tabular-nums;

  &--yes {
    color: $success-color;
    font-weight: 600;
  }

  &--no {
    color: $text-muted;
  }
}

.apply-customs-hint-alert {
  margin-top: 12px;
}

.apply-customs-option-bar {
  margin-top: 10px;
  padding: 12px 16px;
  display: flex;
  justify-content: center;
  align-items: center;
  text-align: center;
  background: #fce8ec;
  border-radius: 8px;
}

.apply-customs-checkbox {
  :deep(.el-checkbox__label) {
    font-size: 15px;
    font-weight: 700;
    color: $text-primary;
  }
}

.apply-stock-lines {
  margin-top: 8px;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  overflow-x: auto;
  overflow-y: visible;
  background: $layer-2;
}

.apply-stock-lines__head,
.apply-stock-lines__row {
  display: grid;
  grid-template-columns: 44px minmax(96px, 1fr) 84px 66px 66px 66px 66px 66px 132px;
  gap: 8px;
  align-items: center;
  padding: 10px 12px;
  min-width: 760px;
}

.apply-stock-lines__head {
  background: var(--crm-table-header-bg);
  font-size: 12px;
  color: var(--crm-table-header-text);
  font-weight: 600;
  border-bottom: 1px solid var(--crm-table-header-line);

  .cell {
    color: inherit;
  }
}

.apply-stock-lines__row {
  border-bottom: 1px solid $border-panel;
  font-size: 13px;
  color: $text-primary;

  .cell {
    color: inherit;
  }

  &:last-child {
    border-bottom: none;
  }
}

.apply-stock-lines .cell--num {
  text-align: right;
  font-variant-numeric: tabular-nums;
}

.apply-stock-lines .cell--num-zero {
  color: var(--el-color-danger);
  font-weight: 600;
}

.apply-qty-cannot-apply {
  display: inline-block;
  min-width: 140px;
  font-size: 13px;
  color: $text-muted;
}
</style>

<style lang="scss">
@import '@/assets/styles/variables.scss';

.apply-so-stock-purchasing-info--has-stock.el-alert .el-alert__content,
.apply-so-stock-purchasing-info--has-stock.el-alert .el-alert__title,
.apply-so-stock-purchasing-info--has-stock.el-alert .el-alert__icon {
  color: $success-color !important;
}
</style>
