<template>
  <div class="so-aggregate-table-wrap sell-order-item-packing-tab-table">
    <el-table v-if="items.length > 0" :data="items" size="small" stripe @row-dblclick="onRowDblClick">
      <el-table-column type="index" width="50" label="#" />
      <el-table-column min-width="160" :label="t('packingList.columns.packingCode')">
        <template #default="{ row }">
          <router-link class="so-tab-link" :to="`/inventory/packing/${row.id}`">{{ row.code?.trim() || '—' }}</router-link>
        </template>
      </el-table-column>
      <el-table-column :label="t('packingList.columns.status')" width="110" align="center">
        <template #default="{ row }">
          <span :class="['status-badge', `packing-status-${row.status}`]">{{ packingStatusLabel(row.status) }}</span>
        </template>
      </el-table-column>
      <el-table-column :label="t('packingList.columns.stockOutType')" width="140" align="center">
        <template #default="{ row }">
          <StockBizTypeTag
            biz="out"
            :type="row.stockOutType"
            :customs-declaration-id="row.customsDeclarationId"
            :customs-declaration-code="row.customsDeclarationCode"
          />
        </template>
      </el-table-column>
      <el-table-column :label="t('packingList.columns.materialType')" width="120" align="center">
        <template #default="{ row }">{{ packingMaterialTypeLabel(row.materialType) }}</template>
      </el-table-column>
      <el-table-column
        v-if="showCustomerIdentityFields"
        :label="t('packingList.columns.customerName')"
        min-width="140"
        show-overflow-tooltip
      >
        <template #default="{ row }">{{ row.customerName?.trim() || '—' }}</template>
      </el-table-column>
      <el-table-column
        v-if="showCustomerIdentityFields"
        :label="t('packingList.columns.salesUserName')"
        width="130"
        show-overflow-tooltip
      >
        <template #default="{ row }">{{ row.salesUserName?.trim() || '—' }}</template>
      </el-table-column>
      <el-table-column :label="t('packingList.columns.warehouseName')" min-width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ row.warehouseName?.trim() || '—' }}</template>
      </el-table-column>
      <el-table-column :label="t('packingList.columns.expectedShipDate')" width="160">
        <template #default="{ row }">{{ formatDateTime(row.requestDate) }}</template>
      </el-table-column>
      <el-table-column :label="t('packingList.columns.shipmentMethod')" width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ shipmentMethodDisplay(row.shipmentMethod) }}</template>
      </el-table-column>
      <el-table-column :label="t('packingList.columns.expressCompany')" width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ expressCompanyDisplay(row.expressCompany) }}</template>
      </el-table-column>
      <el-table-column :label="t('packingList.columns.itemRows')" width="100" align="right" prop="itemRows" />
      <el-table-column :label="t('packingList.columns.remark')" min-width="160" show-overflow-tooltip>
        <template #default="{ row }">{{ row.comment?.trim() || '—' }}</template>
      </el-table-column>
      <el-table-column :label="t('packingList.columns.createTime')" width="170">
        <template #default="{ row }">{{ formatDateTime(row.createTime) }}</template>
      </el-table-column>
      <el-table-column :label="t('packingList.columns.createUserName')" width="140" show-overflow-tooltip>
        <template #default="{ row }">{{ row.createUserName?.trim() || '—' }}</template>
      </el-table-column>
    </el-table>
    <DetailListPanelEmpty v-else size="low" :description="emptyText ?? t('sellOrderItemPackingTab.empty')" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { packingMaterialTypeLabel, packingStatusLabel } from '@/api/packing'
import type { SellOrderItemPackingTabRow } from '@/api/salesOrder'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import DetailListPanelEmpty from '@/components/Common/DetailListPanelEmpty.vue'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { useAuthStore } from '@/stores/auth'

withDefaults(
  defineProps<{
    items: SellOrderItemPackingTabRow[]
    emptyText?: string
  }>(),
  {
    items: () => []
  }
)

const { t } = useI18n()
const router = useRouter()
const authStore = useAuthStore()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const { ensureLoaded: ensureLogisticsDict, shipmentArrivalOptions, expressOptions } = useLogisticsFormDict()

const showCustomerIdentityFields = computed(
  () => authStore.hasPermission('customer.info.read') && !maskSaleSensitiveFields.value
)

const arrivalLabelByCode = computed(() => {
  const m = new Map<string, string>()
  for (const o of shipmentArrivalOptions.value) {
    const k = String(o.value ?? '').trim()
    if (k) m.set(k.toLowerCase(), o.label)
  }
  return m
})

const expressLabelByCode = computed(() => {
  const m = new Map<string, string>()
  for (const o of expressOptions.value) {
    const k = String(o.value ?? '').trim()
    if (k) m.set(k.toLowerCase(), o.label)
  }
  return m
})

function formatDateTime(v?: string | null | number) {
  return v != null && String(v).length > 0 ? formatDisplayDateTime(String(v)) : '—'
}

function shipmentMethodDisplay(code?: string | number | null): string {
  if (code === null || code === undefined || code === '') return '—'
  const c = String(code).trim()
  if (!c) return '—'
  return arrivalLabelByCode.value.get(c.toLowerCase()) ?? c
}

function expressCompanyDisplay(code?: string | null): string {
  const c = String(code ?? '').trim()
  if (!c) return '—'
  return expressLabelByCode.value.get(c.toLowerCase()) ?? c
}

function onRowDblClick(row: SellOrderItemPackingTabRow) {
  const id = String(row?.id ?? '').trim()
  if (!id) return
  void router.push(`/inventory/packing/${id}`)
}

onMounted(() => {
  void ensureLogisticsDict()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.so-tab-link {
  color: $cyan-primary;
  text-decoration: none;
  font-weight: 500;
  &:hover {
    text-decoration: underline;
  }
}

.status-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 56px;
  padding: 3px 10px;
  border-radius: 5px;
  font-size: 12px;
  line-height: 1.1;
  font-weight: 400;
  color: #fff;
  border: none;
  white-space: nowrap;

  &.packing-status-10 {
    background: #909399;
  }
  &.packing-status-20 {
    background: #409eff;
  }
  &.packing-status-30 {
    background: #e6a23c;
  }
  &.packing-status-40 {
    background: #67c23a;
  }
  &.packing-status-50 {
    background: #e6a23c;
  }
  &.packing-status-100 {
    background: #67c23a;
  }
}
</style>
