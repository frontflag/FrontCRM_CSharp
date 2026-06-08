<template>
  <div class="packing-create-page packing-detail-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">箱</div>
          <h1 class="page-title">{{ t('packingCreate.title') }}</h1>
          <span v-if="draft" class="count-badge">{{ t('packingCreate.lineCount', { count: draft.lines.length }) }}</span>
        </div>
      </div>
      <div class="header-right">
        <button type="button" class="btn-secondary" :disabled="submitting" @click="goBack">
          {{ t('packingCreate.back') }}
        </button>
        <button type="button" class="btn-primary" :disabled="loading || submitting || !draft" @click="handleSubmit">
          {{ submitting ? t('packingCreate.submitting') : t('packingCreate.submit') }}
        </button>
      </div>
    </div>

    <el-skeleton v-if="loading" :rows="10" animated />
    <template v-else-if="draft">
      <div class="detail-card">
        <h3 class="section-title">{{ t('packingCreate.sectionSummary') }}</h3>
        <el-descriptions :column="2" border>
          <el-descriptions-item :label="t('packingList.columns.customerName')">
            {{ maskSaleSensitiveFields ? '—' : (draft.customerName?.trim() || '—') }}
          </el-descriptions-item>
          <el-descriptions-item :label="t('packingList.columns.salesUserName')">
            {{ maskSaleSensitiveFields ? '—' : (draft.salesUserName?.trim() || '—') }}
          </el-descriptions-item>
          <el-descriptions-item :label="t('packingList.columns.itemRows')">{{ draft.lines.length }}</el-descriptions-item>
          <el-descriptions-item :label="t('packingDetail.stockOutType')">
            <StockBizTypeTag biz="out" :type="draft.stockOutType ?? StockOutTypeCode.Sales" />
          </el-descriptions-item>
          <el-descriptions-item v-if="isCustomsPacking" :label="t('packingCreate.customsBroker')">
            <el-select
              v-model="customsBrokerId"
              filterable
              clearable
              :placeholder="t('packingCreate.customsBrokerPlaceholder')"
              style="width: 280px"
            >
              <el-option
                v-for="b in customsBrokers"
                :key="b.id"
                :label="b.cname"
                :value="b.id"
              />
            </el-select>
          </el-descriptions-item>
          <el-descriptions-item :label="t('packingList.columns.warehouseName')">
            {{ draftWarehouseDisplay || '—' }}
          </el-descriptions-item>
          <el-descriptions-item :label="t('packingList.columns.shipmentMethod')">
            {{ shipmentMethodDisplay(shipmentMethod) }}
          </el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.detail.expressCompany')">
            {{ expressCompanyDisplay(expressCompany) }}
          </el-descriptions-item>
        </el-descriptions>
      </div>

      <div class="detail-card packing-extend-card">
        <h3 class="section-title">{{ t('packingDetail.sectionExtend') }}</h3>
        <el-tabs v-model="packingExtendTab" type="border-card" class="packing-extend-tabs">
          <el-tab-pane :label="t('packingDetail.tabs.shipAddress')" name="ship">
            <el-form label-width="100px" class="packing-extend-form">
              <el-row :gutter="16">
                <el-col :span="12">
                  <el-form-item :label="t('packingDetail.shipCompany')">
                    <el-input v-model="shipForm.company" clearable />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item :label="t('packingDetail.shipTel')">
                    <el-input v-model="shipForm.tel" clearable />
                  </el-form-item>
                </el-col>
                <el-col :span="24">
                  <el-form-item :label="t('packingDetail.shipAddress')">
                    <el-input v-model="shipForm.address" type="textarea" :rows="2" />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item :label="t('packingDetail.shipAttn')">
                    <el-input v-model="shipForm.attn" clearable />
                  </el-form-item>
                </el-col>
              </el-row>
            </el-form>
          </el-tab-pane>
          <el-tab-pane :label="t('packingDetail.tabs.billAddress')" name="bill">
            <el-form label-width="100px" class="packing-extend-form">
              <el-row :gutter="16">
                <el-col :span="12">
                  <el-form-item :label="t('packingDetail.billCompany')">
                    <el-input v-model="billForm.company" clearable />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item :label="t('packingDetail.billTel')">
                    <el-input v-model="billForm.tel" clearable />
                  </el-form-item>
                </el-col>
                <el-col :span="24">
                  <el-form-item :label="t('packingDetail.billAddress')">
                    <el-input v-model="billForm.address" type="textarea" :rows="2" />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item :label="t('packingDetail.billAttn')">
                    <el-input v-model="billForm.attn" clearable />
                  </el-form-item>
                </el-col>
              </el-row>
            </el-form>
          </el-tab-pane>
          <el-tab-pane :label="t('packingDetail.tabs.deliveryReq')" name="deliveryReq">
            <el-form label-width="100px" class="packing-extend-form">
              <el-row :gutter="16">
                <ShipmentExpressFields
                  v-model:shipment-method="shipmentMethod"
                  v-model:express-company="expressCompany"
                />
              </el-row>
              <el-form-item :label="t('packingDetail.deliveryReq')">
                <el-input v-model="deliveryReq" type="textarea" :rows="4" />
              </el-form-item>
            </el-form>
          </el-tab-pane>
          <el-tab-pane :label="t('packingDetail.tabs.boxParams')" name="box">
            <el-form label-width="100px" class="packing-extend-form">
              <el-row :gutter="16">
                <el-col :span="12">
                  <el-form-item :label="t('packingDetail.boxNw')">
                    <el-input-number v-model="boxForm.nw" :min="0" :precision="4" :controls="false" class="packing-num-input" />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item :label="t('packingDetail.boxGw')">
                    <el-input-number v-model="boxForm.gw" :min="0" :precision="4" :controls="false" class="packing-num-input" />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item :label="t('packingDetail.boxDim')">
                    <el-input v-model="boxForm.dim" clearable />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item :label="t('packingDetail.boxCtns')">
                    <el-input-number v-model="boxForm.ctns" :min="0" :precision="0" :controls="false" class="packing-num-input" />
                  </el-form-item>
                </el-col>
              </el-row>
            </el-form>
          </el-tab-pane>
        </el-tabs>
      </div>

      <div class="detail-card">
        <h3 class="section-title">{{ t('packingDetail.sectionLines') }}</h3>
        <el-table :data="draft.lines" border class="lines-table" size="small" :empty-text="t('packingDetail.linesEmpty')">
          <el-table-column :label="t('stockOutNotifyList.columns.requestCode')" prop="requestCode" min-width="140" show-overflow-tooltip />
          <el-table-column :label="t('packingItemList.columns.pn')" prop="pn" min-width="140" show-overflow-tooltip />
          <el-table-column :label="t('packingItemList.columns.brand')" prop="brand" min-width="120" show-overflow-tooltip />
          <el-table-column :label="t('packingItemList.columns.qty')" prop="qty" width="88" align="right" />
          <el-table-column :label="t('packingItemList.columns.sellOrderCode')" min-width="140" show-overflow-tooltip>
            <template #default="{ row }">{{ row.sellOrderCode || '—' }}</template>
          </el-table-column>
          <el-table-column :label="t('packingDetail.comment')" prop="remark" min-width="120" show-overflow-tooltip>
            <template #default="{ row }">{{ row.remark?.trim() || '—' }}</template>
          </el-table-column>
        </el-table>
      </div>
    </template>
    <el-empty v-else :description="t('packingCreate.invalidSelection')" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import {
  packingApi,
  type PackingCreateExtras,
  type PackingDraftFromStockOutRequests
} from '@/api/packing'
import { StockOutTypeCode } from '@/constants/stockOutType'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import { customerAddressApi, customerApi, normalizeCustomerAddressFromApi } from '@/api/customer'
import {
  firstCustomerAddressByType,
  mapCustomerAddressToPackingFields,
  type PackingAddressFields
} from '@/utils/packingCustomerAddress'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { useStockOutNotifyListBasketStore } from '@/stores/stockOutNotifyListBasket'
import { inventoryCenterApi, type WarehouseInfo } from '@/api/inventoryCenter'
import { fetchCustomsBrokersAdmin, type CustomsBrokerDto } from '@/api/customs'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import ShipmentExpressFields from '@/components/Logistics/ShipmentExpressFields.vue'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const { ensureLoaded: ensureLogisticsDict, shipmentArrivalOptions, expressOptions } = useLogisticsFormDict()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const basketStore = useStockOutNotifyListBasketStore()

const loading = ref(false)
const submitting = ref(false)
const draft = ref<PackingDraftFromStockOutRequests | null>(null)
const warehouses = ref<WarehouseInfo[]>([])
const requestIds = ref<string[]>([])

const draftWarehouseDisplay = computed(() => {
  const d = draft.value
  if (!d) return ''
  const fromApi = d.warehouseName?.trim()
  if (fromApi) return fromApi
  const wid = d.warehouseId?.trim()
  if (!wid) return ''
  const w = warehouses.value.find((x) => x.id === wid)
  if (!w) return wid
  const name = (w.warehouseName || '').trim()
  const code = (w.warehouseCode || '').trim()
  if (name && code) return `${name}（${code}）`
  return name || code || wid
})
const packingExtendTab = ref<'ship' | 'bill' | 'deliveryReq' | 'box'>('ship')

const shipForm = reactive<PackingAddressFields>({ company: '', address: '', attn: '', tel: '' })
const billForm = reactive<PackingAddressFields>({ company: '', address: '', attn: '', tel: '' })
const deliveryReq = ref('')
/** 出货方式 / 快递公司：初始来自出库通知，用户可改，写入 packing_extend_ship */
const shipmentMethod = ref('')
const expressCompany = ref('')
const boxForm = reactive({
  nw: undefined as number | undefined,
  gw: undefined as number | undefined,
  dim: '',
  ctns: undefined as number | undefined
})
const customsBrokers = ref<CustomsBrokerDto[]>([])
const customsBrokerId = ref('')

const isCustomsPacking = computed(
  () => (draft.value?.stockOutType ?? StockOutTypeCode.Sales) === StockOutTypeCode.Customs
)

function shipmentMethodDisplay(code?: string | null): string {
  const c = String(code ?? '').trim()
  if (!c) return '—'
  const hit = shipmentArrivalOptions.value.find((o) => String(o.value) === c)
  return hit?.label ?? c
}

function expressCompanyDisplay(code?: string | null): string {
  const c = String(code ?? '').trim()
  if (!c) return '—'
  const hit = expressOptions.value.find((o) => String(o.value) === c)
  return hit?.label ?? c
}

function parseRequestIdsFromRoute(): string[] {
  const raw = String(route.query.ids || '').trim()
  if (!raw) return []
  return raw
    .split(',')
    .map((x) => x.trim())
    .filter(Boolean)
}

function applyAddressDefaults(companyName: string) {
  const company = companyName.trim()
  const ship = mapCustomerAddressToPackingFields(
    firstCustomerAddressByType(customerAddresses, 'Shipping'),
    company
  )
  const bill = mapCustomerAddressToPackingFields(
    firstCustomerAddressByType(customerAddresses, 'Billing'),
    company
  )
  Object.assign(shipForm, ship)
  Object.assign(billForm, bill)
}

let customerAddresses: ReturnType<typeof normalizeCustomerAddressFromApi>[] = []

async function loadPage() {
  requestIds.value = parseRequestIdsFromRoute()
  if (!requestIds.value.length) {
    draft.value = null
    return
  }
  loading.value = true
  try {
    const preview = await packingApi.previewFromStockOutRequests(requestIds.value)
    if (!preview.warehouseName?.trim() && preview.warehouseId?.trim()) {
      try {
        warehouses.value = await inventoryCenterApi.getWarehouses()
      } catch (e) {
        console.warn('load warehouses for draft display failed', e)
      }
    }
    draft.value = preview
    shipmentMethod.value = String(preview.shipmentMethod ?? '').trim()
    expressCompany.value = String(preview.expressCompany ?? '').trim()
    if ((preview.stockOutType ?? StockOutTypeCode.Sales) === StockOutTypeCode.Customs) {
      try {
        customsBrokers.value = await fetchCustomsBrokersAdmin()
      } catch (e) {
        console.warn('load customs brokers failed', e)
      }
    }
    const customerId = String(preview.customerId || '').trim()
    let companyName = preview.customerName?.trim() || ''
    customerAddresses = []
    if (customerId) {
      try {
        const [addrsRaw, customer] = await Promise.all([
          customerAddressApi.getAddressesByCustomerId(customerId),
          customerApi.getCustomerById(customerId).catch(() => null)
        ])
        customerAddresses = (Array.isArray(addrsRaw) ? addrsRaw : []).map(normalizeCustomerAddressFromApi)
        if (customer) {
          companyName =
            customer.customerName?.trim() ||
            customer.customerShortName?.trim() ||
            customer.englishOfficialName?.trim() ||
            companyName
        }
      } catch (e) {
        console.warn('load customer addresses failed', e)
      }
    }
    applyAddressDefaults(companyName)
  } catch (e) {
    console.error(e)
    draft.value = null
    ElMessage.error(e instanceof Error ? e.message : t('packingCreate.loadFailed'))
  } finally {
    loading.value = false
  }
}

function buildExtras(): PackingCreateExtras {
  return {
    ship: {
      shipCompany: shipForm.company.trim() || null,
      shipAddress: shipForm.address.trim() || null,
      shipAttn: shipForm.attn.trim() || null,
      shipTel: shipForm.tel.trim() || null,
      billCompany: billForm.company.trim() || null,
      billAddress: billForm.address.trim() || null,
      billAttn: billForm.attn.trim() || null,
      billTel: billForm.tel.trim() || null,
      deliveryReq: deliveryReq.value.trim() || null,
      shipmentMethod: shipmentMethod.value.trim() || null,
      expressCompany: expressCompany.value.trim() || null
    },
    box: {
      nw: boxForm.nw ?? null,
      gw: boxForm.gw ?? null,
      dim: boxForm.dim.trim() || null,
      ctns: boxForm.ctns ?? null
    },
    customsBrokerId: isCustomsPacking.value ? customsBrokerId.value.trim() || null : null
  }
}

async function handleSubmit() {
  if (!draft.value || !requestIds.value.length) return
  if (!shipmentMethod.value.trim()) {
    ElMessage.warning(t('packingCreate.shipmentMethodRequired'))
    packingExtendTab.value = 'deliveryReq'
    return
  }
  if (isCustomsPacking.value && !customsBrokerId.value.trim()) {
    ElMessage.warning(t('packingCreate.customsBrokerRequired'))
    return
  }
  submitting.value = true
  try {
    const result = await packingApi.createFromStockOutRequests(requestIds.value, buildExtras())
    ElMessage.success(t('packingCreate.createSuccess', { code: result.packingCode }))
    basketStore.clear()
    await router.push({ name: 'PackingDetail', params: { id: result.packingId } })
  } catch (e) {
    ElMessage.error(e instanceof Error ? e.message : t('packingCreate.createFailed'))
  } finally {
    submitting.value = false
  }
}

function goBack() {
  router.push({ name: 'InventoryStockOutNotifyList' })
}

onMounted(() => {
  void ensureLogisticsDict()
  void loadPage()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.packing-create-page {
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

.header-left {
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
  font-size: 14px;
  font-weight: 600;
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
}

.count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}

.btn-secondary {
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  cursor: pointer;
  border: 1px solid $border-panel;
  background: rgba(255, 255, 255, 0.05);
  color: $text-secondary;

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}

.detail-card {
  margin-bottom: 16px;
  padding: 16px;
  border-radius: 10px;
  border: 1px solid $border-panel;
  background: $layer-2;
}

.section-title {
  margin: 0 0 12px;
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
}

.packing-extend-tabs {
  --el-tabs-header-height: 40px;
}

.packing-extend-tabs :deep(.el-tabs__content) {
  padding: 12px 4px 4px;
}

.lines-table {
  width: 100%;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 10px;
}

.btn-primary {
  padding: 8px 16px;
  border-radius: $border-radius-md;
  font-size: 13px;
  cursor: pointer;
  border: 1px solid rgba(0, 212, 255, 0.4);
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  color: #fff;

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}

.packing-extend-form {
  padding: 4px 8px 8px;
}

.packing-extend-form :deep(.el-input-number.packing-num-input) {
  width: 100%;
}

.packing-extend-form :deep(.el-input-number .el-input__inner) {
  text-align: left;
}
</style>
