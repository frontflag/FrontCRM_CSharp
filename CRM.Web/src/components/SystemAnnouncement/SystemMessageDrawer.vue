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
      <el-tab-pane :label="t('sysAnnouncement.tabMessages')" name="messages">
        <div class="empty-hint">{{ t('sysAnnouncement.messagesComingSoon') }}</div>
      </el-tab-pane>
      <el-tab-pane :label="t('sysAnnouncement.tabAnnouncements')" name="announcements">
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
      </el-tab-pane>
    </el-tabs>

    <SystemAnnouncementModal
      v-model="detailOpen"
      mode="single"
      :items="detailItems"
      :record-read="detailRecordRead"
      @read="onDetailRead"
    />
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
import SystemAnnouncementModal from '@/components/SystemAnnouncement/SystemAnnouncementModal.vue'
import { useSystemAnnouncementUi } from '@/composables/useSystemAnnouncementUi'
import { formatDisplayDate } from '@/utils/displayDateTime'
import { getApiErrorMessage } from '@/utils/apiError'

const { t } = useI18n()
const { messageDrawerOpen, messageDrawerTab, setUnreadCount } = useSystemAnnouncementUi()

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

function typeLabel(type: string) {
  if (type === 'version_update') return t('sysAnnouncement.typeVersionUpdate')
  return t('sysAnnouncement.typePlatformNotice')
}

function formatDate(v?: string | null) {
  if (!v) return '—'
  return formatDisplayDate(v)
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

watch(
  () => [visible.value, tab.value] as const,
  ([open, tname]) => {
    if (open && tname === 'announcements') void loadHistory()
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
</script>

<style lang="scss" scoped>
.sys-msg-tabs {
  height: 100%;
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
  min-height: 120px;
}

.ann-card {
  text-align: left;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  padding: 12px 14px;
  background: var(--el-bg-color);
  cursor: pointer;
  transition: border-color 0.15s, background 0.15s;

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
</style>
