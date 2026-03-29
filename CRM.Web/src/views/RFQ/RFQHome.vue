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
                  placeholder="输入物料型号、品牌或关键词…"
                  autocomplete="off"
                  @keyup.enter="handleMaterialSearch"
                />
              </div>
              <button type="button" class="rfq-home__pill-btn" @click="handleMaterialSearch">搜索</button>
              <button type="button" class="rfq-home__pill-link" @click="goPnPlain">进入物料列表</button>
              <button type="button" class="rfq-home__pill-link" @click="goRfqList">进入需求列表</button>
            </div>
          </div>
        </div>
        <button
          v-if="canCreateRfq"
          type="button"
          class="rfq-home__btn-create"
          @click="goCreateRfq"
        >
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          新建需求
        </button>
      </div>
    </div>

    <div v-if="loadingStats" class="rfq-home__loading">加载概要数据…</div>

    <template v-else-if="stats">
      <div class="rfq-home__section-title">需求</div>
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
            <div class="rfq-home__label">需求总数</div>
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
            <div class="rfq-home__label">30 天新增需求</div>
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
            <div class="rfq-home__label">今日需求</div>
          </div>
        </div>
      </div>

      <div class="rfq-home__duo">
        <div class="rfq-home__duo-col">
          <div class="rfq-home__section-title">报价（近 30 天）</div>
          <div class="rfq-home__row rfq-home__row--2">
            <div class="rfq-home__card">
              <div class="rfq-home__icon rfq-home__icon--blue" aria-hidden="true">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                  <path d="M12 2v20M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6" />
                </svg>
              </div>
              <div class="rfq-home__card-main">
                <div class="rfq-home__value">{{ formatInt(stats.quotesLast30Days) }}</div>
                <div class="rfq-home__label">报价数</div>
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
                <div class="rfq-home__label">报价率</div>
              </div>
            </div>
          </div>
        </div>
        <div class="rfq-home__duo-col">
          <div class="rfq-home__section-title">成单（近 30 天）</div>
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
                <div class="rfq-home__label">成单需求</div>
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
                <div class="rfq-home__label">成单率</div>
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
import { useAuthStore } from '@/stores/auth'
import { rfqApi } from '@/api/rfq'
import { quoteApi } from '@/api/quote'

const router = useRouter()
const authStore = useAuthStore()
const canCreateRfq = computed(() => authStore.hasPermission('rfq.write'))

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
  background: #0b0e14;
  background-image: radial-gradient(ellipse 120% 80% at 50% -20%, rgba(0, 102, 255, 0.12), transparent 55%);
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
  background: rgba(22, 34, 51, 0.95);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 999px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.35);
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
  color: rgba(200, 216, 232, 0.45);
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
    color: rgba(200, 216, 232, 0.38);
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
  background: linear-gradient(135deg, #0066ff 0%, #00b8e6 100%);
  box-shadow: 0 2px 14px rgba(0, 212, 255, 0.25);
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
  color: rgba(0, 212, 255, 0.85);
  cursor: pointer;
  white-space: nowrap;
  transition: color 0.15s ease;

  &:hover {
    color: #00d4ff;
  }
}

.rfq-home__btn-create {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  flex-shrink: 0;
  padding: 10px 18px;
  background: linear-gradient(135deg, rgba(46, 160, 67, 0.85), rgba(70, 191, 145, 0.75));
  border: 1px solid rgba(70, 191, 145, 0.45);
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
    box-shadow: 0 4px 16px rgba(70, 191, 145, 0.3);
  }
}

.rfq-home__loading {
  text-align: center;
  color: rgba(200, 216, 232, 0.5);
  padding: 40px;
  font-size: 14px;
}

.rfq-home__section-title {
  font-size: 15px;
  font-weight: 600;
  color: rgba(224, 244, 255, 0.92);
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
  background: rgba(26, 38, 58, 0.75);
  border: 1px solid rgba(255, 255, 255, 0.06);
  border-radius: 14px;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;

  &:hover {
    border-color: rgba(0, 212, 255, 0.12);
    box-shadow: 0 4px 24px rgba(0, 0, 0, 0.25);
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
  color: #f0f8ff;
  font-family: 'Space Mono', monospace;
  line-height: 1.15;
  letter-spacing: -0.02em;
}

.rfq-home__label {
  margin-top: 6px;
  font-size: 12px;
  color: rgba(200, 216, 232, 0.52);
  line-height: 1.35;
}
</style>
