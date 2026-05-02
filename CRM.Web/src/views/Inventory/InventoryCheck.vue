<template>
  <div class="inventory-check-page">
    <div class="page-header">
      <h2>{{ t('inventoryCheck.title') }}</h2>
      <div class="actions">
        <el-input v-model="createForm.planMonth" :placeholder="t('inventoryCheck.filters.planMonthPlaceholder')" style="width: 120px" />
        <el-input v-model="createForm.warehouseId" :placeholder="t('inventoryCheck.filters.warehouseId')" style="width: 140px" />
        <button class="btn-primary" @click="createPlan">{{ t('inventoryCheck.actions.createPlan') }}</button>
      </div>
    </div>

    <CrmDataTable :data="plans" v-loading="loading">
      <el-table-column prop="planMonth" :label="t('inventoryCheck.columns.planMonth')" width="120" />
      <el-table-column prop="warehouseId" :label="t('inventoryCheck.columns.warehouseId')" width="140" />
      <el-table-column prop="status" :label="t('inventoryCheck.columns.status')" width="100">
        <template #default="{ row }">{{ statusText(row.status) }}</template>
      </el-table-column>
      <el-table-column prop="remark" :label="t('inventoryCheck.columns.remark')" min-width="180" />
      <el-table-column prop="createTime" :label="t('inventoryCheck.columns.createTime')" width="170">
        <template #default="{ row }">{{ formatTime(row.createTime) }}</template>
      </el-table-column>
      <el-table-column
        :label="t('inventoryCheck.columns.actions')"
        :width="checkPlansOpColWidth"
        :min-width="checkPlansOpColMinWidth"
        class-name="op-col"
        label-class-name="op-col"
        align="center"
      >
        <template #header>
          <div class="list-op-col-header--icon-only">
            <button
              type="button"
              class="op-col-toggle-btn list-op-col-toggle"
              :aria-label="checkPlansOpColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
              @click.stop="toggleCheckPlansOpCol"
            >
              {{ checkPlansOpColExpanded ? '>' : '<' }}
            </button>
          </div>
        </template>
        <template #default="{ row }">
          <div @click.stop @dblclick.stop>
            <div v-if="checkPlansOpColExpanded" class="action-btns">
              <button type="button" class="action-btn action-btn--warning" @click.stop="openSubmit(row.id)">{{ t('inventoryCheck.actions.submitResult') }}</button>
            </div>
            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="openSubmit(row.id)">
                    <span class="op-more-item op-more-item--warning">{{ t('inventoryCheck.actions.submitResult') }}</span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
      </el-table-column>
    </CrmDataTable>

    <el-dialog v-model="submitVisible" :title="t('inventoryCheck.actions.submitResult')" width="760px">
      <el-table :data="submitItems">
        <el-table-column prop="materialId" :label="t('inventoryCheck.columns.materialId')" width="180">
          <template #default="{ row }"><el-input v-model="row.materialId" /></template>
        </el-table-column>
        <el-table-column prop="locationId" :label="t('inventoryCheck.columns.locationId')" width="140">
          <template #default="{ row }"><el-input v-model="row.locationId" /></template>
        </el-table-column>
        <el-table-column prop="countQty" :label="t('inventoryCheck.columns.countQty')" width="120">
          <template #default="{ row }"><el-input-number v-model="row.countQty" :min="0" /></template>
        </el-table-column>
        <el-table-column prop="countAmount" :label="t('inventoryCheck.columns.countAmount')" width="140">
          <template #default="{ row }"><el-input-number v-model="row.countAmount" :min="0" :step="0.01" /></template>
        </el-table-column>
        <el-table-column
          :label="t('inventoryCheck.columns.actions')"
          :width="checkDialogOpColWidth"
          :min-width="checkDialogOpColMinWidth"
          align="center"
          class-name="op-col"
          label-class-name="op-col"
        >
          <template #header>
            <div class="list-op-col-header--icon-only">
            <button
              type="button"
              class="op-col-toggle-btn list-op-col-toggle"
              :aria-label="checkDialogOpColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
              @click.stop="toggleCheckDialogOpCol"
            >
              {{ checkDialogOpColExpanded ? '>' : '<' }}
            </button>
          </div>
          </template>
          <template #default="{ $index }">
            <div @click.stop @dblclick.stop>
              <div v-if="checkDialogOpColExpanded" class="action-btns">
                <button type="button" class="action-btn action-btn--danger" @click.stop="removeRow($index)">{{ t('inventoryCheck.actions.delete') }}</button>
              </div>
              <el-dropdown v-else trigger="click" placement="bottom-end">
                <div class="op-more-dropdown-trigger">
                  <button type="button" class="op-more-trigger">...</button>
                </div>
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item @click.stop="removeRow($index)">
                      <span class="op-more-item op-more-item--danger">{{ t('inventoryCheck.actions.delete') }}</span>
                    </el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>
            </div>
          </template>
        </el-table-column>
      </el-table>
      <div class="submit-footer">
        <button class="btn-secondary" @click="addRow">{{ t('inventoryCheck.actions.addRow') }}</button>
        <button class="btn-primary" @click="submitPlan">{{ t('inventoryCheck.actions.submit') }}</button>
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { inventoryCenterApi, type CountPlan } from '@/api/inventoryCenter'
import { formatDisplayDateTime } from '@/utils/displayDateTime'

const loading = ref(false)
const { t } = useI18n()
/** 《列表操作列规范》 */
const CHECK_OP_COL_COLLAPSED = 43
const CHECK_OP_COL_EXPANDED = 173
const CHECK_OP_COL_EXPANDED_MIN = 160
const checkPlansOpColExpanded = ref(false)
const checkPlansOpColWidth = computed(() =>
  checkPlansOpColExpanded.value ? CHECK_OP_COL_EXPANDED : CHECK_OP_COL_COLLAPSED
)
const checkPlansOpColMinWidth = computed(() =>
  checkPlansOpColExpanded.value ? CHECK_OP_COL_EXPANDED_MIN : CHECK_OP_COL_COLLAPSED
)
function toggleCheckPlansOpCol() {
  checkPlansOpColExpanded.value = !checkPlansOpColExpanded.value
}
const checkDialogOpColExpanded = ref(false)
const checkDialogOpColWidth = computed(() =>
  checkDialogOpColExpanded.value ? CHECK_OP_COL_EXPANDED : CHECK_OP_COL_COLLAPSED
)
const checkDialogOpColMinWidth = computed(() =>
  checkDialogOpColExpanded.value ? CHECK_OP_COL_EXPANDED_MIN : CHECK_OP_COL_COLLAPSED
)
function toggleCheckDialogOpCol() {
  checkDialogOpColExpanded.value = !checkDialogOpColExpanded.value
}
const plans = ref<CountPlan[]>([])
const submitVisible = ref(false)
const currentPlanId = ref('')
const submitItems = ref<Array<{ materialId: string; locationId?: string; countQty: number; countAmount: number }>>([])
const createForm = ref({ planMonth: new Date().toISOString().slice(0, 7), warehouseId: '', creatorId: 'SYSTEM' })

const statusText = (s: number) => ({
  1: t('inventoryCheck.status.draft'),
  10: t('inventoryCheck.status.counting'),
  100: t('inventoryCheck.status.completed'),
  [-1]: t('inventoryCheck.status.cancelled')
}[s] || t('rfqDetail.unknown'))
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
    ElMessage.warning(t('inventoryCheck.messages.fillMonthAndWarehouse'))
    return
  }
  try {
    await inventoryCenterApi.createCountPlan(createForm.value)
    ElMessage.success(t('inventoryCheck.messages.createSuccess'))
    await loadPlans()
  } catch (e) {
    console.error(e)
    ElMessage.error(t('inventoryCheck.messages.createFailed'))
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
    ElMessage.warning(t('inventoryCheck.messages.atLeastOneItem'))
    return
  }
  try {
    await inventoryCenterApi.submitCountPlan({
      planId: currentPlanId.value,
      submitterId: 'SYSTEM',
      items: valid
    })
    ElMessage.success(t('inventoryCheck.messages.submitSuccess'))
    submitVisible.value = false
    await loadPlans()
  } catch (e) {
    console.error(e)
    ElMessage.error(t('inventoryCheck.messages.submitFailed'))
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
