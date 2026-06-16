<template>
  <el-dialog
    v-model="visible"
    :title="dialogTitle"
    width="560px"
    destroy-on-close
    @closed="onClosed"
  >
    <el-form ref="formRef" :model="form" :rules="rules" label-width="120px">
      <el-form-item :label="t('bizBrand.colBrandEName')" prop="brandEName">
        <el-input v-model="form.brandEName" maxlength="200" />
      </el-form-item>
      <el-form-item :label="t('bizBrand.colBrandCName')" prop="brandCName">
        <el-input v-model="form.brandCName" maxlength="200" />
      </el-form-item>
      <el-form-item :label="t('bizBrand.colStandardBrand')" prop="standardBrand">
        <el-input v-model="form.standardBrand" maxlength="300" />
      </el-form-item>
      <el-form-item :label="t('bizBrand.colAlias')">
        <el-input v-model="form.alias" type="textarea" :rows="2" maxlength="500" />
      </el-form-item>
      <el-form-item :label="t('bizBrand.colCountryCode')">
        <el-input v-model="form.countryCode" maxlength="10" />
      </el-form-item>
      <el-form-item :label="t('bizBrand.colCountry')">
        <el-input v-model="form.country" maxlength="100" />
      </el-form-item>
      <el-form-item :label="t('bizBrand.colRemark')">
        <el-input v-model="form.remark" type="textarea" :rows="3" maxlength="500" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="visible = false">{{ t('bizBrand.cancel') }}</el-button>
      <el-button type="primary" :loading="saving" @click="save">{{ t('bizBrand.save') }}</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { bizBrandApi, type BizBrandRow, type UpsertBizBrandPayload } from '@/api/bizBrand'
import { getApiErrorMessage } from '@/utils/apiError'

const props = withDefaults(
  defineProps<{
    modelValue?: boolean
    mode?: 'add' | 'edit'
    editTarget?: BizBrandRow | null
  }>(),
  {
    modelValue: false,
    mode: 'add',
    editTarget: null
  }
)

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  created: [row: BizBrandRow]
  updated: [row: BizBrandRow]
}>()

const { t } = useI18n()
const formRef = ref<FormInstance>()
const saving = ref(false)

const visible = computed({
  get: () => props.modelValue,
  set: (v) => emit('update:modelValue', v)
})

const dialogTitle = computed(() =>
  props.mode === 'add' ? t('bizBrand.dialogAddTitle') : t('bizBrand.dialogEditTitle')
)

const form = reactive({
  brandEName: '',
  brandCName: '',
  standardBrand: '',
  alias: '',
  countryCode: '',
  country: '',
  remark: ''
})

const rules = computed<FormRules>(() => {
  if (props.mode !== 'add') return {}
  const req = (message: string) => ({
    required: true,
    validator: (_rule: unknown, value: string, callback: (err?: Error) => void) => {
      if (!String(value ?? '').trim()) callback(new Error(message))
      else callback()
    },
    trigger: 'blur' as const
  })
  return {
    brandEName: [req(t('bizBrand.requiredBrandEName'))],
    brandCName: [req(t('bizBrand.requiredBrandCName'))],
    standardBrand: [req(t('bizBrand.requiredStandardBrand'))]
  }
})

function resetForm() {
  form.brandEName = ''
  form.brandCName = ''
  form.standardBrand = ''
  form.alias = ''
  form.countryCode = ''
  form.country = ''
  form.remark = ''
}

function fillFromRow(row: BizBrandRow) {
  form.brandEName = row.brandEName ?? ''
  form.brandCName = row.brandCName ?? ''
  form.standardBrand = row.standardBrand ?? ''
  form.alias = row.alias ?? ''
  form.countryCode = row.countryCode ?? ''
  form.country = row.country ?? ''
  form.remark = row.remark ?? ''
}

watch(
  () => [props.modelValue, props.mode, props.editTarget] as const,
  ([open, mode, row]) => {
    if (!open) return
    if (mode === 'edit' && row) fillFromRow(row)
    else resetForm()
  }
)

function onClosed() {
  formRef.value?.clearValidate()
  resetForm()
}

function buildPayload(): UpsertBizBrandPayload {
  return {
    brandEName: form.brandEName.trim() || null,
    brandCName: form.brandCName.trim() || null,
    standardBrand: form.standardBrand.trim() || null,
    alias: form.alias.trim() || null,
    countryCode: form.countryCode.trim() || null,
    country: form.country.trim() || null,
    remark: form.remark.trim() || null
  }
}

async function save() {
  if (props.mode === 'add') {
    const ok = await formRef.value?.validate().catch(() => false)
    if (!ok) return
  }
  saving.value = true
  try {
    const payload = buildPayload()
    if (props.mode === 'edit' && props.editTarget) {
      const row = await bizBrandApi.update(props.editTarget.id, payload)
      ElMessage.success(t('bizBrand.saveOk'))
      emit('updated', row)
      visible.value = false
      return
    }
    const row = await bizBrandApi.create(payload)
    ElMessage.success(t('bizBrand.createOk'))
    emit('created', row)
    visible.value = false
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('bizBrand.saveFailed')))
  } finally {
    saving.value = false
  }
}
</script>
