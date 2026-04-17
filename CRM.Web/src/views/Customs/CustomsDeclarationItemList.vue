<template>
  <div class="customs-page">
    <div class="page-header">
      <h1 class="page-title">{{ t('customsPages.items.title') }}</h1>
    </div>
    <el-form :inline="true" class="filter-form" @submit.prevent>
      <el-form-item :label="t('customsPages.items.filterDecCode')">
        <el-input v-model="filters.declarationCode" clearable style="width: 160px" />
      </el-form-item>
      <el-form-item :label="t('customsPages.items.filterPn')">
        <el-input v-model="filters.purchasePn" clearable style="width: 140px" />
      </el-form-item>
      <el-form-item :label="t('customsPages.items.filterCustomer')">
        <el-input v-model="filters.customer" clearable style="width: 160px" />
      </el-form-item>
      <el-form-item :label="t('customsPages.items.filterSales')">
        <el-input v-model="filters.salesUserId" clearable style="width: 160px" />
      </el-form-item>
      <el-form-item :label="t('customsPages.items.filterSoLine')">
        <el-input v-model="filters.sellOrderItemCode" clearable style="width: 160px" />
      </el-form-item>
      <el-form-item :label="t('customsPages.items.filterSor')">
        <el-input v-model="filters.stockOutRequestId" clearable style="width: 180px" />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" @click="load">{{ t('customsPages.declarations.search') }}</el-button>
      </el-form-item>
    </el-form>

    <el-table :data="list" v-loading="loading" stripe border class="data-table" @row-dblclick="onDblClick">
      <el-table-column prop="declareDate" :label="t('customsPages.items.colDeclareDate')" width="110">
        <template #default="{ row }">{{ fmtDate(row.declareDate) }}</template>
      </el-table-column>
      <el-table-column prop="declarationCode" :label="t('customsPages.items.colDecCode')" width="130" />
      <el-table-column prop="customerName" :label="t('customsPages.items.colCustomer')" min-width="140" show-overflow-tooltip />
      <el-table-column prop="salesUserName" :label="t('customsPages.items.colSales')" width="100" />
      <el-table-column prop="purchasePn" :label="t('customsPages.items.colPn')" min-width="120" />
      <el-table-column prop="purchaseBrand" :label="t('customsPages.items.colBrand')" width="100" />
      <el-table-column prop="declareQty" :label="t('customsPages.items.colQty')" width="90" align="right" />
      <el-table-column prop="declareUnitPrice" :label="t('customsPages.items.colUnitPrice')" width="110" align="right" />
      <el-table-column prop="dutyAmount" :label="t('customsPages.items.colDuty')" width="90" align="right" />
      <el-table-column prop="vatAmount" :label="t('customsPages.items.colVat')" width="90" align="right" />
      <el-table-column prop="customsPaymentGoods" :label="t('customsPages.items.colGoods')" width="100" align="right" />
      <el-table-column prop="customsAgencyFee" :label="t('customsPages.items.colAgency')" width="100" align="right" />
      <el-table-column prop="otherFee" :label="t('customsPages.items.colOther')" width="80" align="right" />
      <el-table-column prop="inspectionFee" :label="t('customsPages.items.colInspection')" width="90" align="right" />
      <el-table-column prop="totalValueTax" :label="t('customsPages.items.colTotalTax')" width="100" align="right" />
      <el-table-column prop="taxIncludedUnitPrice" :label="t('customsPages.items.colTaxUnit')" width="110" align="right" />
      <el-table-column prop="createTime" :label="t('customsPages.items.colCreateTime')" width="165">
        <template #default="{ row }">{{ fmtDt(row.createTime) }}</template>
      </el-table-column>
      <el-table-column prop="createUserDisplay" :label="t('customsPages.items.colCreator')" width="100" />
    </el-table>

    <el-dialog v-model="detailVisible" :title="t('customsPages.items.detailTitle')" width="640px" destroy-on-close>
      <el-descriptions v-if="detailRow" :column="1" border>
        <el-descriptions-item :label="t('customsPages.items.colDecCode')">{{ detailRow.declarationCode }}</el-descriptions-item>
        <el-descriptions-item :label="t('customsPages.items.colSor')">{{ detailRow.stockOutRequestId }}</el-descriptions-item>
        <el-descriptions-item :label="t('customsPages.items.colCustomer')">{{ detailRow.customerName || detailRow.customerId || '—' }}</el-descriptions-item>
        <el-descriptions-item :label="t('customsPages.items.colSales')">{{ detailRow.salesUserName || detailRow.salesUserId || '—' }}</el-descriptions-item>
        <el-descriptions-item :label="t('customsPages.items.filterSoLine')">{{ detailRow.sellOrderItemCode || '—' }}</el-descriptions-item>
        <el-descriptions-item :label="t('customsPages.items.colPn')">{{ detailRow.purchasePn || '—' }}</el-descriptions-item>
        <el-descriptions-item :label="t('customsPages.items.colBrand')">{{ detailRow.purchaseBrand || '—' }}</el-descriptions-item>
        <el-descriptions-item :label="t('customsPages.items.colQty')">{{ detailRow.declareQty }}</el-descriptions-item>
        <el-descriptions-item :label="t('customsPages.items.colTotalTax')">{{ detailRow.totalValueTax }}</el-descriptions-item>
      </el-descriptions>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { fetchCustomsDeclarationItems, type CustomsDeclarationItemListItemDto } from '@/api/customs'

const { t } = useI18n()
const loading = ref(false)
const list = ref<CustomsDeclarationItemListItemDto[]>([])
const filters = reactive({
  declarationCode: '',
  purchasePn: '',
  customer: '',
  salesUserId: '',
  sellOrderItemCode: '',
  stockOutRequestId: ''
})
const detailVisible = ref(false)
const detailRow = ref<CustomsDeclarationItemListItemDto | null>(null)

function fmtDate(iso: string) {
  return iso ? iso.slice(0, 10) : '—'
}
function fmtDt(iso: string) {
  return iso ? iso.replace('T', ' ').slice(0, 19) : '—'
}

async function load() {
  loading.value = true
  try {
    const params: Record<string, unknown> = { take: 500 }
    if (filters.declarationCode.trim()) params.declarationCode = filters.declarationCode.trim()
    if (filters.purchasePn.trim()) params.purchasePn = filters.purchasePn.trim()
    if (filters.customer.trim()) params.customer = filters.customer.trim()
    if (filters.salesUserId.trim()) params.salesUserId = filters.salesUserId.trim()
    if (filters.sellOrderItemCode.trim()) params.sellOrderItemCode = filters.sellOrderItemCode.trim()
    if (filters.stockOutRequestId.trim()) params.stockOutRequestId = filters.stockOutRequestId.trim()
    list.value = await fetchCustomsDeclarationItems(params)
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  } finally {
    loading.value = false
  }
}

function onDblClick(row: CustomsDeclarationItemListItemDto) {
  detailRow.value = row
  detailVisible.value = true
}

onMounted(() => {
  void load()
})
</script>

<style scoped>
.customs-page {
  padding: 20px 24px;
}
.page-header {
  margin-bottom: 16px;
}
.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
}
.filter-form {
  margin-bottom: 12px;
}
.data-table {
  width: 100%;
}
</style>
