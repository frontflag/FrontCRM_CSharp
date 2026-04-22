<template>
  <div class="login-view">
    <div class="login-split">
      <!-- 左侧：品牌与 Slogan -->
      <aside class="login-slogan" aria-label="brand">
        <div class="slogan-bg" aria-hidden="true">
          <div class="slogan-orbit slogan-orbit--1" />
          <div class="slogan-orbit slogan-orbit--2" />
          <div class="slogan-orbit slogan-orbit--3" />
          <div class="slogan-beam" />
        </div>
        <div class="slogan-inner">
          <header class="slogan-brand">
            <div class="slogan-brand-mark">
              <img
                class="slogan-brand-img"
                :src="loginBrandLogoSrc"
                :alt="t('login.brandLogoAlt')"
                @error="onLoginBrandLogoError"
              />
            </div>
          </header>

          <h1 class="slogan-headline">
            <span class="slogan-line slogan-line--white">{{ t('login.sloganLine1') }}</span>
            <span class="slogan-line slogan-line--accent">{{ t('login.sloganLine2') }}</span>
          </h1>

          <ul class="slogan-tags" aria-label="features">
            <li><span class="slogan-dot" />{{ t('login.featureSync') }}</li>
            <li><span class="slogan-dot" />{{ t('login.featureTrack') }}</li>
            <li><span class="slogan-dot" />{{ t('login.featureReport') }}</li>
          </ul>
        </div>
      </aside>

      <!-- 右侧：登录 -->
      <main class="login-panel">
        <div class="login-panel__toolbar">
          <span class="version-tag">{{ t('login.version', { version: '03160634' }) }}</span>
          <button type="button" class="theme-moon" title="Theme" aria-label="Theme">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z" />
            </svg>
          </button>
        </div>

        <div class="login-panel__middle">
        <div class="login-card">
          <h2 class="panel-welcome">{{ t('login.welcomeTitle') }}</h2>
          <p class="panel-welcome-sub">{{ t('login.welcomeSub') }}</p>

          <div class="login-tabs">
            <button
              type="button"
              :class="['tab-btn', { active: loginType === 'password' }]"
              @click="switchToPasswordLogin"
            >
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                <circle cx="12" cy="7" r="4" />
              </svg>
              {{ t('login.accountLogin') }}
            </button>
            <button type="button" :class="['tab-btn', { active: loginType === 'wechat' }]" @click="switchToWechatLogin">
              <el-icon style="font-size: 14px"><ChatDotRound /></el-icon>
              {{ t('login.wechatLogin') }}
            </button>
          </div>

          <div v-show="loginType === 'password'">
            <el-form ref="formRef" :model="form" :rules="rules" class="login-form" @submit.prevent="handleLogin">
              <div class="form-field">
                <label class="field-label">{{ t('login.accountLabel') }}</label>
                <el-form-item prop="userName">
                  <div class="input-wrap input-wrap--light">
                    <span class="input-icon">
                      <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
                        <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                        <circle cx="12" cy="7" r="4" />
                      </svg>
                    </span>
                    <el-input v-model="form.userName" :placeholder="t('login.accountPlaceholder')" class="input-el" />
                  </div>
                </el-form-item>
              </div>

              <div class="form-field">
                <label class="field-label">{{ t('login.passwordLabel') }}</label>
                <el-form-item prop="password">
                  <div class="input-wrap input-wrap--light">
                    <span class="input-icon">
                      <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
                        <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
                        <path d="M7 11V7a5 5 0 0 1 10 0v4" />
                      </svg>
                    </span>
                    <el-input
                      v-model="form.password"
                      type="password"
                      :placeholder="t('login.passwordPlaceholder')"
                      show-password
                      class="input-el"
                    />
                  </div>
                </el-form-item>
              </div>

              <div class="form-row-meta">
                <el-checkbox v-model="rememberMe">{{ t('login.rememberMe') }}</el-checkbox>
                <a href="#" class="forgot-link" @click.prevent>{{ t('login.forgotPassword') }}</a>
              </div>

              <div v-if="errorMsg" class="error-alert">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <circle cx="12" cy="12" r="10" />
                  <line x1="12" y1="8" x2="12" y2="12" />
                  <line x1="12" y1="16" x2="12.01" y2="16" />
                </svg>
                {{ errorMsg }}
              </div>

              <button type="submit" class="login-btn login-btn--primary" :class="{ loading }" :disabled="loading">
                <span v-if="!loading" class="btn-content">{{ t('login.loginButton') }}</span>
                <span v-else class="btn-loading">
                  <span class="spinner" />
                  {{ t('login.validating') }}
                </span>
              </button>
            </el-form>
          </div>

          <div v-show="loginType === 'wechat'" class="wechat-login-area">
            <div v-if="qrStatus === 0" class="qr-waiting">
              <div v-if="!qrCodeUrl" class="qr-loading">
                <el-icon class="loading-icon"><Loading /></el-icon>
                <p>{{ t('login.generateQr') }}</p>
              </div>
              <div v-else>
                <img :src="qrCodeUrl" class="qr-code" alt="微信扫码登录" />
                <p class="qr-tip">{{ t('login.scanTip') }}</p>
              </div>
            </div>
            <div v-else-if="qrStatus === 1" class="qr-scanned">
              <el-icon :size="48" color="#67C23A"><CircleCheck /></el-icon>
              <p>{{ t('login.scannedTip') }}</p>
            </div>
            <div v-else-if="qrStatus === 5" class="qr-unbound">
              <el-icon :size="48" color="#E6A23C"><Warning /></el-icon>
              <h3>{{ t('login.unboundTitle') }}</h3>
              <p>{{ t('login.unboundDesc') }}</p>
              <div class="actions">
                <el-button type="primary" @click="switchToPasswordLogin">{{ t('login.usePasswordFirst') }}</el-button>
              </div>
              <p class="tip">{{ t('login.bindTip') }}</p>
            </div>
            <div v-else-if="qrStatus === 3" class="qr-expired">
              <el-icon :size="48" color="#909399"><Timer /></el-icon>
              <p>{{ t('login.expiredTip') }}</p>
              <el-button @click="refreshQrCode">{{ t('login.refreshQr') }}</el-button>
            </div>
          </div>

          <div class="card-footer">
            <span class="footer-text">{{ t('login.noAccount') }}</span>
            <router-link to="/register" class="footer-link">{{ t('login.registerNow') }}</router-link>
          </div>
        </div>
        </div>

        <p class="copyright">{{ t('login.copyright') }}</p>
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { type FormInstance, type FormRules } from 'element-plus'
import { ChatDotRound, Loading, CircleCheck, Warning, Timer } from '@element-plus/icons-vue'
import { useAuthStore } from '@/stores'
import { getWechatQrCode, checkWechatLoginStatus } from '@/api/wechatAuth'
import { COMPANY_LOGIN_LOGO_URL } from '@/api/companyProfile'
import fallbackLoginLogoUrl from '@/assets/brand/semicore-login-logo.png'

const REMEMBER_USER_KEY = 'frontcrm_login_remember_user'

const router = useRouter()
const authStore = useAuthStore()
const { t } = useI18n()

const formRef = ref<FormInstance>()
const loading = ref(false)
const errorMsg = ref('')
const rememberMe = ref(false)

const loginType = ref<'password' | 'wechat'>('password')
const isWechatLogin = ref(false)

/** 公司信息未配置 Logo 或接口 404 时回退到内置图 */
const loginLogoUseFallback = ref(false)
const loginBrandLogoSrc = computed(() =>
  loginLogoUseFallback.value ? fallbackLoginLogoUrl : COMPANY_LOGIN_LOGO_URL
)

function onLoginBrandLogoError() {
  if (!loginLogoUseFallback.value) loginLogoUseFallback.value = true
}

const qrCodeUrl = ref('')
const qrTicket = ref('')
const qrStatus = ref(0)
let pollingTimer: number | null = null

const form = reactive({
  userName: '',
  password: ''
})

const rules: FormRules = {
  userName: [
    { required: true, message: t('login.userNameRequired'), trigger: 'blur' },
    { min: 2, max: 50, message: t('login.userNameLength'), trigger: 'blur' }
  ],
  password: [
    { required: true, message: t('login.passwordRequired'), trigger: 'blur' },
    { min: 6, message: t('login.passwordLength'), trigger: 'blur' }
  ]
}

onMounted(() => {
  try {
    const saved = localStorage.getItem(REMEMBER_USER_KEY)
    if (saved) {
      form.userName = saved
      rememberMe.value = true
    }
  } catch {
    /* ignore */
  }
})

async function switchToWechatLogin() {
  loginType.value = 'wechat'
  isWechatLogin.value = true
  await generateQrCode()
}

function switchToPasswordLogin() {
  loginType.value = 'password'
  isWechatLogin.value = false
  stopPolling()
}

async function generateQrCode() {
  try {
    qrStatus.value = 0
    qrCodeUrl.value = ''
    const res = (await getWechatQrCode({ deviceType: 'web' })) as any
    if (res?.qrCodeUrl) {
      qrCodeUrl.value = res.qrCodeUrl
      qrTicket.value = res.ticket
      startPolling()
    }
  } catch (error) {
    console.error('生成二维码失败', error)
  }
}

function refreshQrCode() {
  stopPolling()
  void generateQrCode()
}

function startPolling() {
  pollingTimer = window.setInterval(async () => {
    try {
      const res = (await checkWechatLoginStatus(qrTicket.value)) as any
      if (res?.status == null) return

      qrStatus.value = res.status

      switch (res.status) {
        case 2:
          stopPolling()
          if (res.authData) {
            const authData = res.authData
            authStore.token = authData.token
            authStore.user = {
              id: authData.userId || '0',
              userName: authData.userName,
              email: authData.email,
              isSysAdmin: !!authData.isSysAdmin,
              roleCodes: authData.roleCodes || [],
              permissionCodes: authData.permissionCodes || [],
              departmentIds: authData.departmentIds || []
            }
            localStorage.setItem('token', authData.token)
            localStorage.setItem('user', JSON.stringify(authStore.user))
            router.push('/dashboard')
          }
          break
        case 5:
          stopPolling()
          break
        case 3:
          stopPolling()
          break
      }
    } catch (error) {
      console.error('轮询失败', error)
    }
  }, 2000)
}

function stopPolling() {
  if (pollingTimer) {
    clearInterval(pollingTimer)
    pollingTimer = null
  }
}

onUnmounted(() => {
  stopPolling()
})

const handleLogin = async () => {
  if (!formRef.value) return
  errorMsg.value = ''

  await formRef.value.validate(async (valid) => {
    if (!valid) return

    loading.value = true
    try {
      const success = await authStore.login(form)
      if (success) {
        try {
          if (rememberMe.value && form.userName.trim()) {
            localStorage.setItem(REMEMBER_USER_KEY, form.userName.trim())
          } else {
            localStorage.removeItem(REMEMBER_USER_KEY)
          }
        } catch {
          /* ignore */
        }
        router.push('/dashboard')
      } else {
        errorMsg.value = t('login.loginFailedDefault')
      }
    } catch (error: any) {
      errorMsg.value = error.message || error.response?.data?.message || t('login.loginFailedRetry')
    } finally {
      loading.value = false
    }
  })
}
</script>

<style scoped lang="scss">
@import url('https://fonts.googleapis.com/css2?family=Noto+Sans+SC:wght@300;400;500;600;700&display=swap');

.login-view {
  /* 主内容区垂直跨度：介于上下留白（示意黄线）之间，随视口增高 */
  --login-band-min-h: clamp(520px, min(74vh, calc(100vh - 10.5rem)), 840px);
  min-height: 100vh;
  width: 100%;
  font-family: 'Noto Sans SC', system-ui, sans-serif;
}

.login-split {
  display: flex;
  min-height: 100vh;
  width: 100%;
}

/* ========== 左侧 Slogan ========== */
.login-slogan {
  flex: 1;
  min-width: 0;
  min-height: 100vh;
  box-sizing: border-box;
  position: relative;
  background: linear-gradient(160deg, #0b1426 0%, #0f1f3a 45%, #0c1730 100%);
  color: #e8eef8;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  padding: clamp(56px, 10vh, 120px) clamp(40px, 4vw, 56px);
}

.slogan-bg {
  position: absolute;
  inset: 0;
  pointer-events: none;
}

.slogan-orbit {
  position: absolute;
  border: 1px solid rgba(255, 255, 255, 0.06);
  border-radius: 50%;
  opacity: 0.85;
}

.slogan-orbit--1 {
  width: 120%;
  height: 140%;
  left: -35%;
  top: -30%;
}

.slogan-orbit--2 {
  width: 95%;
  height: 110%;
  right: -40%;
  bottom: -35%;
}

.slogan-orbit--3 {
  width: 70%;
  height: 70%;
  left: 8%;
  bottom: -18%;
  opacity: 0.5;
}

.slogan-beam {
  position: absolute;
  top: -20%;
  right: -5%;
  width: 55%;
  height: 70%;
  background: radial-gradient(ellipse at 70% 20%, rgba(56, 189, 248, 0.22) 0%, transparent 55%);
  filter: blur(2px);
}

.slogan-inner {
  position: relative;
  z-index: 1;
  max-width: 440px;
  width: 100%;
  min-height: var(--login-band-min-h);
  display: flex;
  flex-direction: column;
  justify-content: space-evenly;
  align-items: stretch;
  box-sizing: border-box;
}

.slogan-brand {
  display: flex;
  align-items: center;
  margin: 0;
}

.slogan-brand-mark {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 10px 18px;
  border-radius: 12px;
  background: rgba(255, 255, 255, 0.96);
  box-shadow: 0 8px 28px rgba(15, 23, 42, 0.35);
}

.slogan-brand-img {
  display: block;
  height: clamp(26px, 3.6vh, 36px);
  width: auto;
  max-width: min(220px, 72vw);
  object-fit: contain;
}

.slogan-headline {
  margin: 0;
  font-size: clamp(2rem, 4vw, 2.75rem);
  font-weight: 700;
  line-height: 1.2;
  letter-spacing: 0.02em;
}

.slogan-line {
  display: block;
}

.slogan-line--white {
  color: #f8fafc;
}

.slogan-line--accent {
  color: #38bdf8;
  margin-top: clamp(10px, 1.4vh, 18px);
}

.slogan-tags {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-wrap: wrap;
  gap: clamp(12px, 1.6vh, 18px) clamp(12px, 1.8vw, 16px);
}

.slogan-tags li {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: clamp(10px, 1.2vh, 14px) clamp(14px, 2vw, 18px);
  border-radius: 999px;
  background: rgba(15, 23, 42, 0.65);
  border: 1px solid rgba(148, 163, 184, 0.2);
  font-size: 0.8125rem;
  color: rgba(241, 245, 249, 0.92);
}

.slogan-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #22c55e;
  flex-shrink: 0;
  box-shadow: 0 0 8px rgba(34, 197, 94, 0.55);
}

/* ========== 右侧登录 ========== */
.login-panel {
  flex: 1;
  min-width: 0;
  min-height: 100vh;
  box-sizing: border-box;
  background: #fff;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 24px;
  position: relative;
}

.login-panel__toolbar {
  position: absolute;
  top: 24px;
  left: 24px;
  right: 24px;
  max-width: 400px;
  margin: 0 auto;
  width: calc(100% - 48px);
  display: flex;
  align-items: center;
  justify-content: space-between;
  z-index: 1;
}

.login-panel__middle {
  flex: 1;
  width: 100%;
  max-width: 400px;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: stretch;
  align-self: center;
  min-height: 0;
  padding-top: clamp(48px, 9vh, 100px);
  padding-bottom: clamp(32px, 5vh, 64px);
  box-sizing: border-box;
}

.version-tag {
  font-size: 11px;
  color: #94a3b8;
  letter-spacing: 0.04em;
}

.theme-moon {
  border: none;
  background: transparent;
  color: #64748b;
  padding: 8px;
  border-radius: 10px;
  cursor: pointer;
  transition: background 0.2s, color 0.2s;

  &:hover {
    background: #f1f5f9;
    color: #334155;
  }
}

.login-card {
  width: 100%;
  max-width: 400px;
  display: flex;
  flex-direction: column;
  gap: clamp(16px, 2.2vh, 28px);
  box-sizing: border-box;
  /* 登录整块（红框区域）相对原垂直位置整体下移 70px（原 100px 再上移 30px），左侧与顶栏不动 */
  transform: translateY(70px);
}

.panel-welcome {
  margin: 0;
  font-size: 1.625rem;
  font-weight: 700;
  color: #0f172a;
  letter-spacing: -0.02em;
}

.panel-welcome-sub {
  margin: 0;
  font-size: 0.875rem;
  color: #64748b;
  line-height: 1.55;
}

.login-tabs {
  display: flex;
  border-bottom: 1px solid #e2e8f0;
  margin: clamp(4px, 0.6vh, 10px) 0 0;
  gap: 0;
}

.tab-btn {
  flex: 1;
  padding: clamp(12px, 1.6vh, 16px) 0 clamp(14px, 1.8vh, 18px);
  background: transparent;
  border: none;
  border-bottom: 2px solid transparent;
  margin-bottom: -1px;
  color: #64748b;
  font-size: 0.8125rem;
  font-weight: 500;
  cursor: pointer;
  transition: color 0.2s, border-color 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;

  &:hover {
    color: #2563eb;
  }

  &.active {
    color: #2563eb;
    border-bottom-color: #2563eb;
  }
}

.login-form {
  :deep(.el-form-item) {
    margin-bottom: 0;
  }

  :deep(.el-form-item__error) {
    color: #dc2626;
    font-size: 12px;
    padding-top: 4px;
  }
}

.form-field {
  margin-bottom: clamp(20px, 2.6vh, 32px);
}

.field-label {
  display: block;
  font-size: 0.8125rem;
  font-weight: 500;
  color: #475569;
  margin-bottom: clamp(8px, 1vh, 12px);
}

.input-wrap {
  position: relative;
  display: flex;
  align-items: center;
}

.input-wrap--light .input-icon {
  position: absolute;
  left: 14px;
  z-index: 2;
  color: #94a3b8;
  display: flex;
  pointer-events: none;
}

.input-wrap--light:focus-within .input-icon {
  color: #2563eb;
}

.input-wrap--light :deep(.el-input__wrapper) {
  border-radius: 10px !important;
  box-shadow: none !important;
  border: 1px solid #e2e8f0 !important;
  background: #fff !important;
  padding-left: 44px !important;
  min-height: 46px;
  transition: border-color 0.2s, box-shadow 0.2s;

  &:hover {
    border-color: #cbd5e1 !important;
  }

  &.is-focus {
    border-color: #2563eb !important;
    box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.12) !important;
  }
}

.input-wrap--light :deep(.el-input__inner) {
  color: #0f172a !important;
  font-size: 0.9375rem;

  &::placeholder {
    color: #94a3b8 !important;
  }
}

.input-wrap--light :deep(.el-input__password) {
  color: #94a3b8 !important;

  &:hover {
    color: #2563eb !important;
  }
}

.form-row-meta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: clamp(20px, 2.5vh, 32px);
  margin-top: clamp(2px, 0.4vh, 8px);
  font-size: 0.8125rem;

  :deep(.el-checkbox__label) {
    color: #475569;
  }
}

.forgot-link {
  color: #2563eb;
  text-decoration: none;
  font-weight: 500;

  &:hover {
    text-decoration: underline;
  }
}

.error-alert {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 14px;
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 10px;
  color: #b91c1c;
  font-size: 0.8125rem;
  margin-bottom: clamp(14px, 1.8vh, 22px);
}

.login-btn--primary {
  width: 100%;
  height: 48px;
  border: none;
  border-radius: 10px;
  background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%);
  color: #fff;
  font-size: 0.9375rem;
  font-weight: 600;
  cursor: pointer;
  box-shadow: 0 8px 24px rgba(37, 99, 235, 0.28);
  transition: transform 0.15s, box-shadow 0.2s;

  &:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 12px 28px rgba(37, 99, 235, 0.35);
  }

  &:active:not(:disabled) {
    transform: translateY(0);
  }

  &:disabled {
    opacity: 0.75;
    cursor: not-allowed;
  }

  .btn-content,
  .btn-loading {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
  }

  .spinner {
    width: 16px;
    height: 16px;
    border: 2px solid rgba(255, 255, 255, 0.35);
    border-top-color: #fff;
    border-radius: 50%;
    animation: spin 0.7s linear infinite;
  }
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.card-footer {
  text-align: center;
  margin-top: clamp(20px, 2.8vh, 36px);
  padding-top: clamp(20px, 2.5vh, 32px);
  padding-bottom: clamp(8px, 1vh, 14px);
  border-top: 1px solid #f1f5f9;
}

.footer-text {
  font-size: 0.8125rem;
  color: #64748b;
}

.footer-link {
  font-size: 0.8125rem;
  color: #2563eb;
  font-weight: 500;
  text-decoration: none;
  margin-left: 4px;

  &:hover {
    text-decoration: underline;
  }
}

.copyright {
  flex-shrink: 0;
  margin-top: auto;
  padding-top: 16px;
  padding-bottom: 8px;
  font-size: 11px;
  color: #cbd5e1;
  text-align: center;
  max-width: 400px;
  width: 100%;
}

/* 微信扫码 */
.wechat-login-area {
  text-align: center;
  padding: clamp(12px, 2vh, 24px) 0 clamp(8px, 1vh, 16px);
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: center;
  min-height: 0;

  .qr-waiting .qr-loading {
    padding: 32px;

    .loading-icon {
      font-size: 32px;
      color: #2563eb;
      animation: spin 1s linear infinite;
    }

    p {
      margin-top: 12px;
      color: #64748b;
    }
  }

  .qr-code {
    width: 200px;
    height: 200px;
    border-radius: 8px;
    background: #fff;
    padding: 10px;
    border: 1px solid #e2e8f0;
  }

  .qr-tip {
    margin-top: 12px;
    color: #64748b;
    font-size: 14px;
  }

  .qr-scanned,
  .qr-unbound,
  .qr-expired {
    padding: 32px 16px;

    p {
      margin-top: 12px;
      color: #334155;
    }
  }

  .qr-unbound h3 {
    margin: 12px 0 8px;
    color: #d97706;
    font-size: 1.125rem;
  }

  .qr-unbound .actions {
    margin: 20px 0;
  }

  .qr-unbound .tip {
    color: #64748b;
    font-size: 13px;
    line-height: 1.7;
  }
}

@media (max-width: 900px) {
  .login-view {
    --login-band-min-h: min(440px, calc(100dvh - 15rem));
  }

  .login-split {
    flex-direction: column;
  }

  .login-slogan {
    min-height: auto;
    padding: 32px 24px 40px;
  }

  .slogan-inner {
    max-width: 100%;
    justify-content: center;
    gap: clamp(16px, 2.2vh, 24px);
    min-height: min(380px, calc(100dvh - 17rem));
  }

  .login-panel {
    min-height: auto;
    padding-top: 56px;
  }

  .login-panel__middle {
    justify-content: flex-start;
    padding-top: 12px;
    padding-bottom: 20px;
  }
}
</style>
