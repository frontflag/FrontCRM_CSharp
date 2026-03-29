<script setup lang="ts">
import { reactive, ref, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
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
  return u.userName && name !== u.userName ? `${name}（${u.userName}）` : name
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
    <div class="vendor-search-panel__head">供应商检索</div>

    <div class="vendor-search-panel__fields">
      <!-- 顺序：关键词 → 状态 → 等级 → 身份 → 类型 → 行业 → 采购员 → 创建日期区间 -->
      <div class="field-col">
        <label class="field-label">{{ canViewVendorInfo ? '关键词' : '关键词（编号）' }}</label>
        <div class="field-control">
          <input
            v-model="form.searchTerm"
            type="text"
            class="field-input"
            :placeholder="canViewVendorInfo ? '供应商名称 / 编号…' : '供应商编号…'"
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
          <el-option label="新建" :value="1" />
          <el-option label="待审核" :value="2" />
          <el-option label="已审核" :value="10" />
          <el-option label="待财务审核" :value="12" />
          <el-option label="财务建档" :value="20" />
          <el-option label="审核失败" :value="-1" />
        </el-select>
      </div>

      <div class="field-col">
        <label class="field-label">等级</label>
        <el-select v-model="form.level" placeholder="全部等级" clearable class="field-select" :teleported="false">
          <el-option v-for="opt in VENDOR_LEVEL_OPTIONS" :key="opt.value" :label="opt.label" :value="opt.value" />
        </el-select>
      </div>

      <div class="field-col">
        <label class="field-label">身份</label>
        <el-select v-model="form.credit" placeholder="全部身份" clearable class="field-select" :teleported="false">
          <el-option
            v-for="opt in VENDOR_IDENTITY_OPTIONS"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
      </div>

      <div class="field-col">
        <label class="field-label">类型</label>
        <el-select
          v-model="form.ascriptionType"
          placeholder="全部类型"
          clearable
          class="field-select"
          :teleported="false"
        >
          <el-option label="专属" :value="1" />
          <el-option label="公海" :value="2" />
        </el-select>
      </div>

      <div class="field-col">
        <label class="field-label">行业</label>
        <el-select v-model="form.industry" placeholder="全部行业" clearable class="field-select" :teleported="false">
          <el-option label="电子/半导体" value="Electronics" />
          <el-option label="机械/设备" value="Machinery" />
          <el-option label="化工/材料" value="Chemical" />
          <el-option label="纺织/服装" value="Textile" />
          <el-option label="食品/农业" value="Food" />
          <el-option label="建筑/工程" value="Construction" />
          <el-option label="贸易/零售" value="Trading" />
          <el-option label="科技/IT" value="Technology" />
          <el-option label="医疗/健康" value="Healthcare" />
          <el-option label="其他" value="Other" />
        </el-select>
      </div>

      <div class="field-col">
        <label class="field-label">采购员</label>
        <el-select
          v-model="form.purchaseUserId"
          placeholder="全部采购员"
          clearable
          filterable
          class="field-select"
          :teleported="false"
        >
          <el-option v-for="u in purchaseUsers" :key="u.id" :label="purchaserUserLabel(u)" :value="u.id" />
        </el-select>
      </div>

      <div class="field-col">
        <label class="field-label">创建日期</label>
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
          <span>仅显示收藏供应商</span>
        </label>
      </div>
    </div>

    <div class="vendor-search-panel__actions">
      <button type="button" class="btn-search" @click="handleSearch">搜索</button>
      <button type="button" class="btn-reset" @click="handleReset">重置</button>
    </div>
  </div>
</template>

<style scoped lang="scss">
.vendor-search-panel {
  min-height: 80px;
  font-size: 12px;
  color: rgba(200, 220, 240, 0.9);
}

.vendor-search-panel__head {
  font-weight: 600;
  color: #e8f4ff;
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

  :deep(.el-range-editor.el-input__wrapper) {
    background: rgba(10, 22, 40, 0.85) !important;
    box-shadow: none !important;
    border: 1px solid rgba(0, 212, 255, 0.18) !important;
    border-radius: 6px !important;
  }

  :deep(.el-range-input) {
    color: rgba(224, 244, 255, 0.92) !important;
    font-size: 12px !important;
  }

  :deep(.el-range-separator) {
    color: rgba(140, 170, 200, 0.55) !important;
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
  color: rgba(200, 220, 240, 0.88);
  user-select: none;

  input {
    accent-color: #00d4ff;
  }
}

.vendor-search-panel__actions {
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
