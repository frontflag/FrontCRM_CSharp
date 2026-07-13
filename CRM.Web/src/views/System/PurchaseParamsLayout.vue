<template>
  <div class="purchase-params-page">
    <div class="page-header">
      <div class="header-left">
        <h2 class="page-title">{{ t('purchaseParams.pageTitle') }}</h2>
        <p class="page-sub">{{ t('purchaseParams.pageSubtitle') }}</p>
      </div>
    </div>

    <div class="settings-body">
      <div class="settings-nav" aria-label="purchase-params-nav">
        <div class="nav-group-label">{{ t('purchaseParams.navTitle') }}</div>
        <router-link
          to="/system/purchase-params/assignee-count"
          class="nav-item"
          active-class="active"
        >
          <el-icon class="nav-icon"><User /></el-icon>
          <span>{{ t('purchaseParams.assigneeCountNav') }}</span>
        </router-link>
        <router-link
          to="/system/purchase-params/quoter-pool"
          class="nav-item"
          active-class="active"
        >
          <el-icon class="nav-icon"><List /></el-icon>
          <span>{{ t('purchaseParams.quoterPoolNav') }}</span>
        </router-link>
        <router-link
          to="/system/purchase-params/demand-protection"
          class="nav-item"
          active-class="active"
        >
          <el-icon class="nav-icon"><Timer /></el-icon>
          <span>{{ t('purchaseParams.demandProtectionNav') }}</span>
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
import { User, List, Timer } from '@element-plus/icons-vue'

const { t } = useI18n()
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.purchase-params-page {
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
