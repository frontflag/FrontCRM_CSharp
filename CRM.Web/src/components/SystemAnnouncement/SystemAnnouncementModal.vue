<template>
  <el-dialog
    v-model="visible"
    class="sys-announcement-modal"
    width="640px"
    align-center
    :title="current?.title || t('sysAnnouncement.modalTitle')"
    :close-on-click-modal="!isForce"
    :close-on-press-escape="!isForce"
    :show-close="canClose"
    :before-close="onBeforeClose"
    @opened="onOpened"
  >
    <div v-if="current" class="ann-meta">
      <el-tag size="small" effect="plain">{{ typeLabel(current.type) }}</el-tag>
      <span v-if="current.publishedAt" class="ann-date">{{ formatDate(current.publishedAt) }}</span>
      <span v-if="isForce && total > 1" class="ann-pager-hint">
        {{ t('sysAnnouncement.forceProgress', { current: index + 1, total }) }}
      </span>
    </div>

    <div ref="bodyRef" class="ann-body markdown-body" v-html="bodyHtml" />

    <div v-if="isForce && remainingBeyondCap > 0" class="ann-more-tip">
      {{ t('sysAnnouncement.moreUnread', { count: remainingBeyondCap }) }}
      <button type="button" class="ann-link" @click="goHistory">
        {{ t('sysAnnouncement.goHistory') }}
      </button>
    </div>

    <div v-else-if="isForce" class="ann-history-link">
      <button type="button" class="ann-link" @click="goHistory">
        {{ t('sysAnnouncement.goHistory') }}
      </button>
    </div>

    <template #footer>
      <div class="ann-footer">
        <el-button v-if="showPager" :disabled="index <= 0" @click="goPrev">
          {{ t('sysAnnouncement.prev') }}
        </el-button>
        <el-button v-if="showPager" :disabled="index >= items.length - 1" type="primary" @click="goNext">
          {{ t('sysAnnouncement.next') }}
        </el-button>
        <el-button v-if="canClose" type="primary" @click="close">
          {{ t('sysAnnouncement.close') }}
        </el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { AnnouncementDetail } from '@/api/sysAnnouncements'
import { sysAnnouncementsApi } from '@/api/sysAnnouncements'
import {
  renderAnnouncementMarkdown,
  resolveAnnouncementDocumentImages,
  revokeObjectUrls
} from '@/utils/sanitizeAnnouncementHtml'
import { useSystemAnnouncementUi } from '@/composables/useSystemAnnouncementUi'
import { formatDisplayDate } from '@/utils/displayDateTime'

const props = withDefaults(
  defineProps<{
    modelValue: boolean
    /** force=强制未读队列；single=历史单条；preview=管理预览（不写已读） */
    mode: 'force' | 'single' | 'preview'
    items?: AnnouncementDetail[]
    /** force 模式下未读总数（含超出 5 条的） */
    totalUnread?: number
    /** single 模式下已读公告传 false，避免重复调已读接口 */
    recordRead?: boolean
  }>(),
  {
    items: () => [],
    totalUnread: 0,
    recordRead: true
  }
)

const emit = defineEmits<{
  'update:modelValue': [boolean]
  closed: []
  read: []
}>()

const { t } = useI18n()
const { openMessageDrawer, setUnreadCount, unreadCount } = useSystemAnnouncementUi()

const visible = computed({
  get: () => props.modelValue,
  set: (v: boolean) => emit('update:modelValue', v)
})

const index = ref(0)
const bodyRef = ref<HTMLElement | null>(null)
const blobUrls = ref<string[]>([])
const markedReadIds = ref<Set<string>>(new Set())

const items = computed(() => props.items || [])
const current = computed(() => items.value[index.value] || null)
const isForce = computed(() => props.mode === 'force')
const showPager = computed(() => isForce.value && items.value.length > 1)
const total = computed(() => items.value.length)
const remainingBeyondCap = computed(() =>
  isForce.value ? Math.max(0, (props.totalUnread || 0) - items.value.length) : 0
)

/** 强制模式：当前批次全部展示过才可关 */
const canClose = computed(() => {
  if (props.mode === 'preview' || props.mode === 'single') return true
  if (!isForce.value) return true
  if (items.value.length === 0) return true
  return items.value.every((x) => markedReadIds.value.has(x.id))
})

const bodyHtml = computed(() => renderAnnouncementMarkdown(current.value?.bodyMd || ''))

function typeLabel(type: string) {
  if (type === 'version_update') return t('sysAnnouncement.typeVersionUpdate')
  return t('sysAnnouncement.typePlatformNotice')
}

function formatDate(v?: string | null) {
  if (!v) return ''
  return formatDisplayDate(v)
}

async function markCurrentRead() {
  const cur = current.value
  if (!cur?.id) return
  if (props.mode === 'preview') {
    markedReadIds.value = new Set([...markedReadIds.value, cur.id])
    return
  }
  if (markedReadIds.value.has(cur.id)) return

  if (props.recordRead !== false) {
    try {
      await sysAnnouncementsApi.markRead(cur.id)
    } catch {
      /* 仍视为本地已展示，避免卡死关窗 */
    }
  }
  markedReadIds.value = new Set([...markedReadIds.value, cur.id])
  if (isForce.value && props.recordRead !== false) {
    setUnreadCount(Math.max(0, unreadCount.value - 1))
  }
  emit('read')
}

async function hydrateImages() {
  revokeObjectUrls(blobUrls.value)
  blobUrls.value = []
  await nextTick()
  blobUrls.value = await resolveAnnouncementDocumentImages(bodyRef.value)
}

async function onOpened() {
  await markCurrentRead()
  await hydrateImages()
}

watch(
  () => [props.modelValue, index.value, current.value?.id] as const,
  async ([open]) => {
    if (!open) return
    await markCurrentRead()
    await hydrateImages()
  }
)

watch(
  () => props.modelValue,
  (open) => {
    if (open) {
      index.value = 0
      if (props.mode === 'force') markedReadIds.value = new Set()
    }
  }
)

function goPrev() {
  if (index.value > 0) index.value -= 1
}

function goNext() {
  if (index.value < items.value.length - 1) index.value += 1
}

function goHistory() {
  openMessageDrawer('announcements')
}

function close() {
  if (!canClose.value) return
  visible.value = false
  emit('closed')
}

function onBeforeClose(done: (cancel?: boolean) => void) {
  if (!canClose.value) {
    done(true)
    return
  }
  done()
  emit('closed')
}

onBeforeUnmount(() => revokeObjectUrls(blobUrls.value))
</script>

<style lang="scss" scoped>
.ann-meta {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 12px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.ann-body {
  max-height: 50vh;
  overflow: auto;
  padding: 4px 2px 12px;
  line-height: 1.6;
  font-size: 14px;

  :deep(img) {
    max-width: 100%;
    height: auto;
  }

  :deep(a) {
    color: var(--el-color-primary);
  }
}

.ann-more-tip,
.ann-history-link {
  margin-top: 8px;
  font-size: 13px;
  color: var(--el-text-color-regular);
}

.ann-link {
  border: none;
  background: none;
  color: var(--el-color-primary);
  cursor: pointer;
  padding: 0 4px;
  font-size: inherit;
}

.ann-footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
</style>
