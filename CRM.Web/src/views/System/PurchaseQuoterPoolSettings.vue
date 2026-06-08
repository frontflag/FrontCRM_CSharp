<template>
  <div class="quoter-pool-settings" v-loading="loading">
    <div class="section-head">
      <div class="section-head__left">
        <div class="section-title">
          <span class="title-bar"></span>
          {{ t('purchaseParams.quoterPoolTitle') }}
          <span class="selected-badge">{{ t('purchaseParams.quoterPoolSelectedCount', { count: localSelectedCount }) }}</span>
        </div>
        <p class="section-hint">{{ t('purchaseParams.quoterPoolHint') }}</p>
      </div>
      <div class="section-head__actions">
        <el-button type="primary" :loading="saving" @click="save">{{ t('purchaseParams.saveBtn') }}</el-button>
        <el-button :loading="loading" @click="load">{{ t('purchaseParams.refreshBtn') }}</el-button>
      </div>
    </div>

    <div class="toolbar">
      <span class="filter-label">{{ t('purchaseParams.filterLabel') }}</span>
      <el-radio-group v-model="listFilter" size="small">
        <el-radio-button value="all">{{ t('purchaseParams.filterAll') }}</el-radio-button>
        <el-radio-button value="selected">{{ t('purchaseParams.filterSelected') }}</el-radio-button>
      </el-radio-group>
      <span class="filter-label filter-label--status">{{ t('purchaseParams.filterStatus') }}</span>
      <el-select
        v-model="statusFilter"
        class="filter-status"
        size="small"
        :teleported="false"
      >
        <el-option :label="t('purchaseParams.filterAll')" value="all" />
        <el-option :label="t('purchaseParams.statusActive')" value="active" />
        <el-option :label="t('purchaseParams.statusInactive')" value="inactive" />
      </el-select>
      <div class="toolbar-actions">
        <el-button size="small" @click="selectAllVisible">{{ t('purchaseParams.selectAll') }}</el-button>
        <el-button size="small" @click="invertVisible">{{ t('purchaseParams.invertSelection') }}</el-button>
        <el-button size="small" @click="clearAll">{{ t('purchaseParams.clearSelection') }}</el-button>
      </div>
    </div>

    <el-table
      :data="displayRows"
      border
      stripe
      size="small"
      class="pool-table"
      :row-class-name="rowClassName"
    >
      <el-table-column width="72" align="center" class-name="col-select">
        <template #header>
          <span class="col-check-label">{{ t('purchaseParams.colSelect') }}</span>
        </template>
        <template #default="{ row }">
          <el-checkbox
            :model-value="isSelected(row.userId)"
            @change="(val: boolean) => setSelected(row.userId, val)"
          />
        </template>
      </el-table-column>
      <el-table-column prop="userName" :label="t('purchaseParams.colUserName')" min-width="140" show-overflow-tooltip />
      <el-table-column prop="realName" :label="t('purchaseParams.colRealName')" min-width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ row.realName || '—' }}</template>
      </el-table-column>
      <el-table-column prop="departmentName" :label="t('purchaseParams.colDepartment')" min-width="160" show-overflow-tooltip>
        <template #default="{ row }">{{ row.departmentName || '—' }}</template>
      </el-table-column>
      <el-table-column prop="isActive" :label="t('purchaseParams.colStatus')" width="100" align="center">
        <template #default="{ row }">
          <el-tag :type="row.isActive ? 'success' : 'info'" size="small">
            {{ row.isActive ? t('purchaseParams.statusActive') : t('purchaseParams.statusInactive') }}
          </el-tag>
        </template>
      </el-table-column>
    </el-table>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { onBeforeRouteLeave } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { purchaseParamsApi, type PurchaseQuoterPoolMemberDto } from '@/api/purchaseParams'

const { t } = useI18n()

const loading = ref(false)
const saving = ref(false)
const listFilter = ref<'all' | 'selected'>('all')
const statusFilter = ref<'all' | 'active' | 'inactive'>('all')
const rows = ref<PurchaseQuoterPoolMemberDto[]>([])
const selectedIds = ref<Set<string>>(new Set())
const savedSelectedIds = ref<Set<string>>(new Set())

const localSelectedCount = computed(() => selectedIds.value.size)

const displayRows = computed(() => {
  let list = rows.value
  if (listFilter.value === 'selected') {
    list = list.filter((r) => selectedIds.value.has(r.userId))
  }
  if (statusFilter.value === 'active') {
    list = list.filter((r) => r.isActive)
  } else if (statusFilter.value === 'inactive') {
    list = list.filter((r) => !r.isActive)
  }
  return list
})

const isDirty = computed(() => {
  if (selectedIds.value.size !== savedSelectedIds.value.size) return true
  for (const id of selectedIds.value) {
    if (!savedSelectedIds.value.has(id)) return true
  }
  return false
})

function cloneSet(source: Set<string>) {
  return new Set(source)
}

function isSelected(userId: string) {
  return selectedIds.value.has(userId)
}

function setSelected(userId: string, checked: boolean) {
  const next = new Set(selectedIds.value)
  if (checked) next.add(userId)
  else next.delete(userId)
  selectedIds.value = next
}

function rowClassName({ row }: { row: PurchaseQuoterPoolMemberDto }) {
  return isSelected(row.userId) ? 'row-selected' : ''
}

function selectAllVisible() {
  const next = new Set(selectedIds.value)
  for (const row of displayRows.value) {
    next.add(row.userId)
  }
  selectedIds.value = next
}

function invertVisible() {
  const next = new Set(selectedIds.value)
  for (const row of displayRows.value) {
    if (next.has(row.userId)) next.delete(row.userId)
    else next.add(row.userId)
  }
  selectedIds.value = next
}

function clearAll() {
  selectedIds.value = new Set()
}

async function load() {
  loading.value = true
  try {
    const res = await purchaseParamsApi.getQuoterPool('all')
    rows.value = res.items ?? []
    const saved = new Set(
      rows.value.filter((r) => r.isSelected).map((r) => r.userId)
    )
    selectedIds.value = cloneSet(saved)
    savedSelectedIds.value = cloneSet(saved)
  } catch {
    ElMessage.error(t('purchaseParams.quoterPoolLoadFailed'))
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  try {
    const res = await purchaseParamsApi.saveQuoterPool([...selectedIds.value])
    rows.value = res.items ?? []
    const saved = new Set(
      rows.value.filter((r) => r.isSelected).map((r) => r.userId)
    )
    selectedIds.value = cloneSet(saved)
    savedSelectedIds.value = cloneSet(saved)
    ElMessage.success(t('purchaseParams.saveSuccess'))
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : t('purchaseParams.saveFailed')
    ElMessage.error(msg)
  } finally {
    saving.value = false
  }
}

onBeforeRouteLeave(async (_to, _from, next) => {
  if (!isDirty.value) {
    next()
    return
  }
  try {
    await ElMessageBox.confirm(
      t('purchaseParams.unsavedLeaveConfirm'),
      t('purchaseParams.unsavedLeaveTitle'),
      { type: 'warning', confirmButtonText: t('purchaseParams.leaveBtn'), cancelButtonText: t('purchaseParams.cancelBtn') }
    )
    next()
  } catch {
    next(false)
  }
})

onMounted(load)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.quoter-pool-settings {
  min-height: 200px;
}

.section-head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 16px;
  margin-bottom: 16px;
  &__left {
    flex: 1;
    min-width: 0;
  }
  &__actions {
    display: flex;
    gap: 8px;
    flex-shrink: 0;
  }
}

.section-title {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 6px;
}

.selected-badge {
  font-size: 12px;
  font-weight: 500;
  color: $cyan-primary;
  background: rgba(0, 212, 255, 0.08);
  padding: 2px 8px;
  border-radius: 10px;
}

.title-bar {
  width: 3px;
  height: 16px;
  background: linear-gradient(180deg, #00c8ff, #0066cc);
  border-radius: 2px;
}

.section-hint {
  margin: 0;
  font-size: 13px;
  color: $text-muted;
  line-height: 1.5;
}

.toolbar {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
  margin-bottom: 12px;
}

.filter-label {
  font-size: 13px;
  color: $text-secondary;

  &--status {
    margin-left: 4px;
  }
}

.filter-status {
  width: 120px;
}

.toolbar-actions {
  margin-left: auto;
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.pool-table {
  width: 100%;
}

:deep(.row-selected) {
  color: $cyan-primary;

  td {
    color: $cyan-primary;
  }
}

.col-check-label {
  font-size: 12px;
  white-space: nowrap;
}
</style>
