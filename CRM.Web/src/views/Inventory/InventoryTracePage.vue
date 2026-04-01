<template>
  <div class="inventory-trace-page">
    <div class="page-header">
      <div class="header-left">
        <h1 class="page-title">{{ t('inventoryTrace.title') }}</h1>
        <div class="sub-title">{{ t('inventoryTrace.materialId') }}: {{ materialId || t('quoteList.na') }}</div>
      </div>
      <div class="header-right">
        <button class="btn-secondary" @click="goBack">{{ t('inventoryTrace.back') }}</button>
        <button class="btn-primary" @click="loadTrace">{{ t('inventoryTrace.refresh') }}</button>
      </div>
    </div>

    <CrmDataTable :data="traceList" v-loading="loading">
      <el-table-column prop="stockInTime" :label="t('inventoryTrace.columns.stockInTime')" width="170">
        <template #default="{ row }">{{ formatTime(row.stockInTime) }}</template>
      </el-table-column>
      <el-table-column prop="stockInCode" :label="t('inventoryTrace.columns.stockInCode')" width="160" />
      <el-table-column prop="quantity" :label="t('inventoryTrace.columns.quantity')" width="100" align="right" />
      <el-table-column prop="unitPrice" :label="t('inventoryTrace.columns.unitPrice')" width="110" align="right">
        <template #default="{ row }">{{ formatUnitPriceNumber(row.unitPrice) }}</template>
      </el-table-column>
      <el-table-column prop="purchaseOrderCode" :label="t('inventoryTrace.columns.purchaseOrderCode')" width="150" />
      <el-table-column prop="purchaseUserName" :label="t('inventoryTrace.columns.purchaser')" width="130" />
      <el-table-column prop="qcCode" :label="t('inventoryTrace.columns.qcCode')" width="150" />
      <el-table-column prop="qcStatus" :label="t('inventoryTrace.columns.qcStatus')" width="110">
        <template #default="{ row }">{{ qcText(row.qcStatus) }}</template>
      </el-table-column>
      <el-table-column :label="t('inventoryTrace.columns.warehouse')" min-width="140" show-overflow-tooltip>
        <template #default="{ row }">{{ row.warehouseName || row.warehouseId || t('quoteList.na') }}</template>
      </el-table-column>
      <el-table-column prop="locationId" :label="t('inventoryTrace.columns.location')" min-width="130" />
    </CrmDataTable>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { inventoryCenterApi, type MaterialTrace } from '@/api/inventoryCenter'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { formatUnitPriceNumber } from '@/utils/moneyFormat'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const loading = ref(false)
const traceList = ref<MaterialTrace[]>([])

const materialId = computed(() => String(route.params.materialId || '').trim())

const formatTime = (v?: string) => formatDisplayDateTime(v)
const qcText = (s?: number) => ({
  [-1]: t('inventoryTrace.qc.failed'),
  10: t('inventoryTrace.qc.partial'),
  100: t('inventoryTrace.qc.passed')
}[s ?? 0] || t('quoteList.na'))

const loadTrace = async () => {
  if (!materialId.value) {
    traceList.value = []
    return
  }
  loading.value = true
  try {
    traceList.value = await inventoryCenterApi.getMaterialTrace(materialId.value)
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, t('inventoryTrace.loadFailed')))
    traceList.value = []
  } finally {
    loading.value = false
  }
}

const goBack = () => {
  router.push('/inventory/list')
}

watch(() => materialId.value, () => loadTrace())
onMounted(() => loadTrace())
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.inventory-trace-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}

.header-left {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
}

.sub-title {
  color: $text-muted;
  font-size: 12px;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.btn-primary,
.btn-secondary {
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

.btn-secondary {
  background: rgba(255, 255, 255, 0.05);
  border-color: $border-panel;
  color: $text-secondary;
}
</style>
