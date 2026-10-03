<template>
  <section
    v-if="items.length"
    class="sys-notices"
    aria-labelledby="dashboard-sys-notice-title"
  >
    <h3 id="dashboard-sys-notice-title" class="sys-notices__title">
      {{ t('dashboard.systemNotices.title') }}
    </h3>
    <ul class="sys-notices__list">
      <li v-for="row in items" :key="`${row.kind}:${row.id}`">
        <button type="button" class="sys-notices__row" @click="openRow(row)">
          <span class="sys-notices__name">{{ row.title }}</span>
          <span class="sys-notices__date">{{ formatDisplayDateTime(row.at) }}</span>
        </button>
      </li>
    </ul>
  </section>

  <SystemAnnouncementModal
    v-model="detailOpen"
    mode="single"
    :items="detailItems"
    :record-read="true"
    @read="onAnnouncementRead"
  />
</template>

<script setup lang="ts">
import { onActivated, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import {
  sysAnnouncementsApi,
  type AnnouncementDetail,
  type DashboardNoticeItem
} from '@/api/sysAnnouncements'
import SystemAnnouncementModal from '@/components/SystemAnnouncement/SystemAnnouncementModal.vue'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { getApiErrorMessage } from '@/utils/apiError'

const { t } = useI18n()
const router = useRouter()

const items = ref<DashboardNoticeItem[]>([])
const detailOpen = ref(false)
const detailItems = ref<AnnouncementDetail[]>([])
const openingId = ref('')
const pendingAnnouncementId = ref('')

async function load() {
  try {
    const list = await sysAnnouncementsApi.dashboardNotices()
    items.value = Array.isArray(list) ? list : []
  } catch {
    items.value = []
  }
}

function drop(kind: string, id: string) {
  items.value = items.value.filter((x) => !(x.kind === kind && x.id === id))
}

async function openRow(row: DashboardNoticeItem) {
  if (openingId.value) return
  openingId.value = row.id
  try {
    if (row.kind === 'bbs') {
      const res = await sysAnnouncementsApi.dismissBbsNotice(row.id)
      if (res?.recorded !== false) drop('bbs', row.id)
      await router.push({ name: 'BbsDetail', params: { id: row.id } })
      return
    }
    const detail = await sysAnnouncementsApi.getPublished(row.id)
    pendingAnnouncementId.value = row.id
    detailItems.value = [detail]
    detailOpen.value = true
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('dashboard.systemNotices.openFailed')))
  } finally {
    openingId.value = ''
  }
}

function onAnnouncementRead() {
  if (pendingAnnouncementId.value) drop('announcement', pendingAnnouncementId.value)
  pendingAnnouncementId.value = ''
}

onMounted(() => {
  void load()
})

onActivated(() => {
  void load()
})
</script>

<style lang="scss" scoped>
@use '@/assets/styles/variables' as vars;

.sys-notices {
  background: vars.$layer-2;
  border: 1px solid rgba(0, 212, 255, 0.12);
  border-radius: 12px;
  padding: 14px 18px 8px;
}

.sys-notices__title {
  margin: 0 0 6px;
  font-size: 15px;
  font-weight: 600;
  color: vars.$text-primary;
}

.sys-notices__list {
  margin: 0;
  padding: 0;
  list-style: none;
}

.sys-notices__row {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 16px;
  width: 100%;
  padding: 8px 0;
  border: none;
  border-bottom: 1px solid var(--el-border-color-lighter);
  background: transparent;
  text-align: left;
  cursor: pointer;
  font-family: 'Noto Sans SC', sans-serif;

  &:last-child {
    border-bottom: none;
  }

  &:hover .sys-notices__name {
    color: #00d4ff;
  }
}

.sys-notices__name {
  min-width: 0;
  font-size: 14px;
  color: vars.$text-primary;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sys-notices__date {
  flex: none;
  font-size: 12px;
  color: vars.$text-secondary;
}
</style>
