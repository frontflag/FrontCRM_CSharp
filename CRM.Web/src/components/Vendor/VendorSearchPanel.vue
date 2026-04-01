<script setup lang="ts">
import { reactive, ref, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { authApi, type PurchaseUserSelectOption } from '@/api/auth'
import {
  buildVendorListQuery,
  parseVendorListQuery,
  type VendorListFilterQuery
} from '@/utils/vendorListQuery'
import { VENDOR_LEVEL_OPTIONS, VENDOR_IDENTITY_OPTIONS } from '@/constants/vendorEnums'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const { t } = useI18n()
const canViewVendorInfo = authStore.hasPermission('vendor.info.read')

const purchaseUsers = ref<PurchaseUserSelectOption[]>([])
const createdDateRange = ref<[string, string] | null>(null)

const form = reactive<VendorListFilterQuery>({
  searchTerm: '',
  status: undefined,
  level: undefined,
  credit: undefined,
  ascriptionType: undefined,
  industry: undefined,
  purchaseUserId: undefined,
  createdFrom: undefined,
  createdTo: undefined,
  favoriteOnly: false
})

function purchaserUserLabel(u: PurchaseUserSelectOption) {
  const name = u.realName || u.label || u.userName
  return u.userName && name !== u.userName ? `${name}(${u.userName})` : name
}

function syncFormFromRoute() {
  if (route.name !== 'VendorList') return
  const p = parseVendorListQuery(route.query)
  form.searchTerm = p.searchTerm
  form.status = p.status
  form.level = p.level
  form.credit = p.credit
  form.ascriptionType = p.ascriptionType
  form.industry = p.industry
  form.purchaseUserId = p.purchaseUserId
  form.createdFrom = p.createdFrom
  form.createdTo = p.createdTo
  form.favoriteOnly = p.favoriteOnly
  createdDateRange.value = p.createdFrom && p.createdTo ? [p.createdFrom, p.createdTo] : null
}

watch(
  () => [route.name, route.query] as const,
  () => syncFormFromRoute(),
  { deep: true, immediate: true }
)

function handleReset() {
  form.searchTerm = ''
  form.status = undefined
  form.level = undefined
  form.credit = undefined
  form.ascriptionType = undefined
  form.industry = undefined
  form.purchaseUserId = undefined
  form.createdFrom = undefined
  form.createdTo = undefined
  form.favoriteOnly = false
  createdDateRange.value = null
  router.push({ name: 'VendorList', query: {} })
}

function onCreatedRangeChange(val: [string, string] | null) {
  if (val && val.length === 2) {
    form.createdFrom = val[0]
    form.createdTo = val[1]
  } else {
    form.createdFrom = undefined
    form.createdTo = undefined
  }
}

function handleSearch() {
  const q = buildVendorListQuery(form)
  router.push({ name: 'VendorList', query: q })
}

onMounted(async () => {
  try {
    purchaseUsers.value = await authApi.getPurchaseUsersForSelect()
  } catch {
    purchaseUsers.value = []
  }
})
</script>

<template>
  <div class="vendor-search-panel">
    <div class="vendor-search-panel__head">{{ t('leftPanel.vendorSearchTitle') }}</div>

    <div class="vendor-search-panel__fields">
      <!-- 顺序：关键词 → 状态 → 等级 → 身份 → 类型 → 行业 → 采购员 → 创建日期区间 -->
      <div class="field-col">
        <label class="field-label">{{ canViewVendorInfo ? t('vendorList.filters.keyword') : t('leftPanel.keywordCode') }}</label>
        <div class="field-control">
          <input
            v-model="form.searchTerm"
            type="text"
            class="field-input"
            :placeholder="canViewVendorInfo ? t('vendorList.filters.keywordPlaceholderFull') : t('vendorList.filters.keywordPlaceholderCode')"
            @keyup.enter="handleSearch"
          />
        </div>
      </div>

      <div class="field-col">
        <label class="field-label">{{ t('leftPanel.type') }}</label>
        <el-select
          v-model="form.status"
          :placeholder="t('vendorList.filters.allStatus')"
          clearable
          class="field-select"
          filterable
          :teleported="false"
        >
          <el-option :label="t('vendorList.status.new')" :value="1" />
          <el-option :label="t('vendorList.status.pending')" :value="2" />
          <el-option :label="t('vendorList.status.approved')" :value="10" />
          <el-option :label="t('vendorList.status.pendingFinance')" :value="12" />
          <el-option :label="t('vendorList.status.financeFiled')" :value="20" />
          <el-option :label="t('vendorList.status.failed')" :value="-1" />
        </el-select>
      </div>

      <div class="field-col">
        <label class="field-label">{{ t('vendorList.columns.level') }}</label>
        <el-select v-model="form.level" :placeholder="t('vendorList.filters.allLevel')" clearable class="field-select" :teleported="false">
          <el-option v-for="opt in VENDOR_LEVEL_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
      </div>

      <div class="field-col">
        <label class="field-label">{{ t('vendorList.columns.identity') }}</label>
        <el-select v-model="form.credit" :placeholder="t('vendorList.filters.allIdentity')" clearable class="field-select" :teleported="false">
          <el-option
            v-for="opt in VENDOR_IDENTITY_OPTIONS"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
      </div>

      <div class="field-col">
        <label class="field-label">{{ t('vendorList.columns.status') }}</label>
        <el-select
          v-model="form.ascriptionType"
          :placeholder="t('vendorList.filters.allType')"
          clearable
          class="field-select"
          :teleported="false"
        >
          <el-option :label="t('vendorList.type.private')" :value="1" />
          <el-option :label="t('vendorList.type.pool')" :value="2" />
        </el-select>
      </div>

      <div class="field-col">
        <label class="field-label">{{ t('vendorList.columns.industry') }}</label>
        <el-select v-model="form.industry" :placeholder="t('vendorList.filters.allIndustry')" clearable class="field-select" :teleported="false">
          <el-option :label="t('vendorList.industry.Electronics')" value="Electronics" />
          <el-option :label="t('vendorList.industry.Machinery')" value="Machinery" />
          <el-option :label="t('vendorList.industry.Chemical')" value="Chemical" />
          <el-option :label="t('vendorList.industry.Textile')" value="Textile" />
          <el-option :label="t('vendorList.industry.Food')" value="Food" />
          <el-option :label="t('vendorList.industry.Construction')" value="Construction" />
          <el-option :label="t('vendorList.industry.Trading')" value="Trading" />
          <el-option :label="t('vendorList.industry.Technology')" value="Technology" />
          <el-option :label="t('vendorList.industry.Healthcare')" value="Healthcare" />
          <el-option :label="t('vendorList.industry.Other')" value="Other" />
        </el-select>
      </div>

      <div class="field-col">
        <label class="field-label">{{ t('vendorList.filters.allPurchasers') }}</label>
        <el-select
          v-model="form.purchaseUserId"
          :placeholder="t('vendorList.filters.allPurchasers')"
          clearable
          filterable
          class="field-select"
          :teleported="false"
        >
          <el-option v-for="u in purchaseUsers" :key="u.id" :label="purchaserUserLabel(u)" :value="u.id" />
        </el-select>
      </div>

      <div class="field-col">
        <label class="field-label">{{ t('leftPanel.createdDate') }}</label>
        <el-date-picker
          v-model="createdDateRange"
          type="daterange"
          :range-separator="t('rfqItemList.filters.to')"
          :start-placeholder="t('rfqItemList.filters.startDate')"
          :end-placeholder="t('rfqItemList.filters.endDate')"
          value-format="YYYY-MM-DD"
          clearable
          class="field-date-range"
          :teleported="false"
          @change="onCreatedRangeChange"
        />
      </div>

      <div class="field-col field-col--checkbox">
        <label class="field-check">
          <input v-model="form.favoriteOnly" type="checkbox" />
          <span>{{ t('leftPanel.onlyFavoriteVendors') }}</span>
        </label>
      </div>
    </div>

    <div class="vendor-search-panel__actions">
      <button type="button" class="btn-search" @click="handleSearch">{{ t('vendorList.filters.search') }}</button>
      <button type="button" class="btn-reset" @click="handleReset">{{ t('vendorList.filters.reset') }}</button>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.vendor-search-panel {
  min-height: 80px;
  font-size: 12px;
  color: $text-secondary;
}

.vendor-search-panel__head {
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 12px;
  font-size: 13px;
}

.vendor-search-panel__fields {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.field-col {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.field-label {
  font-size: 11px;
  font-weight: 500;
  color: $text-muted;
}

.field-control {
  width: 100%;
}

.field-input {
  width: 100%;
  box-sizing: border-box;
  padding: 7px 10px;
  font-size: 12px;
  color: $text-primary;
  background: $layer-3;
  border: 1px solid $border-panel;
  border-radius: 6px;
  outline: none;

  &::placeholder {
    color: $text-placeholder;
  }

  &:focus {
    border-color: var(--crm-accent-06);
  }
}

.field-select {
  width: 100%;
}

.field-date-range {
  width: 100%;

  :deep(.el-range-editor.el-input__wrapper) {
    background: $layer-3 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: 6px !important;
  }

  :deep(.el-range-input) {
    color: $text-primary !important;
    font-size: 12px !important;
  }

  :deep(.el-range-separator) {
    color: $text-muted !important;
  }
}

.field-col--checkbox {
  padding-top: 2px;
}

.field-check {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  font-size: 12px;
  color: $text-secondary;
  user-select: none;

  input {
    accent-color: $cyan-primary;
  }
}

.vendor-search-panel__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 16px;
  padding-top: 12px;
  border-top: 1px solid $border-panel;
}

.btn-search {
  flex: 1;
  min-width: 72px;
  padding: 8px 12px;
  font-size: 12px;
  font-weight: 500;
  color: #fff;
  background: linear-gradient(135deg, $blue-primary, $cyan-primary);
  border: 1px solid var(--crm-action-primary-border);
  border-radius: 6px;
  cursor: pointer;
  transition: box-shadow 0.15s, transform 0.12s;

  &:hover {
    box-shadow: var(--crm-shadow-glow);
    transform: translateY(-1px);
  }
}

.btn-reset {
  padding: 8px 12px;
  font-size: 12px;
  color: $text-secondary;
  background: $layer-3;
  border: 1px solid $border-panel;
  border-radius: 6px;
  cursor: pointer;

  &:hover {
    background: var(--crm-accent-008);
    border-color: var(--crm-accent-018);
  }
}
</style>
