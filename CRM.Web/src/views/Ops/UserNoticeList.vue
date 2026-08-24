<template>
  <div class="user-notice-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon" aria-hidden="true">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9" />
              <path d="M13.73 21a2 2 0 01-3.46 0" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('sysUserNotice.adminTitle') }}</h1>
        </div>
        <div class="count-badge">{{ t('sysUserNotice.count', { count: total }) }}</div>
      </div>
      <el-button type="primary" @click="openSend">{{ t('sysUserNotice.send') }}</el-button>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-model="filters.isUrgent"
          clearable
          :placeholder="t('sysUserNotice.filterUrgent')"
          class="status-select"
          :teleported="false"
          @change="onQuery"
        >
          <el-option :label="t('sysUserNotice.urgentYes')" :value="true" />
          <el-option :label="t('sysUserNotice.urgentNo')" :value="false" />
        </el-select>
        <el-select
          v-model="filters.isRead"
          clearable
          :placeholder="t('sysUserNotice.filterRead')"
          class="status-select"
          :teleported="false"
          @change="onQuery"
        >
          <el-option :label="t('sysUserNotice.read')" :value="true" />
          <el-option :label="t('sysUserNotice.unread')" :value="false" />
        </el-select>
        <el-select
          v-model="filters.recipientUserId"
          clearable
          filterable
          :placeholder="t('sysUserNotice.filterRecipient')"
          class="recipient-select"
          :teleported="false"
          @change="onQuery"
        >
          <el-option
            v-for="u in recipients"
            :key="u.id"
            :label="recipientOptionLabel(u)"
            :value="u.id"
          />
        </el-select>
        <el-input
          v-model="filters.keyword"
          clearable
          :placeholder="t('sysUserNotice.keywordPh')"
          class="keyword-input"
          @keyup.enter="onQuery"
        />
        <el-date-picker
          v-model="sendRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          unlink-panels
          :start-placeholder="t('sysUserNotice.sendFrom')"
          :end-placeholder="t('sysUserNotice.sendTo')"
          class="date-range"
          @change="onQuery"
        />
        <button type="button" class="btn-primary btn-sm" :disabled="loading" @click="onQuery">
          {{ t('sysUserNotice.query') }}
        </button>
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="reset">
          {{ t('sysUserNotice.reset') }}
        </button>
      </div>
    </div>

    <div class="table-wrapper" v-loading="loading">
      <CrmDataTable
        v-show="loading || rows.length > 0"
        ref="dataTableRef"
        row-key="id"
        column-layout-key="ops-user-notice-list-v2"
        :columns="tableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="rows"
        :row-class-name="rowClassName"
        @row-click="openDetail"
      >
        <template #col-isUrgent="{ row }">
          <span
            v-if="row.isUrgent"
            class="urgent-mark"
            :title="t('sysUserNotice.urgentYes')"
            :aria-label="t('sysUserNotice.urgentYes')"
          >
            <NoticeUrgentIcon />
          </span>
        </template>
        <template #col-isRead="{ row }">
          <el-tag :type="row.isRead ? 'success' : 'warning'" size="small" effect="plain">
            {{ row.isRead ? t('sysUserNotice.read') : t('sysUserNotice.unread') }}
          </el-tag>
        </template>
        <template #col-recipientLabel="{ row }">
          <span class="cell-ellipsis" :title="row.recipientLabel">{{ row.recipientLabel }}</span>
        </template>
        <template #col-title="{ row }">
          <span class="cell-ellipsis" :title="row.title">{{ row.title }}</span>
        </template>
        <template #col-bodyPreview="{ row }">
          <span class="cell-ellipsis" :title="row.bodyPreview || undefined">{{ row.bodyPreview || '—' }}</span>
        </template>
        <template #col-createTime="{ row }">
          {{ formatTime(row.createTime) }}
        </template>
      </CrmDataTable>

      <div v-show="!loading && rows.length === 0" class="empty-state">
        <p>{{ t('sysUserNotice.adminEmpty') }}</p>
      </div>
    </div>

    <div class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('systemUser.colSetting')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true" />
      </div>
      <el-pagination
        class="list-main-pagination"
        v-model:current-page="page"
        v-model:page-size="pageSize"
        :total="total"
        :page-sizes="[20, 50, 100]"
        layout="total, sizes, prev, pager, next"
        @size-change="onSizeChange"
        @current-change="load"
      />
    </div>

    <el-dialog
      v-model="sendOpen"
      :title="t('sysUserNotice.sendTitle')"
      width="560px"
      destroy-on-close
      @closed="resetSend"
    >
      <el-form label-width="90px">
        <el-form-item :label="t('sysUserNotice.colRecipient')" required>
          <el-select
            v-model="sendForm.recipientUserId"
            filterable
            :placeholder="t('sysUserNotice.filterRecipient')"
            style="width: 100%"
          >
            <el-option
              v-for="u in recipients"
              :key="u.id"
              :label="recipientOptionLabel(u)"
              :value="u.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('sysUserNotice.colUrgent')">
          <el-checkbox v-model="sendForm.isUrgent">{{ t('sysUserNotice.urgentHint') }}</el-checkbox>
        </el-form-item>
        <el-form-item :label="t('sysUserNotice.colTitle')" required>
          <el-input v-model="sendForm.title" maxlength="100" show-word-limit />
        </el-form-item>
        <el-form-item :label="t('sysUserNotice.body')" required>
          <el-input
            v-model="sendForm.body"
            type="textarea"
            :rows="8"
            maxlength="4000"
            show-word-limit
            :placeholder="t('sysUserNotice.bodyPh')"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="sendOpen = false">{{ t('sysUserNotice.cancel') }}</el-button>
        <el-button type="primary" :loading="sending" @click="submitSend">{{ t('sysUserNotice.send') }}</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="detailOpen" :title="t('sysUserNotice.detailTitle')" width="560px" destroy-on-close>
      <div v-if="detail" v-loading="detailLoading" class="detail-body">
        <div class="detail-meta">
          <el-tag :type="detail.isUrgent ? 'danger' : 'info'" size="small" effect="plain">
            {{ detail.isUrgent ? t('sysUserNotice.urgentYes') : t('sysUserNotice.urgentNo') }}
          </el-tag>
          <el-tag :type="detail.isRead ? 'success' : 'warning'" size="small" effect="plain">
            {{ detail.isRead ? t('sysUserNotice.read') : t('sysUserNotice.unread') }}
          </el-tag>
          <span>{{ detail.recipientLabel }}</span>
          <span>{{ formatTime(detail.createTime) }}</span>
        </div>
        <h3 class="detail-title">{{ detail.title }}</h3>
        <pre class="detail-text">{{ detail.body }}</pre>
        <p class="detail-hint">{{ t('sysUserNotice.adminViewHint') }}</p>
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import {
  sysUserNoticesApi,
  type UserNoticeAdminListItem,
  type UserNoticeDetail,
  type UserNoticeRecipient
} from '@/api/sysUserNotices'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import NoticeUrgentIcon from '@/components/SystemAnnouncement/NoticeUrgentIcon.vue'

const { t, locale } = useI18n()

const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const loading = ref(false)
const rows = ref<UserNoticeAdminListItem[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const recipients = ref<UserNoticeRecipient[]>([])
const sendRange = ref<[string, string] | null>(null)
const filters = reactive({
  isUrgent: undefined as boolean | undefined,
  isRead: undefined as boolean | undefined,
  recipientUserId: undefined as string | undefined,
  keyword: ''
})

const sendOpen = ref(false)
const sending = ref(false)
const sendForm = reactive({
  recipientUserId: '',
  isUrgent: false,
  title: '',
  body: ''
})

const detailOpen = ref(false)
const detailLoading = ref(false)
const detail = ref<UserNoticeDetail | null>(null)

const tableColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return [
    { key: 'isUrgent', label: t('sysUserNotice.colUrgent'), prop: 'isUrgent', width: 96, align: 'center' },
    { key: 'isRead', label: t('sysUserNotice.colRead'), prop: 'isRead', width: 90, align: 'center' },
    { key: 'recipientLabel', label: t('sysUserNotice.colRecipient'), prop: 'recipientLabel', minWidth: 160, showOverflowTooltip: true },
    { key: 'title', label: t('sysUserNotice.colTitle'), prop: 'title', minWidth: 200, showOverflowTooltip: true },
    { key: 'bodyPreview', label: t('sysUserNotice.colBodyPreview'), prop: 'bodyPreview', minWidth: 240, showOverflowTooltip: true },
    { key: 'createTime', label: t('sysUserNotice.colSendTime'), prop: 'createTime', width: 168 }
  ]
})

function rowClassName({ row }: { row: UserNoticeAdminListItem }) {
  return ['table-row-pointer', row.isRead ? 'is-read-row' : ''].filter(Boolean).join(' ')
}

function recipientOptionLabel(u: UserNoticeRecipient) {
  const name = (u.realName || '').trim()
  return name ? `${u.userName} / ${name}` : u.userName
}

function formatTime(v?: string | null) {
  return v ? formatDisplayDateTime(v) : '—'
}

function onQuery() {
  page.value = 1
  void load()
}

function onSizeChange() {
  page.value = 1
  void load()
}

function reset() {
  filters.isUrgent = undefined
  filters.isRead = undefined
  filters.recipientUserId = undefined
  filters.keyword = ''
  sendRange.value = null
  page.value = 1
  void load()
}

async function loadRecipients() {
  try {
    recipients.value = await sysUserNoticesApi.recipients()
  } catch {
    recipients.value = []
  }
}

async function load() {
  loading.value = true
  try {
    const data = await sysUserNoticesApi.adminList({
      isUrgent: filters.isUrgent,
      isRead: filters.isRead,
      recipientUserId: filters.recipientUserId,
      keyword: filters.keyword.trim() || undefined,
      sendFrom: sendRange.value?.[0],
      sendTo: sendRange.value?.[1],
      page: page.value,
      pageSize: pageSize.value
    })
    rows.value = data?.items || []
    total.value = Number(data?.total || 0)
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e) || t('sysUserNotice.loadFailed'))
  } finally {
    loading.value = false
  }
}

function openSend() {
  resetSend()
  sendOpen.value = true
}

function resetSend() {
  sendForm.recipientUserId = ''
  sendForm.isUrgent = false
  sendForm.title = ''
  sendForm.body = ''
}

async function submitSend() {
  if (!sendForm.recipientUserId) {
    ElMessage.warning(t('sysUserNotice.needRecipient'))
    return
  }
  if (!sendForm.title.trim()) {
    ElMessage.warning(t('sysUserNotice.needTitle'))
    return
  }
  if (!sendForm.body.trim()) {
    ElMessage.warning(t('sysUserNotice.needBody'))
    return
  }
  sending.value = true
  try {
    await sysUserNoticesApi.send({
      recipientUserId: sendForm.recipientUserId,
      isUrgent: sendForm.isUrgent,
      title: sendForm.title.trim(),
      body: sendForm.body.trim()
    })
    ElMessage.success(t('sysUserNotice.sent'))
    sendOpen.value = false
    page.value = 1
    void load()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e) || t('sysUserNotice.sendFailed'))
  } finally {
    sending.value = false
  }
}

async function openDetail(row: UserNoticeAdminListItem) {
  detailOpen.value = true
  detailLoading.value = true
  try {
    detail.value = await sysUserNoticesApi.adminGet(row.id)
  } catch (e) {
    detail.value = null
    ElMessage.error(getApiErrorMessage(e) || t('sysUserNotice.loadFailed'))
  } finally {
    detailLoading.value = false
  }
}

onMounted(() => {
  void loadRecipients()
  void load()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.user-notice-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
}

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
}

.page-icon {
  width: 36px;
  height: 36px;
  background: rgba(0, 212, 255, 0.1);
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: $cyan-primary;
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
}

.count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}

.search-bar {
  display: flex;
  align-items: center;
  margin-bottom: 12px;
}

.search-left {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.status-select {
  width: 120px;
}

.recipient-select {
  width: 200px;
}

.keyword-input {
  width: 200px;
}

.date-range {
  width: 260px;
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  padding: 6px 12px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 12px;
  cursor: pointer;
  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
}

.btn-ghost {
  display: inline-flex;
  align-items: center;
  padding: 6px 12px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 12px;
  cursor: pointer;
}

.table-wrapper {
  position: relative;
  min-height: 200px;
  :deep(.table-row-pointer) {
    cursor: pointer;
  }
  :deep(.el-table__body tr.is-read-row > td.el-table__cell) {
    color: $text-muted !important;
  }
  :deep(.el-table__body tr.is-read-row .urgent-mark) {
    color: #e11d48;
  }
  :deep(.el-table__body tr.is-read-row .cell-ellipsis) {
    color: $text-muted;
  }
  :deep(.el-table__body tr.is-read-row .el-tag) {
    color: var(--el-tag-text-color) !important;
  }
}

.urgent-mark {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #e11d48;
  background: none;
}

.cell-ellipsis {
  display: block;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.empty-state {
  display: flex;
  justify-content: center;
  padding: 48px 24px;
  color: $text-muted;
}

.pagination-wrapper {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 12px;
}

.list-footer-left {
  display: flex;
  align-items: center;
  gap: 8px;
}

.detail-meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  margin-bottom: 12px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.detail-title {
  margin: 0 0 12px;
  font-size: 16px;
  word-break: break-word;
}

.detail-text {
  margin: 0;
  white-space: pre-wrap;
  word-break: break-word;
  font-family: inherit;
  font-size: 14px;
  line-height: 1.6;
}

.detail-hint {
  margin: 16px 0 0;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}
</style>
