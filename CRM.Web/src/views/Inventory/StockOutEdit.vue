<template>
  <div class="stockout-edit-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3zM17 14l4 4-4 4M10 17h11" />
            </svg>
          </div>
          <h1 class="page-title">执行出库</h1>
        </div>
      </div>
      <div class="header-right">
        <button class="btn-secondary" @click="goBack">返回出库通知</button>
        <button
          class="btn-picking"
          :disabled="requestAlreadyShipped || hasActivePickingTask"
          :title="generatePickingBtnTitle"
          @click="handleGeneratePicking"
        >
          生成拣货任务
        </button>
        <button
          class="btn-primary"
          style="margin-left: 8px"
          :disabled="submitting || !canExecuteStockOut"
          :title="executeOutHint"
          @click="handleSubmit"
        >
          {{ submitting ? '执行中...' : '执行出库' }}
        </button>
      </div>
    </div>

    <el-alert
      v-if="form.stockOutRequestId"
      class="flow-alert"
      type="info"
      :closable="false"
      show-icon
    >
      <template #title>
        <span class="flow-alert-title">出库流程</span>
      </template>
      <ol class="flow-steps">
        <li :class="{ 'flow-step--done': hasItemsAndWarehouse }">确认仓库与出库明细（可点「从出库通知刷新明细」）</li>
        <li :class="{ 'flow-step--done': pickingTasks.length > 0 }">生成拣货任务（仅建任务壳）</li>
        <li :class="{ 'flow-step--done': pickingLinesSaved }">加载候选并保存拣货明细（按 stockitem），再点「完成拣货」</li>
        <li :class="{ 'flow-step--done': pickingCompleted }">拣货任务状态为已完成</li>
        <li :class="{ 'flow-step--done': requestAlreadyShipped }">执行出库（按拣货明细扣减并关闭出库通知）</li>
      </ol>
      <p v-if="requestAlreadyShipped" class="flow-done-msg">该出库通知已执行出库，请返回列表查看。</p>
      <p v-else-if="!pickingCompleted && pickingTasks.length > 0" class="flow-warn-msg">请先完成拣货任务，再执行出库。</p>
    </el-alert>

    <div class="form-layout">
      <div class="form-card">
        <h3 class="section-title">基础信息</h3>
        <el-form class="basic-info-form" :model="form" label-width="6em">
          <el-form-item label="出库执行单号" required>
            <el-input v-model="form.stockOutCode" placeholder="如：SOUT202603180001" />
          </el-form-item>
          <el-form-item label="仓库名称" required>
            <el-select v-model="form.warehouseId" placeholder="请选择仓库" style="width: 100%">
              <el-option
                v-for="w in warehouses"
                :key="w.id"
                :label="`${w.warehouseName}（${w.warehouseCode}）`"
                :value="w.id"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="出库通知单号">
            <el-input :model-value="notifyRequestCodeDisplay" readonly />
            <div v-if="form.stockOutRequestId" class="form-sub-hint">内部 ID：{{ form.stockOutRequestId }}</div>
          </el-form-item>
          <el-form-item label="操作人">
            <el-input v-model="form.operatorId" placeholder="当前操作人ID（可选）" />
          </el-form-item>
          <el-form-item label="出库日期" required>
            <el-date-picker
              v-model="form.stockOutDate"
              type="datetime"
              format="YYYY-MM-DD HH:mm"
              value-format="YYYY-MM-DDTHH:mm:ss"
              style="width: 100%"
            />
          </el-form-item>
          <el-form-item label="备注">
            <el-input v-model="form.remark" type="textarea" :rows="2" placeholder="备注信息" />
          </el-form-item>
        </el-form>
      </div>

      <div class="form-card">
        <div class="section-header">
          <h3 class="section-title">出库明细</h3>
          <button class="btn-secondary btn-sm" @click="loadItemsFromRequest">从出库通知刷新明细</button>
        </div>
        <el-table :data="form.items" class="quantum-table">
          <el-table-column type="index" width="50" />
          <el-table-column label="物料编码" min-width="140">
            <template #default="{ row }">
              <el-input v-model="row.materialCode" placeholder="物料编码" readonly />
            </template>
          </el-table-column>
          <el-table-column label="物料名称" min-width="160">
            <template #default="{ row }">
              <el-input v-model="row.materialName" placeholder="物料名称" readonly />
            </template>
          </el-table-column>
          <el-table-column label="出库数量" width="110" align="right">
            <template #default="{ row }">
              <span class="qty-cell">{{ formatQty(row.quantity) }}</span>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="80" fixed="right" class-name="op-col" label-class-name="op-col">
            <template #default>
              <button type="button" class="action-btn" disabled title="明细来自出库通知，不可手工删除">删除</button>
            </template>
          </el-table-column>
        </el-table>
        <div class="table-footer">
          <div class="total">
            合计出库数量：<span>{{ totalQuantity }}</span>
          </div>
        </div>
      </div>
    </div>

    <div class="form-card" v-if="form.stockOutRequestId">
      <h3 class="section-title">拣货任务</h3>
      <p class="picking-hint">
        每个出库通知仅允许生成<strong>一个</strong>未取消的拣货任务。候选在库明细 = 与本销售行绑定的 stockitem + 符合规则的备货（型号/品牌匹配）；FIFO 仅用于排序与「自动分配」顺序。请在下方「拣货明细」卡片中加载候选、分配数量并保存后，再点「完成拣货」。执行出库时仅按已保存的拣货行扣减。
      </p>
      <p v-if="!pickingTasks.length" class="picking-empty">暂无拣货任务，请先确认明细与仓库后点击「生成拣货任务」。</p>
      <el-table v-else :data="pickingTasks" row-key="id" class="picking-task-table">
        <el-table-column type="expand" width="44">
          <template #default="{ row }">
            <div class="picking-expand-inner">
              <div class="picking-expand-title">拣货明细（备货行已高亮）</div>
              <el-table
                :data="pickingTaskLines(row)"
                size="small"
                border
                class="picking-lines-table"
                :row-class-name="pickingLineRowClassName"
              >
                <el-table-column label="在库明细" min-width="120" show-overflow-tooltip>
                  <template #default="{ row: line }">
                    <span :title="pickingLineStockItemId(line)">{{ shortId(pickingLineStockItemId(line)) }}</span>
                  </template>
                </el-table-column>
                <el-table-column :label="t('inventoryList.columns.stockType')" width="108" align="center" show-overflow-tooltip>
                  <template #default="{ row: line }">{{ pickingLineStockTypeLabel(line) }}</template>
                </el-table-column>
                <el-table-column prop="materialId" label="物料ID" min-width="140" show-overflow-tooltip />
                <el-table-column label="计划数量" width="110" align="right">
                  <template #default="{ row: line }">{{ formatQty(Number(line.planQty)) }}</template>
                </el-table-column>
                <el-table-column label="已拣" width="100" align="right">
                  <template #default="{ row: line }">{{ formatQty(Number(line.pickedQty)) }}</template>
                </el-table-column>
                <el-table-column label="来源" width="120" align="center">
                  <template #default="{ row: line }">
                    <span v-if="isPickingLineStockingSupplement(line)" class="picking-source-stocking">
                      <el-icon class="picking-stock-icon" aria-hidden="true"><Box /></el-icon>
                      <span>备货</span>
                    </span>
                    <span v-else class="picking-source-normal">关联类型</span>
                  </template>
                </el-table-column>
              </el-table>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="taskCode" label="任务号" width="160" />
        <el-table-column :label="t('inventoryList.columns.stockType')" min-width="120" align="center" show-overflow-tooltip>
          <template #default="{ row }">{{ pickingTaskStockTypesDisplay(row) }}</template>
        </el-table-column>
        <el-table-column label="仓库" min-width="160">
          <template #default="{ row }">{{ warehouseLabel(row.warehouseId) }}</template>
        </el-table-column>
        <el-table-column label="计划拣货" min-width="140" align="right" show-overflow-tooltip>
          <template #default="{ row }">{{ formatQty(pickingQty(row, 'plan')) }}</template>
        </el-table-column>
        <el-table-column label="已拣货" min-width="110" align="right" show-overflow-tooltip>
          <template #default="{ row }">{{ formatQty(pickingQty(row, 'picked')) }}</template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="120">
          <template #default="{ row }">{{ pickingStatusText(row.status) }}</template>
        </el-table-column>
        <el-table-column label="创建时间" min-width="168" show-overflow-tooltip>
          <template #default="{ row }">{{ formatTaskTime(row.createTime) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="120" class-name="op-col" label-class-name="op-col">
          <template #default="{ row }">
            <button
              v-if="row.status !== 100"
              type="button"
              class="action-btn action-btn--warning"
              @click.stop="completePicking(row.id)"
            >完成拣货</button>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <div v-if="form.stockOutRequestId && pendingPickingTask" class="form-card">
      <h3 class="section-title">拣货明细（按在库 stockitem）</h3>
      <p class="picking-hint">
        任务号：<strong>{{ pendingPickingTask.taskCode }}</strong>。合计须等于出库通知数量（{{ notifyTargetQty }}）。保存后方可「完成拣货」。
      </p>
      <div class="picking-draft-toolbar">
        <button type="button" class="btn-secondary btn-sm" :disabled="loadingCandidates" @click="loadPickingCandidates">
          {{ loadingCandidates ? '加载中…' : '加载拣货候选' }}
        </button>
        <button
          type="button"
          class="btn-secondary btn-sm"
          style="margin-left: 8px"
          :disabled="!pickingCandidates.length"
          @click="applyFifoToPickDraft"
        >
          按 FIFO 自动分配
        </button>
        <button
          type="button"
          class="btn-primary btn-sm"
          style="margin-left: 8px"
          :disabled="savingPicking || !pendingPickingTask"
          @click="savePickingDraft"
        >
          {{ savingPicking ? '保存中…' : '保存拣货明细' }}
        </button>
        <span class="picking-draft-sum">已分配：<strong>{{ allocatedPickTotal }}</strong> / 目标：<strong>{{ notifyTargetQty }}</strong></span>
      </div>
      <el-table v-if="pickingCandidates.length" :data="pickingCandidates" class="quantum-table picking-candidates-table" max-height="380">
        <el-table-column label="在库明细" min-width="120" show-overflow-tooltip>
          <template #default="{ row }">
            <span :title="row.stockItemId">{{ shortId(row.stockItemId) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="materialId" label="物料" min-width="120" show-overflow-tooltip />
        <el-table-column label="型号" min-width="100" show-overflow-tooltip>
          <template #default="{ row }">{{ row.purchasePn || '—' }}</template>
        </el-table-column>
        <el-table-column label="品牌" min-width="88" show-overflow-tooltip>
          <template #default="{ row }">{{ row.purchaseBrand || '—' }}</template>
        </el-table-column>
        <el-table-column :label="t('inventoryList.columns.stockType')" width="100" align="center">
          <template #default="{ row }">{{ inventoryStockTypeLabel(row.stockType) }}</template>
        </el-table-column>
        <el-table-column label="来源" width="100" align="center">
          <template #default="{ row }">
            <span v-if="row.isStockingCandidate" class="picking-source-stocking">备货</span>
            <span v-else class="picking-source-normal">客单</span>
          </template>
        </el-table-column>
        <el-table-column label="可用" width="80" align="right">
          <template #default="{ row }">{{ formatQty(row.availableQty) }}</template>
        </el-table-column>
        <el-table-column label="本次拣货" width="130" align="center">
          <template #default="{ row }">
            <el-input-number
              :model-value="pickQty(row)"
              :min="0"
              :max="row.availableQty"
              :step="1"
              :precision="0"
              size="small"
              controls-position="right"
              style="width: 110px"
              @update:model-value="(v: number | undefined | null) => setPickQty(row, v)"
            />
          </template>
        </el-table-column>
      </el-table>
      <p v-else class="picking-empty subtle">请点击「加载拣货候选」获取本仓库可拣在库明细。</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Box } from '@element-plus/icons-vue'
import { stockOutApi, type StockOutRequestDto } from '@/api/stockOut'
import {
  inventoryCenterApi,
  type PickingStockItemCandidate,
  type PickingTask,
  type PickingTaskLine,
  type WarehouseInfo
} from '@/api/inventoryCenter'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime } from '@/utils/displayDateTime'

type ExecuteItem = {
  lineNo: number
  materialCode: string
  materialName: string
  quantity: number
}

type ExecuteForm = {
  stockOutRequestId: string
  stockOutCode: string
  warehouseId: string
  operatorId?: string
  stockOutDate: string
  remark?: string
  items: ExecuteItem[]
}

const router = useRouter()
const route = useRoute()
const { t, locale } = useI18n()
const submitting = ref(false)
const pickingTasks = ref<PickingTask[]>([])
const warehouses = ref<WarehouseInfo[]>([])
const currentRequest = ref<StockOutRequestDto | null>(null)
const pickingCandidates = ref<PickingStockItemCandidate[]>([])
const pickDraft = reactive<Record<string, number>>({})
const loadingCandidates = ref(false)
const savingPicking = ref(false)
const getYYMMDD = (d: Date) => {
  const yy = String(d.getFullYear()).slice(-2)
  const mm = String(d.getMonth() + 1).padStart(2, '0')
  const dd = String(d.getDate()).padStart(2, '0')
  return `${yy}${mm}${dd}`
}
const random4 = () => String(Math.floor(Math.random() * 10000)).padStart(4, '0')

const form = reactive<ExecuteForm>({
  stockOutRequestId: (route.query.requestId as string) || '',
  stockOutCode: `SOUT${getYYMMDD(new Date())}${random4()}`,
  warehouseId: '',
  operatorId: '',
  stockOutDate: new Date().toISOString(),
  remark: '',
  items: []
})

const totalQuantity = computed(() => form.items.reduce((sum, x) => sum + (x.quantity || 0), 0))
const pickingStatusText = (s: number) => ({ 1: '待拣货', 2: '拣货中', 100: '已完成', [-1]: '已取消' }[s] || '未知')

/** 数字展示（避免 el-input-number 只读在表格内不显示） */
const formatQty = (n: number | undefined | null) => {
  if (n == null || (typeof n === 'number' && Number.isNaN(n))) return '—'
  const v = Number(n)
  if (Number.isNaN(v)) return '—'
  return Number.isInteger(v) ? `${v}` : `${+v.toFixed(4)}`.replace(/\.?0+$/, '')
}

const warehouseLabel = (warehouseId: string) => {
  const w = warehouses.value.find((x) => x.id === warehouseId)
  return w ? `${w.warehouseName}（${w.warehouseCode}）` : warehouseId
}

/** 出库通知业务单号（SON），无则退回显式内部 ID */
const notifyRequestCodeDisplay = computed(() => {
  const code = currentRequest.value?.requestCode?.trim()
  if (code) return code
  return form.stockOutRequestId?.trim() || '—'
})

/** 读取拣货汇总数量（兼容 camelCase / PascalCase） */
function pickingTaskLines(row: PickingTask): PickingTaskLine[] {
  const r = row as unknown as Record<string, unknown>
  const raw = row.items ?? r.Items
  return Array.isArray(raw) ? (raw as PickingTaskLine[]) : []
}

function isPickingLineStockingSupplement(line: PickingTaskLine) {
  const x = line as unknown as Record<string, unknown>
  return Boolean(line.isStockingSupplement ?? x.IsStockingSupplement)
}

function pickingLineRowClassName({ row }: { row: PickingTaskLine }) {
  return isPickingLineStockingSupplement(row) ? 'picking-line-row--stocking' : ''
}

function inventoryStockTypeLabel(code: number): string {
  const m: Record<number, string> = {
    1: t('inventoryList.stockTypes.customer'),
    2: t('inventoryList.stockTypes.stocking'),
    3: t('inventoryList.stockTypes.sample')
  }
  return m[code] ?? t('inventoryList.stockTypes.unknown')
}

function pickingLineStockTypeLabel(line: PickingTaskLine): string {
  const x = line as unknown as Record<string, unknown>
  const n = line.stockType ?? x.StockType
  if (n == null || n === '') return t('inventoryList.stockTypes.unknown')
  const num = Number(n)
  return Number.isFinite(num) ? inventoryStockTypeLabel(num) : t('inventoryList.stockTypes.unknown')
}

function pickingTaskStockTypesDisplay(row: PickingTask): string {
  const r = row as unknown as Record<string, unknown>
  const raw = row.distinctStockTypes ?? r.DistinctStockTypes
  if (!Array.isArray(raw) || raw.length === 0) return t('inventoryList.stockTypes.unknown')
  const sep = locale.value === 'zh-CN' ? '、' : ', '
  return (raw as number[])
    .map((c) => Number(c))
    .filter((c) => Number.isFinite(c))
    .map((c) => inventoryStockTypeLabel(c))
    .join(sep)
}

const pickingQty = (row: PickingTask, kind: 'plan' | 'picked') => {
  const r = row as unknown as Record<string, unknown>
  const v =
    kind === 'plan'
      ? (row.planQtyTotal ?? r.PlanQtyTotal)
      : (row.pickedQtyTotal ?? r.PickedQtyTotal)
  if (v == null || v === '') return null
  const n = Number(v)
  return Number.isFinite(n) ? n : null
}

/** 创建时间：按系统展示时区（默认 Asia/Shanghai）格式化，避免原始 ISO 串误解 */
const formatTaskTime = (v?: string) => {
  const r = v as string | undefined
  if (!r) return '--'
  return formatDisplayDateTime(r)
}

const requestAlreadyShipped = computed(() => Number(currentRequest.value?.status) === 1)

const notifyTargetQty = computed(() => {
  const r = currentRequest.value as unknown as Record<string, unknown> | null
  const q = r?.outQuantity ?? r?.OutQuantity
  if (typeof q === 'number' && Number.isFinite(q) && q > 0) return Math.round(q)
  return Math.round(totalQuantity.value)
})

const pendingPickingTask = computed(() => {
  const rid = form.stockOutRequestId?.trim()
  if (!rid) return null
  return (
    pickingTasks.value.find(
      (t) =>
        (t.stockOutRequestId === rid || (t as unknown as Record<string, string>).StockOutRequestId === rid) &&
        t.status !== 100 &&
        t.status !== -1
    ) ?? null
  )
})

const allocatedPickTotal = computed(() =>
  pickingCandidates.value.reduce((s, c) => s + (pickDraft[c.stockItemId] ?? 0), 0)
)

const pickingLinesSaved = computed(() => {
  const rid = form.stockOutRequestId?.trim()
  if (!rid) return false
  return pickingTasks.value.some((t) => {
    if (t.stockOutRequestId !== rid && (t as unknown as Record<string, string>).StockOutRequestId !== rid) return false
    const n = pickingQty(t, 'plan')
    return n != null && n > 0
  })
})

const pickingCompleted = computed(() => pickingTasks.value.some((t) => t.status === 100))

/** 是否存在未取消的拣货任务（与后端「禁止重复生成」一致） */
const hasActivePickingTask = computed(() => pickingTasks.value.some((t) => t.status !== -1))

const generatePickingBtnTitle = computed(() => {
  if (requestAlreadyShipped.value) return '该出库通知已执行出库'
  if (hasActivePickingTask.value) return '该出库通知已有拣货任务，请勿重复生成'
  return ''
})

const hasItemsAndWarehouse = computed(
  () =>
    !!form.warehouseId &&
    form.items.length > 0 &&
    form.items.every((x) => x.materialCode && Number(x.quantity) > 0)
)

const canExecuteStockOut = computed(
  () =>
    !requestAlreadyShipped.value &&
    pickingCompleted.value &&
    !!form.stockOutRequestId &&
    !!form.stockOutCode?.trim() &&
    !!form.warehouseId &&
    form.items.length > 0
)

const executeOutHint = computed(() => {
  if (requestAlreadyShipped.value) return '该出库通知已出库'
  if (!pickingCompleted.value) return '请先保存拣货明细并完成拣货任务后再执行出库'
  return '将按已保存的拣货明细扣减在库并标记出库通知为已出库'
})

function shortId(id?: string | null) {
  const s = (id || '').trim()
  if (!s) return '—'
  return s.length <= 12 ? s : `${s.slice(0, 6)}…${s.slice(-4)}`
}

function pickingLineStockItemId(line: PickingTaskLine) {
  const x = line as unknown as Record<string, unknown>
  return String(line.stockItemId ?? x.StockItemId ?? '').trim()
}

function pickQty(c: PickingStockItemCandidate) {
  return pickDraft[c.stockItemId] ?? 0
}

function setPickQty(c: PickingStockItemCandidate, v: number | undefined | null) {
  const k = c.stockItemId
  const raw = typeof v === 'number' && Number.isFinite(v) ? Math.floor(v) : 0
  const n = Math.max(0, Math.min(raw, Math.max(0, Math.floor(Number(c.availableQty)))))
  if (n <= 0) delete pickDraft[k]
  else pickDraft[k] = n
}

function clearPickDraft() {
  for (const k of Object.keys(pickDraft)) delete pickDraft[k]
}

const loadWarehouses = async () => {
  try {
    warehouses.value = await inventoryCenterApi.getWarehouses()
    if (!form.warehouseId && warehouses.value.length) {
      form.warehouseId = warehouses.value[0].id || ''
    }
  } catch (e) {
    console.error(e)
    warehouses.value = []
  }
}

const loadRequest = async () => {
  if (!form.stockOutRequestId) return
  try {
    const requests = await stockOutApi.getRequestList()
    const rid = form.stockOutRequestId.trim()
    currentRequest.value =
      requests.find((x) => x.id === rid || x.id?.toLowerCase?.() === rid.toLowerCase()) || null
  } catch (e) {
    console.error(e)
    currentRequest.value = null
  }
}

const loadItemsFromRequest = async () => {
  if (!form.stockOutRequestId) {
    ElMessage.warning('请先选择出库通知')
    return
  }
  if (!currentRequest.value?.salesOrderId) {
    await loadRequest()
  }
  const r = currentRequest.value
  if (!r?.salesOrderId) {
    ElMessage.warning('出库通知缺少销售订单信息')
    return
  }
  const materialCode = String(r.materialModel ?? '').trim()
  const raw = r as Record<string, unknown>
  const qRaw = raw.outQuantity ?? raw.OutQuantity
  const qty = typeof qRaw === 'number' ? qRaw : Number(qRaw ?? 0)
  if (!materialCode || !Number.isFinite(qty) || qty <= 0) {
    ElMessage.warning('出库通知缺少物料或数量，无法生成出库明细')
    return
  }
  form.items = [
    {
      lineNo: 1,
      materialCode,
      materialName: String(r.brand ?? '').trim() || '物料',
      quantity: qty
    }
  ]
  await pickRecommendedWarehouse()
}

const pickRecommendedWarehouse = async () => {
  if (!form.items.length || !warehouses.value.length) return
  try {
    const overview = await inventoryCenterApi.getOverview()
    const materialSet = new Set(form.items.map(x => x.materialCode))
    const candidates = overview
      .filter(x => materialSet.has(x.materialId) && Number(x.availableQty || 0) > 0)
      .sort((a, b) => Number(b.availableQty || 0) - Number(a.availableQty || 0))
    if (!candidates.length) return
    const bestWarehouseId = candidates[0].warehouseId
    if (!form.warehouseId || !candidates.some(x => x.warehouseId === form.warehouseId)) {
      form.warehouseId = bestWarehouseId
    }
  } catch {
    // 推荐仓库失败不阻断主流程
  }
}

const loadPickingTasks = async () => {
  try {
    const tasks = await inventoryCenterApi.getPickingTasks()
    const requestId = form.stockOutRequestId
    pickingTasks.value = (tasks || []).filter(x => requestId && x.stockOutRequestId === requestId)
  } catch {
    pickingTasks.value = []
  }
}

const loadPickingCandidates = async () => {
  if (!form.stockOutRequestId?.trim() || !form.warehouseId?.trim()) {
    ElMessage.warning('请先选择出库通知与仓库')
    return
  }
  loadingCandidates.value = true
  try {
    const list = await inventoryCenterApi.getPickingCandidates(form.stockOutRequestId.trim(), form.warehouseId.trim())
    pickingCandidates.value = list || []
    clearPickDraft()
  } catch (e) {
    console.error(e)
    pickingCandidates.value = []
    ElMessage.error(getApiErrorMessage(e, '加载拣货候选失败'))
  } finally {
    loadingCandidates.value = false
  }
}

const applyFifoToPickDraft = () => {
  clearPickDraft()
  const target = notifyTargetQty.value
  let rem = target
  for (const c of pickingCandidates.value) {
    if (rem <= 0) break
    const avail = Math.max(0, Math.floor(Number(c.availableQty)))
    const take = Math.min(rem, avail)
    if (take > 0) {
      pickDraft[c.stockItemId] = take
      rem -= take
    }
  }
  if (rem > 0) ElMessage.warning(`候选可用量不足，尚有 ${rem} 未分配，请补库存或手工调整`)
  else ElMessage.success('已按 FIFO 顺序填满（请核对后保存拣货明细）')
}

const savePickingDraft = async () => {
  const task = pendingPickingTask.value
  if (!task?.id) return
  const lines = pickingCandidates.value
    .map((c) => ({ stockItemId: c.stockItemId, stockId: c.stockAggregateId, qty: pickQty(c) }))
    .filter((l) => l.qty > 0)
  const sum = lines.reduce((a, l) => a + l.qty, 0)
  if (sum !== notifyTargetQty.value) {
    ElMessage.error(`拣货数量合计须等于出库通知数量（${notifyTargetQty.value}），当前为 ${sum}`)
    return
  }
  savingPicking.value = true
  try {
    await inventoryCenterApi.savePickingTaskItems(task.id, lines)
    ElMessage.success('拣货明细已保存')
    await loadPickingTasks()
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, '保存拣货明细失败'))
  } finally {
    savingPicking.value = false
  }
}

const handleGeneratePicking = async () => {
  if (requestAlreadyShipped.value) {
    ElMessage.warning('该出库通知已执行出库，无法再次生成拣货任务')
    return
  }
  if (hasActivePickingTask.value) {
    ElMessage.warning('该出库通知已存在拣货任务，请勿重复生成')
    return
  }
  if (!form.stockOutRequestId) {
    ElMessage.warning('请先填写出库申请单ID')
    return
  }
  if (!form.warehouseId || !form.items.length) {
    ElMessage.warning('请先填写仓库和出库明细')
    return
  }
  if (form.items.some(x => !x.materialCode || Number(x.quantity || 0) <= 0)) {
    ElMessage.warning('出库明细存在空物料或数量为0，请检查来源数据')
    return
  }
  try {
    await inventoryCenterApi.generatePickingTask({
      stockOutRequestId: form.stockOutRequestId,
      warehouseId: form.warehouseId,
      operatorId: form.operatorId,
      items: []
    })
    ElMessage.success('拣货任务已生成')
    await loadPickingTasks()
    await loadPickingCandidates()
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, '生成拣货任务失败'))
  }
}

const completePicking = async (taskId: string) => {
  if (requestAlreadyShipped.value) {
    ElMessage.warning('该出库通知已出库')
    return
  }
  try {
    await inventoryCenterApi.completePickingTask(taskId)
    ElMessage.success('拣货已完成')
    await loadPickingTasks()
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, '完成拣货失败'))
  }
}

const handleSubmit = async () => {
  if (requestAlreadyShipped.value) {
    ElMessage.warning('该出库通知已执行出库')
    return
  }
  if (!pickingCompleted.value) {
    ElMessage.warning('请先完成拣货任务后再执行出库')
    return
  }
  if (!form.stockOutCode || !form.warehouseId) {
    ElMessage.warning('请填写出库单号和仓库')
    return
  }
  if (!form.items.length) {
    ElMessage.warning('请至少添加一条出库明细')
    return
  }

  submitting.value = true
  try {
    await stockOutApi.execute({
      stockOutRequestId: form.stockOutRequestId,
      stockOutCode: form.stockOutCode,
      warehouseId: form.warehouseId,
      operatorId: form.operatorId,
      stockOutDate: form.stockOutDate,
      remark: form.remark,
      items: form.items
    })
    ElMessage.success('执行出库成功，出库通知已标记为已出库')
    router.push({ name: 'StockOutNotifyList' })
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, '执行出库失败'))
  } finally {
    submitting.value = false
  }
}

const goBack = () => {
  router.push({ name: 'StockOutNotifyList' })
}

const init = async () => {
  await loadWarehouses()
  await loadRequest()
  if (requestAlreadyShipped.value) {
    ElMessage.info('该出库通知已执行出库，仅可查看信息')
  }
  await loadItemsFromRequest()
  await loadPickingTasks()
  if (pendingPickingTask.value && form.warehouseId?.trim()) {
    await loadPickingCandidates()
  }
}

init()
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.stockout-edit-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
  .header-left { display: flex; align-items: center; gap: 12px; }
  .header-right { display: flex; align-items: center; gap: 8px; }
}
.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
  .page-icon {
    width: 36px;
    height: 36px;
    background: rgba(0, 212, 255, 0.1);
    border: 1px solid rgba(0, 212, 255, 0.25);
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: $cyan-primary;
  }
  .page-title { font-size: 20px; font-weight: 600; color: $text-primary; margin: 0; }
}
.btn-primary,
.btn-secondary,
.btn-picking {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  cursor: pointer;
  border: 1px solid transparent;
}
.btn-primary {
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border-color: rgba(0, 212, 255, 0.4);
  color: #fff;
}
.btn-picking {
  background: linear-gradient(135deg, rgba(0, 140, 120, 0.55), rgba(0, 100, 90, 0.45));
  border-color: rgba(0, 212, 180, 0.4);
  color: #e8fff8;
  &:hover:not(:disabled) {
    filter: brightness(1.08);
  }
}
.btn-secondary {
  background: rgba(255, 255, 255, 0.05);
  border-color: $border-panel;
  color: $text-secondary;
}
.btn-sm {
  padding: 6px 10px;
  font-size: 12px;
}
.form-layout {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.form-card {
  background: $layer-2;
  border-radius: 8px;
  border: 1px solid $border-panel;
  padding: 16px 18px;
}
.basic-info-form {
  :deep(.el-form-item__label) {
    width: 6em !important;
    min-width: 6em;
    max-width: 6em;
    text-align: right;
    justify-content: flex-end;
    padding-right: 10px;
    box-sizing: content-box;
    white-space: nowrap;
  }
}
.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
}
.section-title {
  font-size: 14px;
  font-weight: 500;
  color: $text-secondary;
  margin: 0 0 8px;
}
.table-footer {
  display: flex;
  justify-content: flex-end;
  margin-top: 8px;
  .total {
    font-size: 13px;
    color: $text-secondary;
    span {
      color: $cyan-primary;
      font-weight: 600;
      margin-left: 4px;
    }
  }
}
.action-btn {
  background: transparent;
  border: none;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 12px;
  padding: 2px 6px;
  white-space: nowrap;
  flex-shrink: 0;
  &:hover:not(:disabled) {
    text-decoration: underline;
  }
  &:disabled,
  &[disabled] {
    color: rgba(140, 155, 175, 0.55) !important;
    cursor: not-allowed;
    opacity: 1;
  }
}
.qty-cell {
  font-variant-numeric: tabular-nums;
  color: $text-primary;
  font-size: 13px;
}

.flow-alert {
  margin-bottom: 16px;
  background: rgba(0, 212, 255, 0.06) !important;
  border: 1px solid rgba(0, 212, 255, 0.2) !important;
}
.flow-alert-title {
  font-weight: 600;
  color: $text-primary;
}
.flow-steps {
  margin: 8px 0 0;
  padding-left: 1.25rem;
  color: rgba(200, 216, 232, 0.85);
  font-size: 13px;
  line-height: 1.7;
  li {
    margin-bottom: 2px;
  }
  .flow-step--done {
    color: #46bf91;
  }
}
.flow-warn-msg,
.flow-done-msg {
  margin: 10px 0 0;
  font-size: 12px;
}
.flow-warn-msg {
  color: #ffc107;
}
.flow-done-msg {
  color: #46bf91;
}
.form-sub-hint {
  margin-top: 6px;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.4;
  word-break: break-all;
}
.picking-hint {
  margin: 0 0 12px;
  font-size: 12px;
  color: rgba(200, 216, 232, 0.75);
  line-height: 1.55;
}
.picking-empty {
  margin: 0 0 12px;
  font-size: 13px;
  color: $text-muted;
}
.picking-expand-inner {
  padding: 8px 12px 14px 40px;
  background: rgba(0, 0, 0, 0.14);
  border-radius: 8px;
  border: 1px solid rgba(0, 212, 255, 0.08);
}
.picking-expand-title {
  font-size: 12px;
  color: $text-muted;
  margin-bottom: 8px;
}
:deep(.picking-lines-table tr.picking-line-row--stocking td.el-table__cell) {
  background: rgba(255, 193, 7, 0.14) !important;
}
:deep(.picking-lines-table tr.picking-line-row--stocking:hover td.el-table__cell) {
  background: rgba(255, 193, 7, 0.22) !important;
}
.picking-source-stocking {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  color: #ffc107;
  font-weight: 600;
  font-size: 12px;
}
.picking-stock-icon {
  font-size: 16px;
}
.picking-source-normal {
  font-size: 12px;
  color: rgba(200, 216, 232, 0.72);
}
.btn-primary:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}
.btn-secondary:disabled,
.btn-picking:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}
.picking-draft-toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  margin-bottom: 12px;
}
.picking-draft-sum {
  margin-left: auto;
  font-size: 13px;
  color: rgba(200, 216, 232, 0.88);
}
.picking-candidates-table {
  margin-top: 4px;
}
.picking-empty.subtle {
  font-size: 12px;
  opacity: 0.92;
}
</style>

