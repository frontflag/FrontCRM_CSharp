<template>
  <div class="so-aggregate-table-wrap sell-order-item-qc-tab-table">
    <el-table v-if="items.length > 0" :data="items" size="small" stripe @row-dblclick="onRowDblClick">
      <el-table-column type="index" width="50" label="#" />
      <el-table-column :label="t('qcList.columns.status')" width="120" align="center">
        <template #default="{ row }">
          <el-tag effect="dark" :type="qcType(row.status)">{{ qcText(row.status) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column :label="t('arrivalNoticeList.columns.arrivalType')" width="140" align="center">
        <template #default="{ row }">
          <StockBizTypeTag
            biz="in"
            :type="row.stockInType"
            :customs-declaration-id="row.customsDeclarationId"
            :customs-declaration-code="row.customsDeclarationCode"
          />
        </template>
      </el-table-column>
      <el-table-column :label="t('qcList.columns.model')" prop="model" min-width="160" show-overflow-tooltip />
      <el-table-column :label="t('qcList.columns.brand')" prop="brand" min-width="120" show-overflow-tooltip />
      <el-table-column :label="t('qcList.columns.vendorName')" min-width="160" show-overflow-tooltip>
        <template #default="{ row }">
          <VendorNameReadonlyText
            :name-zh="row.vendorName"
            :name-en="row.vendorEnglishName"
            :masked="maskPurchaseSensitiveFields"
          />
        </template>
      </el-table-column>
      <el-table-column :label="t('qcList.columns.passQty')" prop="passQty" width="110" align="right" />
      <el-table-column :label="t('qcList.columns.rejectQty')" prop="rejectQty" width="110" align="right" />
      <el-table-column :label="t('qcList.columns.stockInStatus')" width="120" align="center">
        <template #default="{ row }">
          <el-tag effect="dark" :type="stockInStatusTagType(displayStockInStatus(row))">
            {{ stockInText(displayStockInStatus(row)) }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column :label="t('qcList.columns.qcCode')" prop="qcCode" width="160" min-width="160" show-overflow-tooltip />
      <el-table-column
        :label="t('qcList.columns.stockInNotifyCode')"
        prop="stockInNotifyCode"
        width="170"
        show-overflow-tooltip
      />
      <el-table-column
        :label="t('qcList.columns.purchaseOrderCode')"
        prop="purchaseOrderCode"
        width="170"
        show-overflow-tooltip
      />
      <el-table-column
        :label="t('common.freightForwarderOrderNo')"
        prop="freightForwarderOrderNo"
        width="160"
        min-width="140"
        show-overflow-tooltip
      />
      <el-table-column
        :label="t('qcList.columns.salesOrderCode')"
        prop="salesOrderCode"
        width="170"
        show-overflow-tooltip
      />
      <el-table-column :label="t('qcList.columns.createTime')" width="170">
        <template #default="{ row }">{{ formatTime(row.createTime) }}</template>
      </el-table-column>
      <el-table-column :label="t('qcList.columns.createUser')" width="120" show-overflow-tooltip>
        <template #default="{ row }">
          {{ row.createUserName || row.CreateUserName || row.createdBy || t('quoteList.na') }}
        </template>
      </el-table-column>
    </el-table>
    <DetailListPanelEmpty v-else size="low" :description="emptyText ?? t('sellOrderItemQcTab.empty')" />
  </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import type { QcInfoDto } from '@/api/logistics'
import { formatDisplayDateTime2DigitYear } from '@/utils/displayDateTime'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import VendorNameReadonlyText from '@/components/Vendor/VendorNameReadonlyText.vue'
import DetailListPanelEmpty from '@/components/Common/DetailListPanelEmpty.vue'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'

withDefaults(
  defineProps<{
    items: QcInfoDto[]
    emptyText?: string
  }>(),
  {
    items: () => []
  }
)

const { t } = useI18n()
const router = useRouter()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()

function formatTime(v?: string) {
  return v != null && String(v).length > 0 ? formatDisplayDateTime2DigitYear(String(v)) : t('quoteList.na')
}

function qcText(s: number) {
  const keyMap: Record<number, 'failed' | 'partial' | 'passed'> = {
    [-1]: 'failed',
    10: 'partial',
    100: 'passed'
  }
  const k = keyMap[s]
  return k ? t(`qcList.qcStatus.${k}`) : t('qcList.qcStatus.unknown')
}

function qcType(s: number) {
  return ({ [-1]: 'danger', 10: 'warning', 100: 'success' } as Record<number, string>)[s] || 'info'
}

function stockInText(s: number | undefined) {
  const keyMap: Record<number, 'rejected' | 'notStocked' | 'partial' | 'all'> = {
    [-1]: 'rejected',
    1: 'notStocked',
    10: 'partial',
    100: 'all'
  }
  if (s === undefined || s === null) return t('qcList.stockInStatus.unknown')
  const k = keyMap[s]
  return k ? t(`qcList.stockInStatus.${k}`) : t('qcList.stockInStatus.unknown')
}

function stockInStatusTagType(s: number | undefined) {
  return s === undefined || s === null
    ? 'info'
    : ({ [-1]: 'danger', 1: 'info', 10: 'warning', 100: 'success' } as Record<number, string>)[s] || 'info'
}

function displayStockInStatus(row: QcInfoDto) {
  if (row.status === -1) return -1
  if (!row.stockInId) return 1
  return row.stockInStatus
}

function onRowDblClick(row: QcInfoDto) {
  const id = String(row?.id ?? '').trim()
  if (!id) return
  void router.push({ name: 'QcCreate', query: { qcId: id } })
}
</script>
