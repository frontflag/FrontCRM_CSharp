<template>
  <section class="notice-panel" aria-labelledby="dashboard-notice-title">
    <header class="notice-panel__head">
      <h3 id="dashboard-notice-title" class="notice-panel__title">
        {{ t('dashboard.notices.title') }}
      </h3>
      <div class="notice-panel__actions">
        <button type="button" class="notice-panel__link" @click="openAll">
          {{ t('dashboard.notices.viewAll') }}
        </button>
        <button
          type="button"
          class="notice-panel__link"
          :disabled="!hasUnread || marking"
          @click="markAllRead"
        >
          {{ t('dashboard.notices.markAllRead') }}
        </button>
      </div>
    </header>

    <div v-loading="loading" class="notice-panel__list">
      <p v-if="!loading && items.length === 0" class="notice-panel__empty">
        {{ t('dashboard.notices.empty') }}
      </p>
      <button
        v-for="row in items"
        :key="row.key"
        type="button"
        class="notice-row"
        :class="{ 'is-read': row.isRead }"
        @click="openRow(row)"
      >
        <span class="notice-row__dot" :class="dotClass(row)" aria-hidden="true" />
        <div class="notice-row__body">
          <div class="notice-row__line1">
            <span class="notice-row__title">{{ row.title }}</span>
            <span class="notice-row__time">{{ formatTime(row.at) }}</span>
          </div>
          <p class="notice-row__preview">{{ row.preview }}</p>
        </div>
      </button>
    </div>

    <SystemAnnouncementModal
      v-model="detailOpen"
      mode="single"
      :items="detailItems"
      :record-read="detailRecordRead"
      @read="onAnnouncementRead"
    />

    <el-dialog
      v-model="noticeDetailOpen"
      :title="t('sysUserNotice.detailTitle')"
      width="560px"
      append-to-body
      destroy-on-close
    >
      <div v-if="noticeDetail" class="notice-detail">
        <div class="notice-detail__head">
          <h3 class="notice-detail__title">
            <span v-if="noticeDetail.isUrgent" class="urgent-icon" aria-hidden="true">
              <NoticeUrgentIcon />
            </span>
            <span>{{ noticeDetail.title }}</span>
          </h3>
          <span class="notice-detail__date">{{ formatDateTime(noticeDetail.createTime) }}</span>
        </div>
        <pre v-if="(noticeDetail.body || '').trim()" class="notice-detail__body">{{ noticeDetail.body }}</pre>
        <UserNoticeImageGallery :images="noticeDetail.images || []" :thumb-size="96" />
      </div>
    </el-dialog>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import {
  sysAnnouncementsApi,
  type AnnouncementDetail,
  type AnnouncementHistoryItem
} from '@/api/sysAnnouncements'
import {
  sysUserNoticesApi,
  type UserNoticeDetail,
  type UserNoticeMeListItem
} from '@/api/sysUserNotices'
import SystemAnnouncementModal from '@/components/SystemAnnouncement/SystemAnnouncementModal.vue'
import NoticeUrgentIcon from '@/components/SystemAnnouncement/NoticeUrgentIcon.vue'
import UserNoticeImageGallery from '@/components/SystemAnnouncement/UserNoticeImageGallery.vue'
import { useSystemAnnouncementUi } from '@/composables/useSystemAnnouncementUi'
import { formatDisplayDateTime, formatDisplayRelativeNoticeTime } from '@/utils/displayDateTime'
import { getApiErrorMessage } from '@/utils/apiError'

const LIMIT = 5

type FeedKind = 'notice' | 'announcement'

interface FeedItem {
  key: string
  kind: FeedKind
  id: string
  title: string
  preview: string
  at: string
  isRead: boolean
  isUrgent: boolean
}

const { t } = useI18n()
const {
  unreadCount,
  noticeUnreadCount,
  openMessageDrawer,
  setUnreadCount,
  setNoticeUnreadSummary
} = useSystemAnnouncementUi()

const loading = ref(false)
const marking = ref(false)
const items = ref<FeedItem[]>([])

const detailOpen = ref(false)
const detailItems = ref<AnnouncementDetail[]>([])
const detailRecordRead = ref(true)
const noticeDetailOpen = ref(false)
const noticeDetail = ref<UserNoticeDetail | null>(null)

const hasUnread = computed(() => noticeUnreadCount.value > 0 || unreadCount.value > 0)

function formatTime(v?: string | null) {
  return formatDisplayRelativeNoticeTime(v, {
    today: t('sysUserNotice.dateTodayPrefix'),
    yesterday: t('sysUserNotice.dateYesterdayPrefix')
  })
}

function formatDateTime(v?: string | null) {
  if (!v) return '—'
  return formatDisplayDateTime(v)
}

function announcementTypeLabel(type: string) {
  if (type === 'version_update') return t('sysAnnouncement.typeVersionUpdate')
  return t('sysAnnouncement.typePlatformNotice')
}

function noticePreview(row: UserNoticeMeListItem) {
  const text = String(row.bodyPreview || '').trim()
  const n = Number(row.imageCount || 0)
  const img = n > 0 ? t('sysUserNotice.imageCount', { n }) : ''
  if (text && img) return `${text}  ${img}`
  return text || img || t('dashboard.notices.noPreview')
}

function toTime(v?: string | null) {
  const t0 = v ? Date.parse(v) : NaN
  return Number.isFinite(t0) ? t0 : 0
}

function dotClass(row: FeedItem) {
  if (!row.isRead && row.isUrgent) return 'is-urgent'
  if (!row.isRead) return 'is-unread'
  return 'is-read'
}

function mergeFeed(notices: UserNoticeMeListItem[], anns: AnnouncementHistoryItem[]): FeedItem[] {
  const rows: FeedItem[] = [
    ...notices.map((r) => ({
      key: `n-${r.id}`,
      kind: 'notice' as const,
      id: r.id,
      title: r.title,
      preview: noticePreview(r),
      at: r.createTime,
      isRead: r.isRead,
      isUrgent: r.isUrgent
    })),
    ...anns.map((r) => ({
      key: `a-${r.id}`,
      kind: 'announcement' as const,
      id: r.id,
      title: r.title,
      preview: announcementTypeLabel(r.type),
      at: r.publishedAt || '',
      isRead: r.isRead,
      isUrgent: false
    }))
  ]
  rows.sort((a, b) => toTime(b.at) - toTime(a.at))
  return rows.slice(0, LIMIT)
}

async function refreshBadges(notices: UserNoticeMeListItem[], anns: AnnouncementHistoryItem[]) {
  const unreadNotices = notices.filter((r) => !r.isRead)
  setNoticeUnreadSummary(unreadNotices.length, unreadNotices.some((r) => r.isUrgent))
  setUnreadCount(anns.filter((r) => !r.isRead).length)
}

async function load() {
  loading.value = true
  try {
    const [notices, anns] = await Promise.all([
      sysUserNoticesApi.mine(),
      sysAnnouncementsApi.history()
    ])
    items.value = mergeFeed(notices ?? [], anns ?? [])
    await refreshBadges(notices ?? [], anns ?? [])
  } catch {
    items.value = []
  } finally {
    loading.value = false
  }
}

function openAll() {
  openMessageDrawer(noticeUnreadCount.value > 0 ? 'messages' : 'announcements')
}

async function markAllRead() {
  if (!hasUnread.value) return
  marking.value = true
  try {
    await Promise.all([sysUserNoticesApi.markAllRead(), sysAnnouncementsApi.markAllRead()])
    ElMessage.success(t('sysUserNotice.markAllReadOk'))
    await load()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e) || t('sysUserNotice.markAllReadFailed'))
  } finally {
    marking.value = false
  }
}

async function openRow(row: FeedItem) {
  try {
    if (row.kind === 'notice') {
      const detail = await sysUserNoticesApi.getMine(row.id)
      noticeDetail.value = detail
      noticeDetailOpen.value = true
      await sysUserNoticesApi.markRead(row.id)
      await load()
      return
    }
    const detail = await sysAnnouncementsApi.getPublished(row.id)
    detailItems.value = [detail]
    detailRecordRead.value = !row.isRead
    detailOpen.value = true
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e) || t('sysUserNotice.loadFailed'))
  }
}

function onAnnouncementRead() {
  void load()
}

onMounted(() => {
  void load()
})
</script>

<style lang="scss" scoped>
.notice-panel {
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 12px;
  padding: 16px;
}

.notice-panel__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 10px;
}

.notice-panel__title {
  margin: 0;
  font-size: 15px;
  font-weight: 600;
}

.notice-panel__actions {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-shrink: 0;
}

.notice-panel__link {
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

.notice-panel__list {
  min-height: 48px;
}

.notice-panel__empty {
  margin: 8px 0 0;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.notice-row {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  width: 100%;
  padding: 8px 0;
  border: 0;
  border-bottom: 1px solid var(--el-border-color-lighter);
  background: none;
  text-align: left;
  cursor: pointer;
  &:last-child {
    border-bottom: none;
    padding-bottom: 0;
  }
  &:hover .notice-row__title {
    color: var(--el-color-primary);
  }
  &.is-read {
    .notice-row__title,
    .notice-row__preview,
    .notice-row__time {
      color: var(--el-text-color-secondary);
      font-weight: 400;
    }
  }
}

.notice-row__dot {
  flex-shrink: 0;
  width: 8px;
  height: 8px;
  margin-top: 6px;
  border-radius: 50%;
  background: var(--el-border-color);
  &.is-unread {
    background: var(--el-color-primary);
  }
  &.is-urgent {
    background: var(--el-color-warning);
  }
  &.is-read {
    background: var(--el-border-color);
  }
}

.notice-row__body {
  min-width: 0;
  flex: 1;
}

.notice-row__line1 {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 8px;
}

.notice-row__title {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 13px;
  font-weight: 600;
}

.notice-row__time {
  flex-shrink: 0;
  font-size: 11px;
  color: var(--el-text-color-secondary);
}

.notice-row__preview {
  margin: 3px 0 0;
  overflow: hidden;
  display: -webkit-box;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
  font-size: 12px;
  line-height: 1.45;
  color: var(--el-text-color-secondary);
}

.notice-detail__head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 12px;
}

.notice-detail__title {
  display: flex;
  align-items: center;
  gap: 6px;
  margin: 0;
  font-size: 16px;
}

.notice-detail__date {
  flex-shrink: 0;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.notice-detail__body {
  margin: 0 0 12px;
  white-space: pre-wrap;
  font-family: inherit;
  font-size: 13px;
  line-height: 1.6;
}

.urgent-icon {
  display: inline-flex;
  color: var(--el-color-warning);
}
</style>
