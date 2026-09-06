<template>
  <div class="customer-quote-edit-page" v-loading="loading">
    <!-- 详情 CaptionBar（对齐 SalesOrderDetail /《业务详情页面规范》） -->
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="router.back()">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          {{ t('customerQuoteEdit.back') }}
        </button>
        <div v-if="quote" class="cq-caption-title-group">
          <div class="caption-avatar-lg">{{ captionAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1 class="page-title" :class="{ 'page-title--muted': quote.status === 2 }">
                  {{ t('customerQuoteEdit.captionPrefix') }} {{ displayCode }}
                </h1>
                <button
                  type="button"
                  class="btn-favorite-star"
                  disabled
                  :title="t('customerQuoteEdit.favorite')"
                  :aria-label="t('customerQuoteEdit.favorite')"
                >
                  <svg
                    class="star-icon"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="1.75"
                    stroke-linejoin="round"
                    aria-hidden="true"
                  >
                    <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z" />
                  </svg>
                </button>
                <div v-if="showCqHeaderTags" class="cq-header-tags-row tags-row">
                  <button
                    type="button"
                    class="btn-secondary cq-header-add-tag-btn"
                    disabled
                  >
                    <span class="cq-header-add-tag-icon" aria-hidden="true">±</span>
                    {{ t('customerQuoteEdit.tags.add') }}
                  </button>
                </div>
              </div>
            </div>
            <div class="title-meta title-meta--caption cq-header-meta-row">
              <el-tag effect="dark" size="small" :type="statusTagType">
                {{ statusText }}
              </el-tag>
            </div>
          </div>
        </div>
      </div>
      <div v-if="quote" class="header-right">
        <button type="button" class="btn-secondary" :disabled="saving" @click="handleGoPreview">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
            <polyline points="14 2 14 8 20 8" />
            <line x1="12" y1="18" x2="12" y2="12" />
            <line x1="9" y1="15" x2="15" y2="15" />
          </svg>
          {{ t('customerQuoteEdit.exportAndSend') }}
        </button>
        <button v-if="editable" type="button" class="btn-primary" :disabled="saving" @click="handleSave">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
          </svg>
          {{ t('common.save') }}
        </button>
      </div>
    </div>

    <div v-if="quote" class="form-section">
      <div class="section-title">{{ t('customerQuoteEdit.customerSection') }}</div>
      <div class="info-grid info-grid--inline-labels">
        <div class="info-item info-item--customer">
          <span class="info-label">{{ t('customerQuoteEdit.customer') }}</span>
          <span class="info-value">{{ quote.customerName || '—' }}</span>
        </div>
        <div class="info-item">
          <span class="info-label">{{ t('customerQuoteEdit.contactName') }}</span>
          <span class="info-value">
            <el-select
              v-if="editable"
              v-model="form.customerContactId"
              filterable
              clearable
              size="small"
              class="cq-contact-select"
              :placeholder="t('customerQuoteEdit.contactPlaceholder')"
              :disabled="!quote.customerId"
              @change="onContactChange"
            >
              <el-option
                v-for="c in contactOptions"
                :key="c.value"
                :label="c.label"
                :value="c.value"
              />
            </el-select>
            <template v-else>{{ quote.contactName || '—' }}</template>
          </span>
        </div>
        <div class="info-item">
          <span class="info-label">{{ t('customerQuoteEdit.contactEmail') }}</span>
          <span class="info-value">
            <el-input v-if="editable" v-model="form.contactEmail" size="small" />
            <template v-else>{{ quote.contactEmail || '—' }}</template>
          </span>
        </div>
        <div class="info-item info-item--sales">
          <span class="info-label">{{ t('customerQuoteEdit.salesUser') }}</span>
          <span class="info-value">{{ quote.salesUserName || '—' }}</span>
        </div>
      </div>
    </div>

    <div v-if="quote" class="form-section">
      <div class="section-title">{{ t('customerQuoteEdit.remarkSection') }}</div>
      <el-input
        v-model="form.remark"
        type="textarea"
        :rows="1"
        maxlength="2000"
        show-word-limit
        :disabled="!editable"
        :placeholder="t('customerQuoteEdit.remarkPlaceholder')"
      />
    </div>

    <div v-if="quote" class="items-section">
      <div class="section-title-row">
        <div class="section-title">{{ t('customerQuoteEdit.itemsSection') }}</div>
        <div class="items-profit-factor">
          <span class="info-label">{{ t('customerQuoteEdit.profitFactor') }}</span>
          <span class="info-value info-value--profit">
            <template v-if="editable">
              <el-input-number
                v-model="form.profitFactor"
                :min="0.01"
                :max="99.99"
                :step="0.01"
                :precision="2"
                size="small"
              />
              <el-tooltip
                :content="t('customerQuoteEdit.applyProfitFactor')"
                placement="top"
                :hide-after="0"
              >
                <button
                  type="button"
                  class="btn-apply-profit-icon"
                  :disabled="saving"
                  :aria-label="t('customerQuoteEdit.applyProfitFactor')"
                  @click="handleApplyProfitFactor"
                >
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                    <polyline points="23 4 23 10 17 10" />
                    <polyline points="1 20 1 14 7 14" />
                    <path d="M3.51 9a9 9 0 0 1 14.13-3.36L23 10M1 14l5.36 4.36A9 9 0 0 0 20.49 15" />
                  </svg>
                </button>
              </el-tooltip>
            </template>
            <template v-else>{{ Number(quote.profitFactor).toFixed(2) }}</template>
          </span>
        </div>
      </div>
      <div class="detail-items-table-wrap">
        <div class="po-detail-items-table-stack">
          <div class="pagination-wrapper po-detail-items-list-footer">
            <div class="list-footer-left">
              <el-tooltip content="列设置" placement="top" :hide-after="0">
                <el-button
                  class="list-settings-btn"
                  link
                  type="primary"
                  aria-label="列设置"
                  @click="itemsTableRef?.openColumnSettings?.()"
                >
                  <el-icon><Setting /></el-icon>
                </el-button>
              </el-tooltip>
              <span ref="itemsDensityAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
              <div class="list-footer-spacer" aria-hidden="true"></div>
            </div>
          </div>
          <CrmDataTable
            ref="itemsTableRef"
            class="items-table detail-panel-list-table po-detail-items-table"
            column-layout-key="customer-quote-edit-items-v12"
            :columns="itemsColumns"
            :show-column-settings="false"
            :density-toggle-anchor-el="itemsDensityAnchorEl"
            embedded
            :data="form.items"
            row-key="id"
            size="small"
            stripe
          >
            <template #col-quantity="{ row }">
              <span class="po-detail-biz-qty">{{ row.quantity ?? '—' }}</span>
            </template>
            <template #col-customerMpn="{ row }">
              {{ row.customerMpn || '—' }}
            </template>
            <template #col-customerBrand="{ row }">
              {{ row.customerBrand || '—' }}
            </template>
            <template #col-purchasePrice="{ row }">
              <span class="amount-with-code">
                <span>{{ formatUnitPriceNumber(row.purchasePrice) }}</span>
                <span
                  v-if="formatUnitPriceNumber(row.purchasePrice) !== '—'"
                  class="amount-ccy"
                  :class="currencyCodeClass(row.purchaseCurrency)"
                >
                  {{ currencyCodeText(row.purchaseCurrency) }}
                </span>
              </span>
            </template>
            <template #col-sendPrice="{ row }">
              <span class="send-price-cell">
                <template v-if="editable && !row.isLocked">
                  <span class="send-price-edit">
                    <el-input-number
                      v-model="row.sendPrice"
                      :min="0"
                      :precision="6"
                      :step="0.000001"
                      size="small"
                      controls-position="right"
                    />
                    <span class="amount-ccy" :class="currencyCodeClass(row.sendCurrency)">
                      {{ currencyCodeText(row.sendCurrency) }}
                    </span>
                  </span>
                </template>
                <span v-else class="amount-with-code">
                  <span>{{ formatUnitPriceNumber(row.sendPrice) }}</span>
                  <span
                    v-if="formatUnitPriceNumber(row.sendPrice) !== '—'"
                    class="amount-ccy"
                    :class="currencyCodeClass(row.sendCurrency)"
                  >
                    {{ currencyCodeText(row.sendCurrency) }}
                  </span>
                </span>
                <el-button
                  v-if="editable"
                  class="send-price-lock-btn"
                  :class="{ 'send-price-lock-btn--locked': row.isLocked }"
                  link
                  type="primary"
                  size="small"
                  @click="toggleItemLocked(row)"
                >
                  {{ row.isLocked ? t('customerQuoteEdit.unlock') : t('customerQuoteEdit.lock') }}
                </el-button>
              </span>
            </template>
            <template #col-profitRate="{ row }">
              {{ formatLineProfitRate(row) }}
            </template>
            <template #col-leadTime="{ row }">
              <el-input v-if="editable" v-model="row.leadTime" size="small" />
              <span v-else>{{ row.leadTime || '—' }}</span>
            </template>
            <template #col-dateCode="{ row }">
              <el-input v-if="editable" v-model="row.dateCode" size="small" />
              <span v-else>{{ row.dateCode || '—' }}</span>
            </template>
            <template #col-remark="{ row }">
              <el-input v-if="editable" v-model="row.remark" size="small" class="cq-item-remark-input" />
              <span v-else>{{ row.remark || '—' }}</span>
            </template>
          </CrmDataTable>
        </div>
      </div>
    </div>

    <div v-if="quote" class="form-section">
      <div class="section-title">{{ t('customerQuoteEdit.logSection') }}</div>
      <el-table v-if="actionLogs.length" :data="actionLogs" size="small" stripe class="cq-action-log-table">
        <el-table-column :label="t('customerQuoteEdit.logColTime')" min-width="140">
          <template #default="{ row }">
            <template v-for="p in [formatLogTimeParts(row.operationTime)]" :key="`lg-${row.id}`">
              <span v-if="p" class="crm-quote-create-time">
                <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
                <span class="crm-quote-create-time__hm">{{ p.time }}</span>
              </span>
              <span v-else>—</span>
            </template>
          </template>
        </el-table-column>
        <el-table-column :label="t('customerQuoteEdit.logColUser')" min-width="110" show-overflow-tooltip>
          <template #default="{ row }">{{ row.operatorUserName || '—' }}</template>
        </el-table-column>
        <el-table-column prop="actionType" :label="t('customerQuoteEdit.logColAction')" width="120" />
        <el-table-column :label="t('customerQuoteEdit.logColRemark')" min-width="180" show-overflow-tooltip>
          <template #default="{ row }">{{ row.remark || '—' }}</template>
        </el-table-column>
      </el-table>
      <div v-else class="cq-log-empty">{{ t('customerQuoteEdit.logEmpty') }}</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import {
  customerQuoteApi,
  type CustomerQuoteActionLogRow,
  type CustomerQuoteItemRow,
  type CustomerQuoteRow
} from '@/api/customerQuote'
import { customerContactApi } from '@/api/customer'
import { useAuthStore } from '@/stores/auth'
import { formatUnitPriceNumber } from '@/utils/moneyFormat'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatCustomerQuoteDisplayCode } from '@/utils/customerQuoteDisplay'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { estimateListColumnHeaderMinWidth } from '@/utils/listColumnHeaderWidth'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const loading = ref(false)
const saving = ref(false)
const quote = ref<CustomerQuoteRow | null>(null)
const actionLogs = ref<CustomerQuoteActionLogRow[]>([])
const itemsTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)
const itemsDensityAnchorEl = ref<HTMLElement | null>(null)

const form = reactive({
  customerContactId: '',
  contactName: '',
  contactEmail: '',
  profitFactor: 1,
  remark: '',
  items: [] as CustomerQuoteItemRow[]
})

type ContactOption = { value: string; label: string; email: string }
const contactOptions = ref<ContactOption[]>([])

const editable = computed(
  () => authStore.hasPermission('customer-quote.write') && quote.value?.status === 0
)

/** CaptionBar 头像（客户报价单固定「客」） */
const captionAvatarChar = '客'

const showCqHeaderTags = computed(() => editable.value)

const displayCode = computed(() => {
  if (!quote.value) return ''
  return formatCustomerQuoteDisplayCode(quote.value.customerQuoteCode, quote.value.versionNo)
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

function itemTextColMinWidth(
  header: string,
  pick: (row: CustomerQuoteItemRow) => string | null | undefined,
  floor: number
) {
  let width = Math.max(floor, estimateListColumnHeaderMinWidth(header))
  for (const row of form.items) {
    const text = String(pick(row) ?? '').trim()
    if (!text) continue
    width = Math.max(width, estimateListColumnHeaderMinWidth(text))
  }
  return width
}

const itemsColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'index', type: 'index', width: 48, hideable: false, reorderable: false, pinned: 'start' },
  {
    key: 'mpn',
    label: t('customerQuoteEdit.colMpn'),
    prop: 'mpn',
    minWidth: itemTextColMinWidth(t('customerQuoteEdit.colMpn'), (r) => r.mpn, 120),
    className: 'cq-item-full-text',
    showOverflowTooltip: false
  },
  {
    key: 'brand',
    label: t('customerQuoteEdit.colBrand'),
    prop: 'brand',
    minWidth: itemTextColMinWidth(t('customerQuoteEdit.colBrand'), (r) => r.brand, 90),
    className: 'cq-item-full-text',
    showOverflowTooltip: false
  },
  {
    key: 'customerMpn',
    label: t('customerQuoteEdit.colCustomerMpn'),
    prop: 'customerMpn',
    minWidth: itemTextColMinWidth(t('customerQuoteEdit.colCustomerMpn'), (r) => r.customerMpn, 140),
    className: 'cq-item-full-text',
    showOverflowTooltip: false
  },
  {
    key: 'customerBrand',
    label: t('customerQuoteEdit.colCustomerBrand'),
    prop: 'customerBrand',
    minWidth: itemTextColMinWidth(t('customerQuoteEdit.colCustomerBrand'), (r) => r.customerBrand, 120),
    className: 'cq-item-full-text',
    showOverflowTooltip: false
  },
  {
    key: 'quantity',
    label: t('customerQuoteEdit.colQty'),
    prop: 'quantity',
    width: 90,
    align: 'right'
  },
  {
    key: 'purchasePrice',
    label: t('customerQuoteEdit.colPurchasePrice'),
    minWidth: 120,
    align: 'right'
  },
  {
    key: 'sendPrice',
    label: t('customerQuoteEdit.colSendPrice'),
    width: 220
  },
  {
    key: 'profitRate',
    label: t('customerQuoteEdit.colProfitRate'),
    width: 88,
    align: 'right'
  },
  {
    key: 'leadTime',
    label: t('customerQuoteEdit.colLeadTime'),
    minWidth: 100
  },
  {
    key: 'dateCode',
    label: t('customerQuoteEdit.colDateCode'),
    minWidth: 100
  },
  {
    key: 'remark',
    label: t('customerQuoteEdit.colRemark'),
    minWidth: 160
  },
  {
    key: 'sourceQuoteCode',
    label: t('customerQuoteEdit.colSourceQuote'),
    prop: 'sourceQuoteCode',
    width: 110,
    showOverflowTooltip: true
  },
  {
    key: 'purchaseUserName',
    label: t('customerQuoteEdit.colPurchaser'),
    prop: 'purchaseUserName',
    width: 100,
    showOverflowTooltip: true
  }
])

const currencyCodeText = (currency?: number) => {
  const c = Number(currency)
  return CURRENCY_CODE_TO_TEXT[c as keyof typeof CURRENCY_CODE_TO_TEXT] ?? 'RMB'
}

const currencyCodeClass = (currency?: number) => {
  const c = Number(currency)
  if (c === 1 || !Number.isFinite(c)) return 'amount-ccy--rmb'
  return 'amount-ccy--fx'
}

/** 行利润率 = 发送价 / 采购价，保留 2 位小数；采购价为 0 或无效时显示 — */
function formatLineProfitRate(row: CustomerQuoteItemRow) {
  const purchase = Number(row.purchasePrice)
  const send = Number(row.sendPrice)
  if (!Number.isFinite(purchase) || purchase === 0) return '—'
  if (!Number.isFinite(send)) return '—'
  return (send / purchase).toFixed(2)
}

function toggleItemLocked(row: CustomerQuoteItemRow) {
  row.isLocked = !row.isLocked
}

function itemSavePatches() {
  return form.items.map((it) => ({
    id: it.id,
    sendPrice: it.sendPrice,
    sendCurrency: it.sendCurrency,
    isLocked: it.isLocked,
    leadTime: it.leadTime ?? '',
    dateCode: it.dateCode ?? '',
    remark: it.remark ?? ''
  }))
}

function fillFormFromQuote(row: CustomerQuoteRow) {
  form.customerContactId = row.customerContactId || ''
  form.contactName = row.contactName || ''
  form.contactEmail = row.contactEmail || ''
  form.profitFactor = Number(row.profitFactor ?? 1)
  form.remark = row.remark || ''
  form.items = (row.items || []).map((it) => ({ ...it }))
  savedSnapshot.value = formSnapshot()
}

function formSnapshot() {
  return JSON.stringify({
    customerContactId: form.customerContactId,
    contactName: form.contactName,
    contactEmail: form.contactEmail,
    profitFactor: form.profitFactor,
    remark: form.remark,
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
}

const savedSnapshot = ref('')

function isDirty() {
  if (!editable.value) return false
  return formSnapshot() !== savedSnapshot.value
}

function contactLabelFromRaw(c: Record<string, unknown>): string {
  return String(c.contactName ?? c.cName ?? c.eName ?? c.name ?? '').trim() || t('customerQuoteEdit.contactName')
}

function contactEmailFromRaw(c: Record<string, unknown>): string {
  const v = c.email ?? c.Email
  return typeof v === 'string' ? v.trim() : ''
}

function ensureCurrentContactOption() {
  const id = form.customerContactId.trim()
  if (!id) return
  if (contactOptions.value.some((o) => o.value === id)) return
  contactOptions.value = [
    {
      value: id,
      label: form.contactName.trim() || t('customerQuoteEdit.contactName'),
      email: form.contactEmail.trim()
    },
    ...contactOptions.value
  ]
}

async function loadContacts(customerId?: string | null) {
  const cid = (customerId || '').trim()
  if (!cid) {
    contactOptions.value = []
    return
  }
  try {
    const list = await customerContactApi.getContactsByCustomerId(cid)
    const rows = Array.isArray(list) ? list : []
    contactOptions.value = rows
      .map((c) => {
        const raw = c as unknown as Record<string, unknown>
        const id = String(c.id ?? raw.contactId ?? '').trim()
        if (!id) return null
        return {
          value: id,
          label: contactLabelFromRaw(raw),
          email: contactEmailFromRaw(raw)
        }
      })
      .filter((x): x is ContactOption => x != null)
  } catch {
    contactOptions.value = []
  }
  ensureCurrentContactOption()
}

function onContactChange(contactId: string | null | undefined) {
  const id = (contactId || '').trim()
  form.customerContactId = id
  if (!id) {
    form.contactName = ''
    form.contactEmail = ''
    return
  }
  const row = contactOptions.value.find((c) => c.value === id)
  form.contactName = row?.label || ''
  form.contactEmail = row?.email || ''
}

function formatLogTimeParts(v?: string | null) {
  if (!v) return null
  return formatDisplayDateTime2DigitYearParts(v)
}

async function loadActionLogs(id: string) {
  try {
    const rows = await customerQuoteApi.getActionLogs(id)
    actionLogs.value = Array.isArray(rows) ? rows : []
  } catch {
    actionLogs.value = []
  }
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
    await Promise.all([loadContacts(row.customerId), loadActionLogs(row.id)])
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('customerQuoteEdit.loadFailed')))
    void router.replace({ name: 'CustomerQuoteList' })
  } finally {
    loading.value = false
  }
}

async function handleSave(): Promise<boolean> {
  if (!quote.value || !editable.value) return false
  if (form.profitFactor <= 0) {
    ElMessage.warning(t('customerQuoteEdit.profitFactorInvalid'))
    return false
  }
  saving.value = true
  try {
    const updated = await customerQuoteApi.updateQuote(quote.value.id, {
      customerContactId: form.customerContactId.trim() || null,
      contactName: form.contactName.trim(),
      contactEmail: form.contactEmail.trim(),
      profitFactor: form.profitFactor,
      remark: form.remark,
      items: itemSavePatches()
    })
    quote.value = updated
    fillFormFromQuote(updated)
    ElMessage.success(t('customerQuoteEdit.saveSuccess'))
    return true
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('customerQuoteEdit.saveFailed')))
    return false
  } finally {
    saving.value = false
  }
}

async function handleGoPreview() {
  if (!quote.value) return
  if (isDirty()) {
    try {
      await ElMessageBox.confirm(
        t('customerQuoteEdit.unsavedPreviewHint'),
        t('customerQuoteEdit.exportAndSend'),
        {
          distinguishCancelAndClose: true,
          confirmButtonText: t('customerQuoteEdit.saveAndPreview'),
          cancelButtonText: t('customerQuoteEdit.previewWithoutSave'),
          type: 'warning'
        }
      )
      const ok = await handleSave()
      if (!ok) return
    } catch (action) {
      if (action !== 'cancel') return
    }
  }
  void router.push({ name: 'CustomerQuotePreview', params: { id: quote.value.id } })
}

async function handleApplyProfitFactor() {
  if (!quote.value || !editable.value) return
  if (form.profitFactor <= 0) {
    ElMessage.warning(t('customerQuoteEdit.profitFactorInvalid'))
    return
  }
  saving.value = true
  try {
    await customerQuoteApi.updateQuote(quote.value.id, {
      customerContactId: form.customerContactId.trim() || null,
      contactName: form.contactName.trim(),
      contactEmail: form.contactEmail.trim(),
      profitFactor: form.profitFactor,
      remark: form.remark,
      items: itemSavePatches()
    })
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
  margin-bottom: 24px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 10px;
}

.btn-back {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 7px 12px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(255, 255, 255, 0.07);
    color: $text-secondary;
    border-color: rgba(0, 212, 255, 0.2);
  }
}

.cq-caption-title-group {
  display: flex;
  align-items: center;
  gap: 14px;
  min-width: 0;
}

.caption-avatar-lg {
  width: 48px;
  height: 48px;
  flex-shrink: 0;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  font-weight: 700;
  color: $cyan-primary;
  border: 1px solid rgba(0, 212, 255, 0.25);
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.3), rgba(0, 212, 255, 0.2));
}

.page-title-row {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 6px;
}

.page-title-with-icons {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  min-width: 0;
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;

  &--muted {
    color: rgba(150, 170, 195, 0.82);
  }
}

.title-meta--caption {
  margin-top: 4px;
}

.cq-header-meta-row {
  min-height: 28px;
}

.cq-header-tags-row {
  flex-shrink: 0;
}

.cq-header-add-tag-btn {
  padding: 6px 12px;
  font-size: 12px;
}

.cq-header-add-tag-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 13px;
  font-size: 15px;
  font-weight: 500;
  line-height: 1;
}

.btn-favorite-star {
  flex-shrink: 0;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 4px;
  border: none;
  border-radius: 8px;
  background: transparent;
  color: #ffc94d;
  cursor: pointer;
  transition: color 0.15s, background 0.15s, transform 0.12s;

  .star-icon {
    width: 22px;
    height: 22px;
    display: block;
  }

  &:not(.is-favorite) .star-icon {
    stroke-dasharray: 3 2.5;
  }

  &:hover:not(:disabled) {
    background: rgba(255, 201, 77, 0.12);
    transform: scale(1.05);
  }

  &:disabled {
    opacity: 0.45;
    cursor: not-allowed;
  }
}

.title-meta {
  display: flex;
  align-items: center;
  gap: 8px;
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  border: 1px solid rgba(0, 212, 255, 0.4);
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  cursor: pointer;

  &:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }
}

.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  border: 1px solid $border-panel;
  color: $text-secondary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  background: rgba(255, 255, 255, 0.05);
  cursor: pointer;
  transition: all 0.2s;

  &:hover:not(:disabled) {
    background: rgba(255, 255, 255, 0.08);
    border-color: rgba(0, 212, 255, 0.25);
  }

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }
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

.section-title-row {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-bottom: 12px;
  flex-wrap: wrap;

  .section-title {
    margin-bottom: 0;
  }
}

.items-profit-factor {
  display: inline-flex;
  align-items: center;
  gap: 8px;

  .info-label {
    flex-shrink: 0;
    white-space: nowrap;
    font-size: 12px;
    color: $text-secondary;
    font-weight: 400;

    &::after {
      content: '：';
    }
  }

  .info-value--profit {
    display: inline-flex;
    align-items: center;
    gap: 5px;

    :deep(.el-tooltip__trigger) {
      display: inline-flex;
      align-items: center;
      line-height: 1;
    }
  }
}

.info-grid {
  display: grid;
  grid-template-columns: minmax(440px, max-content) 220px 220px 220px;
  gap: 12px 16px;
  align-items: center;
  justify-content: start;
}

.info-grid--inline-labels {
  .info-item {
    display: flex;
    flex-direction: row;
    align-items: center;
    gap: 8px;
  }

  .info-label {
    flex-shrink: 0;
    white-space: nowrap;
    font-size: 12px;
    font-weight: 400;
    color: $text-secondary;

    &::after {
      content: '：';
    }
  }

  .info-value {
    flex: 1;
    min-width: 0;
    word-break: break-word;
  }

  .cq-contact-select {
    width: 100%;
  }

  .info-item--customer {
    min-width: max-content;

    .info-value {
      flex: 0 0 auto;
      min-width: max-content;
      word-break: keep-all;
      white-space: nowrap;
      overflow: visible;
    }
  }

  .info-item--sales {
    margin-left: 50px;
  }

  .info-item--customer .info-value,
  .info-item--sales .info-value {
    font-size: 12px;
    font-weight: 400;
    font-family: inherit;
    color: $text-secondary;
  }
}

.btn-apply-profit-icon {
  flex-shrink: 0;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  padding: 0;
  margin: 0;
  border-radius: $border-radius-md;
  border: 1px solid $border-panel;
  color: $text-secondary;
  background: rgba(255, 255, 255, 0.05);
  cursor: pointer;
  transition: all 0.2s;

  &:hover:not(:disabled) {
    background: rgba(0, 212, 255, 0.1);
    border-color: rgba(0, 212, 255, 0.35);
    color: $cyan-primary;
  }

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }
}

.detail-items-table-wrap {
  margin-top: 4px;
}

.detail-items-table-wrap :deep(.items-table),
.detail-items-table-wrap :deep(.crm-items-table.detail-panel-list-table) {
  --el-table-border-color: transparent;
  --el-table-fixed-box-shadow: none;
  background: transparent !important;
  border-radius: 0;
  border: none;
  min-height: 0;
  overflow: visible;

  :deep(.el-table) {
    color: var(--crm-table-text);
  }

  :deep(.el-table__inner-wrapper) {
    background: transparent;

    &::before {
      display: none !important;
    }

    &::after {
      display: none !important;
    }
  }

  :deep(.el-table__border-left-patch) {
    display: none !important;
  }

  :deep(.el-table__cell) {
    .cell {
      white-space: nowrap;
    }
  }

  :deep(td.cq-item-full-text .cell),
  :deep(td.cq-item-full-text .crm-list-copyable-text-cell__value) {
    overflow: visible;
    text-overflow: clip;
    white-space: nowrap;
  }
}

.po-detail-items-table-stack {
  display: flex;
  flex-direction: column-reverse;
  gap: 12px;
}

.po-detail-items-list-footer.pagination-wrapper {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  flex-wrap: wrap;
}

.po-detail-items-list-footer .list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 6px;
}

.po-detail-items-list-footer .list-footer-density-anchor {
  display: inline-flex;
  align-items: center;
  min-width: 0;
  min-height: 0;
}

.po-detail-items-list-footer .list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
}

.po-detail-items-list-footer .list-settings-btn {
  padding: 4px 6px !important;
  min-width: 28px;
}

.po-detail-biz-qty {
  font-weight: 700;
  color: #27292c;
  font-variant-numeric: tabular-nums;
}

html[data-theme='dark'] .customer-quote-edit-page .po-detail-biz-qty {
  color: $text-primary;
}

.amount-with-code {
  display: inline-flex;
  align-items: baseline;
  gap: 6px;
}

.amount-ccy {
  font-size: 0.92em;
  font-weight: 500;
}

.amount-ccy--rmb {
  color: #ff4f96;
}

.amount-ccy--fx {
  color: #19c37d;
}

.send-price-cell {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.send-price-edit {
  display: inline-flex;
  align-items: center;
  gap: 6px;

  /* 刚好放下 999.123456（3 位整数 + 小数点 + 6 位小数）+ 右侧微调按钮 */
  :deep(.el-input-number) {
    width: 118px;
  }

  :deep(.el-input-number.is-controls-right .el-input__wrapper) {
    padding-left: 8px;
    padding-right: 28px;
  }

  :deep(.el-input__inner) {
    font-variant-numeric: tabular-nums;
  }
}

.cq-item-remark-input {
  width: 100%;
}

.send-price-lock-btn {
  flex-shrink: 0;
  padding: 0 2px !important;

  &--locked,
  &--locked:hover,
  &--locked:focus {
    color: #e6a23c !important;
  }
}

.cq-log-empty {
  color: $text-muted;
  font-size: 13px;
  padding: 8px 0;
}

.cq-action-log-table {
  width: 100%;
}
</style>
