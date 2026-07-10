<template>
  <el-dialog
    v-model="visibleModel"
    :title="t('aiEntityCreate.confirmDialog.title')"
    :width="dialogWidth"
    destroy-on-close
    :close-on-click-modal="false"
    class="ai-entity-parse-confirm-dialog"
  >
    <el-alert
      v-if="entityType === 'CUSTOMER' && similarCustomers.length"
      type="warning"
      :closable="false"
      show-icon
      class="ai-confirm-dialog__alert"
    >
      <template #title>{{ t('aiEntityCreate.confirmDialog.similarCustomerTitle') }}</template>
      <ul class="ai-confirm-dialog__similar-list">
        <li v-for="c in similarCustomers" :key="c.id">{{ c.label }}</li>
      </ul>
      <p class="ai-confirm-dialog__similar-note">{{ t('aiEntityCreate.confirmDialog.similarCustomerNote') }}</p>
    </el-alert>

    <template v-if="entityType === 'CUSTOMER' && customerModel">
      <el-form label-width="120px" class="ai-confirm-dialog__form">
        <el-form-item :label="t('aiEntityCreate.fields.customerName')">
          <el-input v-model="customerModel.customerName" @blur="refreshSimilarCustomers" />
        </el-form-item>
        <el-form-item :label="t('aiEntityCreate.fields.customerShortName')">
          <el-input v-model="customerModel.customerShortName" />
        </el-form-item>
        <el-form-item :label="t('aiEntityCreate.fields.englishOfficialName')">
          <el-input v-model="customerModel.englishOfficialName" />
        </el-form-item>
        <el-form-item :label="t('aiEntityCreate.fields.industry')">
          <el-input v-model="customerModel.industry" />
        </el-form-item>
        <el-form-item :label="t('aiEntityCreate.fields.address')">
          <el-input v-model="customerModel.address" type="textarea" :rows="2" />
        </el-form-item>
        <el-row :gutter="12">
          <el-col :span="8">
            <el-form-item :label="t('aiEntityCreate.fields.province')">
              <el-input v-model="customerModel.province" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item :label="t('aiEntityCreate.fields.city')">
              <el-input v-model="customerModel.city" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item :label="t('aiEntityCreate.fields.district')">
              <el-input v-model="customerModel.district" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item :label="t('aiEntityCreate.fields.remarks')">
          <el-input v-model="customerModel.remarks" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
    </template>

    <template v-else-if="entityType === 'VENDOR' && vendorModel">
      <el-form label-width="120px" class="ai-confirm-dialog__form">
        <el-form-item :label="t('aiEntityCreate.fields.officialName')">
          <el-input v-model="vendorModel.officialName" />
        </el-form-item>
        <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.englishOfficialName')">
              <el-input v-model="vendorModel.englishOfficialName" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.nickName')">
              <el-input v-model="vendorModel.nickName" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.industry')">
              <el-input v-model="vendorModel.industry" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.website')">
              <el-input v-model="vendorModel.website" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item :label="t('aiEntityCreate.fields.officeAddress')">
          <el-input v-model="vendorModel.officeAddress" type="textarea" :rows="2" />
        </el-form-item>
        <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.vendorLevel')">
              <el-input-number v-model="vendorModel.level" :min="1" :max="13" class="ai-qty-input" controls-position="right" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.vendorCredit')">
              <el-input-number v-model="vendorModel.credit" :min="1" :max="10" class="ai-qty-input" controls-position="right" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.priceCurrency')">
              <el-select v-model="vendorModel.currency" style="width: 100%">
                <el-option
                  v-for="opt in settlementCurrencyOptions"
                  :key="opt.value"
                  :label="opt.label"
                  :value="opt.value"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.paymentDays')">
              <el-input-number v-model="vendorModel.paymentDays" :min="0" :max="365" class="ai-qty-input" controls-position="right" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.paymentMethod')">
              <el-input v-model="vendorModel.paymentMethod" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.taxNumber')">
              <el-input v-model="vendorModel.taxNumber" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item :label="t('aiEntityCreate.fields.companyInfo')">
          <el-input v-model="vendorModel.companyInfo" type="textarea" :rows="2" />
        </el-form-item>
        <el-form-item :label="t('aiEntityCreate.fields.remark')">
          <el-input v-model="vendorModel.remark" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
    </template>

    <template v-else-if="entityType === 'CUSTOMER_CONTACT' && customerContactModel">
      <el-form label-width="108px" class="ai-confirm-dialog__form">
        <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.cName')">
              <el-input v-model="customerContactModel.cName" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.englishName')">
              <el-input v-model="customerContactModel.eName" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.gender')">
              <el-select v-model="customerContactModel.gender" style="width: 100%">
                <el-option :label="t('aiEntityCreate.fields.genderUndisclosed')" :value="0" />
                <el-option :label="t('aiEntityCreate.fields.genderMale')" :value="1" />
                <el-option :label="t('aiEntityCreate.fields.genderFemale')" :value="2" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.department')">
              <el-input v-model="customerContactModel.department" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.position')">
              <el-input v-model="customerContactModel.position" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.mobilePhone')">
              <el-input v-model="customerContactModel.mobilePhone" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.landline')">
              <el-input v-model="customerContactModel.phone" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.contactEmail')">
              <el-input v-model="customerContactModel.email" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.fax')">
              <el-input v-model="customerContactModel.fax" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item :label="t('aiEntityCreate.fields.socialAccount')">
          <el-input v-model="customerContactModel.socialAccount" />
        </el-form-item>
        <el-form-item :label="t('aiEntityCreate.fields.remarks')">
          <el-input v-model="customerContactModel.remarks" type="textarea" :rows="2" />
        </el-form-item>
        <el-form-item label=" ">
          <el-checkbox v-model="customerContactModel.isDefault">{{ t('aiEntityCreate.fields.isDefaultContact') }}</el-checkbox>
          <el-checkbox v-model="customerContactModel.isDecisionMaker" style="margin-left: 24px">
            {{ t('aiEntityCreate.fields.isDecisionMaker') }}
          </el-checkbox>
        </el-form-item>
      </el-form>
    </template>

    <template v-else-if="entityType === 'CUSTOMER_ADDRESS' && customerAddressModel">
      <el-form label-width="108px" class="ai-confirm-dialog__form">
        <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.addressType')">
              <el-select v-model="customerAddressModel.addressType" style="width: 100%">
                <el-option :label="t('aiEntityCreate.fields.addressTypeOffice')" value="Office" />
                <el-option :label="t('aiEntityCreate.fields.addressTypeBilling')" value="Billing" />
                <el-option :label="t('aiEntityCreate.fields.addressTypeShipping')" value="Shipping" />
                <el-option :label="t('aiEntityCreate.fields.addressTypeRegistered')" value="Registered" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.country')">
              <el-input v-model="customerAddressModel.country" />
            </el-form-item>
          </el-col>
        </el-row>
        <template v-if="isDomesticAddressConfirm">
          <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
            <el-col :span="8">
              <el-form-item :label="t('aiEntityCreate.fields.province')">
                <el-input v-model="customerAddressModel.province" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('aiEntityCreate.fields.city')">
                <el-input v-model="customerAddressModel.city" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('aiEntityCreate.fields.district')">
                <el-input v-model="customerAddressModel.district" />
              </el-form-item>
            </el-col>
          </el-row>
        </template>
        <template v-else>
          <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
            <el-col :span="8">
              <el-form-item :label="t('aiEntityCreate.fields.stateProvince')">
                <el-input v-model="customerAddressModel.province" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('aiEntityCreate.fields.city')">
                <el-input v-model="customerAddressModel.city" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('aiEntityCreate.fields.zipCode')">
                <el-input v-model="customerAddressModel.zipCode" />
              </el-form-item>
            </el-col>
          </el-row>
        </template>
        <el-form-item :label="t('aiEntityCreate.fields.companyName')">
          <el-input v-model="customerAddressModel.companyName" />
        </el-form-item>
        <el-form-item :label="t('aiEntityCreate.fields.streetAddress')">
          <el-input v-model="customerAddressModel.streetAddress" type="textarea" :rows="2" />
        </el-form-item>
        <el-row v-if="isDomesticAddressConfirm" :gutter="16" class="ai-confirm-dialog__pair-row">
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.zipCode')">
              <el-input v-model="customerAddressModel.zipCode" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.contactName')">
              <el-input v-model="customerAddressModel.contactPerson" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.contactPhone')">
              <el-input v-model="customerAddressModel.contactPhone" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label=" ">
          <el-checkbox v-model="customerAddressModel.isDefault">{{ t('aiEntityCreate.fields.isDefaultAddress') }}</el-checkbox>
        </el-form-item>
      </el-form>
    </template>

    <template v-else-if="entityType === 'VENDOR_ADDRESS' && vendorAddressModel">
      <el-form label-width="108px" class="ai-confirm-dialog__form">
        <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.addressType')">
              <el-select v-model="vendorAddressModel.addressType" style="width: 100%">
                <el-option :label="t('vendorDetail.addresses.typeShipping')" :value="1" />
                <el-option :label="t('vendorDetail.addresses.typeBilling')" :value="2" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.country')">
              <el-input v-model="vendorAddressModel.countryName" />
            </el-form-item>
          </el-col>
        </el-row>
        <template v-if="isDomesticVendorAddressConfirm">
          <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
            <el-col :span="8">
              <el-form-item :label="t('aiEntityCreate.fields.province')">
                <el-input v-model="vendorAddressModel.province" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('aiEntityCreate.fields.city')">
                <el-input v-model="vendorAddressModel.city" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="区/县">
                <el-input v-model="vendorAddressModel.area" />
              </el-form-item>
            </el-col>
          </el-row>
        </template>
        <template v-else>
          <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
            <el-col :span="8">
              <el-form-item :label="t('aiEntityCreate.fields.stateProvince')">
                <el-input v-model="vendorAddressModel.province" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item :label="t('aiEntityCreate.fields.city')">
                <el-input v-model="vendorAddressModel.city" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="区/县">
                <el-input v-model="vendorAddressModel.area" />
              </el-form-item>
            </el-col>
          </el-row>
        </template>
        <el-form-item :label="t('aiEntityCreate.fields.streetAddress')">
          <el-input v-model="vendorAddressModel.address" type="textarea" :rows="2" />
        </el-form-item>
        <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.contactName')">
              <el-input v-model="vendorAddressModel.contactName" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.contactPhone')">
              <el-input v-model="vendorAddressModel.contactPhone" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item :label="t('aiEntityCreate.fields.remark')">
          <el-input v-model="vendorAddressModel.remark" type="textarea" :rows="2" />
        </el-form-item>
        <el-form-item label=" ">
          <el-checkbox v-model="vendorAddressModel.isDefault">{{ t('aiEntityCreate.fields.isDefaultAddress') }}</el-checkbox>
        </el-form-item>
      </el-form>
    </template>

    <template v-else-if="entityType === 'VENDOR_CONTACT' && vendorContactModel">
      <el-form label-width="108px" class="ai-confirm-dialog__form">
        <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.cName')">
              <el-input v-model="vendorContactModel.cName" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.englishName')">
              <el-input v-model="vendorContactModel.eName" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.position')">
              <el-input v-model="vendorContactModel.title" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.department')">
              <el-input v-model="vendorContactModel.department" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.mobilePhone')">
              <el-input v-model="vendorContactModel.mobile" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.landline')">
              <el-input v-model="vendorContactModel.tel" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item :label="t('aiEntityCreate.fields.contactEmail')">
          <el-input v-model="vendorContactModel.email" />
        </el-form-item>
        <el-form-item :label="t('aiEntityCreate.fields.remark')">
          <el-input v-model="vendorContactModel.remark" type="textarea" :rows="2" />
        </el-form-item>
        <el-form-item label=" ">
          <el-checkbox v-model="vendorContactModel.isMain">{{ t('aiEntityCreate.fields.isMainContact') }}</el-checkbox>
        </el-form-item>
      </el-form>
    </template>

    <template v-else-if="entityType === 'RFQ' && rfqModel">
      <el-alert
        v-if="rfqModel.customerName && !rfqModel.customerId && customerMatchOptions.length === 0"
        type="info"
        :closable="false"
        show-icon
        class="ai-confirm-dialog__alert"
      >
        {{ t('aiEntityCreate.confirmDialog.noCustomerMatch') }}
      </el-alert>
      <el-form label-width="108px" class="ai-confirm-dialog__form ai-confirm-dialog__form--rfq">
        <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.customerName')">
              <el-input v-model="rfqModel.customerName" @blur="refreshRfqCustomerMatch" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.contactEmail')">
              <el-input v-model="rfqModel.contactEmail" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item v-if="customerMatchOptions.length" :label="t('aiEntityCreate.fields.matchedCustomer')">
          <el-select
            v-model="rfqModel.customerId"
            filterable
            clearable
            style="width: 100%"
            :placeholder="t('aiEntityCreate.confirmDialog.selectCustomer')"
          >
            <el-option
              v-for="opt in customerMatchOptions"
              :key="opt.id"
              :label="opt.label"
              :value="opt.id"
            />
          </el-select>
        </el-form-item>
        <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.product')">
              <el-input v-model="rfqModel.product" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('aiEntityCreate.fields.industry')">
              <el-input v-model="rfqModel.industry" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item :label="t('aiEntityCreate.fields.projectBackground')">
          <el-input v-model="rfqModel.projectBackground" type="textarea" :rows="2" />
        </el-form-item>
        <div class="ai-confirm-dialog__items-section">
          <div class="ai-confirm-dialog__items-head">
            <span class="ai-confirm-dialog__items-title">
              {{ t('aiEntityCreate.confirmDialog.itemsSection') }} ({{ rfqModel.items.length }})
            </span>
            <div class="ai-confirm-dialog__items-actions">
              <el-button size="small" class="ai-confirm-dialog__fill-blank-btn" @click="fillBlankMpnBrand">
                {{ t('aiEntityCreate.confirmDialog.fillBlankMpnBrand') }}
              </el-button>
              <el-radio-group v-model="itemsViewMode" size="small" class="ai-confirm-dialog__items-toggle">
                <el-radio-button label="panel">{{ t('aiEntityCreate.confirmDialog.viewPanel') }}</el-radio-button>
                <el-radio-button label="list">{{ t('aiEntityCreate.confirmDialog.viewList') }}</el-radio-button>
              </el-radio-group>
            </div>
          </div>

          <!-- 面板 -->
          <div v-if="itemsViewMode === 'panel'" class="ai-confirm-dialog__items-panel">
            <div
              v-for="(row, idx) in rfqModel.items"
              :key="'panel-' + idx"
              class="ai-confirm-dialog__item-block"
            >
              <div class="ai-confirm-dialog__item-head">
                <span class="ai-confirm-dialog__item-title">
                  {{ t('aiEntityCreate.confirmDialog.itemLineTitle', { n: idx + 1 }) }}
                </span>
              </div>
              <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
                <el-col :span="12">
                  <el-form-item :label="t('aiEntityCreate.fields.customerMpn')">
                    <el-input v-model="row.customerMpn" />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item :label="t('aiEntityCreate.fields.customerBrand')">
                    <el-input v-model="row.customerBrand" />
                  </el-form-item>
                </el-col>
              </el-row>
              <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
                <el-col :span="12">
                  <el-form-item :label="t('aiEntityCreate.fields.mpn')">
                    <el-input v-model="row.mpn" />
                  </el-form-item>
                </el-col>
                <el-col :span="12">
                  <el-form-item :label="t('aiEntityCreate.fields.brand')">
                    <el-input v-model="row.brand" />
                  </el-form-item>
                </el-col>
              </el-row>
              <el-row :gutter="16" class="ai-confirm-dialog__pair-row">
                <el-col :span="12">
                  <el-form-item :label="t('aiEntityCreate.fields.targetPrice')">
                    <div class="ai-target-price-row">
                      <el-input-number
                        v-model="row.targetPrice"
                        :min="0"
                        :precision="4"
                        :controls="true"
                        controls-position="right"
                        class="ai-target-price-row__amount"
                      />
                      <el-select
                        v-model="row.priceCurrency"
                        class="ai-target-price-row__ccy"
                        :placeholder="t('aiEntityCreate.fields.priceCurrency')"
                      >
                        <el-option
                          v-for="opt in settlementCurrencyOptions"
                          :key="opt.value"
                          :label="opt.label"
                          :value="opt.value"
                        />
                      </el-select>
                    </div>
                  </el-form-item>
                </el-col>
                <el-col :span="6">
                  <el-form-item :label="t('aiEntityCreate.fields.quantity')">
                    <el-input-number v-model="row.quantity" :min="1" class="ai-qty-input" controls-position="right" />
                  </el-form-item>
                </el-col>
              </el-row>
              <el-form-item :label="t('aiEntityCreate.fields.remark')">
                <el-input v-model="row.remark" type="textarea" :rows="2" />
              </el-form-item>
            </div>
          </div>

          <!-- 列表 -->
          <div v-else class="ai-confirm-dialog__items-table-wrap">
            <el-table
              :data="rfqModel.items"
              size="small"
              class="ai-confirm-dialog__items-table"
              border
              :fit="false"
            >
              <el-table-column
                :label="t('aiEntityCreate.confirmDialog.itemIndex')"
                width="72"
                min-width="72"
                align="center"
                header-align="center"
                class-name="ai-items-table-index-col"
                label-class-name="ai-items-table-index-col"
              >
                <template #default="{ $index }">{{ $index + 1 }}</template>
              </el-table-column>
              <el-table-column :label="t('aiEntityCreate.fields.customerMpn')" min-width="200">
                <template #default="{ $index }">
                  <el-input v-model="rfqModel.items[$index].customerMpn" size="small" class="ai-items-table-input" />
                </template>
              </el-table-column>
              <el-table-column :label="t('aiEntityCreate.fields.customerBrand')" min-width="120">
                <template #default="{ $index }">
                  <el-input v-model="rfqModel.items[$index].customerBrand" size="small" class="ai-items-table-input" />
                </template>
              </el-table-column>
              <el-table-column :label="t('aiEntityCreate.fields.mpn')" min-width="200">
                <template #default="{ $index }">
                  <el-input v-model="rfqModel.items[$index].mpn" size="small" class="ai-items-table-input" />
                </template>
              </el-table-column>
              <el-table-column :label="t('aiEntityCreate.fields.brand')" min-width="100">
                <template #default="{ $index }">
                  <el-input v-model="rfqModel.items[$index].brand" size="small" class="ai-items-table-input" />
                </template>
              </el-table-column>
              <el-table-column :label="t('aiEntityCreate.fields.targetPrice')" min-width="220">
                <template #default="{ $index }">
                  <div class="ai-target-price-row ai-target-price-row--table">
                    <el-input-number
                      v-model="rfqModel.items[$index].targetPrice"
                      :min="0"
                      :precision="4"
                      :controls="false"
                      size="small"
                      class="ai-target-price-row__amount ai-target-price-row__amount--table"
                    />
                    <el-select
                      v-model="rfqModel.items[$index].priceCurrency"
                      size="small"
                      class="ai-target-price-row__ccy ai-target-price-row__ccy--table"
                    >
                      <el-option
                        v-for="opt in settlementCurrencyOptions"
                        :key="opt.value"
                        :label="opt.label"
                        :value="opt.value"
                      />
                    </el-select>
                  </div>
                </template>
              </el-table-column>
              <el-table-column :label="t('aiEntityCreate.fields.quantity')" min-width="100">
                <template #default="{ $index }">
                  <el-input-number
                    v-model="rfqModel.items[$index].quantity"
                    :min="1"
                    :controls="false"
                    size="small"
                    class="ai-items-table-qty"
                  />
                </template>
              </el-table-column>
              <el-table-column :label="t('aiEntityCreate.fields.remark')" min-width="220">
                <template #default="{ $index }">
                  <el-input
                    v-model="rfqModel.items[$index].remark"
                    size="small"
                    class="ai-items-table-input ai-items-table-input--remark"
                  />
                </template>
              </el-table-column>
            </el-table>
          </div>
        </div>
      </el-form>
    </template>

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
import {
  emptyParsedCustomer,
  emptyParsedCustomerContact,
  emptyParsedCustomerAddress,
  emptyParsedRfq,
  emptyParsedRfqItem,
  emptyParsedVendor,
  emptyParsedVendorAddress,
  emptyParsedVendorContact,
  normalizeCustomerContactParseResult,
  normalizeVendorContactParseResult,
  type ParsedCustomerAddressFields,
  type ParsedCustomerContactFields,
  type ParsedCustomerFields,
  type ParsedRfqFields,
  type ParsedVendorAddressFields,
  type ParsedVendorContactFields,
  type ParsedVendorFields
} from '@/utils/entityParseSchema'
import {
  findSimilarCustomers,
  searchCustomersByName,
  type CustomerMatchOption
} from '@/composables/useCustomerFuzzyMatch'
import type { AiPrefillEntityType } from '@/utils/aiPrefill'
import { SETTLEMENT_CURRENCY_OPTIONS, DEFAULT_SETTLEMENT_CURRENCY_CODE } from '@/constants/currency'
import { usesChinaRegionCascader } from '@/constants/customerAddress'

const settlementCurrencyOptions = SETTLEMENT_CURRENCY_OPTIONS

const props = defineProps<{
  visible: boolean
  entityType: AiPrefillEntityType
  customerData?: ParsedCustomerFields | null
  rfqData?: ParsedRfqFields | null
  vendorData?: ParsedVendorFields | null
  customerContactData?: ParsedCustomerContactFields | null
  vendorContactData?: ParsedVendorContactFields | null
  customerAddressData?: ParsedCustomerAddressFields | null
  vendorAddressData?: ParsedVendorAddressFields | null
}>()

const emit = defineEmits<{
  'update:visible': [value: boolean]
  confirm: [
    payload:
      | ParsedCustomerFields
      | ParsedRfqFields
      | ParsedVendorFields
      | ParsedCustomerContactFields
      | ParsedVendorContactFields
      | ParsedCustomerAddressFields
      | ParsedVendorAddressFields
  ]
}>()

const { t } = useI18n()

const customerModel = ref<ParsedCustomerFields>(emptyParsedCustomer())
const rfqModel = ref<ParsedRfqFields>(emptyParsedRfq())
const vendorModel = ref<ParsedVendorFields>(emptyParsedVendor())
const customerContactModel = ref<ParsedCustomerContactFields>(emptyParsedCustomerContact())
const customerAddressModel = ref<ParsedCustomerAddressFields>(emptyParsedCustomerAddress())
const vendorAddressModel = ref<ParsedVendorAddressFields>(emptyParsedVendorAddress())
const vendorContactModel = ref<ParsedVendorContactFields>(emptyParsedVendorContact())
const similarCustomers = ref<CustomerMatchOption[]>([])
const customerMatchOptions = ref<CustomerMatchOption[]>([])
const itemsViewMode = ref<'panel' | 'list'>('panel')

const dialogWidth = computed(() => {
  if (props.entityType === 'RFQ') return '960px'
  if (props.entityType === 'CUSTOMER_ADDRESS' || props.entityType === 'VENDOR_ADDRESS') return '760px'
  return '760px'
})

const isDomesticAddressConfirm = computed(() =>
  usesChinaRegionCascader(customerAddressModel.value.country, customerAddressModel.value.province)
)

const isDomesticVendorAddressConfirm = computed(() =>
  usesChinaRegionCascader(vendorAddressModel.value.countryName, vendorAddressModel.value.province)
)

const visibleModel = computed({
  get: () => props.visible,
  set: (v) => emit('update:visible', v)
})

watch(
  () => props.visible,
  async (open) => {
    if (!open) return
    if (props.entityType === 'CUSTOMER' && props.customerData) {
      customerModel.value = { ...props.customerData }
      await refreshSimilarCustomers()
    }
    if (props.entityType === 'RFQ' && props.rfqData) {
      itemsViewMode.value = 'panel'
      rfqModel.value = {
        ...props.rfqData,
        items: (props.rfqData.items.length ? props.rfqData.items : [{ ...emptyParsedRfqItem() }]).map((it) => ({
          ...it,
          priceCurrency: it.priceCurrency ?? DEFAULT_SETTLEMENT_CURRENCY_CODE,
          quantity: it.quantity != null && it.quantity > 0 ? it.quantity : 1
        }))
      }
      await refreshRfqCustomerMatch()
    }
    if (props.entityType === 'VENDOR' && props.vendorData) {
      vendorModel.value = {
        ...props.vendorData,
        currency: props.vendorData.currency ?? DEFAULT_SETTLEMENT_CURRENCY_CODE
      }
    }
    if (props.entityType === 'CUSTOMER_CONTACT' && props.customerContactData) {
      customerContactModel.value = normalizeCustomerContactParseResult(
        props.customerContactData as unknown as Record<string, unknown>
      )
    }
    if (props.entityType === 'CUSTOMER_ADDRESS' && props.customerAddressData) {
      customerAddressModel.value = { ...props.customerAddressData }
    }
    if (props.entityType === 'VENDOR_ADDRESS' && props.vendorAddressData) {
      vendorAddressModel.value = { ...props.vendorAddressData }
    }
    if (props.entityType === 'VENDOR_CONTACT' && props.vendorContactData) {
      vendorContactModel.value = normalizeVendorContactParseResult(
        props.vendorContactData as unknown as Record<string, unknown>
      )
    }
  }
)

async function refreshSimilarCustomers() {
  const name = customerModel.value.customerName.trim()
  similarCustomers.value = name ? await findSimilarCustomers(name) : []
}

async function refreshRfqCustomerMatch() {
  const name = rfqModel.value.customerName.trim()
  if (!name) {
    customerMatchOptions.value = []
    rfqModel.value.customerId = ''
    return
  }
  const matches = await searchCustomersByName(name)
  customerMatchOptions.value = matches
  if (matches.length === 1) {
    rfqModel.value.customerId = matches[0].id
  } else if (!matches.some((m) => m.id === rfqModel.value.customerId)) {
    rfqModel.value.customerId = ''
  }
}

function isBlankText(v: unknown): boolean {
  return !String(v ?? '').trim()
}

function fillBlankMpnBrand() {
  let filled = 0
  for (const item of rfqModel.value.items) {
    if (isBlankText(item.customerMpn) && !isBlankText(item.mpn)) {
      item.customerMpn = item.mpn.trim()
      filled++
    }
    if (isBlankText(item.mpn) && !isBlankText(item.customerMpn)) {
      item.mpn = item.customerMpn.trim()
      filled++
    }
    if (isBlankText(item.customerBrand) && !isBlankText(item.brand)) {
      item.customerBrand = item.brand.trim()
      filled++
    }
    if (isBlankText(item.brand) && !isBlankText(item.customerBrand)) {
      item.brand = item.customerBrand.trim()
      filled++
    }
  }
  if (filled > 0) {
    ElMessage.success(t('aiEntityCreate.confirmDialog.fillBlankMpnBrandDone'))
  } else {
    ElMessage.info(t('aiEntityCreate.confirmDialog.fillBlankMpnBrandNoop'))
  }
}

function emitConfirm() {
  if (props.entityType === 'CUSTOMER') {
    emit('confirm', { ...customerModel.value })
  } else if (props.entityType === 'VENDOR') {
    emit('confirm', { ...vendorModel.value })
  } else if (props.entityType === 'CUSTOMER_CONTACT') {
    emit('confirm', { ...customerContactModel.value })
  } else if (props.entityType === 'CUSTOMER_ADDRESS') {
    emit('confirm', { ...customerAddressModel.value })
  } else if (props.entityType === 'VENDOR_ADDRESS') {
    emit('confirm', { ...vendorAddressModel.value })
  } else if (props.entityType === 'VENDOR_CONTACT') {
    emit('confirm', { ...vendorContactModel.value })
  } else {
    emit('confirm', {
      ...rfqModel.value,
      items: rfqModel.value.items.map((it) => ({ ...it }))
    })
  }
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.ai-confirm-dialog__alert {
  margin-bottom: 16px;
}

.ai-confirm-dialog__similar-list {
  margin: 8px 0 4px;
  padding-left: 18px;
}

.ai-confirm-dialog__similar-note {
  margin: 4px 0 0;
  font-size: 12px;
}

.ai-confirm-dialog__form {
  max-height: 55vh;
  overflow-y: auto;
  overflow-x: hidden;
  padding-right: 4px;
}

.ai-confirm-dialog__form--rfq {
  :deep(.el-form-item) {
    margin-bottom: 16px;
  }

  .ai-confirm-dialog__items-section {
    overflow: visible;
  }
}

.ai-confirm-dialog__pair-row {
  width: 100%;
}

.ai-confirm-dialog__items-section {
  margin-top: 8px;
  padding-top: 4px;
  border-top: 1px solid var(--el-border-color-lighter);
}

.ai-confirm-dialog__items-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin: 12px 0 16px;
}

.ai-confirm-dialog__items-actions {
  --ai-view-toggle-btn-width: 48px;
  display: flex;
  align-items: center;
  flex-shrink: 0;
}

.ai-confirm-dialog__fill-blank-btn {
  margin-right: 100px;
}

.ai-confirm-dialog__items-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.ai-confirm-dialog__items-toggle {
  flex-shrink: 0;

  :deep(.el-radio-button__inner) {
    min-width: var(--ai-view-toggle-btn-width);
    box-sizing: border-box;
  }
}

.ai-confirm-dialog__items-panel {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-top: 4px;
}

.ai-confirm-dialog__items-table-wrap {
  margin-top: 4px;
  width: 100%;
  max-width: 100%;
  overflow-x: auto;
  overflow-y: hidden;
}

.ai-confirm-dialog__items-table {
  width: max-content;
  min-width: 1220px;

  :deep(.el-table__body-wrapper),
  :deep(.el-table__header-wrapper) {
    overflow: visible;
  }

  :deep(.cell) {
    overflow: visible;
    white-space: nowrap;
  }

  :deep(th.ai-items-table-index-col .cell) {
    white-space: nowrap;
    word-break: keep-all;
    padding-left: 8px;
    padding-right: 8px;
  }

  :deep(td.ai-items-table-index-col .cell) {
    white-space: nowrap;
  }

  :deep(.el-input-number) {
    width: 100%;
  }
}

.ai-items-table-input {
  width: 100%;
  min-width: 0;

  :deep(.el-input__inner) {
    text-overflow: clip;
  }

  &--remark :deep(.el-input__inner) {
    min-width: 180px;
  }
}

.ai-items-table-qty {
  width: 100%;
  min-width: 72px;
}

.ai-confirm-dialog__item-block {
  background: rgba(0, 212, 255, 0.028);
  border: 1px solid rgba(0, 212, 255, 0.14);
  border-radius: $border-radius-md;
  padding: 14px 16px 16px;
}

.ai-confirm-dialog__item-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
  padding-bottom: 8px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
}

.ai-confirm-dialog__item-title {
  margin: 0;
  font-size: 12px;
  font-weight: 600;
  color: rgba(200, 216, 232, 0.75);
  letter-spacing: 0.3px;
}

.ai-qty-input {
  width: 100%;
}

.ai-target-price-row {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
}

.ai-target-price-row__amount {
  flex: 1 1 auto;
  min-width: 168px;
  width: 100%;
  max-width: none;

  :deep(.el-input) {
    width: 100%;
  }

  :deep(.el-input__wrapper) {
    padding-left: 11px;
    padding-right: 42px;
  }

  :deep(.el-input__inner) {
    text-align: left;
    font-variant-numeric: tabular-nums;
  }
}

.ai-target-price-row__ccy {
  width: 100px;
  flex-shrink: 0;

  &--table {
    width: 80px;
  }
}

.ai-target-price-row--table {
  gap: 6px;
  min-width: 196px;

  .ai-target-price-row__amount--table {
    flex: 1 1 auto;
    min-width: 112px;
  }
}
</style>
