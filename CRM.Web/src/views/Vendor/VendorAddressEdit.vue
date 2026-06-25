<template>
  <div class="address-edit-page">
    <div class="page-header">
      <div class="header-left">
        <button class="back-btn" @click="handleBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6"/>
          </svg>
          {{ t('vendorDetail.back') }}
        </button>
        <div class="header-breadcrumb">
          <span class="breadcrumb-item" @click="handleBack">{{ vendorName }}</span>
          <span class="breadcrumb-sep">›</span>
          <span class="breadcrumb-item breadcrumb-item--active">{{ t('vendorDetail.addresses.add') }}</span>
        </div>
      </div>
    </div>

    <div class="form-card">
      <div class="form-card-header">
        <div class="form-card-title">{{ t('vendorDetail.addresses.add') }}</div>
      </div>

      <div class="form-card-body" v-loading="pageLoading">
        <el-form ref="formRef" :model="formData" :rules="formRules" label-width="100px" label-position="left">
          <el-row :gutter="20">
            <el-col :span="12">
              <el-form-item :label="t('vendorDetail.addresses.type')" prop="addressType">
                <el-select v-model="formData.addressType" style="width: 100%">
                  <el-option :value="1" :label="t('vendorDetail.addresses.typeShipping')" />
                  <el-option :value="2" :label="t('vendorDetail.addresses.typeBilling')" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item :label="t('aiEntityCreate.fields.country')">
                <el-select
                  v-model="countrySelect"
                  style="width: 100%"
                  @change="handleCountrySelectChange"
                >
                  <el-option :label="VENDOR_ADDRESS_COUNTRY_CHINA" :value="VENDOR_ADDRESS_COUNTRY_CHINA" />
                  <el-option v-for="c in VENDOR_ADDRESS_OVERSEAS_PRESETS" :key="c" :label="c" :value="c" />
                  <el-option label="其他" :value="VENDOR_ADDRESS_COUNTRY_OTHER" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>

          <el-row v-if="countrySelect === VENDOR_ADDRESS_COUNTRY_OTHER" :gutter="20">
            <el-col :span="24">
              <el-form-item label="国家名称">
                <el-input v-model="countryOther" placeholder="请输入国家/地区名称" />
              </el-form-item>
            </el-col>
          </el-row>

          <template v-if="isDomestic">
            <el-row :gutter="20">
              <el-col :span="24">
                <el-form-item label="所在地区">
                  <RegionCascaderWithQuickPick
                    v-model="regionValue"
                    :options="regionOptions"
                    placeholder="请选择省/市/区"
                    @change="handleRegionChange"
                  />
                </el-form-item>
              </el-col>
            </el-row>
          </template>

          <template v-else>
            <el-row :gutter="20">
              <el-col :span="8">
                <el-form-item :label="t('aiEntityCreate.fields.stateProvince')">
                  <el-input v-model="formData.province" placeholder="省/州" />
                </el-form-item>
              </el-col>
              <el-col :span="8">
                <el-form-item :label="t('aiEntityCreate.fields.city')">
                  <el-input v-model="formData.city" placeholder="城市" />
                </el-form-item>
              </el-col>
              <el-col :span="8">
                <el-form-item label="区/县">
                  <el-input v-model="formData.area" placeholder="区/县" />
                </el-form-item>
              </el-col>
            </el-row>
          </template>

          <el-row :gutter="20">
            <el-col :span="24">
              <el-form-item :label="t('vendorDetail.addresses.fullAddress')" prop="address">
                <el-input
                  v-model="formData.address"
                  type="textarea"
                  :rows="2"
                  placeholder="街道、门牌号等详细地址"
                />
              </el-form-item>
            </el-col>
          </el-row>

          <el-row :gutter="20">
            <el-col :span="12">
              <el-form-item :label="t('vendorDetail.addresses.contactName')">
                <el-input v-model="formData.contactName" placeholder="联系人姓名" />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item :label="t('vendorDetail.addresses.phone')">
                <el-input v-model="formData.contactPhone" placeholder="手机或座机" />
              </el-form-item>
            </el-col>
          </el-row>

          <el-row :gutter="20">
            <el-col :span="24">
              <el-form-item label="备注">
                <el-input v-model="formData.remark" type="textarea" :rows="2" placeholder="备注信息" />
              </el-form-item>
            </el-col>
          </el-row>

          <el-row :gutter="20">
            <el-col :span="24">
              <el-form-item label=" ">
                <el-checkbox v-model="formData.isDefault">{{ t('vendorDetail.addresses.setDefault') }}</el-checkbox>
              </el-form-item>
            </el-col>
          </el-row>
        </el-form>
      </div>

      <div class="form-card-footer">
        <button class="footer-btn footer-btn--cancel" @click="handleBack">{{ t('common.cancel') }}</button>
        <button class="footer-btn footer-btn--create" :disabled="submitting" @click="handleSubmit">
          {{ submitting ? '保存中...' : '确认添加' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, reactive } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElNotification, type FormInstance, type FormRules } from 'element-plus';
import { vendorApi, vendorAddressApi } from '@/api/vendor';
import { getApiErrorMessage } from '@/utils/apiError';
import { consumeAiPrefill } from '@/utils/aiPrefill';
import { markEntityParseSaved } from '@/utils/entityParseLogTrack';
import { regionData } from '@/data/regions';
import RegionCascaderWithQuickPick from '@/components/RegionCascaderWithQuickPick.vue';
import { REGION_DISTRICT_PLACEHOLDER, regionCascaderValueFromFields } from '@/constants/region';
import {
  VENDOR_ADDRESS_COUNTRY_CHINA,
  VENDOR_ADDRESS_COUNTRY_OTHER,
  VENDOR_ADDRESS_COUNTRY_DOMESTIC_CODE,
  VENDOR_ADDRESS_OVERSEAS_PRESETS,
  vendorAddressCountryCode,
  vendorAddressCountryToSelect,
  resolveVendorAddressCountryName,
  usesChinaRegionCascader
} from '@/constants/vendorAddress';

const route = useRoute();
const router = useRouter();
const { t } = useI18n();

const vendorId = route.params.id as string;
const vendorName = ref(t('vendorDetail.titleFallback'));
const pageLoading = ref(false);
const submitting = ref(false);
const formRef = ref<FormInstance>();
const aiParseLogId = ref<string | null>(null);

const countrySelect = ref(VENDOR_ADDRESS_COUNTRY_CHINA);
const countryOther = ref('');
const regionValue = ref<string[]>([]);
const regionOptions = regionData;

const formData = reactive({
  addressType: 1,
  countryName: VENDOR_ADDRESS_COUNTRY_CHINA,
  country: VENDOR_ADDRESS_COUNTRY_DOMESTIC_CODE as number | undefined,
  province: '',
  city: '',
  area: '',
  address: '',
  contactName: '',
  contactPhone: '',
  isDefault: false,
  remark: ''
});

const isDomestic = computed(() =>
  usesChinaRegionCascader(
    resolveVendorAddressCountryName(countrySelect.value, countryOther.value),
    formData.province
  )
);

const formRules: FormRules = {
  addressType: [{ required: true, message: '请选择地址类型', trigger: 'change' }],
  address: [{ required: true, message: '请输入详细地址', trigger: 'blur' }]
};

function syncCountryFields() {
  const name = resolveVendorAddressCountryName(countrySelect.value, countryOther.value);
  formData.countryName = name;
  formData.country = vendorAddressCountryCode(name, formData.province);
}

function handleCountrySelectChange() {
  if (countrySelect.value !== VENDOR_ADDRESS_COUNTRY_OTHER) {
    countryOther.value = '';
  }
  if (!usesChinaRegionCascader(resolveVendorAddressCountryName(countrySelect.value, countryOther.value), formData.province)) {
    regionValue.value = [];
    formData.area = '';
  }
  syncCountryFields();
}

function handleRegionChange(value: string[]) {
  if (value && value.length >= 2) {
    formData.province = value[0];
    formData.city = value[1];
    formData.area = value.length >= 3 ? value[2] : REGION_DISTRICT_PLACEHOLDER;
  } else if (value?.length === 1) {
    formData.province = value[0];
    formData.city = '';
    formData.area = '';
  } else if (!value?.length) {
    formData.province = '';
    formData.city = '';
    formData.area = '';
  }
  syncCountryFields();
}

function applyFormPayload(payload: Record<string, unknown>) {
  const countryName = String(payload.countryName ?? payload.country ?? VENDOR_ADDRESS_COUNTRY_CHINA);
  const { countrySelect: sel, countryOther: other } = vendorAddressCountryToSelect(countryName);
  countrySelect.value = sel;
  countryOther.value = other;

  formData.addressType = Number(payload.addressType ?? 1) === 2 ? 2 : 1;
  formData.countryName = countryName;
  formData.country =
    typeof payload.country === 'number'
      ? payload.country
      : vendorAddressCountryCode(countryName, String(payload.province ?? ''));
  formData.province = String(payload.province ?? '');
  formData.city = String(payload.city ?? '');
  formData.area = String(payload.area ?? '');
  formData.address = String(payload.address ?? '');
  formData.contactName = String(payload.contactName ?? '');
  formData.contactPhone = String(payload.contactPhone ?? '');
  formData.isDefault = payload.isDefault === true;
  formData.remark = String(payload.remark ?? '');

  syncCountryFields();
  if (isDomestic.value && formData.province && formData.city) {
    regionValue.value = regionCascaderValueFromFields(formData.province, formData.city, formData.area);
  } else {
    regionValue.value = [];
  }
}

function applyAiPrefillFromRoute() {
  const raw = route.query.aiPrefill;
  const token = Array.isArray(raw) ? raw[0] : raw;
  if (!token || typeof token !== 'string') return;
  const consumed = consumeAiPrefill('VENDOR_ADDRESS', token);
  const nextQuery = { ...route.query };
  delete nextQuery.aiPrefill;
  void router.replace(Object.keys(nextQuery).length ? { query: nextQuery } : { query: {} });
  if (!consumed) return;
  aiParseLogId.value = consumed.parseLogId;
  applyFormPayload(consumed.payload);
}

onMounted(async () => {
  pageLoading.value = true;
  try {
    const vendor = await vendorApi.getVendorById(vendorId);
    vendorName.value = vendor.officialName || t('vendorDetail.titleFallback');
    applyAiPrefillFromRoute();
  } catch (e) {
    console.error('加载数据失败', e);
  } finally {
    pageLoading.value = false;
  }
});

const handleBack = () => {
  router.push({ name: 'VendorDetail', params: { id: vendorId }, query: { tab: 'addresses' } });
};

const handleSubmit = async () => {
  syncCountryFields();
  const valid = await formRef.value?.validate().catch(() => false);
  if (!valid) return;

  submitting.value = true;
  try {
    const created = await vendorAddressApi.createAddress(vendorId, {
      addressType: formData.addressType,
      country: formData.country,
      province: formData.province || undefined,
      city: formData.city || undefined,
      area: formData.area || undefined,
      address: formData.address,
      contactName: formData.contactName || undefined,
      contactPhone: formData.contactPhone || undefined,
      isDefault: formData.isDefault
    });
    const newId = (created as { id?: string })?.id;
    if (newId) {
      markEntityParseSaved(aiParseLogId.value, newId);
      aiParseLogId.value = null;
    }
    ElNotification.success({ title: t('vendorDetail.messages.addressCreated'), message: '' });
    handleBack();
  } catch (error) {
    ElNotification.error({
      title: t('vendorDetail.messages.addressSaveFailed'),
      message: getApiErrorMessage(error, t('vendorDetail.messages.addressSaveFailed'))
    });
  } finally {
    submitting.value = false;
  }
};
</script>

<style lang="scss" scoped>
@import '@/assets/styles/variables.scss';

.address-edit-page {
  min-height: 100vh;
  background: $layer-1;
  padding: 0 0 40px;
}

.page-header {
  display: flex;
  align-items: center;
  padding: 16px 28px;
  background: $layer-2;
  border-bottom: 1px solid $border-panel;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
}

.back-btn {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 5px 12px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: 5px;
  color: $text-secondary;
  font-size: 13px;
  cursor: pointer;
  &:hover {
    border-color: rgba(0, 212, 255, 0.35);
    color: $text-primary;
  }
}

.header-breadcrumb {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
}

.breadcrumb-item {
  color: $text-secondary;
  cursor: pointer;
  &--active {
    color: $text-primary;
    cursor: default;
    font-weight: 500;
  }
}

.breadcrumb-sep {
  color: rgba(130, 170, 200, 0.35);
}

.form-card {
  margin: 24px 28px 0;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: 10px;
  overflow: hidden;
}

.form-card-header {
  padding: 14px 24px;
  background: rgba(0, 212, 255, 0.06);
  border-bottom: 1px solid $border-panel;
}

.form-card-title {
  font-size: 14px;
  font-weight: 600;
  color: rgba(0, 212, 255, 0.9);
}

.form-card-body {
  padding: 24px 28px 8px;
}

.form-card-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding: 16px 28px;
  border-top: 1px solid $border-panel;
}

.footer-btn {
  padding: 8px 24px;
  border-radius: 6px;
  font-size: 13.5px;
  cursor: pointer;
  &--cancel {
    background: transparent;
    border: 1px solid rgba(0, 212, 255, 0.2);
    color: $text-secondary;
  }
  &--create {
    background: linear-gradient(135deg, rgba(46, 160, 67, 0.88), rgba(70, 191, 145, 0.78));
    border: 1px solid rgba(70, 191, 145, 0.45);
    color: #fff;
    &:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }
  }
}
</style>
