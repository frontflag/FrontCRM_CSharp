<template>
  <aside
    class="so-item-ops-root so-item-ops-root--embedded"
    aria-label="customer-workspace-panel"
  >
    <div v-if="!boundId" class="so-item-ops-root__empty">
      {{ t('customerWorkspace.pickRow') }}
    </div>
    <div
      v-else
      v-loading="loading"
      class="so-item-ops-root__content so-item-ops-root__content--embedded"
    >
      <p v-if="loadError" class="so-item-ops-root__error">{{ loadError }}</p>
      <p v-else-if="!loading && summary && !summary.hasCustomer" class="so-item-ops-root__empty">
        {{ t('customerWorkspace.noCustomer') }}
      </p>
      <template v-else-if="summary?.hasCustomer">
        <section class="ops-card">
          <header class="ops-card__head">
            <h3 class="ops-card__title">{{ t('customerWorkspace.overviewTitle') }}</h3>
          </header>
          <div class="ops-card__body ops-card__body--overview">
            <div
              class="ops-overview-line ops-overview-line--hero"
              :aria-label="t('customerWorkspace.customerCode')"
            >
              <router-link
                v-if="detailLink"
                :to="detailLink"
                class="link-text"
              >{{ dash(summary.customerCode) }}</router-link>
              <template v-else>{{ dash(summary.customerCode) }}</template>
            </div>
            <template v-if="summary.canViewFull">
              <div
                class="ops-overview-line"
                :aria-label="t('customerWorkspace.chineseName')"
              >
                <router-link
                  v-if="detailLink"
                  :to="detailLink"
                  class="link-text"
                >{{ dash(summary.chineseName) }}</router-link>
                <span v-else>{{ dash(summary.chineseName) }}</span>
              </div>
              <div
                class="ops-overview-line"
                :aria-label="t('customerWorkspace.englishName')"
              >
                <router-link
                  v-if="detailLink"
                  :to="detailLink"
                  class="link-text"
                >{{ dash(summary.englishName) }}</router-link>
                <span v-else>{{ dash(summary.englishName) }}</span>
              </div>
            </template>
            <div
              class="ops-overview-line"
              :aria-label="t('customerWorkspace.salesUser')"
            >{{ dash(summary.salesUserName) }}</div>
          </div>
        </section>

        <section v-if="summary.canViewFull" class="ops-card">
          <header class="ops-card__head">
            <h3 class="ops-card__title">{{ t('customerWorkspace.basicTitle') }}</h3>
          </header>
          <div class="ops-card__body">
            <div class="ops-kv-stack">
              <div class="ops-kv-line">
                <span class="ops-kv__label">{{ t('customerWorkspace.customerType') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span>{{ typeText }}</span>
              </div>
              <div class="ops-kv-line">
                <span class="ops-kv__label">{{ t('customerWorkspace.customerLevel') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span>{{ levelText }}</span>
              </div>
              <div class="ops-kv-line">
                <span class="ops-kv__label">{{ t('customerWorkspace.industry') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span>{{ industryText }}</span>
              </div>
              <div class="ops-kv-line">
                <span class="ops-kv__label">{{ t('customerWorkspace.region') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span>{{ dash(summary.region) }}</span>
              </div>
            </div>
          </div>
        </section>

        <section v-if="summary.canViewFull" class="ops-card">
          <header class="ops-card__head">
            <h3 class="ops-card__title">{{ t('customerWorkspace.financeTitle') }}</h3>
          </header>
          <div class="ops-card__body">
            <div class="ops-kv-stack">
              <div class="ops-kv-line">
                <span class="ops-kv__label">{{ t('customerWorkspace.creditLimit') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span>{{ creditText }}</span>
              </div>
              <div class="ops-kv-line">
                <span class="ops-kv__label">{{ t('customerWorkspace.paymentTerms') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span>{{ paymentText }}</span>
              </div>
              <div class="ops-kv-line">
                <span class="ops-kv__label">{{ t('customerWorkspace.settlementCurrency') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span>{{ currencyText }}</span>
              </div>
              <div class="ops-kv-line">
                <span class="ops-kv__label">{{ t('customerWorkspace.taxRate') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span>{{ taxText }}</span>
              </div>
              <div class="ops-kv-line">
                <span class="ops-kv__label">{{ t('customerWorkspace.invoiceType') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span>{{ invoiceText }}</span>
              </div>
            </div>
          </div>
        </section>
      </template>
    </div>
  </aside>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import { useI18n } from 'vue-i18n'
import { useCustomerWorkspacePanelStore } from '@/stores/customerWorkspacePanel'
import { useCustomerDictStore } from '@/stores/customerDict'
import { formatTotalAmountNumber } from '@/utils/moneyFormat'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'

const { t } = useI18n()
const store = useCustomerWorkspacePanelStore()
const { boundId, summary, loading, loadError } = storeToRefs(store)
const customerDict = useCustomerDictStore()

onMounted(() => {
  void customerDict.ensureLoaded()
})

const detailLink = computed(() => {
  const s = summary.value
  if (!s?.canViewFull) return ''
  const id = String(s.customerId ?? '').trim()
  return id ? `/customers/${id}` : ''
})

function dash(v: string | null | undefined) {
  const s = (v ?? '').trim()
  return s || '—'
}

const typeText = computed(() => {
  const n = summary.value?.customerType
  if (n == null || n === 0) return '—'
  const label = customerDict.typeLabel(n)
  return !label || label === '--' ? '—' : label
})

const levelText = computed(() => {
  const label = customerDict.levelLabel(summary.value?.customerLevel)
  return !label || label === '--' ? '—' : label
})

const industryText = computed(() => {
  const label = customerDict.industryLabel(summary.value?.industry)
  return !label || label === '--' ? '—' : label
})

const creditText = computed(() => {
  const n = summary.value?.creditLimit
  if (n == null || !Number.isFinite(Number(n))) return '—'
  return formatTotalAmountNumber(n)
})

const paymentText = computed(() => {
  const n = summary.value?.paymentTerms
  if (n == null || !Number.isFinite(Number(n))) return '—'
  return String(n)
})

const currencyText = computed(() => {
  const n = Number(summary.value?.settlementCurrency)
  if (!Number.isFinite(n) || n <= 0) return '—'
  return CURRENCY_CODE_TO_TEXT[n] ?? String(n)
})

const taxText = computed(() => {
  const n = summary.value?.taxRate
  if (n == null || !Number.isFinite(Number(n))) return '—'
  const label = customerDict.taxRateLabel(Number(n))
  return !label || label === '--' ? '—' : label
})

const invoiceText = computed(() => {
  const n = summary.value?.invoiceType
  if (n == null) return '—'
  const label = customerDict.invoiceTypeLabel(n)
  return !label || label === '--' ? '—' : label
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/so-item-ops-panel.scss';

.ops-kv-stack {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.ops-kv-line {
  display: flex;
  align-items: baseline;
  min-width: 0;
  font-size: 13px;
  line-height: 1.45;
}

.ops-kv-line .ops-kv__label {
  flex: 0 0 auto;
  color: var(--crm-text-secondary, #64748b);
}

.ops-kv-line .ops-kv__value,
.ops-kv-line .link-text,
.ops-kv-line > span:last-child {
  min-width: 0;
  overflow-wrap: anywhere;
}
</style>
