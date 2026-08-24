<template>
  <el-drawer
    v-model="visible"
    :title="t('sysAnnouncement.drawerTitle')"
    direction="rtl"
    size="420px"
    class="sys-message-drawer"
    :close-on-click-modal="true"
  >
    <el-tabs v-model="tab" class="sys-msg-tabs">
      <el-tab-pane name="messages">
        <template #label>
          <span class="tab-label">
            {{ t('sysAnnouncement.tabMessages') }}
            <span v-if="noticeUnreadCount > 0" class="tab-unread">{{ tabBadgeText(noticeUnreadCount) }}</span>
          </span>
        </template>
        <div class="tab-pane-body">
          <div v-loading="noticeLoading" class="ann-list">
            <div v-if="!noticeLoading && noticeRows.length === 0" class="empty-hint">
              {{ t('sysUserNotice.historyEmpty') }}
            </div>
            <button
              v-for="row in noticeRows"
              :key="row.id"
              type="button"
              class="notice-card"
              :class="{ 'is-read': row.isRead }"
              @click="openNotice(row)"
            >
              <div class="notice-card__row1">
                <div class="notice-card__title">
                  <span v-if="row.isUrgent" class="urgent-icon" :title="t('sysUserNotice.urgentYes')" aria-hidden="true">
                    <NoticeUrgentIcon />
                  </span>
                  <span class="notice-card__title-text">{{ row.title }}</span>
                </div>
                <span class="notice-card__date">{{ formatNoticeTime(row.createTime) }}</span>
              </div>
              <div class="notice-card__preview">{{ noticePreviewLine(row) }}</div>
            </button>
          </div>
          <div class="tab-footer">
            <el-button
              :disabled="noticeUnreadCount <= 0"
              :loading="markingNotices"
              @click="markAllNoticesRead"
            >
              {{ t('sysUserNotice.markAllRead') }}
            </el-button>
          </div>
        </div>
      </el-tab-pane>
      <el-tab-pane name="announcements">
        <template #label>
          <span class="tab-label">
            {{ t('sysAnnouncement.tabAnnouncements') }}
            <span v-if="announcementUnreadCount > 0" class="tab-unread">{{ tabBadgeText(announcementUnreadCount) }}</span>
          </span>
        </template>
        <div class="tab-pane-body">
          <div v-loading="loading" class="ann-list">
            <div v-if="!loading && rows.length === 0" class="empty-hint">
              {{ t('sysAnnouncement.historyEmpty') }}
            </div>
            <button
              v-for="row in rows"
              :key="row.id"
              type="button"
              class="ann-card"
              @click="openRow(row)"
            >
              <div class="ann-card__title">{{ row.title }}</div>
              <div class="ann-card__meta">
                <el-tag size="small" effect="plain">{{ typeLabel(row.type) }}</el-tag>
                <span>{{ formatDate(row.publishedAt) }}</span>
                <el-tag :type="row.isRead ? 'info' : 'warning'" size="small" effect="plain">
                  {{ row.isRead ? t('sysAnnouncement.read') : t('sysAnnouncement.unread') }}
                </el-tag>
              </div>
            </button>
          </div>
          <div class="tab-footer">
            <el-button
              :disabled="announcementUnreadCount <= 0"
              :loading="markingAnnouncements"
              @click="markAllAnnouncementsRead"
            >
              {{ t('sysUserNotice.markAllRead') }}
            </el-button>
          </div>
        </div>
      </el-tab-pane>
    </el-tabs>

    <SystemAnnouncementModal
      v-model="detailOpen"
      mode="single"
      :items="detailItems"
      :record-read="detailRecordRead"
      @read="onDetailRead"
    />

    <el-dialog v-model="noticeDetailOpen" :title="t('sysUserNotice.detailTitle')" width="560px" append-to-body destroy-on-close>
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
  </el-drawer>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
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
import { formatDisplayDate, formatDisplayDateTime, formatDisplayRelativeNoticeTime } from '@/utils/displayDateTime'
import { getApiErrorMessage } from '@/utils/apiError'

const { t } = useI18n()
const {
  messageDrawerOpen,
  messageDrawerTab,
  unreadCount: announcementUnreadCount,
  noticeUnreadCount,
  setUnreadCount,
  setNoticeUnreadSummary
} = useSystemAnnouncementUi()

const visible = computed({
  get: () => messageDrawerOpen.value,
  set: (v: boolean) => {
    messageDrawerOpen.value = v
  }
})

const tab = computed({
  get: () => messageDrawerTab.value,
  set: (v: 'messages' | 'announcements') => {
    messageDrawerTab.value = v
  }
})

const loading = ref(false)
const rows = ref<AnnouncementHistoryItem[]>([])
const detailOpen = ref(false)
const detailItems = ref<AnnouncementDetail[]>([])
const detailRecordRead = ref(true)

const noticeLoading = ref(false)
const noticeRows = ref<UserNoticeMeListItem[]>([])
const noticeDetailOpen = ref(false)
const noticeDetail = ref<UserNoticeDetail | null>(null)
const markingNotices = ref(false)
const markingAnnouncements = ref(false)

function typeLabel(type: string) {
  if (type === 'version_update') return t('sysAnnouncement.typeVersionUpdate')
  return t('sysAnnouncement.typePlatformNotice')
}

function formatDate(v?: string | null) {
  if (!v) return '—'
  return formatDisplayDate(v)
}

function formatDateTime(v?: string | null) {
  if (!v) return '—'
  return formatDisplayDateTime(v)
}

function formatNoticeTime(v?: string | null) {
  return formatDisplayRelativeNoticeTime(v, {
    today: t('sysUserNotice.dateTodayPrefix'),
    yesterday: t('sysUserNotice.dateYesterdayPrefix')
  })
}

function noticePreviewLine(row: UserNoticeMeListItem) {
  const text = String(row.bodyPreview || '').trim()
  const n = Number(row.imageCount || 0)
  const img = n > 0 ? t('sysUserNotice.imageCount', { n }) : ''
  if (text && img) return `${text}  ${img}`
  return text || img || '—'
}

function tabBadgeText(n: number) {
  return n > 20 ? '20+' : String(n)
}

async function refreshTabBadges() {
  try {
    const [ann, notice] = await Promise.all([
      sysAnnouncementsApi.unreadSummary(),
      sysUserNoticesApi.unreadSummary()
    ])
    setUnreadCount(Number(ann?.totalUnread || 0))
    setNoticeUnreadSummary(Number(notice?.unreadCount || 0), !!notice?.hasUnreadUrgent)
  } catch {
    /* 角标失败不挡抽屉 */
  }
}

function applyNoticeSummary(list: UserNoticeMeListItem[]) {
  const unread = list.filter((r) => !r.isRead)
  setNoticeUnreadSummary(unread.length, unread.some((r) => r.isUrgent))
}

async function loadHistory() {
  loading.value = true
  try {
    rows.value = await sysAnnouncementsApi.history()
    const unread = rows.value.filter((r) => !r.isRead).length
    setUnreadCount(unread)
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e) || t('sysAnnouncement.loadFailed'))
  } finally {
    loading.value = false
  }
}

async function loadNotices() {
  noticeLoading.value = true
  try {
    noticeRows.value = await sysUserNoticesApi.mine()
    applyNoticeSummary(noticeRows.value)
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e) || t('sysUserNotice.loadFailed'))
  } finally {
    noticeLoading.value = false
  }
}

watch(
  () => [visible.value, tab.value] as const,
  ([open, tname]) => {
    if (!open) return
    void refreshTabBadges()
    if (tname === 'announcements') void loadHistory()
    if (tname === 'messages') void loadNotices()
  },
  { immediate: true }
)

async function openRow(row: AnnouncementHistoryItem) {
  try {
    const detail = await sysAnnouncementsApi.getPublished(row.id)
    detailItems.value = [detail]
    detailRecordRead.value = !row.isRead
    detailOpen.value = true
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e) || t('sysAnnouncement.loadFailed'))
  }
}

function onDetailRead() {
  void loadHistory()
}

async function markAllNoticesRead() {
  if (noticeUnreadCount.value <= 0) return
  markingNotices.value = true
  try {
    await sysUserNoticesApi.markAllRead()
    ElMessage.success(t('sysUserNotice.markAllReadOk'))
    await loadNotices()
    await refreshTabBadges()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e) || t('sysUserNotice.markAllReadFailed'))
  } finally {
    markingNotices.value = false
  }
}

async function markAllAnnouncementsRead() {
  if (announcementUnreadCount.value <= 0) return
  markingAnnouncements.value = true
  try {
    await sysAnnouncementsApi.markAllRead()
    ElMessage.success(t('sysUserNotice.markAllReadOk'))
    await loadHistory()
    await refreshTabBadges()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e) || t('sysUserNotice.markAllReadFailed'))
  } finally {
    markingAnnouncements.value = false
  }
}

async function openNotice(row: UserNoticeMeListItem) {
  try {
    const detail = await sysUserNoticesApi.getMine(row.id)
    noticeDetail.value = detail
    noticeDetailOpen.value = true
    await sysUserNoticesApi.markRead(row.id)
    void loadNotices()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e) || t('sysUserNotice.loadFailed'))
  }
}
</script>

<style lang="scss" scoped>
.sys-msg-tabs {
  height: 100%;
  display: flex;
  flex-direction: column;

  :deep(.el-tabs__content) {
    flex: 1;
    overflow: hidden;
  }

  :deep(.el-tab-pane) {
    height: 100%;
  }
}

.tab-pane-body {
  height: 100%;
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.tab-footer {
  flex-shrink: 0;
  padding: 12px 0 4px;
  display: flex;
  justify-content: center;

  .el-button {
    min-width: 160px;
  }
}

.tab-label {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.tab-unread {
  min-width: 16px;
  height: 16px;
  padding: 0 4px;
  border-radius: 8px;
  background: var(--crm-color-red-brown, #c45c4a);
  color: #fff;
  font-size: 10px;
  font-weight: 700;
  line-height: 16px;
  text-align: center;
}

.empty-hint {
  padding: 32px 12px;
  text-align: center;
  color: var(--el-text-color-secondary);
  font-size: 13px;
}

.ann-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
  min-height: 0;
  flex: 1;
  overflow: auto;
}

.ann-card {
  text-align: left;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  padding: 12px 14px;
  background: var(--el-bg-color);
  cursor: pointer;
  transition: border-color 0.15s, background 0.15s;
  width: 100%;

  &:hover {
    border-color: var(--el-color-primary-light-5);
    background: var(--el-fill-color-light);
  }

  &__title {
    font-size: 14px;
    font-weight: 600;
    color: var(--el-text-color-primary);
    margin-bottom: 8px;
    word-break: break-word;
  }

  &__meta {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 8px;
    font-size: 12px;
    color: var(--el-text-color-secondary);
  }
}

.notice-card {
  display: block;
  text-align: left;
  font: inherit;
  color: inherit;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  padding: 12px 14px;
  background: var(--el-bg-color);
  cursor: pointer;
  transition: border-color 0.15s, background 0.15s;
  width: 100%;

  &:hover {
    border-color: #f0d78c;
    background: #fff6d4;
  }

  &__row1 {
    display: flex;
    align-items: flex-start;
    gap: 10px;
    margin-bottom: 6px;
  }

  &__title {
    display: flex;
    align-items: flex-start;
    gap: 6px;
    min-width: 0;
    flex: 1;
    font-size: 14px;
    font-weight: 600;
    color: var(--el-text-color-primary);
  }

  &__title-text {
    min-width: 0;
    word-break: break-word;
  }

  &__date {
    flex-shrink: 0;
    margin-left: auto;
    font-size: 12px;
    font-weight: 400;
    color: var(--el-text-color-secondary);
    white-space: nowrap;
    padding-top: 2px;
  }

  &__preview {
    font-size: 12px;
    line-height: 1.5;
    color: var(--el-text-color-regular);
    word-break: break-word;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }

  &.is-read {
    .notice-card__title,
    .notice-card__title-text,
    .notice-card__date,
    .notice-card__preview,
    .urgent-icon {
      color: var(--el-text-color-secondary);
    }
  }
}

.urgent-icon {
  display: inline-flex;
  align-items: center;
  color: #e11d48;
  background: none;
  flex-shrink: 0;
  margin-top: 2px;
}

.notice-detail__head {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  margin-bottom: 12px;
}

.notice-detail__title {
  display: flex;
  align-items: flex-start;
  gap: 6px;
  margin: 0;
  min-width: 0;
  flex: 1;
  font-size: 16px;
  word-break: break-word;
}

.notice-detail__date {
  flex-shrink: 0;
  margin-left: auto;
  padding-top: 3px;
  font-size: 12px;
  font-weight: 400;
  color: var(--el-text-color-secondary);
  white-space: nowrap;
}

.notice-detail__body {
  margin: 0;
  white-space: pre-wrap;
  word-break: break-word;
  font-family: inherit;
  font-size: 14px;
  line-height: 1.6;
}
</style>
