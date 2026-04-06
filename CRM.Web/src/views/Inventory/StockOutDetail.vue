<template>
  <div class="stockout-detail-page" v-loading="loading">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3zM17 14l4 4-4 4M10 17h11" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('stockOutDetail.title') }}</h1>
          <span v-if="detail" :class="['status-badge', `status-${detail.status}`]">{{ statusLabel(detail.status) }}</span>
        </div>
      </div>
      <div class="header-right">
        <button type="button" class="btn-secondary" @click="goBack">{{ t('stockOutDetail.back') }}</button>
        <button type="button" class="btn-primary" style="margin-left: 8px" :disabled="saving || !detail" @click="saveHeader">
          {{ saving ? t('stockOutDetail.saving') : t('stockOutDetail.save') }}
        </button>
      </div>
    </div>

    <template v-if="detail">
      <div class="form-card">
        <h3 class="section-title">{{ t('stockOutDetail.sectionReadonly') }}</h3>
        <el-descriptions :column="2" border size="small" class="desc-block">
          <el-descriptions-item :label="t('stockOutList.columns.stockOutCode')">{{ detail.stockOutCode }}</el-descriptions-item>
          <el-descriptions-item :label="t('stockOutDetail.sourceCode')">{{ detail.sourceCode || '—' }}</el-descriptions-item>
          <el-descriptions-item :label="t('stockOutList.columns.customerName')">{{ detail.customerName || '—' }}</el-descriptions-item>
          <el-descriptions-item :label="t('stockOutList.columns.salesUserName')">{{ detail.salesUserName || '—' }}</el-descriptions-item>
          <el-descriptions-item :label="t('stockOutList.columns.sellOrderItemCode')">{{ detail.sellOrderItemCode || '—' }}</el-descriptions-item>
          <el-descriptions-item :label="t('stockOutList.columns.totalQuantity')">{{ formatNum(detail.totalQuantity) }}</el-descriptions-item>
          <el-descriptions-item :label="t('stockOutDetail.warehouseCode')">{{ detail.warehouseCode || '—' }}</el-descriptions-item>
          <el-descriptions-item :label="t('stockOutList.columns.remark')">{{ detail.remark || '—' }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <div class="form-card">
        <h3 class="section-title">{{ t('stockOutDetail.sectionEditable') }}</h3>
        <el-form label-width="120px" class="edit-form">
          <el-form-item :label="t('stockOutList.columns.stockOutDate')">
            <el-date-picker
              v-model="editForm.stockOutDate"
              type="date"
              value-format="YYYY-MM-DD"
              :placeholder="t('stockOutDetail.pickDate')"
              style="width: 100%; max-width: 320px"
            />
          </el-form-item>
          <el-form-item :label="t('stockOutDetail.shipmentMethod')">
            <el-select
              v-model="editForm.shipmentMethod"
              clearable
              filterable
              :placeholder="t('stockOutDetail.shipmentPlaceholder')"
              style="width: 100%; max-width: 320px"
            >
              <el-option v-for="o in shipmentMethodOptions" :key="o.value" :label="o.label" :value="o.value" />
            </el-select>
          </el-form-item>
          <el-form-item :label="t('stockOutDetail.courierTrackingNo')">
            <el-input v-model="editForm.courierTrackingNo" clearable :placeholder="t('stockOutDetail.trackingPlaceholder')" style="max-width: 400px" />
          </el-form-item>
        </el-form>
      </div>

      <div class="form-card">
        <h3 class="section-title">{{ t('stockOutDetail.sectionDocs') }}</h3>
        <p class="doc-hint">{{ t('stockOutDetail.docHint') }}</p>
        <DocumentUploadPanel
          :biz-type="DOC_BIZ"
          :biz-id="detail.id"
          :max-files="20"
          :max-size-mb="100"
          @uploaded="docListRef?.refresh()"
        />
        <DocumentListPanel
          ref="docListRef"
          :biz-type="DOC_BIZ"
          :biz-id="detail.id"
          view-mode="list"
          style="margin-top: 16px"
        />
      </div>
    </template>

    <el-empty v-else-if="!loading" :description="loadError || t('stockOutDetail.notFound')" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { stockOutApi, type StockOutDetailDto } from '@/api/stockOut'
import DocumentUploadPanel from '@/components/Document/DocumentUploadPanel.vue'
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'

const DOC_BIZ = 'STOCK_OUT'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const { ensureLoaded: ensureLogisticsDict, arrivalOptions: shipmentMethodOptions } = useLogisticsFormDict()

const loading = ref(false)
const saving = ref(false)
const loadError = ref('')
const detail = ref<StockOutDetailDto | null>(null)
const docListRef = ref<InstanceType<typeof DocumentListPanel> | null>(null)

const stockOutId = computed(() => {
  const raw = route.params.id
  if (Array.isArray(raw)) return String(raw[0] ?? '').trim()
  return String(raw ?? '').trim()
})

const editForm = ref({
  stockOutDate: '' as string,
  shipmentMethod: '' as string,
  courierTrackingNo: '' as string
})

function toDateOnly(iso?: string) {
  if (!iso) return ''
  const s = String(iso)
  return s.length >= 10 ? s.slice(0, 10) : s
}

function syncEditFromDetail(d: StockOutDetailDto) {
  editForm.value = {
    stockOutDate: toDateOnly(d.stockOutDate),
    shipmentMethod: d.shipmentMethod ?? '',
    courierTrackingNo: d.courierTrackingNo ?? ''
  }
}

watch(detail, (d) => {
  if (d) syncEditFromDetail(d)
})

const formatNum = (v: number) => (v == null ? '—' : Number(v).toLocaleString())

const statusLabel = (s: number) => {
  switch (s) {
    case 0:
      return t('stockOutList.status.draft')
    case 1:
      return t('stockOutList.status.pending')
    case 2:
      return t('stockOutList.status.done')
    case 3:
      return t('stockOutList.status.cancelled')
    case 4:
      return t('stockOutList.status.finished')
    default:
      return String(s)
  }
}

async function load() {
  const id = stockOutId.value
  if (!id) {
    loadError.value = t('stockOutDetail.notFound')
    return
  }
  loading.value = true
  loadError.value = ''
  try {
    await ensureLogisticsDict()
    const d = await stockOutApi.getById(id)
    if (!d) {
      detail.value = null
      loadError.value = t('stockOutDetail.notFound')
      return
    }
    detail.value = d
    syncEditFromDetail(d)
  } catch {
    detail.value = null
    loadError.value = t('stockOutDetail.loadFailed')
  } finally {
    loading.value = false
  }
}

async function saveHeader() {
  const id = stockOutId.value
  const d = detail.value
  if (!id || !d) return
  if (!editForm.value.stockOutDate) {
    ElMessage.warning(t('stockOutDetail.needDate'))
    return
  }
  saving.value = true
  try {
    const dateIso = `${editForm.value.stockOutDate}T00:00:00.000Z`
    await stockOutApi.updateHeader(id, {
      stockOutDate: dateIso,
      shipmentMethod: editForm.value.shipmentMethod?.trim() || null,
      courierTrackingNo: editForm.value.courierTrackingNo?.trim() || null
    })
    ElMessage.success(t('stockOutDetail.saveOk'))
    await load()
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.message || e?.message || t('stockOutDetail.saveFail'))
  } finally {
    saving.value = false
  }
}

function goBack() {
  router.push({ name: 'StockOutList' })
}

onMounted(() => void load())
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.stockout-detail-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
}
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}
.header-left,
.header-right {
  display: flex;
  align-items: center;
  gap: 10px;
}
.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
}
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
.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
}
.btn-secondary,
.btn-primary {
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  cursor: pointer;
}
.btn-secondary {
  border: 1px solid $border-panel;
  color: $text-secondary;
  background: rgba(255, 255, 255, 0.05);
}
.btn-primary {
  border: none;
  background: linear-gradient(135deg, #00a8cc, #0066cc);
  color: #fff;
  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}
.form-card {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: 8px;
  padding: 20px 24px;
  margin-bottom: 16px;
}
.section-title {
  margin: 0 0 16px;
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
}
.desc-block {
  --el-descriptions-item-bordered-label-background: rgba(0, 0, 0, 0.15);
}
.doc-hint {
  font-size: 12px;
  color: $text-muted;
  margin: 0 0 12px;
}
.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  &.status-0 {
    background: rgba(255, 255, 255, 0.05);
    color: $text-muted;
  }
  &.status-1 {
    background: rgba(255, 193, 7, 0.15);
    color: #ffc107;
  }
  &.status-2 {
    background: rgba(70, 191, 145, 0.18);
    color: #46bf91;
  }
  &.status-3 {
    background: rgba(201, 87, 69, 0.18);
    color: #c95745;
  }
  &.status-4 {
    background: rgba(0, 212, 255, 0.18);
    color: $cyan-primary;
  }
}
</style>
