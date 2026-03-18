<template>
  <el-dialog
    :model-value="modelValue"
    @update:model-value="$emit('update:modelValue', $event)"
    :title="isEdit ? '编辑联系人' : '添加联系人'"
    width="560px"
    :close-on-click-modal="false"
    @closed="handleClosed"
  >
    <el-form ref="formRef" :model="formData" :rules="formRules" label-width="90px">
      <el-row :gutter="16">
        <el-col :span="12">
          <el-form-item label="中文名" prop="cName">
            <el-input v-model="formData.cName" placeholder="请输入中文名" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="英文名">
            <el-input v-model="formData.eName" placeholder="请输入英文名" />
          </el-form-item>
        </el-col>
      </el-row>
      <el-row :gutter="16">
        <el-col :span="12">
          <el-form-item label="职位">
            <el-input v-model="formData.title" placeholder="请输入职位" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="部门">
            <el-input v-model="formData.department" placeholder="请输入部门" />
          </el-form-item>
        </el-col>
      </el-row>
      <el-row :gutter="16">
        <el-col :span="12">
          <el-form-item label="手机" prop="mobile">
            <el-input v-model="formData.mobile" placeholder="请输入手机" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="电话">
            <el-input v-model="formData.tel" placeholder="请输入电话" />
          </el-form-item>
        </el-col>
      </el-row>
      <el-row :gutter="16">
        <el-col :span="24">
          <el-form-item label="邮箱">
            <el-input v-model="formData.email" placeholder="请输入邮箱" />
          </el-form-item>
        </el-col>
      </el-row>
      <el-row :gutter="16">
        <el-col :span="24">
          <el-form-item label="备注">
            <el-input v-model="formData.remark" type="textarea" :rows="2" placeholder="备注" />
          </el-form-item>
        </el-col>
      </el-row>
      <el-row :gutter="16">
        <el-col :span="24">
          <el-form-item>
            <el-checkbox v-model="formData.isMain">主联系人</el-checkbox>
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
import { vendorContactApi } from '@/api/vendor';
import type { VendorContactInfo, AddVendorContactRequest, UpdateVendorContactRequest } from '@/types/vendor';

const props = defineProps<{
  modelValue: boolean;
  vendorId: string;
  contact?: VendorContactInfo | null;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void;
  (e: 'success'): void;
}>();

const formRef = ref<FormInstance>();
const submitting = ref(false);
const isEdit = computed(() => !!props.contact);

const formData = ref<AddVendorContactRequest & { remark?: string }>({
  cName: '',
  eName: '',
  title: '',
  department: '',
  mobile: '',
  tel: '',
  email: '',
  isMain: false,
  remark: ''
});

const formRules: FormRules = {
  cName: [{ required: true, message: '请输入中文名', trigger: 'blur' }]
};

watch(() => props.contact, (newVal) => {
  if (newVal) {
    formData.value = {
      cName: newVal.cName || '',
      eName: newVal.eName || '',
      title: newVal.title || '',
      department: newVal.department || '',
      mobile: newVal.mobile || '',
      tel: newVal.tel || '',
      email: newVal.email || '',
      isMain: newVal.isMain ?? false,
      remark: newVal.remark || ''
    };
  } else {
    resetForm();
  }
}, { immediate: true });

const resetForm = () => {
  formData.value = {
    cName: '', eName: '', title: '', department: '',
    mobile: '', tel: '', email: '', isMain: false, remark: ''
  };
  formRef.value?.resetFields();
};

const handleClosed = () => { resetForm(); };

const handleSubmit = async () => {
  const valid = await formRef.value?.validate().catch(() => false);
  if (!valid) return;
  submitting.value = true;
  try {
    if (isEdit.value && props.contact) {
      await vendorContactApi.updateContact(props.contact.id, formData.value as UpdateVendorContactRequest);
      ElMessage.success('更新成功');
    } else {
      await vendorContactApi.createContact(props.vendorId, formData.value);
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
