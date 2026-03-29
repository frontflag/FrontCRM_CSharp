<template>
  <el-dialog
    :model-value="modelValue"
    @update:model-value="$emit('update:modelValue', $event)"
    :title="isEdit ? '编辑银行信息' : '添加银行信息'"
    width="600px"
    :close-on-click-modal="false"
    @closed="handleClosed"
  >
    <el-form
      ref="formRef"
      :model="formData"
      :rules="formRules"
      label-width="120px"
    >
      <el-row :gutter="20">
        <el-col :span="24">
          <el-form-item label="账户名称" prop="accountName">
            <el-input v-model="formData.accountName" placeholder="请输入账户名称（通常为公司名称）" />
          </el-form-item>
        </el-col>
      </el-row>
      <el-row :gutter="20">
        <el-col :span="24">
          <el-form-item label="开户银行" prop="bankName">
            <el-select-v2
              v-model="formData.bankName"
              :options="bankOptions"
              placeholder="请选择或输入开户银行"
              allow-create
              filterable
              style="width: 100%"
            />
          </el-form-item>
        </el-col>
      </el-row>
      <el-row :gutter="20">
        <el-col :span="24">
          <el-form-item label="开户支行" prop="bankBranch">
            <el-input v-model="formData.bankBranch" placeholder="请输入开户支行（如：北京分行朝阳支行）" />
          </el-form-item>
        </el-col>
      </el-row>
      <el-row :gutter="20">
        <el-col :span="16">
          <el-form-item label="银行账号" prop="accountNumber">
            <el-input v-model="formData.accountNumber" placeholder="请输入银行账号" />
          </el-form-item>
        </el-col>
        <el-col :span="8">
          <el-form-item label="币种" prop="currency">
            <el-select v-model="formData.currency" placeholder="请选择" style="width: 100%">
              <el-option
                v-for="opt in SETTLEMENT_CURRENCY_OPTIONS"
                :key="opt.value"
                :label="opt.label"
                :value="opt.value"
              />
            </el-select>
          </el-form-item>
        </el-col>
      </el-row>
      <el-row :gutter="20">
        <el-col :span="24">
          <el-form-item label="SWIFT代码">
            <el-input v-model="formData.swiftCode" placeholder="请输入SWIFT代码（跨境汇款时需要）" />
          </el-form-item>
        </el-col>
      </el-row>
      <el-row :gutter="20">
        <el-col :span="24">
          <el-form-item>
            <el-checkbox v-model="formData.isDefault">设为默认账户</el-checkbox>
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
import { customerBankApi } from '@/api/customer';
import type { CustomerBankInfo, CreateBankInfoRequest } from '@/types/customer';
import { SETTLEMENT_CURRENCY_OPTIONS } from '@/constants/currency';

const props = defineProps<{
  modelValue: boolean;
  customerId: string;
  bank?: CustomerBankInfo;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void;
  (e: 'success'): void;
}>();

const formRef = ref<FormInstance>();
const submitting = ref(false);

const isEdit = computed(() => !!props.bank);

// 表单数据
const formData = ref<CreateBankInfoRequest>({
  accountName: '',
  bankName: '',
  bankBranch: '',
  accountNumber: '',
  currency: 1,
  swiftCode: '',
  isDefault: false
});

// 表单校验规则
const formRules: FormRules = {
  accountName: [
    { required: true, message: '请输入账户名称', trigger: 'blur' }
  ],
  bankName: [
    { required: true, message: '请输入开户银行', trigger: 'blur' }
  ],
  bankBranch: [
    { required: true, message: '请输入开户支行', trigger: 'blur' }
  ],
  accountNumber: [
    { required: true, message: '请输入银行账号', trigger: 'blur' },
    { min: 10, max: 30, message: '账号长度应在10-30位之间', trigger: 'blur' }
  ],
  currency: [
    { required: true, message: '请选择币种', trigger: 'change' }
  ]
};

// 银行选项
const bankOptions = ref([
  { value: '中国工商银行', label: '中国工商银行' },
  { value: '中国建设银行', label: '中国建设银行' },
  { value: '中国农业银行', label: '中国农业银行' },
  { value: '中国银行', label: '中国银行' },
  { value: '交通银行', label: '交通银行' },
  { value: '招商银行', label: '招商银行' },
  { value: '中信银行', label: '中信银行' },
  { value: '中国光大银行', label: '中国光大银行' },
  { value: '华夏银行', label: '华夏银行' },
  { value: '中国民生银行', label: '中国民生银行' },
  { value: '广发银行', label: '广发银行' },
  { value: '平安银行', label: '平安银行' },
  { value: '兴业银行', label: '兴业银行' },
  { value: '浦发银行', label: '浦发银行' },
  { value: '北京银行', label: '北京银行' },
  { value: '上海银行', label: '上海银行' },
  { value: '汇丰银行', label: '汇丰银行' },
  { value: '渣打银行', label: '渣打银行' },
  { value: '花旗银行', label: '花旗银行' }
]);

// 重置表单
const resetForm = () => {
  formData.value = {
    accountName: '',
    bankName: '',
    bankBranch: '',
    accountNumber: '',
    currency: 1,
    swiftCode: '',
    isDefault: false
  };
  formRef.value?.resetFields();
};

// 监听bank变化
watch(() => props.bank, (newVal) => {
  if (newVal) {
    formData.value = {
      accountName: newVal.accountName,
      bankName: newVal.bankName,
      bankBranch: newVal.bankBranch,
      accountNumber: newVal.accountNumber,
      currency: newVal.currency,
      swiftCode: newVal.swiftCode || '',
      isDefault: newVal.isDefault
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
    if (isEdit.value && props.bank) {
      await customerBankApi.updateBank(props.bank.id, formData.value);
      ElNotification.success({ title: '保存成功', message: '银行信息已更新' });
    } else {
      await customerBankApi.createBank(props.customerId, formData.value);
      ElNotification.success({ title: '添加成功', message: '银行信息已添加' });
    }
    emit('success');
    emit('update:modelValue', false);
  } catch (error) {
    console.error('保存失败:', error);
    ElNotification.error({ title: '保存失败', message: '银行信息保存失败，请稍后重试' });
  } finally {
    submitting.value = false;
  }
};
</script>
