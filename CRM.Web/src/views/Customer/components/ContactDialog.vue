<template>
  <el-dialog
    :model-value="modelValue"
    @update:model-value="$emit('update:modelValue', $event)"
    :title="isEdit ? '编辑联系人' : '添加联系人'"
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
          <el-form-item label="姓名" prop="contactName">
            <el-input v-model="formData.contactName" placeholder="请输入姓名" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="性别">
            <el-radio-group v-model="formData.gender">
              <el-radio :label="0">男</el-radio>
              <el-radio :label="1">女</el-radio>
              <el-radio :label="2">保密</el-radio>
            </el-radio-group>
          </el-form-item>
        </el-col>
      </el-row>
      <el-row :gutter="20">
        <el-col :span="12">
          <el-form-item label="部门">
            <el-input v-model="formData.department" placeholder="请输入部门" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="职位">
            <el-input v-model="formData.position" placeholder="请输入职位" />
          </el-form-item>
        </el-col>
      </el-row>
      <el-row :gutter="20">
        <el-col :span="12">
          <el-form-item label="手机" prop="mobilePhone">
            <el-input v-model="formData.mobilePhone" placeholder="请输入手机号码" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="固话">
            <el-input v-model="formData.phone" placeholder="请输入固话号码" />
          </el-form-item>
        </el-col>
      </el-row>
      <el-row :gutter="20">
        <el-col :span="12">
          <el-form-item label="邮箱" prop="email">
            <el-input v-model="formData.email" placeholder="请输入邮箱地址" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="传真">
            <el-input v-model="formData.fax" placeholder="请输入传真号码" />
          </el-form-item>
        </el-col>
      </el-row>
      <el-row :gutter="20">
        <el-col :span="24">
          <el-form-item label="QQ/微信">
            <el-input v-model="formData.socialAccount" placeholder="请输入QQ或微信号" />
          </el-form-item>
        </el-col>
      </el-row>
      <el-row :gutter="20">
        <el-col :span="24">
          <el-form-item label="备注">
            <el-input
              v-model="formData.remarks"
              type="textarea"
              :rows="3"
              placeholder="请输入备注信息"
            />
          </el-form-item>
        </el-col>
      </el-row>
      <el-row :gutter="20">
        <el-col :span="24">
          <el-form-item>
            <el-checkbox v-model="formData.isDefault">设为默认联系人</el-checkbox>
            <el-checkbox v-model="formData.isDecisionMaker">是决策人</el-checkbox>
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
import { customerContactApi } from '@/api/customer';
import type { CustomerContactInfo, CreateContactRequest } from '@/types/customer';

const props = defineProps<{
  modelValue: boolean;
  customerId: string;
  contact?: CustomerContactInfo;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void;
  (e: 'success'): void;
}>();

const formRef = ref<FormInstance>();
const submitting = ref(false);

const isEdit = computed(() => !!props.contact);

// 表单数据
const formData = ref<CreateContactRequest>({
  contactName: '',
  gender: 0,
  department: '',
  position: '',
  mobilePhone: '',
  phone: '',
  email: '',
  fax: '',
  socialAccount: '',
  isDefault: false,
  isDecisionMaker: false,
  remarks: ''
});

// 表单校验规则
const formRules: FormRules = {
  contactName: [
    { required: true, message: '请输入姓名', trigger: 'blur' }
  ],
  mobilePhone: [
    { required: true, message: '请输入手机号码', trigger: 'blur' },
    { pattern: /^1[3-9]\d{9}$/, message: '请输入正确的手机号码', trigger: 'blur' }
  ],
  email: [
    { type: 'email', message: '请输入正确的邮箱地址', trigger: 'blur' }
  ]
};

// 重置表单
const resetForm = () => {
  formData.value = {
    contactName: '',
    gender: 0,
    department: '',
    position: '',
    mobilePhone: '',
    phone: '',
    email: '',
    fax: '',
    socialAccount: '',
    isDefault: false,
    isDecisionMaker: false,
    remarks: ''
  };
  formRef.value?.resetFields();
};

// 监听contact变化
watch(() => props.contact, (newVal) => {
  if (newVal) {
    formData.value = {
      contactName: newVal.contactName,
      gender: newVal.gender,
      department: newVal.department || '',
      position: newVal.position || '',
      mobilePhone: newVal.mobilePhone,
      phone: newVal.phone || '',
      email: newVal.email || '',
      fax: newVal.fax || '',
      socialAccount: newVal.socialAccount || '',
      isDefault: newVal.isDefault,
      isDecisionMaker: newVal.isDecisionMaker,
      remarks: newVal.remarks || ''
    };
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
    if (isEdit.value && props.contact) {
      await customerContactApi.updateContact(props.contact.id, formData.value);
      ElMessage.success('更新成功');
    } else {
      await customerContactApi.createContact(props.customerId, formData.value);
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
