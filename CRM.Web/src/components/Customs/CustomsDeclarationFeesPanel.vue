<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import {
  fetchEffectivePurchaseCostParam,
  patchCustomsDeclarationHeader,
  patchCustomsDeclarationItem,
  recalculateCustomsDeclarationFees,
  type CustomsDeclarationDetailDto,
  type CustomsDeclarationDetailItemViewDto
} from '@/api/customs'
import { financeExchangeRateApi } from '@/api/financeExchangeRate'
import { CURRENCY_CODE_TO_TEXT, CurrencyCode } from '@/constants/currency'
import { formatDate as formatDateTimeZh } from '@/utils/date'
import { isValidCustomsAgencyRate } from '@/utils/customsAgencyRate'

const props = defineProps<{
  detail: CustomsDeclarationDetailDto
  canWrite: boolean
  maskPurchase: boolean
}>()

const emit = defineEmits<{
  refresh: []
}>()

const { t } = useI18n()

const recalculating = ref(false)
const systemPurchaseRatio = ref<number | null>(null)
const systemRatioLoadFailed = ref(false)

const headerExchangeRate = ref(0)
const agencyRateManual = ref(false)
const headerBrokerAgencyRate = ref(1)
const itemDrafts = reactive<Record<string, ItemDraft>>({})

type ItemDraft = {
  hsCode: string
  dutyRate: number
  vatRate: number
  otherFee: number
  inspectionFee: number
}

type PanelMode =
  | 'readonly_void'
  | 'readonly_completed'
  | 'readonly_locked'
  | 'blocked_no_p0'
  | 'editable'

function syncDraftsFromDetail(d: CustomsDeclarationDetailDto) {
  headerExchangeRate.value = Number(d.exchangeRate) || 0
  agencyRateManual.value = Boolean(d.agencyRateManual)
  const master = Number(d.brokerMasterAgencyRate)
  const snapshot = Number(d.brokerAgencyRate ?? 1)
  if (agencyRateManual.value) {
    headerBrokerAgencyRate.value = Number.isFinite(snapshot) && snapshot > 0 ? snapshot : 1
  } else if (Number.isFinite(master) && master > 0) {
    headerBrokerAgencyRate.value = master
  } else {
    headerBrokerAgencyRate.value = Number.isFinite(snapshot) && snapshot > 0 ? snapshot : 1
  }
  for (const row of d.items ?? []) {
    itemDrafts[row.id] = {
      hsCode: (row.hsCode ?? '').trim(),
      dutyRate: Number(row.dutyRate ?? 0),
      vatRate: Number(row.vatRate ?? 0.13) || 0.13,
      otherFee: Number(row.otherFee ?? 0),
      inspectionFee: Number(row.inspectionFee ?? 0)
    }
  }
}

watch(
  () => props.detail,
  (d) => {
    syncDraftsFromDetail(d)
    void loadSystemPurchaseRatio()
  },
  { immediate: true, deep: true }
)

async function loadSystemPurchaseRatio() {
  systemRatioLoadFailed.value = false
  try {
    const dto = await fetchEffectivePurchaseCostParam()
    systemPurchaseRatio.value = Number(dto.ratio)
  } catch {
    systemPurchaseRatio.value = null
    systemRatioLoadFailed.value = true
  }
}

const panelMode = computed<PanelMode>(() => {
  const d = props.detail
  if (d.internalStatus === -1) return 'readonly_void'
  if (d.internalStatus === 3) return 'readonly_completed'
  if (d.feesLocked) return 'readonly_locked'
  const items = d.items ?? []
  if (items.some((r) => Number(r.originalPurchasePrice) <= 0)) return 'blocked_no_p0'
  return 'editable'
})

const isLockedPartial = computed(() => panelMode.value === 'readonly_locked')

/** 未拣货（P0=0）仍可改汇率、看到试算按钮（禁用）；与原型 blocked_no_p0 一致 */
const canMaintainFees = computed(
  () => panelMode.value === 'editable' || panelMode.value === 'blocked_no_p0'
)

const canEditHeaderRate = computed(() => props.canWrite && canMaintainFees.value)

const canEditAgencyRate = computed(() => canEditHeaderRate.value && !props.maskPurchase)

const canEditLineCoreInputs = computed(() => props.canWrite && panelMode.value === 'editable')

const canEditLineFooterInputs = computed(
  () => props.canWrite && (panelMode.value === 'editable' || panelMode.value === 'readonly_locked')
)

const showRecalculateActions = computed(() => props.canWrite && canMaintainFees.value)

const showLockedSave = computed(() => props.canWrite && panelMode.value === 'readonly_locked')

const hasMissingP0 = computed(() =>
  (props.detail.items ?? []).some((r) => Number(r.originalPurchasePrice) <= 0)
)

const snapshotPurchaseRatio = computed(() => {
  const items = props.detail.items ?? []
  const first = items.find((r) => r.purchaseRatio != null && Number(r.purchaseRatio) > 0)
  return first?.purchaseRatio ?? null
})

const headerPurchaseRatio = computed(() => {
  if (snapshotPurchaseRatio.value != null && Number(snapshotPurchaseRatio.value) > 0) {
    return snapshotPurchaseRatio.value
  }
  return systemPurchaseRatio.value
})

const ratioStale = computed(() => {
  if (props.maskPurchase) return false
  if (systemPurchaseRatio.value == null || snapshotPurchaseRatio.value == null) return false
  return Math.abs(systemPurchaseRatio.value - snapshotPurchaseRatio.value) > 0.0001
})

const feesStatusTag = computed(() => {
  if (panelMode.value === 'readonly_void') return { type: 'danger' as const, text: t('customsPages.declarations.internalVoid') }
  if (panelMode.value === 'readonly_completed') return { type: 'success' as const, text: t('customsPages.declarations.internalDone') }
  if (panelMode.value === 'readonly_locked') return { type: 'warning' as const, text: t('customsPages.fees.statusLocked') }
  if (!props.detail.feesCalculatedAt) return { type: 'info' as const, text: t('customsPages.fees.statusNotCalculated') }
  if (props.detail.canCreateArrivalNotifies) return { type: 'success' as const, text: t('customsPages.fees.statusReadyArrival') }
  return { type: 'warning' as const, text: t('customsPages.fees.statusCalculated') }
})

const recalcDisabled = computed(() => {
  if (!showRecalculateActions.value) return true
  if (headerExchangeRate.value <= 0) return true
  if (systemRatioLoadFailed.value || systemPurchaseRatio.value == null) return true
  if (hasMissingP0.value) return true
  if (agencyRateManual.value && !isValidCustomsAgencyRate(headerBrokerAgencyRate.value)) return true
  return recalculating.value
})

function agencyRateHint(rate: number | undefined): string {
  const r = Number(rate)
  if (!Number.isFinite(r) || r <= 1) return '—'
  const pct = (r - 1) * 100
  return t('customsPages.fees.agencyRateHint', { pct: pct.toFixed(2) })
}

const agencyRateMode = computed({
  get: () => (agencyRateManual.value ? 'manual' : 'system'),
  set: (mode: string) => {
    agencyRateManual.value = mode === 'manual'
    if (!agencyRateManual.value) {
      const master = Number(props.detail.brokerMasterAgencyRate)
      headerBrokerAgencyRate.value = Number.isFinite(master) && master > 0 ? master : 1
    }
  }
})

function currencyText(code: number | null | undefined): string {
  if (code == null) return '—'
  return CURRENCY_CODE_TO_TEXT[code] ?? String(code)
}

function moneyText(n: number | null | undefined): string {
  if (props.maskPurchase) return '—'
  const x = Number(n)
  if (!Number.isFinite(x)) return '—'
  if (x === 0) return '0.00'
  return x.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function unitPriceText(n: number | null | undefined): string {
  if (props.maskPurchase) return '—'
  const x = Number(n)
  if (!Number.isFinite(x)) return '—'
  if (x === 0) return '0.000000'
  return x.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 6 })
}

function ratioText(n: number | null | undefined): string {
  if (props.maskPurchase) return '—'
  if (n == null) return '—'
  const x = Number(n)
  if (!Number.isFinite(x)) return '—'
  return x.toFixed(4)
}

function lineHasFeeSnapshot(row: CustomsDeclarationDetailItemViewDto): boolean {
  return Boolean(props.detail.feesCalculatedAt) || Number(row.costUsd) > 0
}

function linePurchaseRatio(row: CustomsDeclarationDetailItemViewDto): number | null {
  const ratio = row.purchaseRatio
  if (ratio != null && Number(ratio) > 0) return Number(ratio)
  if (lineHasFeeSnapshot(row) && headerPurchaseRatio.value != null) return headerPurchaseRatio.value
  return null
}

function linePurchaseCurrency(row: CustomsDeclarationDetailItemViewDto): number | null {
  const currency = row.purchaseCurrency
  if (currency != null && Number(currency) > 0) return Number(currency)
  if (lineHasFeeSnapshot(row)) return CurrencyCode.RMB
  return null
}

function usd6Text(n: number | null | undefined): string {
  if (props.maskPurchase) return '—'
  const x = Number(n)
  if (!Number.isFinite(x)) return '—'
  return x.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 6 })
}

function calculatedAtText(iso: string | null | undefined): string {
  if (!iso) return '—'
  return formatDateTimeZh(iso, 'YYYY-MM-DD HH:mm')
}

function rowDraft(row: CustomsDeclarationDetailItemViewDto): ItemDraft {
  if (!itemDrafts[row.id]) {
    itemDrafts[row.id] = {
      hsCode: (row.hsCode ?? '').trim(),
      dutyRate: Number(row.dutyRate ?? 0),
      vatRate: Number(row.vatRate ?? 0.13) || 0.13,
      otherFee: Number(row.otherFee ?? 0),
      inspectionFee: Number(row.inspectionFee ?? 0)
    }
  }
  return itemDrafts[row.id]
}

function rowMissingP0(row: CustomsDeclarationDetailItemViewDto): boolean {
  return Number(row.originalPurchasePrice) <= 0
}

function validateDrafts(): string | null {
  for (const row of props.detail.items ?? []) {
    const d = rowDraft(row)
    if (d.dutyRate < 0) return t('customsPages.fees.validateDutyNegative')
    if (d.dutyRate === 0 && !d.hsCode.trim()) return t('customsPages.fees.validateZeroDutyHs', { line: row.lineNo })
    if (d.vatRate <= 0) return t('customsPages.fees.validateVatPositive', { line: row.lineNo })
  }
  if (headerExchangeRate.value <= 0) return t('customsPages.fees.alertNoExchangeRate')
  if (agencyRateManual.value && !isValidCustomsAgencyRate(headerBrokerAgencyRate.value)) {
    return t('customsPages.fees.validateAgencyRate')
  }
  return null
}

async function applyFinanceRate() {
  try {
    const fx = await financeExchangeRateApi.getCurrent()
    const rate = Number(fx.usdToCny)
    if (!Number.isFinite(rate) || rate <= 0) {
      ElMessage.warning(t('customsPages.fees.financeRateInvalid'))
      return
    }
    headerExchangeRate.value = rate
    ElMessage.success(t('customsPages.fees.financeRateApplied'))
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  }
}

async function persistDirtyFields(): Promise<void> {
  const d = props.detail
  const headerPatch: Parameters<typeof patchCustomsDeclarationHeader>[1] = {}
  const serverRate = Number(d.exchangeRate) || 0
  if (Math.abs(headerExchangeRate.value - serverRate) > 0.000001) {
    headerPatch.exchangeRate = headerExchangeRate.value
  }

  const serverManual = Boolean(d.agencyRateManual)
  const serverAgency = Number(d.brokerAgencyRate ?? 1)
  if (canEditAgencyRate.value) {
    if (agencyRateManual.value) {
      if (!isValidCustomsAgencyRate(headerBrokerAgencyRate.value)) {
        throw new Error(t('customsPages.fees.validateAgencyRate'))
      }
      if (!serverManual || Math.abs(headerBrokerAgencyRate.value - serverAgency) > 0.000001) {
        headerPatch.agencyRateManual = true
        headerPatch.brokerAgencyRate = headerBrokerAgencyRate.value
      }
    } else if (serverManual) {
      headerPatch.agencyRateManual = false
    }
  }

  if (Object.keys(headerPatch).length > 0) {
    await patchCustomsDeclarationHeader(d.id, headerPatch)
  }

  for (const row of d.items ?? []) {
    const draft = rowDraft(row)
    const patch: Parameters<typeof patchCustomsDeclarationItem>[1] = {}
    const hs = draft.hsCode.trim()
    if (hs !== (row.hsCode ?? '').trim()) patch.hsCode = hs || null
    if (Math.abs(draft.dutyRate - Number(row.dutyRate ?? 0)) > 0.000001) patch.dutyRate = draft.dutyRate
    if (Math.abs(draft.vatRate - Number(row.vatRate ?? 0.13)) > 0.000001) patch.vatRate = draft.vatRate
    if (Math.abs(draft.otherFee - Number(row.otherFee ?? 0)) > 0.000001) patch.otherFee = draft.otherFee
    if (Math.abs(draft.inspectionFee - Number(row.inspectionFee ?? 0)) > 0.000001) {
      patch.inspectionFee = draft.inspectionFee
    }
    if (Object.keys(patch).length > 0) {
      await patchCustomsDeclarationItem(row.id, patch)
    }
  }
}

async function handleRecalculate() {
  const err = validateDrafts()
  if (err) {
    ElMessage.warning(err)
    return
  }
  recalculating.value = true
  try {
    await persistDirtyFields()
    await recalculateCustomsDeclarationFees(props.detail.id)
    ElMessage.success(t('customsPages.fees.recalculateOk'))
    emit('refresh')
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  } finally {
    recalculating.value = false
  }
}

async function handleSaveLockedFooter() {
  recalculating.value = true
  try {
    for (const row of props.detail.items ?? []) {
      const draft = rowDraft(row)
      const patch: Parameters<typeof patchCustomsDeclarationItem>[1] = {}
      if (Math.abs(draft.otherFee - Number(row.otherFee ?? 0)) > 0.000001) patch.otherFee = draft.otherFee
      if (Math.abs(draft.inspectionFee - Number(row.inspectionFee ?? 0)) > 0.000001) {
        patch.inspectionFee = draft.inspectionFee
      }
      if (Object.keys(patch).length > 0) {
        await patchCustomsDeclarationItem(row.id, patch)
      }
    }
    ElMessage.success(t('customsPages.fees.saveOk'))
    emit('refresh')
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  } finally {
    recalculating.value = false
  }
}

async function handleSave() {
  if (isLockedPartial.value) {
    await handleSaveLockedFooter()
    return
  }
  await handleRecalculate()
}

function rowClassName({ row }: { row: CustomsDeclarationDetailItemViewDto }) {
  return rowMissingP0(row) ? 'fees-row--no-p0' : ''
}
</script>

<template>
  <div class="info-section customs-fees-panel">
    <div class="section-header">
      <div class="section-header__main">
        <div class="section-dot section-dot--cyan"></div>
        <span class="section-title">{{ t('customsPages.declarations.sectionFees') }}</span>
        <el-tag :type="feesStatusTag.type" size="small" effect="dark">{{ feesStatusTag.text }}</el-tag>
        <span v-if="detail.feesCalculatedAt" class="fees-calculated-at">
          {{ t('customsPages.fees.calculatedAt') }}：{{ calculatedAtText(detail.feesCalculatedAt) }}
        </span>
      </div>
      <div v-if="showRecalculateActions || showLockedSave" class="section-header__actions">
        <el-button
          v-if="showRecalculateActions"
          size="small"
          :loading="recalculating"
          :disabled="recalcDisabled"
          @click="handleRecalculate"
        >
          {{ t('customsPages.fees.btnRecalculate') }}
        </el-button>
        <el-button
          type="primary"
          size="small"
          :loading="recalculating"
          :disabled="showRecalculateActions ? recalcDisabled : false"
          @click="handleSave"
        >
          {{ t('customsPages.fees.btnSave') }}
        </el-button>
      </div>
    </div>

    <div class="customs-fees-panel__body">
      <el-alert
        v-if="headerExchangeRate <= 0 && canEditHeaderRate"
        type="warning"
        :closable="false"
        show-icon
        class="fees-alert"
        :title="t('customsPages.fees.alertNoExchangeRate')"
      />
      <el-alert
        v-if="systemRatioLoadFailed"
        type="error"
        :closable="false"
        show-icon
        class="fees-alert"
        :title="t('customsPages.fees.alertNoPurchaseRatio')"
      />
      <el-alert
        v-if="isLockedPartial"
        type="warning"
        :closable="false"
        show-icon
        class="fees-alert"
        :title="t('customsPages.fees.alertLockedPartial')"
      />
      <el-alert
        v-if="hasMissingP0 && canMaintainFees"
        type="warning"
        :closable="false"
        show-icon
        class="fees-alert"
        :title="t('customsPages.fees.alertNoP0')"
      />
      <el-alert
        v-if="ratioStale"
        type="info"
        :closable="false"
        show-icon
        class="fees-alert"
        :title="t('customsPages.fees.alertRatioStale')"
      />

      <div class="fees-header-grid info-grid info-grid--inline-labels">
        <div class="info-item info-item--field-highlight">
          <span class="info-label">{{ t('customsPages.fees.exchangeRate') }}</span>
          <span class="info-value fees-exchange-rate-value">
            <el-input-number
              v-if="canEditHeaderRate"
              v-model="headerExchangeRate"
              :min="0"
              :precision="6"
              :step="0.0001"
              controls-position="right"
              class="fees-input-number fees-field-highlight"
            />
            <span v-else>{{ maskPurchase ? '—' : headerExchangeRate > 0 ? headerExchangeRate.toFixed(6) : '—' }}</span>
            <el-button
              v-if="showRecalculateActions && canEditHeaderRate"
              size="small"
              @click="applyFinanceRate"
            >
              {{ t('customsPages.fees.btnApplyFinanceRate') }}
            </el-button>
          </span>
        </div>
        <div class="info-item">
          <span class="info-label">{{ t('customsPages.fees.agencyRate') }}</span>
          <span class="info-value fees-exchange-rate-value">
            <el-input-number
              v-if="canEditAgencyRate && agencyRateManual"
              v-model="headerBrokerAgencyRate"
              :min="1"
              :precision="6"
              :step="0.001"
              controls-position="right"
              class="fees-input-number fees-field-highlight"
            />
            <span v-else>{{ maskPurchase ? '—' : headerBrokerAgencyRate.toFixed(6) }}</span>
            <span v-if="!maskPurchase" class="fees-hint">{{ agencyRateHint(headerBrokerAgencyRate) }}</span>
            <el-radio-group
              v-if="canEditAgencyRate"
              v-model="agencyRateMode"
              size="small"
              class="fees-agency-rate-mode"
            >
              <el-radio-button value="system">{{ t('customsPages.fees.agencyRateModeSystem') }}</el-radio-button>
              <el-radio-button value="manual">{{ t('customsPages.fees.agencyRateModeManual') }}</el-radio-button>
            </el-radio-group>
          </span>
        </div>
        <div class="info-item">
          <span class="info-label">{{ t('customsPages.fees.purchaseRatio') }}</span>
          <span class="info-value">
            {{ ratioText(headerPurchaseRatio) }}
            <span
              v-if="
                systemPurchaseRatio != null &&
                !maskPurchase &&
                snapshotPurchaseRatio != null &&
                Number(snapshotPurchaseRatio) > 0 &&
                Math.abs(systemPurchaseRatio - Number(snapshotPurchaseRatio)) > 0.0001
              "
              class="fees-hint"
            >
              {{ t('customsPages.fees.systemRatioHint', { ratio: systemPurchaseRatio.toFixed(4) }) }}
            </span>
          </span>
        </div>
        <div class="info-item">
          <span class="info-label">{{ t('customsPages.declarations.colTotal') }}</span>
          <span class="info-value">{{ moneyText(detail.totalTaxAmount) }}</span>
        </div>
      </div>

      <div v-if="detail.items?.length" class="fees-lines-wrap">
        <el-table
          :data="detail.items"
          size="small"
          border
          :fit="false"
          class="detail-panel-list-table fees-lines-table"
          :row-class-name="rowClassName"
        >
          <el-table-column prop="lineNo" label="#" width="52" align="center" />
          <CrmCopyableTableColumn prop="purchasePn" :label="t('customsPages.items.colPn')" min-width="128" />
          <CrmCopyableTableColumn prop="purchaseBrand" :label="t('customsPages.items.colBrand')" min-width="108" />
          <el-table-column prop="declareQty" :label="t('customsPages.items.colQty')" min-width="112" align="right" />
          <el-table-column :label="t('stockInDetail.originalPrice')" min-width="118" align="right">
            <template #default="{ row }">{{ unitPriceText(row.originalPurchasePrice) }}</template>
          </el-table-column>
          <el-table-column :label="t('customsPages.fees.purchaseCurrency')" min-width="112" align="center">
            <template #default="{ row }">{{ maskPurchase ? '—' : currencyText(linePurchaseCurrency(row)) }}</template>
          </el-table-column>
          <el-table-column :label="t('customsPages.fees.purchaseRatio')" min-width="112" align="right">
            <template #default="{ row }">{{ ratioText(linePurchaseRatio(row)) }}</template>
          </el-table-column>
          <el-table-column :label="t('customsPages.fees.costUsd')" min-width="118" align="right">
            <template #default="{ row }">{{ usd6Text(row.costUsd) }}</template>
          </el-table-column>
          <el-table-column :label="t('customsPages.items.colHs')" min-width="128">
            <template #default="{ row }">
              <el-input
                v-if="canEditLineCoreInputs && !rowMissingP0(row)"
                v-model="rowDraft(row).hsCode"
                size="small"
                maxlength="32"
                class="fees-hs-input fees-field-highlight"
              />
              <span v-else>{{ row.hsCode || '—' }}</span>
            </template>
          </el-table-column>
          <el-table-column :label="t('customsPages.fees.dutyRate')" min-width="140" align="right">
            <template #default="{ row }">
              <el-input-number
                v-if="canEditLineCoreInputs && !rowMissingP0(row)"
                v-model="rowDraft(row).dutyRate"
                size="small"
                :min="0"
                :precision="6"
                :step="0.01"
                :controls="false"
                class="fees-input-number fees-input-number--plain fees-field-highlight"
              />
              <span v-else>{{ Number(row.dutyRate ?? 0).toFixed(6) }}</span>
            </template>
          </el-table-column>
          <el-table-column :label="t('customsPages.fees.vatRate')" min-width="140" align="right">
            <template #default="{ row }">
              <el-input-number
                v-if="canEditLineCoreInputs && !rowMissingP0(row)"
                v-model="rowDraft(row).vatRate"
                size="small"
                :min="0.000001"
                :precision="6"
                :step="0.01"
                :controls="false"
                class="fees-input-number fees-input-number--plain fees-field-highlight"
              />
              <span v-else>{{ Number(row.vatRate ?? 0.13).toFixed(6) }}</span>
            </template>
          </el-table-column>
          <el-table-column :label="t('customsPages.items.colGoods')" min-width="128" align="right">
            <template #default="{ row }">{{ moneyText(row.customsPaymentGoods) }}</template>
          </el-table-column>
          <el-table-column :label="t('customsPages.items.colDuty')" min-width="88" align="right">
            <template #default="{ row }">{{ moneyText(row.dutyAmount) }}</template>
          </el-table-column>
          <el-table-column :label="t('customsPages.items.colVat')" min-width="88" align="right">
            <template #default="{ row }">{{ moneyText(row.vatAmount) }}</template>
          </el-table-column>
          <el-table-column :label="t('customsPages.items.colAgency')" min-width="128" align="right">
            <template #default="{ row }">{{ moneyText(row.customsAgencyFee) }}</template>
          </el-table-column>
          <el-table-column :label="t('customsPages.items.colOther')" min-width="156" align="right">
            <template #default="{ row }">
              <span
                v-if="canEditLineFooterInputs && (!rowMissingP0(row) || isLockedPartial)"
                class="fees-footer-fee-row"
              >
                <el-input-number
                  v-model="rowDraft(row).otherFee"
                  size="small"
                  :precision="2"
                  :step="1"
                  controls-position="right"
                  class="fees-input-number fees-input-number--footer"
                />
                <span class="fees-footer-fee-row__text">{{ moneyText(rowDraft(row).otherFee) }}</span>
              </span>
              <span v-else>{{ moneyText(row.otherFee) }}</span>
            </template>
          </el-table-column>
          <el-table-column :label="t('customsPages.items.colInspection')" min-width="156" align="right">
            <template #default="{ row }">
              <span
                v-if="canEditLineFooterInputs && (!rowMissingP0(row) || isLockedPartial)"
                class="fees-footer-fee-row"
              >
                <el-input-number
                  v-model="rowDraft(row).inspectionFee"
                  size="small"
                  :precision="2"
                  :step="1"
                  controls-position="right"
                  class="fees-input-number fees-input-number--footer"
                />
                <span class="fees-footer-fee-row__text">{{ moneyText(rowDraft(row).inspectionFee) }}</span>
              </span>
              <span v-else>{{ moneyText(row.inspectionFee) }}</span>
            </template>
          </el-table-column>
          <el-table-column :label="t('customsPages.items.colTotalTax')" min-width="108" align="right">
            <template #default="{ row }">{{ moneyText(row.totalValueTax) }}</template>
          </el-table-column>
          <el-table-column :label="t('customsPages.items.colTaxUnit')" min-width="118" align="right">
            <template #default="{ row }">{{ unitPriceText(row.taxIncludedUnitPrice) }}</template>
          </el-table-column>
        </el-table>
      </div>

      <el-collapse class="fees-formula-collapse">
        <el-collapse-item :title="t('customsPages.fees.formulaTitle')" name="formula">
          <pre class="fees-formula-text">{{ t('customsPages.fees.formulaBody') }}</pre>
        </el-collapse-item>
      </el-collapse>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

$fees-highlight-text: #78350f;

.customs-fees-panel {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  margin-bottom: 16px;
  overflow: hidden;
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: var(--crm-detail-section-header-bg);
  flex-wrap: wrap;
}

.section-header__main {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
  flex-wrap: wrap;
}

.fees-calculated-at {
  font-size: 12px;
  color: $text-muted;
  white-space: nowrap;
}

.section-header__actions {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.section-title {
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
}

.section-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;

  &--cyan {
    background: $cyan-primary;
    box-shadow: 0 0 6px rgba(0, 212, 255, 0.6);
  }
}

.customs-fees-panel__body {
  padding: 12px 16px 16px;
}

.fees-alert {
  margin-bottom: 10px;
}

.fees-header-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  margin-bottom: 12px;
  border: 1px solid rgba(255, 255, 255, 0.05);
  border-radius: 8px;
  overflow: hidden;
}

.fees-header-grid.info-grid--inline-labels .info-item {
  flex-direction: row;
  align-items: center;
  gap: 8px;
  padding: 10px 16px;
  border-bottom: none;
  border-right: 1px solid rgba(255, 255, 255, 0.04);
  min-width: 0;

  &:nth-child(4n) {
    border-right: none;
  }
}

.info-label {
  font-size: 12px;
  color: $text-muted;
  flex-shrink: 0;

  &::after {
    content: '：';
  }
}

.info-item--field-highlight .info-label {
  color: $fees-highlight-text;
}

.info-value {
  font-size: 13px;
  color: $text-secondary;
  flex: 1;
  min-width: 0;
}

.fees-hint {
  margin-left: 6px;
  font-size: 11px;
  color: $text-muted;
}

.fees-exchange-rate-value {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  min-width: 0;
}

.fees-agency-rate-mode {
  flex-shrink: 0;
}

.fees-lines-wrap {
  overflow-x: auto;
  max-width: 100%;
  -webkit-overflow-scrolling: touch;
}

:deep(.fees-lines-table) {
  width: max-content;
  min-width: 100%;

  .el-table__header-wrapper th.el-table__cell .cell,
  .el-table__fixed-header-wrapper th.el-table__cell .cell {
    white-space: nowrap;
    overflow: visible;
    text-overflow: clip;
  }

  .el-table__body-wrapper td.el-table__cell .cell,
  .el-table__fixed-body-wrapper td.el-table__cell .cell {
    white-space: nowrap;
  }
}

.fees-hs-input {
  width: 100%;
  min-width: 96px;
}

.fees-input-number {
  width: 100%;
  max-width: 120px;
}

.fees-field-highlight {
  :deep(.el-input__wrapper) {
    background-color: #fffbeb !important;
    border-color: #fde68a !important;
    box-shadow: none !important;
  }

  :deep(.el-input__wrapper:hover) {
    border-color: #fbbf24 !important;
  }

  :deep(.el-input__wrapper.is-focus) {
    background-color: #fffbeb !important;
    border-color: #f59e0b !important;
    box-shadow: 0 0 0 2px rgba(251, 191, 36, 0.28) !important;
  }

  :deep(.el-input__inner) {
    color: $fees-highlight-text !important;
  }
}

.fees-input-number--spin {
  width: 120px;
  min-width: 120px;
  max-width: none;

  :deep(.el-input__wrapper) {
    padding-left: 8px;
    padding-right: 4px;
  }

  :deep(.el-input__inner) {
    text-align: right;
  }
}

.fees-input-number--plain {
  width: 120px;
  min-width: 120px;
  max-width: none;

  :deep(.el-input__wrapper) {
    padding-left: 8px;
    padding-right: 8px;
  }

  :deep(.el-input__inner) {
    text-align: right;
  }
}

.fees-footer-fee-row {
  display: inline-flex;
  align-items: center;
  justify-content: flex-end;
  gap: 6px;
  width: 100%;
  min-width: max-content;
}

.fees-input-number--footer {
  flex: 0 0 auto;
  width: auto;
  min-width: 104px;
  max-width: none;

  :deep(.el-input__wrapper) {
    padding-left: 8px;
    padding-right: 4px;
  }

  :deep(.el-input__inner) {
    text-align: right;
    overflow: visible;
  }
}

.fees-footer-fee-row__text {
  flex: 0 0 auto;
  min-width: 4.5em;
  text-align: right;
  white-space: nowrap;
  font-variant-numeric: tabular-nums;
  font-size: 13px;
  color: $text-secondary;
}

:deep(.fees-row--no-p0) {
  background: rgba(245, 108, 108, 0.08) !important;
}

.fees-formula-collapse {
  margin-top: 12px;
  border: none;
  background: transparent;
}

.fees-formula-text {
  margin: 0;
  font-size: 12px;
  line-height: 1.6;
  color: $text-muted;
  white-space: pre-wrap;
  font-family: inherit;
}
</style>
