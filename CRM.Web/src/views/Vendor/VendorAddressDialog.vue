<template>
  <el-dialog
    :model-value="modelValue"
    :title="mode === 'create' ? '新增地址' : '编辑地址'"
    width="560px"
    @close="handleClose"
  >
    <el-form :model="form" label-width="90px" :disabled="loading">
      <el-form-item label="地址类型">
        <el-select v-model="form.addressType" placeholder="选择地址类型" style="width: 200px">
          <el-option :value="1" label="收货地址" />
          <el-option :value="2" label="账单地址" />
        </el-select>
      </el-form-item>
      <el-form-item label="国家/地区">
        <el-input v-model="form.countryName" placeholder="国家/地区" />
      </el-form-item>
      <el-form-item label="省市区">
        <div class="inline-3">
          <el-input v-model="form.province" placeholder="省份" />
          <el-input v-model="form.city" placeholder="城市" />
          <el-input v-model="form.area" placeholder="区/县" />
        </div>
      </el-form-item>
      <el-form-item label="详细地址">
        <el-input v-model="form.address" placeholder="街道、门牌号等详细地址" />
      </el-form-item>
      <el-form-item label="联系人">
        <el-input v-model="form.contactName" placeholder="联系人姓名" />
      </el-form-item>
      <el-form-item label="联系电话">
        <el-input v-model="form.contactPhone" placeholder="手机或座机" />
      </el-form-item>
      <el-form-item label="默认地址">
        <el-switch v-model="form.isDefault" />
      </el-form-item>
      <el-form-item label="备注">
        <el-input v-model="form.remark" type="textarea" :rows="3" placeholder="备注信息" />
      </el-form-item>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="handleClose">取 消</el-button>
        <el-button type="primary" :loading="loading" @click="handleConfirm">
          确 定
        </el-button>
      </span>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { reactive, watch } from 'vue';
import type { VendorAddress } from '@/types/vendor';

const props = defineProps<{
  modelValue: boolean;
  mode: 'create' | 'edit';
  address?: VendorAddress | null;
  loading?: boolean;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void;
  (e: 'confirm', payload: any): void;
}>();

const form = reactive<any>({
  addressType: 1,
  country: undefined as number | undefined,
  countryName: '',
  province: '',
  city: '',
  area: '',
  address: '',
  contactName: '',
  contactPhone: '',
  isDefault: false,
  remark: ''
});

watch(
  () => props.address,
  (val) => {
    if (props.mode === 'edit' && val) {
      form.addressType = val.addressType ?? 1;
      form.country = val.country;
      form.province = val.province || '';
      form.city = val.city || '';
      form.area = val.area || '';
      form.address = val.address || '';
      form.contactName = val.contactName || '';
      form.contactPhone = val.contactPhone || '';
      form.isDefault = !!val.isDefault;
      form.remark = val.remark || '';
    } else if (props.mode === 'create') {
      form.addressType = 1;
      form.country = undefined;
      form.province = '';
      form.city = '';
      form.area = '';
      form.address = '';
      form.contactName = '';
      form.contactPhone = '';
      form.isDefault = false;
      form.remark = '';
    }
  },
  { immediate: true }
);

const handleClose = () => {
  emit('update:modelValue', false);
};

const handleConfirm = () => {
  const payload = {
    addressType: form.addressType,
    country: form.country,
    province: form.province,
    city: form.city,
    area: form.area,
    address: form.address,
    contactName: form.contactName,
    contactPhone: form.contactPhone,
    isDefault: form.isDefault,
    remark: form.remark
  };
  emit('confirm', payload);
};
</script>

<style scoped lang="scss">
.inline-3 {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 8px;
}
</style>

