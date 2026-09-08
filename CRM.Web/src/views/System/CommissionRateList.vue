<template>
  <div class="commission-rate-list">
    <div class="section-head">
      <div class="section-head__left">
        <div class="section-title"><span class="title-bar"></span>{{ sectionTitle }}</div>
        <p class="section-hint">{{ t('commissionParams.listHint') }}</p>
        <div class="version-toolbar">
          <span class="version-label">{{ t('commissionParams.editVersion') }}</span>
          <el-select
            :model-value="selectedVersionId"
            class="version-select"
            :filterable="false"
            @change="onVersionChange"
          >
            <el-option
              v-for="v in versions"
              :key="v.id"
              :label="formatCommissionVersionLabel(v, t('commissionParams.activeTag'))"
              :value="v.id"
            />
          </el-select>
        </div>
      </div>
    </div>

    <div class="table-wrapper" v-loading="loading">
      <CrmDataTable
        v-show="loading || rows.length > 0"
        ref="dataTableRef"
        :column-layout-key="`system-commission-rate-${roleType}-v3`"
        :columns="tableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="rows"
        row-key="id"
        @row-dblclick="onRowDblclick"
      >
        <template #col-levelDesc="{ row }">{{ levelDesc(row.userLevel) }}</template>
        <template #col-remark="{ row }">{{ row.remark || '—' }}</template>
        <template #col-actions-header>
          <div class="list-op-col-header--icon-only">
            <button
              type="button"
              class="op-col-toggle-btn list-op-col-toggle"
              :aria-label="opColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
              @click.stop="toggleOpCol"
            >
              {{ opColExpanded ? '>' : '<' }}
            </button>
          </div>
        </template>
        <template #col-actions="{ row }">
          <div @click.stop @dblclick.stop>
            <div v-if="opColExpanded" class="action-btns">
              <el-button v-if="canWrite" link type="primary" @click.stop="openEdit(row)">
                {{ t('commissionParams.edit') }}
              </el-button>
            </div>
            <el-dropdown v-else-if="canWrite" trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="openEdit(row)">
                    <span class="op-more-item op-more-item--primary">{{ t('commissionParams.edit') }}</span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
      </CrmDataTable>
    </div>

    <div class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('systemUser.colSetting')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true" />
      </div>
    </div>

    <el-dialog
      v-model="editVisible"
      :title="editTitle"
      width="640px"
      destroy-on-close
      @closed="editRow = null"
    >
      <p class="edit-hint">{{ t('commissionParams.editHint') }}</p>
      <el-form v-if="editRow" label-width="96px">
        <el-form-item :label="t('commissionParams.colLevel')">
          <span>{{ editRow.userLevel }}{{ levelDesc(editRow.userLevel) !== '—' ? ` · ${levelDesc(editRow.userLevel)}` : '' }}</span>
        </el-form-item>
        <div class="ladder-grid">
          <div class="ladder-head">
            <span>{{ t('commissionParams.ladderIndex') }}</span>
            <span>{{ t('commissionParams.colThreshold') }}</span>
            <span>{{ t('commissionParams.colPoints') }}</span>
          </div>
          <div v-for="i in COMMISSION_LADDER_COUNT" :key="i" class="ladder-row">
            <span class="ladder-idx">{{ t('commissionParams.ladderN', { n: i }) }}</span>
            <el-input-number
              v-model="editForm.ladders[i - 1].thresholdAmount"
              :precision="0"
              :step="1"
              :min="0"
              :controls="false"
              class="ladder-input"
            />
            <el-input-number
              v-model="editForm.ladders[i - 1].ratePoints"
              :precision="1"
              :step="0.1"
              :min="0"
              :max="100"
              :controls="false"
              class="ladder-input"
            />
          </div>
        </div>
        <el-form-item :label="t('commissionParams.colRemark')" class="remark-item">
          <el-input
            v-model="editForm.remark"
            type="textarea"
            :rows="2"
            maxlength="200"
            show-word-limit
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="saving" @click="submitEdit">{{ t('common.confirm') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import CrmDataTable from '@/components/CrmDataTable.vue'
import { useAuthStore } from '@/stores'
import {
  COMMISSION_LADDER_COUNT,
  COMMISSION_ROLE_PURCHASE,
  commissionRatesApi,
  formatCommissionVersionLabel,
  formatLadderCell,
  type CommissionRateRow,
  type CommissionRateVersion
} from '@/api/commissionRates'
import { userLevelApi, type UserLevelDefinition } from '@/api/userLevel'
import { validateCommissionLadders, type CommissionLadderDraftSlot } from '@/utils/commissionLadderRules'
import { estimateListColumnHeaderMinWidth } from '@/utils/listColumnHeaderWidth'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const props = defineProps<{ roleType: number }>()
const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const versions = ref<CommissionRateVersion[]>([])
const selectedVersionId = ref('')

const canWrite = computed(() => authStore.canForceDelete())

const sectionTitle = computed(() =>
  props.roleType === COMMISSION_ROLE_PURCHASE
    ? t('commissionParams.purchaseNav')
    : t('commissionParams.salesNav')
)

const loading = ref(false)
const saving = ref(false)
const rows = ref<CommissionRateRow[]>([])
const levelDefs = ref<UserLevelDefinition[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 88
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

function headerMin(label: string) {
  return estimateListColumnHeaderMinWidth(label)
}

const tableColumns = computed<CrmTableColumnDef[]>(() => {
  const level = t('commissionParams.colLevel')
  const levelDescLabel = t('commissionParams.colLevelDesc')
  const remark = t('commissionParams.colRemark')
  const ladderCols: CrmTableColumnDef[] = Array.from({ length: COMMISSION_LADDER_COUNT }, (_, i) => {
    const label = t('commissionParams.colLadder', { n: i + 1 })
    return {
      key: `ladder${i + 1}`,
      label,
      minWidth: Math.max(148, headerMin(label)),
      align: 'center',
      showOverflowTooltip: true,
      formatter: (row: unknown) => {
        const r = row as CommissionRateRow
        return formatLadderCell(r.thresholds?.[i], r.ratePoints?.[i])
      }
    }
  })
  return [
    { key: 'userLevel', label: level, prop: 'userLevel', width: Math.max(88, headerMin(level)), align: 'center' },
    { key: 'levelDesc', label: levelDescLabel, minWidth: Math.max(140, headerMin(levelDescLabel)), showOverflowTooltip: true },
    ...ladderCols,
    { key: 'remark', label: remark, minWidth: Math.max(140, headerMin(remark)), showOverflowTooltip: true },
    {
      key: 'actions',
      label: t('commissionParams.colActions'),
      width: opColWidth.value,
      minWidth: opColWidth.value,
      fixed: 'right',
      hideable: false,
      pinned: 'end',
      reorderable: false,
      className: 'op-col',
      labelClassName: 'op-col',
      resizable: false
    }
  ]
})

const editVisible = ref(false)
const editRow = ref<CommissionRateRow | null>(null)
const editForm = reactive({
  remark: '',
  ladders: emptyLadders()
})

const editTitle = computed(() =>
  t('commissionParams.editTitle', {
    kind: sectionTitle.value,
    n: editRow.value?.userLevel ?? ''
  })
)

function emptyLadders(): CommissionLadderDraftSlot[] {
  return Array.from({ length: COMMISSION_LADDER_COUNT }, () => ({
    thresholdAmount: null,
    ratePoints: null
  }))
}

function openEdit(row: CommissionRateRow) {
  if (!canWrite.value) return
  editRow.value = row
  editForm.remark = row.remark || ''
  for (let i = 0; i < COMMISSION_LADDER_COUNT; i++) {
    editForm.ladders[i].thresholdAmount = row.thresholds?.[i] ?? null
    editForm.ladders[i].ratePoints = row.ratePoints?.[i] ?? null
  }
  editVisible.value = true
}

function onRowDblclick(row: CommissionRateRow) {
  openEdit(row)
}

async function submitEdit() {
  if (!editRow.value) return
  const err = validateCommissionLadders(editForm.ladders)
  if (err) {
    ElMessage.error(err)
    return
  }
  saving.value = true
  try {
    const updated = await commissionRatesApi.update(editRow.value.id, {
      remark: editForm.remark,
      ladders: editForm.ladders.map((s) => ({
        thresholdAmount: s.thresholdAmount,
        ratePoints: s.ratePoints
      }))
    })
    ElMessage.success(t('commissionParams.saveSuccess'))
    editVisible.value = false
    const idx = rows.value.findIndex((r) => r.id === updated.id)
    if (idx >= 0) rows.value[idx] = updated
    else await load()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('commissionParams.saveFailed'))
  } finally {
    saving.value = false
  }
}

function levelDesc(level: number) {
  const desc = levelDefs.value.find((d) => d.userLevel === level)?.description?.trim()
  return desc || '—'
}

function onVersionChange(id: string) {
  selectedVersionId.value = id
  void router.replace({ query: { ...route.query, versionId: id } })
}

async function load() {
  loading.value = true
  try {
    const [versionList, defs] = await Promise.all([
      commissionRatesApi.listVersions(props.roleType),
      userLevelApi.listDefinitions().catch(() => [] as UserLevelDefinition[])
    ])
    versions.value = versionList
    levelDefs.value = defs
    const q = typeof route.query.versionId === 'string' ? route.query.versionId : ''
    selectedVersionId.value =
      (q && versionList.some((v) => v.id === q) ? q : '') ||
      versionList.find((v) => v.isActive)?.id ||
      versionList[0]?.id ||
      ''
    rows.value = selectedVersionId.value
      ? await commissionRatesApi.list(props.roleType, selectedVersionId.value)
      : []
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('commissionParams.loadFailed'))
  } finally {
    loading.value = false
  }
}

watch(
  () => [props.roleType, route.query.versionId] as const,
  () => {
    void load()
  },
  { immediate: true }
)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.commission-rate-list {
  min-height: 100%;
}

.section-head {
  margin-bottom: 14px;
}

.section-title {
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
  display: flex;
  align-items: center;
  gap: 8px;
}

.title-bar {
  display: inline-block;
  width: 3px;
  height: 14px;
  border-radius: 2px;
  background: $cyan-primary;
}

.section-hint {
  margin: 6px 0 0;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.5;
}

.version-toolbar {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-top: 10px;
}

.version-label {
  font-size: 13px;
  color: $text-secondary;
}

.version-select {
  width: 320px;
}

.table-wrapper {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: 8px;
  padding: 8px;
}

.pagination-wrapper {
  display: flex;
  align-items: center;
  margin-top: 8px;
}

.list-footer-left {
  display: flex;
  align-items: center;
  gap: 8px;
}

.list-footer-spacer {
  flex: 1;
}

.action-btns {
  display: flex;
  justify-content: flex-end;
}

.edit-hint {
  margin: 0 0 12px;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.5;
}

.ladder-grid {
  margin: 0 0 16px 12px;
}

.ladder-head,
.ladder-row {
  display: grid;
  grid-template-columns: 72px 1fr 1fr;
  gap: 10px;
  align-items: center;
  margin-bottom: 8px;
}

.ladder-head {
  font-size: 12px;
  color: $text-muted;
}

.ladder-idx {
  font-size: 13px;
}

.ladder-input {
  width: 100%;
}

.remark-item {
  margin-top: 8px;
}

.op-more-trigger {
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
  color: $cyan-primary;
  font-size: 16px;
  line-height: 1;
}

.op-more-item--primary {
  color: $cyan-primary;
}
</style>
