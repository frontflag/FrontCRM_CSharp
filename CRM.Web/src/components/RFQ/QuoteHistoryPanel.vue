<template>
  <div class="quote-history-panel" v-loading="loading">
    <section class="qh-block">
      <h4 class="qh-block__title">{{ t('quoteDesktop.history.materialTitle') }}</h4>
      <div class="qh-kv">
        <span class="qh-kv__label">{{ t('quoteDesktop.history.mpn') }}</span>
        <span class="qh-kv__value qh-kv__value--mpn">{{ mpn || '—' }}</span>
      </div>
      <div class="qh-kv">
        <span class="qh-kv__label">{{ t('quoteDesktop.history.brand') }}</span>
        <span class="qh-kv__value">{{ brand || '—' }}</span>
      </div>
    </section>

    <section class="qh-block">
      <h4 class="qh-block__title">{{ t('quoteDesktop.history.listTitle') }}</h4>
      <div v-if="!mpn" class="qh-empty">{{ t('quoteDesktop.empty.history') }}</div>
      <div v-else-if="!rows.length && !loading" class="qh-empty">{{ t('quoteDesktop.empty.history') }}</div>
      <template v-else>
        <article v-for="row in rows" :key="row.id" class="qh-card">
          <div class="qh-cols qh-card__head">
            <span class="qh-card__code">
              <span :title="row.quoteCode">{{ row.quoteCode || '—' }}</span>
              <el-tag v-if="row.status === 1" size="small" type="success" effect="dark">
                {{ t('quoteList.status.won') }}
              </el-tag>
            </span>
            <span class="qh-card__date">{{ formatDate(row.createTime) }}</span>
            <span class="qh-card__quoter" :title="row.quoterName">{{ row.quoterName || '—' }}</span>
          </div>
          <ul class="qh-tiers">
            <li v-for="(tier, idx) in row.tiers" :key="`${row.id}-${idx}`" class="qh-cols qh-tier">
              <span class="qh-tier__price">
                {{ t('quoteDesktop.history.priceLabel') }}{{ formatPrice(tier.unitPrice) }}
                <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(tier.currency)]">
                  {{ listAmountCurrencyIso(tier.currency) }}
                </span>
              </span>
              <span class="qh-tier__qty">
                {{ t('quoteDesktop.history.qtyLabel') }}{{ formatQty(tier.quantity) }}
              </span>
              <span class="qh-tier__pad" aria-hidden="true" />
            </li>
          </ul>
        </article>
      </template>
    </section>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { quoteApi } from '@/api/quote'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { listAmountCurrencyDockClass, listAmountCurrencyIso } from '@/utils/moneyFormat'

const props = defineProps<{
  mpn?: string | null
  brand?: string | null
}>()

const { t } = useI18n()
const loading = ref(false)

type Tier = { quantity: number; unitPrice: number; currency: number | null }
type HistoryRow = {
  id: string
  status: number
  quoteCode: string
  createTime: string
  quoterName: string
  tiers: Tier[]
}

const rows = ref<HistoryRow[]>([])

function formatDate(v?: string) {
  if (!v) return '—'
  return formatDisplayDateTime(v) || '—'
}

/** 默认 4 位小数；实际有效小数位超过 4 时按真实位数显示（最多 10 位） */
function formatPrice(n: number) {
  if (!Number.isFinite(n)) return '—'
  const trimmed = n.toFixed(10).replace(/\.?0+$/, '')
  const dot = trimmed.indexOf('.')
  const actualDecimals = dot < 0 ? 0 : trimmed.length - dot - 1
  const digits = Math.min(10, Math.max(4, actualDecimals))
  return n.toLocaleString('zh-CN', {
    minimumFractionDigits: 4,
    maximumFractionDigits: digits
  })
}

function formatQty(n: number) {
  if (!Number.isFinite(n)) return '—'
  if (Math.abs(n - Math.round(n)) < 1e-9) return String(Math.round(n))
  return n.toLocaleString('zh-CN', { maximumFractionDigits: 4 })
}

function resolveCurrencyCode(raw: unknown): number | null {
  const n = Number(raw)
  return Number.isFinite(n) && n > 0 ? n : null
}

function mapTiers(quote: Record<string, unknown>): Tier[] {
  const items = (quote.items ?? quote.Items ?? []) as Record<string, unknown>[]
  if (!Array.isArray(items) || !items.length) {
    const unitPrice = Number(quote.unitPrice ?? quote.UnitPrice ?? NaN)
    const quantity = Number(quote.quantity ?? quote.Quantity ?? NaN)
    const ccy = resolveCurrencyCode(quote.currency ?? quote.Currency)
    if (Number.isFinite(unitPrice)) {
      return [
        {
          quantity: Number.isFinite(quantity) ? quantity : 0,
          unitPrice,
          currency: ccy
        }
      ]
    }
    return []
  }
  return items.map((it) => ({
    quantity: Number(it.quantity ?? it.Quantity ?? 0),
    unitPrice: Number(it.unitPrice ?? it.UnitPrice ?? 0),
    currency: resolveCurrencyCode(it.currency ?? it.Currency ?? quote.currency)
  }))
}

async function loadHistory(mpn: string) {
  loading.value = true
  try {
    const res = await quoteApi.getList({ exactMpn: mpn, page: 1, pageSize: 10 })
    rows.value = (res.data || []).map((raw) => {
      const q = raw as Record<string, unknown>
      const quoterName = String(
        q.purchaseUserName ??
          q.PurchaseUserName ??
          q.createUserName ??
          q.CreateUserName ??
          q.salesUserName ??
          q.SalesUserName ??
          ''
      ).trim()
      return {
        id: String(q.id ?? q.Id ?? Math.random()),
        status: Number(q.status ?? q.Status ?? 0),
        quoteCode: String(q.quoteCode ?? q.QuoteCode ?? q.quoteNumber ?? '').trim(),
        createTime: String(q.createTime ?? q.CreateTime ?? q.quoteDate ?? q.QuoteDate ?? ''),
        quoterName,
        tiers: mapTiers(q)
      }
    })
  } catch {
    rows.value = []
  } finally {
    loading.value = false
  }
}

watch(
  () => String(props.mpn || '').trim(),
  (mpn) => {
    if (!mpn) {
      rows.value = []
      return
    }
    void loadHistory(mpn)
  },
  { immediate: true }
)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.quote-history-panel {
  padding: 12px 14px 20px;
  height: 100%;
  overflow: auto;
  box-sizing: border-box;
}

.qh-block {
  margin-bottom: 16px;

  &__title {
    margin: 0 0 8px;
    font-size: 13px;
    font-weight: 600;
  }
}

.qh-kv {
  display: flex;
  gap: 10px;
  font-size: 12px;
  line-height: 1.6;

  &__label {
    flex-shrink: 0;
    opacity: 0.65;
    min-width: 4em;
  }

  &__value {
    word-break: break-all;

    &--mpn {
      color: $color-amber;
      font-weight: 600;
    }
  }
}

.qh-empty {
  font-size: 12px;
  opacity: 0.65;
  padding: 8px 0;
}

.qh-cols {
  display: grid;
  grid-template-columns: minmax(0, 1.2fr) minmax(0, 1.1fr) minmax(0, 1fr);
  gap: 8px;
  align-items: center;
  font-size: 12px;
}

.qh-card {
  border: 1px solid rgba(0, 0, 0, 0.08);
  border-radius: 8px;
  padding: 8px 10px;
  margin-bottom: 8px;

  &__head {
    margin-bottom: 6px;
  }

  &__code {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    min-width: 0;
    font-weight: 600;

    > span:first-child {
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
  }

  &__date,
  &__quoter {
    opacity: 0.8;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
}

.qh-tiers {
  list-style: none;
  margin: 0;
  padding: 0;
}

.qh-tier {
  line-height: 1.55;

  &__price {
    font-weight: 500;

    .dock-tier-ccy {
      margin-left: 4px;
    }
  }

  &__qty {
    min-width: 0;
  }
}
</style>
