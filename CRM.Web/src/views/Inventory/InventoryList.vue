<template>
  <div class="inventory-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 003 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z"/>
              <polyline points="3.27 6.96 12 12.01 20.73 6.96"/>
              <line x1="12" y1="22.08" x2="12" y2="12"/>
            </svg>
          </div>
          <h1 class="page-title">库存列表</h1>
        </div>
        <div class="count-badge">共 {{ list.length }} 条</div>
      </div>
      <div class="header-right">
        <el-input
          v-model="warehouseFilter"
          placeholder="仓库ID 筛选"
          clearable
          style="width: 180px; margin-right: 8px;"
          @keyup.enter="fetchList"
        />
        <button class="btn-primary" @click="fetchList">刷新</button>
      </div>
    </div>

    <div class="table-panel">
      <el-table
        :data="list"
        v-loading="loading"
        class="quantum-table"
        :header-cell-style="tableHeaderStyle"
        :cell-style="tableCellStyle"
      >
        <el-table-column type="index" width="50" align="center" />
        <el-table-column prop="materialId" label="物料ID" width="140" show-overflow-tooltip />
        <el-table-column prop="warehouseId" label="仓库ID" width="140" show-overflow-tooltip />
        <el-table-column prop="quantity" label="库存数量" width="110" align="right">
          <template #default="{ row }">{{ formatNum(row.quantity) }}</template>
        </el-table-column>
        <el-table-column prop="availableQuantity" label="可用数量" width="110" align="right">
          <template #default="{ row }">{{ formatNum(row.availableQuantity) }}</template>
        </el-table-column>
        <el-table-column prop="lockedQuantity" label="锁定数量" width="110" align="right">
          <template #default="{ row }">{{ formatNum(row.lockedQuantity) }}</template>
        </el-table-column>
        <el-table-column prop="unit" label="单位" width="70" />
        <el-table-column prop="batchNo" label="批次号" width="100" show-overflow-tooltip />
        <el-table-column prop="status" label="状态" width="80">
          <template #default="{ row }">
            <span :class="['status-dot', row.status === 1 ? 'normal' : 'frozen']">
              {{ row.status === 1 ? '正常' : '冻结' }}
            </span>
          </template>
        </el-table-column>
      </el-table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { stockApi, type StockInfo } from '@/api/stock'

const loading = ref(false)
const list = ref<StockInfo[]>([])
const warehouseFilter = ref('')

const tableHeaderStyle = () => ({
  background: '#0A1628',
  color: 'rgba(200,216,232,0.55)',
  fontSize: '12px',
  fontWeight: '500',
  borderBottom: '1px solid rgba(0,212,255,0.12)',
  padding: '10px 0'
})
const tableCellStyle = () => ({
  background: 'transparent',
  borderBottom: '1px solid rgba(255,255,255,0.05)',
  color: 'rgba(224,244,255,0.85)',
  fontSize: '13px'
})

const formatNum = (v: number) => (v == null ? '--' : Number(v).toLocaleString())

const fetchList = async () => {
  loading.value = true
  try {
    list.value = await stockApi.getList(warehouseFilter.value || undefined)
  } catch (e) {
    console.error(e)
    list.value = []
  } finally {
    loading.value = false
  }
}

onMounted(() => fetchList())
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.inventory-list-page {
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
  .header-right { display: flex; align-items: center; }
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
.count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}
.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  border-radius: 8px;
  color: #fff;
  font-size: 13px;
  cursor: pointer;
}
.table-panel {
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: 8px;
  padding: 16px;
}
.status-dot {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  &.normal { background: rgba(70, 191, 145, 0.2); color: #46BF91; }
  &.frozen { background: rgba(255, 255, 255, 0.1); color: $text-muted; }
}
</style>
