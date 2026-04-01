<template>
  <div class="bom-detail-page">
    <!-- 加载中 -->
    <div v-if="loading" class="loading-state">
      <el-icon class="is-loading"><Loading /></el-icon>加载中...
    </div>

    <template v-else-if="bom">
      <!-- 详情 CaptionBar -->
      <div class="page-header">
        <div class="header-left">
          <button type="button" class="btn-back" @click="router.push({ name: 'BOMList' })">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="15 18 9 12 15 6" />
            </svg>
            返回
          </button>
          <div class="bom-caption-title-group">
            <div class="caption-avatar-lg">{{ captionAvatarChar }}</div>
            <div>
              <div class="page-title-row">
                <div class="page-title-with-icons">
                  <h1 class="page-title">{{ bom.bomCode }}</h1>
                  <el-tag effect="dark" :type="getStatusTagType(bom.status)" size="small">{{ getStatusText(bom.status) }}</el-tag>
                  <el-tag effect="dark" v-if="bom.bomType" size="small" :type="getBOMTypeTagType(bom.bomType)">
                    {{ getBOMTypeText(bom.bomType) }}
                  </el-tag>
                </div>
              </div>
              <div class="title-meta caption-meta-line">
                <span class="caption-muted">{{ bom.customerName || '—' }}</span>
              </div>
            </div>
          </div>
        </div>
        <div class="header-right">
          <el-button
            type="success"
            :loading="autoQuoting"
            :disabled="!hasPendingItems"
            @click="handleAutoQuote"
          >
            <el-icon><MagicStick /></el-icon>一键快速报价
          </el-button>
          <el-dropdown
            trigger="click"
            placement="bottom-end"
            popper-class="bom-detail-header-more-popper"
            @command="onHeaderMoreCommand"
          >
            <button type="button" class="btn-more-actions" title="更多操作" aria-label="更多操作">
              <span class="btn-more-actions__dots" aria-hidden="true">⋯</span>
            </button>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="delete" class="detail-more-item--danger">删除 BOM</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </div>

      <!-- 基础信息卡片 -->
      <div class="info-cards">
        <div class="info-card">
          <div class="info-label">客户</div>
          <div class="info-value">{{ bom.customerName || '—' }}</div>
        </div>
        <div class="info-card">
          <div class="info-label">需求日期</div>
          <div class="info-value">{{ formatBomDate(bom.bomDate) }}</div>
        </div>
        <div class="info-card">
          <div class="info-label">来源</div>
          <div class="info-value">{{ getSourceText(bom.source) }}</div>
        </div>
        <div class="info-card">
          <div class="info-label">明细总数</div>
          <div class="info-value highlight">{{ bom.items?.length ?? 0 }}</div>
        </div>
        <div class="info-card">
          <div class="info-label">已报价</div>
          <div class="info-value success">{{ quotedCount }}</div>
        </div>
        <div class="info-card">
          <div class="info-label">待报价</div>
          <div class="info-value warning">{{ pendingCount }}</div>
        </div>
        <div class="info-card">
          <div class="info-label">创建时间</div>
          <div class="info-value">{{ formatDate(bom.createdAt) }}</div>
        </div>
        <div class="info-card" v-if="bom.remark">
          <div class="info-label">备注</div>
          <div class="info-value">{{ bom.remark }}</div>
        </div>
      </div>

      <!-- 明细列表 -->
      <div class="items-panel">
        <div class="items-header">
          <div class="items-title">
            <span class="dot"></span>BOM 明细列表
          </div>
          <div class="items-filter">
            <el-radio-group v-model="filterStatus" size="small" @change="applyFilter">
              <el-radio-button value="">全部</el-radio-button>
              <el-radio-button :value="0">待报价</el-radio-button>
              <el-radio-button :value="1">系统报价</el-radio-button>
              <el-radio-button :value="2">人工报价</el-radio-button>
              <el-radio-button :value="3">无货</el-radio-button>
            </el-radio-group>
          </div>
        </div>

        <CrmDataTable
          :data="filteredItems"
          class="items-table"
          row-key="id"
          @selection-change="handleItemSelection"
        >
          <el-table-column type="selection" width="44" />
          <el-table-column label="#" prop="lineNo" width="50" align="center" />
          <el-table-column label="客户物料型号" prop="customerMaterialModel" min-width="130" show-overflow-tooltip />
          <el-table-column label="MPN" prop="materialModel" min-width="150" show-overflow-tooltip>
            <template #default="{ row }">
              <span class="mpn-text">{{ row.materialModel || '—' }}</span>
            </template>
          </el-table-column>
          <el-table-column label="品牌" prop="brand" width="100" show-overflow-tooltip />
          <el-table-column label="数量" prop="quantity" width="80" align="right" />
          <el-table-column label="目标价" width="90" align="right">
            <template #default="{ row }">
              <span v-if="row.targetPrice">{{ row.targetPrice }} {{ row.currency }}</span>
              <span v-else class="text-muted">—</span>
            </template>
          </el-table-column>
          <el-table-column label="报价状态" width="100" align="center">
            <template #default="{ row }">
              <el-tag effect="dark" size="small" :type="getItemStatusTagType(row.quoteStatus)">
                {{ getItemStatusText(row.quoteStatus) }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="报价单价" width="110" align="right">
            <template #default="{ row }">
              <span v-if="row.quotedPrice" class="quoted-price">
                {{ formatUnitPriceNumber(row.quotedPrice) }} {{ row.quotedCurrency || row.currency }}
              </span>
              <span v-else class="text-muted">—</span>
            </template>
          </el-table-column>
          <el-table-column label="可供数量" width="90" align="right">
            <template #default="{ row }">
              <span v-if="row.quotedStock != null">{{ row.quotedStock }}</span>
              <span v-else class="text-muted">—</span>
            </template>
          </el-table-column>
          <el-table-column label="报价品牌" width="100" show-overflow-tooltip>
            <template #default="{ row }">{{ row.quotedBrand || '—' }}</template>
          </el-table-column>
          <el-table-column label="交货天数" width="80" align="center">
            <template #default="{ row }">
              <span v-if="row.quotedDeliveryDays">{{ row.quotedDeliveryDays }}天</span>
              <span v-else class="text-muted">—</span>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="160" fixed="right" class-name="op-col" label-class-name="op-col">
            <template #default="{ row }">
              <div @click.stop @dblclick.stop>
                <div class="action-btns">
                  <el-button
                    v-if="row.quoteStatus === 0 || row.quoteStatus === 3"
                    size="small"
                    type="primary"
                    @click.stop="openManualQuote(row, false)"
                  >人工报价</el-button>
                  <el-button
                    v-else
                    size="small"
                    type="warning"
                    @click.stop="openManualQuote(row, true)"
                  >修改报价</el-button>
                </div>
              </div>
            </template>
          </el-table-column>
        </CrmDataTable>
      </div>
    </template>

    <!-- 人工报价 / 修改报价弹窗 -->
    <el-dialog
      v-model="quoteDialogVisible"
      :title="isEditQuote ? '修改报价' : '人工报价'"
      width="480px"
      class="quote-dialog"
    >
      <div class="quote-item-info" v-if="currentItem">
        <div class="qi-row">
          <span class="qi-label">MPN：</span>
          <span class="qi-value mpn">{{ currentItem.materialModel }}</span>
        </div>
        <div class="qi-row">
          <span class="qi-label">数量：</span>
          <span class="qi-value">{{ currentItem.quantity }}</span>
          <span class="qi-label" style="margin-left:16px">目标价：</span>
          <span class="qi-value">{{ currentItem.targetPrice ?? '—' }} {{ currentItem.currency }}</span>
        </div>
      </div>
      <el-form ref="quoteFormRef" :model="quoteForm" :rules="quoteRules" label-position="top">
        <el-row :gutter="14">
          <el-col :span="12">
            <el-form-item label="报价单价" prop="quotedPrice">
              <el-input-number v-model="quoteForm.quotedPrice" :min="0" :precision="6" class="w-full" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="报价货币">
              <el-select v-model="quoteForm.quotedCurrency" class="w-full">
                <el-option
                  v-for="opt in SETTLEMENT_CURRENCY_STRING_OPTIONS"
                  :key="opt.value"
                  :label="opt.label"
                  :value="opt.value"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="可供数量">
              <el-input-number v-model="quoteForm.quotedStock" :min="0" class="w-full" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="交货天数">
              <el-input-number v-model="quoteForm.quotedDeliveryDays" :min="0" class="w-full" />
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item label="报价品牌">
              <el-input v-model="quoteForm.quotedBrand" placeholder="报价品牌（可选）" />
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item label="报价备注">
              <el-input v-model="quoteForm.quoteRemark" type="textarea" :rows="2" placeholder="报价备注（可选）" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button @click="quoteDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="quoteSubmitting" @click="submitQuote">
          {{ isEditQuote ? '保存修改' : '确认报价' }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { MagicStick, Loading } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { bomApi } from '@/api/bom'
import { runValidatedFormSave } from '@/composables/useFormSubmit'
import type { BOM, BOMItem } from '@/types/bom'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import { formatUnitPriceNumber } from '@/utils/moneyFormat'
import { SETTLEMENT_CURRENCY_STRING_OPTIONS } from '@/constants/currency'

const router = useRouter()
const route = useRoute()
const bomId = computed(() => route.params.id as string)

// ── 数据 ──
const loading = ref(false)
const bom = ref<BOM | null>(null)
const filterStatus = ref<number | ''>('')
const selectedItemIds = ref<string[]>([])

// ── 统计 ──
const quotedCount = computed(() => bom.value?.items?.filter(i => i.quoteStatus === 1 || i.quoteStatus === 2 || i.quoteStatus === 4).length ?? 0)
const pendingCount = computed(() => bom.value?.items?.filter(i => i.quoteStatus === 0).length ?? 0)
const hasPendingItems = computed(() => pendingCount.value > 0)

const filteredItems = computed(() => {
  if (!bom.value?.items) return []
  if (filterStatus.value === '') return bom.value.items
  return bom.value.items.filter(i => i.quoteStatus === filterStatus.value)
})

const captionAvatarChar = computed(() => {
  const c = bom.value?.bomCode?.trim()
  return (c && c[0]) || 'B'
})

function onHeaderMoreCommand(cmd: string) {
  if (cmd === 'delete') void handleDelete()
}

// ── 加载 ──
const loadData = async () => {
  loading.value = true
  try {
    bom.value = await bomApi.getBOMById(bomId.value)
  } catch {
    ElMessage.error('加载 BOM 详情失败')
  } finally {
    loading.value = false
  }
}

// ── 文本/标签 ──
const getStatusText = (s: number) => {
  const m: Record<number, string> = { 0: '草稿', 1: '待报价', 2: '报价中', 3: '已报价', 4: '已接受', 5: '已关闭', 6: '已取消' }
  return m[s] ?? '未知'
}
const getStatusTagType = (s: number): '' | 'success' | 'warning' | 'danger' | 'info' => {
  const m: Record<number, '' | 'success' | 'warning' | 'danger' | 'info'> = { 0: 'info', 1: 'warning', 2: '', 3: 'success', 4: 'success', 5: 'info', 6: 'danger' }
  return m[s] ?? 'info'
}
const getBOMTypeText = (t: number) => ({ 1: '现货', 2: '期货', 3: '样品', 4: '批量' }[t] ?? '—')
const getBOMTypeTagType = (t: number): '' | 'success' | 'warning' | 'danger' | 'info' => ({ 1: '' as const, 2: 'warning' as const, 3: 'success' as const, 4: 'info' as const }[t] ?? 'info')
const getSourceText = (s?: number) => ({ 1: '线下', 2: '线上', 3: '邮件', 4: '电话', 5: '导入' }[s ?? 0] ?? '—')
const getItemStatusText = (s: number) => ({ 0: '待报价', 1: '系统报价', 2: '人工报价', 3: '无货', 4: '已接受', 5: '已拒绝' }[s] ?? '—')
const getItemStatusTagType = (s: number): '' | 'success' | 'warning' | 'danger' | 'info' => {
  const m: Record<number, '' | 'success' | 'warning' | 'danger' | 'info'> = { 0: 'info', 1: 'success', 2: '', 3: 'danger', 4: 'success', 5: 'danger' }
  return m[s] ?? 'info'
}
const formatBomDate = (d?: string) => {
  if (!d) return '—'
  const s = formatDisplayDate(d)
  return s === '--' ? '—' : s
}
const formatDate = (d?: string) => {
  if (!d) return '—'
  const s = formatDisplayDateTime(d)
  return s === '--' ? '—' : s
}

// ── 筛选 ──
const applyFilter = () => {}

// ── 选择 ──
const handleItemSelection = (rows: BOMItem[]) => {
  selectedItemIds.value = rows.map(r => r.id)
}

// ── 一键报价 ──
const autoQuoting = ref(false)
const handleAutoQuote = async () => {
  autoQuoting.value = true
  try {
    const res = await bomApi.autoQuote({ bomId: bomId.value })
    ElMessage.success(`一键报价完成：${res.quotedItems} 条已报价，${res.noStockItems} 条无货`)
    await loadData()
  } catch {
    ElMessage.error('一键报价失败，请稍后重试')
  } finally {
    autoQuoting.value = false
  }
}

// ── 人工报价 / 修改报价 ──
const quoteDialogVisible = ref(false)
const quoteSubmitting = ref(false)
const isEditQuote = ref(false)
const currentItem = ref<BOMItem | null>(null)
const quoteFormRef = ref()
const quoteForm = ref({
  quotedPrice: 0,
  quotedCurrency: 'RMB',
  quotedStock: undefined as number | undefined,
  quotedDeliveryDays: undefined as number | undefined,
  quotedBrand: '',
  quoteRemark: '',
})
const quoteRules = {
  quotedPrice: [{ required: true, message: '请输入报价单价', trigger: 'blur' }],
}

const openManualQuote = (item: BOMItem, isEdit: boolean) => {
  currentItem.value = item
  isEditQuote.value = isEdit
  quoteForm.value = {
    quotedPrice: item.quotedPrice ?? 0,
    quotedCurrency: item.quotedCurrency || item.currency || 'RMB',
    quotedStock: item.quotedStock,
    quotedDeliveryDays: item.quotedDeliveryDays,
    quotedBrand: item.quotedBrand || '',
    quoteRemark: item.quoteRemark || '',
  }
  quoteDialogVisible.value = true
}

const submitQuote = async () => {
  await runValidatedFormSave(quoteFormRef, {
    loading: quoteSubmitting,
    afterValidate: async () => !!currentItem.value,
    task: async () => {
      const item = currentItem.value!
      const payload = {
        quotedPrice: quoteForm.value.quotedPrice,
        quotedCurrency: quoteForm.value.quotedCurrency,
        quotedStock: quoteForm.value.quotedStock,
        quotedDeliveryDays: quoteForm.value.quotedDeliveryDays,
        quotedBrand: quoteForm.value.quotedBrand || undefined,
        quoteRemark: quoteForm.value.quoteRemark || undefined,
      }
      if (isEditQuote.value) {
        await bomApi.updateItemQuote(bomId.value, item.id, payload)
      } else {
        await bomApi.manualQuoteItem(bomId.value, item.id, payload)
      }
      return isEditQuote.value ? ('edit' as const) : ('create' as const)
    },
    formatSuccess: (mode) => (mode === 'edit' ? '报价已更新' : '人工报价成功'),
    onSuccess: async () => {
      quoteDialogVisible.value = false
      await loadData()
    },
    errorMessage: () => '报价提交失败，请稍后重试'
  })
}

// ── 删除 ──
const handleDelete = async () => {
  await ElMessageBox.confirm(`确认删除 BOM「${bom.value?.bomCode}」？此操作不可恢复。`, '删除确认', {
    confirmButtonText: '确认删除', cancelButtonText: '取消', type: 'warning'
  })
  try {
    await bomApi.deleteBOM(bomId.value)
    ElMessage.success('删除成功')
    router.push({ name: 'BOMList' })
  } catch {
    ElMessage.error('删除失败，请稍后重试')
  }
}

onMounted(loadData)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&family=Noto+Sans+SC:wght@300;400;500&display=swap');

.bom-detail-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.loading-state {
  display: flex; align-items: center; gap: 8px; color: #6a7f94; padding: 40px;
  justify-content: center; font-size: 14px;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
}
.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
}
.header-right {
  display: flex;
  align-items: center;
  gap: 10px;
}
.btn-back {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 7px 12px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.2s;
  &:hover {
    background: rgba(255, 255, 255, 0.07);
    color: $text-secondary;
    border-color: rgba(0, 212, 255, 0.2);
  }
}
.bom-caption-title-group {
  display: flex;
  align-items: center;
  gap: 14px;
}
.caption-avatar-lg {
  width: 48px;
  height: 48px;
  flex-shrink: 0;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  font-weight: 700;
  color: $cyan-primary;
  border: 1px solid rgba(0, 212, 255, 0.25);
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.3), rgba(0, 212, 255, 0.2));
}
.page-title-row {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 6px;
}
.page-title-with-icons {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  min-width: 0;
}
.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
  font-family: 'Space Mono', monospace;
}
.caption-meta-line {
  margin-top: 0;
}
.caption-muted {
  font-size: 12px;
  color: $text-muted;
}
.btn-more-actions {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  padding: 0;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  background: rgba(255, 255, 255, 0.04);
  color: $text-muted;
  cursor: pointer;
  transition: all 0.2s;
  &:hover {
    background: rgba(255, 255, 255, 0.08);
    color: $text-secondary;
    border-color: rgba(0, 212, 255, 0.2);
  }
  .btn-more-actions__dots {
    font-size: 18px;
    line-height: 1;
    letter-spacing: 1px;
  }
}

/* ── 基础信息卡片 ── */
.info-cards {
  display: flex; flex-wrap: wrap; gap: 12px; margin-bottom: 16px;
}
.info-card {
  padding: 12px 16px;
  background: rgba(0, 20, 45, 0.6);
  border: 1px solid rgba(0, 212, 255, 0.1);
  border-radius: 8px;
  min-width: 120px;
  .info-label { font-size: 11px; color: #556; margin-bottom: 4px; }
  .info-value { font-size: 14px; color: #c8d8e8; font-weight: 600; }
  .info-value.highlight { color: #00d4ff; }
  .info-value.success { color: #27ae60; }
  .info-value.warning { color: #e6a23c; }
}

/* ── 明细列表 ── */
.items-panel {
  background: rgba(0, 20, 45, 0.6);
  border: 1px solid rgba(0, 212, 255, 0.1);
  border-radius: 10px;
  overflow: hidden;
}
.items-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 14px 18px;
  border-bottom: 1px solid rgba(0, 212, 255, 0.08);
  .items-title {
    display: flex; align-items: center; gap: 8px;
    font-size: 14px; font-weight: 700; color: #e0f0ff;
    .dot { width: 4px; height: 16px; background: linear-gradient(180deg, #00d4ff, #0066cc); border-radius: 2px; }
  }
}

.mpn-text { font-family: 'Courier New', monospace; color: #c8d8e8; font-size: 12px; }
.quoted-price { color: #27ae60; font-weight: 600; }
.text-muted { color: #445; }

.action-btns {
  display: flex; gap: 6px; flex-wrap: nowrap;
  .el-button { white-space: nowrap; }
}

:deep(.items-table) {
  // 无外边框，行间细线分隔，对标客户管理列表风格
  --el-table-border-color: transparent;
  --el-table-header-bg-color: rgba(0, 212, 255, 0.04);
  --el-table-row-hover-bg-color: rgba(0, 212, 255, 0.04);
  --el-table-bg-color: transparent;
  --el-table-tr-bg-color: transparent;
  --el-table-fixed-box-shadow: none;
  background: transparent !important;
  .el-table__inner-wrapper {
    background: transparent;
    &::before { display: none !important; }
    &::after  { display: none !important; }
  }
  .el-table__border-left-patch { display: none !important; }
  .el-table__header-wrapper {
    th.el-table__cell {
      background: rgba(0, 212, 255, 0.04) !important;
      border-bottom: 1px solid rgba(0, 212, 255, 0.1) !important;
      border-right: none !important;
      color: rgba(200, 216, 232, 0.55);
      font-size: 12px;
      font-weight: 500;
      letter-spacing: 0.3px;
    }
  }
  .el-table__row {
    background: transparent !important;
    td.el-table__cell {
      background: transparent !important;
      border-bottom: 1px solid rgba(255, 255, 255, 0.04) !important;
      border-right: none !important;
      color: rgba(224, 244, 255, 0.85);
      font-size: 13px;
    }
    &:last-child td.el-table__cell { border-bottom: none !important; }
    &:hover td.el-table__cell { background: rgba(0, 212, 255, 0.04) !important; }
  }
  .el-table__cell {
    .el-button { white-space: nowrap !important; }
    .cell { white-space: nowrap; }
  }
}

/* ── 报价弹窗 ── */
.quote-item-info {
  padding: 10px 14px;
  background: rgba(0, 212, 255, 0.04);
  border: 1px solid rgba(0, 212, 255, 0.12);
  border-radius: 6px;
  margin-bottom: 16px;
  .qi-row { display: flex; align-items: center; margin-bottom: 4px; font-size: 13px; }
  .qi-label { color: #6a7f94; min-width: 50px; }
  .qi-value { color: #c8d8e8; font-weight: 600; }
  .qi-value.mpn { font-family: 'Courier New', monospace; color: #00d4ff; }
}
.w-full { width: 100%; }

:deep(.quote-dialog) {
  .el-dialog { background: #0d1e35; border: 1px solid rgba(0, 212, 255, 0.2); }
  .el-dialog__title { color: #e0f0ff; }
  .el-form-item__label { color: #8aa0b8; }
  .el-input__wrapper, .el-select .el-input__wrapper, .el-textarea__inner {
    background: rgba(255, 255, 255, 0.04);
    border-color: rgba(255, 255, 255, 0.12);
    color: #c8d8e8;
  }
  .el-input-number { width: 100%; }
}
</style>

<style lang="scss">
@import '@/assets/styles/variables.scss';

.bom-detail-header-more-popper.el-dropdown__popper,
.bom-detail-header-more-popper.el-popper {
  background: $layer-2 !important;
  border: 1px solid rgba(0, 212, 255, 0.15) !important;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.45) !important;
}
.bom-detail-header-more-popper .el-dropdown-menu {
  background: transparent !important;
  border: none !important;
  box-shadow: none !important;
  padding: 4px 0 !important;
}
.bom-detail-header-more-popper .el-dropdown-menu__item {
  color: rgba(200, 220, 240, 0.92) !important;
  font-size: 13px;
  &:hover,
  &:focus {
    background: rgba(0, 212, 255, 0.1) !important;
    color: #e8f4ff !important;
  }
}
.bom-detail-header-more-popper .detail-more-item--danger {
  color: rgba(245, 108, 108, 0.95) !important;
  &:hover,
  &:focus {
    background: rgba(245, 108, 108, 0.12) !important;
    color: #ff9a9a !important;
  }
}
</style>
