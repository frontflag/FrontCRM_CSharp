<template>
  <div class="finance-page write-off-page">
    <h1 class="finance-list-page-title">{{ t('financeReceiptWriteOff.pageTitle') }}</h1>

    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-model="customerId"
          filterable
          remote
          clearable
          :remote-method="searchCustomer"
          :loading="customerLoading"
          :placeholder="t('financeReceiptWriteOff.customerPh')"
          class="customer-select"
          @change="loadCandidates"
        >
          <el-option
            v-for="c in customerOptions"
            :key="c.id"
            :label="c.customerName || c.customerCode"
            :value="c.id"
          />
        </el-select>
        <el-button type="primary" :disabled="!customerId" @click="loadCandidates">
          {{ t('financeReceiptWriteOff.load') }}
        </el-button>
      </div>
    </div>

    <div v-if="customerId && advanceBalances.length" class="advance-bar">
      <span class="advance-bar__label">{{ t('financeReceiptWriteOff.advanceBalances') }}</span>
      <el-tag v-for="(a, idx) in advanceBalances" :key="idx" type="success" effect="plain" class="advance-tag">
        {{ currencyLabel(a.currency) }} {{ formatAmount(a.balance) }}
      </el-tag>
    </div>

    <div v-if="customerId" class="panels" v-loading="loading">
      <div class="panel">
        <h3>{{ t('financeReceiptWriteOff.receiptItemsTitle') }}</h3>
        <el-table :data="receiptItems" size="small" highlight-current-row @current-change="onReceiptItemSelect">
          <el-table-column prop="financeReceiptCode" :label="t('financeReceiptWriteOff.colReceiptCode')" min-width="120" />
          <el-table-column :label="t('financeReceiptWriteOff.colPurpose')" width="80">
            <template #default="{ row }">
              {{ row.receiptPurpose === 20 ? t('financeReceiptWriteOff.purposeAdvance') : t('financeReceiptWriteOff.purposeNormal') }}
            </template>
          </el-table-column>
          <el-table-column :label="t('financeReceiptWriteOff.colRemaining')" width="110" align="right">
            <template #default="{ row }">{{ formatAmount(row.remainingAmount) }}</template>
          </el-table-column>
          <el-table-column :label="t('financeReceiptWriteOff.colPn')" min-width="100">
            <template #default="{ row }">{{ row.item.pn || '—' }}</template>
          </el-table-column>
        </el-table>
      </div>

      <div class="panel">
        <h3>{{ t('financeReceiptWriteOff.receivablesTitle') }}</h3>
        <el-table :data="receivableRows" size="small">
          <el-table-column prop="stockOutCode" :label="t('financeReceiptWriteOff.colStockOut')" min-width="120" />
          <el-table-column prop="pn" :label="t('financeReceiptWriteOff.colPn')" min-width="100" />
          <el-table-column :label="t('financeReceiptWriteOff.colToBe')" width="100" align="right">
            <template #default="{ row }">{{ formatAmount(row.verifiedToBe) }}</template>
          </el-table-column>
          <el-table-column :label="t('financeReceiptWriteOff.colWriteOffAmount')" width="140">
            <template #default="{ row }">
              <el-input-number
                v-model="row.writeOffAmount"
                :min="0"
                :max="row.verifiedToBe"
                :precision="2"
                :controls="false"
                size="small"
                style="width: 100%"
              />
            </template>
          </el-table-column>
          <el-table-column :label="t('financeReceiptWriteOff.colPoolAmount')" width="140">
            <template #default="{ row }">
              <el-input-number
                v-model="row.poolAmount"
                :min="0"
                :max="Math.min(row.verifiedToBe, poolMaxForCurrency(row.currency))"
                :precision="2"
                :controls="false"
                size="small"
                style="width: 100%"
              />
            </template>
          </el-table-column>
        </el-table>
      </div>
    </div>

    <div v-if="customerId && canWriteFinanceReceipt" class="footer-actions">
      <el-button @click="router.push({ name: 'FinanceReceivableList' })">
        {{ t('financeReceiptWriteOff.back') }}
      </el-button>
      <el-button type="primary" :loading="submitting" @click="submitWriteOff">
        {{ t('financeReceiptWriteOff.submit') }}
      </el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  financeReceivableApi,
  type FinanceAdvancePoolAllocation,
  type FinanceReceiptItemWriteOffCandidate,
  type FinanceReceivable,
  type FinanceReceivableWriteOffSoMismatch
} from '@/api/financeReceivable'
import type { FinanceCustomerAdvanceBalance } from '@/api/financeCustomerAdvance'
import { CURRENCY_MAP } from '@/api/finance'
import { customerApi } from '@/api/customer'
import type { Customer } from '@/types/customer'
import { useFinanceWriteGate } from '@/composables/useDepartmentDataReadOnly'

const { t } = useI18n()
const router = useRouter()
const { canWriteFinanceReceipt } = useFinanceWriteGate()

const customerId = ref('')
const customerOptions = ref<Customer[]>([])
const customerLoading = ref(false)
const loading = ref(false)
const submitting = ref(false)

const receiptItems = ref<FinanceReceiptItemWriteOffCandidate[]>([])
const receivableRows = ref<(FinanceReceivable & { writeOffAmount: number; poolAmount: number })[]>([])
const advanceBalances = ref<FinanceCustomerAdvanceBalance[]>([])
const selectedReceiptItemId = ref('')

function formatAmount(v?: number) {
  if (v == null) return '—'
  return Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function currencyLabel(currency: number) {
  return CURRENCY_MAP[currency] ?? String(currency)
}

function poolMaxForCurrency(currency: number) {
  return advanceBalances.value.find(a => a.currency === currency)?.balance ?? 0
}

async function searchCustomer(keyword: string) {
  if (!keyword?.trim()) {
    customerOptions.value = []
    return
  }
  customerLoading.value = true
  try {
    const res = await customerApi.searchCustomers({
      pageNumber: 1,
      pageSize: 20,
      searchTerm: keyword.trim()
    })
    customerOptions.value = res.items ?? []
  } finally {
    customerLoading.value = false
  }
}

function onReceiptItemSelect(row: FinanceReceiptItemWriteOffCandidate | undefined) {
  selectedReceiptItemId.value = row?.item.id ?? ''
}

async function loadCandidates() {
  if (!customerId.value) return
  loading.value = true
  try {
    const res = await financeReceivableApi.getWriteOffCandidates(customerId.value)
    receiptItems.value = res.receiptItems ?? []
    advanceBalances.value = res.advanceBalances ?? []
    receivableRows.value = (res.receivables ?? []).map(r => ({
      ...r,
      writeOffAmount: 0,
      poolAmount: 0
    }))
    selectedReceiptItemId.value = receiptItems.value[0]?.item.id ?? ''
  } finally {
    loading.value = false
  }
}

function buildPayload(confirmSoMismatch = false) {
  const itemAllocs = selectedReceiptItemId.value
    ? receivableRows.value
        .filter(r => r.writeOffAmount > 0)
        .map(r => ({
          financeReceiptItemId: selectedReceiptItemId.value,
          financeReceivableId: r.id,
          amount: r.writeOffAmount
        }))
    : []

  const poolAllocs: FinanceAdvancePoolAllocation[] = receivableRows.value
    .filter(r => r.poolAmount > 0)
    .map(r => ({
      financeReceivableId: r.id,
      amount: r.poolAmount
    }))

  return {
    allocations: itemAllocs,
    advancePoolAllocations: poolAllocs,
    confirmSoMismatch
  }
}

function validatePayload() {
  const payload = buildPayload()
  const itemTotal = payload.allocations.reduce((s, a) => s + a.amount, 0)
  const poolTotal = (payload.advancePoolAllocations ?? []).reduce((s, a) => s + a.amount, 0)

  if (itemTotal <= 0 && poolTotal <= 0) {
    ElMessage.warning(t('financeReceiptWriteOff.noAmount'))
    return null
  }

  if (itemTotal > 0 && !selectedReceiptItemId.value) {
    ElMessage.warning(t('financeReceiptWriteOff.selectReceiptItem'))
    return null
  }

  const selectedReceipt = receiptItems.value.find(r => r.item.id === selectedReceiptItemId.value)
  if (selectedReceipt && itemTotal > selectedReceipt.remainingAmount + 0.001) {
    ElMessage.warning(t('financeReceiptWriteOff.exceedRemaining'))
    return null
  }

  for (const row of receivableRows.value) {
    const total = row.writeOffAmount + row.poolAmount
    if (total > row.verifiedToBe + 0.001) {
      ElMessage.warning(t('financeReceiptWriteOff.exceedReceivable'))
      return null
    }
  }

  const poolByCurrency = new Map<number, number>()
  for (const row of receivableRows.value) {
    if (row.poolAmount <= 0) continue
    poolByCurrency.set(row.currency, (poolByCurrency.get(row.currency) ?? 0) + row.poolAmount)
  }
  for (const [currency, amount] of poolByCurrency) {
    const max = poolMaxForCurrency(currency)
    if (amount > max + 0.001) {
      ElMessage.warning(t('financeReceiptWriteOff.exceedAdvancePool', { currency: currencyLabel(currency) }))
      return null
    }
  }

  return payload
}

function formatSoMismatchMessage(mismatches: FinanceReceivableWriteOffSoMismatch[]) {
  return mismatches.map(m => m.message || m.financeReceivableId).join('\n')
}

async function submitWriteOff(confirmSoMismatch = false) {
  const payload = validatePayload()
  if (!payload) return

  if (confirmSoMismatch) payload.confirmSoMismatch = true

  submitting.value = true
  try {
    const result = await financeReceivableApi.applyWriteOff(payload)
    if (result?.requiresSoMismatchConfirm && !confirmSoMismatch) {
      await ElMessageBox.confirm(
        formatSoMismatchMessage(result.soMismatches ?? []),
        t('financeReceiptWriteOff.soMismatchTitle'),
        { type: 'warning', confirmButtonText: t('financeReceiptWriteOff.soMismatchConfirm') }
      )
      await submitWriteOff(true)
      return
    }
    ElMessage.success(t('financeReceiptWriteOff.success'))
    await loadCandidates()
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : t('financeReceiptWriteOff.failed')
    ElMessage.error(msg)
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped lang="scss">
@import './finance-common.scss';

.write-off-page {
  .customer-select {
    width: 320px;
  }

  .advance-bar {
    margin-top: 12px;
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 8px;

    &__label {
      font-size: 13px;
      color: var(--el-text-color-secondary);
    }
  }

  .advance-tag {
    font-variant-numeric: tabular-nums;
  }

  .panels {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 20px;
    margin-top: 16px;

    @media (max-width: 960px) {
      grid-template-columns: 1fr;
    }
  }

  .panel h3 {
    margin: 0 0 12px;
    font-size: 15px;
    font-weight: 600;
  }

  .footer-actions {
    margin-top: 20px;
    display: flex;
    gap: 12px;
    justify-content: flex-end;
  }
}
</style>
