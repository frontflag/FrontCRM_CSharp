<template>
  <div class="history" v-loading="listLoading">
    <aside class="history__dates" :aria-label="t('dashboard.industryNews.historyDates')">
      <p v-if="!listLoading && dates.length === 0" class="history__empty">
        {{ t('dashboard.industryNews.historyEmpty') }}
      </p>
      <button
        v-for="row in dates"
        :key="row.briefingDate"
        type="button"
        class="history__date"
        :class="{ 'is-active': dateKey(row.briefingDate) === selectedDate }"
        @click="selectDate(row.briefingDate)"
      >
        <span class="history__date-main">{{ dateKey(row.briefingDate) }}</span>
        <span class="history__date-sub">{{ periodText(row) }}</span>
      </button>
    </aside>
    <section class="history__body" v-loading="detailLoading">
      <p v-if="detailError" class="history__empty">{{ detailError }}</p>
      <IndustryNewsBriefingView
        v-else-if="detail"
        variant="page"
        :markdown="detail.markdown || ''"
      />
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import {
  industryNewsApi,
  type IndustryNewsLatest,
  type IndustryNewsListItem
} from '@/api/industryNews'
import IndustryNewsBriefingView from '@/components/Dashboard/IndustryNewsBriefingView.vue'
import { getApiErrorMessage } from '@/utils/apiError'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const listLoading = ref(false)
const detailLoading = ref(false)
const dates = ref<IndustryNewsListItem[]>([])
const detail = ref<IndustryNewsLatest | null>(null)
const detailError = ref('')

const selectedDate = computed(() => {
  const q = route.query.date
  return typeof q === 'string' ? dateKey(q) : ''
})

function dateKey(raw: string | null | undefined) {
  if (!raw) return ''
  const m = String(raw).match(/^(\d{4}-\d{2}-\d{2})/)
  return m ? m[1] : String(raw)
}

function periodText(row: IndustryNewsListItem) {
  const start = shortDate(row.periodStart)
  const end = shortDate(row.periodEnd)
  if (!start && !end) return ''
  return t('dashboard.industryNews.periodCover', { start, end })
}

function shortDate(raw: string | null | undefined) {
  if (!raw) return ''
  const m = raw.match(/^(\d{4})-(\d{2})-(\d{2})/)
  if (!m) return raw
  return `${Number(m[2])}-${Number(m[3])}`
}

async function selectDate(date: string) {
  const key = dateKey(date)
  if (!key || key === selectedDate.value) return
  await router.replace({ path: '/industry-news', query: { date: key } })
}

async function ensureDateQuery() {
  if (selectedDate.value || dates.value.length === 0) return
  await router.replace({ path: '/industry-news', query: { date: dateKey(dates.value[0].briefingDate) } })
}

async function loadList() {
  listLoading.value = true
  try {
    const rows = await industryNewsApi.list()
    dates.value = Array.isArray(rows) ? rows : []
    await ensureDateQuery()
  } catch (e: unknown) {
    dates.value = []
    detailError.value = getApiErrorMessage(e, t('dashboard.industryNews.historyEmpty'))
  } finally {
    listLoading.value = false
  }
}

async function loadDetail(date: string) {
  if (!date) {
    detail.value = null
    detailError.value = dates.value.length ? '' : t('dashboard.industryNews.historyEmpty')
    return
  }
  detailLoading.value = true
  try {
    detailError.value = ''
    detail.value = await industryNewsApi.getByDate(date)
  } catch {
    detail.value = null
    detailError.value = t('dashboard.industryNews.historyMissing')
  } finally {
    detailLoading.value = false
  }
}

watch(selectedDate, (date) => {
  void loadDetail(date)
}, { immediate: true })

onMounted(() => {
  void loadList()
})
</script>

<style lang="scss" scoped>
.history {
  display: flex;
  height: calc(100vh - 132px);
  min-height: 420px;
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 12px;
  overflow: hidden;
}

.history__dates {
  flex: 0 0 188px;
  overflow: auto;
  padding: 10px 8px;
  border-right: 1px solid var(--el-border-color-lighter);
}

.history__date {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 2px;
  width: 100%;
  margin: 0;
  padding: 8px 10px;
  border: 0;
  border-radius: 8px;
  background: none;
  text-align: left;
  cursor: pointer;
  & + & {
    margin-top: 2px;
  }
  &:hover:not(.is-active) {
    background: var(--el-fill-color-light);
  }
  &.is-active {
    background: var(--el-color-primary-light-9);
  }
}

.history__date-main {
  font-size: 13px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.history__date-sub {
  font-size: 11px;
  color: var(--el-text-color-secondary);
}

.history__body {
  flex: 1;
  min-width: 0;
  min-height: 0;
  display: flex;
  overflow: hidden;
  padding: 16px 12px 16px 16px;
}

.history__empty {
  margin: 16px;
  font-size: 13px;
  color: var(--el-text-color-secondary);
}
</style>
