<template>
  <div class="mail-mailbox-menu">
    <section class="mail-mailbox-menu__group">
      <div class="mail-mailbox-menu__panel">
        <el-select
          v-if="!mailboxesLoaded || hasMailbox"
          v-model="mailboxId"
          filterable
          class="mail-mailbox-menu__select"
          :disabled="!hasMailbox"
          :placeholder="hasMailbox ? t('myMails.filters.mailbox') : ''"
          @change="onMailboxChange"
        >
          <el-option
            v-for="m in mailboxOptions"
            :key="m.id"
            :label="m.displayName ? `${m.displayName} <${m.address}>` : m.address"
            :value="m.id"
          />
        </el-select>
        <router-link
          v-else
          class="mail-mailbox-menu__setup"
          :to="profileMailboxLocation('/my/mails')"
        >
          {{ t('myMails.filters.setupMailbox') }}
        </router-link>
      </div>
    </section>

    <section class="mail-mailbox-menu__group">
      <div
        class="mail-mailbox-menu__panel mail-mailbox-menu__panel--fn"
        :class="{ 'is-disabled': !hasMailbox }"
        :style="{ backgroundImage: `url(${fnPanelWatermark})` }"
      >
      <ul class="mail-mailbox-menu__actions">
        <li>
          <button
            type="button"
            class="mail-mailbox-menu__action"
            :disabled="!hasMailbox"
            @click="startCompose()"
          >
            <img class="mail-mailbox-menu__action-icon" :src="fnWriteIcon" alt="" />
            <span>{{ t('myMails.fn.write') }}</span>
          </button>
        </li>
        <li>
          <button
            type="button"
            class="mail-mailbox-menu__action"
            :disabled="!hasMailbox || syncing"
            @click="receiveSelectedMailbox"
          >
            <img class="mail-mailbox-menu__action-icon" :src="fnReceiveIcon" alt="" />
            <span>{{ t('myMails.fn.receive') }}</span>
          </button>
        </li>
        <li>
          <button
            type="button"
            class="mail-mailbox-menu__action"
            :class="{ 'is-active': viewMode === 'contacts' }"
            :disabled="!hasMailbox"
            @click="openAddressBook"
          >
            <img class="mail-mailbox-menu__action-icon" :src="fnAddressIcon" alt="" />
            <span>{{ t('myMails.fn.addressBook') }}</span>
          </button>
        </li>
      </ul>
      </div>
    </section>

    <section class="mail-mailbox-menu__group">
      <div class="mail-mailbox-menu__panel" :class="{ 'is-disabled': !hasMailbox }">
      <ul class="mail-mailbox-menu__list">
        <li>
          <button
            type="button"
            class="mail-mailbox-menu__item"
            :class="{
              'is-active': hasMailbox && viewMode !== 'contacts' && folderId === 'inbox'
            }"
            :disabled="!hasMailbox"
            @click="selectFolder('inbox')"
          >
            <span>{{ t('myMails.menu.inboxWithUnread', { count: summary.unreadCount }) }}</span>
          </button>
        </li>
        <li>
          <button
            type="button"
            class="mail-mailbox-menu__item"
            :class="{ 'is-active': hasMailbox && viewMode !== 'contacts' && folderId === 'draft' }"
            :disabled="!hasMailbox"
            @click="selectFolder('draft')"
          >
            <span>{{ t('myMails.menu.draft') }}</span>
          </button>
        </li>
        <li>
          <button
            type="button"
            class="mail-mailbox-menu__item"
            :class="{ 'is-active': hasMailbox && viewMode !== 'contacts' && folderId === 'sent' }"
            :disabled="!hasMailbox"
            @click="selectFolder('sent')"
          >
            <span>{{ t('myMails.menu.sent') }}</span>
          </button>
        </li>
        <li>
          <button
            type="button"
            class="mail-mailbox-menu__item"
            :class="{ 'is-active': hasMailbox && viewMode !== 'contacts' && folderId === 'deleted' }"
            :disabled="!hasMailbox"
            @click="selectFolder('deleted')"
          >
            <span>{{ t('myMails.menu.deleted') }}</span>
          </button>
        </li>
      </ul>
      </div>
    </section>

    <section class="mail-mailbox-menu__group">
      <div class="mail-mailbox-menu__panel" :class="{ 'is-disabled': !hasMailbox }">
      <ul class="mail-mailbox-menu__list">
        <li>
          <button
            type="button"
            class="mail-mailbox-menu__item"
            :class="{ 'is-active': hasMailbox && viewMode !== 'contacts' && readFilter === 'all' }"
            :disabled="!hasMailbox"
            @click="selectListFilter('all')"
          >
            <span>{{ t('myMails.filters.readAll') }}</span>
          </button>
        </li>
        <li>
          <button
            type="button"
            class="mail-mailbox-menu__item"
            :class="{ 'is-active': hasMailbox && viewMode !== 'contacts' && readFilter === 'unread' }"
            :disabled="!hasMailbox"
            @click="selectListFilter('unread')"
          >
            <span>{{ t('myMails.filters.unreadOnly') }}</span>
          </button>
        </li>
        <li>
          <button
            type="button"
            class="mail-mailbox-menu__item"
            :class="{ 'is-active': hasMailbox && viewMode !== 'contacts' && readFilter === 'read' }"
            :disabled="!hasMailbox"
            @click="selectListFilter('read')"
          >
            <span>{{ t('myMails.filters.readOnly') }}</span>
          </button>
        </li>
        <li>
          <button
            type="button"
            class="mail-mailbox-menu__item"
            :class="{ 'is-active': hasMailbox && viewMode !== 'contacts' && readFilter === 'starred' }"
            :disabled="!hasMailbox"
            @click="selectListFilter('starred')"
          >
            <span>{{ t('myMails.filters.starredOnly') }}</span>
          </button>
        </li>
        <li>
          <button
            type="button"
            class="mail-mailbox-menu__item"
            :class="{ 'is-active': hasMailbox && viewMode !== 'contacts' && readFilter === 'remarked' }"
            :disabled="!hasMailbox"
            @click="selectListFilter('remarked')"
          >
            <span>{{ t('myMails.filters.remarkedOnly') }}</span>
          </button>
        </li>
      </ul>
      <el-date-picker
        v-model="receivedRange"
        type="daterange"
        value-format="YYYY-MM-DD"
        unlink-panels
        class="mail-mailbox-menu__dates"
        :disabled="!hasMailbox"
        :start-placeholder="t('myMails.filters.fromDate')"
        :end-placeholder="t('myMails.filters.toDate')"
        @change="search"
      />
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useMyMailsWorkspace } from '@/composables/useMyMailsWorkspace'
import { profileMailboxLocation } from '@/utils/profileMailboxLink'
import fnWriteIcon from '@/assets/icons/mail/fn-write.svg'
import fnReceiveIcon from '@/assets/icons/mail/fn-receive.svg'
import fnAddressIcon from '@/assets/icons/mail/fn-address.svg'
import fnPanelWatermark from '@/assets/icons/mail/fn-panel-watermark.svg'

const { t } = useI18n()
const {
  mailboxId,
  mailboxOptions,
  mailboxesLoaded,
  hasMailbox,
  onMailboxChange,
  folderId,
  viewMode,
  readFilter,
  receivedRange,
  summary,
  syncing,
  selectListFilter,
  search,
  selectFolder,
  startCompose,
  receiveSelectedMailbox,
  openAddressBook
} = useMyMailsWorkspace()
</script>

<style scoped lang="scss">
@use '@/assets/styles/variables' as *;

.mail-mailbox-menu {
  padding: 4px 2px 12px;
  font-size: 12px;
  color: $text-secondary;
}

.mail-mailbox-menu__group {
  margin-bottom: 14px;
}

.mail-mailbox-menu__panel {
  position: relative;
  background: #f8f9fb;
  border: 1px solid #ebeef5;
  border-radius: 8px;
  padding: 10px 12px;
  overflow: hidden;
}

.mail-mailbox-menu__panel--fn {
  background-repeat: no-repeat;
  background-position: right 6px center;
  background-size: 76px auto;
}

.mail-mailbox-menu__panel.is-disabled {
  opacity: 0.55;
}

.mail-mailbox-menu__setup {
  display: flex;
  align-items: center;
  min-height: 32px;
  font-size: 13px;
  color: var(--el-color-primary);
  text-decoration: none;

  &:hover {
    text-decoration: underline;
  }
}

.mail-mailbox-menu__select,
.mail-mailbox-menu__dates {
  width: 100%;
}

.mail-mailbox-menu__select {
  :deep(.el-select__selected-item),
  :deep(.el-select__placeholder),
  :deep(.el-select__input),
  :deep(.el-select__wrapper) {
    font-weight: 700;
  }
}

.mail-mailbox-menu__actions {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
}

.mail-mailbox-menu__actions > li + li {
  border-top: 1px solid #e6e6e6;
}

.mail-mailbox-menu__action {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 12px;
  text-align: left;
  padding: 10px 8px;
  font-family: 'Microsoft YaHei', 'PingFang SC', 'Noto Sans SC', sans-serif;
  font-size: 14px;
  font-weight: 700;
  letter-spacing: 0.02em;
  color: #1a3a5c;
  text-shadow: 0 0 6px rgba(150, 198, 230, 0.55);
  background: transparent;
  border: 1px solid transparent;
  border-radius: 4px;
  cursor: pointer;

  &:hover:not(:disabled) {
    background: var(--crm-accent-008);
  }

  &.is-active {
    background: var(--crm-accent-012);
    border-color: var(--crm-accent-04);
  }

  &:disabled {
    opacity: 0.45;
    cursor: not-allowed;
  }
}

.mail-mailbox-menu__action-icon {
  width: 28px;
  height: 28px;
  flex-shrink: 0;
  display: block;
}

.mail-mailbox-menu__list {
  list-style: none;
  margin: 0 0 10px;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.mail-mailbox-menu__group:last-child .mail-mailbox-menu__list {
  margin-bottom: 10px;
}

.mail-mailbox-menu__item {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: flex-start;
  text-align: left;
  padding: 7px 4px;
  font-size: 13px;
  color: #2c4c5b;
  background: transparent;
  border: 1px solid transparent;
  border-radius: 4px;
  cursor: pointer;

  &:hover:not(:disabled) {
    background: var(--crm-accent-008);
    border-color: var(--crm-accent-018);
    color: $text-primary;
  }

  &.is-active {
    background: #e5fbff;
    border-color: var(--crm-accent-035);
    color: $text-primary;
    font-weight: 700;
  }

  &.is-disabled,
  &:disabled {
    opacity: 0.45;
    cursor: not-allowed;
  }
}

.mail-mailbox-menu__radios {
  display: flex;
  flex-wrap: wrap;
  margin-bottom: 10px;

  :deep(.el-radio-button__inner) {
    padding: 5px 8px;
    font-size: 12px;
  }
}
</style>
