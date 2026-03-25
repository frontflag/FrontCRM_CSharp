<template>
  <div class="inventory-check-page">
    <div class="page-header">
      <h2>库存盘点</h2>
      <div class="actions">
        <el-input v-model="createForm.planMonth" placeholder="yyyy-MM" style="width: 120px" />
        <el-input v-model="createForm.warehouseId" placeholder="仓库ID" style="width: 140px" />
        <button class="btn-primary" @click="createPlan">创建月度盘点</button>
      </div>
    </div>

    <CrmDataTable :data="plans" v-loading="loading">
      <el-table-column prop="planMonth" label="盘点月份" width="120" />
      <el-table-column prop="warehouseId" label="仓库ID" width="140" />
      <el-table-column prop="status" label="状态" width="100">
        <template #default="{ row }">{{ statusText(row.status) }}</template>
      </el-table-column>
      <el-table-column prop="remark" label="备注" min-width="180" />
      <el-table-column prop="createTime" label="创建时间" width="170">
        <template #default="{ row }">{{ formatTime(row.createTime) }}</template>
      </el-table-column>
      <el-table-column label="操作" width="220">
        <template #default="{ row }">
          <button class="action-btn" @click="openSubmit(row.id)">提交盘点结果</button>
        </template>
      </el-table-column>
    </CrmDataTable>

    <el-dialog v-model="submitVisible" title="提交盘点结果" width="760px">
      <el-table :data="submitItems">
        <el-table-column prop="materialId" label="物料ID" width="180">
          <template #default="{ row }"><el-input v-model="row.materialId" /></template>
        </el-table-column>
        <el-table-column prop="locationId" label="库位ID" width="140">
          <template #default="{ row }"><el-input v-model="row.locationId" /></template>
        </el-table-column>
        <el-table-column prop="countQty" label="实盘数量" width="120">
          <template #default="{ row }"><el-input-number v-model="row.countQty" :min="0" /></template>
        </el-table-column>
        <el-table-column prop="countAmount" label="实盘金额" width="140">
          <template #default="{ row }"><el-input-number v-model="row.countAmount" :min="0" :step="0.01" /></template>
        </el-table-column>
        <el-table-column label="操作" width="90">
          <template #default="{ $index }"><button class="action-btn" @click="removeRow($index)">删除</button></template>
        </el-table-column>
      </el-table>
      <div class="submit-footer">
        <button class="btn-secondary" @click="addRow">新增一行</button>
        <button class="btn-primary" @click="submitPlan">提交</button>
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { inventoryCenterApi, type CountPlan } from '@/api/inventoryCenter'
import { formatDisplayDateTime } from '@/utils/displayDateTime'

const loading = ref(false)
const plans = ref<CountPlan[]>([])
const submitVisible = ref(false)
const currentPlanId = ref('')
const submitItems = ref<Array<{ materialId: string; locationId?: string; countQty: number; countAmount: number }>>([])
const createForm = ref({ planMonth: new Date().toISOString().slice(0, 7), warehouseId: '', creatorId: 'SYSTEM' })

const statusText = (s: number) => ({ 1: '草稿', 10: '盘点中', 100: '已完成', [-1]: '已取消' }[s] || '未知')
const formatTime = (v?: string) => formatDisplayDateTime(v)

const loadPlans = async () => {
  loading.value = true
  try {
    plans.value = await inventoryCenterApi.getCountPlans()
  } finally {
    loading.value = false
  }
}

const createPlan = async () => {
  if (!createForm.value.planMonth || !createForm.value.warehouseId) {
    ElMessage.warning('请填写盘点月份和仓库ID')
    return
  }
  try {
    await inventoryCenterApi.createCountPlan(createForm.value)
    ElMessage.success('盘点计划创建成功')
    await loadPlans()
  } catch (e) {
    console.error(e)
    ElMessage.error('创建盘点计划失败')
  }
}

const openSubmit = (planId: string) => {
  currentPlanId.value = planId
  submitItems.value = [{ materialId: '', locationId: '', countQty: 0, countAmount: 0 }]
  submitVisible.value = true
}

const addRow = () => submitItems.value.push({ materialId: '', locationId: '', countQty: 0, countAmount: 0 })
const removeRow = (idx: number) => submitItems.value.splice(idx, 1)

const submitPlan = async () => {
  if (!currentPlanId.value) return
  const valid = submitItems.value.filter(x => x.materialId && x.countQty >= 0)
  if (!valid.length) {
    ElMessage.warning('请填写至少一条有效盘点明细')
    return
  }
  try {
    await inventoryCenterApi.submitCountPlan({
      planId: currentPlanId.value,
      submitterId: 'SYSTEM',
      items: valid
    })
    ElMessage.success('盘点提交成功')
    submitVisible.value = false
    await loadPlans()
  } catch (e) {
    console.error(e)
    ElMessage.error('盘点提交失败')
  }
}

onMounted(loadPlans)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
.inventory-check-page {
  padding: 24px;
  .page-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 14px;
  }
  h2 {
    margin: 0;
    color: $text-primary;
    font-size: 20px;
  }
  .actions {
    display: flex;
    align-items: center;
    gap: 8px;
  }
}
.btn-primary,
.btn-secondary {
  display: inline-flex;
  align-items: center;
  padding: 8px 12px;
  border-radius: 8px;
  border: 1px solid transparent;
  cursor: pointer;
}
.btn-primary {
  color: #fff;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border-color: rgba(0, 212, 255, 0.4);
}
.btn-secondary {
  color: $text-secondary;
  background: rgba(255, 255, 255, 0.05);
  border-color: $border-panel;
}
.action-btn {
  background: transparent;
  color: $cyan-primary;
  border: none;
  cursor: pointer;
}
.submit-footer {
  margin-top: 12px;
  display: flex;
  justify-content: space-between;
}
</style>
