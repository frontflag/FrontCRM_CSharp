<template>
  <el-dialog
    :model-value="modelValue"
    :title="mode === 'create' ? '新增联系人' : '编辑联系人'"
    width="520px"
    @close="handleClose"
  >
    <el-form :model="form" label-width="80px" :disabled="loading">
      <el-form-item label="姓名">
        <el-input v-model="form.cName" placeholder="联系人姓名" />
      </el-form-item>
      <el-form-item label="职位">
        <el-input v-model="form.title" placeholder="职位/角色" />
      </el-form-item>
      <el-form-item label="部门">
        <el-input v-model="form.department" placeholder="所属部门" />
      </el-form-item>
      <el-form-item label="手机">
        <el-input v-model="form.mobile" placeholder="手机号" />
      </el-form-item>
      <el-form-item label="电话">
        <el-input v-model="form.tel" placeholder="座机电话" />
      </el-form-item>
      <el-form-item label="邮箱">
        <el-input v-model="form.email" placeholder="邮箱" />
      </el-form-item>
      <el-form-item label="主联系人">
        <el-switch v-model="form.isMain" />
      </el-form-item>
      <el-form-item label="备注">
        <el-input v-model="form.remark" type="textarea" :rows="3" placeholder="补充说明信息" />
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
import type { VendorContactInfo, AddVendorContactRequest, UpdateVendorContactRequest } from '@/types/vendor';

const props = defineProps<{
  modelValue: boolean;
  mode: 'create' | 'edit';
  contact?: VendorContactInfo | null;
  loading?: boolean;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void;
  (e: 'confirm', payload: AddVendorContactRequest | UpdateVendorContactRequest): void;
}>();

const form = reactive<AddVendorContactRequest & { id?: string }>({
  cName: '',
  title: '',
  department: '',
  mobile: '',
  tel: '',
  email: '',
  isMain: false,
  remark: ''
});

watch(
  () => props.contact,
  (val) => {
    if (props.mode === 'edit' && val) {
      form.cName = val.cName || '';
      form.title = val.title || '';
      form.department = val.department || '';
      form.mobile = val.mobile || '';
      form.tel = val.tel || '';
      form.email = val.email || '';
      form.isMain = !!val.isMain;
      form.remark = val.remark || '';
      form.id = val.id;
    } else if (props.mode === 'create') {
      form.cName = '';
      form.title = '';
      form.department = '';
      form.mobile = '';
      form.tel = '';
      form.email = '';
      form.isMain = false;
      form.remark = '';
      form.id = undefined;
    }
  },
  { immediate: true }
);

const handleClose = () => {
  emit('update:modelValue', false);
};

const handleConfirm = () => {
  emit('confirm', { ...form });
};
</script>

