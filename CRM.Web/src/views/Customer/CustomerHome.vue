<template>
  <div class="customer-home">
    <!-- 一体化搜索条（药丸形，搜索按钮与列表入口同条） -->
    <div class="customer-home__search-hero">
      <div class="customer-home__search-row">
        <!-- 药丸在「统计区左缘 ~ 新建按钮左缘」之间水平居中 -->
        <div class="customer-home__pill-slot">
          <div class="customer-home__pill-wrap">
            <div class="customer-home__pill" role="search">
              <div class="customer-home__pill-field">
              <svg
                class="customer-home__pill-mag"
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
                class="customer-home__pill-input"
                placeholder="输入客户名称、编号或关键词…"
                autocomplete="off"
                @keyup.enter="handleSearch"
              />
              </div>
              <button type="button" class="customer-home__pill-btn" @click="handleSearch">搜索</button>
              <button type="button" class="customer-home__pill-link" @click="goListPlain">进入列表查询</button>
            </div>
          </div>
        </div>
        <button
          v-if="canCreateCustomer"
          type="button"
          class="customer-home__btn-create"
          @click="goCreateCustomer"
        >
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          新建客户
        </button>
      </div>
    </div>

    <div v-if="loadingStats" class="customer-home__loading">加载概要数据…</div>

    <template v-else-if="stats">
      <div class="customer-home__section-title">客户</div>
      <div class="customer-home__row customer-home__row--4">
        <div class="customer-home__card">
          <div class="customer-home__icon customer-home__icon--blue" aria-hidden="true">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
              <circle cx="9" cy="7" r="4" />
            </svg>
          </div>
          <div class="customer-home__card-main">
            <div class="customer-home__value">{{ formatInt(stats.totalCustomers) }}</div>
            <div class="customer-home__label">客户总数</div>
          </div>
        </div>
        <div class="customer-home__card">
          <div class="customer-home__icon customer-home__icon--green" aria-hidden="true">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <polyline points="22 12 18 12 15 21 9 3 6 12 2 12" />
            </svg>
          </div>
          <div class="customer-home__card-main">
            <div class="customer-home__value">{{ formatInt(stats.activeCustomers) }}</div>
            <div class="customer-home__label">活跃客户</div>
          </div>
        </div>
        <div class="customer-home__card">
          <div class="customer-home__icon customer-home__icon--amber" aria-hidden="true">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
              <circle cx="8.5" cy="7" r="4" />
              <polyline points="17 11 19 13 23 9" />
            </svg>
          </div>
          <div class="customer-home__card-main">
            <div class="customer-home__value">{{ formatInt(stats.customersWithDeals) }}</div>
            <div class="customer-home__label">成单客户</div>
          </div>
        </div>
        <div class="customer-home__card">
          <div class="customer-home__icon customer-home__icon--violet" aria-hidden="true">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <line x1="12" y1="5" x2="12" y2="19" />
              <line x1="5" y1="12" x2="19" y2="12" />
            </svg>
          </div>
          <div class="customer-home__card-main">
            <div class="customer-home__value">{{ formatInt(stats.newLast30Days) }}</div>
            <div class="customer-home__label">30 天新增客户</div>
          </div>
        </div>
      </div>

      <div class="customer-home__duo">
        <div class="customer-home__duo-col">
          <div class="customer-home__section-title">应收款</div>
          <div class="customer-home__row customer-home__row--2">
            <div class="customer-home__card">
              <div class="customer-home__icon customer-home__icon--blue" aria-hidden="true">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                  <line x1="12" y1="1" x2="12" y2="23" />
                  <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6" />
                </svg>
              </div>
              <div class="customer-home__card-main">
                <div class="customer-home__value">{{ formatMoney(stats.receivableGoodsAmount) }}</div>
                <div class="customer-home__label">应收货款</div>
              </div>
            </div>
            <div class="customer-home__card">
              <div class="customer-home__icon customer-home__icon--teal" aria-hidden="true">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                  <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
                  <circle cx="9" cy="7" r="4" />
                  <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
                  <path d="M16 3.13a4 4 0 0 1 0 7.75" />
                </svg>
              </div>
              <div class="customer-home__card-main">
                <div class="customer-home__value">{{ formatInt(stats.receivableCustomerCount) }}</div>
                <div class="customer-home__label">应收客户</div>
              </div>
            </div>
          </div>
        </div>
        <div class="customer-home__duo-col">
          <div class="customer-home__section-title">待出库</div>
          <div class="customer-home__row customer-home__row--2">
            <div class="customer-home__card">
              <div class="customer-home__icon customer-home__icon--mint" aria-hidden="true">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                  <rect x="1" y="3" width="15" height="13" rx="2" />
                  <path d="M16 8h4l3 3v5h-7V8z" />
                  <circle cx="5.5" cy="18.5" r="2.5" />
                  <circle cx="18.5" cy="18.5" r="2.5" />
                </svg>
              </div>
              <div class="customer-home__card-main">
                <div class="customer-home__value">{{ formatMoney(stats.pendingOutboundAmount) }}</div>
                <div class="customer-home__label">待出货款</div>
              </div>
            </div>
            <div class="customer-home__card">
              <div class="customer-home__icon customer-home__icon--rose" aria-hidden="true">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                  <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
                  <circle cx="8.5" cy="7" r="4" />
                  <line x1="20" y1="8" x2="20" y2="14" />
                  <line x1="23" y1="11" x2="17" y2="11" />
                </svg>
              </div>
              <div class="customer-home__card-main">
                <div class="customer-home__value">{{ formatInt(stats.pendingOutboundCustomerCount) }}</div>
                <div class="customer-home__label">待出客户</div>
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
import { useAuthStore } from '@/stores/auth'
import { customerApi } from '@/api/customer'
import { buildCustomerListQuery } from '@/utils/customerListQuery'
import type { CustomerStatistics } from '@/types/customer'

const router = useRouter()
const authStore = useAuthStore()
const canCreateCustomer = computed(() => authStore.hasPermission('customer.write'))
const keyword = ref('')
const stats = ref<CustomerStatistics | null>(null)
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
  const q = buildCustomerListQuery({
    searchTerm: keyword.value,
    favoriteOnly: false
  })
  router.push({ name: 'CustomerList', query: q })
}

function goListPlain() {
  router.push({ name: 'CustomerList', query: {} })
}

function goCreateCustomer() {
  router.push({ name: 'CustomerCreate' })
}

onMounted(async () => {
  loadingStats.value = true
  try {
    stats.value = await customerApi.getCustomerStatistics()
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

// 参考稿：更深主底、居中一体化搜索条、底部两列并排
.customer-home {
  min-height: 100%;
  /* 标签栏 → 搜索条：参考稿约 15%–20% 视口高，搜索靠上区中部 */
  padding-top: clamp(120px, 19vh, 300px);
  padding-bottom: 40px;
  /* 主内容约 60%–70% 宽居中感：两侧宽留白（统计卡片距窗口边更远） */
  padding-inline: clamp(36px, 14vw, 240px);
  font-family: 'Noto Sans SC', sans-serif;
  background: $layer-1;
  background-image: radial-gradient(ellipse 120% 80% at 50% -20%, var(--crm-home-hero-glow), transparent 55%);
}

.customer-home__search-hero {
  display: flex;
  justify-content: center;
  width: 100%;
  /* 搜索条 → 「客户」统计区：参考稿约 100–120px */
  margin-bottom: clamp(96px, 12vh, 128px);
}

.customer-home__search-row {
  display: flex;
  align-items: center;
  gap: 16px;
  width: 100%;
}

/* 与下方统计卡片同宽的左段：药丸在此段内居中（右边界为新建按钮左缘） */
.customer-home__pill-slot {
  flex: 1;
  min-width: 0;
  display: flex;
  justify-content: center;
}

.customer-home__pill-wrap {
  width: 100%;
  max-width: calc(100% * 2 / 3);
  min-width: 0;
}

.customer-home__pill {
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

.customer-home__pill-field {
  flex: 1;
  display: flex;
  align-items: center;
  min-width: 0;
  padding-left: 14px;
  padding-right: 8px;
}

.customer-home__pill-mag {
  flex-shrink: 0;
  margin-right: 10px;
  color: $text-muted;
}

.customer-home__pill-input {
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

.customer-home__pill-btn {
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

.customer-home__pill-link {
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

.customer-home__btn-create {
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

.customer-home__loading {
  text-align: center;
  color: $text-muted;
  padding: 40px;
  font-size: 14px;
}

.customer-home__section-title {
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
  margin: 0 0 14px;
  letter-spacing: 0.04em;
}

.customer-home__row {
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

.customer-home__duo {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 28px 32px;
  align-items: start;

  @media (max-width: 900px) {
    grid-template-columns: 1fr;
  }
}

.customer-home__duo-col {
  min-width: 0;
}

.customer-home__card {
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

.customer-home__icon {
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

.customer-home__card-main {
  min-width: 0;
  padding-top: 2px;
}

.customer-home__value {
  font-size: 24px;
  font-weight: 700;
  color: $text-primary;
  font-family: 'Space Mono', monospace;
  line-height: 1.15;
  letter-spacing: -0.02em;
}

.customer-home__label {
  margin-top: 6px;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.35;
}
</style>
