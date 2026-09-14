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
        <router-link to="/industry-news" class="news__link">
          {{ t('dashboard.industryNews.viewHistory') }}
        </router-link>
        <button
          type="button"
          class="news__link"
          :disabled="!canOpenFull"
          @click="openFull"
        >
          {{ t('dashboard.industryNews.viewToday') }}
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
      <IndustryNewsBriefingView
        ref="briefingRef"
        variant="dialog"
        :markdown="data?.markdown || ''"
        :stale-hint="data?.isStale ? t('dashboard.industryNews.staleHint') : ''"
      />
    </el-dialog>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import {
  industryNewsApi,
  waitForIndustryNewsRefresh,
  type IndustryNewsLatest
} from '@/api/industryNews'
import IndustryNewsBriefingView from '@/components/Dashboard/IndustryNewsBriefingView.vue'
import { useAuthStore } from '@/stores'
import { getApiErrorMessage } from '@/utils/apiError'

const { t } = useI18n()
const authStore = useAuthStore()
const isSysAdmin = computed(() => authStore.hasSysAdminRole())

const loading = ref(false)
const refreshing = ref(false)
const data = ref<IndustryNewsLatest | null>(null)
const dialogOpen = ref(false)
const briefingRef = ref<{ resetScroll: () => Promise<void> } | null>(null)

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

async function onDialogOpened() {
  await briefingRef.value?.resetScroll()
}

async function refreshNow() {
  if (!isSysAdmin.value || refreshing.value) return
  refreshing.value = true
  try {
    const previous = data.value
    const result = await industryNewsApi.runToday(true)
    if (result.latest) data.value = result.latest
    if (result.pending) {
      data.value = await waitForIndustryNewsRefresh(previous)
      ElMessage.success(t('dashboard.industryNews.refreshDone'))
    } else if (result.success) {
      if (!result.latest) data.value = await industryNewsApi.getLatest()
      ElMessage.success(t('dashboard.industryNews.refreshDone'))
    } else {
      ElMessage.warning(result.message || t('dashboard.industryNews.refreshFailed'))
    }
  } catch (e: unknown) {
    const msg = getApiErrorMessage(e, t('dashboard.industryNews.refreshFailed'))
    if (/仍在生成|still running/i.test(msg))
      ElMessage.warning(t('dashboard.industryNews.refreshStillRunning'))
    else ElMessage.error(msg)
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
  text-decoration: none;
  line-height: 1.4;
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
</style>

<style lang="scss">
.industry-news-dialog.el-dialog {
  .el-dialog__body {
    overflow: hidden;
  }
}
</style>
