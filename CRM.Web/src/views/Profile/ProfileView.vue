<template>
  <div class="profile-page">
    <div class="profile-header">
      <div class="profile-avatar-wrap">
        <div class="profile-avatar">{{ userInitial }}</div>
        <div class="avatar-edit-btn" :title="t('profilePage.changeAvatar')">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M23 19a2 2 0 01-2 2H3a2 2 0 01-2-2V8a2 2 0 012-2h4l2-3h6l2 3h4a2 2 0 012 2z"/>
            <circle cx="12" cy="13" r="4"/>
          </svg>
        </div>
      </div>
      <div class="profile-header-info">
        <h2 class="profile-name">{{ form.userName }}</h2>
        <p class="profile-email">{{ form.email }}</p>
        <span class="profile-role-badge">{{ roleLabel }}</span>
      </div>
    </div>

    <!-- Tab 导航 -->
    <div class="profile-tabs">
      <button
        v-for="tab in tabList"
        :key="tab.key"
        class="profile-tab"
        :class="{ active: activeTab === tab.key }"
        @click="activeTab = tab.key"
      >
        <span class="tab-icon" v-html="tab.icon"></span>
        {{ tab.label }}
      </button>
    </div>

    <!-- 基本信息 -->
    <div class="tab-content" v-if="activeTab === 'basic'">
      <div class="section-card">
        <h3 class="section-title">{{ t('profilePage.basic.sectionTitle') }}</h3>
        <div class="form-grid">
          <div class="form-item">
            <label class="form-label">{{ t('profilePage.basic.userName') }}</label>
            <input
              v-model="form.userName"
              class="form-input form-input--readonly"
              readonly
              tabindex="-1"
              :title="t('profilePage.basic.readonlyHint')"
            />
          </div>
          <div class="form-item">
            <label class="form-label">{{ t('profilePage.basic.email') }}</label>
            <input
              v-model="form.email"
              class="form-input form-input--readonly"
              type="email"
              readonly
              tabindex="-1"
              :title="t('profilePage.basic.readonlyHint')"
            />
          </div>
          <div class="form-item">
            <label class="form-label">{{ t('profilePage.basic.phone') }}</label>
            <input
              v-model="form.phone"
              class="form-input form-input--readonly"
              readonly
              tabindex="-1"
              :title="t('profilePage.basic.readonlyHint')"
            />
          </div>
          <div class="form-item">
            <label class="form-label">{{ t('profilePage.basic.department') }}</label>
            <input
              v-model="form.department"
              class="form-input form-input--readonly"
              readonly
              tabindex="-1"
              :title="t('profilePage.basic.readonlyHint')"
            />
          </div>
          <div class="form-item full-width">
            <label class="form-label">{{ t('profilePage.basic.bio') }}</label>
            <textarea v-model="form.bio" class="form-textarea" :placeholder="t('profilePage.basic.phBio')" rows="3"></textarea>
          </div>
        </div>
        <div class="form-actions">
          <button class="btn-primary" @click="saveBasicInfo">{{ t('profilePage.basic.saveChanges') }}</button>
        </div>
      </div>
    </div>

    <!-- 修改密码 -->
    <div class="tab-content" v-if="activeTab === 'password'">
      <div class="section-card">
        <h3 class="section-title">{{ t('profilePage.password.sectionTitle') }}</h3>
        <div class="form-grid single-col">
          <div class="form-item">
            <label class="form-label">{{ t('profilePage.password.current') }}</label>
            <div class="input-with-eye">
              <input
                v-model="pwdForm.current"
                :type="showPwd.current ? 'text' : 'password'"
                class="form-input"
                :placeholder="t('profilePage.password.phCurrent')"
              />
              <button class="eye-btn" @click="showPwd.current = !showPwd.current">
                <svg v-if="!showPwd.current" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                  <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                  <circle cx="12" cy="12" r="3"/>
                </svg>
                <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                  <path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19m-6.72-1.07a3 3 0 11-4.24-4.24"/>
                  <line x1="1" y1="1" x2="23" y2="23"/>
                </svg>
              </button>
            </div>
          </div>
          <div class="form-item">
            <label class="form-label">{{ t('profilePage.password.newPwd') }}</label>
            <div class="input-with-eye">
              <input
                v-model="pwdForm.newPwd"
                :type="showPwd.newPwd ? 'text' : 'password'"
                class="form-input"
                :placeholder="t('profilePage.password.phNew')"
                @input="calcStrength"
              />
              <button class="eye-btn" @click="showPwd.newPwd = !showPwd.newPwd">
                <svg v-if="!showPwd.newPwd" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                  <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                  <circle cx="12" cy="12" r="3"/>
                </svg>
                <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                  <path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19m-6.72-1.07a3 3 0 11-4.24-4.24"/>
                  <line x1="1" y1="1" x2="23" y2="23"/>
                </svg>
              </button>
            </div>
            <!-- 密码强度条 -->
            <div class="strength-bar" v-if="pwdForm.newPwd">
              <div class="strength-track">
                <div class="strength-fill" :class="strengthClass" :style="{ width: strengthWidth }"></div>
              </div>
              <span class="strength-label" :class="strengthClass">{{ strengthText }}</span>
            </div>
          </div>
          <div class="form-item">
            <label class="form-label">{{ t('profilePage.password.confirm') }}</label>
            <div class="input-with-eye">
              <input
                v-model="pwdForm.confirm"
                :type="showPwd.confirm ? 'text' : 'password'"
                class="form-input"
                :class="{ 'input-error': pwdForm.confirm && pwdForm.confirm !== pwdForm.newPwd }"
                :placeholder="t('profilePage.password.phConfirm')"
              />
              <button class="eye-btn" @click="showPwd.confirm = !showPwd.confirm">
                <svg v-if="!showPwd.confirm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                  <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                  <circle cx="12" cy="12" r="3"/>
                </svg>
                <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                  <path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19m-6.72-1.07a3 3 0 11-4.24-4.24"/>
                  <line x1="1" y1="1" x2="23" y2="23"/>
                </svg>
              </button>
            </div>
            <p class="input-error-msg" v-if="pwdForm.confirm && pwdForm.confirm !== pwdForm.newPwd">{{ t('profilePage.password.mismatch') }}</p>
          </div>
        </div>
        <div class="form-actions">
          <button class="btn-primary" @click="changePassword" :disabled="!canChangePassword">{{ t('profilePage.password.update') }}</button>
        </div>
      </div>
    </div>

    <!-- 微信绑定 -->
    <div class="tab-content" v-if="activeTab === 'wechat'">
      <div class="section-card">
        <h3 class="section-title">{{ t('profilePage.wechat.sectionTitle') }}</h3>
        <div class="wechat-bind-section">
          <div v-if="wechatBindInfo.isBound" class="wechat-bound">
            <div class="wechat-info">
              <el-avatar :size="64" :src="wechatBindInfo.avatarUrl" />
              <div class="wechat-detail">
                <p class="nickname">{{ wechatBindInfo.nickname }}</p>
                <p class="bind-time">{{ t('profilePage.wechat.boundAt') }}{{ formatDate(wechatBindInfo.bindTime) }}</p>
              </div>
            </div>
            <el-button type="danger" @click="unbindWechatHandler">{{ t('profilePage.wechat.unbind') }}</el-button>
          </div>
          <div v-else class="wechat-unbound">
            <div class="bind-intro">
              <el-icon :size="48" color="#07C160"><ChatDotRound /></el-icon>
              <p class="bind-text">{{ t('profilePage.wechat.intro') }}</p>
            </div>
            <el-button type="primary" size="large" @click="goToWechatBind">
              {{ t('profilePage.wechat.goBind') }}
            </el-button>
          </div>
        </div>
      </div>
    </div>

    <!-- 通知偏好 -->
    <div class="tab-content" v-if="activeTab === 'notifications'">
      <div class="section-card">
        <h3 class="section-title">{{ t('profilePage.notifications.sectionTitle') }}</h3>
        <div class="notify-list">
          <div class="notify-item" v-for="item in notifySettings" :key="item.key">
            <div class="notify-info">
              <span class="notify-name">{{ t(`profilePage.notifications.items.${item.key}.name`) }}</span>
              <span class="notify-desc">{{ t(`profilePage.notifications.items.${item.key}.desc`) }}</span>
            </div>
            <div class="toggle-switch" :class="{ on: item.enabled }" @click="item.enabled = !item.enabled">
              <div class="toggle-thumb"></div>
            </div>
          </div>
        </div>
        <div class="form-actions">
          <button class="btn-primary" @click="saveNotifications">{{ t('profilePage.notifications.save') }}</button>
        </div>
      </div>
    </div>

    <!-- 安全设置 -->
    <div class="tab-content" v-if="activeTab === 'security'">
      <div class="section-card">
        <h3 class="section-title">{{ t('profilePage.security.twoFactorSection') }}</h3>
        <div class="security-row">
          <div class="security-info">
            <span class="security-name">{{ t('profilePage.security.twoFactorName') }}</span>
            <span class="security-desc">{{ t('profilePage.security.twoFactorDesc') }}</span>
          </div>
          <div class="toggle-switch" :class="{ on: security.twoFactor }" @click="security.twoFactor = !security.twoFactor">
            <div class="toggle-thumb"></div>
          </div>
        </div>
      </div>
      <div class="section-card mt-16">
        <h3 class="section-title">{{ t('profilePage.security.devicesSection') }}</h3>
        <div class="device-list">
          <div class="device-item" v-for="device in devices" :key="device.id">
            <div class="device-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <rect x="2" y="3" width="20" height="14" rx="2"/>
                <line x1="8" y1="21" x2="16" y2="21"/>
                <line x1="12" y1="17" x2="12" y2="21"/>
              </svg>
            </div>
            <div class="device-info">
              <span class="device-name">{{ device.name }}</span>
              <span class="device-meta">{{ t(`profilePage.devices.${device.locKey}`) }} · {{ t(`profilePage.devices.${device.timeKey}`) }}</span>
            </div>
            <span class="device-current" v-if="device.current">{{ t('profilePage.security.currentDevice') }}</span>
            <button class="btn-ghost-danger" v-else @click="revokeDevice(device.id)">{{ t('profilePage.security.revoke') }}</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ChatDotRound } from '@element-plus/icons-vue'
import { getWechatBindInfo, unbindWechat } from '@/api/wechatAuth'
import { formatDate } from '@/utils/date'

const authStore = useAuthStore()
const router = useRouter()
const { t, locale } = useI18n()

const userInitial = computed(() => (authStore.user?.userName || 'U')[0].toUpperCase())

const roleLabel = computed(() =>
  authStore.user?.isSysAdmin ? t('profilePage.badge.sysAdmin') : t('profilePage.badge.user')
)

const activeTab = ref('basic')

const tabList = computed(() => {
  void locale.value
  return [
    {
      key: 'basic',
      label: t('profilePage.tabs.basic'),
      icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" width="15" height="15"><path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>'
    },
    {
      key: 'password',
      label: t('profilePage.tabs.password'),
      icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" width="15" height="15"><rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0110 0v4"/></svg>'
    },
    {
      key: 'wechat',
      label: t('profilePage.tabs.wechat'),
      icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" width="15" height="15"><path d="M9 11a2 2 0 1 1 0-4 2 2 0 0 1 0 4z"/><path d="M15 11a2 2 0 1 1 0-4 2 2 0 0 1 0 4z"/><path d="M12 22c5.523 0 10-4.477 10-10S17.523 2 12 2 2 6.477 2 12s4.477 10 10 10z"/></svg>'
    },
    {
      key: 'notifications',
      label: t('profilePage.tabs.notifications'),
      icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" width="15" height="15"><path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.73 21a2 2 0 01-3.46 0"/></svg>'
    },
    {
      key: 'security',
      label: t('profilePage.tabs.security'),
      icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" width="15" height="15"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/></svg>'
    }
  ]
})

// 基本信息表单（账号/邮箱/手机/部门由服务端同步，只读；简介可编辑）
const form = ref({
  userName: authStore.user?.userName || '',
  email: authStore.user?.email || '',
  phone: authStore.user?.mobile || '',
  department: authStore.user?.department || '',
  bio: ''
})

function syncBasicFormFromAuthUser() {
  const u = authStore.user
  if (!u) return
  form.value.userName = u.userName || ''
  form.value.email = u.email || ''
  form.value.phone = (u.mobile || '').trim()
  form.value.department = (u.department || '').trim()
}

const saveBasicInfo = () => {
  ElMessage.success(t('profilePage.messages.basicSaved'))
}

// 密码表单
const pwdForm = ref({ current: '', newPwd: '', confirm: '' })
const showPwd = ref({ current: false, newPwd: false, confirm: false })
const strengthLevel = ref(0)

const calcStrength = () => {
  const pwd = pwdForm.value.newPwd
  let score = 0
  if (pwd.length >= 8) score++
  if (/[A-Z]/.test(pwd)) score++
  if (/[0-9]/.test(pwd)) score++
  if (/[^A-Za-z0-9]/.test(pwd)) score++
  strengthLevel.value = score
}

const strengthClass = computed(() => {
  if (strengthLevel.value <= 1) return 'weak'
  if (strengthLevel.value === 2) return 'medium'
  if (strengthLevel.value === 3) return 'strong'
  return 'very-strong'
})

const strengthWidth = computed(() => {
  return `${(strengthLevel.value / 4) * 100}%`
})

const strengthText = computed(() => {
  const n = strengthLevel.value
  const key = n <= 1 ? 'weak' : n === 2 ? 'medium' : n === 3 ? 'strong' : 'veryStrong'
  return t(`profilePage.password.strength.${key}`)
})

const canChangePassword = computed(() => {
  return pwdForm.value.current &&
    pwdForm.value.newPwd.length >= 8 &&
    pwdForm.value.newPwd === pwdForm.value.confirm
})

const changePassword = () => {
  if (!canChangePassword.value) return
  ElMessage.success(t('profilePage.messages.passwordUpdated'))
  pwdForm.value = { current: '', newPwd: '', confirm: '' }
}

// 通知设置（文案随语言切换；enabled 独立存储）
const notifySettings = ref([
  { key: 'order' as const, enabled: true },
  { key: 'quote' as const, enabled: true },
  { key: 'finance' as const, enabled: false },
  { key: 'system' as const, enabled: true },
  { key: 'email' as const, enabled: false }
])

const saveNotifications = () => {
  ElMessage.success(t('profilePage.messages.notifySaved'))
}

// 安全设置
const security = ref({ twoFactor: false })

type DeviceRow = {
  id: number
  name: string
  locKey: 'shanghai' | 'beijing' | 'guangzhou'
  timeKey: 'justNow' | 'daysAgo2' | 'daysAgo7'
  current: boolean
}

const devices = ref<DeviceRow[]>([
  { id: 1, name: 'Chrome on Windows', locKey: 'shanghai', timeKey: 'justNow', current: true },
  { id: 2, name: 'Safari on macOS', locKey: 'beijing', timeKey: 'daysAgo2', current: false },
  { id: 3, name: 'Chrome on Android', locKey: 'guangzhou', timeKey: 'daysAgo7', current: false }
])

const revokeDevice = (id: number) => {
  devices.value = devices.value.filter((d) => d.id !== id)
  ElMessage.success(t('profilePage.messages.deviceRevoked'))
}

// 微信绑定
const wechatBindInfo = ref({
  isBound: false,
  nickname: '',
  avatarUrl: '',
  bindTime: ''
})

const fetchWechatBindInfo = async () => {
  try {
    const info = await getWechatBindInfo()
    wechatBindInfo.value = {
      isBound: info.isBound,
      nickname: info.nickname || '',
      avatarUrl: info.avatarUrl || '',
      bindTime: info.bindTime || ''
    }
  } catch (error) {
    console.error('获取微信绑定信息失败', error)
  }
}

const goToWechatBind = () => {
  router.push('/profile/wechat-bind')
}

const unbindWechatHandler = async () => {
  try {
    await ElMessageBox.confirm(t('profilePage.wechat.unbindConfirmMessage'), t('profilePage.wechat.unbindConfirmTitle'), {
      type: 'warning',
      confirmButtonText: t('common.confirm'),
      cancelButtonText: t('common.cancel')
    })
    const ok = await unbindWechat()
    if (ok) {
      ElMessage.success(t('profilePage.wechat.unbindSuccess'))
      fetchWechatBindInfo()
    }
  } catch {
    // 取消
  }
}

watch(
  () => authStore.user,
  () => syncBasicFormFromAuthUser(),
  { deep: true }
)

onMounted(async () => {
  try {
    await authStore.fetchCurrentUser()
  } catch {
    /* fetchCurrentUser 失败会登出；此处忽略 */
  }
  syncBasicFormFromAuthUser()
  await fetchWechatBindInfo()
})
</script>

<style lang="scss" scoped>
@use '@/assets/styles/variables' as vars;

.profile-page {
  max-width: 800px;
  margin: 0 auto;
  padding: 24px;
}

// 头部
.profile-header {
  display: flex;
  align-items: center;
  gap: 24px;
  padding: 24px;
  background: vars.$layer-2;
  border: 1px solid vars.$border-card;
  border-radius: 12px;
  margin-bottom: 20px;
}

.profile-avatar-wrap {
  position: relative;
  flex-shrink: 0;
}

.profile-avatar {
  width: 72px;
  height: 72px;
  border-radius: 50%;
  background: linear-gradient(135deg, #0066FF, #00D4FF);
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: 'Orbitron', monospace;
  font-size: 26px;
  font-weight: 700;
  color: #fff;
  box-shadow: 0 0 20px rgba(0, 212, 255, 0.3);
}

.avatar-edit-btn {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 24px;
  height: 24px;
  border-radius: 50%;
  background: #162233;
  border: 1px solid rgba(0, 212, 255, 0.3);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  color: rgba(0, 212, 255, 0.7);

  svg {
    width: 12px;
    height: 12px;
  }
}

.profile-header-info {
  flex: 1;
}

.profile-name {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 20px;
  font-weight: 700;
  color: vars.$text-primary;
  margin: 0 0 4px;
}

.profile-email {
  font-size: 13px;
  color: vars.$text-muted;
  margin: 0 0 8px;
}

.profile-role-badge {
  display: inline-block;
  padding: 2px 10px;
  border-radius: 20px;
  background: var(--crm-accent-008);
  border: 1px solid var(--crm-accent-018);
  font-size: 11px;
  color: vars.$cyan-primary;
  font-family: 'Noto Sans SC', sans-serif;
}

// Tab 导航
.profile-tabs {
  display: flex;
  gap: 4px;
  margin-bottom: 16px;
  background: vars.$layer-2;
  border: 1px solid vars.$border-card;
  border-radius: 10px;
  padding: 6px;
}

.profile-tab {
  display: flex;
  align-items: center;
  gap: 6px;
  flex: 1;
  padding: 8px 12px;
  border-radius: 7px;
  border: none;
  background: transparent;
  color: vars.$text-secondary;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.2s, color 0.2s, box-shadow 0.2s;
  justify-content: center;

  .tab-icon {
    display: flex;
    align-items: center;
    color: inherit;
  }

  &:hover {
    background: var(--crm-accent-006);
    color: vars.$text-primary;
  }

  &.active {
    background: var(--crm-accent-012);
    color: vars.$cyan-primary;
    font-weight: 600;
    box-shadow: inset 0 0 0 1px var(--crm-accent-022);
  }
}

// 内容卡片
.section-card {
  background: vars.$layer-2;
  border: 1px solid vars.$border-card;
  border-radius: 12px;
  padding: 24px;

  &.mt-16 {
    margin-top: 16px;
  }
}

.section-title {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 15px;
  font-weight: 600;
  color: vars.$text-primary;
  margin: 0 0 20px;
  padding-bottom: 12px;
  border-bottom: 1px solid vars.$border-panel;
}

// 表单
.form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;

  &.single-col {
    grid-template-columns: 1fr;
    max-width: 480px;
  }
}

.form-item {
  display: flex;
  flex-direction: column;
  gap: 6px;

  &.full-width {
    grid-column: 1 / -1;
  }
}

.form-label {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 12.5px;
  color: vars.$text-muted;
  font-weight: 500;
}

.form-input {
  height: 38px;
  padding: 0 12px;
  background: vars.$layer-3;
  border: 1px solid vars.$border-card;
  border-radius: 7px;
  color: vars.$text-primary;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13px;
  outline: none;
  transition: border-color 0.2s, background 0.2s;

  &::placeholder {
    color: vars.$text-placeholder;
  }

  &:focus {
    border-color: var(--crm-accent-045);
    background: vars.$layer-2;
  }

  &.input-error {
    border-color: rgba(201, 87, 69, 0.5);
  }

  &.form-input--readonly {
    cursor: default;
    color: vars.$text-secondary;
    background: vars.$layer-1;
    border-color: vars.$border-panel;
    box-shadow: none;

    &:focus {
      border-color: vars.$border-panel;
      background: vars.$layer-1;
    }
  }
}

.form-textarea {
  padding: 10px 12px;
  background: vars.$layer-3;
  border: 1px solid vars.$border-card;
  border-radius: 7px;
  color: vars.$text-primary;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13px;
  outline: none;
  resize: vertical;
  transition: border-color 0.2s, background 0.2s;

  &::placeholder {
    color: vars.$text-placeholder;
  }

  &:focus {
    border-color: var(--crm-accent-045);
  }
}

.input-with-eye {
  position: relative;

  .form-input {
    width: 100%;
    padding-right: 40px;
    box-sizing: border-box;
  }
}

.eye-btn {
  position: absolute;
  right: 10px;
  top: 50%;
  transform: translateY(-50%);
  background: transparent;
  border: none;
  cursor: pointer;
  color: vars.$text-muted;
  display: flex;
  align-items: center;
  padding: 0;

  svg {
    width: 16px;
    height: 16px;
  }

  &:hover {
    color: vars.$text-secondary;
  }
}

.input-error-msg {
  font-size: 11.5px;
  color: #C95745;
  margin: 0;
}

// 密码强度
.strength-bar {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-top: 4px;
}

.strength-track {
  flex: 1;
  height: 4px;
  background: rgba(255, 255, 255, 0.08);
  border-radius: 2px;
  overflow: hidden;
}

.strength-fill {
  height: 100%;
  border-radius: 2px;
  transition: width 0.3s, background 0.3s;

  &.weak { background: #C95745; }
  &.medium { background: #E8A838; }
  &.strong { background: #3DB88A; }
  &.very-strong { background: #00D4FF; }
}

.strength-label {
  font-size: 11px;
  font-family: 'Noto Sans SC', sans-serif;
  min-width: 40px;

  &.weak { color: #C95745; }
  &.medium { color: #E8A838; }
  &.strong { color: #3DB88A; }
  &.very-strong { color: #00D4FF; }
}

.form-actions {
  margin-top: 20px;
  padding-top: 16px;
  border-top: 1px solid vars.$border-panel;
}

.btn-primary {
  padding: 9px 24px;
  background: linear-gradient(135deg, #0066FF, #00D4FF);
  border: none;
  border-radius: 8px;
  color: #fff;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13.5px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  box-shadow: 0 4px 12px rgba(0, 212, 255, 0.2);

  &:hover:not(:disabled) {
    box-shadow: 0 6px 18px rgba(0, 212, 255, 0.35);
    transform: translateY(-1px);
  }

  &:disabled {
    opacity: 0.4;
    cursor: not-allowed;
    transform: none;
    box-shadow: none;
  }
}

// 通知设置
.notify-list {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.notify-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 0;
  border-bottom: 1px solid vars.$border-panel;

  &:last-child {
    border-bottom: none;
  }
}

.notify-info {
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.notify-name {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13.5px;
  color: vars.$text-primary;
  font-weight: 500;
}

.notify-desc {
  font-size: 12px;
  color: vars.$text-muted;
  line-height: 1.45;
}

// 开关
.toggle-switch {
  width: 42px;
  height: 24px;
  border-radius: 12px;
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.1);
  cursor: pointer;
  position: relative;
  transition: all 0.25s;
  flex-shrink: 0;

  &.on {
    background: rgba(0, 212, 255, 0.3);
    border-color: rgba(0, 212, 255, 0.5);
  }
}

.toggle-thumb {
  position: absolute;
  top: 2px;
  left: 2px;
  width: 18px;
  height: 18px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.4);
  transition: all 0.25s;

  .on & {
    left: 20px;
    background: #00D4FF;
    box-shadow: 0 0 8px rgba(0, 212, 255, 0.5);
  }
}

// 安全设置
.security-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 0;
}

.security-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.security-name {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 14px;
  color: vars.$text-primary;
  font-weight: 600;
}

.security-desc {
  font-size: 12px;
  color: vars.$text-secondary;
  line-height: 1.45;
}

// 设备列表
.device-list {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.device-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 14px 0;
  border-bottom: 1px solid vars.$border-panel;

  &:last-child {
    border-bottom: none;
  }
}

.device-icon {
  width: 36px;
  height: 36px;
  border-radius: 8px;
  background: var(--crm-accent-008);
  border: 1px solid vars.$border-card;
  display: flex;
  align-items: center;
  justify-content: center;
  color: vars.$cyan-primary;
  flex-shrink: 0;

  svg {
    width: 18px;
    height: 18px;
  }
}

.device-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.device-name {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13.5px;
  color: vars.$text-primary;
  font-weight: 500;
}

.device-meta {
  font-size: 12px;
  color: vars.$text-muted;
}

.device-current {
  font-size: 11.5px;
  color: vars.$cyan-primary;
  background: var(--crm-accent-008);
  border: 1px solid var(--crm-accent-022);
  border-radius: 20px;
  padding: 2px 10px;
  font-family: 'Noto Sans SC', sans-serif;
  font-weight: 500;
}

.btn-ghost-danger {
  padding: 5px 12px;
  background: transparent;
  border: 1px solid var(--crm-danger-color);
  border-radius: 6px;
  color: vars.$danger-color;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 12.5px;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s, border-color 0.2s, opacity 0.2s;
  opacity: 0.92;

  &:hover {
    background: var(--crm-white-06);
    border-color: vars.$danger-color;
    opacity: 1;
  }
}

// 微信绑定
.wechat-bind-section {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 40px 20px;
}

.wechat-bound {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 24px;

  .wechat-info {
    display: flex;
    align-items: center;
    gap: 16px;
    padding: 20px 30px;
    background: var(--crm-accent-005);
    border: 1px solid vars.$border-card;
    border-radius: 12px;

    .wechat-detail {
      text-align: left;

      .nickname {
        font-size: 16px;
        font-weight: 600;
        color: vars.$text-primary;
        margin: 0 0 4px;
      }

      .bind-time {
        font-size: 12px;
        color: vars.$text-muted;
        margin: 0;
      }
    }
  }
}

.wechat-unbound {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 24px;

  .bind-intro {
    text-align: center;

    .bind-text {
      margin-top: 12px;
      font-size: 14px;
      color: vars.$text-secondary;
      line-height: 1.5;
    }
  }
}
</style>
