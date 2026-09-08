<template>
  <div class="commission-params-page">
    <div class="page-header">
      <div class="header-left">
        <h2 class="page-title">{{ t('commissionParams.pageTitle') }}</h2>
        <p class="page-sub">{{ t('commissionParams.pageSubtitle') }}</p>
      </div>
    </div>

    <div class="settings-body">
      <div class="settings-nav" aria-label="commission-params-nav">
        <div class="nav-group-label">{{ t('commissionParams.navTitle') }}</div>
        <router-link
          to="/system/commission-params/settings"
          class="nav-item"
          active-class="active"
        >
          <el-icon class="nav-icon"><Setting /></el-icon>
          <span>{{ t('commissionParams.settingsNav') }}</span>
        </router-link>
        <router-link
          v-if="canSales"
          to="/system/commission-params/sales"
          class="nav-item"
          active-class="active"
        >
          <el-icon class="nav-icon"><User /></el-icon>
          <span>{{ t('commissionParams.salesNav') }}</span>
        </router-link>
        <router-link
          v-if="canPurchase"
          to="/system/commission-params/purchase"
          class="nav-item"
          active-class="active"
        >
          <el-icon class="nav-icon"><ShoppingCart /></el-icon>
          <span>{{ t('commissionParams.purchaseNav') }}</span>
        </router-link>
      </div>

      <div class="settings-content">
        <router-view />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { Setting, ShoppingCart, User } from '@element-plus/icons-vue'
import { useAuthStore } from '@/stores'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const canSales = computed(() => authStore.canForceDelete())
const canPurchase = computed(() => authStore.canForceDelete())

onMounted(() => {
  if (route.path === '/system/commission-params' || route.path === '/system/commission-params/') {
    router.replace('/system/commission-params/settings')
  }
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.commission-params-page {
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
  padding: 8px;

  .nav-group-label {
    padding: 6px 14px 10px;
    font-size: 12px;
    font-weight: 600;
    color: $text-muted;
    border-bottom: 1px solid $border-panel;
    margin-bottom: 6px;
  }

  .nav-item {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 10px 14px;
    border-radius: 6px;
    cursor: pointer;
    color: $text-muted;
    font-size: 13px;
    text-decoration: none;
    transition: all 0.2s;

    .nav-icon {
      font-size: 16px;
    }

    &:hover {
      background: rgba(0, 212, 255, 0.06);
      color: $text-secondary;
    }

    &.active {
      background: rgba(0, 212, 255, 0.18);
      color: $cyan-primary;
      font-weight: 500;
    }
  }
}

.settings-content {
  flex: 1;
  min-width: 0;
}
</style>
