<template>
  <div class="my-mails-page">
    <div class="page-header">
      <h2>{{ t('myMails.title') }}</h2>
      <span class="count">{{
        t('myMails.count', { count: viewMode === 'contacts' ? addressTotal : total })
      }}</span>
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

    <el-card v-else-if="viewMode === 'list'" shadow="never" class="info-panel stats-card">
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

    <template v-if="viewMode === 'list'">
      <el-card shadow="never" class="filter-card">
        <el-form :inline="true" class="filter-form" @submit.prevent="search">
          <el-form-item :label="t('myMails.filters.keyword')">
            <el-input
              v-model="keyword"
              clearable
              style="width: 280px"
              :placeholder="t('myMails.filters.keywordPh')"
              @keyup.enter="search"
            />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" :loading="loading" @click="search">
              {{ t('myMails.filters.search') }}
            </el-button>
            <el-button :disabled="!summary.hasVerifiedMailbox" :loading="syncing" @click="receiveSelectedMailbox">
              {{ t('myMails.filters.readMails') }}
            </el-button>
            <el-button :disabled="!summary.hasVerifiedMailbox" @click="onWriteMail">
              {{ t('myMails.compose.write') }}
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
          row-key="id"
          :row-class-name="rowClassName"
          @row-click="(row: MyMailListItem) => selectRow(row)"
          @row-dblclick="(row: MyMailListItem) => openBody(row)"
        >
          <el-table-column width="72" align="center">
            <template #default="{ row }">
              <img
                class="read-icon"
                :src="row.isUnread ? mailUnreadIcon : mailReadIcon"
                :alt="row.isUnread ? t('myMails.columns.unread') : t('myMails.columns.read')"
                :title="row.isUnread ? t('myMails.columns.unread') : t('myMails.columns.read')"
              />
            </template>
          </el-table-column>
          <el-table-column width="56" align="center" class-name="star-col">
            <template #default="{ row }">
              <button
                type="button"
                class="star-btn"
                :title="row.isStarred ? t('myMails.columns.starred') : t('myMails.columns.star')"
                @click.stop="toggleStar(row)"
              >
                <img
                  class="star-icon"
                  :src="row.isStarred ? mailStarOnIcon : mailStarOffIcon"
                  :alt="row.isStarred ? t('myMails.columns.starred') : t('myMails.columns.star')"
                />
              </button>
            </template>
          </el-table-column>
          <el-table-column :label="t('myMails.columns.from')" min-width="160" show-overflow-tooltip>
            <template #default="{ row }">
              <span :class="{ 'is-unread': row.isUnread }">{{ formatMailFrom(row) }}</span>
            </template>
          </el-table-column>
          <el-table-column :label="t('myMails.columns.subject')" min-width="240">
            <template #default="{ row }">
              <div class="mail-subject-cell" :title="subjectCellTitle(row)">
                <span class="mail-subject-cell__title" :class="{ 'is-unread': row.isUnread }">
                  {{ row.subject || '—' }}
                </span>
                <span v-if="mailSnippetPreview(row.snippet)" class="mail-subject-cell__snippet">
                  - {{ mailSnippetPreview(row.snippet) }}
                </span>
              </div>
            </template>
          </el-table-column>
          <el-table-column
            :label="t('myMails.columns.remark')"
            min-width="140"
            show-overflow-tooltip
          >
            <template #default="{ row }">
              <span>{{ row.remark?.trim() || '—' }}</span>
            </template>
          </el-table-column>
          <el-table-column :label="t('myMails.columns.receivedAt')" width="170">
            <template #default="{ row }">
              <span :class="{ 'is-unread': row.isUnread }">{{ formatMailAt(row.receivedAt) }}</span>
            </template>
          </el-table-column>
        </el-table>
        <div class="pager pager--with-action">
          <el-button
            link
            class="mark-all-read-btn"
            :disabled="!hasMailbox"
            :loading="markingAllRead"
            @click="markAllRead"
          >
            {{ t('myMails.filters.markAllRead') }}
          </el-button>
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
    </template>

    <template v-else-if="viewMode === 'body'">
      <div v-loading="detailLoading">
        <el-card shadow="never" class="body-card">
          <div class="mail-toolbar mail-toolbar--split">
            <el-button @click="backToList">{{ t('myMails.body.back') }}</el-button>
            <div class="mail-toolbar__right">
              <el-button
                v-if="isDeletedFolder"
                :loading="restoring"
                :disabled="!detail"
                @click="onRestore"
              >
                {{ t('myMails.body.restore') }}
              </el-button>
              <el-button
                v-else
                :loading="deleting"
                :disabled="!detail"
                @click="onDelete"
              >
                {{ t('myMails.body.delete') }}
              </el-button>
              <el-button
                v-if="folderId !== 'sent' && folderId !== 'draft'"
                type="primary"
                :disabled="!detail"
                @click="onReply"
              >
                {{ t('myMails.body.reply') }}
              </el-button>
            </div>
          </div>
        </el-card>
        <el-card v-if="detail" shadow="never" class="mail-header-card">
          <div class="mail-summary">
            <div class="mail-summary__title">{{ detail.subject || t('myMails.detail.title') }}</div>
            <div class="mail-summary__row">
              <span class="mail-summary__label">{{ t('myMails.body.from') }}</span>
              <span>{{ formatMailFrom(detail) }}{{ detail.fromAddress ? ` <${detail.fromAddress}>` : '' }}</span>
            </div>
            <div class="mail-summary__row">
              <span class="mail-summary__label">{{ t('myMails.body.sentAt') }}</span>
              <span>{{ formatMailAt(detail.receivedAt) }}</span>
            </div>
            <div class="mail-summary__row">
              <span class="mail-summary__label">{{ t('myMails.body.to') }}</span>
              <span>{{ detail.toAddresses || '—' }}</span>
            </div>
            <div class="mail-summary__row">
              <span class="mail-summary__label">{{ t('myMails.body.cc') }}</span>
              <span>—</span>
            </div>
          </div>
        </el-card>
        <el-card shadow="never" class="body-card">
          <div v-if="detail?.bodyHtml" class="mail-body" v-html="sanitizedHtml" />
          <pre v-else class="mail-body mail-body--text">{{ detail?.bodyText || detail?.snippet || '' }}</pre>
          <div class="mail-signature">
            <div class="mail-signature__title">{{ t('myMails.body.signature') }}</div>
            <div class="mail-signature__empty">{{ t('myMails.body.signatureEmpty') }}</div>
          </div>
        </el-card>
        <el-card shadow="never" class="mail-remark-card">
          <div class="mail-remark">
            <div class="mail-remark__title">{{ t('myMails.remark.title') }}</div>
            <el-input
              v-model="remarkDraft"
              type="textarea"
              :rows="4"
              maxlength="2000"
              show-word-limit
              :placeholder="t('myMails.remark.placeholder')"
            />
            <div class="mail-remark__actions">
              <el-button
                type="primary"
                :disabled="!canSaveRemark"
                :loading="remarkSaving"
                @click="saveRemark"
              >
                {{ t('myMails.remark.save') }}
              </el-button>
              <el-button
                :disabled="!canClearRemark"
                :loading="remarkClearing"
                @click="onClearRemark"
              >
                {{ t('myMails.remark.clear') }}
              </el-button>
            </div>
          </div>
        </el-card>
      </div>
    </template>

    <template v-else-if="viewMode === 'contacts'">
      <el-card shadow="never" class="filter-card">
        <el-form :inline="true" class="filter-form" @submit.prevent="search">
          <el-form-item :label="t('myMails.filters.keyword')">
            <el-input
              v-model="addressKeyword"
              clearable
              style="width: 280px"
              :placeholder="t('myMails.addressBook.keywordPh')"
              @keyup.enter="search"
            />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" :loading="addressLoading" @click="search">
              {{ t('myMails.filters.search') }}
            </el-button>
          </el-form-item>
        </el-form>
      </el-card>
      <el-card shadow="never" class="table-card">
        <el-table
          v-loading="addressLoading"
          :data="addressRows"
          stripe
          highlight-current-row
          row-key="id"
          :row-class-name="addressRowClassName"
          @row-click="(row: MyMailAddressBookItem) => selectAddress(row)"
          @row-dblclick="(row: MyMailAddressBookItem) => composeToAddress(row)"
        >
          <el-table-column
            :label="t('myMails.addressBook.contactName')"
            min-width="140"
            show-overflow-tooltip
          >
            <template #default="{ row }">{{ row.contactName || '—' }}</template>
          </el-table-column>
          <el-table-column
            :label="t('myMails.addressBook.email')"
            min-width="200"
            show-overflow-tooltip
            prop="email"
          />
          <el-table-column
            :label="t('myMails.addressBook.partyName')"
            min-width="200"
            show-overflow-tooltip
          >
            <template #default="{ row }">{{ row.partyName || '—' }}</template>
          </el-table-column>
        </el-table>
        <div class="pager">
          <el-pagination
            v-model:current-page="addressPage"
            v-model:page-size="addressPageSize"
            layout="total, sizes, prev, pager, next"
            :total="addressTotal"
            :page-sizes="[10, 20, 50]"
            @current-change="loadAddressBook"
            @size-change="onAddressPageSizeChange"
          />
        </div>
      </el-card>
    </template>

    <template v-else-if="viewMode === 'compose'">
      <el-card shadow="never" class="body-card">
        <div class="mail-toolbar mail-toolbar--split">
          <el-button @click="cancelCompose">{{ t('myMails.body.back') }}</el-button>
          <div class="mail-toolbar__right">
            <el-button :loading="savingDraft" :disabled="!hasMailbox" @click="saveDraft">
              {{ t('myMails.compose.saveDraft') }}
            </el-button>
            <el-button type="primary" :loading="sending" @click="onSend">
              {{ t('myMails.compose.send') }}
            </el-button>
            <el-button
              v-if="compose.draftId"
              :loading="deleting"
              @click="onDelete"
            >
              {{ t('myMails.body.delete') }}
            </el-button>
          </div>
        </div>
      </el-card>
      <el-form label-width="96px" @submit.prevent="onSend">
        <el-card shadow="never" class="body-card compose-panel">
          <div class="compose-form">
            <el-form-item :label="t('myMails.compose.from')">
              <span class="compose-from">{{ sendFromDisplay || '—' }}</span>
            </el-form-item>
            <el-form-item :label="t('myMails.compose.to')">
              <el-input v-model="compose.to" :placeholder="t('myMails.compose.toPh')" />
            </el-form-item>
            <el-form-item :label="t('myMails.compose.cc')">
              <el-input v-model="compose.cc" :placeholder="t('myMails.compose.ccPh')" />
            </el-form-item>
          </div>
        </el-card>
        <el-card shadow="never" class="body-card compose-panel">
          <div class="compose-form compose-form--full">
            <el-form-item :label="t('myMails.compose.subject')">
              <el-input v-model="compose.subject" :placeholder="t('myMails.compose.subjectPh')" />
            </el-form-item>
            <el-form-item :label="t('myMails.compose.body')">
              <el-input
                v-model="compose.body"
                type="textarea"
                :rows="16"
                :placeholder="t('myMails.compose.bodyPh')"
              />
            </el-form-item>
          </div>
        </el-card>
      </el-form>
    </template>

  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { ElMessageBox } from 'element-plus'
import DOMPurify from 'dompurify'
import type { MyMailAddressBookItem, MyMailListItem } from '@/api/myMails'
import { profileMailboxLocation } from '@/utils/profileMailboxLink'
import {
  formatMailAt,
  formatMailFrom,
  useMyMailsWorkspace
} from '@/composables/useMyMailsWorkspace'
import mailReadIcon from '@/assets/icons/mail/mail-read.svg'
import mailUnreadIcon from '@/assets/icons/mail/mail-unread.svg'
import mailStarOnIcon from '@/assets/icons/mail/mail-star-on.svg'
import mailStarOffIcon from '@/assets/icons/mail/mail-star-off.svg'

const { t } = useI18n()
const router = useRouter()
const {
  summaryLoaded,
  summary,
  keyword,
  rows,
  loading,
  total,
  page,
  pageSize,
  selectedId,
  folderId,
  viewMode,
  detail,
  detailLoading,
  syncing,
  loadList,
  search,
  onPageSizeChange,
  selectRow,
  toggleStar,
  openBody,
  backToList,
  receiveSelectedMailbox,
  ensureLoaded,
  sending,
  savingDraft,
  saveDraft,
  deleting,
  restoring,
  markingAllRead,
  markAllRead,
  hasMailbox,
  isDeletedFolder,
  compose,
  sendFromDisplay,
  startCompose,
  startReply,
  cancelCompose,
  sendCompose,
  deleteCurrent,
  restoreCurrent,
  remarkDraft,
  remarkSaving,
  remarkClearing,
  canSaveRemark,
  canClearRemark,
  saveRemark,
  clearRemark,
  addressKeyword,
  addressRows,
  addressLoading,
  addressTotal,
  addressPage,
  addressPageSize,
  selectedAddressId,
  loadAddressBook,
  selectAddress,
  composeToAddress,
  onAddressPageSizeChange
} = useMyMailsWorkspace()

const sanitizedHtml = computed(() =>
  detail.value?.bodyHtml
    ? DOMPurify.sanitize(detail.value.bodyHtml, { USE_PROFILES: { html: true } })
    : ''
)

function mailSnippetPreview(raw?: string | null, max = 100) {
  const s = (raw || '').replace(/\s+/g, ' ').trim()
  if (!s) return ''
  return s.length > max ? s.slice(0, max) : s
}

function subjectCellTitle(row: MyMailListItem) {
  const title = (row.subject || '').trim() || '—'
  const snippet = mailSnippetPreview(row.snippet, 200)
  return snippet ? `${title} - ${snippet}` : title
}

function rowClassName({ row }: { row: MyMailListItem }) {
  return row.id === selectedId.value ? 'is-current' : ''
}

function addressRowClassName({ row }: { row: MyMailAddressBookItem }) {
  return row.id === selectedAddressId.value ? 'is-current' : ''
}

function goMailboxSettings() {
  router.push(profileMailboxLocation('/my/mails'))
}

function onWriteMail() {
  startCompose()
}

function onReply() {
  if (!detail.value) return
  startReply(detail.value)
}

async function onSend() {
  await sendCompose()
}

async function onDelete() {
  try {
    await ElMessageBox.confirm(t('myMails.body.deleteConfirm'), t('myMails.body.delete'), {
      type: 'warning',
      confirmButtonText: t('common.confirm'),
      cancelButtonText: t('common.cancel')
    })
  } catch {
    return
  }
  await deleteCurrent()
}

async function onRestore() {
  await restoreCurrent()
}

async function onClearRemark() {
  if (!canClearRemark.value) return
  try {
    await ElMessageBox.confirm(t('myMails.remark.clearConfirm'), t('myMails.remark.clear'), {
      type: 'warning',
      confirmButtonText: t('common.confirm'),
      cancelButtonText: t('common.cancel')
    })
  } catch {
    return
  }
  await clearRemark()
}


onMounted(() => {
  void ensureLoaded()
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
.table-card,
.body-card,
.mail-header-card,
.mail-remark-card {
  margin-bottom: 12px;
}
.pager {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  margin-top: 12px;
}
.pager--with-action {
  justify-content: space-between;
}
.mark-all-read-btn {
  --el-button-text-color: #1a3a5c;
  --el-button-hover-text-color: #1a3a5c;
  --el-button-disabled-text-color: #1a3a5c;
}
.is-unread {
  font-weight: 600;
}
.mail-subject-cell {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.mail-subject-cell__snippet {
  color: var(--el-text-color-secondary);
  font-weight: 400;
}
.read-icon {
  display: block;
  width: 20px;
  height: 20px;
  margin: 0 auto;
}
:deep(.star-col.el-table__cell) {
  padding-left: 4px !important;
  padding-right: 4px !important;
  overflow: visible;
}
.star-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 22px;
  height: 22px;
  padding: 0;
  border: 0;
  background: transparent;
  cursor: pointer;
  border-radius: 4px;
}
.star-btn:hover {
  background: var(--crm-accent-008);
}
.star-icon {
  display: block;
  width: 18px;
  height: 18px;
}
:deep(.el-table__row.is-current > td.el-table__cell) {
  background: var(--crm-accent-008) !important;
}
.mail-toolbar {
  display: flex;
  gap: 8px;
}
.mail-toolbar--split {
  justify-content: space-between;
  align-items: center;
}
.mail-toolbar__right {
  display: flex;
  gap: 8px;
}
.mail-summary {
  font-size: 13px;
  line-height: 1.7;
  color: var(--el-text-color-regular);
}
.mail-summary__title {
  font-size: 16px;
  font-weight: 600;
  color: var(--el-text-color-primary);
  margin-bottom: 10px;
}
.mail-summary__row {
  display: flex;
  gap: 10px;
}
.mail-summary__label {
  width: 72px;
  flex-shrink: 0;
  color: var(--el-text-color-secondary);
}
.mail-body {
  min-height: calc(1.6em * 10 + 24px);
  max-height: 520px;
  overflow: auto;
  border: 0;
  border-radius: 0;
  padding: 0;
  background: transparent;
  line-height: 1.6;
}
.mail-body--text {
  white-space: pre-wrap;
  font-family: inherit;
  margin: 0;
}
.mail-signature {
  margin-top: 14px;
  padding: 0;
  border: 0;
}
.mail-signature__title {
  font-size: 13px;
  font-weight: 600;
  margin-bottom: 6px;
}
.mail-signature__empty {
  font-size: 12px;
  color: var(--el-text-color-placeholder);
}
.mail-remark-card {
  background: var(--el-fill-color-light);
}
.mail-remark-card :deep(.el-card__body) {
  background: var(--el-fill-color-light);
}
.mail-remark {
  padding: 0;
}
.mail-remark__title {
  font-size: 13px;
  font-weight: 600;
  margin-bottom: 8px;
}
.mail-remark :deep(.el-textarea__inner) {
  background-color: #fff !important;
}
.mail-remark__actions {
  display: flex;
  gap: 8px;
  margin-top: 10px;
}
.compose-panel {
  background: #f8f9fb;
}
.compose-panel :deep(.el-card__body) {
  background: #f8f9fb;
}
.compose-panel :deep(.el-input__wrapper),
.compose-panel :deep(.el-textarea__inner) {
  background-color: #fff !important;
}
.compose-form {
  max-width: 920px;
}
.compose-form--full {
  max-width: none;
  width: 100%;
}
.compose-form--full :deep(.el-form-item),
.compose-form--full :deep(.el-form-item__content),
.compose-form--full :deep(.el-input),
.compose-form--full :deep(.el-textarea) {
  width: 100%;
  max-width: none;
}
.compose-form :deep(.el-form-item:last-child) {
  margin-bottom: 0;
}
.compose-from {
  display: inline-flex;
  align-items: center;
  min-height: 32px;
  font-size: 14px;
  color: var(--el-text-color-regular);
  word-break: break-all;
}
</style>
