<template>
  <el-dialog
    v-model="visibleModel"
    :title="t('aiBusinessCard.confirmDialog.title')"
    width="920px"
    destroy-on-close
    :close-on-click-modal="false"
    class="bc-confirm-dialog"
  >
    <div class="bc-confirm-dialog__layout">
      <div v-if="displayPreviewUrls.length" class="bc-confirm-dialog__preview">
        <div
          v-for="(url, idx) in displayPreviewUrls"
          :key="idx"
          class="bc-confirm-dialog__preview-item"
        >
          <span v-if="displayPreviewUrls.length > 1" class="bc-confirm-dialog__preview-label">
            {{ idx === 0 ? t('aiBusinessCard.uploadDialog.frontSide') : t('aiBusinessCard.uploadDialog.backSide') }}
          </span>
          <img :src="url" alt="" />
        </div>
      </div>
      <div class="bc-confirm-dialog__forms">
        <el-alert
          v-if="mode === 'customer' && similarCustomers.length"
          type="warning"
          :closable="false"
          show-icon
          class="bc-confirm-dialog__alert"
        >
          <template #title>{{ t('aiEntityCreate.confirmDialog.similarCustomerTitle') }}</template>
          <ul class="bc-confirm-dialog__similar-list">
            <li v-for="c in similarCustomers" :key="c.id">{{ c.label }}</li>
          </ul>
          <p class="bc-confirm-dialog__similar-note">{{ t('aiEntityCreate.confirmDialog.similarCustomerNote') }}</p>
        </el-alert>
        <el-alert
          v-if="mode === 'vendor' && similarVendors.length"
          type="warning"
          :closable="false"
          show-icon
          class="bc-confirm-dialog__alert"
        >
          <template #title>{{ t('aiBusinessCard.confirmDialog.similarVendorTitle') }}</template>
          <ul class="bc-confirm-dialog__similar-list">
            <li v-for="v in similarVendors" :key="v.id">{{ v.label }}</li>
          </ul>
          <p class="bc-confirm-dialog__similar-note">{{ t('aiBusinessCard.confirmDialog.similarVendorNote') }}</p>
        </el-alert>

        <template v-if="mode === 'customer' && customerModel">
          <div class="bc-confirm-dialog__section-title">{{ t('aiBusinessCard.confirmDialog.sectionCustomer') }}</div>
          <el-form label-width="100px" size="default">
            <el-row :gutter="16">
              <el-col :span="12">
                <el-form-item :label="t('aiEntityCreate.fields.customerName')">
                  <el-input v-model="customerModel.customer.customerName" @blur="refreshSimilarCustomers" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('aiEntityCreate.fields.customerShortName')">
                  <el-input v-model="customerModel.customer.customerShortName" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('aiEntityCreate.fields.englishOfficialName')">
                  <el-input v-model="customerModel.customer.englishOfficialName" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('aiEntityCreate.fields.industry')">
                  <el-input v-model="customerModel.customer.industry" />
                </el-form-item>
              </el-col>
              <el-col :span="24">
                <el-form-item :label="t('aiEntityCreate.fields.address')">
                  <el-input v-model="customerModel.customer.address" />
                </el-form-item>
              </el-col>
              <el-col :span="24">
                <el-form-item :label="t('aiBusinessCard.confirmDialog.companyIntro')">
                  <el-input
                    v-model="customerModel.customer.companyInfo"
                    type="textarea"
                    :rows="2"
                    :placeholder="t('aiBusinessCard.confirmDialog.companyIntroPlaceholder')"
                  />
                </el-form-item>
              </el-col>
            </el-row>
          </el-form>

          <div class="bc-confirm-dialog__section-title">{{ t('aiBusinessCard.confirmDialog.sectionContact') }}</div>
          <el-form label-width="100px" size="default">
            <el-row :gutter="16">
              <el-col :span="12">
                <el-form-item :label="t('customerEdit.contacts.cName')">
                  <el-input v-model="customerModel.contact.cName" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('customerEdit.contacts.eName')">
                  <el-input v-model="customerModel.contact.eName" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('customerEdit.contacts.gender')">
                  <el-select v-model="customerModel.contact.gender" style="width: 100%">
                    <el-option :label="t('customerEdit.contacts.genderMale')" :value="1" />
                    <el-option :label="t('customerEdit.contacts.genderFemale')" :value="2" />
                    <el-option :label="t('customerEdit.contacts.genderUndisclosed')" :value="0" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('customerEdit.contacts.mobile')">
                  <el-input v-model="customerModel.contact.mobilePhone" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('customerEdit.contacts.email')">
                  <el-input v-model="customerModel.contact.email" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('customerEdit.contacts.position')">
                  <el-input v-model="customerModel.contact.position" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('customerEdit.contacts.department')">
                  <el-input v-model="customerModel.contact.department" />
                </el-form-item>
              </el-col>
            </el-row>
          </el-form>

          <template v-if="customerModel.address">
            <div class="bc-confirm-dialog__section-title">{{ t('aiBusinessCard.confirmDialog.sectionAddress') }}</div>
            <el-form label-width="100px" size="default">
              <el-row :gutter="16">
                <el-col :span="8">
                  <el-form-item :label="t('aiEntityCreate.fields.province')">
                    <el-input v-model="customerModel.address.province" />
                  </el-form-item>
                </el-col>
                <el-col :span="8">
                  <el-form-item :label="t('aiEntityCreate.fields.city')">
                    <el-input v-model="customerModel.address.city" />
                  </el-form-item>
                </el-col>
                <el-col :span="8">
                  <el-form-item :label="t('aiEntityCreate.fields.district')">
                    <el-input v-model="customerModel.address.district" />
                  </el-form-item>
                </el-col>
                <el-col :span="24">
                  <el-form-item :label="t('aiEntityCreate.fields.address')">
                    <el-input v-model="customerModel.address.streetAddress" />
                  </el-form-item>
                </el-col>
              </el-row>
            </el-form>
          </template>
        </template>

        <template v-else-if="mode === 'vendor' && vendorModel">
          <div class="bc-confirm-dialog__section-title">{{ t('aiBusinessCard.confirmDialog.sectionVendor') }}</div>
          <el-form label-width="100px" size="default">
            <el-row :gutter="16">
              <el-col :span="12">
                <el-form-item :label="t('aiEntityCreate.fields.officialName')">
                  <el-input v-model="vendorModel.vendor.officialName" @blur="refreshSimilarVendors" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('aiEntityCreate.fields.nickName')">
                  <el-input v-model="vendorModel.vendor.nickName" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('aiEntityCreate.fields.englishOfficialName')">
                  <el-input v-model="vendorModel.vendor.englishOfficialName" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('aiEntityCreate.fields.industry')">
                  <el-input v-model="vendorModel.vendor.industry" />
                </el-form-item>
              </el-col>
              <el-col :span="24">
                <el-form-item :label="t('aiEntityCreate.fields.officeAddress')">
                  <el-input v-model="vendorModel.vendor.officeAddress" />
                </el-form-item>
              </el-col>
              <el-col :span="24">
                <el-form-item :label="t('aiBusinessCard.confirmDialog.companyIntro')">
                  <el-input
                    v-model="vendorModel.vendor.companyInfo"
                    type="textarea"
                    :rows="2"
                    :placeholder="t('aiBusinessCard.confirmDialog.companyIntroPlaceholder')"
                  />
                </el-form-item>
              </el-col>
            </el-row>
          </el-form>

          <div class="bc-confirm-dialog__section-title">{{ t('aiBusinessCard.confirmDialog.sectionContact') }}</div>
          <el-form label-width="100px" size="default">
            <el-row :gutter="16">
              <el-col :span="12">
                <el-form-item :label="t('vendorEdit.contacts.cName')">
                  <el-input v-model="vendorModel.contact.cName" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('vendorEdit.contacts.eName')">
                  <el-input v-model="vendorModel.contact.eName" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('vendorEdit.contacts.gender')">
                  <el-select v-model="vendorModel.contact.gender" style="width: 100%">
                    <el-option :label="t('vendorEdit.contacts.genderMale')" :value="1" />
                    <el-option :label="t('vendorEdit.contacts.genderFemale')" :value="2" />
                    <el-option :label="t('vendorEdit.contacts.genderUndisclosed')" :value="0" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('vendorEdit.contacts.mobile')">
                  <el-input v-model="vendorModel.contact.mobile" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('vendorEdit.contacts.email')">
                  <el-input v-model="vendorModel.contact.email" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('vendorEdit.contacts.title')">
                  <el-input v-model="vendorModel.contact.title" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="t('vendorEdit.contacts.department')">
                  <el-input v-model="vendorModel.contact.department" />
                </el-form-item>
              </el-col>
            </el-row>
          </el-form>

          <template v-if="vendorModel.address">
            <div class="bc-confirm-dialog__section-title">{{ t('aiBusinessCard.confirmDialog.sectionAddress') }}</div>
            <el-form label-width="100px" size="default">
              <el-row :gutter="16">
                <el-col :span="8">
                  <el-form-item :label="t('aiEntityCreate.fields.province')">
                    <el-input v-model="vendorModel.address.province" />
                  </el-form-item>
                </el-col>
                <el-col :span="8">
                  <el-form-item :label="t('aiEntityCreate.fields.city')">
                    <el-input v-model="vendorModel.address.city" />
                  </el-form-item>
                </el-col>
                <el-col :span="8">
                  <el-form-item :label="t('aiEntityCreate.fields.district')">
                    <el-input v-model="vendorModel.address.area" />
                  </el-form-item>
                </el-col>
                <el-col :span="24">
                  <el-form-item :label="t('aiEntityCreate.fields.address')">
                    <el-input v-model="vendorModel.address.address" />
                  </el-form-item>
                </el-col>
              </el-row>
            </el-form>
          </template>
        </template>
      </div>
    </div>
    <template #footer>
      <el-button @click="visibleModel = false">{{ t('common.cancel') }}</el-button>
      <el-button type="primary" @click="emitConfirm">{{ t('aiEntityCreate.confirmDialog.confirm') }}</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { findSimilarCustomers, type CustomerMatchOption } from '@/composables/useCustomerFuzzyMatch'
import { findSimilarVendors, type VendorMatchOption } from '@/composables/useVendorFuzzyMatch'
import {
  customerBusinessCardConfirmPayload,
  vendorBusinessCardConfirmPayload,
  type ParsedCustomerBusinessCardFields,
  type ParsedVendorBusinessCardFields
} from '@/utils/entityParseSchema'

const props = defineProps<{
  visible: boolean
  mode: 'customer' | 'vendor'
  previewUrls?: string[]
  customerData?: ParsedCustomerBusinessCardFields | null
  vendorData?: ParsedVendorBusinessCardFields | null
}>()

const emit = defineEmits<{
  'update:visible': [value: boolean]
  confirm: [payload: Record<string, unknown>]
}>()

const { t } = useI18n()
const customerModel = ref<ParsedCustomerBusinessCardFields | null>(null)
const vendorModel = ref<ParsedVendorBusinessCardFields | null>(null)
const similarCustomers = ref<CustomerMatchOption[]>([])
const similarVendors = ref<VendorMatchOption[]>([])

const visibleModel = computed({
  get: () => props.visible,
  set: (v) => emit('update:visible', v)
})

const displayPreviewUrls = computed(() => props.previewUrls ?? [])

function cloneCustomer(data: ParsedCustomerBusinessCardFields): ParsedCustomerBusinessCardFields {
  return JSON.parse(JSON.stringify(data)) as ParsedCustomerBusinessCardFields
}

function cloneVendor(data: ParsedVendorBusinessCardFields): ParsedVendorBusinessCardFields {
  return JSON.parse(JSON.stringify(data)) as ParsedVendorBusinessCardFields
}

async function refreshSimilarCustomers() {
  const name = customerModel.value?.customer.customerName?.trim() ?? ''
  similarCustomers.value = name ? await findSimilarCustomers(name) : []
}

async function refreshSimilarVendors() {
  const name = vendorModel.value?.vendor.officialName?.trim() ?? ''
  similarVendors.value = name ? await findSimilarVendors(name) : []
}

watch(
  () => props.visible,
  async (open) => {
    if (!open) return
    if (props.mode === 'customer' && props.customerData) {
      customerModel.value = cloneCustomer(props.customerData)
      vendorModel.value = null
      await refreshSimilarCustomers()
    } else if (props.mode === 'vendor' && props.vendorData) {
      vendorModel.value = cloneVendor(props.vendorData)
      customerModel.value = null
      await refreshSimilarVendors()
    }
  }
)

function emitConfirm() {
  if (props.mode === 'customer' && customerModel.value) {
    if (!customerModel.value.customer.customerName.trim()) {
      ElMessage.warning(t('aiEntityCreate.errors.noCustomerName'))
      return
    }
    if (!customerModel.value.contact.cName.trim() && !customerModel.value.contact.eName.trim()) {
      ElMessage.warning(t('aiEntityCreate.errors.noContactName'))
      return
    }
    emit('confirm', customerBusinessCardConfirmPayload(customerModel.value))
    visibleModel.value = false
    return
  }
  if (props.mode === 'vendor' && vendorModel.value) {
    if (!vendorModel.value.vendor.officialName.trim()) {
      ElMessage.warning(t('aiEntityCreate.errors.noOfficialName'))
      return
    }
    if (!vendorModel.value.contact.cName.trim() && !vendorModel.value.contact.eName.trim()) {
      ElMessage.warning(t('aiEntityCreate.errors.noContactName'))
      return
    }
    emit('confirm', vendorBusinessCardConfirmPayload(vendorModel.value))
    visibleModel.value = false
  }
}
</script>

<style scoped lang="scss">
.bc-confirm-dialog__layout {
  display: flex;
  gap: 16px;
  align-items: flex-start;
}

.bc-confirm-dialog__preview {
  flex: 0 0 240px;
  display: flex;
  flex-direction: column;
  gap: 10px;

  img {
    width: 100%;
    max-height: 200px;
    object-fit: contain;
    border-radius: 8px;
    border: 1px solid var(--el-border-color-lighter);
    background: var(--el-fill-color-light);
  }
}

.bc-confirm-dialog__preview-label {
  display: block;
  margin-bottom: 4px;
  font-size: 12px;
  font-weight: 600;
  color: var(--el-text-color-secondary);
}

.bc-confirm-dialog__forms {
  flex: 1;
  min-width: 0;
  max-height: 62vh;
  overflow-y: auto;
}

.bc-confirm-dialog__section-title {
  margin: 12px 0 8px;
  font-weight: 600;
  font-size: 14px;
}

.bc-confirm-dialog__alert {
  margin-bottom: 12px;
}

.bc-confirm-dialog__similar-list {
  margin: 4px 0;
  padding-left: 18px;
}

.bc-confirm-dialog__similar-note {
  margin: 4px 0 0;
  font-size: 12px;
}
</style>
