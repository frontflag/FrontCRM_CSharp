<template>
  <div class="manual-transfer-page">
    <div class="page-header">
      <div class="header-left">
        <h1 class="page-title">{{ t('inventoryTransfer.title') }}</h1>
        <p v-if="sourceStockItemId" class="sub-hint">{{ t('inventoryTransfer.sourceLine', { id: sourceStockItemId }) }}</p>
      </div>
      <div class="header-right">
        <button v-if="returnStockId" type="button" class="btn-secondary" @click="goBackToStockDetail">
          {{ t('inventoryTransfer.backToSummary') }}
        </button>
      </div>
    </div>

    <div v-if="!sourceStockItemId" class="empty-panel">
      <p>{{ t('inventoryTransfer.noSourceHint') }}</p>
    </div>

    <template v-else>
      <div v-if="previewLoading" class="loading-wrap" v-loading="true" />

      <div v-else-if="previewError" class="error-panel">
        <p>{{ previewError }}</p>
        <button v-if="returnStockId" type="button" class="btn-secondary" @click="goBackToStockDetail">
          {{ t('inventoryTransfer.backToSummary') }}
        </button>
      </div>

      <div v-else-if="preview" class="form-panel">
        <section class="readonly-block">
          <h2 class="block-title">{{ t('inventoryTransfer.readonlySection') }}</h2>
          <dl class="kv">
            <dt>{{ t('inventoryTransfer.fields.stockItemCode') }}</dt>
            <dd>{{ preview.stockItemCode || t('quoteList.na') }}</dd>
            <dt>{{ t('inventoryTransfer.fields.materialModel') }}</dt>
            <dd>{{ preview.materialModel?.trim() || t('quoteList.na') }}</dd>
            <dt>{{ t('inventoryTransfer.fields.materialBrand') }}</dt>
            <dd>{{ preview.materialBrand?.trim() || t('quoteList.na') }}</dd>
            <dt>{{ t('inventoryTransfer.fields.regionType') }}</dt>
            <dd>{{ regionTypeLabel(preview.regionType) }}</dd>
            <dt>{{ t('inventoryTransfer.fields.fromWarehouse') }}</dt>
            <dd>{{ preview.fromWarehouseName || preview.fromWarehouseId }}</dd>
            <dt>{{ t('inventoryTransfer.fields.location') }}</dt>
            <dd>{{ preview.sourceLocationId || t('quoteList.na') }}</dd>
            <dt>{{ t('inventoryTransfer.fields.qtyRepertory') }}</dt>
            <dd>{{ preview.qtyRepertory }}</dd>
            <dt>{{ t('inventoryTransfer.fields.qtyAvailable') }}</dt>
            <dd>{{ preview.qtyRepertoryAvailable }}</dd>
            <dt>{{ t('inventoryTransfer.fields.plannedMoveQty') }}</dt>
            <dd class="emph">{{ preview.plannedMoveQty }}</dd>
          </dl>
          <ul v-if="preview.blockReasons?.length" class="block-reasons">
            <li v-for="(r, i) in preview.blockReasons" :key="i">{{ r }}</li>
          </ul>
        </section>

        <section class="edit-block">
          <h2 class="block-title">{{ t('inventoryTransfer.targetSection') }}</h2>
          <el-form label-width="120px" class="transfer-form">
            <el-form-item :label="t('inventoryTransfer.fields.toWarehouse')" required>
              <el-select
                v-model="toWarehouseId"
                filterable
                :placeholder="t('inventoryTransfer.placeholders.toWarehouse')"
                style="width: 100%; max-width: 420px"
                :disabled="!preview.canExecute || submitting"
              >
                <el-option v-for="w in warehouseOptions" :key="w.id" :label="warehouseLabel(w)" :value="w.id" />
              </el-select>
            </el-form-item>
            <el-form-item :label="t('inventoryTransfer.fields.toLocation')">
              <el-input
                v-model="toLocationId"
                clearable
                :placeholder="t('inventoryTransfer.placeholders.toLocation')"
                style="width: 100%; max-width: 420px"
                :disabled="!preview.canExecute || submitting"
              />
            </el-form-item>
            <el-form-item :label="t('inventoryTransfer.fields.remark')">
              <el-input
                v-model="remark"
                type="textarea"
                :rows="2"
                :placeholder="t('inventoryTransfer.placeholders.remark')"
                style="width: 100%; max-width: 520px"
                :disabled="!preview.canExecute || submitting"
              />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :disabled="!canSubmit" :loading="submitting" @click="onSubmitClick">
                {{ t('inventoryTransfer.submit') }}
              </el-button>
            </el-form-item>
          </el-form>
        </section>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { manualStockTransferApi, type ManualTransferPreview } from '@/api/manualStockTransfer'
import { inventoryCenterApi, type WarehouseInfo } from '@/api/inventoryCenter'
import { getApiErrorMessage } from '@/utils/apiError'
import { normalizeRegionType, REGION_TYPE_OVERSEAS } from '@/constants/regionType'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const sourceStockItemId = computed(() => String(route.query.sourceStockItemId || '').trim())
const returnStockId = computed(() => String(route.query.returnStockId || '').trim())

const summaryQueryKeys = ['stockCode', 'materialModel', 'materialBrand', 'warehouseId', 'materialId'] as const

function buildReturnQuery(): Record<string, string> {
  const q: Record<string, string> = {}
  for (const k of summaryQueryKeys) {
    const v = route.query[k]
    const s = Array.isArray(v) ? v[0] : v
    if (s != null && String(s).trim() !== '') q[k] = String(s).trim()
  }
  return q
}

const preview = ref<ManualTransferPreview | null>(null)
const previewLoading = ref(false)
const previewError = ref('')
const warehouses = ref<WarehouseInfo[]>([])
const toWarehouseId = ref('')
const toLocationId = ref('')
const remark = ref('')
const submitting = ref(false)

const warehouseOptions = computed(() => warehouses.value.filter(w => (w.status ?? 1) === 1))

const canSubmit = computed(() => {
  if (!preview.value?.canExecute) return false
  if (!(toWarehouseId.value || '').trim()) return false
  return !submitting.value
})

const regionTypeLabel = (v: number | undefined) => {
  const n = normalizeRegionType(v)
  return n === REGION_TYPE_OVERSEAS ? t('inventoryList.warehouse.regionOverseas') : t('inventoryList.warehouse.regionDomestic')
}

const warehouseLabel = (w: WarehouseInfo) => {
  const code = (w.warehouseCode || '').trim()
  const name = (w.warehouseName || '').trim()
  if (code && name) return `${code} — ${name}`
  return name || code || w.id
}

const loadWarehouses = async () => {
  try {
    warehouses.value = await inventoryCenterApi.getWarehouses()
  } catch {
    warehouses.value = []
  }
}

const loadPreview = async () => {
  const sid = sourceStockItemId.value
  if (!sid) {
    preview.value = null
    return
  }
  previewLoading.value = true
  previewError.value = ''
  try {
    preview.value = await manualStockTransferApi.preview(sid)
    toWarehouseId.value = ''
    toLocationId.value = ''
    remark.value = ''
  } catch (e) {
    console.error(e)
    preview.value = null
    previewError.value = getApiErrorMessage(e, t('inventoryTransfer.previewFailed'))
  } finally {
    previewLoading.value = false
  }
}

const goBackToStockDetail = () => {
  const id = returnStockId.value
  if (!id) {
    router.push('/inventory/list')
    return
  }
  router.replace({ path: `/inventory/stocks/${encodeURIComponent(id)}`, query: buildReturnQuery() })
}

const onSubmitClick = async () => {
  if (!preview.value?.canExecute || !sourceStockItemId.value) return
  const tw = toWarehouseId.value.trim()
  if (!tw) {
    ElMessage.warning(t('inventoryTransfer.selectWarehouse'))
    return
  }
  const fromName = preview.value.fromWarehouseName || preview.value.fromWarehouseId
  const toW = warehouses.value.find(x => x.id === tw)
  const toName = toW ? warehouseLabel(toW) : tw
  try {
    await ElMessageBox.confirm(
      t('inventoryTransfer.confirmBody', {
        qty: preview.value.plannedMoveQty,
        from: fromName,
        to: toName
      }),
      t('inventoryTransfer.confirmTitle'),
      { type: 'warning', confirmButtonText: t('inventoryTransfer.confirmOk'), cancelButtonText: t('common.cancel') }
    )
  } catch {
    return
  }

  submitting.value = true
  try {
    const result = await manualStockTransferApi.execute({
      sourceStockItemId: sourceStockItemId.value,
      toWarehouseId: tw,
      toLocationId: (toLocationId.value || '').trim() || null,
      remark: (remark.value || '').trim() || null
    })
    ElMessage.success(t('inventoryTransfer.success', { code: result.transferCode, qty: result.moveQty }))
    if (returnStockId.value) {
      router.replace({ path: `/inventory/stocks/${encodeURIComponent(returnStockId.value)}`, query: buildReturnQuery() })
    } else {
      await loadPreview()
    }
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, t('inventoryTransfer.executeFailed')))
  } finally {
    submitting.value = false
  }
}

watch(
  () => sourceStockItemId.value,
  () => {
    void loadPreview()
  }
)

onMounted(async () => {
  await loadWarehouses()
  await loadPreview()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.manual-transfer-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 20px;
}

.page-title {
  margin: 0 0 6px;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
}

.sub-hint {
  margin: 0;
  font-size: 13px;
  color: $text-muted;
  word-break: break-all;
}

.btn-secondary {
  padding: 8px 16px;
  border-radius: 6px;
  font-size: 13px;
  cursor: pointer;
  border: 1px solid $border-panel;
  background: $layer-2;
  color: $text-primary;
}

.empty-panel,
.error-panel {
  padding: 24px;
  background: $layer-2;
  border-radius: 8px;
  color: $text-secondary;
  font-size: 14px;
}

.error-panel {
  color: $text-primary;
}

.loading-wrap {
  min-height: 200px;
}

.form-panel {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.readonly-block,
.edit-block {
  background: $layer-2;
  border-radius: 8px;
  padding: 20px;
  border: 1px solid $border-panel;
}

.block-title {
  margin: 0 0 14px;
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
}

.kv {
  display: grid;
  grid-template-columns: 160px 1fr;
  gap: 8px 12px;
  margin: 0;
  font-size: 13px;
  dt {
    color: $text-muted;
  }
  dd {
    margin: 0;
    color: $text-primary;
  }
  .emph {
    font-weight: 600;
    color: $primary-color;
  }
}

.block-reasons {
  margin: 12px 0 0;
  padding-left: 18px;
  color: #c45656;
  font-size: 13px;
}

.transfer-form {
  max-width: 640px;
}
</style>
