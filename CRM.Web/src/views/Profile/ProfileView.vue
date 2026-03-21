<template>
  <div class="profile-page">
    <div class="profile-header">
      <div class="profile-avatar-wrap">
        <div class="profile-avatar">{{ userInitial }}</div>
        <div class="avatar-edit-btn" title="更换头像">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M23 19a2 2 0 01-2 2H3a2 2 0 01-2-2V8a2 2 0 012-2h4l2-3h6l2 3h4a2 2 0 012 2z"/>
            <circle cx="12" cy="13" r="4"/>
          </svg>
        </div>
      </div>
      <div class="profile-header-info">
        <h2 class="profile-name">{{ form.userName }}</h2>
        <p class="profile-email">{{ form.email }}</p>
        <span class="profile-role-badge">系统管理员</span>
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
        <h3 class="section-title">基本信息</h3>
        <div class="form-grid">
          <div class="form-item">
            <label class="form-label">员工账号</label>
            <input v-model="form.userName" class="form-input" placeholder="请输入员工账号" />
          </div>
          <div class="form-item">
            <label class="form-label">邮箱</label>
            <input v-model="form.email" class="form-input" placeholder="请输入邮箱" type="email" />
          </div>
          <div class="form-item">
            <label class="form-label">手机号</label>
            <input v-model="form.phone" class="form-input" placeholder="请输入手机号" />
          </div>
          <div class="form-item">
            <label class="form-label">部门</label>
            <input v-model="form.department" class="form-input" placeholder="请输入部门" />
          </div>
          <div class="form-item full-width">
            <label class="form-label">个人简介</label>
            <textarea v-model="form.bio" class="form-textarea" placeholder="请输入个人简介" rows="3"></textarea>
          </div>
        </div>
        <div class="form-actions">
          <button class="btn-primary" @click="saveBasicInfo">保存更改</button>
        </div>
      </div>
    </div>

    <!-- 修改密码 -->
    <div class="tab-content" v-if="activeTab === 'password'">
      <div class="section-card">
        <h3 class="section-title">修改密码</h3>
        <div class="form-grid single-col">
          <div class="form-item">
            <label class="form-label">当前密码</label>
            <div class="input-with-eye">
              <input
                v-model="pwdForm.current"
                :type="showPwd.current ? 'text' : 'password'"
                class="form-input"
                placeholder="请输入当前密码"
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
            <label class="form-label">新密码</label>
            <div class="input-with-eye">
              <input
                v-model="pwdForm.newPwd"
                :type="showPwd.newPwd ? 'text' : 'password'"
                class="form-input"
                placeholder="请输入新密码（至少8位）"
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
            <label class="form-label">确认新密码</label>
            <div class="input-with-eye">
              <input
                v-model="pwdForm.confirm"
                :type="showPwd.confirm ? 'text' : 'password'"
                class="form-input"
                :class="{ 'input-error': pwdForm.confirm && pwdForm.confirm !== pwdForm.newPwd }"
                placeholder="请再次输入新密码"
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
            <p class="input-error-msg" v-if="pwdForm.confirm && pwdForm.confirm !== pwdForm.newPwd">两次密码不一致</p>
          </div>
        </div>
        <div class="form-actions">
          <button class="btn-primary" @click="changePassword" :disabled="!canChangePassword">更新密码</button>
        </div>
      </div>
    </div>

    <!-- 微信绑定 -->
    <div class="tab-content" v-if="activeTab === 'wechat'">
      <div class="section-card">
        <h3 class="section-title">微信绑定</h3>
        <div class="wechat-bind-section">
          <div v-if="wechatBindInfo.isBound" class="wechat-bound">
            <div class="wechat-info">
              <el-avatar :size="64" :src="wechatBindInfo.avatarUrl" />
              <div class="wechat-detail">
                <p class="nickname">{{ wechatBindInfo.nickname }}</p>
                <p class="bind-time">绑定时间：{{ formatDate(wechatBindInfo.bindTime) }}</p>
              </div>
            </div>
            <el-button type="danger" @click="unbindWechatHandler">解除绑定</el-button>
          </div>
          <div v-else class="wechat-unbound">
            <div class="bind-intro">
              <el-icon :size="48" color="#07C160"><ChatDotRound /></el-icon>
              <p class="bind-text">绑定微信后，可以使用微信扫码快速登录</p>
            </div>
            <el-button type="primary" size="large" @click="goToWechatBind">
              去绑定微信
            </el-button>
          </div>
        </div>
      </div>
    </div>

    <!-- 通知偏好 -->
    <div class="tab-content" v-if="activeTab === 'notifications'">
      <div class="section-card">
        <h3 class="section-title">通知偏好</h3>
        <div class="notify-list">
          <div class="notify-item" v-for="item in notifySettings" :key="item.key">
            <div class="notify-info">
              <span class="notify-name">{{ item.name }}</span>
              <span class="notify-desc">{{ item.desc }}</span>
            </div>
            <div class="toggle-switch" :class="{ on: item.enabled }" @click="item.enabled = !item.enabled">
              <div class="toggle-thumb"></div>
            </div>
          </div>
        </div>
        <div class="form-actions">
          <button class="btn-primary" @click="saveNotifications">保存设置</button>
        </div>
      </div>
    </div>

    <!-- 安全设置 -->
    <div class="tab-content" v-if="activeTab === 'security'">
      <div class="section-card">
        <h3 class="section-title">两步验证</h3>
        <div class="security-row">
          <div class="security-info">
            <span class="security-name">两步验证（2FA）</span>
            <span class="security-desc">使用身份验证器 App 增强账户安全</span>
          </div>
          <div class="toggle-switch" :class="{ on: security.twoFactor }" @click="security.twoFactor = !security.twoFactor">
            <div class="toggle-thumb"></div>
          </div>
        </div>
      </div>
      <div class="section-card mt-16">
        <h3 class="section-title">登录设备</h3>
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
              <span class="device-meta">{{ device.location }} · {{ device.time }}</span>
            </div>
            <span class="device-current" v-if="device.current">当前设备</span>
            <button class="btn-ghost-danger" v-else @click="revokeDevice(device.id)">撤销</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ChatDotRound } from '@element-plus/icons-vue'
import { getWechatBindInfo, unbindWechat } from '@/api/wechatAuth'
import { formatDate } from '@/utils/date'

const authStore = useAuthStore()
const router = useRouter()

const userInitial = computed(() => (authStore.user?.userName || '管')[0].toUpperCase())

const activeTab = ref('basic')

const tabList = [
  {
    key: 'basic',
    label: '基本信息',
    icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" width="15" height="15"><path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>'
  },
  {
    key: 'password',
    label: '修改密码',
    icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" width="15" height="15"><rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0110 0v4"/></svg>'
  },
  {
    key: 'wechat',
    label: '微信绑定',
    icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" width="15" height="15"><path d="M9 11a2 2 0 1 1 0-4 2 2 0 0 1 0 4z"/><path d="M15 11a2 2 0 1 1 0-4 2 2 0 0 1 0 4z"/><path d="M12 22c5.523 0 10-4.477 10-10S17.523 2 12 2 2 6.477 2 12s4.477 10 10 10z"/></svg>'
  },
  {
    key: 'notifications',
    label: '通知偏好',
    icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" width="15" height="15"><path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.73 21a2 2 0 01-3.46 0"/></svg>'
  },
  {
    key: 'security',
    label: '安全设置',
    icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" width="15" height="15"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/></svg>'
  }
]

// 基本信息表单
const form = ref({
  userName: authStore.user?.userName || '',
  email: authStore.user?.email || '',
  phone: '',
  department: '',
  bio: ''
})

const saveBasicInfo = () => {
  ElMessage.success('基本信息已保存')
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
  const map = ['弱', '弱', '中等', '强', '非常强']
  return map[strengthLevel.value] || '弱'
})

const canChangePassword = computed(() => {
  return pwdForm.value.current &&
    pwdForm.value.newPwd.length >= 8 &&
    pwdForm.value.newPwd === pwdForm.value.confirm
})

const changePassword = () => {
  if (!canChangePassword.value) return
  ElMessage.success('密码已更新')
  pwdForm.value = { current: '', newPwd: '', confirm: '' }
}

// 通知设置
const notifySettings = ref([
  { key: 'order', name: '订单通知', desc: '新订单创建、状态变更时通知', enabled: true },
  { key: 'quote', name: '报价通知', desc: '报价单审批、到期提醒', enabled: true },
  { key: 'finance', name: '财务通知', desc: '付款、收款、发票相关通知', enabled: false },
  { key: 'system', name: '系统通知', desc: '系统维护、版本更新通知', enabled: true },
  { key: 'email', name: '邮件通知', desc: '通过邮件接收重要通知', enabled: false },
])

const saveNotifications = () => {
  ElMessage.success('通知偏好已保存')
}

// 安全设置
const security = ref({ twoFactor: false })

const devices = ref([
  { id: 1, name: 'Chrome on Windows', location: '上海', time: '刚刚', current: true },
  { id: 2, name: 'Safari on macOS', location: '北京', time: '2天前', current: false },
  { id: 3, name: 'Chrome on Android', location: '广州', time: '7天前', current: false },
])

const revokeDevice = (id: number) => {
  devices.value = devices.value.filter(d => d.id !== id)
  ElMessage.success('已撤销该设备的登录')
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
    await ElMessageBox.confirm(
      '解除绑定后将无法使用微信扫码登录，确定解除吗？',
      '确认解除绑定',
      { type: 'warning' }
    )
    const ok = await unbindWechat()
    if (ok) {
      ElMessage.success('已解除微信绑定')
      fetchWechatBindInfo()
    }
  } catch {
    // 取消
  }
}

onMounted(() => {
  fetchWechatBindInfo()
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
  border: 1px solid rgba(0, 212, 255, 0.1);
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
  color: #E8F4FF;
  margin: 0 0 4px;
}

.profile-email {
  font-size: 13px;
  color: rgba(80, 187, 227, 0.6);
  margin: 0 0 8px;
}

.profile-role-badge {
  display: inline-block;
  padding: 2px 10px;
  border-radius: 20px;
  background: rgba(0, 212, 255, 0.1);
  border: 1px solid rgba(0, 212, 255, 0.25);
  font-size: 11px;
  color: #00D4FF;
  font-family: 'Noto Sans SC', sans-serif;
}

// Tab 导航
.profile-tabs {
  display: flex;
  gap: 4px;
  margin-bottom: 16px;
  background: vars.$layer-2;
  border: 1px solid rgba(0, 212, 255, 0.1);
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
  color: rgba(180, 210, 230, 0.6);
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.2s;
  justify-content: center;

  .tab-icon {
    display: flex;
    align-items: center;
  }

  &:hover {
    background: rgba(0, 212, 255, 0.06);
    color: rgba(180, 210, 230, 0.9);
  }

  &.active {
    background: rgba(0, 212, 255, 0.12);
    color: #00D4FF;
    font-weight: 600;
    box-shadow: 0 0 10px rgba(0, 212, 255, 0.1);
  }
}

// 内容卡片
.section-card {
  background: vars.$layer-2;
  border: 1px solid rgba(0, 212, 255, 0.1);
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
  color: #E8F4FF;
  margin: 0 0 20px;
  padding-bottom: 12px;
  border-bottom: 1px solid rgba(0, 212, 255, 0.08);
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
  color: rgba(80, 187, 227, 0.7);
  font-weight: 500;
}

.form-input {
  height: 38px;
  padding: 0 12px;
  background: rgba(0, 212, 255, 0.04);
  border: 1px solid rgba(0, 212, 255, 0.15);
  border-radius: 7px;
  color: #E8F4FF;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13px;
  outline: none;
  transition: border-color 0.2s;

  &::placeholder {
    color: rgba(80, 187, 227, 0.3);
  }

  &:focus {
    border-color: rgba(0, 212, 255, 0.4);
    background: rgba(0, 212, 255, 0.06);
  }

  &.input-error {
    border-color: rgba(201, 87, 69, 0.5);
  }
}

.form-textarea {
  padding: 10px 12px;
  background: rgba(0, 212, 255, 0.04);
  border: 1px solid rgba(0, 212, 255, 0.15);
  border-radius: 7px;
  color: #E8F4FF;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13px;
  outline: none;
  resize: vertical;
  transition: border-color 0.2s;

  &::placeholder {
    color: rgba(80, 187, 227, 0.3);
  }

  &:focus {
    border-color: rgba(0, 212, 255, 0.4);
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
  color: rgba(80, 187, 227, 0.4);
  display: flex;
  align-items: center;
  padding: 0;

  svg {
    width: 16px;
    height: 16px;
  }

  &:hover {
    color: rgba(80, 187, 227, 0.8);
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
  border-top: 1px solid rgba(0, 212, 255, 0.06);
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
  border-bottom: 1px solid rgba(0, 212, 255, 0.06);

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
  color: #E8F4FF;
  font-weight: 500;
}

.notify-desc {
  font-size: 12px;
  color: rgba(80, 187, 227, 0.5);
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
  color: #E8F4FF;
  font-weight: 500;
}

.security-desc {
  font-size: 12px;
  color: rgba(80, 187, 227, 0.5);
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
  border-bottom: 1px solid rgba(0, 212, 255, 0.06);

  &:last-child {
    border-bottom: none;
  }
}

.device-icon {
  width: 36px;
  height: 36px;
  border-radius: 8px;
  background: rgba(0, 212, 255, 0.08);
  border: 1px solid rgba(0, 212, 255, 0.12);
  display: flex;
  align-items: center;
  justify-content: center;
  color: rgba(0, 212, 255, 0.7);
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
  color: #E8F4FF;
}

.device-meta {
  font-size: 12px;
  color: rgba(80, 187, 227, 0.5);
}

.device-current {
  font-size: 11.5px;
  color: #00D4FF;
  background: rgba(0, 212, 255, 0.1);
  border: 1px solid rgba(0, 212, 255, 0.2);
  border-radius: 20px;
  padding: 2px 10px;
  font-family: 'Noto Sans SC', sans-serif;
}

.btn-ghost-danger {
  padding: 5px 12px;
  background: transparent;
  border: 1px solid rgba(201, 87, 69, 0.3);
  border-radius: 6px;
  color: rgba(201, 87, 69, 0.7);
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 12.5px;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(201, 87, 69, 0.1);
    border-color: rgba(201, 87, 69, 0.5);
    color: #C95745;
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
    background: rgba(0, 212, 255, 0.05);
    border: 1px solid rgba(0, 212, 255, 0.15);
    border-radius: 12px;

    .wechat-detail {
      text-align: left;

      .nickname {
        font-size: 16px;
        font-weight: 600;
        color: #E8F4FF;
        margin: 0 0 4px;
      }

      .bind-time {
        font-size: 12px;
        color: rgba(80, 187, 227, 0.6);
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
      color: rgba(80, 187, 227, 0.7);
    }
  }
}
</style>
