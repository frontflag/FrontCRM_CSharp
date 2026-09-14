<template>
  <section class="news" aria-labelledby="dashboard-industry-news-title">
    <header class="news__head">
      <div class="news__title-row">
        <h3 id="dashboard-industry-news-title" class="news__title">
          {{ t('dashboard.industryNews.title') }}
        </h3>
        <p v-if="!loading && data?.hasBriefing" class="news__meta">
          <span>{{ metaText }}</span>
        </p>
      </div>
      <div class="news__actions">
        <button
          v-if="isSysAdmin"
          type="button"
          class="news__link"
          :disabled="refreshing"
          @click="refreshNow"
        >
          {{ refreshing ? t('dashboard.industryNews.refreshing') : t('dashboard.industryNews.refreshNow') }}
        </button>
        <button
          type="button"
          class="news__link"
          :disabled="!canOpenFull"
          @click="openFull"
        >
          {{ t('dashboard.industryNews.viewAll') }}
        </button>
      </div>
    </header>

    <div v-loading="loading || refreshing" class="news__body">
      <p v-if="!loading && displayItems.length === 0" class="news__empty">
        {{ emptyText }}
      </p>
      <button
        v-for="(row, idx) in displayItems"
        :key="`${row.occurredOn}-${idx}-${row.title}`"
        type="button"
        class="news-row"
        @click="openFull"
      >
        <span class="news-row__chip" :class="chipClass(row.category)">{{ row.category }}</span>
        <span class="news-row__title">{{ row.title }}</span>
        <span class="news-row__time">{{ formatDate(row.occurredOn) }}</span>
      </button>
    </div>

    <el-dialog
      v-model="dialogOpen"
      :title="dialogTitle"
      width="920px"
      append-to-body
      destroy-on-close
      class="industry-news-dialog"
      @opened="onDialogOpened"
    >
      <div class="news-dialog__layout">
        <div ref="scrollRef" class="news-dialog__scroll" @scroll.passive="syncActiveFloor">
          <p v-if="data?.isStale" class="news-dialog__stale">
            {{ t('dashboard.industryNews.staleHint') }}
          </p>
          <div class="news-dialog__md" v-html="decoratedHtml" />
        </div>
        <nav
          v-if="floors.length"
          class="news-dialog__floors"
          :aria-label="t('dashboard.industryNews.floorNav')"
        >
          <p class="news-dialog__floors-title">{{ t('dashboard.industryNews.floorNav') }}</p>
          <button
            v-for="floor in floors"
            :key="floor.id"
            type="button"
            class="news-dialog__floor"
            :class="{ 'is-active': floor.id === activeFloorId, 'is-sub': floor.level === 3 }"
            @click="scrollToFloor(floor.id)"
          >
            {{ floor.title }}
          </button>
        </nav>
      </div>
    </el-dialog>
  </section>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import { industryNewsApi, type IndustryNewsLatest } from '@/api/industryNews'
import { useAuthStore } from '@/stores'
import { getApiErrorMessage } from '@/utils/apiError'
import { renderAnnouncementMarkdown } from '@/utils/sanitizeAnnouncementHtml'

const { t } = useI18n()
const authStore = useAuthStore()
const isSysAdmin = computed(() => authStore.hasSysAdminRole())

const loading = ref(false)
const refreshing = ref(false)
const data = ref<IndustryNewsLatest | null>(null)
const dialogOpen = ref(false)
const scrollRef = ref<HTMLElement | null>(null)
const activeFloorId = ref('')

type NewsFloor = { id: string; title: string; level: 2 | 3 }

const displayItems = computed(() => (data.value?.items ?? []).slice(0, 5))
const canOpenFull = computed(
  () => !!(data.value?.hasBriefing && (data.value.markdown || displayItems.value.length))
)

const metaText = computed(() => {
  if (!data.value?.hasBriefing) return ''
  const n = displayItems.value.length
  return data.value.isStale
    ? t('dashboard.industryNews.metaStale', { n })
    : t('dashboard.industryNews.metaToday', { n })
})

const emptyText = computed(() =>
  data.value?.hasBriefing
    ? t('dashboard.industryNews.emptyItems')
    : t('dashboard.industryNews.empty')
)

const dialogTitle = computed(() => {
  const date = data.value?.briefingDate
  return date
    ? t('dashboard.industryNews.dialogTitleDated', { date: formatDate(date) })
    : t('dashboard.industryNews.dialogTitle')
})

function normalizeBriefingStars(md: string) {
  return md.replace(/[★☆⭐✦✪🌟]{2,}/g, '★')
}

const renderedMarkdown = computed(() =>
  renderAnnouncementMarkdown(normalizeBriefingStars(data.value?.markdown || ''))
)

const decorated = computed(() => decorateIndustryNewsHtml(renderedMarkdown.value))
const decoratedHtml = computed(() => decorated.value.html)
const floors = computed(() => decorated.value.floors)

const categoryFloorTitles = new Set([
  '行业动态',
  '关键厂商',
  '行情价格',
  '重组并购',
  '公司治理',
  '展会信息',
  '行業動態',
  '關鍵廠商',
  '行情價格',
  '重組併購',
  '展會信息',
  '展會資訊'
])

function headingTitle(el: Element) {
  return (el.textContent || '').replace(/\s+/g, ' ').trim()
}

function decorateIndustryNewsHtml(html: string): { html: string; floors: NewsFloor[] } {
  if (!html.trim() || typeof DOMParser === 'undefined') return { html, floors: [] }
  const doc = new DOMParser().parseFromString(`<div class="in-root">${html}</div>`, 'text/html')
  const root = doc.body.querySelector('.in-root')
  if (!root) return { html, floors: [] }
  const floors: NewsFloor[] = []
  const nodes = Array.from(root.querySelectorAll('h2, h3, p'))
  nodes.forEach((el) => {
    const title = headingTitle(el)
    if (!title) return
    const isHeading = el.tagName === 'H2' || el.tagName === 'H3'
    const isCategoryPara = el.tagName === 'P' && categoryFloorTitles.has(title)
    if (!isHeading && !isCategoryPara) return
    const id = `industry-news-floor-${floors.length}`
    el.setAttribute('id', id)
    floors.push({
      id,
      title,
      level: el.tagName === 'H2' ? 2 : 3
    })
  })
  return { html: root.innerHTML, floors }
}

function offsetInScroll(root: HTMLElement, el: HTMLElement) {
  return el.getBoundingClientRect().top - root.getBoundingClientRect().top + root.scrollTop
}

function syncActiveFloor() {
  const root = scrollRef.value
  if (!root || floors.value.length === 0) return
  const y = root.scrollTop + 24
  let current = floors.value[0].id
  for (const floor of floors.value) {
    const el = root.querySelector(`#${floor.id}`) as HTMLElement | null
    if (el && offsetInScroll(root, el) <= y) current = floor.id
  }
  activeFloorId.value = current
}

function scrollToFloor(id: string) {
  const root = scrollRef.value
  const el = root?.querySelector(`#${id}`) as HTMLElement | null
  if (!root || !el) return
  activeFloorId.value = id
  root.scrollTo({ top: Math.max(0, offsetInScroll(root, el) - 8), behavior: 'smooth' })
}

async function onDialogOpened() {
  await nextTick()
  scrollRef.value?.scrollTo({ top: 0 })
  activeFloorId.value = floors.value[0]?.id ?? ''
}

function chipClass(category: string) {
  const map: Record<string, string> = {
    行业动态: 'is-trend',
    关键厂商: 'is-vendor',
    行情价格: 'is-price',
    重组并购: 'is-ma',
    公司治理: 'is-gov',
    展会信息: 'is-expo'
  }
  return map[category] || 'is-other'
}

function formatDate(raw: string | null | undefined) {
  if (!raw) return ''
  const m = raw.match(/^(\d{4})-(\d{2})-(\d{2})/)
  if (!m) return raw
  return `${Number(m[2])}-${Number(m[3])}`
}

function openFull() {
  if (!canOpenFull.value) return
  dialogOpen.value = true
}

async function refreshNow() {
  if (!isSysAdmin.value || refreshing.value) return
  refreshing.value = true
  try {
    const result = await industryNewsApi.runToday(true)
    if (result.latest) data.value = result.latest
    else data.value = await industryNewsApi.getLatest()
    if (result.success) ElMessage.success(t('dashboard.industryNews.refreshDone'))
    else ElMessage.warning(result.message || t('dashboard.industryNews.refreshFailed'))
  } catch (e: unknown) {
    ElMessage.error(getApiErrorMessage(e, t('dashboard.industryNews.refreshFailed')))
  } finally {
    refreshing.value = false
  }
}

onMounted(async () => {
  loading.value = true
  try {
    data.value = await industryNewsApi.getLatest()
  } catch {
    data.value = {
      hasBriefing: false,
      isStale: false,
      briefingDate: null,
      periodStart: null,
      periodEnd: null,
      generatedAt: null,
      items: [],
      markdown: ''
    }
  } finally {
    loading.value = false
  }
})
</script>

<style lang="scss" scoped>
.news {
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 12px;
  padding: 16px 20px 14px;
}

.news__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 12px;
}

.news__title-row {
  display: flex;
  align-items: baseline;
  gap: 10px;
  min-width: 0;
}

.news__title {
  margin: 0;
  font-size: 15px;
  font-weight: 600;
}

.news__meta {
  margin: 0;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.news__actions {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-shrink: 0;
}

.news__link {
  flex-shrink: 0;
  padding: 0;
  border: 0;
  background: none;
  font-size: 12px;
  color: var(--el-color-primary);
  cursor: pointer;
  &:hover:not(:disabled) {
    text-decoration: underline;
  }
  &:disabled {
    color: var(--el-text-color-disabled);
    cursor: default;
  }
}

.news__empty {
  margin: 0;
  font-size: 13px;
  color: var(--el-text-color-secondary);
}

.news-row {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  padding: 8px 0;
  border: 0;
  background: transparent;
  text-align: left;
  cursor: pointer;
  & + & {
    border-top: 1px solid var(--el-border-color-extra-light);
  }
}

.news-row__chip {
  flex-shrink: 0;
  padding: 1px 6px;
  border-radius: 4px;
  font-size: 11px;
  line-height: 18px;
  font-weight: 600;
  &.is-trend {
    color: #3b5bdb;
    background: #edf2ff;
  }
  &.is-vendor {
    color: #7048e8;
    background: #f3f0ff;
  }
  &.is-price {
    color: #e67700;
    background: #fff4e6;
  }
  &.is-ma {
    color: #0b7285;
    background: #e3fafc;
  }
  &.is-gov {
    color: #2b8a3e;
    background: #ebfbee;
  }
  &.is-expo {
    color: #c2255c;
    background: #fff0f6;
  }
  &.is-other {
    color: var(--el-text-color-regular);
    background: var(--el-fill-color);
  }
}

.news-row__title {
  min-width: 0;
  flex: 1;
  font-size: 13px;
  font-weight: 600;
  color: var(--el-text-color-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.news-row__time {
  flex-shrink: 0;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.news-dialog__layout {
  display: flex;
  align-items: stretch;
  gap: 8px;
  min-height: 280px;
  max-height: min(70vh, 640px);
}

.news-dialog__scroll {
  flex: 1;
  min-width: 0;
  overflow: auto;
  padding-right: 8px;
}

.news-dialog__floors {
  flex: 0 0 148px;
  display: flex;
  flex-direction: column;
  gap: 2px;
  overflow: auto;
  padding: 0 0 0 14px;
  border-left: 1px solid var(--el-border-color-lighter);
}

.news-dialog__floors-title {
  margin: 0 0 6px;
  font-size: 11px;
  font-weight: 600;
  color: var(--el-text-color-secondary);
}

.news-dialog__floor {
  display: block;
  width: 100%;
  padding: 5px 0;
  border: 0;
  background: none;
  text-align: left;
  font-size: 12px;
  line-height: 1.35;
  color: var(--el-text-color-regular);
  cursor: pointer;
  &.is-sub {
    padding-left: 10px;
    color: var(--el-text-color-secondary);
  }
  &.is-active {
    color: var(--el-color-primary);
    font-weight: 600;
  }
  &:hover:not(.is-active) {
    color: var(--el-color-primary);
  }
}

.news-dialog__stale {
  margin: 0 0 12px;
  font-size: 12px;
  color: var(--el-color-warning);
}

.news-dialog__md {
  font-size: 13px;
  line-height: 1.7;
  color: var(--el-text-color-primary);
  :deep(h2),
  :deep(h3) {
    scroll-margin-top: 8px;
  }
  :deep(h2) {
    margin: 18px 0 8px;
    font-size: 16px;
    color: #8d4e16;
  }
  :deep(h3) {
    margin: 14px 0 6px;
    font-size: 14px;
    font-weight: 600;
  }
  :deep(h2:first-child) {
    margin-top: 0;
  }
  :deep(p) {
    margin: 0 0 8px;
  }
  :deep(table) {
    width: 100%;
    border-collapse: collapse;
    margin: 8px 0 12px;
    font-size: 12px;
  }
  :deep(th),
  :deep(td) {
    border: 1px solid var(--el-border-color-lighter);
    padding: 6px 8px;
    text-align: left;
  }
}
</style>

<style lang="scss">
.industry-news-dialog.el-dialog {
  .el-dialog__body {
    overflow: hidden;
  }
}

@media (max-width: 640px) {
  .industry-news-dialog .news-dialog__floors {
    display: none;
  }
}
</style>
