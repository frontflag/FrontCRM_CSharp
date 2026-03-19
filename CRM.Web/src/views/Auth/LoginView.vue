<template>
  <div class="login-view">
    <!-- 版本号 -->
    <div class="version-tag">版本号：03160634</div>

    <!-- 背景动态粒子网格 -->
    <div class="bg-grid"></div>
    <div class="bg-glow bg-glow--left"></div>
    <div class="bg-glow bg-glow--right"></div>

    <div class="login-container">
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

      <!-- 登录卡片 -->
      <div class="login-card">
        <!-- 卡片顶部装饰线 -->
        <div class="card-top-line"></div>

        <div class="card-header">
          <h2 class="card-title">欢迎回来</h2>
          <p class="card-subtitle">请登录您的账号以继续</p>
        </div>

        <el-form
          ref="formRef"
          :model="form"
          :rules="rules"
          @submit.prevent="handleLogin"
          class="login-form"
        >
          <!-- 账号 -->
          <div class="form-field">
            <label class="field-label">登录账号</label>
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
                  placeholder="请输入登录账号"
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
                  placeholder="请输入登录密码"
                  show-password
                  class="custom-input"
                />
              </div>
            </el-form-item>
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

          <!-- 登录按钮 -->
          <button
            type="submit"
            class="login-btn"
            :class="{ 'loading': loading }"
            :disabled="loading"
          >
            <span v-if="!loading" class="btn-content">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4"/>
                <polyline points="10 17 15 12 10 7"/>
                <line x1="15" y1="12" x2="3" y2="12"/>
              </svg>
              登录系统
            </span>
            <span v-else class="btn-loading">
              <span class="spinner"></span>
              验证中...
            </span>
          </button>
        </el-form>

        <!-- 底部链接 -->
        <div class="card-footer">
          <span class="footer-text">还没有账号？</span>
          <router-link to="/register" class="footer-link">立即注册</router-link>
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
import { reactive, ref } from 'vue'
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
  password: ''
})

const rules: FormRules = {
  userName: [
    { required: true, message: '请输入登录账号', trigger: 'blur' },
    { min: 2, max: 50, message: '账号长度在 2 到 50 个字符', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码长度不能少于6个字符', trigger: 'blur' }
  ]
}

const handleLogin = async () => {
  if (!formRef.value) return
  errorMsg.value = ''

  await formRef.value.validate(async (valid) => {
    if (!valid) return

    loading.value = true
    try {
      const success = await authStore.login(form)
      if (success) {
        router.push('/dashboard')
      } else {
        errorMsg.value = '登录失败，请检查账号和密码'
      }
    } catch (error: any) {
      // 拦截器会抛出带 message 的错误对象
      errorMsg.value = error.message || error.response?.data?.message || '登录失败，请稍后重试'
    } finally {
      loading.value = false
    }
  })
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

// 字体引入（通过 index.html 或此处 @import）
@import url('https://fonts.googleapis.com/css2?family=Orbitron:wght@600;700&family=Noto+Sans+SC:wght@300;400;500&family=Space+Mono&display=swap');

.login-view {
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

// 版本号标签
.version-tag {
  position: fixed;
  top: 12px;
  left: 14px;
  font-family: 'Space Mono', monospace;
  font-size: 11px;
  color: rgba(0, 212, 255, 0.5);
  letter-spacing: 1px;
  z-index: 100;
  pointer-events: none;
  user-select: none;
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
    top: -100px;
    background: radial-gradient(circle, rgba(0, 102, 255, 0.12) 0%, transparent 70%);
  }

  &--right {
    right: -200px;
    bottom: -100px;
    background: radial-gradient(circle, rgba(0, 212, 255, 0.10) 0%, transparent 70%);
  }
}

// 主容器
.login-container {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 420px;
  padding: 0 20px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 24px;
}

// 品牌区域
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

// 登录卡片
.login-card {
  width: 100%;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-xl;
  padding: 32px;
  position: relative;
  box-shadow: $shadow-md, $shadow-glow;

  // 顶部青色装饰线
  .card-top-line {
    position: absolute;
    top: 0;
    left: 32px;
    right: 32px;
    height: 2px;
    background: linear-gradient(90deg, transparent, $cyan-primary, transparent);
    border-radius: 0 0 2px 2px;
    opacity: 0.6;
  }
}

// 卡片标题
.card-header {
  margin-bottom: 28px;

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
.login-form {
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
  margin-bottom: 18px;

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

// 输入框包装
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
  margin-bottom: 18px;
}

// 登录按钮
.login-btn {
  width: 100%;
  height: 46px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
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
    box-shadow: 0 6px 24px rgba(0, 212, 255, 0.25);
    border-color: rgba(0, 212, 255, 0.7);

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
