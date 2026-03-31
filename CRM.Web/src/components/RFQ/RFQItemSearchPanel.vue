<script setup lang="ts">
import { reactive, ref, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { authApi, type PurchaseUserSelectOption, type SalesUserSelectOption } from '@/api/auth'

const route = useRoute()
const router = useRouter()

const salesUsers = ref<SalesUserSelectOption[]>([])
const purchaseUsers = ref<PurchaseUserSelectOption[]>([])
const dateRange = ref<[string, string] | null>(null)

const form = reactive({
  customerKeyword: '',
  materialModel: '',
  salesUserId: undefined as string | undefined,
  purchaserUserId: undefined as string | undefined,
  hasQuotesOnly: false
})

function salesUserLabel(u: SalesUserSelectOption) {
  const name = u.realName || u.label || u.userName
  return u.userName && name !== u.userName ? `${name}（${u.userName}）` : name
}

function purchaseUserLabel(u: PurchaseUserSelectOption) {
  const name = u.realName || u.label || u.userName
  return u.userName && name !== u.userName ? `${name}（${u.userName}）` : name
}

function syncFromRoute() {
  if (route.name !== 'RFQItemList') return
  const q = route.query
  const s = q.startDate
  const e = q.endDate
  if (typeof s === 'string' && typeof e === 'string' && s && e) {
    dateRange.value = [s, e]
  } else {
    dateRange.value = null
  }
  form.customerKeyword = typeof q.customerKeyword === 'string' ? q.customerKeyword : ''
  form.materialModel = typeof q.materialModel === 'string' ? q.materialModel : ''
  const sid = q.salesUserId
  form.salesUserId = typeof sid === 'string' && sid !== '' ? sid : undefined
  const pid = q.purchaserUserId
  form.purchaserUserId = typeof pid === 'string' && pid !== '' ? pid : undefined
  const hq = q.hasQuotesOnly
  const hqRaw = Array.isArray(hq) ? hq[0] : hq
  const hqStr = hqRaw != null && typeof hqRaw !== 'object' ? String(hqRaw).trim().toLowerCase() : ''
  form.hasQuotesOnly = hqStr === '1' || hqStr === 'true' || hqStr === 'yes'
}

watch(
  () => [route.name, route.query] as const,
  () => syncFromRoute(),
  { deep: true, immediate: true }
)

function handleReset() {
  router.replace({ name: 'RFQItemList', query: {} })
}

function handleSearch() {
  const query: Record<string, string> = {}
  if (dateRange.value?.[0] && dateRange.value[1]) {
    query.startDate = dateRange.value[0]
    query.endDate = dateRange.value[1]
  }
  const ck = form.customerKeyword.trim()
  if (ck) query.customerKeyword = ck
  const mm = form.materialModel.trim()
  if (mm) query.materialModel = mm
  if (form.salesUserId) query.salesUserId = form.salesUserId
  if (form.purchaserUserId) query.purchaserUserId = form.purchaserUserId
  if (form.hasQuotesOnly) query.hasQuotesOnly = '1'
  router.replace({ name: 'RFQItemList', query })
}

onMounted(async () => {
  try {
    salesUsers.value = await authApi.getSalesUsersForSelect()
  } catch {
    salesUsers.value = []
  }
  try {
    purchaseUsers.value = await authApi.getPurchaseUsersForSelect()
  } catch {
    purchaseUsers.value = []
  }
})
</script>

<template>
  <div class="rfq-item-search-panel">
    <div class="rfq-item-search-panel__head">需求明细检索</div>

    <div class="rfq-item-search-panel__fields">
      <div class="field-col">
        <label class="field-label">需求创建日期</label>
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          range-separator="至"
          start-placeholder="起"
          end-placeholder="止"
          value-format="YYYY-MM-DD"
          clearable
          class="field-date-range"
          :teleported="false"
        />
      </div>

      <div class="field-col">
        <label class="field-label">客户</label>
        <input
          v-model="form.customerKeyword"
          type="text"
          class="field-input"
          placeholder="客户名称模糊"
          @keyup.enter="handleSearch"
        />
      </div>

      <div class="field-col">
        <label class="field-label">物料型号</label>
        <input
          v-model="form.materialModel"
          type="text"
          class="field-input"
          placeholder="MPN / 客户料号"
          @keyup.enter="handleSearch"
        />
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
          <el-option v-for="u in salesUsers" :key="u.id" :label="salesUserLabel(u)" :value="u.id" />
        </el-select>
      </div>

      <div class="field-col">
        <label class="field-label">采购员</label>
        <el-select
          v-model="form.purchaserUserId"
          placeholder="全部采购员"
          clearable
          filterable
          class="field-select"
          :teleported="false"
        >
          <el-option v-for="u in purchaseUsers" :key="u.id" :label="purchaseUserLabel(u)" :value="u.id" />
        </el-select>
      </div>

      <div class="field-col field-col--checkbox">
        <el-checkbox v-model="form.hasQuotesOnly" class="field-checkbox-has-quotes" @change="handleSearch">
          有报价
        </el-checkbox>
      </div>
    </div>

    <div class="rfq-item-search-panel__actions">
      <button type="button" class="btn-search" @click="handleSearch">搜索</button>
      <button type="button" class="btn-reset" @click="handleReset">重置</button>
    </div>
  </div>
</template>

<style scoped lang="scss">
.rfq-item-search-panel {
  min-height: 80px;
  font-size: 12px;
  color: rgba(200, 220, 240, 0.9);
}

.rfq-item-search-panel__head {
  font-weight: 600;
  color: #e8f4ff;
  margin-bottom: 12px;
  font-size: 13px;
}

.rfq-item-search-panel__fields {
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
  flex-direction: row;
  align-items: center;
  padding-top: 4px;
}

.field-checkbox-has-quotes {
  :deep(.el-checkbox__label) {
    font-size: 12px;
    color: rgba(200, 220, 240, 0.9);
  }
}

.rfq-item-search-panel__actions {
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
