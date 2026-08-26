<template>
  <div class="report-params-page">
    <div class="page-header">
      <div class="header-left">
        <h2 class="page-title">{{ t('reportParams.pageTitle') }}</h2>
        <p class="page-sub">{{ t('reportParams.pageSubtitle') }}</p>
      </div>
    </div>

    <div class="settings-body">
      <div class="settings-nav" aria-label="report-params-nav">
        <div class="nav-group-label">{{ t('reportParams.navTitle') }}</div>
        <router-link
          v-if="canAccessSystemPermission('system.params.report.global.read')"
          to="/system/report-params/global"
          class="nav-item"
          active-class="active"
        >
          <el-icon class="nav-icon"><Setting /></el-icon>
          <span>{{ t('reportParams.globalNav') }}</span>
        </router-link>
      </div>

      <div class="settings-content">
        <router-view />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { Setting } from '@element-plus/icons-vue'
import { useAuthStore } from '@/stores'

const { t } = useI18n()
const authStore = useAuthStore()
const canAccessSystemPermission = authStore.canAccessSystemPermission
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.report-params-page {
  padding: 20px;
  min-height: 100%;
}

.page-header {
  margin-bottom: 20px;
  .page-title {
    font-size: 18px;
    font-weight: 600;
    color: $text-primary;
    margin: 0 0 6px;
  }
  .page-sub {
    margin: 0;
    font-size: 13px;
    color: $text-muted;
    line-height: 1.5;
  }
}

.settings-body {
  display: flex;
  gap: 16px;
  align-items: flex-start;
}

.settings-nav {
  width: 200px;
  flex-shrink: 0;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: 8px;
  padding: 12px 0;
}

.nav-group-label {
  padding: 0 16px 8px;
  font-size: 11px;
  font-weight: 600;
  color: $text-muted;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 16px;
  font-size: 13px;
  color: $text-secondary;
  text-decoration: none;
  transition: background 0.15s, color 0.15s;

  &:hover {
    background: rgba(0, 212, 255, 0.06);
    color: $text-secondary;
  }

  &.active {
    background: rgba(0, 212, 255, 0.18);
    color: $cyan-primary;
    font-weight: 500;
    border-right: 2px solid $cyan-primary;
  }
}

.nav-icon {
  font-size: 16px;
}

.settings-content {
  flex: 1;
  min-width: 0;
}
</style>
