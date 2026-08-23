<template>
  <div class="mail-inbox-cards">
    <div class="mail-inbox-cards__head">{{ t('myMails.cardList.title') }}</div>
    <div v-if="loading" class="mail-inbox-cards__hint">{{ t('myMails.cardList.loading') }}</div>
    <div v-else-if="rows.length === 0" class="mail-inbox-cards__hint">{{ t('myMails.cardList.empty') }}</div>
    <ul v-else class="mail-inbox-cards__list">
      <li
        v-for="row in rows"
        :key="row.id"
        class="mail-inbox-cards__item"
        :class="{
          'is-active': selectedId === row.id,
          'is-unread': row.isUnread
        }"
        @click="selectRow(row)"
        @dblclick="openBody(row)"
      >
        <img
          class="mail-inbox-cards__icon"
          :src="row.isUnread ? mailUnreadIcon : mailReadIcon"
          :alt="row.isUnread ? t('myMails.columns.unread') : t('myMails.columns.read')"
        />
        <div class="mail-inbox-cards__main">
          <div class="mail-inbox-cards__from">{{ formatMailFrom(row) }}</div>
          <div class="mail-inbox-cards__subject">{{ row.subject || '—' }}</div>
          <div class="mail-inbox-cards__date">{{ formatMailAt(row.receivedAt) }}</div>
        </div>
      </li>
    </ul>
  </div>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import {
  formatMailAt,
  formatMailFrom,
  useMyMailsWorkspace
} from '@/composables/useMyMailsWorkspace'
import mailReadIcon from '@/assets/icons/mail/mail-read.svg'
import mailUnreadIcon from '@/assets/icons/mail/mail-unread.svg'

const { t } = useI18n()
const { rows, loading, selectedId, selectRow, openBody } = useMyMailsWorkspace()
</script>

<style scoped lang="scss">
@use '@/assets/styles/variables' as *;

.mail-inbox-cards {
  padding: 4px 2px 12px;
  font-size: 12px;
}

.mail-inbox-cards__head {
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 10px;
  font-size: 13px;
}

.mail-inbox-cards__hint {
  color: $text-muted;
  padding: 8px 4px;
}

.mail-inbox-cards__list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.mail-inbox-cards__item {
  display: flex;
  gap: 8px;
  padding: 8px 10px;
  border: 1px solid $border-panel;
  border-radius: 8px;
  background: $layer-3;
  cursor: pointer;

  &:hover {
    border-color: var(--crm-accent-03);
  }

  &.is-active {
    border-color: var(--crm-accent-06);
    background: var(--crm-accent-008);
  }

  &.is-unread .mail-inbox-cards__from,
  &.is-unread .mail-inbox-cards__subject {
    font-weight: 600;
    color: $text-primary;
  }
}

.mail-inbox-cards__icon {
  width: 18px;
  height: 18px;
  margin-top: 1px;
  flex-shrink: 0;
  display: block;
}

.mail-inbox-cards__main {
  min-width: 0;
  flex: 1;
}

.mail-inbox-cards__from {
  color: $text-primary;
  margin-bottom: 2px;
}

.mail-inbox-cards__subject,
.mail-inbox-cards__date {
  color: $text-muted;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.mail-inbox-cards__date {
  margin-top: 2px;
  font-size: 11px;
}
</style>
