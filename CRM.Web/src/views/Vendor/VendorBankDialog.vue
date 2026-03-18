<template>
  <el-dialog
    :model-value="modelValue"
    :title="mode === 'create' ? '添加银行信息' : '编辑银行信息'"
    width="560px"
    @close="handleClose"
  >
    <el-form :model="form" label-width="100px" :disabled="loading">
      <el-form-item label="账户名称">
        <el-input v-model="form.accountName" placeholder="账户名称（通常为公司名称）" />
      </el-form-item>
      <el-form-item label="开户银行">
        <el-input v-model="form.bankName" placeholder="开户银行" />
      </el-form-item>
      <el-form-item label="开户支行">
        <el-input v-model="form.bankBranch" placeholder="开户支行" />
      </el-form-item>
      <el-form-item label="银行账号">
        <el-input v-model="form.bankAccount" placeholder="银行账号" />
      </el-form-item>
      <el-form-item label="币种">
        <el-select v-model="form.currency" placeholder="请选择" style="width: 200px">
          <el-option label="人民币(CNY)" :value="1" />
          <el-option label="美元(USD)" :value="2" />
          <el-option label="欧元(EUR)" :value="3" />
          <el-option label="日元(JPY)" :value="4" />
          <el-option label="英镑(GBP)" :value="5" />
          <el-option label="港币(HKD)" :value="6" />
        </el-select>
      </el-form-item>
      <el-form-item label="默认账户">
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
import type { VendorBankInfo } from '@/types/vendor';

const props = defineProps<{
  modelValue: boolean;
  mode: 'create' | 'edit';
  bank?: VendorBankInfo | null;
  loading?: boolean;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void;
  (e: 'confirm', payload: any): void;
}>();

const form = reactive<any>({
  accountName: '',
  bankName: '',
  bankBranch: '',
  bankAccount: '',
  currency: 1,
  isDefault: false,
  remark: ''
});

watch(
  () => props.bank,
  (val) => {
    if (props.mode === 'edit' && val) {
      form.accountName = val.accountName || '';
      form.bankName = val.bankName || '';
      form.bankBranch = val.bankBranch || '';
      form.bankAccount = val.bankAccount || '';
      form.currency = (val.currency as any) ?? 1;
      form.isDefault = !!val.isDefault;
      form.remark = val.remark || '';
    } else if (props.mode === 'create') {
      form.accountName = '';
      form.bankName = '';
      form.bankBranch = '';
      form.bankAccount = '';
      form.currency = 1;
      form.isDefault = false;
      form.remark = '';
    }
  },
  { immediate: true }
);

const handleClose = () => emit('update:modelValue', false);
const handleConfirm = () => emit('confirm', { ...form });
</script>

