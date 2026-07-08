<template>
  <div class="purchase-cost-param-settings">
    <div class="form-section" v-loading="loading">
      <div class="section-head">
        <div class="section-head__left">
          <div class="section-title">
            <span class="title-bar"></span>{{ t('financeParams.purchaseCostParamsTitle') }}
          </div>
          <p class="section-hint">{{ t('financeParams.purchaseCostParamsHint') }}</p>
        </div>
        <div class="section-head__actions">
          <el-button type="primary" @click="openCreate">{{ t('financeParams.purchaseCostParamsAdd') }}</el-button>
          <el-button :loading="loading" @click="reloadAll">{{ t('financeParams.refreshBtn') }}</el-button>
        </div>
      </div>

      <div class="group-card group-card--effective">
        <div v-if="effective" class="effective-summary">
          <span class="effective-label">{{ t('financeParams.effectiveTag') }}</span>
          <span class="effective-ratio">{{ formatRatio(effective.ratio) }}</span>
          <span class="effective-meta">
            {{ t('financeParams.effectiveSummary', { time: formatTime(effective.startTimeUtc) }) }}
          </span>
          <span v-if="effective.remark" class="effective-remark">{{ effective.remark }}</span>
        </div>
        <p v-else class="no-effective">{{ t('financeParams.purchaseCostParamsNoEffective') }}</p>
      </div>

      <div class="group-card group-card--list">
        <el-table :data="rows" border stripe size="small" class="param-table">
          <el-table-column prop="ratio" :label="t('financeParams.colRatio')" width="120" align="right">
            <template #default="{ row }">
              {{ formatRatio(row.ratio) }}
            </template>
          </el-table-column>
          <el-table-column prop="startTimeUtc" :label="t('financeParams.colStartTime')" width="200">
            <template #default="{ row }">
              {{ formatTime(row.startTimeUtc) }}
            </template>
          </el-table-column>
          <el-table-column
            prop="remark"
            :label="t('financeParams.colRemark')"
            min-width="180"
            show-overflow-tooltip
          >
            <template #default="{ row }">
              {{ row.remark || '—' }}
            </template>
          </el-table-column>
          <el-table-column :label="t('financeParams.colStatus')" width="100" align="center">
            <template #default="{ row }">
              <el-tag v-if="effective?.id === row.id" size="small" type="success">
                {{ t('financeParams.effectiveTag') }}
              </el-tag>
              <span v-else>—</span>
            </template>
          </el-table-column>
          <el-table-column :label="t('financeParams.colActions')" width="100" align="center" fixed="right">
            <template #default="{ row }">
              <el-button link type="danger" :loading="row._deleting" @click="onDelete(row)">
                {{ t('financeParams.deleteBtn') }}
              </el-button>
            </template>
          </el-table-column>
        </el-table>
        <div class="pager">
          <el-pagination
            v-model:current-page="page"
            :page-size="pageSize"
            :total="total"
            layout="total, prev, pager, next"
            @current-change="loadList"
          />
        </div>
      </div>

      <div class="group-card group-card--log" v-loading="logLoading">
        <div class="group-card__head">
          <span class="group-card__title">{{ t('financeParams.changeLogTitle') }}</span>
        </div>
        <el-table :data="logRows" border stripe size="small" class="log-table">
          <el-table-column prop="changeTimeUtc" :label="t('financeParams.colChangeTime')" width="200">
            <template #default="{ row }">
              {{ formatTime(row.changeTimeUtc) }}
            </template>
          </el-table-column>
          <el-table-column
            prop="changeUserName"
            :label="t('financeParams.colChangeUser')"
            width="140"
            show-overflow-tooltip
          />
          <el-table-column
            prop="changeSummary"
            :label="t('financeParams.colChangeContent')"
            min-width="280"
            show-overflow-tooltip
          />
          <el-table-column prop="ratio" :label="t('financeParams.colRatio')" width="110" align="right">
            <template #default="{ row }">
              {{ formatRatio(row.ratio) }}
            </template>
          </el-table-column>
          <el-table-column prop="startTimeUtc" :label="t('financeParams.colStartTime')" width="200">
            <template #default="{ row }">
              {{ formatTime(row.startTimeUtc) }}
            </template>
          </el-table-column>
        </el-table>
        <div class="pager">
          <el-pagination
            v-model:current-page="logPage"
            :page-size="logPageSize"
            :total="logTotal"
            layout="total, prev, pager, next"
            @current-change="loadLog"
          />
        </div>
      </div>
    </div>

    <el-dialog
      v-model="dialogVisible"
      :title="t('financeParams.purchaseCostParamsCreateTitle')"
      width="520px"
      destroy-on-close
      @closed="resetCreateForm"
    >
      <el-form ref="formRef" :model="createForm" :rules="rules" label-width="120px">
        <el-form-item :label="t('financeParams.fieldRatio')" prop="ratio">
          <el-input-number
            v-model="createForm.ratio"
            :min="0.0001"
            :max="999999"
            :precision="4"
            :step="0.0001"
            controls-position="right"
            class="ratio-input"
          />
        </el-form-item>
        <el-form-item :label="t('financeParams.fieldStartTime')" prop="startTimeUtc">
          <el-date-picker
            v-model="createForm.startTimeUtc"
            type="datetime"
            value-format="x"
            format="YYYY-MM-DD HH:mm:ss"
            class="start-time-picker"
            :teleported="false"
          />
        </el-form-item>
        <el-form-item :label="t('financeParams.fieldRemark')" prop="remark">
          <el-input v-model="createForm.remark" type="textarea" :rows="3" maxlength="500" show-word-limit />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">{{ t('financeParams.cancelBtn') }}</el-button>
        <el-button type="primary" :loading="creating" @click="submitCreate">
          {{ t('financeParams.saveBtn') }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import {
  purchaseCostParamApi,
  type PurchaseCostParamDto,
  type PurchaseCostParamChangeLogDto
} from '@/api/purchaseCostParam'

type Row = PurchaseCostParamDto & { _deleting?: boolean }

const { t, locale } = useI18n()

const loading = ref(false)
const logLoading = ref(false)
const creating = ref(false)
const dialogVisible = ref(false)
const formRef = ref<FormInstance>()

const effective = ref<PurchaseCostParamDto | null>(null)
const rows = ref<Row[]>([])
const page = ref(1)
const pageSize = ref(20)
const total = ref(0)

const logRows = ref<PurchaseCostParamChangeLogDto[]>([])
const logPage = ref(1)
const logPageSize = ref(20)
const logTotal = ref(0)

const createForm = reactive({
  ratio: 1,
  startTimeUtc: '' as string | number,
  remark: ''
})

const rules: FormRules = {
  ratio: [{ required: true, message: () => t('financeParams.ruleRatio'), trigger: 'blur' }],
  startTimeUtc: [{ required: true, message: () => t('financeParams.ruleStartTime'), trigger: 'change' }]
}

function formatRatio(v: number) {
  return Number(v).toFixed(4)
}

function formatTime(iso: string) {
  try {
    const d = new Date(iso)
    if (Number.isNaN(d.getTime())) return iso
    return new Intl.DateTimeFormat(locale.value === 'zh-CN' ? 'zh-CN' : 'en-US', {
      dateStyle: 'short',
      timeStyle: 'medium'
    }).format(d)
  } catch {
    return iso
  }
}

async function loadEffective(options?: { silent?: boolean }) {
  const silent = options?.silent === true
  try {
    effective.value = await purchaseCostParamApi.getEffective()
    return true
  } catch {
    effective.value = null
    if (!silent) return false
    return false
  }
}

async function loadList(options?: { silent?: boolean }) {
  const silent = options?.silent === true
  loading.value = true
  try {
    const p = await purchaseCostParamApi.list(page.value, pageSize.value)
    rows.value = (p.items || []).map((x) => ({ ...x }))
    total.value = p.totalCount ?? 0
    return true
  } catch (e: unknown) {
    if (!silent) ElMessage.error((e as Error)?.message || t('financeParams.purchaseCostParamsLoadFailed'))
    return false
  } finally {
    loading.value = false
  }
}

async function loadLog(options?: { silent?: boolean }) {
  const silent = options?.silent === true
  logLoading.value = true
  try {
    const p = await purchaseCostParamApi.getChangeLog(logPage.value, logPageSize.value)
    logRows.value = p.items || []
    logTotal.value = p.totalCount ?? 0
    return true
  } catch (e: unknown) {
    if (!silent) ElMessage.error((e as Error)?.message || t('financeParams.logLoadFailed'))
    return false
  } finally {
    logLoading.value = false
  }
}

async function reloadAll() {
  await Promise.all([loadEffective({ silent: true }), loadList(), loadLog()])
}

function openCreate() {
  createForm.ratio = 1
  createForm.startTimeUtc = Date.now()
  createForm.remark = ''
  dialogVisible.value = true
}

function resetCreateForm() {
  formRef.value?.resetFields()
}

async function submitCreate() {
  const ok = await formRef.value?.validate().catch(() => false)
  if (!ok) return

  const startMs = Number(createForm.startTimeUtc)
  if (!Number.isFinite(startMs) || startMs <= 0) {
    ElMessage.error(t('financeParams.ruleStartTime'))
    return
  }

  creating.value = true
  try {
    await purchaseCostParamApi.create({
      ratio: createForm.ratio,
      startTimeUtc: new Date(startMs).toISOString(),
      remark: createForm.remark.trim() || null
    })
    ElMessage.success(t('financeParams.purchaseCostParamsCreateSuccess'))
    dialogVisible.value = false
    page.value = 1
    await Promise.all([loadEffective({ silent: true }), loadList(), loadLog()])
  } catch (e: unknown) {
    ElMessage.error((e as Error)?.message || t('financeParams.purchaseCostParamsCreateFailed'))
  } finally {
    creating.value = false
  }
}

async function onDelete(row: Row) {
  try {
    await ElMessageBox.confirm(
      t('financeParams.purchaseCostParamsConfirmDelete'),
      t('financeParams.deleteBtn'),
      { type: 'warning', confirmButtonText: t('financeParams.deleteBtn'), cancelButtonText: t('financeParams.cancelBtn') }
    )
  } catch {
    return
  }

  row._deleting = true
  try {
    await purchaseCostParamApi.remove(row.id)
    ElMessage.success(t('financeParams.purchaseCostParamsDeleteSuccess'))
    await Promise.all([loadEffective({ silent: true }), loadList(), loadLog()])
  } catch (e: unknown) {
    ElMessage.error((e as Error)?.message || t('financeParams.purchaseCostParamsDeleteFailed'))
  } finally {
    row._deleting = false
  }
}

onMounted(async () => {
  await loadEffective({ silent: true })
  const [okList, okLog] = await Promise.all([loadList({ silent: true }), loadLog({ silent: true })])
  if (!okList) {
    ElMessage.error(t('financeParams.purchaseCostParamsLoadFailed'))
  }
  if (!okLog) {
    ElMessage.error(t('financeParams.logLoadFailed'))
  }
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.purchase-cost-param-settings {
  min-width: 0;
}

.form-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: 8px;
  padding: 20px 24px;
}

.section-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
}

.section-head__left {
  flex: 1;
  min-width: 0;
}

.section-head__actions {
  display: flex;
  flex-shrink: 0;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
  margin-top: 2px;
}

.section-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
  margin: 0 0 6px;

  .title-bar {
    width: 3px;
    height: 16px;
    background: linear-gradient(180deg, #00c8ff, #0066cc);
    border-radius: 2px;
    flex-shrink: 0;
  }
}

.section-hint {
  font-size: 12px;
  color: $text-muted;
  margin: 0;
  line-height: 1.5;
}

.group-card {
  background: rgba(0, 212, 255, 0.03);
  border: 1px solid $border-panel;
  border-radius: 8px;
  padding: 14px 16px;
  margin-bottom: 14px;

  &--log {
    margin-bottom: 0;
  }
}

.group-card__head {
  margin-bottom: 10px;
}

.group-card__title {
  font-size: 13px;
  font-weight: 600;
  color: $text-secondary;
}

.effective-summary {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
  font-size: 13px;
  color: $text-secondary;
}

.effective-label {
  font-weight: 600;
  color: $cyan-primary;
}

.effective-ratio {
  font-size: 18px;
  font-weight: 700;
  color: $text-primary;
  font-variant-numeric: tabular-nums;
}

.effective-meta,
.effective-remark {
  color: $text-muted;
}

.no-effective {
  margin: 0;
  font-size: 13px;
  color: $text-muted;
}

.ratio-input,
.start-time-picker {
  width: 100%;
}

.pager {
  display: flex;
  justify-content: flex-end;
  margin-top: 12px;
}

.param-table,
.log-table {
  width: 100%;
}
</style>
