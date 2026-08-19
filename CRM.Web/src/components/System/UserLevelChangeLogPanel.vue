<template>
  <div class="so-item-ops-root so-item-ops-root--embedded" aria-label="user-level-change-log">
    <div v-if="!row" class="so-item-ops-root__empty">
      {{ t('systemUserLevel.logPickRow') }}
    </div>
    <div v-else v-loading="loading" class="so-item-ops-root__content so-item-ops-root__content--embedded">
      <p v-if="loadError" class="so-item-ops-root__error">{{ loadError }}</p>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('systemUserLevel.userInfoTitle') }}</h3>
        </header>
        <div class="ops-card__body">
          <div class="level-change-pair">
            <div class="ops-kv">
              <span class="ops-kv__label">{{ t('systemUserLevel.colUserName') }}</span>
              <span class="ops-kv__sep">：</span>
              <span class="ops-kv__value">{{ row.userName || '—' }}</span>
            </div>
            <div class="ops-kv">
              <span class="ops-kv__label">{{ t('systemUserLevel.colRealName') }}</span>
              <span class="ops-kv__sep">：</span>
              <span class="ops-kv__value">{{ row.realName || '—' }}</span>
            </div>
          </div>
          <div class="ops-kv">
            <span class="ops-kv__label">{{ t('systemUserLevel.colDept') }}</span>
            <span class="ops-kv__sep">：</span>
            <span class="ops-kv__value">{{ row.primaryDepartmentName || '—' }}</span>
          </div>
          <div class="ops-kv">
            <span class="ops-kv__label">{{ t('systemUserLevel.colLevel') }}</span>
            <span class="ops-kv__sep">：</span>
            <span class="ops-kv__value">{{ row.level ?? 1 }}</span>
          </div>
        </div>
      </section>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('systemUserLevel.changePanelTitle') }}</h3>
        </header>
        <div class="ops-card__body">
          <p v-if="!history.length" class="so-item-ops-root__empty">{{ t('systemUserLevel.historyEmpty') }}</p>
          <div v-for="item in history" :key="item.id" class="level-change-row">
            <div class="level-change-pair">
              <div class="ops-kv">
                <span class="ops-kv__label">{{ t('systemUserLevel.colUserName') }}</span>
                <span class="ops-kv__sep">：</span>
                <span class="ops-kv__value">{{ item.userName || row.userName || '—' }}</span>
              </div>
              <div class="ops-kv">
                <span class="ops-kv__label">{{ t('systemUserLevel.colRealName') }}</span>
                <span class="ops-kv__sep">：</span>
                <span class="ops-kv__value">{{ row.realName || '—' }}</span>
              </div>
            </div>
            <div class="level-change-pair">
              <div class="ops-kv">
                <span class="ops-kv__label">{{ t('systemUserLevel.colChangedAt') }}</span>
                <span class="ops-kv__sep">：</span>
                <span class="ops-kv__value">{{ formatTime(item.changeTime) }}</span>
              </div>
              <div class="ops-kv">
                <span class="ops-kv__label">{{ t('systemUserLevel.operator') }}</span>
                <span class="ops-kv__sep">：</span>
                <span class="ops-kv__value">{{ item.operatorUserName || '—' }}</span>
              </div>
            </div>
            <div class="ops-kv">
              <span class="ops-kv__label">{{ t('systemUserLevel.changeLevel') }}</span>
              <span class="ops-kv__sep">：</span>
              <span class="ops-kv__value">{{ item.oldLevel }} → {{ item.newLevel }}</span>
            </div>
            <div v-if="item.remark?.trim()" class="ops-kv">
              <span class="ops-kv__label">{{ t('systemUserLevel.colRemark') }}</span>
              <span class="ops-kv__sep">：</span>
              <span class="ops-kv__value">{{ item.remark }}</span>
            </div>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { useI18n } from 'vue-i18n'
import { useUserLevelLogStore } from '@/stores/userLevelLog'
import { formatDisplayDateTime } from '@/utils/displayDateTime'

const { t } = useI18n()
const store = useUserLevelLogStore()
const { row, history, loading, loadError } = storeToRefs(store)

function formatTime(v?: string) {
  return formatDisplayDateTime(v) || '—'
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/so-item-ops-panel.scss';

.so-item-ops-root--embedded .ops-kv {
  display: flex;
  align-items: baseline;
  justify-content: flex-start;
  gap: 0;
  padding: 4px 0;
  font-size: 13px;
}

.so-item-ops-root--embedded .ops-kv__label,
.so-item-ops-root--embedded .ops-kv__sep {
  flex: 0 0 auto;
  color: $text-muted;
  text-align: left;
}

.so-item-ops-root--embedded .ops-kv__value {
  flex: 1 1 auto;
  max-width: none;
  color: $text-primary;
  text-align: left;
  font-weight: 400;
  word-break: break-all;
}

.level-change-pair {
  display: flex;
  align-items: baseline;
  gap: 12px;
  min-width: 0;
}

.level-change-pair .ops-kv {
  flex: 1 1 0;
  min-width: 0;
}

.level-change-row + .level-change-row {
  margin-top: 10px;
  padding-top: 10px;
  border-top: 1px solid $border-panel;
}
</style>
