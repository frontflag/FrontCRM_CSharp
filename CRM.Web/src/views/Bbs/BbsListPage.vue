<template>
  <div class="bbs-list-page">
    <div class="bbs-layout">
      <BbsCategoryAside :active-key="category" @select="selectCategory" />

      <main class="bbs-main">
        <div class="bbs-toolbar">
          <el-select
            v-model="statusFilter"
            clearable
            class="bbs-status"
            :placeholder="t('bbs.filters.status')"
            @change="search"
          >
            <el-option :label="t('bbs.status.open')" :value="BbsSubjectStatus.Open" />
            <el-option :label="t('bbs.status.close')" :value="BbsSubjectStatus.Close" />
          </el-select>
          <el-input
            v-model="keywordDraft"
            clearable
            class="bbs-search"
            :placeholder="t('bbs.filters.keywordPhAll')"
            @keyup.enter="search"
          >
            <template #prefix>
              <svg class="bbs-search-ico" viewBox="0 0 24 24" width="14" height="14" aria-hidden="true">
                <path
                  fill="currentColor"
                  d="M10 2a8 8 0 0 1 6.32 12.9l4.39 4.39-1.42 1.42-4.39-4.39A8 8 0 1 1 10 2zm0 2a6 6 0 1 0 0 12A6 6 0 0 0 10 4z"
                />
              </svg>
            </template>
            <template #append>
              <el-button class="bbs-search-btn" @click="search">{{ t('bbs.filters.search') }}</el-button>
            </template>
          </el-input>
          <el-button type="primary" class="bbs-create" @click="goCreate">{{ t('bbs.create') }}</el-button>
        </div>

        <div class="bbs-feed" v-loading="loading">

          <div v-if="!displayRows.length && !loading" class="bbs-empty">{{ t('bbs.empty') }}</div>
          <button
            v-for="row in displayRows"
            :key="row.id"
            type="button"
            class="bbs-row"
            @click="goDetail(row.id)"
          >
            <div class="bbs-row__type" :title="rowTypeLabel(row)">
              <span class="bbs-type-mark" :data-type="row.type">{{ rowTypeShort(row) }}</span>
            </div>
            <div class="bbs-row__body">
              <div class="bbs-row__title">
                <span v-if="row.isTop" class="bbs-badge bbs-badge--top">{{ t('bbs.badgeTop') }}</span>
                <span v-if="row.isHot" class="bbs-badge bbs-badge--hot">{{ t('bbs.badgeHot') }}</span>
                <span class="bbs-row__title-text">{{ row.title }}</span>
              </div>
              <div class="bbs-row__meta">
                <span>{{ row.authorDisplay }}</span>
                <span>·</span>
                <span>{{ formatTime(row.lastReplyTime || row.createTime) }}</span>
                <span>·</span>
                <span>{{ rowTypeLabel(row) }}</span>
                <span>·</span>
                <span>{{ t('bbs.metaViews', { n: row.viewCount }) }}</span>
                <template v-if="row.status === BbsSubjectStatus.Close">
                  <span>·</span>
                  <span class="bbs-badge bbs-badge--closed">{{ t('bbs.badgeClosed') }}</span>
                </template>
              </div>
            </div>
            <div class="bbs-row__stats">
              <div class="bbs-row__stat" :title="t('bbs.metaLikes', { n: row.likeCount || 0 })">
                <svg class="bbs-row__stat-ico" viewBox="0 0 24 24" aria-hidden="true">
                  <path
                    fill="currentColor"
                    d="M2 21h4V9H2v12zm20.1-10.9c-.4-.5-1-.8-1.6-.8h-5.5l.8-4c.1-.5 0-1-.3-1.4L14.9 2 8.4 8.5c-.4.4-.6.9-.6 1.5V19c0 1.1.9 2 2 2h8c.8 0 1.5-.5 1.8-1.2l3-7c.2-.5.1-1.1-.3-1.5z"
                  />
                </svg>
                <span>{{ row.likeCount || 0 }}</span>
              </div>
              <div class="bbs-row__stat" :title="t('bbs.metaDislikes', { n: row.dislikeCount || 0 })">
                <svg class="bbs-row__stat-ico" viewBox="0 0 24 24" aria-hidden="true">
                  <path
                    fill="currentColor"
                    d="M22 3h-4v12h4V3zM2.9 13.9c.4.5 1 .8 1.6.8h5.5l-.8 4c-.1.5 0 1 .3 1.4l.6 1.2 6.5-6.5c.4-.4.6-.9.6-1.5V5c0-1.1-.9-2-2-2H6c-.8 0-1.5.5-1.8 1.2l-3 7c-.2.5-.1 1.1.3 1.5z"
                  />
                </svg>
                <span>{{ row.dislikeCount || 0 }}</span>
              </div>
              <div class="bbs-row__stat" :title="t('bbs.metaReplies', { n: row.replyCount })">
                <svg class="bbs-row__stat-ico" viewBox="0 0 24 24" aria-hidden="true">
                  <path
                    fill="currentColor"
                    d="M4 4h16a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H9l-5 4v-4H4a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2z"
                  />
                </svg>
                <span>{{ row.replyCount }}</span>
              </div>
            </div>
          </button>

          <div v-if="showPager" class="bbs-pager">
            <el-pagination
              background
              layout="prev, pager, next, total"
              :total="total"
              :page-size="pageSize"
              :current-page="page"
              @current-change="onPage"
            />
          </div>
        </div>
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import {
  bbsApi,
  BbsSubjectStatus,
  BbsSubjectType,
  BbsSubjectTypeI18nKey,
  BbsSubjectTypeOptions,
  bbsIsAdminOnlyPostType,
  bbsIsCustomBoardType,
  type BbsSubjectListItem
} from '@/api/bbs'
import BbsCategoryAside, { type BbsCategoryKey } from '@/components/Bbs/BbsCategoryAside.vue'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { getApiErrorMessage } from '@/utils/apiError'
import { useAuthStore } from '@/stores/auth'

type CategoryKey = BbsCategoryKey

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const topItems = ref<BbsSubjectListItem[]>([])
const items = ref<BbsSubjectListItem[]>([])
const loading = ref(false)
const keywordDraft = ref('')
const keyword = ref('')
const category = ref<CategoryKey>('all')
const statusFilter = ref<number | undefined>()
const page = ref(1)
const pageSize = 20
const total = ref(0)

const TYPE_SHORT: Record<number, string> = {
  [BbsSubjectType.CompanyNotice]: '通',
  [BbsSubjectType.IndustryNews]: '讯',
  [BbsSubjectType.Share]: '享',
  [BbsSubjectType.OpsGuide]: '说',
  [BbsSubjectType.Suggestion]: '建',
  [BbsSubjectType.SystemUpdate]: '更'
}

function typeLabel(type: number) {
  const key = BbsSubjectTypeI18nKey[type]
  return key ? t(key) : String(type)
}

function rowTypeLabel(row: BbsSubjectListItem) {
  return (row.typeLabel || '').trim() || typeLabel(row.type)
}

function rowTypeShort(row: BbsSubjectListItem) {
  if (TYPE_SHORT[row.type]) return TYPE_SHORT[row.type]
  const label = rowTypeLabel(row)
  return (label && label[0]) || '板'
}

function formatTime(v: string | null | undefined) {
  return v ? formatDisplayDateTime(v) : '—'
}

function matchKeyword(row: BbsSubjectListItem) {
  const q = keyword.value.trim().toLowerCase()
  if (!q) return true
  return (row.title ?? '').toLowerCase().includes(q)
}

function matchStatus(row: BbsSubjectListItem) {
  if (statusFilter.value == null) return true
  return row.status === statusFilter.value
}

const filteredTops = computed(() =>
  topItems.value.filter((r) => matchKeyword(r) && matchStatus(r))
)

const displayRows = computed((): BbsSubjectListItem[] => {
  if (category.value === 'top') return filteredTops.value
  if (category.value === 'all' && page.value === 1) {
    const topIds = new Set(filteredTops.value.map((r) => r.id))
    return [...filteredTops.value, ...items.value.filter((r) => !topIds.has(r.id))]
  }
  return items.value
})

const showPager = computed(() => category.value !== 'top' && total.value > pageSize)

function goCreate() {
  const q: Record<string, string> = {}
  if (typeof category.value === 'number') {
    const isAdmin = authStore.user?.isSysAdmin === true
    if (isAdmin || !bbsIsAdminOnlyPostType(category.value)) {
      q.type = String(category.value)
    }
  }
  router.push({ name: 'BbsCreate', query: q })
}

function goDetail(id: string) {
  router.push({ name: 'BbsDetail', params: { id } })
}

async function loadTop() {
  try {
    topItems.value = (await bbsApi.getTop()) || []
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.loadFailed')))
  }
}

async function loadList() {
  if (category.value === 'top') {
    loading.value = true
    try {
      await loadTop()
    } finally {
      loading.value = false
    }
    return
  }

  loading.value = true
  try {
    if (category.value === 'all') await loadTop()
    const type = typeof category.value === 'number' ? category.value : null
    const data = await bbsApi.list({
      keyword: keyword.value.trim() || undefined,
      type,
      status: statusFilter.value ?? null,
      page: page.value,
      pageSize
    })
    items.value = data?.items || []
    total.value = data?.total || 0
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bbs.loadFailed')))
  } finally {
    loading.value = false
  }
}

function selectCategory(key: CategoryKey) {
  const same =
    (key === 'all' && !route.query.cat && route.query.type == null) ||
    (key === 'top' && route.query.cat === 'top') ||
    (typeof key === 'number' && String(route.query.type) === String(key) && route.query.cat == null)
  if (same) return

  if (key === 'all') {
    void router.replace({ name: 'BbsList', query: {} })
  } else if (key === 'top') {
    void router.replace({ name: 'BbsList', query: { cat: 'top' } })
  } else {
    void router.replace({ name: 'BbsList', query: { type: String(key) } })
  }
}

function applyCategoryFromRoute() {
  const cat = String(route.query.cat || '')
  const typeRaw = Number(route.query.type)
  let next: CategoryKey = 'all'
  if (cat === 'top') next = 'top'
  else if (
    Number.isFinite(typeRaw) &&
    (BbsSubjectTypeOptions.includes(typeRaw as (typeof BbsSubjectTypeOptions)[number]) ||
      bbsIsCustomBoardType(typeRaw))
  ) {
    next = typeRaw
  }
  category.value = next
  page.value = 1
  void loadList()
}

function search() {
  keyword.value = keywordDraft.value
  page.value = 1
  void loadList()
}

function onPage(p: number) {
  page.value = p
  void loadList()
}

watch(
  () => [route.query.cat, route.query.type] as const,
  () => {
    applyCategoryFromRoute()
  }
)

onMounted(() => {
  applyCategoryFromRoute()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.bbs-list-page {
  padding: 16px 20px 32px;
  min-height: 100%;
}

.bbs-layout {
  display: grid;
  grid-template-columns: 268px minmax(0, 1fr);
  gap: 16px;
  align-items: start;
}

.bbs-row__type {
  flex: 0 0 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.06);
}

.bbs-type-mark {
  font-size: 13px;
  font-weight: 700;
  color: $cyan-primary;
}

.bbs-main {
  min-width: 0;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}

.bbs-toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
  padding: 12px 14px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
}

.bbs-search {
  flex: 0 0 50%;
  width: 50%;
  min-width: 0;
  max-width: 50%;
}

.bbs-search :deep(.el-input-group__append) {
  padding: 0;
  background: transparent;
  border: none;
  box-shadow: none;
}

.bbs-search-btn {
  margin: 0;
  height: 100%;
  border-radius: 0 var(--el-border-radius-base) var(--el-border-radius-base) 0;
  padding: 0 14px;
}

.bbs-search-ico {
  color: $text-muted;
  font-size: 14px;
}

.bbs-status {
  width: 120px;
}

.bbs-create {
  margin-left: auto;
  width: 8em;
  padding-left: 0;
  padding-right: 0;
  text-align: center;
}

.bbs-row {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  width: 100%;
  margin: 0;
  padding: 14px 16px;
  border: none;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: transparent;
  color: inherit;
  text-align: left;
  cursor: pointer;
  transition: background 0.12s ease;

  &:hover {
    background: rgba(0, 212, 255, 0.05);
  }

  &:last-of-type {
    border-bottom: none;
  }
}

.bbs-row__body {
  flex: 1 1 auto;
  min-width: 0;
}

.bbs-row__title {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px;
  margin-bottom: 4px;
}

.bbs-row__title-text {
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
  line-height: 1.4;
  word-break: break-word;
}

.bbs-badge {
  flex-shrink: 0;
  padding: 1px 6px;
  border-radius: 4px;
  font-size: 11px;
  font-weight: 600;
  line-height: 1.4;

  &--top {
    color: #fbbf24;
    background: rgba(251, 191, 36, 0.12);
  }

  &--hot {
    color: #f87171;
    background: rgba(248, 113, 113, 0.12);
  }

  &--closed {
    color: $text-muted;
    background: rgba(148, 163, 184, 0.18);
    border: 1px solid rgba(148, 163, 184, 0.28);
  }
}

.bbs-row__meta {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.5;
}

.bbs-row__stats {
  flex: 0 0 auto;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  margin-top: 6px;
}

.bbs-row__stat {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  padding: 2px 7px;
  border-radius: 999px;
  font-size: 12px;
  color: $text-secondary;
  background: rgba(255, 255, 255, 0.04);
}

.bbs-row__stat-ico {
  width: 13px;
  height: 13px;
  opacity: 0.75;
}

.bbs-empty {
  padding: 48px 16px;
  text-align: center;
  color: $text-muted;
  font-size: 13px;
}

.bbs-pager {
  display: flex;
  justify-content: flex-end;
  padding: 12px 16px 16px;
}

@media (max-width: 900px) {
  .bbs-layout {
    grid-template-columns: 1fr;
  }
}
</style>
