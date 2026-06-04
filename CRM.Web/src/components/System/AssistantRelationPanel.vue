<template>
  <div class="relation-panel">
    <div class="relation-split">
      <!-- 助理列表 -->
      <div class="relation-pane relation-pane--left">
        <div class="pane-toolbar">
          <span class="pane-title">{{ assistantTitle }}</span>
          <el-select
            v-model="leftStatusFilter"
            class="filter-select"
            :placeholder="t('userConfig.filterStatusAll')"
          >
            <el-option :label="t('userConfig.filterStatusAll')" value="all" />
            <el-option :label="t('userConfig.statusEnabled')" value="1" />
            <el-option :label="t('userConfig.statusFrozen')" value="2" />
            <el-option :label="t('userConfig.statusDisabled')" value="0" />
          </el-select>
        </div>
        <el-table
          :data="filteredAssistants"
          border
          stripe
          class="relation-table"
          :row-key="(row: AdminUserDto) => row.id"
          :row-class-name="assistantRowClassName"
          empty-text="—"
          @row-click="(row: AdminUserDto) => void onAssistantRowChange(row)"
        >
          <el-table-column prop="userName" :label="t('userConfig.colUserName')" min-width="120">
            <template #default="{ row }">
              <span :class="assistantSelectedTextClass(row)">{{ row.userName }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="realName" :label="t('userConfig.colRealName')" min-width="100">
            <template #default="{ row }">
              <span :class="assistantSelectedTextClass(row)">{{ row.realName || '—' }}</span>
            </template>
          </el-table-column>
          <el-table-column :label="t('userConfig.colDepartment')" min-width="120" show-overflow-tooltip>
            <template #default="{ row }">
              <span :class="assistantSelectedTextClass(row)">{{ deptLabel(row) }}</span>
            </template>
          </el-table-column>
          <el-table-column :label="t('userConfig.colAccountStatus')" width="88" align="center">
            <template #default="{ row }">
              <span :class="statusClass(row.status)">{{ statusText(row.status) }}</span>
            </template>
          </el-table-column>
        </el-table>
      </div>

      <!-- 目标员工 + 勾选 -->
      <div class="relation-pane relation-pane--right">
        <div class="pane-toolbar">
          <div class="pane-title-group">
            <span class="pane-title">{{ targetTitle }}</span>
            <span v-if="selectedAssistantId" class="pane-selected-count">
              {{ t('userConfig.selectedCount', { count: checkedDestCount }) }}
            </span>
          </div>
          <div class="pane-filters">
            <el-select
              v-model="rightCheckFilter"
              class="filter-select"
              :placeholder="t('userConfig.filterCheckAll')"
              :disabled="!selectedAssistantId"
            >
              <el-option :label="t('userConfig.filterCheckAll')" value="all" />
              <el-option :label="t('userConfig.filterChecked')" value="checked" />
              <el-option :label="t('userConfig.filterUnchecked')" value="unchecked" />
            </el-select>
            <el-select
              v-model="rightStatusFilter"
              class="filter-select"
              :placeholder="t('userConfig.filterStatusAll')"
              :disabled="!selectedAssistantId"
            >
              <el-option :label="t('userConfig.filterStatusAll')" value="all" />
              <el-option :label="t('userConfig.statusEnabled')" value="1" />
              <el-option :label="t('userConfig.statusFrozen')" value="2" />
              <el-option :label="t('userConfig.statusDisabled')" value="0" />
            </el-select>
          </div>
        </div>
        <el-table
          :data="filteredTargets"
          border
          stripe
          class="relation-table"
          empty-text="—"
        >
          <el-table-column width="48" align="center">
            <template #header>
              <el-checkbox
                :model-value="isAllTargetsChecked"
                :indeterminate="isTargetsIndeterminate"
                :disabled="!selectedAssistantId || filteredTargets.length === 0"
                @change="(v: boolean) => toggleAllTargets(v)"
              />
            </template>
            <template #default="{ row }">
              <el-checkbox
                :model-value="checkedDestIds.has(row.id)"
                :disabled="!selectedAssistantId"
                @change="(v: boolean) => toggleTarget(row.id, v)"
              />
            </template>
          </el-table-column>
          <el-table-column prop="userName" :label="t('userConfig.colUserName')" min-width="120">
            <template #default="{ row }">
              <span :class="targetCheckedTextClass(row)">{{ row.userName }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="realName" :label="t('userConfig.colRealName')" min-width="100">
            <template #default="{ row }">
              <span :class="targetCheckedTextClass(row)">{{ row.realName || '—' }}</span>
            </template>
          </el-table-column>
          <el-table-column :label="t('userConfig.colDepartment')" min-width="120" show-overflow-tooltip>
            <template #default="{ row }">
              <span :class="targetCheckedTextClass(row)">{{ deptLabel(row, targetDeptIds) }}</span>
            </template>
          </el-table-column>
          <el-table-column :label="t('userConfig.colAccountStatus')" width="88" align="center">
            <template #default="{ row }">
              <span :class="statusClass(row.status)">{{ statusText(row.status) }}</span>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </div>

    <div class="relation-actions">
      <span v-if="selectedAssistantId" class="dirty-hint">
        <template v-if="isDirty">{{ t('userConfig.unsavedHint') }}</template>
        <template v-else>{{ t('userConfig.savedHint') }}</template>
      </span>
      <el-button
        type="primary"
        :loading="saving"
        :disabled="!selectedAssistantId || !isDirty"
        @click="save"
      >
        {{ t('userConfig.saveBtn') }}
      </el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { AdminUserDto } from '@/api/rbacAdmin'
import { sysRelationMapApi } from '@/api/sysRelationMap'
import { accountStatusLabel, sortByAccountStatusThenUserName } from '@/utils/staffListSort'

const props = defineProps<{
  relationType: number
  assistantTitle: string
  targetTitle: string
  assistants: AdminUserDto[]
  targets: AdminUserDto[]
  assistantDeptIds: Set<string>
  targetDeptIds: Set<string>
  deptNameById: Map<string, string>
}>()

const { t } = useI18n()

const leftStatusFilter = ref<'all' | '0' | '1' | '2'>('all')
const rightStatusFilter = ref<'all' | '0' | '1' | '2'>('all')
const rightCheckFilter = ref<'all' | 'checked' | 'unchecked'>('all')

const selectedAssistantId = ref<string | null>(null)
const checkedDestIds = ref<Set<string>>(new Set())
const savedDestIds = ref<Set<string>>(new Set())
const saving = ref(false)
const loadingMappings = ref(false)

const statusLabels = computed(() => ({
  enabled: t('userConfig.statusEnabled'),
  frozen: t('userConfig.statusFrozen'),
  disabled: t('userConfig.statusDisabled')
}))

const sortedAssistants = computed(() => sortByAccountStatusThenUserName(props.assistants))
const sortedTargets = computed(() => sortByAccountStatusThenUserName(props.targets))

function matchesStatus(status: number, filter: string): boolean {
  if (filter === 'all') return true
  return String(status) === filter
}

const filteredAssistants = computed(() =>
  sortedAssistants.value.filter((u) => matchesStatus(u.status, leftStatusFilter.value))
)

const filteredTargets = computed(() => {
  if (!selectedAssistantId.value) return []
  return sortedTargets.value.filter((u) => {
    if (!matchesStatus(u.status, rightStatusFilter.value)) return false
    if (rightCheckFilter.value === 'checked') return checkedDestIds.value.has(u.id)
    if (rightCheckFilter.value === 'unchecked') return !checkedDestIds.value.has(u.id)
    return true
  })
})

const checkedDestCount = computed(() => checkedDestIds.value.size)

const isDirty = computed(() => {
  if (!selectedAssistantId.value) return false
  const a = checkedDestIds.value
  const b = savedDestIds.value
  if (a.size !== b.size) return true
  for (const id of a) {
    if (!b.has(id)) return true
  }
  return false
})

const isAllTargetsChecked = computed(() => {
  const rows = filteredTargets.value
  if (rows.length === 0) return false
  return rows.every((r) => checkedDestIds.value.has(r.id))
})

const isTargetsIndeterminate = computed(() => {
  const rows = filteredTargets.value
  if (rows.length === 0) return false
  const n = rows.filter((r) => checkedDestIds.value.has(r.id)).length
  return n > 0 && n < rows.length
})

function deptLabel(user: AdminUserDto, relevantDeptIds: Set<string> = props.assistantDeptIds): string {
  const primary = user.primaryDepartmentId
  if (primary && relevantDeptIds.has(primary)) {
    return user.primaryDepartmentName || props.deptNameById.get(primary) || '—'
  }
  const matchId = (user.departmentIds ?? []).find((id) => relevantDeptIds.has(id))
  if (matchId) return props.deptNameById.get(matchId) ?? '—'
  return user.primaryDepartmentName || '—'
}

function statusText(status: number): string {
  return accountStatusLabel(status, statusLabels.value)
}

function statusClass(status: number): string {
  if (status === 1) return 'status-on'
  if (status === 2) return 'status-warn'
  return 'status-off'
}

function assistantRowClassName({ row }: { row: AdminUserDto }): string {
  return row.id === selectedAssistantId.value ? 'row-selected' : ''
}

function assistantSelectedTextClass(row: AdminUserDto): string {
  return row.id === selectedAssistantId.value ? 'relation-text-highlight' : ''
}

function targetCheckedTextClass(row: AdminUserDto): string {
  return checkedDestIds.value.has(row.id) ? 'relation-text-highlight' : ''
}

async function loadMappingsForAssistant(assistantId: string) {
  loadingMappings.value = true
  try {
    const ids = await sysRelationMapApi.getDestinations(props.relationType, assistantId)
    const set = new Set(ids)
    checkedDestIds.value = new Set(set)
    savedDestIds.value = new Set(set)
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : String(e)
    ElMessage.error(msg || t('userConfig.loadMappingsFailed'))
    checkedDestIds.value = new Set()
    savedDestIds.value = new Set()
  } finally {
    loadingMappings.value = false
  }
}

async function onAssistantRowChange(row: AdminUserDto | null | undefined) {
  if (!row) {
    selectedAssistantId.value = null
    checkedDestIds.value = new Set()
    savedDestIds.value = new Set()
    return
  }
  if (row.id === selectedAssistantId.value) return

  if (isDirty.value) {
    try {
      await ElMessageBox.confirm(t('userConfig.discardConfirm'), t('userConfig.discardTitle'), {
        type: 'warning',
        confirmButtonText: t('userConfig.discardConfirmBtn'),
        cancelButtonText: t('userConfig.discardCancelBtn')
      })
    } catch {
      return
    }
  }

  selectedAssistantId.value = row.id
  rightCheckFilter.value = 'all'
  rightStatusFilter.value = 'all'
  await loadMappingsForAssistant(row.id)
}

function toggleTarget(userId: string, checked: boolean) {
  const next = new Set(checkedDestIds.value)
  if (checked) next.add(userId)
  else next.delete(userId)
  checkedDestIds.value = next
}

function toggleAllTargets(checked: boolean) {
  const next = new Set(checkedDestIds.value)
  for (const row of filteredTargets.value) {
    if (checked) next.add(row.id)
    else next.delete(row.id)
  }
  checkedDestIds.value = next
}

async function save() {
  const src = selectedAssistantId.value
  if (!src || !isDirty.value) return

  const toAdd: string[] = []
  const toRemove: string[] = []
  for (const id of checkedDestIds.value) {
    if (!savedDestIds.value.has(id)) toAdd.push(id)
  }
  for (const id of savedDestIds.value) {
    if (!checkedDestIds.value.has(id)) toRemove.push(id)
  }

  saving.value = true
  try {
    await sysRelationMapApi.saveBatch({
      type: props.relationType,
      objSrc: src,
      addDestIds: toAdd,
      removeDestIds: toRemove
    })
    savedDestIds.value = new Set(checkedDestIds.value)
    ElMessage.success(t('userConfig.saveSuccess'))
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : String(e)
    ElMessage.error(msg || t('userConfig.saveFailed'))
  } finally {
    saving.value = false
  }
}

watch(
  () => [props.assistants, props.targets] as const,
  () => {
    if (
      selectedAssistantId.value &&
      !props.assistants.some((u) => u.id === selectedAssistantId.value)
    ) {
      selectedAssistantId.value = null
      checkedDestIds.value = new Set()
      savedDestIds.value = new Set()
    }
  }
)

// 暴露给父级刷新后可选中首行
defineExpose({ resetSelection: () => {
  selectedAssistantId.value = null
  checkedDestIds.value = new Set()
  savedDestIds.value = new Set()
} })
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.relation-panel {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.relation-split {
  display: flex;
  gap: 12px;
  align-items: stretch;
  min-height: 420px;
}

.relation-pane {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.pane-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  flex-wrap: wrap;
}

.pane-title-group {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.pane-title {
  font-size: 13px;
  font-weight: 600;
  color: $text-secondary;
}

.pane-selected-count {
  font-size: 13px;
  font-weight: 500;
  color: $cyan-primary;
}

.pane-filters {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.filter-select {
  width: 120px;
}

.relation-table {
  flex: 1;
  width: 100%;
}

.relation-actions {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 16px;
  padding-top: 4px;
  border-top: 1px solid $border-card;
}

.dirty-hint {
  flex: 1;
  font-size: 12px;
  color: $text-muted;
}

.status-on {
  color: $color-mint-green;
  font-size: 12px;
}

.status-warn {
  color: #e6a23c;
  font-size: 12px;
}

.status-off {
  color: $text-muted;
  font-size: 12px;
}

:deep(.row-selected > td) {
  background-color: rgba(0, 212, 255, 0.12) !important;
}

:deep(.relation-table .el-table__row) {
  cursor: pointer;
}

.relation-text-highlight {
  color: $cyan-primary;
}
</style>
