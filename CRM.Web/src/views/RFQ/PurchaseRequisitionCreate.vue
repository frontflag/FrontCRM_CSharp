<template>
  <div class="pr-create-page">
    <el-page-header @back="$router.push('/purchase-requisitions')">
      <template #content>
        <span class="title">新建采购申请</span>
      </template>
    </el-page-header>

    <el-card class="card">
      <el-form label-width="140px" :model="topForm" :rules="topRules" ref="topFormRef">
        <el-form-item label="销售订单">
          <div class="row">
            <el-input v-model="topForm.sellOrderId" placeholder="销售订单 ID（GUID）" clearable style="width: 360px" />
            <el-button type="primary" @click="loadSo" :loading="loadingSo">加载明细</el-button>
          </div>
          <div v-if="soCode" class="hint">单号：{{ soCode }}，状态：{{ soStatusText }}</div>
        </el-form-item>

        <el-form-item label="预计采购日期" prop="expectedPurchaseTime" required>
          <el-date-picker
            v-model="topForm.expectedPurchaseTime"
            type="datetime"
            placeholder="请选择预计采购日期"
            value-format="YYYY-MM-DDTHH:mm:ss[Z]"
            style="width: 280px"
          />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="topForm.remark" type="textarea" rows="2" placeholder="请输入备注" />
        </el-form-item>

        <el-form-item v-if="lineRows.length" label="明细行">
          <div class="table-wrap">
            <el-table :data="lineRows" border size="small" row-key="sellOrderItemId">
              <el-table-column prop="pn" label="物料型号" min-width="130" show-overflow-tooltip>
                <template #default="{ row }">
                  <el-link type="primary">{{ row.pn || '—' }}</el-link>
                </template>
              </el-table-column>
              <el-table-column prop="brand" label="品牌" width="120" show-overflow-tooltip>
                <template #default="{ row }">
                  <el-link type="primary">{{ row.brand || '—' }}</el-link>
                </template>
              </el-table-column>
              <el-table-column prop="salesOrderQty" label="销售订单数量" width="120" align="right" />
              <el-table-column prop="remainingQty" label="剩余数量" width="100" align="right" />
              <el-table-column label="本次申请采购数" width="160" align="center">
                <template #default="{ row }">
                  <el-input-number v-model="row.requestQty" :min="0" :precision="4" :max="row.remainingQty" size="small" controls-position="right" style="width: 130px" />
                </template>
              </el-table-column>
            </el-table>
          </div>
          <p class="hint">仅展示尚未创建采购申请的明细；可为多行填写本次申请数量后一次提交。</p>
        </el-form-item>

        <el-form-item label="类型">
          <el-radio-group v-model="topForm.type">
            <el-radio :value="0">专属</el-radio>
            <el-radio :value="1">公开备货</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :loading="submitting" :disabled="!lineRows.length" @click="submitBatch">确认</el-button>
          <el-button @click="$router.push('/purchase-requisitions')">取消</el-button>
          <el-button v-if="topForm.sellOrderId.trim()" :loading="autoLoading" @click="autoGen">整单自动生成</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import salesOrderApi from '@/api/salesOrder'
import purchaseRequisitionApi from '@/api/purchaseRequisition'
import { validateElFormOrWarn, runSaveTask } from '@/composables/useFormSubmit'

interface LineRow {
  sellOrderItemId: string
  pn?: string
  brand?: string
  salesOrderQty: number
  remainingQty: number
  requestQty: number
}

const router = useRouter()
const route = useRoute()
const topFormRef = ref<FormInstance>()
const loadingSo = ref(false)
const submitting = ref(false)
const autoLoading = ref(false)
const soCode = ref('')
const soStatus = ref<number | null>(null)
const lineRows = ref<LineRow[]>([])

const topForm = reactive({
  sellOrderId: '',
  expectedPurchaseTime: '' as string,
  remark: '',
  type: 0
})

const topRules: FormRules = {
  expectedPurchaseTime: [{ required: true, message: '请选择预计采购日期', trigger: 'change' }]
}

const soStatusText = computed(() => {
  const s = soStatus.value
  if (s === null) return '—'
  const m: Record<number, string> = {
    0: '草稿', 1: '审批中', 2: '已审批', 3: '已确认', 6: '已完成', [-1]: '已取消', [-2]: '已驳回'
  }
  return m[s] ?? String(s)
})

async function loadSo() {
  const id = topForm.sellOrderId.trim()
  if (!id) {
    ElMessage.warning('请输入销售订单 ID')
    return
  }
  loadingSo.value = true
  lineRows.value = []
  try {
    const [d, opts] = await Promise.all([
      salesOrderApi.getById(id),
      purchaseRequisitionApi.getLineOptions(id)
    ])
    soCode.value = d.sellOrderCode
    soStatus.value = d.status

    const list = opts || []
    lineRows.value = list.map((r: any) => ({
      sellOrderItemId: r.sellOrderItemId,
      pn: r.pn,
      brand: r.brand,
      salesOrderQty: r.salesOrderQty,
      remainingQty: r.remainingQty,
      requestQty: Number(r.remainingQty) || 0
    }))
    applyItemIdFilterFromRoute()
    if (!lineRows.value.length) {
      ElMessage.info('当前订单没有可新建采购申请的明细（可能均已创建或明细已取消）')
    }
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.message || e?.message || '加载失败')
  } finally {
    loadingSo.value = false
  }
}

/** 路由 ?itemIds=a,b,c 时只保留对应明细行 */
function applyItemIdFilterFromRoute() {
  const raw = route.query.itemIds
  if (!raw || typeof raw !== 'string') return
  const allow = new Set(
    raw
      .split(',')
      .map((s) => s.trim())
      .filter(Boolean)
  )
  if (!allow.size) return
  lineRows.value = lineRows.value.filter((r) => allow.has(r.sellOrderItemId))
  lineRows.value.forEach((r) => {
    if (r.requestQty <= 0 && r.remainingQty > 0) r.requestQty = Number(r.remainingQty) || 0
  })
}

onMounted(async () => {
  const qso = route.query.sellOrderId
  if (typeof qso === 'string' && qso.trim()) {
    topForm.sellOrderId = qso.trim()
    await loadSo()
  }
})

async function submitBatch() {
  if (!(await validateElFormOrWarn(topFormRef))) return
  if (!lineRows.value.length) {
    ElMessage.warning('请先加载销售订单并确保有可申请的明细行')
    return
  }
  const rows = lineRows.value.filter((r) => r.requestQty > 0)
  if (!rows.length) {
    ElMessage.warning('请至少在一行填写大于 0 的本次申请采购数')
    return
  }
  for (const r of rows) {
    if (r.requestQty > r.remainingQty) {
      ElMessage.warning(`物料 ${r.pn || r.sellOrderItemId} 的申请数不能大于剩余数量`)
      return
    }
  }
  const exp = topForm.expectedPurchaseTime
  if (!exp) {
    ElMessage.warning('请选择预计采购日期')
    return
  }
  await runSaveTask({
    loading: submitting,
    task: async () => {
      let lastId = ''
      for (const r of rows) {
        const created = await purchaseRequisitionApi.create({
          sellOrderItemId: r.sellOrderItemId,
          qty: r.requestQty,
          expectedPurchaseTime: exp,
          type: topForm.type,
          remark: topForm.remark || undefined
        })
        lastId = created?.id || lastId
      }
      return { lastId, count: rows.length }
    },
    formatSuccess: (r) => `已创建 ${r.count} 条采购申请`,
    onSuccess: (r) => {
      if (r.count === 1 && r.lastId) router.push(`/purchase-requisitions/${r.lastId}`)
      else router.push('/purchase-requisitions')
    },
    errorMessage: (e: unknown) => {
      const err = e as { response?: { data?: { message?: string } }; message?: string }
      return err?.response?.data?.message || err?.message || '创建失败'
    }
  })
}

async function autoGen() {
  const id = topForm.sellOrderId.trim()
  if (!id) {
    ElMessage.warning('请先填写销售订单 ID 并加载')
    return
  }
  autoLoading.value = true
  try {
    const generated = await purchaseRequisitionApi.autoGenerate(id)
    const n = Array.isArray(generated) ? generated.length : 0
    ElMessage.success(n ? `已生成 ${n} 条申请` : '没有可生成的明细（可能已全部存在申请）')
    router.push('/purchase-requisitions')
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.message || e?.message || '生成失败')
  } finally {
    autoLoading.value = false
  }
}
</script>

<style scoped>
.pr-create-page { padding: 0 0 32px; max-width: 1000px; }
.title { font-weight: 600; }
.card { margin-top: 16px; }
.row { display: flex; gap: 12px; align-items: center; flex-wrap: wrap; }
.hint { margin-top: 8px; color: var(--el-text-color-secondary); font-size: 13px; }
.table-wrap { width: 100%; overflow-x: auto; }
</style>
