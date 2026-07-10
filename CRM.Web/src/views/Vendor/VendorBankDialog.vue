<template>
  <el-dialog
    :model-value="modelValue"
    :title="mode === 'create' ? t('vendorDetail.banks.dialogCreate') : t('vendorDetail.banks.dialogEdit')"
    width="720px"
    @close="handleClose"
  >
    <el-form :model="form" label-width="100px" :disabled="loading">
      <el-row :gutter="16">
        <el-col :span="12">
          <el-form-item :label="t('vendorDetail.banks.accountName')">
            <el-input v-model="form.accountName" :placeholder="t('vendorDetail.banks.phAccountName')" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item :label="t('vendorDetail.banks.bankName')">
            <finance-payment-bank-select
              v-model="form.financePaymentBankId"
              :placeholder="t('vendorDetail.banks.phBankName')"
              clearable
            />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item :label="t('vendorDetail.banks.branch')">
            <el-input v-model="form.bankBranch" :placeholder="t('vendorDetail.banks.phBranch')" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item :label="t('vendorDetail.banks.accountNo')">
            <el-input v-model="form.bankAccount" :placeholder="t('vendorDetail.banks.phAccountNo')" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item :label="t('vendorDetail.banks.currency')">
            <el-select v-model="form.currency" :placeholder="t('vendorDetail.banks.phCurrency')" style="width: 100%">
              <el-option
                v-for="opt in SETTLEMENT_CURRENCY_OPTIONS"
                :key="opt.value"
                :label="opt.label"
                :value="opt.value"
              />
            </el-select>
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item :label="t('vendorDetail.banks.country')">
            <el-input v-model="form.country" :placeholder="t('vendorDetail.banks.phCountry')" />
          </el-form-item>
        </el-col>
        <el-col :span="24">
          <el-form-item :label="t('vendorDetail.banks.bankAddress')">
            <el-input v-model="form.bankAddress" :placeholder="t('vendorDetail.banks.phBankAddress')" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item>
            <template #label>
              <span :title="t('vendorDetail.banks.swiftTitle')">{{ t('vendorDetail.banks.swift') }}</span>
            </template>
            <el-input v-model="form.swift" :placeholder="t('vendorDetail.banks.phSwift')" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item>
            <template #label>
              <span :title="t('vendorDetail.banks.ibanTitle')">{{ t('vendorDetail.banks.iban') }}</span>
            </template>
            <el-input v-model="form.iban" :placeholder="t('vendorDetail.banks.phIban')" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item :label="t('vendorDetail.banks.bankCode')">
            <el-input v-model="form.bankCode" :placeholder="t('vendorDetail.banks.phBankCode')" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item :label="t('vendorDetail.banks.bankType')">
            <el-select v-model="form.accountType" style="width: 100%">
              <el-option :label="t('vendorDetail.banks.bankTypeRmb')" value="rmb" />
              <el-option :label="t('vendorDetail.banks.bankTypeForeign')" value="foreign" />
            </el-select>
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item :label="t('vendorDetail.banks.purposeType')">
            <el-select v-model="form.purposeType" style="width: 100%">
              <el-option :label="t('vendorDetail.banks.purposePayment')" value="payment" />
              <el-option :label="t('vendorDetail.banks.purposeReceipt')" value="receipt" />
            </el-select>
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item :label="t('vendorDetail.banks.defaultCol')">
            <el-switch v-model="form.isDefault" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item :label="t('vendorDetail.banks.enabled')">
            <el-switch v-model="form.isEnabled" />
          </el-form-item>
        </el-col>
        <el-col :span="24">
          <el-form-item :label="t('vendorDetail.banks.remark')">
            <el-input v-model="form.remark" type="textarea" :rows="3" :placeholder="t('vendorDetail.banks.phRemark')" />
          </el-form-item>
        </el-col>
      </el-row>
    </el-form>
    <template #footer>
      <span class="dialog-footer">
        <el-button @click="handleClose">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="loading" @click="handleConfirm">
          {{ t('common.confirm') }}
        </el-button>
      </span>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { reactive, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import type { VendorBankFormPayload, VendorBankInfo } from '@/types/vendor';
import { CurrencyCode, SETTLEMENT_CURRENCY_OPTIONS, DEFAULT_SETTLEMENT_CURRENCY_CODE } from '@/constants/currency';
import FinancePaymentBankSelect from '@/components/Finance/FinancePaymentBankSelect.vue';

const { t } = useI18n();

const props = defineProps<{
  modelValue: boolean;
  mode: 'create' | 'edit';
  bank?: VendorBankInfo | null;
  loading?: boolean;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void;
  (e: 'confirm', payload: VendorBankFormPayload): void;
}>();

const defaultForm = (): VendorBankFormPayload => ({
  accountName: '',
  financePaymentBankId: '',
  bankBranch: '',
  bankAccount: '',
  bankAddress: '',
  swift: '',
  iban: '',
  bankCode: '',
  country: '',
  accountType: 'rmb',
  purposeType: 'payment',
  currency: DEFAULT_SETTLEMENT_CURRENCY_CODE,
  isDefault: false,
  isEnabled: true,
  remark: ''
});

const form = reactive<VendorBankFormPayload>(defaultForm());

function syncAccountTypeFromCurrency(currency: number | undefined) {
  form.accountType = currency === CurrencyCode.RMB ? 'rmb' : 'foreign';
}

function applyBankToForm(val: VendorBankInfo) {
  form.accountName = val.accountName || '';
  form.financePaymentBankId = val.financePaymentBankId?.trim() ?? '';
  form.bankBranch = val.bankBranch || '';
  form.bankAccount = val.bankAccount || '';
  form.bankAddress = val.bankAddress || '';
  form.swift = val.swift || '';
  form.iban = val.iban || '';
  form.bankCode = val.bankCode || '';
  form.country = val.country || '';
  form.accountType = val.accountType === 'foreign' ? 'foreign' : 'rmb';
  form.purposeType = val.purposeType === 'receipt' ? 'receipt' : 'payment';
  form.currency = val.currency ?? DEFAULT_SETTLEMENT_CURRENCY_CODE;
  form.isDefault = !!val.isDefault;
  form.isEnabled = val.isEnabled !== false;
  form.remark = val.remark || '';
}

watch(
  () => props.bank,
  (val) => {
    if (props.mode === 'edit' && val) {
      applyBankToForm(val);
    } else if (props.mode === 'create') {
      Object.assign(form, defaultForm());
    }
  },
  { immediate: true }
);

watch(
  () => form.currency,
  (currency) => syncAccountTypeFromCurrency(currency)
);

const handleClose = () => emit('update:modelValue', false);
const handleConfirm = () => emit('confirm', { ...form });
</script>
