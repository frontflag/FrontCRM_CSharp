<template>
  <div class="payment-bank-settings" v-loading="loading">
    <div class="section-head">
      <div class="section-head__left">
        <div class="section-title">
          <span class="title-bar"></span>{{ t('financeParams.paymentBanksTitle') }}
        </div>
        <p class="section-hint">{{ t('financeParams.paymentBanksHint') }}</p>
      </div>
      <div class="section-head__actions">
        <el-button type="primary" @click="openAdd">{{ t('financeParams.paymentBanksAdd') }}</el-button>
        <el-button :loading="loading" @click="load">{{ t('financeParams.refreshBtn') }}</el-button>
      </div>
    </div>

    <el-table :data="rows" border stripe size="small" class="bank-table">
      <el-table-column prop="sortOrder" :label="t('financeParams.colSortOrder')" width="100" align="center" />
      <el-table-column prop="bankName" :label="t('financeParams.colBankName')" min-width="260" show-overflow-tooltip />
      <el-table-column :label="t('financeParams.colStatus')" width="120" align="center">
        <template #default="{ row }">
          <el-switch
            :model-value="!row.isDisabled"
            :loading="row._toggleLoading"
            @change="(v: boolean) => onToggleDisabled(row, v)"
          />
        </template>
      </el-table-column>
      <el-table-column :label="t('financeParams.colActions')" width="100" align="center" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" @click="openEdit(row)">{{ t('financeParams.editBtn') }}</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="480px" destroy-on-close @closed="resetForm">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item :label="t('financeParams.fieldBankName')" prop="bankName">
          <el-input v-model="form.bankName" maxlength="200" show-word-limit />
        </el-form-item>
        <el-form-item :label="t('financeParams.fieldSortOrder')" prop="sortOrder">
          <el-input-number v-model="form.sortOrder" :min="0" :max="999999" controls-position="right" class="w-full" />
        </el-form-item>
        <el-form-item v-if="isEdit" :label="t('financeParams.fieldDisabled')">
          <el-checkbox v-model="form.isDisabled">{{ t('financeParams.switchDisabledOn') }}</el-checkbox>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">{{ t('financeParams.cancelBtn') }}</el-button>
        <el-button type="primary" :loading="saving" @click="submit">{{ t('financeParams.saveBtn') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { financePaymentBankApi, type FinancePaymentBankDto } from '@/api/financePaymentBank'

const { t } = useI18n()

type RowVm = FinancePaymentBankDto & { _toggleLoading?: boolean }

const loading = ref(false)
const saving = ref(false)
const rows = ref<RowVm[]>([])
const dialogVisible = ref(false)
const isEdit = ref(false)
const editingId = ref<string | null>(null)
const formRef = ref<FormInstance>()

const form = reactive({
  bankName: '',
  sortOrder: 0,
  isDisabled: false
})

const rules = computed<FormRules>(() => ({
  bankName: [{ required: true, message: t('financeParams.ruleBankName'), trigger: 'blur' }],
  sortOrder: [{ required: true, message: t('financeParams.ruleSortOrder'), trigger: 'change' }]
}))

const dialogTitle = computed(() =>
  isEdit.value ? t('financeParams.dialogEditBank') : t('financeParams.dialogAddBank')
)

async function load() {
  loading.value = true
  try {
    const list = await financePaymentBankApi.list()
    rows.value = list.map((r) => ({ ...r, _toggleLoading: false }))
  } catch {
    ElMessage.error(t('financeParams.paymentBanksLoadFailed'))
  } finally {
    loading.value = false
  }
}

function openAdd() {
  isEdit.value = false
  editingId.value = null
  const maxOrder = rows.value.reduce((m, r) => Math.max(m, r.sortOrder), 0)
  form.bankName = ''
  form.sortOrder = maxOrder + 1
  form.isDisabled = false
  dialogVisible.value = true
}

function openEdit(row: FinancePaymentBankDto) {
  isEdit.value = true
  editingId.value = row.id
  form.bankName = row.bankName
  form.sortOrder = row.sortOrder
  form.isDisabled = row.isDisabled
  dialogVisible.value = true
}

function resetForm() {
  formRef.value?.resetFields()
}

async function submit() {
  await formRef.value?.validate().catch(() => Promise.reject())
  saving.value = true
  try {
    if (isEdit.value && editingId.value) {
      await financePaymentBankApi.update(editingId.value, {
        bankName: form.bankName.trim(),
        sortOrder: form.sortOrder,
        isDisabled: form.isDisabled
      })
      ElMessage.success(t('financeParams.saveSuccess'))
    } else {
      await financePaymentBankApi.create({
        bankName: form.bankName.trim(),
        sortOrder: form.sortOrder
      })
      ElMessage.success(t('financeParams.saveSuccess'))
    }
    dialogVisible.value = false
    await load()
  } catch {
    ElMessage.error(t('financeParams.saveFailed'))
  } finally {
    saving.value = false
  }
}

async function onToggleDisabled(row: RowVm, enabled: boolean) {
  const isDisabled = !enabled
  if (row.isDisabled === isDisabled) return
  row._toggleLoading = true
  try {
    await financePaymentBankApi.update(row.id, {
      bankName: row.bankName,
      sortOrder: row.sortOrder,
      isDisabled
    })
    row.isDisabled = isDisabled
    ElMessage.success(t('financeParams.saveSuccess'))
  } catch {
    ElMessage.error(t('financeParams.saveFailed'))
  } finally {
    row._toggleLoading = false
  }
}

onMounted(() => {
  load()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.payment-bank-settings {
  min-height: 200px;
}

.section-head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 16px;
  margin-bottom: 16px;
}

.section-head__left {
  min-width: 0;
}

.section-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 6px;
}

.title-bar {
  width: 3px;
  height: 14px;
  border-radius: 2px;
  background: $cyan-primary;
}

.section-hint {
  margin: 0;
  font-size: 13px;
  color: $text-muted;
  line-height: 1.5;
}

.section-head__actions {
  flex-shrink: 0;
  display: flex;
  gap: 8px;
}

.bank-table {
  width: 100%;
}

.w-full {
  width: 100%;
}
</style>
