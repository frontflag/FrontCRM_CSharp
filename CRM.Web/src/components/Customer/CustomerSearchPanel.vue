<script setup lang="ts">
import { reactive, ref, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
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
  return u.userName && name !== u.userName ? `${name}（${u.userName}）` : name
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
    <div class="customer-search-panel__head">客户检索</div>

    <div class="customer-search-panel__fields">
      <div class="field-col">
        <label class="field-label">{{ canViewCustomerInfo ? '关键词' : '关键词（编号）' }}</label>
        <div class="field-control">
          <input
            v-model="form.searchTerm"
            type="text"
            class="field-input"
            :placeholder="canViewCustomerInfo ? '客户名称 / 联系人…' : '客户编号…'"
            @keyup.enter="handleSearch"
          />
        </div>
      </div>

      <div class="field-col">
        <label class="field-label">状态</label>
        <el-select
          v-model="form.status"
          placeholder="全部状态"
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
        <label class="field-label">级别</label>
        <el-select
          v-model="form.customerLevel"
          placeholder="全部级别"
          clearable
          class="field-select"
          filterable
          :teleported="false"
        >
          <el-option label="D级" value="D" />
          <el-option label="C级" value="C" />
          <el-option label="B级" value="B" />
          <el-option label="BPO" value="BPO" />
          <el-option label="VIP" value="VIP" />
          <el-option label="VPO" value="VPO" />
        </el-select>
      </div>

      <div class="field-col">
        <label class="field-label">客户类型</label>
        <el-select
          v-model="form.customerType"
          placeholder="全部类型"
          clearable
          class="field-select"
          filterable
          :teleported="false"
        >
          <el-option label="OEM" :value="1" />
          <el-option label="ODM" :value="2" />
          <el-option label="终端用户" :value="3" />
          <el-option label="贸易商" :value="5" />
          <el-option label="代理商" :value="6" />
        </el-select>
      </div>

      <div class="field-col">
        <label class="field-label">行业</label>
        <el-select
          v-model="form.industry"
          placeholder="全部行业"
          clearable
          class="field-select"
          filterable
          :teleported="false"
        >
          <el-option label="制造业" value="Manufacturing" />
          <el-option label="科技/IT" value="Technology" />
          <el-option label="贸易/零售" value="Trading" />
          <el-option label="建筑/工程" value="Construction" />
          <el-option label="其他" value="Other" />
        </el-select>
      </div>

      <div class="field-col">
        <label class="field-label">业务员</label>
        <el-select
          v-model="form.salesUserId"
          placeholder="全部业务员"
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
        <label class="field-label">创建日期区间</label>
        <el-date-picker
          v-model="createdDateRange"
          type="daterange"
          range-separator="至"
          start-placeholder="起"
          end-placeholder="止"
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
          <span>仅显示收藏客户</span>
        </label>
      </div>
    </div>

    <div class="customer-search-panel__actions">
      <button type="button" class="btn-search" @click="handleSearch">搜索</button>
      <button type="button" class="btn-reset" @click="handleReset">重置</button>
    </div>
  </div>
</template>

<style scoped lang="scss">
.customer-search-panel {
  min-height: 80px;
  font-size: 12px;
  color: rgba(200, 220, 240, 0.9);
}

.customer-search-panel__head {
  font-weight: 600;
  color: #e8f4ff;
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
  color: rgba(120, 190, 232, 0.7);
}

.field-control {
  width: 100%;
}

.field-input {
  width: 100%;
  box-sizing: border-box;
  padding: 7px 10px;
  font-size: 12px;
  color: rgba(224, 244, 255, 0.92);
  background: rgba(10, 22, 40, 0.85);
  border: 1px solid rgba(0, 212, 255, 0.18);
  border-radius: 6px;
  outline: none;

  &::placeholder {
    color: rgba(140, 170, 200, 0.45);
  }

  &:focus {
    border-color: rgba(0, 212, 255, 0.45);
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
  color: rgba(200, 220, 240, 0.88);
  user-select: none;

  input {
    accent-color: #00d4ff;
  }
}

.customer-search-panel__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 16px;
  padding-top: 12px;
  border-top: 1px solid rgba(0, 212, 255, 0.1);
}

.btn-search {
  flex: 1;
  min-width: 72px;
  padding: 8px 12px;
  font-size: 12px;
  font-weight: 500;
  color: #fff;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.75), rgba(0, 212, 255, 0.65));
  border: 1px solid rgba(0, 212, 255, 0.35);
  border-radius: 6px;
  cursor: pointer;
  transition: box-shadow 0.15s, transform 0.12s;

  &:hover {
    box-shadow: 0 2px 12px rgba(0, 212, 255, 0.2);
    transform: translateY(-1px);
  }
}

.btn-reset {
  padding: 8px 12px;
  font-size: 12px;
  color: rgba(180, 210, 230, 0.85);
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(0, 212, 255, 0.12);
  border-radius: 6px;
  cursor: pointer;

  &:hover {
    background: rgba(0, 212, 255, 0.08);
    border-color: rgba(0, 212, 255, 0.22);
  }
}
</style>
