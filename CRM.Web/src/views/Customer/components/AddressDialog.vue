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
            <el-cascader
              v-model="regionValue"
              :options="regionOptions"
              placeholder="请选择省/市/区"
              style="width: 100%"
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
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';
import { customerAddressApi } from '@/api/customer';
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

// 地区选项（简化版）
const regionOptions = ref([
  {
    value: '北京市',
    label: '北京市',
    children: [
      {
        value: '北京市',
        label: '北京市',
        children: [
          { value: '朝阳区', label: '朝阳区' },
          { value: '海淀区', label: '海淀区' },
          { value: '东城区', label: '东城区' },
          { value: '西城区', label: '西城区' },
          { value: '丰台区', label: '丰台区' },
          { value: '石景山区', label: '石景山区' }
        ]
      }
    ]
  },
  {
    value: '上海市',
    label: '上海市',
    children: [
      {
        value: '上海市',
        label: '上海市',
        children: [
          { value: '浦东新区', label: '浦东新区' },
          { value: '黄浦区', label: '黄浦区' },
          { value: '徐汇区', label: '徐汇区' },
          { value: '长宁区', label: '长宁区' },
          { value: '静安区', label: '静安区' }
        ]
      }
    ]
  },
  {
    value: '广东省',
    label: '广东省',
    children: [
      {
        value: '广州市',
        label: '广州市',
        children: [
          { value: '天河区', label: '天河区' },
          { value: '越秀区', label: '越秀区' },
          { value: '荔湾区', label: '荔湾区' },
          { value: '海珠区', label: '海珠区' }
        ]
      },
      {
        value: '深圳市',
        label: '深圳市',
        children: [
          { value: '南山区', label: '南山区' },
          { value: '福田区', label: '福田区' },
          { value: '罗湖区', label: '罗湖区' },
          { value: '宝安区', label: '宝安区' }
        ]
      }
    ]
  },
  {
    value: '浙江省',
    label: '浙江省',
    children: [
      {
        value: '杭州市',
        label: '杭州市',
        children: [
          { value: '西湖区', label: '西湖区' },
          { value: '上城区', label: '上城区' },
          { value: '下城区', label: '下城区' },
          { value: '滨江区', label: '滨江区' }
        ]
      }
    ]
  }
]);

// 监听address变化
watch(() => props.address, (newVal) => {
  if (newVal) {
    formData.value = {
      addressType: newVal.addressType,
      country: newVal.country || '中国',
      province: newVal.province || '',
      city: newVal.city || '',
      district: newVal.district || '',
      streetAddress: newVal.streetAddress,
      zipCode: newVal.zipCode || '',
      contactPerson: newVal.contactPerson || '',
      contactPhone: newVal.contactPhone || '',
      isDefault: newVal.isDefault
    };
    if (newVal.province && newVal.city && newVal.district) {
      regionValue.value = [newVal.province, newVal.city, newVal.district];
    }
  } else {
    resetForm();
  }
}, { immediate: true });

// 地区变更
const handleRegionChange = (value: string[]) => {
  if (value && value.length >= 3) {
    formData.value.province = value[0];
    formData.value.city = value[1];
    formData.value.district = value[2];
    formData.value.country = '中国';
  }
};

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
      ElMessage.success('更新成功');
    } else {
      await customerAddressApi.createAddress(props.customerId, formData.value);
      ElMessage.success('添加成功');
    }
    emit('success');
    emit('update:modelValue', false);
  } catch (error) {
    console.error('保存失败:', error);
    ElMessage.error('保存失败');
  } finally {
    submitting.value = false;
  }
};
</script>
