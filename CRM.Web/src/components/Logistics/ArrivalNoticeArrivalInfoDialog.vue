<template>
  <el-dialog
    :model-value="visible"
    :title="t('arrivalNoticeList.arrivalInfoDialog.title')"
    width="520px"
    destroy-on-close
    @update:model-value="emit('update:visible', $event)"
  >
    <el-form label-position="top" @submit.prevent>
      <el-row :gutter="12">
        <ShipmentExpressFields
          v-model:shipment-method="form.shipmentMethod"
          v-model:express-company="form.expressCompany"
          :shipment-label="t('arrivalNoticeList.columns.expectedArrivalMethod')"
          :express-label="t('arrivalNoticeList.arrivalInfoDialog.expressCompany')"
          :placeholder="t('arrivalNoticeList.arrivalInfoDialog.selectPlaceholder')"
          :shipment-required="false"
          :shipment-clearable="true"
          :col-span="24"
        />
        <el-col :span="24">
          <el-form-item :label="t('arrivalNoticeList.columns.expectedArrivalExpressNo')">
            <el-input v-model="form.courierTrackingNo" clearable />
          </el-form-item>
        </el-col>
      </el-row>
    </el-form>
    <template #footer>
      <el-button @click="emit('update:visible', false)">{{ t('arrivalNoticeList.arrivalInfoDialog.cancel') }}</el-button>
      <el-button type="primary" :loading="saving" @click="submit">{{ t('arrivalNoticeList.arrivalInfoDialog.save') }}</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { logisticsApi, type StockInNotifyDto } from '@/api/logistics'
import { getApiErrorMessage } from '@/utils/apiError'
import { isExpressShipmentMethod } from '@/composables/useLogisticsFormDict'
import ShipmentExpressFields from '@/components/Logistics/ShipmentExpressFields.vue'

const props = defineProps<{
  visible: boolean
  notice: StockInNotifyDto | null
}>()

const emit = defineEmits<{
  'update:visible': [v: boolean]
  saved: [row: StockInNotifyDto]
}>()

const { t } = useI18n()
const saving = ref(false)
const form = reactive({
  shipmentMethod: '',
  expressCompany: '',
  courierTrackingNo: ''
})

watch(
  () => [props.visible, props.notice] as const,
  ([open, notice]) => {
    if (!open || !notice) return
    form.shipmentMethod = String(notice.shipmentMethod ?? '').trim()
    form.expressCompany = String(notice.expressCompany ?? '').trim()
    form.courierTrackingNo = String(notice.courierTrackingNo ?? '').trim()
  }
)

async function submit() {
  const id = props.notice?.id?.trim()
  if (!id) return
  saving.value = true
  try {
    const express = isExpressShipmentMethod(form.shipmentMethod) ? form.expressCompany.trim() : ''
    const updated = await logisticsApi.updateArrivalInfo(id, {
      shipmentMethod: form.shipmentMethod.trim() || null,
      expressCompany: express || null,
      courierTrackingNo: form.courierTrackingNo.trim() || null
    })
    ElMessage.success(t('arrivalNoticeList.messages.arrivalInfoSaved'))
    emit('saved', updated)
    emit('update:visible', false)
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('arrivalNoticeList.messages.arrivalInfoFailed')))
  } finally {
    saving.value = false
  }
}
</script>
