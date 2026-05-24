<template>
  <div class="warehouse-manage-page">
    <div class="page-header">
      <div class="header-left">
        <el-button link type="primary" class="back-btn" @click="goBack">
          {{ t('warehouseManage.back') }}
        </el-button>
        <h1 class="page-title">{{ t('warehouseManage.title') }}</h1>
      </div>
    </div>

    <div v-loading="loading" class="form-section">
      <div class="section-head">
        <div class="section-head__left">
          <div class="section-title">
            <span class="title-bar"></span>{{ t('warehouseManage.sectionTitle') }}
          </div>
          <p class="section-hint">{{ t('warehouseManage.sectionHint') }}</p>
        </div>
        <el-button type="primary" class="save-all-btn" :loading="saving" @click="saveAll">
          {{ t('warehouseManage.saveAll') }}
        </el-button>
      </div>

      <div v-for="(row, idx) in rows" :key="row._key" class="group-card">
        <div class="group-card__head">
          <span class="group-card__title">{{ t('warehouseManage.groupTitle', { n: idx + 1 }) }}</span>
          <div class="group-card__actions">
            <span class="switch-label">{{ t('warehouseManage.enabled') }}</span>
            <el-switch :model-value="row.status === 1" @update:model-value="(on: boolean) => (row.status = on ? 1 : 0)" />
          </div>
        </div>
        <el-form label-width="120px" class="settings-form" :model="row">
          <el-row :gutter="16">
            <el-col :span="8">
              <el-form-item :label="t('warehouseManage.warehouseCode')" required>
                <el-input v-model="row.warehouseCode" :placeholder="t('warehouseManage.phCode')" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('warehouseManage.warehouseName')" required>
                <el-input v-model="row.warehouseName" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('warehouseManage.regionType')">
                <el-select v-model="row.regionType" style="width: 100%" :teleported="false">
                  <el-option :value="REGION_TYPE_DOMESTIC" :label="t('warehouseManage.regionDomestic')" />
                  <el-option :value="REGION_TYPE_OVERSEAS" :label="t('warehouseManage.regionOverseas')" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item :label="t('warehouseManage.contactName')">
                <el-input v-model="row.contactName" />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item :label="t('warehouseManage.contactPhone')">
                <el-input v-model="row.contactPhone" />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item :label="t('warehouseManage.workHours')">
                <el-input v-model="row.workHours" :placeholder="t('warehouseManage.phWorkHours')" />
              </el-form-item>
            </el-col>
            <el-col :span="24">
              <el-form-item :label="t('warehouseManage.address')">
                <el-input v-model="row.address" type="textarea" :rows="2" />
              </el-form-item>
            </el-col>
          </el-row>
        </el-form>
        <div class="group-card__footer">
          <el-button
            class="group-mini-btn"
            circle
            type="primary"
            plain
            :title="t('warehouseManage.addBelow')"
            @click="insertAfter(idx)"
          >
            <el-icon><Plus /></el-icon>
          </el-button>
          <el-button
            class="group-mini-btn group-mini-btn--minus"
            circle
            plain
            :title="t('warehouseManage.remove')"
            @click="removeAt(idx)"
          >
            <el-icon><Minus /></el-icon>
          </el-button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Minus, Plus } from '@element-plus/icons-vue'
import { inventoryCenterApi, type WarehouseInfo } from '@/api/inventoryCenter'
import { REGION_TYPE_DOMESTIC, REGION_TYPE_OVERSEAS, normalizeRegionType } from '@/constants/regionType'
import { getApiErrorMessage } from '@/utils/apiError'

type WarehouseRowVm = WarehouseInfo & { _key: string }

const { t } = useI18n()
const router = useRouter()

const loading = ref(false)
const saving = ref(false)
const rows = ref<WarehouseRowVm[]>([])

function newKey() {
  return crypto.randomUUID()
}

function emptyRow(): WarehouseRowVm {
  return {
    _key: newKey(),
    warehouseCode: '',
    warehouseName: '',
    address: '',
    contactName: '',
    contactPhone: '',
    workHours: '',
    regionType: REGION_TYPE_DOMESTIC,
    status: 1
  }
}

function normalizeRow(raw: WarehouseInfo): WarehouseRowVm {
  const r = raw as unknown as Record<string, unknown>
  const idRaw = r.id ?? r.Id
  const id = typeof idRaw === 'string' && idRaw.trim() ? idRaw.trim() : undefined
  return {
    _key: id || newKey(),
    id,
    warehouseCode: String(r.warehouseCode ?? r.WarehouseCode ?? '').trim(),
    warehouseName: String(r.warehouseName ?? r.WarehouseName ?? '').trim(),
    address: String(r.address ?? r.Address ?? ''),
    contactName: String(r.contactName ?? r.ContactName ?? ''),
    contactPhone: String(r.contactPhone ?? r.ContactPhone ?? ''),
    workHours: String(r.workHours ?? r.WorkHours ?? ''),
    regionType: normalizeRegionType(r.regionType ?? r.RegionType),
    status: Number(r.status ?? r.Status ?? 1) === 0 ? 0 : 1
  }
}

function toPayload(row: WarehouseRowVm): WarehouseInfo {
  const rt = normalizeRegionType(row.regionType)
  const base: WarehouseInfo = {
    warehouseCode: row.warehouseCode.trim(),
    warehouseName: row.warehouseName.trim(),
    address: row.address?.trim() || undefined,
    contactName: row.contactName?.trim() || undefined,
    contactPhone: row.contactPhone?.trim() || undefined,
    workHours: row.workHours?.trim() || undefined,
    regionType: rt,
    status: row.status === 0 ? 0 : 1
  }
  const persistedId = row.id?.trim()
  if (persistedId && !persistedId.startsWith('00000000')) {
    return { ...base, id: persistedId }
  }
  return base
}

function validateRows(): boolean {
  for (let i = 0; i < rows.value.length; i++) {
    const r = rows.value[i]
    if (!r.warehouseCode.trim()) {
      ElMessage.warning(t('warehouseManage.validateCode', { n: i + 1 }))
      return false
    }
    if (!r.warehouseName.trim()) {
      ElMessage.warning(t('warehouseManage.validateName', { n: i + 1 }))
      return false
    }
  }
  const codes = rows.value.map((r) => r.warehouseCode.trim().toLowerCase()).filter(Boolean)
  if (new Set(codes).size !== codes.length) {
    ElMessage.warning(t('warehouseManage.validateDuplicateCode'))
    return false
  }
  return true
}

async function load() {
  loading.value = true
  try {
    const list = await inventoryCenterApi.getWarehouses()
    const normalized = list.map(normalizeRow)
    rows.value = normalized.length > 0 ? normalized : [emptyRow()]
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('warehouseManage.loadFailed')))
    rows.value = [emptyRow()]
  } finally {
    loading.value = false
  }
}

async function saveAll() {
  if (!validateRows()) return
  saving.value = true
  try {
    const payload = rows.value.map(toPayload)
    const saved = await inventoryCenterApi.saveWarehousesBatch(payload)
    rows.value = saved.length > 0 ? saved.map(normalizeRow) : [emptyRow()]
    ElMessage.success(t('warehouseManage.saveSuccess'))
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('warehouseManage.saveFailed')))
  } finally {
    saving.value = false
  }
}

function insertAfter(index: number) {
  rows.value.splice(index + 1, 0, emptyRow())
}

async function removeAt(index: number) {
  if (rows.value.length <= 1) {
    ElMessage.warning(t('warehouseManage.keepOne'))
    return
  }
  const row = rows.value[index]
  const name = row.warehouseName.trim() || row.warehouseCode.trim()
  const message = name
    ? t('warehouseManage.removeConfirmMessageWithName', { name })
    : t('warehouseManage.removeConfirmMessage', { n: index + 1 })
  try {
    await ElMessageBox.confirm(message, t('warehouseManage.removeConfirmTitle'), {
      type: 'warning'
    })
    rows.value.splice(index, 1)
  } catch {
    // user cancelled
  }
}

function goBack() {
  router.back()
}

onMounted(() => void load())
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.warehouse-manage-page {
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
}

.header-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
}

.back-btn {
  padding-left: 0;
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

.save-all-btn {
  flex-shrink: 0;
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
  padding: 14px 16px 8px;
  margin-bottom: 14px;
}

.group-card__head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  margin-bottom: 8px;
  padding-bottom: 10px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
}

.group-card__title {
  font-size: 13px;
  font-weight: 600;
  color: $text-secondary;
}

.group-card__actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px;
}

.switch-label {
  font-size: 12px;
  color: $text-muted;
}

.group-card__footer {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  gap: 6px;
  margin-top: 10px;
  padding-top: 8px;
  border-top: 1px solid rgba(255, 255, 255, 0.06);
}

.group-mini-btn {
  width: 24px !important;
  min-width: 24px !important;
  height: 24px !important;
  padding: 0 !important;

  :deep(.el-icon) {
    font-size: 12px;
  }
}

.group-mini-btn--minus {
  border-color: rgba(201, 87, 69, 0.45) !important;
  color: #c95745 !important;
  background: rgba(201, 87, 69, 0.08) !important;

  &:hover {
    border-color: rgba(201, 87, 69, 0.65) !important;
    background: rgba(201, 87, 69, 0.14) !important;
  }
}

.settings-form {
  :deep(.el-form-item__label) {
    color: $text-muted;
    font-size: 13px;
  }

  :deep(.el-input__wrapper),
  :deep(.el-textarea__inner) {
    background: $layer-3;
    border-color: $border-panel;
    box-shadow: none;
    &:hover {
      border-color: rgba(0, 212, 255, 0.35);
    }
    &.is-focus {
      border-color: $cyan-primary;
    }
  }

  :deep(.el-input__inner),
  :deep(.el-textarea__inner) {
    color: $text-primary;
    background: transparent;
    &::placeholder {
      color: $text-placeholder;
    }
  }

  :deep(.el-select .el-select__wrapper) {
    background: $layer-3 !important;
    border: 1px solid $border-panel !important;
    box-shadow: none !important;
  }
}
</style>
