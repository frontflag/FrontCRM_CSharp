<template>
  <div class="inventory-trace-page">
    <div class="page-header">
      <div class="header-left">
        <h1 class="page-title">物料入库追溯</h1>
        <div class="sub-title">物料ID：{{ materialId || '--' }}</div>
      </div>
      <div class="header-right">
        <button class="btn-secondary" @click="goBack">返回库存中心</button>
        <button class="btn-primary" @click="loadTrace">刷新</button>
      </div>
    </div>

    <CrmDataTable :data="traceList" v-loading="loading">
      <el-table-column prop="stockInTime" label="入库时间" width="170">
        <template #default="{ row }">{{ formatTime(row.stockInTime) }}</template>
      </el-table-column>
      <el-table-column prop="stockInCode" label="入库单号" width="160" />
      <el-table-column prop="quantity" label="数量" width="100" align="right" />
      <el-table-column prop="unitPrice" label="单价" width="110" align="right">
        <template #default="{ row }">{{ formatMoney(row.unitPrice) }}</template>
      </el-table-column>
      <el-table-column prop="purchaseOrderCode" label="采购单号" width="150" />
      <el-table-column prop="purchaseUserName" label="采购员" width="130" />
      <el-table-column prop="qcCode" label="质检单号" width="150" />
      <el-table-column prop="qcStatus" label="质检状态" width="110">
        <template #default="{ row }">{{ qcText(row.qcStatus) }}</template>
      </el-table-column>
      <el-table-column label="仓库" min-width="140" show-overflow-tooltip>
        <template #default="{ row }">{{ row.warehouseName || row.warehouseId || '--' }}</template>
      </el-table-column>
      <el-table-column prop="locationId" label="库位" min-width="130" />
    </CrmDataTable>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { inventoryCenterApi, type MaterialTrace } from '@/api/inventoryCenter'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime } from '@/utils/displayDateTime'

const route = useRoute()
const router = useRouter()
const loading = ref(false)
const traceList = ref<MaterialTrace[]>([])

const materialId = computed(() => String(route.params.materialId || '').trim())

const formatMoney = (v: number) => (v == null ? '--' : Number(v).toFixed(2))
const formatTime = (v?: string) => formatDisplayDateTime(v)
const qcText = (s?: number) => ({ [-1]: '未通过', 10: '部分通过', 100: '通过' }[s ?? 0] || '--')

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
    ElMessage.error(getApiErrorMessage(e, '加载入库追溯失败'))
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
