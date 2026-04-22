<template>
  <div class="picking-slip-detail-page stockout-notify-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('pickingSlip.detailTitle') }}</h1>
        </div>
      </div>
      <div class="header-right">
        <button type="button" class="btn-secondary" @click="goBack">{{ t('pickingSlip.detail.back') }}</button>
      </div>
    </div>

    <el-skeleton v-if="loading" :rows="6" animated />
    <template v-else-if="detail">
      <div class="detail-card">
        <h3 class="section-title">{{ t('pickingSlip.detail.sectionHeader') }}</h3>
        <el-descriptions :column="2" border>
          <el-descriptions-item :label="t('pickingSlip.columns.status')">{{ statusLabel(detail) }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.warehouse')">{{ d('warehouseDisplay') }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.materialModel')">{{ d('materialModel') }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.brand')">{{ d('brand') }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.customerName')">{{ d('customerName') }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.salesUserName')">{{ d('salesUserName') }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.planQtyTotal')">{{ d('planQtyTotal') }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.lineCount')">{{ d('lineCount') }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.stockOutRequestCode')">{{ d('stockOutRequestCode') }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.taskCode')">{{ d('taskCode') }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.createTime')">{{ formatTime }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.createUser')">{{ d('createUserDisplay') }}</el-descriptions-item>
          <el-descriptions-item v-if="d('remark') !== '—'" :label="t('pickingSlip.detail.remark')" :span="2">
            {{ d('remark') }}
          </el-descriptions-item>
          <el-descriptions-item
            v-if="stockTypesDisplay"
            :label="t('pickingSlip.detail.stockTypes')"
            :span="2"
          >{{ stockTypesDisplay }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <div class="detail-card">
        <h3 class="section-title">{{ t('pickingSlip.detail.sectionLines') }}</h3>
        <el-table :data="lines" border class="lines-table" size="small" empty-text="—">
          <el-table-column label="物料" min-width="120" prop="materialId" show-overflow-tooltip />
          <el-table-column label="在库明细编号" min-width="140" show-overflow-tooltip>
            <template #default="{ row }">{{ lineStockItemCode(row) }}</template>
          </el-table-column>
          <el-table-column label="入库明细编号" min-width="140" show-overflow-tooltip>
            <template #default="{ row }">{{ lineStockInItemCode(row) }}</template>
          </el-table-column>
          <el-table-column :label="t('inventoryList.columns.stockType')" width="100" align="center">
            <template #default="{ row }">{{ stockTypeLabel(row) }}</template>
          </el-table-column>
          <el-table-column label="计划" width="88" align="right">
            <template #default="{ row }">{{ row.planQty }}</template>
          </el-table-column>
          <el-table-column label="已拣" width="88" align="right">
            <template #default="{ row }">{{ row.pickedQty }}</template>
          </el-table-column>
          <el-table-column label="来源" width="110" align="center">
            <template #default="{ row }">
              <span v-if="lineIsStocking(row)" class="tag-stocking">{{ t('inventoryList.stockTypes.stocking') }}</span>
              <span v-else class="tag-normal">{{ t('inventoryList.stockTypes.customer') }}</span>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </template>
    <el-empty v-else :description="loadError || '—'" />
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { inventoryCenterApi, type PickingTaskDetailView, type PickingTaskLine } from '@/api/inventoryCenter'
import { formatDate as formatDateTimeZh } from '@/utils/date'
import { getApiErrorMessage } from '@/utils/apiError'

const route = useRoute()
const router = useRouter()
const { t, locale } = useI18n()
const loading = ref(false)
const detail = ref<PickingTaskDetailView | null>(null)
const loadError = ref('')

const raw = computed(() => detail.value as unknown as Record<string, unknown> | null)

function d(key: string) {
  const r = raw.value
  if (!r) return '—'
  const pascal = key.charAt(0).toUpperCase() + key.slice(1)
  const v = r[key] ?? r[pascal]
  if (v == null || v === '') return '—'
  return String(v)
}

const formatTime = computed(() => {
  const r = raw.value
  if (!r) return '—'
  const v = (r.createTime ?? r.CreateTime) as string | undefined
  if (!v) return '—'
  return formatDateTimeZh(v, 'YYYY-MM-DD HH:mm')
})

const lines = computed<PickingTaskLine[]>(() => {
  const x = detail.value as unknown as Record<string, unknown> | null
  if (!x) return []
  const rawLines = x.items ?? x.Items
  return Array.isArray(rawLines) ? (rawLines as PickingTaskLine[]) : []
})

const stockTypesDisplay = computed(() => {
  const r = raw.value
  if (!r) return ''
  const arr = (r.distinctStockTypes ?? r.DistinctStockTypes) as unknown
  if (!Array.isArray(arr) || arr.length === 0) return ''
  return (arr as number[])
    .map((c) => stockTypeLabelCode(Number(c)))
    .filter(Boolean)
    .join(locale.value === 'zh-CN' ? '、' : ', ')
})

function stockTypeLabelCode(code: number) {
  const m: Record<number, string> = {
    1: t('inventoryList.stockTypes.customer'),
    2: t('inventoryList.stockTypes.stocking'),
    3: t('inventoryList.stockTypes.sample')
  }
  return m[code] ?? ''
}

function statusLabel(row: PickingTaskDetailView) {
  const r = row as unknown as Record<string, unknown>
  const s = Number(r.status ?? r.Status ?? 0)
  if (s === 1) return t('pickingSlip.status.pending')
  if (s === 2) return t('pickingSlip.status.inProgress')
  if (s === 100) return t('pickingSlip.status.done')
  if (s === -1) return t('pickingSlip.status.cancelled')
  return t('pickingSlip.status.unknown')
}

function lineRecord(line: PickingTaskLine) {
  return line as unknown as Record<string, unknown>
}

function stockTypeLabel(line: PickingTaskLine) {
  const x = lineRecord(line)
  const n = Number(x.stockType ?? x.StockType ?? '')
  if (!Number.isFinite(n)) return '—'
  return stockTypeLabelCode(n) || '—'
}

function lineIsStocking(line: PickingTaskLine) {
  const x = lineRecord(line)
  return Boolean(x.isStockingSupplement ?? x.IsStockingSupplement)
}

function lineStockItemCode(line: PickingTaskLine) {
  const x = lineRecord(line)
  const code = String(x.stockItemCode ?? x.StockItemCode ?? '').trim()
  if (code) return code
  const id = String(x.stockItemId ?? x.StockItemId ?? '').trim()
  if (!id) return '—'
  return id.length <= 12 ? id : `${id.slice(0, 6)}…${id.slice(-4)}`
}

function lineStockInItemCode(line: PickingTaskLine) {
  const x = lineRecord(line)
  const v = String(x.stockInItemCode ?? x.StockInItemCode ?? '').trim()
  return v || '—'
}

const goBack = () => {
  router.push({ name: 'PickingSlipList' })
}

const load = async () => {
  const id = String(route.params.id || '').trim()
  if (!id) {
    loadError.value = '—'
    detail.value = null
    return
  }
  loading.value = true
  loadError.value = ''
  try {
    detail.value = await inventoryCenterApi.getPickingListDetail(id)
  } catch (e) {
    console.error(e)
    detail.value = null
    loadError.value = getApiErrorMessage(e, t('pickingSlip.messages.loadDetailFailed'))
    ElMessage.error(loadError.value)
  } finally {
    loading.value = false
  }
}

watch(
  () => route.params.id,
  () => {
    void load()
  },
  { immediate: true }
)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.picking-slip-detail-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
}
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}
.header-left,
.header-right {
  display: flex;
  align-items: center;
  gap: 10px;
}
.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
}
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
.page-title {
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
  margin: 0;
}
.btn-secondary {
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  cursor: pointer;
  border: 1px solid $border-panel;
  background: rgba(255, 255, 255, 0.05);
  color: $text-secondary;
}
.detail-card {
  background: $layer-2;
  border-radius: 8px;
  padding: 16px 18px;
  margin-bottom: 16px;
  border: 1px solid rgba(255, 255, 255, 0.06);
}
.section-title {
  margin: 0 0 12px;
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
}
.lines-table {
  width: 100%;
}
.tag-stocking {
  color: #ffc107;
  font-weight: 600;
  font-size: 12px;
}
.tag-normal {
  font-size: 12px;
  color: rgba(200, 216, 232, 0.75);
}
</style>
