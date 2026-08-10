<template>
  <div class="my-mails-page">
    <div class="page-header">
      <h2>{{ t('myMails.title') }}</h2>
      <span class="count">{{ t('myMails.count', { count: total }) }}</span>
    </div>

    <el-alert
      v-if="summaryLoaded && !summary.hasVerifiedMailbox"
      type="warning"
      :closable="false"
      show-icon
      class="info-panel"
    >
      <template #title>{{ t('myMails.noMailboxTitle') }}</template>
      <p>{{ t('myMails.noMailboxHint') }}</p>
      <el-button type="primary" size="small" @click="goMailboxSettings">
        {{ t('myMails.goMailboxSettings') }}
      </el-button>
    </el-alert>

    <el-card v-else shadow="never" class="info-panel stats-card">
      <div class="stats-row">
        <div class="stat">
          <div class="stat-label">{{ t('myMails.stats.total') }}</div>
          <div class="stat-value">{{ summary.totalCount }}</div>
        </div>
        <div class="stat">
          <div class="stat-label">{{ t('myMails.stats.unread') }}</div>
          <div class="stat-value unread">{{ summary.unreadCount }}</div>
        </div>
      </div>
    </el-card>

    <el-card shadow="never" class="filter-card">
      <el-form :inline="true" class="filter-form" @submit.prevent="onSearch">
        <el-form-item :label="t('myMails.filters.mailbox')">
          <el-select
            v-model="filters.mailboxId"
            clearable
            filterable
            style="width: 220px"
            :placeholder="t('myMails.filters.mailboxAll')"
          >
            <el-option
              v-for="m in mailboxOptions"
              :key="m.id"
              :label="m.address"
              :value="m.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('myMails.filters.subject')">
          <el-input v-model="filters.subject" clearable style="width: 160px" @keyup.enter="onSearch" />
        </el-form-item>
        <el-form-item :label="t('myMails.filters.from')">
          <el-input v-model="filters.from" clearable style="width: 160px" @keyup.enter="onSearch" />
        </el-form-item>
        <el-form-item :label="t('myMails.filters.body')">
          <el-input v-model="filters.body" clearable style="width: 160px" @keyup.enter="onSearch" />
        </el-form-item>
        <el-form-item :label="t('myMails.filters.receivedRange')">
          <el-date-picker
            v-model="receivedRange"
            type="daterange"
            value-format="YYYY-MM-DD"
            unlink-panels
            style="width: 260px"
            :start-placeholder="t('myMails.filters.fromDate')"
            :end-placeholder="t('myMails.filters.toDate')"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :loading="loading" @click="onSearch">{{ t('myMails.filters.search') }}</el-button>
          <el-button :disabled="!summary.hasVerifiedMailbox" :loading="syncing" @click="openSyncDialog">
            {{ t('myMails.filters.readMails') }}
          </el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never" class="table-card">
      <el-table
        v-loading="loading"
        :data="rows"
        stripe
        highlight-current-row
        @row-dblclick="(row: MyMailListItem) => openDetail(row)"
      >
        <el-table-column :label="t('myMails.columns.receivedAt')" width="170">
          <template #default="{ row }">
            <span :class="{ 'is-unread': row.isUnread }">{{ formatAt(row.receivedAt) }}</span>
          </template>
        </el-table-column>
        <el-table-column :label="t('myMails.columns.subject')" min-width="200" show-overflow-tooltip>
          <template #default="{ row }">
            <span :class="{ 'is-unread': row.isUnread }">{{ row.subject || '—' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="snippet" :label="t('myMails.columns.snippet')" min-width="240" show-overflow-tooltip />
        <el-table-column :label="t('myMails.columns.from')" min-width="160" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.fromName ? `${row.fromName} <${row.fromAddress || ''}>` : row.fromAddress || '—' }}
          </template>
        </el-table-column>
        <el-table-column prop="mailboxAddress" :label="t('myMails.columns.mailbox')" min-width="160" show-overflow-tooltip />
        <el-table-column :label="t('myMails.columns.actions')" width="100" fixed="right" align="center">
          <template #default="{ row }">
            <el-button link type="primary" @click="openDetail(row)">{{ t('myMails.actions.view') }}</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="pager">
        <el-pagination
          v-model:current-page="page"
          v-model:page-size="pageSize"
          layout="total, sizes, prev, pager, next"
          :total="total"
          :page-sizes="[10, 20, 50]"
          @current-change="loadList"
          @size-change="onPageSizeChange"
        />
      </div>
    </el-card>

    <el-dialog
      v-model="syncVisible"
      :title="t('myMails.syncDialog.title')"
      width="420px"
      destroy-on-close
    >
      <el-form label-width="100px">
        <el-form-item :label="t('myMails.syncDialog.mailbox')">
          <el-select v-model="syncMailboxId" style="width: 100%">
            <el-option :label="t('myMails.syncDialog.all')" value="" />
            <el-option
              v-for="m in mailboxOptions"
              :key="m.id"
              :label="m.address"
              :value="m.id"
            />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="syncVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="syncing" @click="runSync">{{ t('myMails.syncDialog.confirm') }}</el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="detailVisible"
      :title="detail?.subject || t('myMails.detail.title')"
      width="720px"
      destroy-on-close
      class="mail-detail-dialog"
    >
      <template v-if="detail">
        <div class="detail-meta">
          <div><b>{{ t('myMails.columns.from') }}：</b>{{ detail.fromName || '' }} {{ detail.fromAddress }}</div>
          <div><b>{{ t('myMails.columns.mailbox') }}：</b>{{ detail.mailboxAddress }}</div>
          <div><b>{{ t('myMails.columns.receivedAt') }}：</b>{{ formatAt(detail.receivedAt) }}</div>
        </div>
        <div v-if="detail.bodyHtml" class="detail-body" v-html="sanitizedHtml" />
        <pre v-else class="detail-body detail-body--text">{{ detail.bodyText || detail.snippet || '' }}</pre>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import {
  fetchMyMailDetail,
  fetchMyMailMailboxOptions,
  fetchMyMails,
  fetchMyMailSummary,
  markMyMailRead,
  syncMyMails,
  type MyMailDetail,
  type MyMailListItem,
  type MyMailMailboxOption,
  type MyMailSummary
} from '@/api/myMails'
import { getApiErrorMessage } from '@/utils/apiError'
import { profileMailboxLocation } from '@/utils/profileMailboxLink'
import DOMPurify from 'dompurify'

const { t } = useI18n()
const router = useRouter()

const summaryLoaded = ref(false)
const summary = reactive<MyMailSummary>({
  hasVerifiedMailbox: false,
  verifiedMailboxCount: 0,
  totalCount: 0,
  unreadCount: 0
})
const mailboxOptions = ref<MyMailMailboxOption[]>([])
const filters = reactive({
  mailboxId: '' as string,
  subject: '',
  from: '',
  body: ''
})
const receivedRange = ref<[string, string] | null>(null)
const rows = ref<MyMailListItem[]>([])
const loading = ref(false)
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)

const syncVisible = ref(false)
const syncMailboxId = ref('')
const syncing = ref(false)

const detailVisible = ref(false)
const detail = ref<MyMailDetail | null>(null)
const sanitizedHtml = computed(() =>
  detail.value?.bodyHtml
    ? DOMPurify.sanitize(detail.value.bodyHtml, { USE_PROFILES: { html: true } })
    : ''
)

function formatAt(v?: string | null) {
  if (!v) return '—'
  const d = new Date(v)
  if (Number.isNaN(d.getTime())) return String(v)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`
}

function goMailboxSettings() {
  router.push(profileMailboxLocation('/my/mails'))
}

async function loadSummary() {
  try {
    const s = await fetchMyMailSummary()
    Object.assign(summary, s)
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('myMails.messages.loadFailed')))
  } finally {
    summaryLoaded.value = true
  }
}

async function loadMailboxes() {
  try {
    mailboxOptions.value = await fetchMyMailMailboxOptions()
  } catch {
    mailboxOptions.value = []
  }
}

async function loadList() {
  loading.value = true
  try {
    const data = await fetchMyMails({
      mailboxId: filters.mailboxId || undefined,
      subject: filters.subject || undefined,
      from: filters.from || undefined,
      body: filters.body || undefined,
      receivedFrom: receivedRange.value?.[0],
      receivedTo: receivedRange.value?.[1],
      page: page.value,
      pageSize: pageSize.value
    })
    rows.value = data?.items ?? []
    total.value = data?.total ?? 0
  } catch (e) {
    rows.value = []
    total.value = 0
    ElMessage.error(getApiErrorMessage(e, t('myMails.messages.loadFailed')))
  } finally {
    loading.value = false
  }
}

function onSearch() {
  page.value = 1
  void loadList()
}

function onPageSizeChange() {
  page.value = 1
  void loadList()
}

function openSyncDialog() {
  syncMailboxId.value = ''
  syncVisible.value = true
}

async function runSync() {
  syncing.value = true
  try {
    const result = await syncMyMails(syncMailboxId.value || null)
    const err = (result.errors || []).filter(Boolean)
    if (err.length) {
      ElMessage.warning(err.join('；'))
    } else {
      ElMessage.success(
        t('myMails.messages.syncDone', {
          fetched: result.fetchedCount,
          upserted: result.upsertedCount
        })
      )
    }
    syncVisible.value = false
    await loadSummary()
    await loadList()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('myMails.messages.syncFailed')))
  } finally {
    syncing.value = false
  }
}

async function openDetail(row: MyMailListItem) {
  try {
    detail.value = await fetchMyMailDetail(row.id)
    detailVisible.value = true
    if (row.isUnread) {
      await markMyMailRead(row.id)
      row.isUnread = false
      if (summary.unreadCount > 0) summary.unreadCount -= 1
      if (detail.value) detail.value.isUnread = false
    }
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('myMails.messages.loadFailed')))
  }
}

onMounted(async () => {
  await Promise.all([loadSummary(), loadMailboxes()])
  await loadList()
})
</script>

<style scoped>
.my-mails-page {
  padding: 16px 20px 28px;
}
.page-header {
  display: flex;
  align-items: baseline;
  gap: 12px;
  margin-bottom: 12px;
}
.page-header h2 {
  margin: 0;
  font-size: 20px;
}
.count {
  color: var(--el-text-color-secondary);
  font-size: 13px;
}
.info-panel {
  margin-bottom: 12px;
}
.stats-card :deep(.el-card__body) {
  padding: 14px 18px;
}
.stats-row {
  display: flex;
  gap: 32px;
}
.stat-label {
  font-size: 13px;
  color: var(--el-text-color-secondary);
}
.stat-value {
  font-size: 22px;
  font-weight: 600;
  margin-top: 4px;
}
.stat-value.unread {
  color: var(--el-color-warning);
}
.filter-card,
.table-card {
  margin-bottom: 12px;
}
.pager {
  display: flex;
  justify-content: flex-end;
  margin-top: 12px;
}
.is-unread {
  font-weight: 600;
}
.detail-meta {
  margin-bottom: 12px;
  font-size: 13px;
  line-height: 1.7;
  color: var(--el-text-color-regular);
}
.detail-body {
  max-height: 480px;
  overflow: auto;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 6px;
  padding: 12px;
  background: var(--el-fill-color-blank);
}
.detail-body--text {
  white-space: pre-wrap;
  font-family: inherit;
  margin: 0;
}
</style>
