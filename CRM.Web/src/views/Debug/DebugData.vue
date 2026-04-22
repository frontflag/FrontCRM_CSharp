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

    <section class="debug-panel panel-chain">
      <h2 class="panel-title">删除数据链（按需求单号）</h2>
      <p class="chain-tip">
        输入 <strong>RFQ 需求单号</strong>（<span class="mono">rfq.rfq_code</span>），查询从该需求产生的下游业务节点与编号；删除将移除这些关联数据（含库存/出库/拣货等与造数链路一致的表）。操作不可恢复，仅限调试环境使用。
      </p>
      <div class="panel-body chain-toolbar">
        <span class="simulate-form__inline-label">需求单号：</span>
        <el-input
          v-model="rfqChainCode"
          clearable
          placeholder="例如 RFQ2504180001"
          style="width: 280px"
          @keyup.enter="onPreviewRfqChain"
        />
        <el-button type="primary" plain :loading="chainLoading" @click="onPreviewRfqChain">查询下游链</el-button>
        <el-button type="danger" :loading="chainDeleting" :disabled="!chainPreview?.nodes?.length" @click="onDeleteRfqChain">
          删除下游数据
        </el-button>
      </div>
      <div v-if="chainError" class="chain-error">{{ chainError }}</div>
      <el-table
        v-if="chainPreview && chainPreview.nodes.length"
        :data="chainPreview.nodes"
        border
        stripe
        size="small"
        class="chain-table"
        max-height="420"
      >
        <el-table-column prop="node" label="业务节点" width="180" />
        <el-table-column prop="code" label="数据编号（业务号）" min-width="200" show-overflow-tooltip>
          <template #default="{ row }">
            <span class="mono">{{ row.code }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="id" label="主键 Id" min-width="280" show-overflow-tooltip>
          <template #default="{ row }">
            <span class="mono">{{ row.id }}</span>
          </template>
        </el-table-column>
      </el-table>
      <div v-else-if="chainSearched && !chainLoading && !chainError" class="chain-empty">未查询到数据或无下游记录</div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  simulateBusinessChain,
  getRfqChainPreview,
  deleteRfqChain,
  type SimulateBusinessChainResponse,
  type SimulateDataOrigin,
  type RfqChainPreview
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

const rfqChainCode = ref('')
const chainLoading = ref(false)
const chainDeleting = ref(false)
const chainPreview = ref<RfqChainPreview | null>(null)
const chainError = ref<string | null>(null)
const chainSearched = ref(false)

const onPreviewRfqChain = async () => {
  const code = rfqChainCode.value.trim()
  if (!code) {
    ElMessage.warning('请输入需求单号')
    return
  }
  chainLoading.value = true
  chainError.value = null
  chainSearched.value = true
  try {
    chainPreview.value = await getRfqChainPreview(code)
    if (!chainPreview.value.nodes.length) {
      ElMessage.info('未找到下游数据（需求可能不存在或无关联单据）')
    }
  } catch (e) {
    chainPreview.value = null
    chainError.value = getApiErrorMessage(e, '查询失败')
    ElMessage.error(chainError.value)
  } finally {
    chainLoading.value = false
  }
}

const onDeleteRfqChain = async () => {
  const code = rfqChainCode.value.trim()
  if (!code) {
    ElMessage.warning('请输入需求单号')
    return
  }
  if (!chainPreview.value?.nodes?.length) {
    ElMessage.warning('请先查询下游链')
    return
  }
  try {
    await ElMessageBox.confirm(
      `将永久删除需求「${code}」及其下游全部关联数据（见上表），不可恢复。是否继续？`,
      '确认删除',
      { type: 'warning', confirmButtonText: '删除', cancelButtonText: '取消' }
    )
  } catch {
    return
  }
  chainDeleting.value = true
  chainError.value = null
  try {
    await deleteRfqChain(code)
    ElMessage.success('已删除')
    chainPreview.value = null
    chainSearched.value = false
    rfqChainCode.value = ''
  } catch (e) {
    chainError.value = getApiErrorMessage(e, '删除失败')
    ElMessage.error(chainError.value)
  } finally {
    chainDeleting.value = false
  }
}

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
/* 本页在 AppLayout 浅色主内容区内渲染，使用深色文字与 Element 变量以保证对比度 */
.debug-page {
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 20px;
  color: #303133;
}

.debug-header h1 {
  margin: 0;
  font-size: 20px;
  font-weight: 700;
  color: #303133;
}

.debug-sub {
  margin-top: 6px;
  font-size: 13px;
  color: #606266;
  line-height: 1.6;

  &.muted {
    margin-top: 4px;
  }
}

.debug-link {
  margin-left: 10px;
  color: var(--el-color-primary);
  text-decoration: none;
  font-weight: 600;
  white-space: nowrap;

  &:hover {
    text-decoration: underline;
    color: var(--el-color-primary-light-3);
  }
}

.debug-panel {
  padding: 16px 18px;
  border-radius: 10px;
  border: 1px solid var(--el-border-color-lighter);
  background: var(--el-bg-color);
  box-shadow: var(--el-box-shadow-light);
}

.panel-title {
  margin: 0 0 12px;
  font-size: 15px;
  font-weight: 600;
  color: #303133;
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
  border-left: 1px solid var(--el-border-color-lighter);
}

.simulate-form__inline-label {
  font-size: 13px;
  font-weight: 600;
  color: #606266;
  white-space: nowrap;
}

.simulate-tip {
  margin-top: 8px;
  color: #909399;
  font-size: 12px;
  line-height: 1.55;
}

.simulate-result {
  margin-top: 10px;
  border: 1px solid var(--el-border-color-lighter);
  background: var(--el-fill-color-light);
  border-radius: 8px;
  padding: 10px 12px;
  color: #303133;
  font-size: 13px;
  display: grid;
  gap: 4px;
}

.panel-chain .chain-tip {
  margin: 0 0 14px;
  font-size: 12px;
  color: #909399;
  line-height: 1.55;
}

.chain-toolbar {
  flex-wrap: wrap;
  gap: 10px 12px;
}

.chain-error {
  margin-top: 10px;
  padding: 10px 12px;
  border-radius: 8px;
  border: 1px solid var(--el-color-danger-light-5);
  background: var(--el-color-danger-light-9);
  color: var(--el-color-danger);
  font-size: 13px;
}

.chain-empty {
  margin-top: 12px;
  padding: 12px;
  font-size: 13px;
  color: #909399;
  border: 1px dashed var(--el-border-color);
  border-radius: 8px;
  text-align: center;
}

.chain-table {
  margin-top: 12px;
  width: 100%;
}
</style>
