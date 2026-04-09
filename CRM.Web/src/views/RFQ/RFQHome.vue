<template>
  <div class="rfq-home">
    <div class="rfq-home__search-hero">
      <div class="rfq-home__search-row">
        <div class="rfq-home__pill-slot">
          <div class="rfq-home__pill-wrap">
            <div class="rfq-home__pill" role="search">
              <div class="rfq-home__pill-field">
                <svg
                  class="rfq-home__pill-mag"
                  width="18"
                  height="18"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2"
                  aria-hidden="true"
                >
                  <circle cx="11" cy="11" r="8" />
                  <line x1="21" y1="21" x2="16.65" y2="16.65" />
                </svg>
                <input
                  v-model="keyword"
                  type="search"
                  class="rfq-home__pill-input"
                  :placeholder="t('rfqHome.searchPlaceholder')"
                  autocomplete="off"
                  @keyup.enter="handleMaterialSearch"
                />
              </div>
              <button type="button" class="rfq-home__pill-btn" @click="handleMaterialSearch">{{ t('rfqHome.search') }}</button>
              <button type="button" class="rfq-home__pill-link" @click="goPnPlain">{{ t('rfqHome.goMaterialList') }}</button>
              <button type="button" class="rfq-home__pill-link" @click="goRfqList">{{ t('rfqHome.goRfqList') }}</button>
            </div>
          </div>
        </div>
        <button
          v-if="showCreateRfqButton"
          type="button"
          class="rfq-home__btn-create"
          @click="goCreateRfq"
        >
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          {{ t('rfqHome.create') }}
        </button>
      </div>
    </div>

    <div v-if="loadingStats" class="rfq-home__loading">{{ t('rfqHome.loading') }}</div>

    <template v-else-if="stats">
      <div class="rfq-home__section-title">{{ t('rfqHome.sections.rfq') }}</div>
      <div class="rfq-home__row rfq-home__row--3">
        <div class="rfq-home__card">
          <div class="rfq-home__icon rfq-home__icon--blue" aria-hidden="true">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
              <polyline points="14 2 14 8 20 8" />
            </svg>
          </div>
          <div class="rfq-home__card-main">
            <div class="rfq-home__value">{{ formatInt(stats.totalRfqs) }}</div>
            <div class="rfq-home__label">{{ t('rfqHome.cards.totalRfqs') }}</div>
          </div>
        </div>
        <div class="rfq-home__card">
          <div class="rfq-home__icon rfq-home__icon--violet" aria-hidden="true">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <line x1="12" y1="5" x2="12" y2="19" />
              <line x1="5" y1="12" x2="19" y2="12" />
            </svg>
          </div>
          <div class="rfq-home__card-main">
            <div class="rfq-home__value">{{ formatInt(stats.newRfqsLast30Days) }}</div>
            <div class="rfq-home__label">{{ t('rfqHome.cards.newRfqsLast30Days') }}</div>
          </div>
        </div>
        <div class="rfq-home__card">
          <div class="rfq-home__icon rfq-home__icon--green" aria-hidden="true">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <circle cx="12" cy="12" r="10" />
              <polyline points="12 6 12 12 16 14" />
            </svg>
          </div>
          <div class="rfq-home__card-main">
            <div class="rfq-home__value">{{ formatInt(stats.todayRfqs) }}</div>
            <div class="rfq-home__label">{{ t('rfqHome.cards.todayRfqs') }}</div>
          </div>
        </div>
      </div>

      <div class="rfq-home__duo">
        <div class="rfq-home__duo-col">
          <div class="rfq-home__section-title">{{ t('rfqHome.sections.quoteLast30') }}</div>
          <div class="rfq-home__row rfq-home__row--2">
            <div class="rfq-home__card">
              <div class="rfq-home__icon rfq-home__icon--blue" aria-hidden="true">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                  <path d="M12 2v20M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6" />
                </svg>
              </div>
              <div class="rfq-home__card-main">
                <div class="rfq-home__value">{{ formatInt(stats.quotesLast30Days) }}</div>
                <div class="rfq-home__label">{{ t('rfqHome.cards.quotesLast30Days') }}</div>
              </div>
            </div>
            <div class="rfq-home__card">
              <div class="rfq-home__icon rfq-home__icon--teal" aria-hidden="true">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                  <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
                  <polyline points="22 4 12 14.01 9 11.01" />
                </svg>
              </div>
              <div class="rfq-home__card-main">
                <div class="rfq-home__value">{{ formatPct(stats.quoteRateLast30Days) }}</div>
                <div class="rfq-home__label">{{ t('rfqHome.cards.quoteRateLast30Days') }}</div>
              </div>
            </div>
          </div>
        </div>
        <div class="rfq-home__duo-col">
          <div class="rfq-home__section-title">{{ t('rfqHome.sections.winLast30') }}</div>
          <div class="rfq-home__row rfq-home__row--2">
            <div class="rfq-home__card">
              <div class="rfq-home__icon rfq-home__icon--amber" aria-hidden="true">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                  <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
                  <polyline points="22 4 12 14.01 9 11.01" />
                </svg>
              </div>
              <div class="rfq-home__card-main">
                <div class="rfq-home__value">{{ formatInt(stats.wonRfqsLast30Days) }}</div>
                <div class="rfq-home__label">{{ t('rfqHome.cards.wonRfqsLast30Days') }}</div>
              </div>
            </div>
            <div class="rfq-home__card">
              <div class="rfq-home__icon rfq-home__icon--mint" aria-hidden="true">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                  <line x1="12" y1="20" x2="12" y2="10" />
                  <line x1="18" y1="20" x2="18" y2="4" />
                  <line x1="6" y1="20" x2="6" y2="16" />
                </svg>
              </div>
              <div class="rfq-home__card-main">
                <div class="rfq-home__value">{{ formatPct(stats.winRateLast30Days) }}</div>
                <div class="rfq-home__label">{{ t('rfqHome.cards.winRateLast30Days') }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '@/stores/auth'
import { rfqApi } from '@/api/rfq'
import { quoteApi } from '@/api/quote'

const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()
/** 与需求首页路由 rfq.read 一致；销售主部门账号在后端汇总中会合并 rfq.write */
const showCreateRfqButton = computed(() => authStore.hasPermission('rfq.read'))

const keyword = ref('')

interface RfqHomeStats {
  totalRfqs: number
  newRfqsLast30Days: number
  todayRfqs: number
  quotesLast30Days: number
  quoteRateLast30Days: number | null
  wonRfqsLast30Days: number
  winRateLast30Days: number | null
}

const stats = ref<RfqHomeStats | null>(null)
const loadingStats = ref(false)

const intFmt = new Intl.NumberFormat('zh-CN')

function ymd(d: Date) {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

function addDays(base: Date, days: number) {
  const d = new Date(base.getFullYear(), base.getMonth(), base.getDate() + days)
  return d
}

function parseTime(raw: unknown): Date | null {
  if (raw == null || raw === '') return null
  const d = new Date(String(raw))
  return Number.isNaN(d.getTime()) ? null : d
}

function startOfDay(d: Date) {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate())
}

function isInRange(t: Date, start: Date, endExclusive: Date) {
  return t >= start && t < endExclusive
}

function formatInt(n: number) {
  return intFmt.format(n ?? 0)
}

function formatPct(n: number | null) {
  if (n == null || Number.isNaN(n)) return '—'
  return `${n.toFixed(1)}%`
}

function handleMaterialSearch() {
  const q = keyword.value.trim()
  router.push({ path: '/pn', query: q ? { keyword: q } : {} })
}

function goPnPlain() {
  router.push({ path: '/pn', query: {} })
}

function goRfqList() {
  router.push({ name: 'RFQList' })
}

function goCreateRfq() {
  if (authStore.isIdentityBlockedForPermission('rfq.write')) {
    ElMessage.warning(t('rfqHome.createBlockedByIdentity'))
    return
  }
  if (!authStore.hasPermission('rfq.write')) {
    ElMessage.warning(t('rfqHome.createNeedRfqWrite'))
    return
  }
  router.push({ name: 'RFQCreate' })
}

async function loadStats() {
  loadingStats.value = true
  try {
    const today = new Date()
    const todayStr = ymd(today)
    const tomorrowStr = ymd(addDays(today, 1))
    const start30 = addDays(today, -30)
    const start30Str = ymd(start30)
    const rangeStart = startOfDay(start30)
    const rangeEndExclusive = startOfDay(addDays(today, 1))

    const [
      totalRes,
      new30Res,
      todayRes,
      items30Res,
      won30Res,
      quoteListRes
    ] = await Promise.all([
      rfqApi.searchRFQs({ pageNumber: 1, pageSize: 1 }),
      rfqApi.searchRFQs({ pageNumber: 1, pageSize: 1, startDate: start30Str, endDate: tomorrowStr }),
      rfqApi.searchRFQs({ pageNumber: 1, pageSize: 1, startDate: todayStr, endDate: tomorrowStr }),
      rfqApi.searchRFQItems({ pageNumber: 1, pageSize: 1, startDate: start30Str, endDate: tomorrowStr }),
      rfqApi.searchRFQs({
        pageNumber: 1,
        pageSize: 1,
        startDate: start30Str,
        endDate: tomorrowStr,
        status: 5
      }),
      quoteApi.getList({}).catch(() => ({ data: [] as unknown[] }))
    ])

    const quotes = (quoteListRes.data || []) as Record<string, unknown>[]
    let quotesLast30 = 0
    for (const q of quotes) {
      const t = parseTime(q.createTime ?? q.createdAt ?? q.quoteDate ?? q.QuoteDate)
      if (t && isInRange(t, rangeStart, rangeEndExclusive)) quotesLast30++
    }

    const newRfq30 = new30Res.totalCount ?? 0
    const items30 = items30Res.totalCount ?? 0
    const quoteRate = items30 > 0 ? (quotesLast30 / items30) * 100 : null
    const won30 = won30Res.totalCount ?? 0
    const winRate = newRfq30 > 0 ? (won30 / newRfq30) * 100 : null

    stats.value = {
      totalRfqs: totalRes.totalCount ?? 0,
      newRfqsLast30Days: newRfq30,
      todayRfqs: todayRes.totalCount ?? 0,
      quotesLast30Days: quotesLast30,
      quoteRateLast30Days: quoteRate,
      wonRfqsLast30Days: won30,
      winRateLast30Days: winRate
    }
  } catch {
    stats.value = null
  } finally {
    loadingStats.value = false
  }
}

onMounted(() => {
  loadStats()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&family=Noto+Sans+SC:wght@300;400;500;600&display=swap');

.rfq-home {
  min-height: 100%;
  padding-top: clamp(120px, 19vh, 300px);
  padding-bottom: 40px;
  padding-inline: clamp(36px, 14vw, 240px);
  font-family: 'Noto Sans SC', sans-serif;
  background: $layer-1;
  background-image: radial-gradient(ellipse 120% 80% at 50% -20%, var(--crm-home-hero-glow), transparent 55%);
}

.rfq-home__search-hero {
  display: flex;
  justify-content: center;
  width: 100%;
  margin-bottom: clamp(96px, 12vh, 128px);
}

.rfq-home__search-row {
  display: flex;
  align-items: center;
  gap: 16px;
  width: 100%;
}

.rfq-home__pill-slot {
  flex: 1;
  min-width: 0;
  display: flex;
  justify-content: center;
}

.rfq-home__pill-wrap {
  width: 100%;
  max-width: calc(100% * 2 / 3);
  min-width: 0;
}

.rfq-home__pill {
  display: flex;
  align-items: stretch;
  flex-wrap: wrap;
  width: 100%;
  min-height: 52px;
  padding: 4px 6px 4px 4px;
  gap: 4px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: 999px;
  box-shadow: $shadow-md;
}

.rfq-home__pill-field {
  flex: 1 1 160px;
  display: flex;
  align-items: center;
  min-width: 0;
  padding-left: 14px;
  padding-right: 8px;
}

.rfq-home__pill-mag {
  flex-shrink: 0;
  margin-right: 10px;
  color: $text-muted;
}

.rfq-home__pill-input {
  flex: 1;
  min-width: 0;
  border: none;
  background: transparent;
  color: $text-primary;
  font-size: 14px;
  font-family: inherit;
  outline: none;

  &::placeholder {
    color: $text-placeholder;
  }
}

.rfq-home__pill-btn {
  flex-shrink: 0;
  margin: 0 4px;
  padding: 0 22px;
  border: none;
  border-radius: 999px;
  font-size: 14px;
  font-weight: 500;
  font-family: inherit;
  color: #fff;
  cursor: pointer;
  background: linear-gradient(135deg, $blue-primary 0%, $cyan-primary 100%);
  box-shadow: var(--crm-shadow-glow);
  transition: filter 0.15s ease, transform 0.15s ease;

  &:hover {
    filter: brightness(1.06);
  }

  &:active {
    transform: scale(0.98);
  }
}

.rfq-home__pill-link {
  flex-shrink: 0;
  align-self: center;
  margin: 0 10px 0 4px;
  padding: 8px 4px;
  border: none;
  background: none;
  font-size: 13px;
  font-family: inherit;
  color: $cyan-primary;
  cursor: pointer;
  white-space: nowrap;
  transition: color 0.15s ease;
  opacity: 0.92;

  &:hover {
    opacity: 1;
    color: $cyan-primary;
  }
}

.rfq-home__btn-create {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  flex-shrink: 0;
  padding: 10px 18px;
  background: linear-gradient(135deg, $color-mint-green, $success-color);
  border: 1px solid color-mix(in srgb, $color-mint-green 55%, transparent);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  font-family: inherit;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  letter-spacing: 0.5px;
  white-space: nowrap;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px color-mix(in srgb, $color-mint-green 35%, transparent);
  }
}

.rfq-home__loading {
  text-align: center;
  color: $text-muted;
  padding: 40px;
  font-size: 14px;
}

.rfq-home__section-title {
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
  margin: 0 0 14px;
  letter-spacing: 0.04em;
}

.rfq-home__row {
  display: grid;
  gap: 14px;
  margin-bottom: 28px;

  &--3 {
    grid-template-columns: repeat(3, 1fr);
  }

  &--2 {
    grid-template-columns: repeat(2, 1fr);
  }

  @media (max-width: 900px) {
    &--3 {
      grid-template-columns: 1fr;
    }
  }

  @media (max-width: 520px) {
    &--2 {
      grid-template-columns: 1fr;
    }
  }
}

.rfq-home__duo {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 28px 32px;
  align-items: start;

  @media (max-width: 900px) {
    grid-template-columns: 1fr;
  }
}

.rfq-home__duo-col {
  min-width: 0;
}

.rfq-home__card {
  display: flex;
  align-items: flex-start;
  gap: 14px;
  padding: 18px 18px 20px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: 14px;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;

  &:hover {
    border-color: var(--crm-accent-012);
    box-shadow: $shadow-md;
  }
}

.rfq-home__icon {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;

  &--blue {
    background: rgba(50, 149, 201, 0.22);
    color: #6ec8f0;
    border: 1px solid rgba(50, 149, 201, 0.35);
  }
  &--green {
    background: rgba(70, 191, 145, 0.2);
    color: #5ee0ad;
    border: 1px solid rgba(70, 191, 145, 0.35);
  }
  &--amber {
    background: rgba(201, 154, 69, 0.2);
    color: #e4b86a;
    border: 1px solid rgba(201, 154, 69, 0.35);
  }
  &--violet {
    background: rgba(138, 99, 210, 0.22);
    color: #b89cf0;
    border: 1px solid rgba(138, 99, 210, 0.38);
  }
  &--teal {
    background: rgba(0, 180, 170, 0.18);
    color: #5ee6dc;
    border: 1px solid rgba(0, 212, 255, 0.28);
  }
  &--mint {
    background: rgba(70, 191, 145, 0.18);
    color: #6bdfb0;
    border: 1px solid rgba(70, 191, 145, 0.32);
  }
}

.rfq-home__card-main {
  min-width: 0;
  padding-top: 2px;
}

.rfq-home__value {
  font-size: 24px;
  font-weight: 700;
  color: $text-primary;
  font-family: 'Space Mono', monospace;
  line-height: 1.15;
  letter-spacing: -0.02em;
}

.rfq-home__label {
  margin-top: 6px;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.35;
}
</style>
