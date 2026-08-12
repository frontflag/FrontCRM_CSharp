<template>
  <div class="ecoinf-login">
    <header class="ecoinf-login__header">
      <div class="ecoinf-brand">
        <div class="ecoinf-brand__mark" aria-hidden="true">
          <svg viewBox="0 0 48 48" width="40" height="40" fill="none">
            <circle cx="24" cy="24" r="22" stroke="#c8ff00" stroke-width="1.5" />
            <text x="24" y="18" text-anchor="middle" fill="#c8ff00" font-size="8" font-weight="700">易趣合</text>
            <text x="24" y="32" text-anchor="middle" fill="#c8ff00" font-size="9" font-weight="700">ECO</text>
          </svg>
        </div>
        <span class="ecoinf-brand__name">eco-inf</span>
      </div>
    </header>

    <div class="ecoinf-login__body">
      <aside class="ecoinf-login__hero" aria-label="brand">
        <div class="ecoinf-hero__frame">
          <span class="ecoinf-hero__corner ecoinf-hero__corner--tl" aria-hidden="true" />
          <span class="ecoinf-hero__corner ecoinf-hero__corner--br" aria-hidden="true" />
          <h1 class="ecoinf-hero__title">{{ sloganLine1 }}</h1>
          <p class="ecoinf-hero__subtitle">{{ sloganLine2 }}</p>
        </div>
        <p class="ecoinf-hero__desc">{{ descriptionText }}</p>
        <div class="ecoinf-hero__chip" aria-hidden="true">
          <svg viewBox="0 0 120 120" width="120" height="120" fill="none">
            <rect x="20" y="20" width="80" height="80" rx="8" stroke="rgba(255,255,255,0.35)" stroke-width="2" />
            <rect x="36" y="36" width="48" height="48" rx="4" stroke="rgba(255,255,255,0.2)" stroke-width="1.5" />
            <line x1="20" y1="44" x2="8" y2="44" stroke="rgba(255,255,255,0.25)" stroke-width="2" />
            <line x1="20" y1="60" x2="8" y2="60" stroke="rgba(255,255,255,0.25)" stroke-width="2" />
            <line x1="20" y1="76" x2="8" y2="76" stroke="rgba(255,255,255,0.25)" stroke-width="2" />
            <line x1="100" y1="44" x2="112" y2="44" stroke="rgba(255,255,255,0.25)" stroke-width="2" />
            <line x1="100" y1="60" x2="112" y2="60" stroke="rgba(255,255,255,0.25)" stroke-width="2" />
            <line x1="100" y1="76" x2="112" y2="76" stroke="rgba(255,255,255,0.25)" stroke-width="2" />
            <line x1="44" y1="20" x2="44" y2="8" stroke="rgba(255,255,255,0.25)" stroke-width="2" />
            <line x1="60" y1="20" x2="60" y2="8" stroke="rgba(255,255,255,0.25)" stroke-width="2" />
            <line x1="76" y1="20" x2="76" y2="8" stroke="rgba(255,255,255,0.25)" stroke-width="2" />
            <line x1="44" y1="100" x2="44" y2="112" stroke="rgba(255,255,255,0.25)" stroke-width="2" />
            <line x1="60" y1="100" x2="60" y2="112" stroke="rgba(255,255,255,0.25)" stroke-width="2" />
            <line x1="76" y1="100" x2="76" y2="112" stroke="rgba(255,255,255,0.25)" stroke-width="2" />
          </svg>
        </div>
      </aside>

      <main class="ecoinf-login__panel">
        <div class="ecoinf-auth-card">
          <h2 class="ecoinf-auth__title">{{ welcomeTitle }}</h2>
          <p class="ecoinf-auth__subtitle">{{ welcomeSub }}</p>

          <el-form ref="formRef" :model="form" :rules="rules" class="ecoinf-auth__form" @submit.prevent="handleLogin">
            <div class="ecoinf-field">
              <label class="ecoinf-field__label">{{ t('login.accountLabelEcoinf') }}</label>
              <el-form-item prop="userName">
                <div class="ecoinf-input">
                  <span class="ecoinf-input__icon" aria-hidden="true">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
                      <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                      <circle cx="12" cy="7" r="4" />
                    </svg>
                  </span>
                  <el-input
                    v-model="form.userName"
                    :placeholder="accountPlaceholder"
                    class="ecoinf-input__el"
                  />
                </div>
              </el-form-item>
            </div>

            <div class="ecoinf-field">
              <div class="ecoinf-field__row">
                <label class="ecoinf-field__label">{{ t('login.passwordLabel') }}</label>
                <a href="#" class="ecoinf-forgot" @click.prevent>{{ t('login.forgotPassword') }}</a>
              </div>
              <el-form-item prop="password">
                <div class="ecoinf-input">
                  <span class="ecoinf-input__icon" aria-hidden="true">
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
                    class="ecoinf-input__el"
                  />
                </div>
              </el-form-item>
            </div>

            <div class="ecoinf-remember">
              <el-checkbox v-model="rememberMe">{{ t('login.rememberMe30Days') }}</el-checkbox>
            </div>

            <div v-if="errorMsg" class="ecoinf-error">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
              </svg>
              {{ errorMsg }}
            </div>

            <button type="submit" class="ecoinf-submit" :disabled="loading">
              <span v-if="!loading">{{ t('login.loginButtonEcoinf') }}</span>
              <span v-else class="ecoinf-submit__loading">
                <span class="ecoinf-spinner" />
                {{ t('login.validating') }}
              </span>
            </button>
          </el-form>

          <p class="ecoinf-auth__help">
            <a href="#" @click.prevent>{{ t('login.contactAdmin') }}</a>
          </p>
        </div>
      </main>
    </div>

    <footer class="ecoinf-login__footer">
      <span class="ecoinf-login__copyright">{{ copyrightText }}</span>
      <span class="ecoinf-login__status">
        <span class="ecoinf-login__status-dot" aria-hidden="true" />
        {{ t('login.systemStatusOperational') }}
      </span>
    </footer>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { type FormInstance, type FormRules } from 'element-plus'
import { useAuthStore } from '@/stores'
import { loginTenantText, loginThemeCssHref } from '@/config/loginTenant'

const REMEMBER_USER_KEY = 'frontcrm_login_remember_user'
const ECoinf_TENANT_ID = 'ecoinf'

let loginThemeLinkEl: HTMLLinkElement | null = null

const router = useRouter()
const authStore = useAuthStore()
const { t, locale } = useI18n()

const formRef = ref<FormInstance>()
const loading = ref(false)
const errorMsg = ref('')
const rememberMe = ref(false)

const sloganLine1 = computed(() =>
  loginTenantText('VITE_LOGIN_SLOGAN_LINE1', t('login.ecoinfSloganLine1'), locale.value)
)
const sloganLine2 = computed(() =>
  loginTenantText('VITE_LOGIN_SLOGAN_LINE2', t('login.ecoinfSloganLine2'), locale.value)
)
const descriptionText = computed(() => {
  if (!String(locale.value).toLowerCase().startsWith('zh')) {
    return t('login.ecoinfDescription')
  }
  return import.meta.env.VITE_LOGIN_DESCRIPTION?.trim() || t('login.ecoinfDescription')
})
const welcomeTitle = computed(() =>
  loginTenantText('VITE_LOGIN_WELCOME_TITLE', t('login.ecoinfWelcomeTitle'), locale.value)
)
const welcomeSub = computed(() =>
  loginTenantText('VITE_LOGIN_WELCOME_SUB', t('login.ecoinfWelcomeSub'), locale.value)
)
const copyrightText = computed(() =>
  loginTenantText('VITE_LOGIN_COPYRIGHT', t('login.ecoinfCopyright'), locale.value)
)
const accountPlaceholder = computed(() =>
  (import.meta.env.VITE_LOGIN_ACCOUNT_PLACEHOLDER?.trim() || 'user@ecoinf.com')
)

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
  loginThemeLinkEl = document.createElement('link')
  loginThemeLinkEl.rel = 'stylesheet'
  loginThemeLinkEl.href = loginThemeCssHref(ECoinf_TENANT_ID)
  loginThemeLinkEl.setAttribute('data-login-tenant-theme', ECoinf_TENANT_ID)
  document.head.appendChild(loginThemeLinkEl)

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

onUnmounted(() => {
  if (loginThemeLinkEl?.parentNode) {
    loginThemeLinkEl.parentNode.removeChild(loginThemeLinkEl)
    loginThemeLinkEl = null
  }
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
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Noto+Sans+SC:wght@400;500;600;700&display=swap');

.ecoinf-login {
  --ecoinf-accent: #c8ff00;
  --ecoinf-accent-dim: rgba(200, 255, 0, 0.12);
  --ecoinf-bg: #0a0a0a;
  --ecoinf-card: #141414;
  --ecoinf-border: rgba(255, 255, 255, 0.08);
  --ecoinf-text: #f5f5f5;
  --ecoinf-muted: #8a8a8a;

  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background-color: var(--ecoinf-bg);
  background-image:
    linear-gradient(rgba(255, 255, 255, 0.03) 1px, transparent 1px),
    linear-gradient(90deg, rgba(255, 255, 255, 0.03) 1px, transparent 1px);
  background-size: 48px 48px;
  color: var(--ecoinf-text);
  font-family: 'Inter', 'Noto Sans SC', system-ui, sans-serif;
}

.ecoinf-login__header {
  flex-shrink: 0;
  padding: 28px 48px 0;
}

.ecoinf-brand {
  display: inline-flex;
  align-items: center;
  gap: 14px;
}

.ecoinf-brand__mark {
  display: flex;
  align-items: center;
  justify-content: center;
}

.ecoinf-brand__name {
  font-size: 1.75rem;
  font-weight: 700;
  letter-spacing: -0.03em;
}

.ecoinf-login__body {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: clamp(32px, 6vw, 80px);
  padding: clamp(32px, 6vh, 64px) clamp(24px, 5vw, 72px);
  max-width: 1280px;
  width: 100%;
  margin: 0 auto;
  box-sizing: border-box;
}

.ecoinf-login__hero {
  flex: 1;
  min-width: 0;
  max-width: 520px;
  display: flex;
  flex-direction: column;
  gap: 28px;
}

.ecoinf-hero__frame {
  position: relative;
  padding: 8px 0 0;
}

.ecoinf-hero__corner {
  position: absolute;
  width: 28px;
  height: 28px;
  border-color: var(--ecoinf-accent);
  border-style: solid;
  opacity: 0.85;
}

.ecoinf-hero__corner--tl {
  top: 0;
  left: -40px;
  border-width: 2px 0 0 2px;
}

.ecoinf-hero__corner--br {
  right: 40px;
  bottom: 0;
  border-width: 0 2px 2px 0;
}

.ecoinf-hero__title {
  margin: 0 0 16px;
  font-size: clamp(2rem, 4.2vw, 3rem);
  font-weight: 700;
  line-height: 1.15;
  letter-spacing: 0.02em;
}

.ecoinf-hero__subtitle {
  margin: 0;
  font-size: clamp(0.75rem, 1.2vw, 0.875rem);
  font-weight: 600;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ecoinf-accent);
}

.ecoinf-hero__desc {
  margin: 0;
  max-width: 420px;
  font-size: 0.9375rem;
  line-height: 1.65;
  color: var(--ecoinf-muted);
}

.ecoinf-hero__chip {
  margin-top: auto;
  padding-top: 32px;
  opacity: 0.9;
}

.ecoinf-login__panel {
  flex: 1;
  min-width: 0;
  max-width: 440px;
  display: flex;
  justify-content: center;
}

.ecoinf-auth-card {
  width: 100%;
  padding: clamp(28px, 4vh, 40px);
  border-radius: 4px;
  background: var(--ecoinf-card);
  border: 1px solid var(--ecoinf-border);
  box-shadow: 0 24px 64px rgba(0, 0, 0, 0.45);
}

.ecoinf-auth__title {
  margin: 0;
  font-size: 1.75rem;
  font-weight: 700;
  letter-spacing: 0.02em;
}

.ecoinf-auth__subtitle {
  margin: 8px 0 28px;
  font-size: 0.6875rem;
  font-weight: 600;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--ecoinf-muted);
}

.ecoinf-auth__form {
  :deep(.el-form-item) {
    margin-bottom: 0;
  }

  :deep(.el-form-item__error) {
    color: #ff6b6b;
    font-size: 12px;
    padding-top: 4px;
  }
}

.ecoinf-field {
  margin-bottom: 22px;
}

.ecoinf-field__row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 10px;
}

.ecoinf-field__label {
  display: block;
  font-size: 0.8125rem;
  font-weight: 500;
  color: rgba(245, 245, 245, 0.88);
  margin-bottom: 10px;
}

.ecoinf-field__row .ecoinf-field__label {
  margin-bottom: 0;
}

.ecoinf-forgot {
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--ecoinf-accent);
  text-decoration: none;

  &:hover {
    text-decoration: underline;
  }
}

.ecoinf-input {
  position: relative;
  display: flex;
  align-items: center;
}

.ecoinf-input__icon {
  position: absolute;
  left: 14px;
  z-index: 2;
  color: var(--ecoinf-muted);
  display: flex;
  pointer-events: none;
}

.ecoinf-input:focus-within .ecoinf-input__icon {
  color: var(--ecoinf-accent);
}

.ecoinf-input :deep(.el-input__wrapper) {
  border-radius: 6px !important;
  box-shadow: none !important;
  border: 1px solid var(--ecoinf-border) !important;
  background: rgba(0, 0, 0, 0.35) !important;
  padding-left: 44px !important;
  min-height: 48px;
  transition: border-color 0.2s, box-shadow 0.2s;

  &:hover {
    border-color: rgba(255, 255, 255, 0.16) !important;
  }

  &.is-focus {
    border-color: var(--ecoinf-accent) !important;
    box-shadow: 0 0 0 2px var(--ecoinf-accent-dim) !important;
  }
}

.ecoinf-input :deep(.el-input__inner) {
  color: var(--ecoinf-text) !important;
  font-size: 0.9375rem;

  &::placeholder {
    color: rgba(138, 138, 138, 0.85) !important;
  }
}

.ecoinf-input :deep(.el-input__password) {
  color: var(--ecoinf-muted) !important;

  &:hover {
    color: var(--ecoinf-accent) !important;
  }
}

.ecoinf-remember {
  margin-bottom: 24px;

  :deep(.el-checkbox__label) {
    color: var(--ecoinf-muted);
    font-size: 0.8125rem;
  }

  :deep(.el-checkbox__input.is-checked .el-checkbox__inner) {
    background-color: var(--ecoinf-accent);
    border-color: var(--ecoinf-accent);
  }

  :deep(.el-checkbox__input.is-checked + .el-checkbox__label) {
    color: rgba(245, 245, 245, 0.85);
  }
}

.ecoinf-error {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 14px;
  margin-bottom: 16px;
  border-radius: 6px;
  background: rgba(255, 80, 80, 0.1);
  border: 1px solid rgba(255, 80, 80, 0.25);
  color: #ff8a8a;
  font-size: 0.8125rem;
}

.ecoinf-submit {
  width: 100%;
  height: 50px;
  border: none;
  border-radius: 6px;
  background: var(--ecoinf-accent);
  color: #0a0a0a;
  font-size: 0.9375rem;
  font-weight: 700;
  letter-spacing: 0.02em;
  cursor: pointer;
  transition: transform 0.15s, filter 0.2s;

  &:hover:not(:disabled) {
    filter: brightness(1.05);
    transform: translateY(-1px);
  }

  &:disabled {
    opacity: 0.7;
    cursor: not-allowed;
  }
}

.ecoinf-submit__loading {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

.ecoinf-spinner {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(10, 10, 10, 0.25);
  border-top-color: #0a0a0a;
  border-radius: 50%;
  animation: ecoinf-spin 0.7s linear infinite;
}

@keyframes ecoinf-spin {
  to {
    transform: rotate(360deg);
  }
}

.ecoinf-auth__help {
  margin: 24px 0 0;
  text-align: center;
  font-size: 0.8125rem;

  a {
    color: var(--ecoinf-muted);
    text-decoration: none;

    &:hover {
      color: var(--ecoinf-accent);
    }
  }
}

.ecoinf-login__footer {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 16px 48px 24px;
  font-size: 0.6875rem;
  letter-spacing: 0.04em;
  color: rgba(138, 138, 138, 0.9);
  border-top: 1px solid rgba(255, 255, 255, 0.04);
}

.ecoinf-login__status {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.ecoinf-login__status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--ecoinf-accent);
  box-shadow: 0 0 10px rgba(200, 255, 0, 0.65);
}

@media (max-width: 900px) {
  .ecoinf-login__body {
    flex-direction: column;
    align-items: stretch;
  }

  .ecoinf-login__hero,
  .ecoinf-login__panel {
    max-width: 100%;
  }

  .ecoinf-hero__chip {
    display: none;
  }

  .ecoinf-login__footer {
    flex-direction: column;
    align-items: flex-start;
    padding-inline: 24px;
  }
}
</style>
