<template>
  <el-dialog
    :model-value="modelValue"
    @update:model-value="$emit('update:modelValue', $event)"
    :title="isEdit ? '编辑地址' : '添加地址'"
    width="640px"
    :close-on-click-modal="false"
    @closed="handleClosed"
  >
    <el-form
      ref="formRef"
      :model="formData"
      :rules="formRules"
      label-width="110px"
    >
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
              <el-option
                v-for="c in CUSTOMER_ADDRESS_OVERSEAS_PRESETS"
                :key="c"
                :label="c"
                :value="c"
              />
              <el-option label="其他" :value="CUSTOMER_ADDRESS_COUNTRY_OTHER" />
            </el-select>
          </el-form-item>
        </el-col>
      </el-row>

      <el-row v-if="countrySelect === CUSTOMER_ADDRESS_COUNTRY_OTHER" :gutter="20">
        <el-col :span="24">
          <el-form-item label="国家名称" prop="countryOther">
            <el-input v-model="countryOther" placeholder="请输入国家/地区名称，如 United States" />
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
            <el-input
              v-model="formData.companyName"
              placeholder="该地址对应的公司名称（可与客户主档不同）"
            />
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
          <el-form-item label="联系人" prop="contactPerson">
            <el-input v-model="formData.contactPerson" placeholder="请输入联系人姓名" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="联系电话" prop="contactPhone">
            <el-input v-model="formData.contactPhone" placeholder="请输入联系电话" />
          </el-form-item>
        </el-col>
      </el-row>
      <el-row :gutter="20">
        <el-col :span="24">
          <el-form-item>
            <el-checkbox v-model="formData.isDefault">设为默认地址</el-checkbox>
          </el-form-item>
        </el-col>
      </el-row>
    </el-form>
    <template #footer>
      <el-button @click="$emit('update:modelValue', false)">取消</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="submitting">确定</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { ElNotification, type FormInstance, type FormRules } from 'element-plus';
import { customerAddressApi, normalizeCustomerAddressFromApi } from '@/api/customer';
import { getApiErrorMessage } from '@/utils/apiError';
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
  resolveCustomerAddressCountryName
} from '@/constants/customerAddress';
import type { CustomerAddress, CreateAddressRequest } from '@/types/customer';

const props = defineProps<{
  modelValue: boolean;
  customerId: string;
  address?: CustomerAddress;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void;
  (e: 'success'): void;
}>();

const formRef = ref<FormInstance>();
const submitting = ref(false);

const isEdit = computed(() => !!props.address);

const countrySelect = ref(CUSTOMER_ADDRESS_COUNTRY_CHINA);
const countryOther = ref('');
const regionValue = ref<string[]>([]);

const isDomestic = computed(() =>
  countrySelect.value === CUSTOMER_ADDRESS_COUNTRY_CHINA
);

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

const regionOptions = regionData;

function syncCountryFields() {
  const name = resolveCustomerAddressCountryName(countrySelect.value, countryOther.value);
  formData.value.country = name;
  formData.value.countryCode = isCustomerAddressDomestic(name)
    ? CUSTOMER_ADDRESS_COUNTRY_DOMESTIC_CODE
    : CUSTOMER_ADDRESS_COUNTRY_OVERSEAS_CODE;
}

function resetForm() {
  countrySelect.value = CUSTOMER_ADDRESS_COUNTRY_CHINA;
  countryOther.value = '';
  formData.value = {
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
  };
  regionValue.value = [];
  formRef.value?.resetFields();
}

function handleCountrySelectChange() {
  if (countrySelect.value !== CUSTOMER_ADDRESS_COUNTRY_OTHER) {
    countryOther.value = '';
  }
  if (isDomestic.value) {
    formData.value.province = '';
    formData.value.city = '';
    formData.value.district = '';
    regionValue.value = [];
  } else {
    regionValue.value = [];
    formData.value.province = '';
    formData.value.city = '';
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

watch(countryOther, () => {
  syncCountryFields();
});

watch(
  () => props.address,
  (newVal) => {
    if (newVal) {
      const n = normalizeCustomerAddressFromApi(newVal);
      const { countrySelect: sel, countryOther: other } = customerAddressCountryToSelect(n.country);
      countrySelect.value = sel;
      countryOther.value = other;
      formData.value = {
        addressType: n.addressType,
        country: n.country || CUSTOMER_ADDRESS_COUNTRY_CHINA,
        countryCode: n.countryCode ?? (isCustomerAddressDomestic(n.country, n.countryCode)
          ? CUSTOMER_ADDRESS_COUNTRY_DOMESTIC_CODE
          : CUSTOMER_ADDRESS_COUNTRY_OVERSEAS_CODE),
        province: n.province || '',
        city: n.city || '',
        district: n.district || '',
        streetAddress: n.streetAddress,
        companyName: n.companyName || '',
        zipCode: n.zipCode || '',
        contactPerson: n.contactPerson || '',
        contactPhone: n.contactPhone || '',
        isDefault: n.isDefault
      };
      if (isCustomerAddressDomestic(n.country, n.countryCode) && n.province && n.city) {
        regionValue.value = regionCascaderValueFromFields(n.province, n.city, n.district);
      } else {
        regionValue.value = [];
      }
    } else {
      resetForm();
    }
  },
  { immediate: true }
);

const handleClosed = () => {
  resetForm();
};

const handleSubmit = async () => {
  syncCountryFields();
  const valid = await formRef.value?.validate();
  if (!valid) return;

  submitting.value = true;
  try {
    if (isEdit.value && props.address) {
      await customerAddressApi.updateAddress(props.address.id, formData.value);
      ElNotification.success({ title: '保存成功', message: '地址信息已更新' });
    } else {
      await customerAddressApi.createAddress(props.customerId, formData.value);
      ElNotification.success({ title: '添加成功', message: '地址已添加' });
    }
    emit('success');
    emit('update:modelValue', false);
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
