<template>
  <div class="address-edit-page">
    <div class="page-header">
      <div class="header-left">
        <button class="back-btn" @click="handleBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6"/>
          </svg>
          返回
        </button>
        <div class="header-breadcrumb">
          <span class="breadcrumb-item" @click="handleBack">{{ customerName }}</span>
          <span class="breadcrumb-sep">›</span>
          <span class="breadcrumb-item breadcrumb-item--active">新增地址</span>
        </div>
      </div>
    </div>

    <div class="form-card">
      <div class="form-card-header">
        <div class="form-card-title">新增地址</div>
      </div>

      <div class="form-card-body" v-loading="pageLoading">
        <el-form ref="formRef" :model="formData" :rules="formRules" label-width="110px" label-position="left">
          <el-row :gutter="20">
            <el-col :span="12">
              <el-form-item label="地址类型" prop="addressType">
                <el-select v-model="formData.addressType" placeholder="请选择" style="width: 100%">
                  <el-option label="办公地址" value="Office" />
                  <el-option label="账单地址" value="Billing" />
                  <el-option label="收货地址" value="Shipping" />
                  <el-option label="注册地址" value="Registered" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="国家/地区" prop="countrySelect">
                <el-select
                  v-model="countrySelect"
                  placeholder="请选择国家/地区"
                  style="width: 100%"
                  @change="handleCountrySelectChange"
                >
                  <el-option :label="CUSTOMER_ADDRESS_COUNTRY_CHINA" :value="CUSTOMER_ADDRESS_COUNTRY_CHINA" />
                  <el-option v-for="c in CUSTOMER_ADDRESS_OVERSEAS_PRESETS" :key="c" :label="c" :value="c" />
                  <el-option label="其他" :value="CUSTOMER_ADDRESS_COUNTRY_OTHER" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>

          <el-row v-if="countrySelect === CUSTOMER_ADDRESS_COUNTRY_OTHER" :gutter="20">
            <el-col :span="24">
              <el-form-item label="国家名称" prop="countryOther">
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
            <el-row :gutter="20">
              <el-col :span="12">
                <el-form-item label="邮政编码">
                  <el-input v-model="formData.zipCode" placeholder="请输入邮政编码" />
                </el-form-item>
              </el-col>
            </el-row>
          </template>

          <template v-else>
            <el-row :gutter="20">
              <el-col :span="8">
                <el-form-item label="省/州">
                  <el-input v-model="formData.province" placeholder="省/州" />
                </el-form-item>
              </el-col>
              <el-col :span="8">
                <el-form-item label="城市">
                  <el-input v-model="formData.city" placeholder="城市" />
                </el-form-item>
              </el-col>
              <el-col :span="8">
                <el-form-item label="邮编">
                  <el-input v-model="formData.zipCode" placeholder="Postal Code" />
                </el-form-item>
              </el-col>
            </el-row>
          </template>

          <el-row :gutter="20">
            <el-col :span="24">
              <el-form-item label="地址公司名称">
                <el-input v-model="formData.companyName" placeholder="该地址对应的公司名称（可与客户主档不同）" />
              </el-form-item>
            </el-col>
          </el-row>

          <el-row :gutter="20">
            <el-col :span="24">
              <el-form-item label="详细地址" prop="streetAddress">
                <el-input
                  v-model="formData.streetAddress"
                  type="textarea"
                  :rows="2"
                  :placeholder="isDomestic ? '请输入详细街道地址' : '街道、门牌号等详细地址'"
                />
              </el-form-item>
            </el-col>
          </el-row>

          <el-row :gutter="20">
            <el-col :span="12">
              <el-form-item label="联系人">
                <el-input v-model="formData.contactPerson" placeholder="请输入联系人姓名" />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="联系电话">
                <el-input v-model="formData.contactPhone" placeholder="请输入联系电话" />
              </el-form-item>
            </el-col>
          </el-row>

          <el-row :gutter="20">
            <el-col :span="24">
              <el-form-item label=" ">
                <el-checkbox v-model="formData.isDefault">设为默认地址</el-checkbox>
              </el-form-item>
            </el-col>
          </el-row>
        </el-form>
      </div>

      <div class="form-card-footer">
        <button class="footer-btn footer-btn--cancel" @click="handleBack">取消</button>
        <button class="footer-btn footer-btn--create" :disabled="submitting" @click="handleSubmit">
          {{ submitting ? '保存中...' : '确认添加' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElNotification, type FormInstance, type FormRules } from 'element-plus';
import { customerApi, customerAddressApi } from '@/api/customer';
import { getApiErrorMessage } from '@/utils/apiError';
import { consumeAiPrefill } from '@/utils/aiPrefill';
import { markEntityParseSaved } from '@/utils/entityParseLogTrack';
import { regionData } from '@/data/regions';
import RegionCascaderWithQuickPick from '@/components/RegionCascaderWithQuickPick.vue';
import {
  REGION_DISTRICT_PLACEHOLDER,
  regionCascaderValueFromFields
} from '@/constants/region';
import {
  CUSTOMER_ADDRESS_COUNTRY_CHINA,
  CUSTOMER_ADDRESS_COUNTRY_OTHER,
  CUSTOMER_ADDRESS_COUNTRY_DOMESTIC_CODE,
  CUSTOMER_ADDRESS_COUNTRY_OVERSEAS_CODE,
  CUSTOMER_ADDRESS_OVERSEAS_PRESETS,
  customerAddressCountryToSelect,
  isCustomerAddressDomestic,
  resolveCustomerAddressCountryName,
  usesChinaRegionCascader
} from '@/constants/customerAddress';
import type { CreateAddressRequest } from '@/types/customer';

const route = useRoute();
const router = useRouter();

const customerId = route.params.id as string;
const customerName = ref('客户详情');
const pageLoading = ref(false);
const submitting = ref(false);
const formRef = ref<FormInstance>();
const aiParseLogId = ref<string | null>(null);

const countrySelect = ref(CUSTOMER_ADDRESS_COUNTRY_CHINA);
const countryOther = ref('');
const regionValue = ref<string[]>([]);
const regionOptions = regionData;

const formData = ref<CreateAddressRequest>({
  addressType: 'Office',
  country: CUSTOMER_ADDRESS_COUNTRY_CHINA,
  countryCode: CUSTOMER_ADDRESS_COUNTRY_DOMESTIC_CODE,
  province: '',
  city: '',
  district: '',
  streetAddress: '',
  companyName: '',
  zipCode: '',
  contactPerson: '',
  contactPhone: '',
  isDefault: false
});

const isDomestic = computed(() => {
  const name = resolveCustomerAddressCountryName(countrySelect.value, countryOther.value);
  return usesChinaRegionCascader(name, formData.value.province);
});

const formRules: FormRules = {
  addressType: [{ required: true, message: '请选择地址类型', trigger: 'change' }],
  countryOther: [
    {
      validator: (_rule, _value, callback) => {
        if (countrySelect.value === CUSTOMER_ADDRESS_COUNTRY_OTHER && !countryOther.value.trim()) {
          callback(new Error('请输入国家名称'));
          return;
        }
        callback();
      },
      trigger: 'blur'
    }
  ],
  streetAddress: [{ required: true, message: '请输入详细地址', trigger: 'blur' }]
};

function syncCountryFields() {
  const name = resolveCustomerAddressCountryName(countrySelect.value, countryOther.value);
  formData.value.country = name;
  formData.value.countryCode = usesChinaRegionCascader(name, formData.value.province)
    ? CUSTOMER_ADDRESS_COUNTRY_DOMESTIC_CODE
    : CUSTOMER_ADDRESS_COUNTRY_OVERSEAS_CODE;
}

function handleCountrySelectChange() {
  if (countrySelect.value !== CUSTOMER_ADDRESS_COUNTRY_OTHER) {
    countryOther.value = '';
  }
  if (usesChinaRegionCascader(resolveCustomerAddressCountryName(countrySelect.value, countryOther.value), formData.value.province)) {
    if (countrySelect.value !== CUSTOMER_ADDRESS_COUNTRY_CHINA) {
      regionValue.value = [];
    }
  } else {
    regionValue.value = [];
    formData.value.district = '';
  }
  syncCountryFields();
}

function handleRegionChange(value: string[]) {
  if (value && value.length >= 2) {
    formData.value.province = value[0];
    formData.value.city = value[1];
    formData.value.district = value.length >= 3 ? value[2] : REGION_DISTRICT_PLACEHOLDER;
  } else if (value?.length === 1) {
    formData.value.province = value[0];
    formData.value.city = '';
    formData.value.district = '';
  } else if (!value?.length) {
    formData.value.province = '';
    formData.value.city = '';
    formData.value.district = '';
  }
  syncCountryFields();
}

function applyFormPayload(payload: Record<string, unknown>) {
  const country = String(payload.country ?? CUSTOMER_ADDRESS_COUNTRY_CHINA);
  const { countrySelect: sel, countryOther: other } = customerAddressCountryToSelect(country);
  countrySelect.value = sel;
  countryOther.value = other;

  formData.value = {
    addressType: String(payload.addressType ?? 'Office'),
    country,
    countryCode:
      typeof payload.countryCode === 'number'
        ? payload.countryCode
        : usesChinaRegionCascader(country, String(payload.province ?? ''))
          ? CUSTOMER_ADDRESS_COUNTRY_DOMESTIC_CODE
          : CUSTOMER_ADDRESS_COUNTRY_OVERSEAS_CODE,
    province: String(payload.province ?? ''),
    city: String(payload.city ?? ''),
    district: String(payload.district ?? ''),
    streetAddress: String(payload.streetAddress ?? ''),
    companyName: String(payload.companyName ?? ''),
    zipCode: String(payload.zipCode ?? ''),
    contactPerson: String(payload.contactPerson ?? ''),
    contactPhone: String(payload.contactPhone ?? ''),
    isDefault: payload.isDefault === true
  };

  syncCountryFields();
  if (isCustomerAddressDomestic(formData.value.country, formData.value.countryCode) && formData.value.province && formData.value.city) {
    regionValue.value = regionCascaderValueFromFields(
      formData.value.province,
      formData.value.city,
      formData.value.district
    );
  } else {
    regionValue.value = [];
  }
}

function applyAiPrefillFromRoute() {
  const raw = route.query.aiPrefill;
  const token = Array.isArray(raw) ? raw[0] : raw;
  if (!token || typeof token !== 'string') return;
  const consumed = consumeAiPrefill('CUSTOMER_ADDRESS', token);
  const nextQuery = { ...route.query };
  delete nextQuery.aiPrefill;
  if (Object.keys(nextQuery).length) {
    void router.replace({ query: nextQuery });
  } else {
    void router.replace({ query: {} });
  }
  if (!consumed) return;
  aiParseLogId.value = consumed.parseLogId;
  applyFormPayload(consumed.payload);
}

onMounted(async () => {
  pageLoading.value = true;
  try {
    const customer = await customerApi.getCustomerById(customerId);
    customerName.value = customer.customerName || '客户详情';
    applyAiPrefillFromRoute();
  } catch (e) {
    console.error('加载数据失败', e);
  } finally {
    pageLoading.value = false;
  }
});

const handleBack = () => {
  router.push({ name: 'CustomerDetail', params: { id: customerId }, query: { tab: 'addresses' } });
};

const handleSubmit = async () => {
  syncCountryFields();
  const valid = await formRef.value?.validate().catch(() => false);
  if (!valid) return;

  submitting.value = true;
  try {
    const created = await customerAddressApi.createAddress(customerId, formData.value);
    const newId = (created as { id?: string })?.id;
    if (newId) {
      markEntityParseSaved(aiParseLogId.value, newId);
      aiParseLogId.value = null;
    }
    ElNotification.success({ title: '添加成功', message: '地址已添加' });
    handleBack();
  } catch (error) {
    console.error('保存失败:', error);
    ElNotification.error({
      title: '保存失败',
      message: getApiErrorMessage(error, '地址保存失败，请稍后重试')
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
