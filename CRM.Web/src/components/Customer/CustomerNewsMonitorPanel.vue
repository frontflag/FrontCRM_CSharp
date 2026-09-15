<template>
  <div class="customer-news" v-loading="listLoading">
    <div class="customer-news__toolbar">
      <span class="customer-news__hint">{{ t('customerDetail.news.hint') }}</span>
      <div class="customer-news__actions">
        <el-button
          v-if="showDeleteButton"
          class="customer-news__delete"
          type="danger"
          plain
          :loading="deleting"
          :disabled="busy || deleting || !selectedId"
          @click="runDelete"
        >
          {{ t('customerDetail.news.delete') }}
        </el-button>
        <el-button
          v-if="showFetchButton"
          class="customer-news__fetch"
          type="primary"
          :loading="busy"
          :disabled="busy"
          @click="runFetch"
        >
          {{ busy ? t('customerDetail.news.fetchRunning') : t('customerDetail.news.fetchNow') }}
        </el-button>
      </div>
    </div>

    <div class="customer-news__body">
      <aside class="customer-news__dates" :aria-label="t('customerDetail.news.historyDates')">
        <p v-if="!listLoading && items.length === 0" class="customer-news__empty">
          {{ listError || t('customerDetail.news.historyEmpty') }}
        </p>
        <button
          v-for="row in items"
          :key="row.id"
          type="button"
          class="customer-news__date"
          :class="{ 'is-active': row.id === selectedId }"
          @click="selectRow(row.id)"
        >
          <span class="customer-news__date-main">{{ dateKey(row.briefingDate) }}</span>
          <span class="customer-news__date-sub">{{ subText(row) }}</span>
        </button>
      </aside>
      <section class="customer-news__content" v-loading="detailLoading">
        <p v-if="detailError" class="customer-news__empty">{{ detailError }}</p>
        <IndustryNewsBriefingView
          v-else-if="detail"
          variant="page"
          :normalize-markdown="false"
          :markdown="detail.markdown || ''"
        />
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter, type LocationQuery } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useAuthStore } from '@/stores/auth'
import { AI_PERMISSION_CUSTOMER_INTEL_LOOKUP } from '@/api/ai'
import {
  customerNewsApi,
  type CustomerNewsDetail,
  type CustomerNewsListItem
} from '@/api/customerNews'
import IndustryNewsBriefingView from '@/components/Dashboard/IndustryNewsBriefingView.vue'
import { getApiErrorMessage } from '@/utils/apiError'

const props = defineProps<{
  customerId: string
  salesUserId?: string
  active: boolean
}>()

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const listLoading = ref(false)
const detailLoading = ref(false)
const busy = ref(false)
const deleting = ref(false)
const canFetchFromApi = ref(false)
const canDeleteFromApi = ref(false)
const listError = ref('')
const items = ref<CustomerNewsListItem[]>([])
const detail = ref<CustomerNewsDetail | null>(null)
const detailError = ref('')
const loaded = ref(false)
let watchGen = 0
let watchTimer: ReturnType<typeof setTimeout> | null = null

const showDeleteButton = computed(() => {
  if (canDeleteFromApi.value) return true
  return authStore.hasSysAdminRole()
})

const showFetchButton = computed(() => {
  if (canFetchFromApi.value) return true
  if (authStore.canForceDelete() || authStore.hasSysAdminRole()) return true
  if (authStore.hasPermission(AI_PERMISSION_CUSTOMER_INTEL_LOOKUP)) return true
  const sid = (props.salesUserId || '').trim().toLowerCase()
  const uid = (authStore.user?.id || '').trim().toLowerCase()
  return !!sid && !!uid && sid === uid
})

const selectedId = computed(() => {
  const q = route.query.newsId
  return typeof q === 'string' ? q : ''
})

function dateKey(raw: string | null | undefined) {
  if (!raw) return ''
  const m = String(raw).match(/^(\d{4}-\d{2}-\d{2})/)
  return m ? m[1] : String(raw)
}

function timeText(raw: string | null | undefined) {
  if (!raw) return ''
  const d = new Date(raw)
  if (Number.isNaN(d.getTime())) return ''
  const hh = String(d.getHours()).padStart(2, '0')
  const mm = String(d.getMinutes()).padStart(2, '0')
  return `${hh}:${mm}`
}

function shortDate(raw: string | null | undefined) {
  if (!raw) return ''
  const m = String(raw).match(/^(\d{4})-(\d{2})-(\d{2})/)
  if (!m) return raw
  return `${Number(m[2])}-${Number(m[3])}`
}

function subText(row: CustomerNewsListItem) {
  const start = shortDate(row.periodStart)
  const end = shortDate(row.periodEnd)
  const time = timeText(row.generatedAt)
  const period = start && end ? t('customerDetail.news.periodCover', { start, end }) : ''
  return [time, period].filter(Boolean).join(' · ')
}

async function selectRow(id: string) {
  if (!id || id === selectedId.value) return
  await router.replace({ query: { ...route.query, tab: 'news', newsId: id } })
}

async function ensureSelected() {
  if (selectedId.value || items.value.length === 0) return
  await router.replace({
    query: { ...route.query, tab: 'news', newsId: items.value[0].id }
  })
}

function stopWatch() {
  watchGen += 1
  if (watchTimer != null) {
    clearTimeout(watchTimer)
    watchTimer = null
  }
}

async function applyListPayload(
  data: CustomerNewsListItem[] | {
    canFetch?: boolean
    CanFetch?: boolean
    canDelete?: boolean
    CanDelete?: boolean
    isRunning?: boolean
    IsRunning?: boolean
    items?: CustomerNewsListItem[]
  }
) {
  if (Array.isArray(data)) {
    canFetchFromApi.value = false
    canDeleteFromApi.value = false
    items.value = data
    return undefined as boolean | undefined
  }
  canFetchFromApi.value = !!(data?.canFetch ?? data?.CanFetch)
  canDeleteFromApi.value = !!(data?.canDelete ?? data?.CanDelete)
  items.value = Array.isArray(data?.items) ? data.items : []
  if (data?.isRunning === true || data?.IsRunning === true) return true
  if (data?.isRunning === false || data?.IsRunning === false) return false
  return undefined
}

async function loadList(opts?: { followRunning?: boolean }) {
  listLoading.value = true
  listError.value = ''
  try {
    const data = await customerNewsApi.list(props.customerId)
    const stillRunning = await applyListPayload(data as CustomerNewsListItem[] | {
      canFetch?: boolean
      CanFetch?: boolean
      canDelete?: boolean
      CanDelete?: boolean
      isRunning?: boolean
      IsRunning?: boolean
      items?: CustomerNewsListItem[]
    })
    await ensureSelected()
    if (opts?.followRunning !== false && stillRunning === true) {
      busy.value = true
      startWatch()
    }
  } catch (e: unknown) {
    items.value = []
    canFetchFromApi.value = false
    canDeleteFromApi.value = false
    listError.value = getApiErrorMessage(e, t('customerDetail.news.historyEmpty'))
  } finally {
    listLoading.value = false
  }
}

async function loadDetail(id: string) {
  if (!id) {
    detail.value = null
    detailError.value = listError.value || (items.value.length ? '' : t('customerDetail.news.historyEmpty'))
    return
  }
  detailLoading.value = true
  try {
    detailError.value = ''
    detail.value = await customerNewsApi.getById(props.customerId, id)
  } catch {
    detail.value = null
    detailError.value = t('customerDetail.news.historyMissing')
  } finally {
    detailLoading.value = false
  }
}

async function revealLatest() {
  await loadList({ followRunning: false })
  const first = items.value[0]?.id
  if (first && first !== selectedId.value) {
    await router.replace({ query: { ...route.query, tab: 'news', newsId: first } })
    return
  }
  await loadDetail(selectedId.value)
}

function startWatch() {
  const gen = ++watchGen
  if (watchTimer != null) {
    clearTimeout(watchTimer)
    watchTimer = null
  }
  const deadline = Date.now() + 8 * 60 * 1000
  const prevFirstId = items.value[0]?.id || ''

  const tick = async () => {
    if (gen !== watchGen) return
    try {
      const run = await customerNewsApi.latestRun(props.customerId)
      if (gen !== watchGen) return
      const running = run?.isRunning ?? run?.IsRunning
      if (running === true) {
        /* still going */
      } else if (running === false) {
        const status = String(run?.status ?? '').toLowerCase()
        await revealLatest()
        if (gen !== watchGen) return
        busy.value = false
        if (status === 'failed') {
          ElMessage.error(run?.message || t('customerDetail.news.fetchFailed'))
        } else {
          ElMessage.success(t('customerDetail.news.fetchDone'))
        }
        return
      } else {
        await loadList({ followRunning: false })
        if (gen !== watchGen) return
        const firstId = items.value[0]?.id || ''
        const status = String(run?.status ?? '').toLowerCase()
        if (firstId && firstId !== prevFirstId) {
          await revealLatest()
          if (gen !== watchGen) return
          busy.value = false
          ElMessage.success(t('customerDetail.news.fetchDone'))
          return
        }
        if (status === 'failed') {
          busy.value = false
          ElMessage.error(run?.message || t('customerDetail.news.fetchFailed'))
          return
        }
      }
    } catch {
      /* 轻量查询失败时继续等，不放开按钮 */
    }
    if (Date.now() >= deadline) {
      if (gen !== watchGen) return
      busy.value = false
      return
    }
    watchTimer = setTimeout(() => {
      void tick()
    }, 4000)
  }

  watchTimer = setTimeout(() => {
    void tick()
  }, 4000)
}

async function runFetch() {
  if (busy.value) return
  busy.value = true
  try {
    await customerNewsApi.run(props.customerId)
    ElMessage({
      type: 'success',
      message: t('customerDetail.news.fetchStarted'),
      duration: 8000,
      showClose: true
    })
    startWatch()
  } catch (e: unknown) {
    busy.value = false
    stopWatch()
    ElMessage.error(getApiErrorMessage(e, t('customerDetail.news.fetchFailed')))
  }
}

async function selectAfterDelete(fallbackId: string) {
  const remaining = items.value
  const next = remaining.find((x) => x.id === fallbackId) || remaining[0]
  const query: LocationQuery = { ...route.query, tab: 'news' }
  if (next) {
    await router.replace({ query: { ...query, newsId: next.id } })
    if (next.id === selectedId.value) {
      await loadDetail(next.id)
    }
    return
  }
  const nextQuery: LocationQuery = { ...query }
  delete nextQuery.newsId
  await router.replace({ query: nextQuery })
  await loadDetail('')
}

function escapeHtml(text: string) {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
}

function selectedRowConfirmHtml(row: CustomerNewsListItem | undefined) {
  const main = dateKey(row?.briefingDate)
  const sub = row ? subText(row) : ''
  const mainLine = `<div style="font-size:13px;font-weight:600;line-height:1.4">${escapeHtml(main)}</div>`
  const subLine = sub
    ? `<div style="margin-top:2px;font-size:11px;color:var(--el-text-color-secondary);line-height:1.4">${escapeHtml(sub)}</div>`
    : ''
  return `${mainLine}${subLine}`
}

async function runDelete() {
  if (busy.value || deleting.value || !showDeleteButton.value) return
  const id = selectedId.value
  if (!id) return
  const row = items.value.find((x) => x.id === id)
  try {
    await ElMessageBox.confirm(
      selectedRowConfirmHtml(row),
      t('customerDetail.news.deleteTitle'),
      { type: 'warning', dangerouslyUseHTMLString: true }
    )
  } catch {
    return
  }

  const ids = items.value.map((x) => x.id)
  const idx = ids.indexOf(id)
  const fallbackId = (idx >= 0 ? ids[idx + 1] || ids[idx - 1] : '') || ''

  deleting.value = true
  try {
    await customerNewsApi.remove(props.customerId, id)
    await loadList({ followRunning: false })
    await selectAfterDelete(fallbackId)
    ElMessage.success(t('customerDetail.news.deleteDone'))
  } catch (e: unknown) {
    ElMessage.error(getApiErrorMessage(e, t('customerDetail.news.deleteFailed')))
  } finally {
    deleting.value = false
  }
}

watch(selectedId, (id) => {
  if (!props.active) return
  void loadDetail(id)
})

watch(
  () => props.active,
  (on) => {
    if (!on) return
    if (!loaded.value) {
      loaded.value = true
      void loadList().then(() => loadDetail(selectedId.value))
    }
  },
  { immediate: true }
)

onMounted(() => {
  if (props.active && !loaded.value) {
    loaded.value = true
    void loadList().then(() => loadDetail(selectedId.value))
  }
})

onUnmounted(() => {
  stopWatch()
})

</script>

<style lang="scss" scoped>
.customer-news {
  display: flex;
  flex-direction: column;
  min-height: 420px;
  height: calc(100vh - 220px);
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 12px;
  overflow: hidden;
}

.customer-news__toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 10px 16px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.customer-news__hint {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.customer-news__actions {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  gap: 8px;
}

.customer-news__delete,
.customer-news__fetch {
  flex-shrink: 0;
}

.customer-news__body {
  display: flex;
  flex: 1;
  min-height: 0;
}

.customer-news__dates {
  flex: 0 0 188px;
  overflow: auto;
  padding: 10px 8px;
  border-right: 1px solid var(--el-border-color-lighter);
}

.customer-news__date {
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

.customer-news__date-main {
  font-size: 13px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.customer-news__date-sub {
  font-size: 11px;
  color: var(--el-text-color-secondary);
}

.customer-news__content {
  flex: 1;
  min-width: 0;
  min-height: 0;
  display: flex;
  overflow: hidden;
  padding: 16px 12px 16px 16px;
}

.customer-news__empty {
  margin: 16px;
  font-size: 13px;
  color: var(--el-text-color-secondary);
}
</style>
