<template>
  <div class="vendor-home">
    <div class="vendor-home__search-hero">
      <div class="vendor-home__search-row">
        <!-- 与客户首页一致：药丸在「统计区左缘 ~ 新建按钮左缘」之间水平居中 -->
        <div class="vendor-home__pill-slot">
          <div class="vendor-home__pill-wrap">
            <div class="vendor-home__pill" role="search">
              <div class="vendor-home__pill-field">
                <svg
                  class="vendor-home__pill-mag"
                  width="18"
                  height="18"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2"
                  aria-hidden="true"
                >
                  <circle cx="11" cy="11" r="8" />
                  <line x1="21" y1="21" x2="16.65" y2="16.65" />
                </svg>
                <input
                  v-model="keyword"
                  type="search"
                  class="vendor-home__pill-input"
                  :placeholder="canViewVendorInfo ? t('vendorHome.searchPlaceholderFull') : t('vendorHome.searchPlaceholderCode')"
                  autocomplete="off"
                  @keyup.enter="handleSearch"
                />
              </div>
              <button type="button" class="vendor-home__pill-btn" @click="handleSearch">{{ t('vendorHome.search') }}</button>
              <button type="button" class="vendor-home__pill-link" @click="goListPlain">{{ t('vendorHome.goList') }}</button>
            </div>
          </div>
        </div>
        <button
          v-if="canCreateVendor"
          type="button"
          class="vendor-home__btn-create"
          @click="goCreateVendor"
        >
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          {{ t('vendorHome.create') }}
        </button>
      </div>
    </div>

    <div v-if="loadingStats" class="vendor-home__loading">{{ t('vendorHome.loading') }}</div>

    <template v-else-if="stats">
      <div class="vendor-home__section-title">{{ t('vendorHome.sections.vendor') }}</div>
      <div class="vendor-home__row vendor-home__row--4">
        <div class="vendor-home__card">
          <div class="vendor-home__icon vendor-home__icon--blue" aria-hidden="true">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
              <circle cx="9" cy="7" r="4" />
            </svg>
          </div>
          <div class="vendor-home__card-main">
            <div class="vendor-home__value">{{ formatInt(stats.totalVendors) }}</div>
            <div class="vendor-home__label">{{ t('vendorHome.cards.totalVendors') }}</div>
          </div>
        </div>
        <div class="vendor-home__card">
          <div class="vendor-home__icon vendor-home__icon--green" aria-hidden="true">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <polyline points="22 12 18 12 15 21 9 3 6 12 2 12" />
            </svg>
          </div>
          <div class="vendor-home__card-main">
            <div class="vendor-home__value">{{ formatInt(stats.activeVendors) }}</div>
            <div class="vendor-home__label">{{ t('vendorHome.cards.activeVendors') }}</div>
          </div>
        </div>
        <div class="vendor-home__card">
          <div class="vendor-home__icon vendor-home__icon--amber" aria-hidden="true">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
              <circle cx="8.5" cy="7" r="4" />
              <polyline points="17 11 19 13 23 9" />
            </svg>
          </div>
          <div class="vendor-home__card-main">
            <div class="vendor-home__value">{{ formatInt(stats.vendorsWithDeals) }}</div>
            <div class="vendor-home__label">{{ t('vendorHome.cards.vendorsWithDeals') }}</div>
          </div>
        </div>
        <div class="vendor-home__card">
          <div class="vendor-home__icon vendor-home__icon--violet" aria-hidden="true">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <line x1="12" y1="5" x2="12" y2="19" />
              <line x1="5" y1="12" x2="19" y2="12" />
            </svg>
          </div>
          <div class="vendor-home__card-main">
            <div class="vendor-home__value">{{ formatInt(stats.newLast30Days) }}</div>
            <div class="vendor-home__label">{{ t('vendorHome.cards.newLast30Days') }}</div>
          </div>
        </div>
      </div>

      <div class="vendor-home__duo">
        <div class="vendor-home__duo-col">
          <div class="vendor-home__section-title">{{ t('vendorHome.sections.payable') }}</div>
          <div class="vendor-home__row vendor-home__row--2">
            <div class="vendor-home__card">
              <div class="vendor-home__icon vendor-home__icon--blue" aria-hidden="true">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                  <line x1="12" y1="1" x2="12" y2="23" />
                  <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6" />
                </svg>
              </div>
              <div class="vendor-home__card-main">
                <div class="vendor-home__value">{{ maskPurchaseSensitiveFields ? '—' : formatMoney(stats.payableAmount) }}</div>
                <div class="vendor-home__label">{{ t('vendorHome.cards.payableAmount') }}</div>
              </div>
            </div>
            <div class="vendor-home__card">
              <div class="vendor-home__icon vendor-home__icon--teal" aria-hidden="true">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                  <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
                  <circle cx="9" cy="7" r="4" />
                  <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
                  <path d="M16 3.13a4 4 0 0 1 0 7.75" />
                </svg>
              </div>
              <div class="vendor-home__card-main">
                <div class="vendor-home__value">{{ formatInt(stats.payableVendorCount) }}</div>
                <div class="vendor-home__label">{{ t('vendorHome.cards.payableVendorCount') }}</div>
              </div>
            </div>
          </div>
        </div>
        <div class="vendor-home__duo-col">
          <div class="vendor-home__section-title">{{ t('vendorHome.sections.pendingInbound') }}</div>
          <div class="vendor-home__row vendor-home__row--2">
            <div class="vendor-home__card">
              <div class="vendor-home__icon vendor-home__icon--mint" aria-hidden="true">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                  <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z" />
                  <polyline points="3.27 6.96 12 12.01 20.73 6.96" />
                  <line x1="12" y1="22.08" x2="12" y2="12" />
                </svg>
              </div>
              <div class="vendor-home__card-main">
                <div class="vendor-home__value">{{ maskPurchaseSensitiveFields ? '—' : formatMoney(stats.pendingInboundAmount) }}</div>
                <div class="vendor-home__label">{{ t('vendorHome.cards.pendingInboundAmount') }}</div>
              </div>
            </div>
            <div class="vendor-home__card">
              <div class="vendor-home__icon vendor-home__icon--rose" aria-hidden="true">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                  <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
                  <circle cx="8.5" cy="7" r="4" />
                  <line x1="20" y1="8" x2="20" y2="14" />
                  <line x1="23" y1="11" x2="17" y2="11" />
                </svg>
              </div>
              <div class="vendor-home__card-main">
                <div class="vendor-home__value">{{ formatInt(stats.pendingInboundVendorCount) }}</div>
                <div class="vendor-home__label">{{ t('vendorHome.cards.pendingInboundVendorCount') }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { vendorApi } from '@/api/vendor'
import { buildVendorListQuery } from '@/utils/vendorListQuery'
import type { VendorStatistics } from '@/types/vendor'

const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const canViewVendorInfo = authStore.hasPermission('vendor.info.read')
const canCreateVendor = computed(() => authStore.hasPermission('vendor.write'))

const keyword = ref('')
const stats = ref<VendorStatistics | null>(null)
const loadingStats = ref(false)

const moneyFmt = new Intl.NumberFormat('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const intFmt = new Intl.NumberFormat('zh-CN')

function formatMoney(n: number) {
  return moneyFmt.format(n ?? 0)
}

function formatInt(n: number) {
  return intFmt.format(n ?? 0)
}

function handleSearch() {
  const q = buildVendorListQuery({
    searchTerm: keyword.value,
    favoriteOnly: false
  })
  router.push({ name: 'VendorList', query: q })
}

function goListPlain() {
  router.push({ name: 'VendorList', query: {} })
}

function goCreateVendor() {
  router.push({ name: 'VendorCreate' })
}

onMounted(async () => {
  loadingStats.value = true
  try {
    stats.value = await vendorApi.getVendorStatistics()
  } catch {
    stats.value = null
  } finally {
    loadingStats.value = false
  }
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&family=Noto+Sans+SC:wght@300;400;500;600&display=swap');

// 与客户首页 CustomerHome 对齐：深底、药丸搜索、底部应付款/待入库两列
.vendor-home {
  min-height: 100%;
  padding-top: clamp(120px, 19vh, 300px);
  padding-bottom: 40px;
  padding-inline: clamp(36px, 14vw, 240px);
  font-family: 'Noto Sans SC', sans-serif;
  background: $layer-1;
  background-image: radial-gradient(ellipse 120% 80% at 50% -20%, var(--crm-home-hero-glow), transparent 55%);
}

.vendor-home__search-hero {
  display: flex;
  justify-content: center;
  width: 100%;
  margin-bottom: clamp(96px, 12vh, 128px);
}

.vendor-home__search-row {
  display: flex;
  align-items: center;
  gap: 16px;
  width: 100%;
}

/* 与下方统计卡片同宽的左段：药丸在此段内居中（右边界为新建按钮左缘） */
.vendor-home__pill-slot {
  flex: 1;
  min-width: 0;
  display: flex;
  justify-content: center;
}

.vendor-home__pill-wrap {
  width: 100%;
  max-width: calc(100% * 2 / 3);
  min-width: 0;
}

.vendor-home__pill {
  display: flex;
  align-items: stretch;
  width: 100%;
  max-width: 100%;
  min-height: 52px;
  padding: 4px 6px 4px 4px;
  gap: 0;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: 999px;
  box-shadow: $shadow-md;
}

.vendor-home__pill-field {
  flex: 1;
  display: flex;
  align-items: center;
  min-width: 0;
  padding-left: 14px;
  padding-right: 8px;
}

.vendor-home__pill-mag {
  flex-shrink: 0;
  margin-right: 10px;
  color: $text-muted;
}

.vendor-home__pill-input {
  flex: 1;
  min-width: 0;
  border: none;
  background: transparent;
  color: $text-primary;
  font-size: 14px;
  font-family: inherit;
  outline: none;

  &::placeholder {
    color: $text-placeholder;
  }
}

.vendor-home__pill-btn {
  flex-shrink: 0;
  margin: 0 4px;
  padding: 0 22px;
  border: none;
  border-radius: 999px;
  font-size: 14px;
  font-weight: 500;
  font-family: inherit;
  color: #fff;
  cursor: pointer;
  background: linear-gradient(135deg, $blue-primary 0%, $cyan-primary 100%);
  box-shadow: var(--crm-shadow-glow);
  transition: filter 0.15s ease, transform 0.15s ease;

  &:hover {
    filter: brightness(1.06);
  }

  &:active {
    transform: scale(0.98);
  }
}

.vendor-home__pill-link {
  flex-shrink: 0;
  align-self: center;
  margin: 0 14px 0 8px;
  padding: 8px 4px;
  border: none;
  background: none;
  font-size: 13px;
  font-family: inherit;
  color: $cyan-primary;
  cursor: pointer;
  white-space: nowrap;
  transition: color 0.15s ease;
  opacity: 0.92;

  &:hover {
    opacity: 1;
    color: $cyan-primary;
  }
}

.vendor-home__btn-create {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  flex-shrink: 0;
  padding: 10px 18px;
  background: linear-gradient(135deg, $color-mint-green, $success-color);
  border: 1px solid color-mix(in srgb, $color-mint-green 55%, transparent);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  font-family: inherit;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  letter-spacing: 0.5px;
  white-space: nowrap;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px color-mix(in srgb, $color-mint-green 35%, transparent);
  }
}

.vendor-home__loading {
  text-align: center;
  color: $text-muted;
  padding: 40px;
  font-size: 14px;
}

.vendor-home__section-title {
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
  margin: 0 0 14px;
  letter-spacing: 0.04em;
}

.vendor-home__row {
  display: grid;
  gap: 14px;
  margin-bottom: 28px;

  &--4 {
    grid-template-columns: repeat(4, 1fr);
  }

  &--2 {
    grid-template-columns: repeat(2, 1fr);
  }

  @media (max-width: 1024px) {
    &--4 {
      grid-template-columns: repeat(2, 1fr);
    }
  }

  @media (max-width: 520px) {
    &--4,
    &--2 {
      grid-template-columns: 1fr;
    }
  }
}

.vendor-home__duo {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 28px 32px;
  align-items: start;

  @media (max-width: 900px) {
    grid-template-columns: 1fr;
  }
}

.vendor-home__duo-col {
  min-width: 0;
}

.vendor-home__card {
  display: flex;
  align-items: flex-start;
  gap: 14px;
  padding: 18px 18px 20px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: 14px;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;

  &:hover {
    border-color: var(--crm-accent-012);
    box-shadow: $shadow-md;
  }
}

.vendor-home__icon {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;

  &--blue {
    background: rgba(50, 149, 201, 0.22);
    color: #6ec8f0;
    border: 1px solid rgba(50, 149, 201, 0.35);
  }
  &--green {
    background: rgba(70, 191, 145, 0.2);
    color: #5ee0ad;
    border: 1px solid rgba(70, 191, 145, 0.35);
  }
  &--amber {
    background: rgba(201, 154, 69, 0.2);
    color: #e4b86a;
    border: 1px solid rgba(201, 154, 69, 0.35);
  }
  &--violet {
    background: rgba(138, 99, 210, 0.22);
    color: #b89cf0;
    border: 1px solid rgba(138, 99, 210, 0.38);
  }
  &--teal {
    background: rgba(0, 180, 170, 0.18);
    color: #5ee6dc;
    border: 1px solid rgba(0, 212, 255, 0.28);
  }
  &--mint {
    background: rgba(70, 191, 145, 0.18);
    color: #6bdfb0;
    border: 1px solid rgba(70, 191, 145, 0.32);
  }
  &--rose {
    background: rgba(201, 87, 69, 0.2);
    color: #f09080;
    border: 1px solid rgba(201, 87, 69, 0.35);
  }
}

.vendor-home__card-main {
  min-width: 0;
  padding-top: 2px;
}

.vendor-home__value {
  font-size: 24px;
  font-weight: 700;
  color: $text-primary;
  font-family: 'Space Mono', monospace;
  line-height: 1.15;
  letter-spacing: -0.02em;
}

.vendor-home__label {
  margin-top: 6px;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.35;
}
</style>
