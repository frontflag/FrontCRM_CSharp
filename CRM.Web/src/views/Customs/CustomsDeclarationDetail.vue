<template>
  <div class="customs-declaration-detail" v-loading="loading">
    <div class="page-header">
      <h1 class="page-title">{{ t('customsPages.declarations.detailTitle') }}</h1>
      <el-button @click="goBack">{{ t('stockOutDetail.back') }}</el-button>
    </div>

    <template v-if="detail">
      <el-descriptions :column="2" border class="desc-block">
        <el-descriptions-item :label="t('customsPages.declarations.colDecCode')">{{ detail.declarationCode }}</el-descriptions-item>
        <el-descriptions-item :label="t('customsPages.declarations.colBroker')">
          {{ detail.customsBrokerName || detail.customsBrokerCode || detail.customsBrokerId || '—' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('customsPages.declarations.colInternal')">{{ internalLabel(detail.internalStatus) }}</el-descriptions-item>
        <el-descriptions-item :label="t('customsPages.declarations.colClearance')">{{ clearanceLabel(detail.customsClearanceStatus) }}</el-descriptions-item>
        <el-descriptions-item :label="t('customsPages.declarations.colDeclareDate')">{{ formatDate(detail.declareDate) }}</el-descriptions-item>
        <el-descriptions-item :label="t('stockInDetail.exchangeRate')">{{ moneyText(detail.exchangeRate) }}</el-descriptions-item>
        <el-descriptions-item :label="t('customsPages.declarations.colTotal')">{{ moneyText(detail.totalTaxAmount) }}</el-descriptions-item>
        <el-descriptions-item :label="t('stockInDetail.warehouseRoute')">{{ warehouseRoute }}</el-descriptions-item>
        <el-descriptions-item :label="t('stockInDetail.customsPacking')">
          <router-link
            v-if="detail.packingId"
            :to="{ name: 'PackingDetail', params: { id: detail.packingId } }"
            class="cell-link"
          >
            {{ detail.packingCode || detail.packingId }}
          </router-link>
          <span v-else>—</span>
        </el-descriptions-item>
        <el-descriptions-item :label="t('customsPages.declarations.colSor')">
          <router-link
            v-if="detail.stockOutRequestId"
            :to="{ name: 'StockOutNotifyDetail', params: { id: detail.stockOutRequestId } }"
            class="cell-link"
          >
            {{ detail.stockOutRequestId }}
          </router-link>
          <span v-else>—</span>
        </el-descriptions-item>
        <el-descriptions-item :label="t('customsPages.declarations.colRemark')" :span="2">{{ detail.remark || '—' }}</el-descriptions-item>
      </el-descriptions>

      <h4 class="sub-title">{{ t('customsPages.declarations.itemsTitle') }}</h4>
      <el-table v-if="detail.items?.length" :data="detail.items" size="small" border class="items-table">
        <el-table-column prop="lineNo" label="#" width="56" align="center" />
        <el-table-column prop="purchasePn" :label="t('customsPages.items.colPn')" min-width="120" show-overflow-tooltip />
        <el-table-column prop="purchaseBrand" :label="t('customsPages.items.colBrand')" width="96" show-overflow-tooltip />
        <el-table-column prop="hsCode" :label="t('customsPages.items.colHs')" width="100" show-overflow-tooltip />
        <el-table-column prop="declareQty" :label="t('customsPages.items.colQty')" width="90" align="right" />
        <el-table-column :label="t('customsPages.items.colCustomer')" min-width="120" show-overflow-tooltip>
          <template #default="{ row }">{{ maskSale ? '—' : row.customerName || '—' }}</template>
        </el-table-column>
        <el-table-column :label="t('stockInDetail.sellOrderItemCode')" min-width="120" show-overflow-tooltip>
          <template #default="{ row }">{{ maskSale ? '—' : row.sellOrderItemCode || '—' }}</template>
        </el-table-column>
        <el-table-column :label="t('stockInDetail.vendor')" min-width="120" show-overflow-tooltip>
          <template #default="{ row }">{{ maskPurchase ? '—' : row.vendorName || '—' }}</template>
        </el-table-column>
        <el-table-column :label="t('customsPages.items.colUnitPrice')" width="100" align="right">
          <template #default="{ row }">{{ moneyText(row.declareUnitPrice) }}</template>
        </el-table-column>
        <el-table-column :label="t('stockInDetail.originalPrice')" width="100" align="right">
          <template #default="{ row }">{{ unitPriceText(row.originalPurchasePrice) }}</template>
        </el-table-column>
        <el-table-column :label="t('customsPages.items.colTaxUnit')" width="100" align="right">
          <template #default="{ row }">{{ unitPriceText(row.taxIncludedUnitPrice) }}</template>
        </el-table-column>
        <el-table-column :label="t('customsPages.items.colDuty')" width="90" align="right">
          <template #default="{ row }">{{ moneyText(row.dutyAmount) }}</template>
        </el-table-column>
        <el-table-column :label="t('customsPages.items.colVat')" width="90" align="right">
          <template #default="{ row }">{{ moneyText(row.vatAmount) }}</template>
        </el-table-column>
        <el-table-column :label="t('customsPages.items.colGoods')" width="100" align="right">
          <template #default="{ row }">{{ moneyText(row.customsPaymentGoods) }}</template>
        </el-table-column>
        <el-table-column :label="t('customsPages.items.colAgency')" width="100" align="right">
          <template #default="{ row }">{{ moneyText(row.customsAgencyFee) }}</template>
        </el-table-column>
        <el-table-column :label="t('customsPages.items.colOther')" width="80" align="right">
          <template #default="{ row }">{{ moneyText(row.otherFee) }}</template>
        </el-table-column>
        <el-table-column :label="t('customsPages.items.colInspection')" width="90" align="right">
          <template #default="{ row }">{{ moneyText(row.inspectionFee) }}</template>
        </el-table-column>
        <el-table-column :label="t('customsPages.items.colTotalTax')" width="100" align="right">
          <template #default="{ row }">{{ moneyText(row.totalValueTax) }}</template>
        </el-table-column>
      </el-table>
    </template>

    <el-empty v-else-if="!loading" :description="loadError || t('stockOutDetail.notFound')" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { fetchCustomsDeclarationById, type CustomsDeclarationDetailDto } from '@/api/customs'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const { maskPurchaseSensitiveFields: maskPurchase } = usePurchaseSensitiveFieldMask()
const { maskSaleSensitiveFields: maskSale } = useSaleSensitiveFieldMask()
const loading = ref(false)
const loadError = ref('')
const detail = ref<CustomsDeclarationDetailDto | null>(null)

const warehouseRoute = computed(() => {
  const d = detail.value
  if (!d) return '—'
  const from = (d.fromWarehouseCode ?? d.fromWarehouseId ?? '').trim()
  const to = (d.toWarehouseCode ?? d.toWarehouseId ?? '').trim()
  if (from && to) return `${from} → ${to}`
  return from || to || '—'
})

function internalLabel(v: number) {
  if (v === -1) return t('customsPages.declarations.internalVoid')
  const m: Record<number, string> = {
    1: t('customsPages.declarations.internalPending'),
    2: t('customsPages.declarations.internalProcessing'),
    3: t('customsPages.declarations.internalDone')
  }
  return m[v] ?? String(v)
}

function clearanceLabel(v: number) {
  const m: Record<number, string> = {
    0: t('customsPages.declarations.clearanceNone'),
    10: t('customsPages.declarations.clearanceReleased'),
    100: t('customsPages.declarations.clearanceCleared')
  }
  return m[v] ?? String(v)
}

function formatDate(iso: string | undefined) {
  if (!iso) return '—'
  return iso.includes('T') ? iso.slice(0, 10) : iso.slice(0, 10)
}

function moneyText(n: number | null | undefined): string {
  if (maskPurchase.value) return '—'
  const x = Number(n)
  if (!Number.isFinite(x) || x <= 0) return '—'
  return x.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function unitPriceText(n: number | null | undefined): string {
  if (maskPurchase.value) return '—'
  const x = Number(n)
  if (!Number.isFinite(x) || x <= 0) return '—'
  return x.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 6 })
}

function goBack() {
  router.push({ name: 'CustomsDeclarationList' })
}

async function load() {
  const id = typeof route.params.id === 'string' ? route.params.id.trim() : ''
  if (!id) {
    loadError.value = t('stockOutDetail.notFound')
    return
  }
  loading.value = true
  loadError.value = ''
  try {
    detail.value = await fetchCustomsDeclarationById(id)
  } catch (e: unknown) {
    detail.value = null
    loadError.value = e instanceof Error ? e.message : String(e)
    ElMessage.error(loadError.value)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  void load()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.customs-declaration-detail {
  padding: 20px 24px;
}
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}
.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
}
.desc-block {
  margin-bottom: 16px;
}
.sub-title {
  margin: 16px 0 8px;
  font-size: 14px;
  font-weight: 600;
}
.items-table {
  width: 100%;
}
.cell-link {
  color: $cyan-primary;
  text-decoration: none;
  &:hover {
    text-decoration: underline;
  }
}
</style>
