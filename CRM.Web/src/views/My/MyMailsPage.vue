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
          <el-table-column :label="t('myMails.columns.subject')" min-width="240" show-overflow-tooltip>
            <template #default="{ row }">
              <span :class="{ 'is-unread': row.isUnread }">{{ row.subject || '—' }}</span>
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
    </template>

    <template v-else-if="viewMode === 'body'">
      <el-card shadow="never" class="body-card" v-loading="detailLoading">
        <div class="mail-toolbar">
          <el-button @click="backToList">{{ t('myMails.body.back') }}</el-button>
          <el-button v-if="folderId !== 'sent'" :disabled="!detail" @click="onReply">
            {{ t('myMails.body.reply') }}
          </el-button>
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
        </div>
        <div v-if="detail" class="mail-summary">
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
        <div v-if="detail?.bodyHtml" class="mail-body" v-html="sanitizedHtml" />
        <pre v-else class="mail-body mail-body--text">{{ detail?.bodyText || detail?.snippet || '' }}</pre>
        <div class="mail-signature">
          <div class="mail-signature__title">{{ t('myMails.body.signature') }}</div>
          <div class="mail-signature__empty">{{ t('myMails.body.signatureEmpty') }}</div>
        </div>
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
        <div class="mail-toolbar">
          <el-button @click="cancelCompose">{{ t('myMails.body.back') }}</el-button>
          <el-button type="primary" :loading="sending" @click="onSend">
            {{ t('myMails.compose.send') }}
          </el-button>
        </div>
        <el-form label-width="96px" class="compose-form" @submit.prevent="onSend">
          <el-form-item :label="t('myMails.compose.from')">
            <span class="compose-from">{{ sendFromDisplay || '—' }}</span>
          </el-form-item>
          <el-form-item :label="t('myMails.compose.to')">
            <el-input v-model="compose.to" :placeholder="t('myMails.compose.toPh')" />
          </el-form-item>
          <el-form-item :label="t('myMails.compose.cc')">
            <el-input v-model="compose.cc" :placeholder="t('myMails.compose.ccPh')" />
          </el-form-item>
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
        </el-form>
      </el-card>
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
  deleting,
  restoring,
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
.body-card {
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
  margin-bottom: 16px;
}
.mail-summary {
  margin-bottom: 14px;
  font-size: 13px;
  line-height: 1.7;
  color: var(--el-text-color-regular);
}
.mail-summary__title {
  font-size: 18px;
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
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 6px;
  padding: 12px;
  background: var(--el-fill-color-blank);
  line-height: 1.6;
}
.mail-body--text {
  white-space: pre-wrap;
  font-family: inherit;
  margin: 0;
}
.mail-signature {
  margin-top: 14px;
  padding: 10px 12px;
  border: 1px dashed var(--el-border-color);
  border-radius: 6px;
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
.mail-remark {
  margin-top: 14px;
  padding: 10px 12px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 6px;
  background: var(--el-fill-color-light);
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
.compose-form {
  max-width: 920px;
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
