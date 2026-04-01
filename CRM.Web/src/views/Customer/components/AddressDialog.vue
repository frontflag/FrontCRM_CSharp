<template>
  <el-dialog
    :model-value="modelValue"
    @update:model-value="$emit('update:modelValue', $event)"
    :title="isEdit ? '编辑地址' : '添加地址'"
    width="600px"
    :close-on-click-modal="false"
    @closed="handleClosed"
  >
    <el-form
      ref="formRef"
      :model="formData"
      :rules="formRules"
      label-width="100px"
    >
      <el-row :gutter="20">
        <el-col :span="12">
          <el-form-item label="地址类型" prop="addressType">
            <el-select v-model="formData.addressType" placeholder="请选择" style="width: 100%">
              <el-option label="办公地址" value="Office" />
              <el-option label="开票地址" value="Billing" />
              <el-option label="收货地址" value="Shipping" />
              <el-option label="注册地址" value="Registered" />
            </el-select>
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="邮政编码">
            <el-input v-model="formData.zipCode" placeholder="请输入邮政编码" />
          </el-form-item>
        </el-col>
      </el-row>
      <el-row :gutter="20">
        <el-col :span="24">
          <el-form-item label="所在地区" prop="region">
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
        <el-col :span="24">
          <el-form-item label="详细地址" prop="streetAddress">
            <el-input
              v-model="formData.streetAddress"
              type="textarea"
              :rows="2"
              placeholder="请输入详细街道地址"
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

// 地区选择值
const regionValue = ref<string[]>([]);

// 表单数据
const formData = ref<CreateAddressRequest>({
  addressType: 'Office',
  country: '中国',
  province: '',
  city: '',
  district: '',
  streetAddress: '',
  zipCode: '',
  contactPerson: '',
  contactPhone: '',
  isDefault: false
});

// 表单校验规则
const formRules: FormRules = {
  addressType: [
    { required: true, message: '请选择地址类型', trigger: 'change' }
  ],
  streetAddress: [
    { required: true, message: '请输入详细地址', trigger: 'blur' }
  ]
};

// 地区选项（全国省市区）
const regionOptions = regionData;

// 重置表单
const resetForm = () => {
  formData.value = {
    addressType: 'Office',
    country: '中国',
    province: '',
    city: '',
    district: '',
    streetAddress: '',
    zipCode: '',
    contactPerson: '',
    contactPhone: '',
    isDefault: false
  };
  regionValue.value = [];
  formRef.value?.resetFields();
};

// 地区变更
const handleRegionChange = (value: string[]) => {
  if (value && value.length >= 2) {
    formData.value.province = value[0];
    formData.value.city = value[1];
    formData.value.district = value.length >= 3 ? value[2] : REGION_DISTRICT_PLACEHOLDER;
    formData.value.country = '中国';
  } else if (value?.length === 1) {
    formData.value.province = value[0];
    formData.value.city = '';
    formData.value.district = '';
    formData.value.country = '中国';
  } else if (!value?.length) {
    formData.value.province = '';
    formData.value.city = '';
    formData.value.district = '';
    formData.value.country = '中国';
  }
};

// 监听address变化
watch(() => props.address, (newVal) => {
  if (newVal) {
    const n = normalizeCustomerAddressFromApi(newVal);
    formData.value = {
      addressType: n.addressType,
      country: n.country || '中国',
      province: n.province || '',
      city: n.city || '',
      district: n.district || '',
      streetAddress: n.streetAddress,
      zipCode: n.zipCode || '',
      contactPerson: n.contactPerson || '',
      contactPhone: n.contactPhone || '',
      isDefault: n.isDefault
    };
    if (n.province && n.city) {
      regionValue.value = regionCascaderValueFromFields(n.province, n.city, n.district);
    }
  } else {
    resetForm();
  }
}, { immediate: true });

// 关闭时重置
const handleClosed = () => {
  resetForm();
};

// 提交
const handleSubmit = async () => {
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
