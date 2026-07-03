<template>
  <div v-if="row" class="company-bank-account-fields">
    <div class="group-card__head">
      <span v-if="title" class="group-card__title">{{ title }}</span>
      <div class="group-card__actions" :class="{ 'group-card__actions--only': !title }">
        <el-checkbox v-model="row.availableForPayment">
          {{ t('companyInfo.bank.availableForPayment') }}
        </el-checkbox>
        <el-checkbox
          :model-value="row.isDefault"
          @update:model-value="(on: boolean) => emit('toggle-default', on)"
        >
          {{ t('companyInfo.common.default') }}
        </el-checkbox>
        <span class="switch-label">{{ t('companyInfo.common.enabled') }}</span>
        <el-switch v-model="row.enabled" />
      </div>
    </div>
    <el-form label-width="120px" class="settings-form" :model="row">
      <el-row :gutter="16">
        <el-col :span="12">
          <el-form-item :label="t('companyInfo.bank.bankName')"><el-input v-model="row.bankName" /></el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item :label="t('companyInfo.bank.accountName')"><el-input v-model="row.accountName" /></el-form-item>
        </el-col>
        <el-col :span="24">
          <el-form-item :label="t('companyInfo.bank.bankAddress')"><el-input v-model="row.bankAddress" /></el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item>
            <template #label>
              <span :title="t('companyInfo.bank.swiftTitle')">{{ t('companyInfo.bank.swift') }}</span>
            </template>
            <el-input v-model="row.swift" :placeholder="t('companyInfo.bank.phSwift')" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item>
            <template #label>
              <span :title="t('companyInfo.bank.ibanTitle')">{{ t('companyInfo.bank.iban') }}</span>
            </template>
            <el-input v-model="row.iban" :placeholder="t('companyInfo.bank.phIban')" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item :label="t('companyInfo.bank.bankCode')"><el-input v-model="row.bankCode" /></el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item :label="t('companyInfo.bank.accountNumber')"><el-input v-model="row.accountNumber" /></el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item :label="t('companyInfo.bank.currency')">
            <el-select v-model="row.currency" style="width: 100%">
              <el-option label="RMB" value="RMB" />
              <el-option label="USD" value="USD" />
              <el-option label="EUR" value="EUR" />
            </el-select>
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item :label="t('companyInfo.bank.bankType')">
            <el-select v-model="row.bankType" style="width: 100%">
              <el-option :label="t('companyInfo.bank.bankTypeRmb')" value="rmb" />
              <el-option :label="t('companyInfo.bank.bankTypeForeign')" value="foreign" />
            </el-select>
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item :label="t('companyInfo.bank.country')"><el-input v-model="row.country" /></el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item :label="t('companyInfo.bank.purposeType')">
            <el-select v-model="row.purposeType" style="width: 100%">
              <el-option :label="t('companyInfo.bank.purposePayment')" value="payment" />
              <el-option :label="t('companyInfo.bank.purposeReceipt')" value="receipt" />
            </el-select>
          </el-form-item>
        </el-col>
        <el-col :span="24">
          <el-form-item :label="t('companyInfo.bank.remark')">
            <el-input v-model="row.remark" type="textarea" :rows="2" />
          </el-form-item>
        </el-col>
      </el-row>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import type { CompanyBankRow } from '@/api/companyProfile'

defineProps<{
  row: CompanyBankRow | null
  title?: string
}>()

const emit = defineEmits<{
  'toggle-default': [on: boolean]
}>()

const { t } = useI18n()
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.group-card__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 12px;
  flex-wrap: wrap;
}

.group-card__title {
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
}

.group-card__actions {
  display: flex;
  align-items: center;
  gap: 14px;
  flex-wrap: wrap;
  margin-left: auto;

  &--only {
    width: 100%;
    justify-content: flex-end;
  }
}

.switch-label {
  font-size: 13px;
  color: $text-muted;
}
</style>
