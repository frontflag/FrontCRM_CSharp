<template>
  <div class="customer-quote-edit-page" v-loading="loading">
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="router.back()">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          {{ t('customerQuoteEdit.back') }}
        </button>
        <div v-if="quote" class="title-block">
          <h1 class="page-title">{{ displayCode }}</h1>
          <el-tag effect="dark" size="small" :type="statusTagType">{{ statusText }}</el-tag>
        </div>
      </div>
      <div v-if="editable" class="header-actions">
        <button type="button" class="btn-ghost btn-sm" :disabled="saving" @click="handleApplyProfitFactor">
          {{ t('customerQuoteEdit.applyProfitFactor') }}
        </button>
        <button type="button" class="btn-primary btn-sm" :disabled="saving" @click="handleSave">
          {{ t('common.save') }}
        </button>
      </div>
    </div>

    <div v-if="quote" class="form-section">
      <div class="section-title">{{ t('customerQuoteEdit.customerSection') }}</div>
      <div class="info-grid">
        <div class="info-item">
          <span class="label">{{ t('customerQuoteEdit.customer') }}</span>
          <span>{{ quote.customerName || '—' }}</span>
        </div>
        <div class="info-item">
          <span class="label">{{ t('customerQuoteEdit.salesUser') }}</span>
          <span>{{ quote.salesUserName || '—' }}</span>
        </div>
        <div class="info-item">
          <span class="label">{{ t('customerQuoteEdit.contactName') }}</span>
          <el-input v-if="editable" v-model="form.contactName" size="small" />
          <span v-else>{{ quote.contactName || '—' }}</span>
        </div>
        <div class="info-item">
          <span class="label">{{ t('customerQuoteEdit.contactEmail') }}</span>
          <el-input v-if="editable" v-model="form.contactEmail" size="small" />
          <span v-else>{{ quote.contactEmail || '—' }}</span>
        </div>
        <div class="info-item">
          <span class="label">{{ t('customerQuoteEdit.profitFactor') }}</span>
          <el-input-number
            v-if="editable"
            v-model="form.profitFactor"
            :min="0.01"
            :max="99.99"
            :step="0.01"
            :precision="2"
            size="small"
          />
          <span v-else>{{ Number(quote.profitFactor).toFixed(2) }}</span>
        </div>
      </div>
    </div>

    <div v-if="quote" class="items-section">
      <div class="section-title">{{ t('customerQuoteEdit.itemsSection') }}</div>
      <el-table :data="form.items" border stripe size="small" class="items-table">
        <el-table-column type="index" width="48" />
        <el-table-column prop="mpn" :label="t('customerQuoteEdit.colMpn')" min-width="120" show-overflow-tooltip />
        <el-table-column prop="brand" :label="t('customerQuoteEdit.colBrand')" min-width="90" show-overflow-tooltip />
        <el-table-column prop="quantity" :label="t('customerQuoteEdit.colQty')" width="90" align="right" />
        <el-table-column :label="t('customerQuoteEdit.colPurchasePrice')" min-width="120" align="right">
          <template #default="{ row }">
            {{ formatPrice(row.purchasePrice) }}
            <span class="ccy">{{ currencyLabel(row.purchaseCurrency) }}</span>
          </template>
        </el-table-column>
        <el-table-column :label="t('customerQuoteEdit.colSendPrice')" min-width="140">
          <template #default="{ row }">
            <template v-if="editable">
              <el-input-number
                v-model="row.sendPrice"
                :min="0"
                :precision="6"
                :step="0.000001"
                size="small"
                controls-position="right"
              />
              <span class="ccy">{{ currencyLabel(row.sendCurrency) }}</span>
            </template>
            <template v-else>
              {{ formatPrice(row.sendPrice) }}
              <span class="ccy">{{ currencyLabel(row.sendCurrency) }}</span>
            </template>
          </template>
        </el-table-column>
        <el-table-column :label="t('customerQuoteEdit.colLocked')" width="72" align="center">
          <template #default="{ row }">
            <el-checkbox v-if="editable" v-model="row.isLocked" />
            <span v-else>{{ row.isLocked ? t('customerQuoteEdit.yes') : t('customerQuoteEdit.no') }}</span>
          </template>
        </el-table-column>
        <el-table-column :label="t('customerQuoteEdit.colLeadTime')" min-width="100">
          <template #default="{ row }">
            <el-input v-if="editable" v-model="row.leadTime" size="small" />
            <span v-else>{{ row.leadTime || '—' }}</span>
          </template>
        </el-table-column>
        <el-table-column :label="t('customerQuoteEdit.colDateCode')" min-width="100">
          <template #default="{ row }">
            <el-input v-if="editable" v-model="row.dateCode" size="small" />
            <span v-else>{{ row.dateCode || '—' }}</span>
          </template>
        </el-table-column>
        <el-table-column :label="t('customerQuoteEdit.colRemark')" min-width="120">
          <template #default="{ row }">
            <el-input v-if="editable" v-model="row.remark" size="small" />
            <span v-else>{{ row.remark || '—' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="sourceQuoteCode" :label="t('customerQuoteEdit.colSourceQuote')" width="110" />
        <el-table-column prop="purchaseUserName" :label="t('customerQuoteEdit.colPurchaser')" width="100" />
      </el-table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import {
  customerQuoteApi,
  type CustomerQuoteItemRow,
  type CustomerQuoteRow
} from '@/api/customerQuote'
import { useAuthStore } from '@/stores/auth'
import { listAmountCurrencyIso } from '@/utils/moneyFormat'
import { getApiErrorMessage } from '@/utils/apiError'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const loading = ref(false)
const saving = ref(false)
const quote = ref<CustomerQuoteRow | null>(null)

const form = reactive({
  contactName: '',
  contactEmail: '',
  profitFactor: 1,
  items: [] as CustomerQuoteItemRow[]
})

const editable = computed(
  () => authStore.hasPermission('customer-quote.write') && quote.value?.status === 0
)

const displayCode = computed(() => {
  if (!quote.value) return ''
  return quote.value.displayCode || `${quote.value.customerQuoteCode}-${quote.value.versionNo}`
})

const statusText = computed(() => {
  const s = quote.value?.status ?? 0
  if (s === 1) return t('customerQuoteList.statusSent')
  if (s === 2) return t('customerQuoteList.statusVoid')
  return t('customerQuoteList.statusUnsent')
})

const statusTagType = computed(() => {
  const s = quote.value?.status ?? 0
  if (s === 1) return 'success'
  if (s === 2) return 'info'
  return 'warning'
})

function formatPrice(v?: number | null) {
  if (v == null || Number.isNaN(Number(v))) return '—'
  return Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 6 })
}

function currencyLabel(c?: number) {
  return listAmountCurrencyIso(c ?? 1)
}

function fillFormFromQuote(row: CustomerQuoteRow) {
  form.contactName = row.contactName || ''
  form.contactEmail = row.contactEmail || ''
  form.profitFactor = Number(row.profitFactor ?? 1)
  form.items = (row.items || []).map((it) => ({ ...it }))
}

async function loadData() {
  const id = String(route.params.id || '')
  if (!id) {
    ElMessage.error(t('customerQuoteEdit.invalidId'))
    void router.replace({ name: 'CustomerQuoteList' })
    return
  }
  loading.value = true
  try {
    const row = await customerQuoteApi.getQuoteById(id)
    quote.value = row
    fillFormFromQuote(row)
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('customerQuoteEdit.loadFailed')))
    void router.replace({ name: 'CustomerQuoteList' })
  } finally {
    loading.value = false
  }
}

async function handleSave() {
  if (!quote.value || !editable.value) return
  if (form.profitFactor <= 0) {
    ElMessage.warning(t('customerQuoteEdit.profitFactorInvalid'))
    return
  }
  saving.value = true
  try {
    const updated = await customerQuoteApi.updateQuote(quote.value.id, {
      contactName: form.contactName.trim(),
      contactEmail: form.contactEmail.trim(),
      profitFactor: form.profitFactor,
      items: form.items.map((it) => ({
        id: it.id,
        sendPrice: it.sendPrice,
        sendCurrency: it.sendCurrency,
        isLocked: it.isLocked,
        leadTime: it.leadTime ?? '',
        dateCode: it.dateCode ?? '',
        remark: it.remark ?? ''
      }))
    })
    quote.value = updated
    fillFormFromQuote(updated)
    ElMessage.success(t('customerQuoteEdit.saveSuccess'))
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('customerQuoteEdit.saveFailed')))
  } finally {
    saving.value = false
  }
}

async function handleApplyProfitFactor() {
  if (!quote.value || !editable.value) return
  if (form.profitFactor <= 0) {
    ElMessage.warning(t('customerQuoteEdit.profitFactorInvalid'))
    return
  }
  saving.value = true
  try {
    await customerQuoteApi.updateQuote(quote.value.id, { profitFactor: form.profitFactor })
    const updated = await customerQuoteApi.applyProfitFactor(quote.value.id)
    quote.value = updated
    fillFormFromQuote(updated)
    ElMessage.success(t('customerQuoteEdit.applySuccess'))
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('customerQuoteEdit.applyFailed')))
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  void loadData()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.customer-quote-edit-page {
  padding: 24px;
  background: $layer-1;
  min-height: 100%;
  box-sizing: border-box;
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
  gap: 12px;
}

.title-block {
  display: flex;
  align-items: center;
  gap: 10px;
}

.page-title {
  margin: 0;
  font-size: 20px;
}

.header-actions {
  display: flex;
  gap: 8px;
}

.form-section,
.items-section {
  background: #fff;
  border-radius: 8px;
  padding: 16px;
  margin-bottom: 16px;
  border: 1px solid $border-panel;
}

.section-title {
  font-weight: 600;
  margin-bottom: 12px;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 12px 16px;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 4px;

  .label {
    font-size: 12px;
    color: $text-secondary;
  }
}

.items-table .ccy {
  margin-left: 4px;
  font-size: 12px;
  color: $text-secondary;
}
</style>
