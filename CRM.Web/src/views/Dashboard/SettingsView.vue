<template>
  <div class="settings-page">
    <!-- 页面标题 -->
    <div class="page-header">
      <div class="header-left">
        <h2 class="page-title">{{ t('dashboardSettings.title') }}</h2>
      </div>
    </div>

    <!-- 设置内容 -->
    <div class="settings-body">
      <!-- 左侧导航 -->
      <div class="settings-nav">
        <div
          v-for="tab in tabs"
          :key="tab.key"
          class="nav-item"
          :class="{ active: activeTab === tab.key }"
          @click="activeTab = tab.key"
        >
          <el-icon class="nav-icon"><component :is="tab.icon" /></el-icon>
          <span>{{ tab.label }}</span>
        </div>
      </div>

      <!-- 右侧内容 -->
      <div class="settings-content">
        <!-- 基本信息 -->
        <div v-if="activeTab === 'basic'" class="form-section">
          <div class="section-title">
            <span class="title-bar"></span>{{ t('dashboardSettings.tabs.basic') }}
          </div>
          <el-form :model="basicSettings" label-width="120px" class="settings-form">
            <el-row :gutter="24">
              <el-col :span="12">
                <el-form-item :label="t('dashboardSettings.basic.systemName')">
                  <el-input v-model="basicSettings.systemName" :placeholder="t('dashboardSettings.basic.systemNamePlaceholder')" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('dashboardSettings.basic.systemVersion')">
                  <el-input v-model="basicSettings.systemVersion" :placeholder="t('dashboardSettings.basic.systemVersionPlaceholder')" />
                </el-form-item>
              </el-col>
            </el-row>
            <el-row :gutter="24">
              <el-col :span="12">
                <el-form-item :label="t('dashboardSettings.basic.contactEmail')">
                  <el-input v-model="basicSettings.contactEmail" type="email" :placeholder="t('dashboardSettings.basic.contactEmailPlaceholder')" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('dashboardSettings.basic.contactPhone')">
                  <el-input v-model="basicSettings.contactPhone" :placeholder="t('dashboardSettings.basic.contactPhonePlaceholder')" />
                </el-form-item>
              </el-col>
            </el-row>
            <el-row :gutter="24">
              <el-col :span="24">
                <el-form-item :label="t('dashboardSettings.basic.description')">
                  <el-input
                    v-model="basicSettings.description"
                    type="textarea"
                    :rows="3"
                    :placeholder="t('dashboardSettings.basic.descriptionPlaceholder')"
                  />
                </el-form-item>
              </el-col>
            </el-row>
            <el-form-item>
              <el-button type="primary" @click="handleSaveBasic">
                <el-icon><Check /></el-icon> {{ t('dashboardSettings.save') }}
              </el-button>
            </el-form-item>
          </el-form>
        </div>

        <!-- 安全设置 -->
        <div v-if="activeTab === 'security'" class="form-section">
          <div class="section-title">
            <span class="title-bar"></span>{{ t('dashboardSettings.tabs.security') }}
          </div>
          <el-form :model="securitySettings" label-width="150px" class="settings-form">
            <el-form-item :label="t('dashboardSettings.security.passwordComplexity')">
              <div class="switch-row">
                <el-switch v-model="securitySettings.requireComplexPassword" />
                <span class="switch-desc">{{ t('dashboardSettings.security.passwordComplexityHint') }}</span>
              </div>
            </el-form-item>
            <el-form-item :label="t('dashboardSettings.security.minPasswordLength')">
              <el-input-number v-model="securitySettings.minPasswordLength" :min="6" :max="20" />
            </el-form-item>
            <el-form-item :label="t('dashboardSettings.security.enableLockout')">
              <el-switch v-model="securitySettings.enableLockout" />
            </el-form-item>
            <el-form-item :label="t('dashboardSettings.security.maxFailedAttempts')">
              <el-input-number
                v-model="securitySettings.maxFailedAttempts"
                :min="3" :max="10"
                :disabled="!securitySettings.enableLockout"
              />
            </el-form-item>
            <el-form-item :label="t('dashboardSettings.security.lockoutDuration')">
              <el-input-number
                v-model="securitySettings.lockoutDuration"
                :min="5" :max="60"
                :disabled="!securitySettings.enableLockout"
              />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="handleSaveSecurity">
                <el-icon><Check /></el-icon> {{ t('dashboardSettings.save') }}
              </el-button>
            </el-form-item>
          </el-form>
        </div>

        <!-- 通知设置 -->
        <div v-if="activeTab === 'notification'" class="form-section">
          <div class="section-title">
            <span class="title-bar"></span>{{ t('dashboardSettings.tabs.notification') }}
          </div>
          <el-form :model="notificationSettings" label-width="150px" class="settings-form">
            <el-form-item :label="t('dashboardSettings.notification.emailEnabled')">
              <el-switch v-model="notificationSettings.emailEnabled" />
            </el-form-item>
            <el-form-item :label="t('dashboardSettings.notification.newCustomerNotification')">
              <el-switch v-model="notificationSettings.newCustomerNotification" />
            </el-form-item>
            <el-form-item :label="t('dashboardSettings.notification.systemUpdateNotification')">
              <el-switch v-model="notificationSettings.systemUpdateNotification" />
            </el-form-item>
            <el-form-item :label="t('dashboardSettings.notification.securityAlertNotification')">
              <el-switch v-model="notificationSettings.securityAlertNotification" />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="handleSaveNotification">
                <el-icon><Check /></el-icon> {{ t('dashboardSettings.save') }}
              </el-button>
            </el-form-item>
          </el-form>
        </div>

        <!-- 关于 -->
        <div v-if="activeTab === 'about'" class="form-section">
          <div class="section-title">
            <span class="title-bar"></span>{{ t('dashboardSettings.tabs.about') }}
          </div>
          <div class="about-section">
            <div class="about-logo">
              <svg viewBox="0 0 40 40" fill="none" xmlns="http://www.w3.org/2000/svg" width="48" height="48">
                <polygon points="20,2 36,11 36,29 20,38 4,29 4,11" fill="none" stroke="#00D4FF" stroke-width="1.5"/>
                <polygon points="20,8 30,14 30,26 20,32 10,26 10,14" fill="rgba(0,212,255,0.1)" stroke="#0066FF" stroke-width="1"/>
                <circle cx="20" cy="20" r="5" fill="#00D4FF" opacity="0.9"/>
              </svg>
              <div class="about-brand">
                <h3>FrontCRM</h3>
                <p>{{ t('layout.brandSub') }}</p>
              </div>
            </div>
            <p class="about-desc">{{ t('dashboardSettings.about.description') }}</p>

            <div class="info-grid">
              <div class="info-item">
                <span class="label">{{ t('dashboardSettings.about.systemVersion') }}</span>
                <span class="value">1.0.0</span>
              </div>
              <div class="info-item">
                <span class="label">{{ t('dashboardSettings.about.frontend') }}</span>
                <span class="value">Vue 3 + TypeScript + Vite</span>
              </div>
              <div class="info-item">
                <span class="label">{{ t('dashboardSettings.about.backend') }}</span>
                <span class="value">ASP.NET Core 9.0</span>
              </div>
              <div class="info-item">
                <span class="label">{{ t('dashboardSettings.about.ui') }}</span>
                <span class="value">Element Plus</span>
              </div>
              <div class="info-item">
                <span class="label">{{ t('dashboardSettings.about.database') }}</span>
                <span class="value">SQL Server / PostgreSQL</span>
              </div>
              <div class="info-item">
                <span class="label">{{ t('dashboardSettings.about.copyright') }}</span>
                <span class="value">© 2026 FrontCRM. All rights reserved.</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElNotification } from 'element-plus'
import { Setting, Lock, Bell, InfoFilled, Check } from '@element-plus/icons-vue'

const { t, locale } = useI18n()

const activeTab = ref('basic')

const tabs = computed(() => {
  void locale.value
  return [
    { key: 'basic' as const, label: t('dashboardSettings.tabs.basic'), icon: Setting },
    { key: 'security' as const, label: t('dashboardSettings.tabs.security'), icon: Lock },
    { key: 'notification' as const, label: t('dashboardSettings.tabs.notification'), icon: Bell },
    { key: 'about' as const, label: t('dashboardSettings.tabs.about'), icon: InfoFilled }
  ]
})

const basicSettings = reactive({
  systemName: 'FrontCRM',
  systemVersion: '1.0.0',
  contactEmail: 'admin@frontcrm.com',
  contactPhone: '400-123-4567',
  description: '一个基于 Vue 3 和 .NET 的现代化客户关系管理系统'
})

const securitySettings = reactive({
  requireComplexPassword: true,
  minPasswordLength: 8,
  enableLockout: true,
  maxFailedAttempts: 5,
  lockoutDuration: 30
})

const notificationSettings = reactive({
  emailEnabled: true,
  newCustomerNotification: true,
  systemUpdateNotification: true,
  securityAlertNotification: true
})

const handleSaveBasic = () => {
  ElNotification.success({ title: t('common.saveSuccess'), message: t('dashboardSettings.messages.basicSaved') })
}

const handleSaveSecurity = () => {
  ElNotification.success({ title: t('common.saveSuccess'), message: t('dashboardSettings.messages.securitySaved') })
}

const handleSaveNotification = () => {
  ElNotification.success({ title: t('common.saveSuccess'), message: t('dashboardSettings.messages.notificationSaved') })
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.settings-page {
  padding: 20px;
  min-height: 100%;
}

.page-header {
  margin-bottom: 20px;
  .page-title {
    font-size: 18px;
    font-weight: 600;
    color: $text-primary;
    margin: 0;
  }
}

.settings-body {
  display: flex;
  gap: 16px;
  align-items: flex-start;
}

// 左侧导航
.settings-nav {
  width: 180px;
  flex-shrink: 0;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: 8px;
  padding: 8px;

  .nav-item {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 10px 14px;
    border-radius: 6px;
    cursor: pointer;
    color: $text-muted;
    font-size: 13px;
    transition: all 0.2s;

    .nav-icon {
      font-size: 16px;
    }

    &:hover {
      background: rgba(0, 212, 255, 0.06);
      color: $text-secondary;
    }

    &.active {
      background: rgba(0, 212, 255, 0.1);
      color: $cyan-primary;
      font-weight: 500;
    }
  }
}

// 右侧内容
.settings-content {
  flex: 1;
  min-width: 0;
}

.form-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: 8px;
  padding: 20px 24px;
}

.section-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 24px;

  .title-bar {
    width: 3px;
    height: 16px;
    background: linear-gradient(180deg, #00c8ff, #0066cc);
    border-radius: 2px;
    flex-shrink: 0;
  }
}

.settings-form {
  :deep(.el-form-item__label) {
    color: $text-muted;
    font-size: 13px;
  }

  :deep(.el-input__wrapper),
  :deep(.el-textarea__inner) {
    background: $layer-3;
    border-color: $border-panel;
    box-shadow: none;
    &:hover { border-color: rgba(0, 212, 255, 0.35); }
    &.is-focus { border-color: $cyan-primary; }
  }

  :deep(.el-input__inner),
  :deep(.el-textarea__inner) {
    color: $text-primary;
    background: transparent;
    &::placeholder { color: $text-placeholder; }
  }

  :deep(.el-input-number .el-input__wrapper) {
    background: $layer-3;
    border-color: $border-panel;
  }

  :deep(.el-switch.is-checked .el-switch__core) {
    background-color: #00d4ff;
    border-color: #00d4ff;
  }

  .switch-row {
    display: flex;
    align-items: center;
    gap: 12px;

    .switch-desc {
      color: $text-muted;
      font-size: 12px;
    }
  }
}

// 关于页面
.about-section {
  .about-logo {
    display: flex;
    align-items: center;
    gap: 16px;
    margin-bottom: 16px;

    .about-brand {
      h3 {
        font-size: 20px;
        font-weight: 700;
        color: $text-primary;
        margin: 0 0 4px;
      }
      p {
        font-size: 12px;
        color: $text-muted;
        margin: 0;
      }
    }
  }

  .about-desc {
    color: $text-secondary;
    font-size: 13px;
    margin-bottom: 24px;
    line-height: 1.6;
  }

  .info-grid {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 12px;

    .info-item {
      background: rgba(0, 212, 255, 0.03);
      border: 1px solid $border-panel;
      border-radius: 6px;
      padding: 12px 16px;
      display: flex;
      flex-direction: column;
      gap: 4px;

      .label {
        font-size: 11px;
        color: $text-muted;
        text-transform: uppercase;
        letter-spacing: 0.5px;
      }

      .value {
        font-size: 13px;
        color: $text-secondary;
        font-weight: 500;
      }
    }
  }
}
</style>
