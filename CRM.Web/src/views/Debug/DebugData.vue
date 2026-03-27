<template>
  <div class="debug-page">
    <div class="debug-header">
      <h1>Debug 模拟数据</h1>
      <div class="debug-sub muted">
        业务链路模拟写入数据库，需登录后使用。
        <router-link class="debug-link" to="/debug">返回 Debug</router-link>
      </div>
    </div>

    <section class="debug-panel panel-simulate">
      <h2 class="panel-title">业务链路模拟数据</h2>
      <div class="panel-body simulate-form simulate-form--row1">
        <div class="simulate-form__group">
          <span class="simulate-form__inline-label">数据来源：</span>
          <el-select v-model="simulateForm.dataOrigin" placeholder="数据起源" style="width: 140px">
            <el-option v-for="opt in dataOriginOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
          <el-input
            v-if="simulateForm.dataOrigin !== 'ignore'"
            v-model="simulateForm.originReferenceCode"
            :placeholder="originCodePlaceholder"
            clearable
            style="width: 220px"
          />
        </div>
        <div class="simulate-form__group simulate-form__group--generate">
          <span class="simulate-form__inline-label">生成：</span>
          <el-select v-model="simulateForm.businessNode" placeholder="选择业务节点" style="width: 220px">
            <el-option v-for="opt in businessNodeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
          <el-select v-model="simulateForm.status" placeholder="选择状态" style="width: 260px">
            <el-option
              v-for="opt in currentStatusOptions"
              :key="`${simulateForm.businessNode}-${opt.value}`"
              :label="`${opt.value} - ${opt.label}`"
              :value="opt.value"
            />
          </el-select>
        </div>
        <el-button type="primary" :loading="simulating" @click="onSimulate">生成链路数据</el-button>
      </div>
      <div class="simulate-tip">
        按业务节点选择状态枚举值，系统将自动补齐当前节点前序链路。「数据起源」选「忽略」时与旧版一致；选客户/供应商/订单时需填写对应编号，并从该实体衔接后续模拟数据。
      </div>
      <div v-if="simulateResult" class="simulate-result">
        <div>链路号：<span class="mono">{{ simulateResult.chainNo }}</span></div>
        <div>节点：{{ simulateResult.businessNode }}，状态：{{ simulateResult.targetStatus }}</div>
        <div>创建结果：{{ simulateResult.createdNodes.join(' -> ') }}</div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import {
  simulateBusinessChain,
  type SimulateBusinessChainResponse,
  type SimulateDataOrigin
} from '@/api/debug'
import { getApiErrorMessage } from '@/utils/apiError'

const simulating = ref(false)
const simulateResult = ref<SimulateBusinessChainResponse | null>(null)
const dataOriginOptions: { label: string; value: SimulateDataOrigin }[] = [
  { label: '忽略', value: 'ignore' },
  { label: '客户', value: 'customer' },
  { label: '供应商', value: 'vendor' },
  { label: '销售订单', value: 'salesorder' },
  { label: '采购订单', value: 'purchaseorder' }
]

const simulateForm = ref({
  businessNode: 'stockin',
  status: 2,
  dataOrigin: 'ignore' as SimulateDataOrigin,
  originReferenceCode: ''
})

const originCodePlaceholder = computed(() => {
  switch (simulateForm.value.dataOrigin) {
    case 'customer':
      return '客户编号'
    case 'vendor':
      return '供应商编码'
    case 'salesorder':
      return '销售订单编号'
    case 'purchaseorder':
      return '采购订单编号'
    default:
      return ''
  }
})

const businessNodeOptions = [
  { label: 'RFQ', value: 'rfq' },
  { label: 'Quote', value: 'quote' },
  { label: 'SalesOrder', value: 'salesorder' },
  { label: 'PurchaseRequisition', value: 'purchaserequisition' },
  { label: 'PurchaseOrder', value: 'purchaseorder' },
  { label: 'StockInNotify', value: 'stockinnotify' },
  { label: 'QC', value: 'qc' },
  { label: 'StockIn', value: 'stockin' },
  { label: 'StockOutRequest', value: 'stockoutrequest' }
]

type StatusOption = { value: number; label: string }

const statusOptionsByNode: Record<string, StatusOption[]> = {
  rfq: [
    { value: 0, label: '待分配' },
    { value: 1, label: '已分配' },
    { value: 2, label: '报价中' },
    { value: 3, label: '已报价' },
    { value: 4, label: '已选价' },
    { value: 5, label: '已转订单' },
    { value: 6, label: '已关闭' }
  ],
  quote: [
    { value: 0, label: '草稿' },
    { value: 1, label: '待审核' },
    { value: 2, label: '已审核' },
    { value: 3, label: '已发送' },
    { value: 4, label: '已接受' },
    { value: 5, label: '已拒绝' },
    { value: 6, label: '已过期' },
    { value: 7, label: '已关闭' }
  ],
  salesorder: [
    { value: 1, label: '新建' },
    { value: 2, label: '待审核' },
    { value: 10, label: '审核通过' },
    { value: 20, label: '进行中' },
    { value: 100, label: '完成' },
    { value: -1, label: '审核失败' },
    { value: -2, label: '取消' }
  ],
  purchaserequisition: [
    { value: 0, label: '新建' },
    { value: 1, label: '部分完成' },
    { value: 2, label: '全部完成' },
    { value: 3, label: '已取消' }
  ],
  purchaseorder: [
    { value: 1, label: '新建' },
    { value: 2, label: '待审核' },
    { value: 10, label: '审核通过' },
    { value: 20, label: '待确认' },
    { value: 30, label: '已确认' },
    { value: 50, label: '进行中' },
    { value: 100, label: '采购完成' },
    { value: -1, label: '审核失败' },
    { value: -2, label: '取消' }
  ],
  stockinnotify: [
    { value: 1, label: '新建' },
    { value: 10, label: '未到货' },
    { value: 20, label: '到货待检' },
    { value: 30, label: '已质检' },
    { value: 100, label: '已入库' }
  ],
  qc: [
    { value: -1, label: '未通过' },
    { value: 10, label: '部分通过' },
    { value: 100, label: '已通过' }
  ],
  stockin: [
    { value: 0, label: '草稿' },
    { value: 1, label: '待入库' },
    { value: 2, label: '已入库' },
    { value: 3, label: '已取消' }
  ],
  stockoutrequest: [
    { value: 0, label: '待出库' },
    { value: 1, label: '已出库' },
    { value: 2, label: '已取消' }
  ]
}

const currentStatusOptions = computed<StatusOption[]>(
  () => statusOptionsByNode[simulateForm.value.businessNode] ?? [{ value: 0, label: '默认' }]
)

watch(
  () => simulateForm.value.dataOrigin,
  (origin) => {
    if (origin === 'ignore') simulateForm.value.originReferenceCode = ''
  }
)

watch(
  () => simulateForm.value.businessNode,
  (node) => {
    const first = (statusOptionsByNode[node] ?? [])[0]
    if (!first) return
    const exists = (statusOptionsByNode[node] ?? []).some(x => x.value === simulateForm.value.status)
    if (!exists) simulateForm.value.status = first.value
  },
  { immediate: true }
)

const onSimulate = async () => {
  const origin = simulateForm.value.dataOrigin
  if (origin !== 'ignore') {
    const code = simulateForm.value.originReferenceCode.trim()
    if (!code) {
      ElMessage.warning(`请填写${originCodePlaceholder.value || '业务编号'}`)
      return
    }
  }
  simulating.value = true
  try {
    const payload: Parameters<typeof simulateBusinessChain>[0] = {
      businessNode: simulateForm.value.businessNode,
      status: Number(simulateForm.value.status ?? 0)
    }
    if (simulateForm.value.dataOrigin !== 'ignore') {
      payload.dataOrigin = simulateForm.value.dataOrigin
      payload.originReferenceCode = simulateForm.value.originReferenceCode.trim()
    }
    simulateResult.value = await simulateBusinessChain(payload)
    ElMessage.success('模拟数据生成成功')
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '模拟数据生成失败'))
  } finally {
    simulating.value = false
  }
}
</script>

<style lang="scss" scoped>
.debug-page {
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.debug-header h1 {
  margin: 0;
  font-size: 20px;
  font-weight: 700;
  color: #e8f4ff;
}

.debug-sub {
  margin-top: 6px;
  font-size: 12px;
  color: rgba(200, 220, 240, 0.7);
  line-height: 1.5;

  &.muted {
    margin-top: 4px;
    opacity: 0.85;
  }
}

.debug-link {
  margin-left: 10px;
  color: #00d4ff;
  text-decoration: none;
  font-weight: 600;
  white-space: nowrap;

  &:hover {
    text-decoration: underline;
  }
}

.debug-panel {
  padding: 16px 18px;
  border-radius: 10px;
  border: 1px solid rgba(0, 212, 255, 0.2);
  background: rgba(0, 212, 255, 0.06);
}

.panel-title {
  margin: 0 0 12px;
  font-size: 15px;
  font-weight: 600;
  color: #e8f4ff;
}

.panel-body {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px;
}

.mono {
  font-family: ui-monospace, 'Cascadia Code', 'Consolas', monospace;
}

.simulate-form {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px 16px;
}

.simulate-form__group {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
}

.simulate-form__group--generate {
  margin-left: 8px;
  padding-left: 16px;
  border-left: 1px solid rgba(0, 212, 255, 0.2);
}

.simulate-form__inline-label {
  font-size: 13px;
  font-weight: 600;
  color: rgba(200, 216, 232, 0.85);
  white-space: nowrap;
}

.simulate-tip {
  margin-top: 8px;
  color: rgba(200, 220, 240, 0.7);
  font-size: 12px;
}

.simulate-result {
  margin-top: 10px;
  border: 1px solid rgba(0, 212, 255, 0.2);
  background: rgba(0, 212, 255, 0.05);
  border-radius: 8px;
  padding: 10px;
  color: #e8f4ff;
  font-size: 13px;
  display: grid;
  gap: 4px;
}
</style>
