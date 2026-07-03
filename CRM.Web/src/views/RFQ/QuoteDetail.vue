<template>
  <div class="quote-detail-page">
    <!-- 详情 CaptionBar（《业务详情页面规范》§3 单据类） -->
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          {{ t('quoteDetail.back') }}
        </button>
        <div v-if="quote" class="quote-caption-title-group">
          <div class="caption-avatar-lg">{{ quoteCaptionAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1
                  class="page-title"
                  :class="{ 'page-title--muted': quote.status === 2 }"
                >
                  <template v-if="quote.quoteCode">
                    {{ t('quoteDetail.captionPrefix') }} {{ quote.quoteCode }}
                  </template>
                  <template v-else>{{ t('quoteDetail.title') }}</template>
                </h1>
              </div>
            </div>
            <div class="title-meta title-meta--caption quote-header-meta-row">
              <el-tag effect="dark" :type="getStatusType(quote.status)" size="small">
                {{ getStatusText(quote.status) }}
              </el-tag>
              <span v-if="quoteItemCount > 0" class="quote-caption-meta-text">
                {{ t('quoteDetail.itemCount', { count: quoteItemCount }) }}
              </span>
            </div>
          </div>
        </div>
      </div>
      <div v-if="quote" class="header-right">
        <button type="button" class="btn-secondary" :disabled="loading" @click="fetchQuote">
          {{ t('quoteDetail.refresh') }}
        </button>
        <button v-if="canEditQuote" type="button" class="btn-primary" @click="handleEdit">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
          </svg>
          {{ t('quoteDetail.edit') }}
        </button>
        <el-dropdown
          trigger="click"
          placement="bottom-end"
          popper-class="quote-detail-header-more-popper"
          @command="onHeaderMoreCommand"
        >
          <button
            type="button"
            class="btn-more-actions"
            :title="t('quoteDetail.more')"
            :aria-label="t('quoteDetail.more')"
          >
            <span class="btn-more-actions__dots" aria-hidden="true">⋯</span>
          </button>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item
                v-if="canDeleteQuote"
                command="delete"
                class="detail-more-item--danger"
              >
                {{ t('quoteList.actions.delete') }}
              </el-dropdown-item>
              <el-dropdown-item v-else disabled>
                {{ t('quoteList.warnings.cannotDeleteWon') }}
              </el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>
    </div>

    <div v-loading="loading" element-loading-background="rgba(10,22,40,0.8)" class="detail-content">
      <template v-if="quote">
        <!-- 基本信息（§4–§5；CaptionBar 已展示报价编号，此处不重复） -->
        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('quoteDetail.basicInfo') }}</span>
            </div>
            <div class="section-header__meta">
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('quoteDetail.createDate') }}</span>
                <span class="section-header-meta-item__value">{{ quoteBasicCreateDateText }}</span>
              </span>
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('quoteDetail.createUser') }}</span>
                <span class="section-header-meta-item__value">{{ quoteBasicCreateUserText }}</span>
              </span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('quoteDetail.fields.rfqCode') }}</span>
              <span class="info-value info-value--code">
                <button
                  v-if="quote.rfqId && quote.rfqCode"
                  type="button"
                  class="info-link-btn"
                  @click="goRfqDetail(quote.rfqId)"
                >
                  {{ quote.rfqCode }}
                </button>
                <template v-else>{{ quote.rfqCode || '—' }}</template>
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('quoteDetail.fields.mpn') }}</span>
              <span class="info-value">{{ quote.mpn || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('quoteDetail.fields.brand') }}</span>
              <span class="info-value">{{ quoteBrandDisplay }}</span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('quoteDetail.fields.quoteDate') }}</span>
              <span class="info-value info-value--time">{{ quoteDateText }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('quoteDetail.fields.customer') }}</span>
              <span class="info-value">{{ maskSaleSensitiveFields ? '—' : (quote.customerName || '—') }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('quoteDetail.fields.salesUser') }}</span>
              <span class="info-value">{{ maskSaleSensitiveFields ? '—' : (quote.salesUserName || '—') }}</span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('quoteDetail.fields.purchaseUser') }}</span>
              <span class="info-value">{{ quote.purchaseUserName || '—' }}</span>
            </div>
            <div class="info-item info-item--basic-spacer" aria-hidden="true"></div>
            <div class="info-item info-item--basic-spacer" aria-hidden="true"></div>
          </div>
          <div class="info-grid info-grid--inline-labels">
            <div class="info-item info-item--span-all">
              <span class="info-label">{{ t('quoteDetail.fields.remark') }}</span>
              <span class="info-value">{{ quote.remark?.trim() || '—' }}</span>
            </div>
          </div>
        </div>

        <!-- TabBar（§6） -->
        <div class="tabs-section">
          <div class="tabs-nav">
            <button
              class="tab-btn"
              :class="{ 'tab-btn--active': activeTab === 'items' }"
              type="button"
              @click="activeTab = 'items'"
            >
              {{ t('quoteDetail.tabs.items') }}
              <span v-if="quoteItemCount" class="tab-count">{{ quoteItemCount }}</span>
            </button>
            <button
              class="tab-btn"
              :class="{ 'tab-btn--active': activeTab === 'documents' }"
              type="button"
              @click="activeTab = 'documents'"
            >
              {{ t('quoteDetail.tabs.documents') }}
            </button>
          </div>
          <div class="tabs-body">
            <div v-show="activeTab === 'items'" class="detail-items-table-wrap">
              <CrmDataTable
                v-if="quoteItemCount > 0"
                :data="quote.items"
                class="items-table detail-panel-list-table"
                size="small"
                stripe
              >
                <el-table-column type="index" width="50" label="#" />
                <el-table-column
                  :label="t('quoteDetail.itemTable.vendor')"
                  prop="vendorName"
                  min-width="140"
                  show-overflow-tooltip
                >
                  <template #default="{ row }">
                    {{ maskPurchaseSensitiveFields ? '—' : (row.vendorName || '—') }}
                  </template>
                </el-table-column>
                <el-table-column
                  :label="t('quoteDetail.itemTable.contact')"
                  prop="contactName"
                  width="100"
                  show-overflow-tooltip
                >
                  <template #default="{ row }">
                    {{ maskPurchaseSensitiveFields ? '—' : (row.contactName || '—') }}
                  </template>
                </el-table-column>
                <el-table-column
                  :label="t('quoteDetail.itemTable.brand')"
                  prop="brand"
                  width="100"
                  show-overflow-tooltip
                />
                <el-table-column
                  :label="t('quoteDetail.itemTable.quantity')"
                  prop="quantity"
                  width="80"
                  align="right"
                />
                <el-table-column
                  :label="t('quoteDetail.itemTable.unitPrice')"
                  width="110"
                  align="right"
                >
                  <template #default="{ row }">
                    {{ formatCurrency(row.unitPrice, row.currency) }}
                  </template>
                </el-table-column>
                <el-table-column
                  :label="t('quoteDetail.itemTable.amount')"
                  width="110"
                  align="right"
                >
                  <template #default="{ row }">
                    {{ formatCurrency(Number(row.quantity) * Number(row.unitPrice), row.currency) }}
                  </template>
                </el-table-column>
                <el-table-column
                  :label="t('quoteDetail.itemTable.leadTime')"
                  prop="leadTime"
                  width="100"
                  show-overflow-tooltip
                />
                <el-table-column
                  :label="t('quoteDetail.itemTable.stock')"
                  prop="stockQty"
                  width="80"
                  align="right"
                />
              </CrmDataTable>
              <p v-else class="quote-items-empty">{{ t('quoteDetail.itemsEmpty') }}</p>
            </div>
            <div v-show="activeTab === 'documents'" class="doc-tab-content">
              <DocumentUploadPanel
                biz-type="QUOTE"
                :biz-id="String(quote.id)"
                :max-files="20"
                :max-size-mb="100"
                @uploaded="docListRef?.refresh()"
              />
              <DocumentListPanel
                ref="docListRef"
                biz-type="QUOTE"
                :biz-id="String(quote.id)"
                view-mode="list"
                style="margin-top: 16px"
              />
            </div>
          </div>
        </div>
      </template>
      <p v-else-if="!loading" class="quote-items-empty">{{ t('quoteDetail.notFound') }}</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { quoteApi } from '@/api/quote'
import CrmDataTable from '@/components/CrmDataTable.vue'
import DocumentUploadPanel from '@/components/Document/DocumentUploadPanel.vue'
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { formatDisplayDate } from '@/utils/displayDateTime'
import {
  isQuoteDeleteForbidden,
  isQuoteReadOnly,
  quoteMainStatusI18nKey,
  quoteMainStatusTagType
} from '@/utils/quoteMainStatus'

type QuoteRecord = {
  id?: string
  quoteCode?: string
  status?: number
  rfqId?: string
  rfqCode?: string
  mpn?: string
  brand?: string
  quoteDate?: string
  customerName?: string
  salesUserName?: string
  purchaseUserName?: string
  remark?: string
  createTime?: string
  createUserName?: string
  createByUserId?: string
  items?: Array<Record<string, unknown>>
}

const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const { t } = useI18n()
const router = useRouter()
const route = useRoute()

const loading = ref(false)
const quote = ref<QuoteRecord | null>(null)
const activeTab = ref<'items' | 'documents'>('items')
const docListRef = ref<InstanceType<typeof DocumentListPanel> | null>(null)

const quoteId = computed(() => String(route.params.id ?? ''))
const quoteItemCount = computed(() => quote.value?.items?.length ?? 0)
const canEditQuote = computed(() => quote.value != null && !isQuoteReadOnly(quote.value.status))
const canDeleteQuote = computed(() => quote.value != null && !isQuoteDeleteForbidden(quote.value.status))

const quoteCaptionAvatarChar = computed(() => {
  const code = String(quote.value?.quoteCode ?? '').trim()
  return code ? code.charAt(0).toUpperCase() : 'Q'
})

const quoteBrandDisplay = computed(() => {
  const q = quote.value
  if (!q) return '—'
  if (q.brand) return q.brand
  const first = q.items?.[0] as { brand?: string } | undefined
  return first?.brand || '—'
})

const quoteBasicCreateDateText = computed(() => {
  const raw = quote.value?.createTime
  if (!raw) return '—'
  return formatDisplayDate(String(raw)) || '—'
})

const quoteBasicCreateUserText = computed(() => {
  const q = quote.value
  if (!q) return '—'
  return q.createUserName || q.createByUserId || '—'
})

const quoteDateText = computed(() => {
  const raw = quote.value?.quoteDate
  if (!raw) return '—'
  const s = String(raw)
  return formatDisplayDate(s.includes('T') ? s : s.slice(0, 10)) || s.slice(0, 10)
})

onMounted(() => {
  void fetchQuote()
})

async function fetchQuote() {
  if (!quoteId.value) {
    quote.value = null
    return
  }
  loading.value = true
  try {
    const res = await quoteApi.getById(quoteId.value)
    quote.value = (res.data as QuoteRecord | null) || null
  } catch {
    quote.value = null
    ElMessage.error(t('quoteList.loadFailed'))
  } finally {
    loading.value = false
  }
}

function goBack() {
  router.push({ name: 'QuoteList' })
}

function goRfqDetail(rfqId: string) {
  router.push({ name: 'RFQDetail', params: { id: rfqId } })
}

function handleEdit() {
  if (!canEditQuote.value || !quoteId.value) return
  router.push({ name: 'QuoteEdit', params: { id: quoteId.value } })
}

async function onHeaderMoreCommand(command: string) {
  if (command !== 'delete' || !quote.value || !canDeleteQuote.value) return
  try {
    await ElMessageBox.confirm(
      t('quoteList.deleteConfirm', { code: quote.value.quoteCode || quoteId.value }),
      t('quoteList.deleteTitle'),
      { type: 'warning', confirmButtonText: t('common.confirm'), cancelButtonText: t('common.cancel') }
    )
  } catch {
    return
  }
  try {
    await quoteApi.delete(quoteId.value)
    ElMessage.success(t('quoteDetail.deleteSuccess'))
    router.push({ name: 'QuoteList' })
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('quoteList.loadFailed'))
  }
}

const getStatusType = (status: unknown) => quoteMainStatusTagType(status)
const getStatusText = (status: unknown) => t(quoteMainStatusI18nKey(status))

function formatCurrency(value: number, currency?: number) {
  if (!value) return '—'
  const symbol = currency === 1 ? '$' : '¥'
  return symbol + value.toLocaleString('zh-CN', { minimumFractionDigits: 4, maximumFractionDigits: 4 })
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import '@/assets/styles/business-detail-info-grid.scss';

.quote-detail-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: 24px;
  flex-wrap: wrap;
  gap: 12px;
}

.header-left {
  display: flex;
  align-items: flex-start;
  gap: 16px;
  min-width: 0;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-shrink: 0;
}

.btn-back {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 7px 12px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  flex-shrink: 0;

  &:hover {
    background: rgba(255, 255, 255, 0.07);
    color: $text-secondary;
    border-color: rgba(0, 212, 255, 0.2);
  }
}

.quote-caption-title-group {
  display: flex;
  align-items: center;
  gap: 14px;
  min-width: 0;
}

.caption-avatar-lg {
  width: 48px;
  height: 48px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.3), rgba(0, 212, 255, 0.2));
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  font-weight: 700;
  color: $cyan-primary;
  flex-shrink: 0;
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

.quote-header-meta-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  min-height: 28px;
}

.quote-caption-meta-text {
  font-size: 13px;
  color: $text-muted;
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
    transform: none;
  }
}

.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-secondary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(255, 255, 255, 0.08);
    border-color: rgba(0, 212, 255, 0.25);
  }

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
}

.btn-more-actions {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 36px;
  height: 36px;
  padding: 0 10px;
  box-sizing: border-box;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-secondary;
  cursor: pointer;
  transition: all 0.2s;
  font-family: 'Noto Sans SC', sans-serif;

  &:hover {
    background: rgba(255, 255, 255, 0.08);
    border-color: rgba(0, 212, 255, 0.25);
    color: $text-primary;
  }

  &__dots {
    font-size: 18px;
    line-height: 1;
    letter-spacing: 0.5px;
    transform: translateY(-1px);
    font-weight: 700;
  }
}

.detail-content {
  min-height: 200px;
}

.info-section {
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
  gap: 16px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: var(--crm-detail-section-header-bg);
}

.section-header__main {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.section-header__meta {
  display: flex;
  align-items: center;
  gap: 20px;
  flex-shrink: 0;
  margin-left: auto;
}

.section-header-meta-item {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  white-space: nowrap;

  &__label {
    color: $text-muted;

    &::after {
      content: '：';
    }
  }

  &__value {
    color: $text-secondary;
  }
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

.section-title {
  font-size: 14px;
  font-weight: 500;
  color: $text-primary;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.04);
  border-right: 1px solid rgba(255, 255, 255, 0.04);

  &:nth-child(3n) {
    border-right: none;
  }
}

.info-grid--inline-labels .info-item {
  flex-direction: row;
  align-items: center;
  gap: 8px;

  .info-label {
    flex-shrink: 0;
    white-space: nowrap;
    text-transform: none;
    letter-spacing: 0;
    font-size: 12px;

    &::after {
      content: '：';
    }
  }

  .info-value {
    flex: 1;
    min-width: 0;
    word-break: break-word;
  }
}

.info-grid--basic {
  .info-item:nth-child(3n) {
    border-right: none;
  }

  .info-item--basic-spacer {
    border-right: none;
  }
}

.info-grid--inline-labels .info-item--span-all {
  grid-column: 1 / -1;
  border-right: none;
}

.info-label {
  font-size: 11px;
  color: $text-muted;
  letter-spacing: 0.5px;
  text-transform: uppercase;
}

.info-value {
  font-size: 13px;
  color: $text-secondary;

  &--code {
    font-family: 'Noto Sans SC', sans-serif;
    font-size: 12px;
    color: $color-ice-blue;
  }

  &--time {
    font-size: 12px;
    color: $text-muted;
  }
}

.info-link-btn {
  padding: 0;
  border: none;
  background: transparent;
  color: $color-ice-blue;
  font: inherit;
  font-size: 12px;
  cursor: pointer;
  text-align: left;

  &:hover {
    color: $cyan-primary;
    text-decoration: underline;
  }
}

.tabs-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}

.tabs-nav {
  display: flex;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  padding: 0 16px;
  background: var(--crm-detail-section-header-bg);
}

.tab-btn {
  padding: 12px 16px;
  background: transparent;
  border: none;
  border-bottom: 2px solid transparent;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  margin-bottom: -1px;
  display: inline-flex;
  align-items: center;
  gap: 6px;

  &:hover {
    color: $text-secondary;
  }

  &--active {
    color: $cyan-primary;
    border-bottom-color: $cyan-primary;
  }
}

.tab-count {
  font-size: 11px;
  padding: 1px 7px;
  border-radius: 999px;
  background: rgba(0, 212, 255, 0.1);
  color: $cyan-primary;
}

.tabs-body {
  padding: 20px;
}

.detail-items-table-wrap {
  margin-top: 4px;
}

.detail-items-table-wrap :deep(.items-table) {
  --el-table-border-color: transparent;
  --el-table-fixed-box-shadow: none;
  background: transparent !important;

  .el-table {
    color: var(--crm-table-text);
  }

  .el-table__inner-wrapper {
    background: transparent;

    &::before {
      display: none !important;
    }

    &::after {
      display: none !important;
    }
  }

  .el-table__border-left-patch {
    display: none !important;
  }

  .el-table__cell {
    .el-button {
      white-space: nowrap !important;
    }

    .cell {
      white-space: nowrap;
    }
  }
}

.quote-items-empty {
  margin: 0;
  padding: 24px 12px;
  text-align: center;
  color: $text-muted;
  font-size: 13px;
}

.doc-tab-content {
  padding-top: 4px;
}
</style>
