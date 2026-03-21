<template>
  <div class="register-view">
    <!-- 背景动态粒子网格 -->
    <div class="bg-grid"></div>
    <div class="bg-glow bg-glow--left"></div>
    <div class="bg-glow bg-glow--right"></div>

    <div class="register-container">
      <!-- Logo 区域 -->
      <div class="brand">
        <div class="brand-icon">
          <svg width="32" height="32" viewBox="0 0 32 32" fill="none">
            <polygon points="16,2 30,10 30,22 16,30 2,22 2,10" fill="none" stroke="#00D4FF" stroke-width="1.5"/>
            <polygon points="16,8 24,13 24,19 16,24 8,19 8,13" fill="rgba(0,212,255,0.15)" stroke="#00D4FF" stroke-width="1"/>
            <circle cx="16" cy="16" r="3" fill="#00D4FF"/>
          </svg>
        </div>
        <div class="brand-text">
          <span class="brand-name">FrontCRM</span>
          <span class="brand-sub">AI智销系统</span>
        </div>
      </div>

      <!-- 注册卡片 -->
      <div class="register-card">
        <!-- 卡片顶部装饰线 -->
        <div class="card-top-line"></div>

        <div class="card-header">
          <h2 class="card-title">创建账号</h2>
          <p class="card-subtitle">填写以下信息完成注册</p>
        </div>

        <el-form
          ref="formRef"
          :model="form"
          :rules="rules"
          @submit.prevent="handleRegister"
          class="register-form"
        >
          <!-- 员工账号 -->
          <div class="form-field">
            <label class="field-label">员工账号</label>
            <el-form-item prop="userName">
              <div class="input-wrapper">
                <span class="input-icon">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                    <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/>
                    <circle cx="12" cy="7" r="4"/>
                  </svg>
                </span>
                <el-input
                  v-model="form.userName"
                  placeholder="请输入员工账号（2-50个字符）"
                  class="custom-input"
                />
              </div>
            </el-form-item>
          </div>

          <!-- 邮箱 -->
          <div class="form-field">
            <label class="field-label">邮箱地址</label>
            <el-form-item prop="email">
              <div class="input-wrapper">
                <span class="input-icon">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                    <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/>
                    <polyline points="22,6 12,13 2,6"/>
                  </svg>
                </span>
                <el-input
                  v-model="form.email"
                  type="email"
                  placeholder="请输入邮箱地址"
                  class="custom-input"
                />
              </div>
            </el-form-item>
          </div>

          <!-- 密码 -->
          <div class="form-field">
            <label class="field-label">登录密码</label>
            <el-form-item prop="password">
              <div class="input-wrapper">
                <span class="input-icon">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                    <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/>
                    <path d="M7 11V7a5 5 0 0 1 10 0v4"/>
                  </svg>
                </span>
                <el-input
                  v-model="form.password"
                  type="password"
                  placeholder="请输入密码（至少6个字符）"
                  show-password
                  class="custom-input"
                />
              </div>
            </el-form-item>
          </div>

          <!-- 密码强度指示 -->
          <div v-if="form.password" class="password-strength">
            <div class="strength-bars">
              <span
                v-for="i in 4"
                :key="i"
                class="strength-bar"
                :class="{ active: passwordStrength >= i, [`level-${passwordStrength}`]: true }"
              ></span>
            </div>
            <span class="strength-label" :class="`strength-text-${passwordStrength}`">
              {{ strengthLabels[passwordStrength - 1] || '' }}
            </span>
          </div>

          <!-- 错误提示 -->
          <div v-if="errorMsg" class="error-alert">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10"/>
              <line x1="12" y1="8" x2="12" y2="12"/>
              <line x1="12" y1="16" x2="12.01" y2="16"/>
            </svg>
            {{ errorMsg }}
          </div>

          <!-- 注册按钮 -->
          <button
            type="submit"
            class="register-btn"
            :class="{ 'loading': loading }"
            :disabled="loading"
          >
            <span v-if="!loading" class="btn-content">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
                <circle cx="8.5" cy="7" r="4"/>
                <line x1="20" y1="8" x2="20" y2="14"/>
                <line x1="23" y1="11" x2="17" y2="11"/>
              </svg>
              创建账号
            </span>
            <span v-else class="btn-loading">
              <span class="spinner"></span>
              注册中...
            </span>
          </button>
        </el-form>

        <!-- 底部链接 -->
        <div class="card-footer">
          <span class="footer-text">已有账号？</span>
          <router-link to="/login" class="footer-link">立即登录</router-link>
        </div>
      </div>

      <!-- 版权信息 -->
      <div class="copyright">
        © 2026 FrontCRM · 智能进销存管理系统
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { type FormInstance, type FormRules } from 'element-plus'
import { useAuthStore } from '@/stores'

const router = useRouter()
const authStore = useAuthStore()

const formRef = ref<FormInstance>()
const loading = ref(false)
const errorMsg = ref('')

const form = reactive({
  userName: '',
  email: '',
  password: ''
})

const rules: FormRules = {
  userName: [
    { required: true, message: '请输入员工账号', trigger: 'blur' },
    { min: 2, max: 50, message: '员工账号长度在 2 到 50 个字符', trigger: 'blur' }
  ],
  email: [
    { required: true, message: '请输入邮箱地址', trigger: 'blur' },
    { type: 'email', message: '请输入正确的邮箱地址', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码长度不能少于6个字符', trigger: 'blur' }
  ]
}

// 密码强度计算
const strengthLabels = ['弱', '一般', '较强', '强']
const passwordStrength = computed(() => {
  const pwd = form.password
  if (!pwd) return 0
  let score = 0
  if (pwd.length >= 6) score++
  if (pwd.length >= 10) score++
  if (/[A-Z]/.test(pwd) || /[0-9]/.test(pwd)) score++
  if (/[^A-Za-z0-9]/.test(pwd)) score++
  return Math.min(score, 4)
})

const handleRegister = async () => {
  if (!formRef.value) return
  errorMsg.value = ''

  await formRef.value.validate(async (valid) => {
    if (!valid) return

    loading.value = true
    try {
      const success = await authStore.register(form)
      if (success) {
        router.push('/dashboard')
      } else {
        errorMsg.value = '注册失败，请稍后重试'
      }
    } catch (error: any) {
      // 现在拦截器会抛出带 message 的错误对象
      errorMsg.value = error.message || error.response?.data?.message || '注册失败，请稍后重试'
    } finally {
      loading.value = false
    }
  })
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Orbitron:wght@600;700&family=Noto+Sans+SC:wght@300;400;500&display=swap');

.register-view {
  width: 100%;
  min-height: 100vh;
  background-color: $layer-1;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  overflow: hidden;
  font-family: 'Noto Sans SC', sans-serif;
}

// 背景网格
.bg-grid {
  position: absolute;
  inset: 0;
  background-image:
    linear-gradient(rgba(0, 212, 255, 0.04) 1px, transparent 1px),
    linear-gradient(90deg, rgba(0, 212, 255, 0.04) 1px, transparent 1px);
  background-size: 40px 40px;
  pointer-events: none;
}

// 背景光晕
.bg-glow {
  position: absolute;
  width: 500px;
  height: 500px;
  border-radius: 50%;
  pointer-events: none;
  filter: blur(100px);

  &--left {
    left: -200px;
    bottom: -100px;
    background: radial-gradient(circle, rgba(0, 102, 255, 0.12) 0%, transparent 70%);
  }

  &--right {
    right: -200px;
    top: -100px;
    background: radial-gradient(circle, rgba(0, 212, 255, 0.10) 0%, transparent 70%);
  }
}

// 主容器
.register-container {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 440px;
  padding: 0 20px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 24px;
}

// 品牌区域（与登录页一致）
.brand {
  display: flex;
  align-items: center;
  gap: 12px;

  .brand-icon {
    width: 48px;
    height: 48px;
    background: rgba(0, 212, 255, 0.08);
    border: 1px solid rgba(0, 212, 255, 0.25);
    border-radius: 12px;
    display: flex;
    align-items: center;
    justify-content: center;
    box-shadow: 0 0 16px rgba(0, 212, 255, 0.1);
  }

  .brand-text {
    display: flex;
    flex-direction: column;
  }

  .brand-name {
    font-family: 'Orbitron', sans-serif;
    font-size: 20px;
    font-weight: 700;
    color: $text-primary;
    letter-spacing: 1px;
    line-height: 1.2;
  }

  .brand-sub {
    font-size: 11px;
    color: rgba(0, 212, 255, 0.6);
    letter-spacing: 2px;
    margin-top: 2px;
  }
}

// 注册卡片
.register-card {
  width: 100%;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-xl;
  padding: 32px;
  position: relative;
  box-shadow: $shadow-md, $shadow-glow;

  .card-top-line {
    position: absolute;
    top: 0;
    left: 32px;
    right: 32px;
    height: 2px;
    background: linear-gradient(90deg, transparent, $color-mint-green, transparent);
    border-radius: 0 0 2px 2px;
    opacity: 0.6;
  }
}

// 卡片标题
.card-header {
  margin-bottom: 24px;

  .card-title {
    font-size: 22px;
    font-weight: 600;
    color: $text-primary;
    margin-bottom: 6px;
    letter-spacing: 0.5px;
  }

  .card-subtitle {
    font-size: 13px;
    color: $text-muted;
  }
}

// 表单
.register-form {
  :deep(.el-form-item) {
    margin-bottom: 0;
  }

  :deep(.el-form-item__error) {
    color: $color-red-brown;
    font-size: 11px;
    padding-top: 4px;
  }
}

// 表单字段
.form-field {
  margin-bottom: 16px;

  .field-label {
    display: block;
    font-size: 12px;
    font-weight: 500;
    color: $text-muted;
    margin-bottom: 8px;
    letter-spacing: 0.5px;
    text-transform: uppercase;
  }
}

// 输入框包装（与登录页一致）
.input-wrapper {
  position: relative;
  display: flex;
  align-items: center;

  .input-icon {
    position: absolute;
    left: 12px;
    z-index: 2;
    color: rgba(0, 212, 255, 0.45);
    display: flex;
    align-items: center;
    pointer-events: none;
    transition: color 0.2s;
  }

  &:focus-within .input-icon {
    color: rgba(0, 212, 255, 0.8);
  }

  :deep(.el-input) {
    width: 100%;
  }

  :deep(.el-input__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;
    padding-left: 38px !important;
    height: 44px;
    transition: border-color 0.2s, box-shadow 0.2s;

    &:hover {
      border-color: rgba(0, 212, 255, 0.3) !important;
    }

    &.is-focus {
      border-color: rgba(0, 212, 255, 0.6) !important;
      box-shadow: 0 0 0 3px rgba(0, 212, 255, 0.08) !important;
    }
  }

  :deep(.el-input__inner) {
    color: $text-primary !important;
    background: transparent !important;
    font-size: 14px;
    font-family: 'Noto Sans SC', sans-serif;

    &::placeholder {
      color: $text-placeholder !important;
    }
  }

  :deep(.el-input__password) {
    color: rgba(0, 212, 255, 0.5) !important;

    &:hover {
      color: $cyan-primary !important;
    }
  }
}

// 密码强度
.password-strength {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-top: -8px;
  margin-bottom: 12px;

  .strength-bars {
    display: flex;
    gap: 4px;
  }

  .strength-bar {
    width: 32px;
    height: 3px;
    background: rgba(255, 255, 255, 0.08);
    border-radius: 2px;
    transition: background 0.3s;

    &.active {
      &.level-1 { background: $color-red-brown; }
      &.level-2 { background: $color-amber; }
      &.level-3 { background: $color-ice-blue; }
      &.level-4 { background: $color-mint-green; }
    }
  }

  .strength-label {
    font-size: 11px;

    &.strength-text-1 { color: $color-red-brown; }
    &.strength-text-2 { color: $color-amber; }
    &.strength-text-3 { color: $color-ice-blue; }
    &.strength-text-4 { color: $color-mint-green; }
  }
}

// 错误提示
.error-alert {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 14px;
  background: rgba(201, 87, 69, 0.10);
  border: 1px solid rgba(201, 87, 69, 0.25);
  border-radius: $border-radius-md;
  color: $color-red-brown;
  font-size: 13px;
  margin-bottom: 16px;
}

// 注册按钮（薄荷绿主色调，与登录区分）
.register-btn {
  width: 100%;
  height: 46px;
  background: linear-gradient(135deg, rgba(50, 149, 201, 0.8), rgba(70, 191, 145, 0.7));
  border: 1px solid rgba(70, 191, 145, 0.4);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 15px;
  font-weight: 500;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.25s ease;
  margin-top: 8px;
  letter-spacing: 1px;
  position: relative;
  overflow: hidden;

  &::before {
    content: '';
    position: absolute;
    inset: 0;
    background: linear-gradient(135deg, rgba(255,255,255,0.08), transparent);
    opacity: 0;
    transition: opacity 0.25s;
  }

  &:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 6px 24px rgba(70, 191, 145, 0.2);
    border-color: rgba(70, 191, 145, 0.7);

    &::before {
      opacity: 1;
    }
  }

  &:active:not(:disabled) {
    transform: translateY(0);
  }

  &:disabled {
    opacity: 0.7;
    cursor: not-allowed;
  }

  .btn-content {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
  }

  .btn-loading {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
  }

  .spinner {
    width: 16px;
    height: 16px;
    border: 2px solid rgba(255, 255, 255, 0.3);
    border-top-color: #fff;
    border-radius: 50%;
    animation: spin 0.7s linear infinite;
  }
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

// 卡片底部
.card-footer {
  text-align: center;
  margin-top: 24px;
  padding-top: 20px;
  border-top: 1px solid $border-panel;

  .footer-text {
    font-size: 13px;
    color: $text-muted;
  }

  .footer-link {
    font-size: 13px;
    color: $color-ice-blue;
    text-decoration: none;
    margin-left: 6px;
    font-weight: 500;
    transition: color 0.2s;

    &:hover {
      color: $cyan-primary;
      text-decoration: underline;
    }
  }
}

// 版权
.copyright {
  font-size: 11px;
  color: rgba(200, 216, 232, 0.25);
  letter-spacing: 0.5px;
}
</style>
