<script setup lang="ts">
import { reactive, ref, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { authApi } from '@/api/auth'
import {
  buildCustomerListQuery,
  parseCustomerListQuery,
  type CustomerListFilterQuery
} from '@/utils/customerListQuery'
import { CUSTOMER_WORKFLOW_STATUS_OPTIONS } from '@/constants/customerWorkflowStatus'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const { t } = useI18n()
const canViewCustomerInfo = authStore.hasPermission('customer.info.read')

const workflowStatusOptions = CUSTOMER_WORKFLOW_STATUS_OPTIONS

type SalesUserOption = { id: string; userName: string; realName?: string; label?: string }

const salesUsers = ref<SalesUserOption[]>([])
const createdDateRange = ref<[string, string] | null>(null)

const form = reactive<CustomerListFilterQuery>({
  searchTerm: '',
  customerType: undefined,
  customerLevel: undefined,
  industry: undefined,
  status: undefined,
  salesUserId: undefined,
  createdFrom: undefined,
  createdTo: undefined,
  favoriteOnly: false
})

function salesUserLabel(u: SalesUserOption) {
  const name = u.realName || u.label || u.userName
  return u.userName && name !== u.userName ? `${name}(${u.userName})` : name
}

function syncFormFromRoute() {
  if (route.name !== 'CustomerList') return
  const p = parseCustomerListQuery(route.query)
  form.searchTerm = p.searchTerm
  form.customerType = p.customerType
  form.customerLevel = p.customerLevel
  form.industry = p.industry
  form.status = p.status
  form.salesUserId = p.salesUserId
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
  form.customerType = undefined
  form.customerLevel = undefined
  form.industry = undefined
  form.status = undefined
  form.salesUserId = undefined
  form.createdFrom = undefined
  form.createdTo = undefined
  form.favoriteOnly = false
  createdDateRange.value = null
  router.push({ name: 'CustomerList', query: {} })
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
  const q = buildCustomerListQuery(form)
  router.push({ name: 'CustomerList', query: q })
}

onMounted(async () => {
  try {
    salesUsers.value = await authApi.getSalesUsersForSelect()
  } catch {
    salesUsers.value = []
  }
})
</script>

<template>
  <div class="customer-search-panel">
    <div class="customer-search-panel__head">{{ t('leftPanel.customerSearchTitle') }}</div>

    <div class="customer-search-panel__fields">
      <div class="field-col">
        <label class="field-label">{{ canViewCustomerInfo ? t('customerList.filters.keyword') : t('leftPanel.keywordCode') }}</label>
        <div class="field-control">
          <input
            v-model="form.searchTerm"
            type="text"
            class="field-input"
            :placeholder="canViewCustomerInfo ? t('customerList.filters.keywordPlaceholderFull') : t('customerList.filters.keywordPlaceholderCode')"
            @keyup.enter="handleSearch"
          />
        </div>
      </div>

      <div class="field-col">
        <label class="field-label">{{ t('customerList.columns.status') }}</label>
        <el-select
          v-model="form.status"
          :placeholder="t('customerList.filters.allStatus')"
          clearable
          class="field-select"
          filterable
          :teleported="false"
        >
          <el-option
            v-for="opt in workflowStatusOptions"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
      </div>

      <div class="field-col">
        <label class="field-label">{{ t('customerList.columns.level') }}</label>
        <el-select
          v-model="form.customerLevel"
          :placeholder="t('customerList.filters.allLevel')"
          clearable
          class="field-select"
          filterable
          :teleported="false"
        >
          <el-option :label="t('customerList.level.D')" value="D" />
          <el-option :label="t('customerList.level.C')" value="C" />
          <el-option :label="t('customerList.level.B')" value="B" />
          <el-option label="BPO" value="BPO" />
          <el-option label="VIP" value="VIP" />
          <el-option label="VPO" value="VPO" />
        </el-select>
      </div>

      <div class="field-col">
        <label class="field-label">{{ t('customerList.columns.type') }}</label>
        <el-select
          v-model="form.customerType"
          :placeholder="t('customerList.filters.allType')"
          clearable
          class="field-select"
          filterable
          :teleported="false"
        >
          <el-option label="OEM" :value="1" />
          <el-option label="ODM" :value="2" />
          <el-option :label="t('customerList.type.endUser')" :value="3" />
          <el-option :label="t('customerList.type.trader')" :value="5" />
          <el-option :label="t('customerList.type.agency')" :value="6" />
        </el-select>
      </div>

      <div class="field-col">
        <label class="field-label">{{ t('customerList.columns.industry') }}</label>
        <el-select
          v-model="form.industry"
          :placeholder="t('customerList.filters.allIndustry')"
          clearable
          class="field-select"
          filterable
          :teleported="false"
        >
          <el-option :label="t('customerList.industry.Manufacturing')" value="Manufacturing" />
          <el-option :label="t('customerList.industry.Technology')" value="Technology" />
          <el-option :label="t('customerList.industry.Trading')" value="Trading" />
          <el-option :label="t('customerList.industry.Construction')" value="Construction" />
          <el-option :label="t('customerList.industry.Other')" value="Other" />
        </el-select>
      </div>

      <div class="field-col">
        <label class="field-label">{{ t('customerList.columns.createUser') }}</label>
        <el-select
          v-model="form.salesUserId"
          :placeholder="t('customerList.filters.allSalesUsers')"
          clearable
          filterable
          class="field-select"
          :teleported="false"
        >
          <el-option
            v-for="u in salesUsers"
            :key="u.id"
            :label="salesUserLabel(u)"
            :value="u.id"
          />
        </el-select>
      </div>

      <div class="field-col">
        <label class="field-label">{{ t('leftPanel.createdDateRange') }}</label>
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
          <span>{{ t('leftPanel.onlyFavoriteCustomers') }}</span>
        </label>
      </div>
    </div>

    <div class="customer-search-panel__actions">
      <button type="button" class="btn-search" @click="handleSearch">{{ t('customerList.filters.search') }}</button>
      <button type="button" class="btn-reset" @click="handleReset">{{ t('customerList.filters.reset') }}</button>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.customer-search-panel {
  min-height: 80px;
  font-size: 12px;
  color: $text-secondary;
}

.customer-search-panel__head {
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 12px;
  font-size: 13px;
}

.customer-search-panel__fields {
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

.customer-search-panel__actions {
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
