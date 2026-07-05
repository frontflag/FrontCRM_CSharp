<template>
  <div class="biz-brand-country-fields">
    <el-form-item :label="t('bizBrand.colCountry')">
      <el-select
        v-model="countrySelect"
        filterable
        clearable
        :filter-method="onFilterCountry"
        :placeholder="t('bizBrand.phCountrySelect')"
        style="width: 100%"
        @change="onCountrySelectChange"
        @clear="onCountrySelectChange"
        @visible-change="onCountryDropdownVisible"
      >
        <el-option
          v-for="opt in filteredCountryOptions"
          :key="opt.code"
          :label="bizBrandCountryOptionLabel(opt)"
          :value="opt.label"
        />
        <el-option :label="t('bizBrand.countryOtherOption')" :value="BIZ_BRAND_COUNTRY_OTHER" />
      </el-select>
    </el-form-item>

    <el-form-item
      v-if="countrySelect === BIZ_BRAND_COUNTRY_OTHER"
      :label="t('bizBrand.colCountryOtherName')"
    >
      <el-input
        v-model="countryOther"
        maxlength="100"
        :placeholder="t('bizBrand.phCountryOtherName')"
        @input="onCountryOtherInput"
      />
    </el-form-item>

    <el-form-item :label="t('bizBrand.colCountryCode')">
      <el-input
        :model-value="countryCode"
        maxlength="32"
        :placeholder="t('bizBrand.phCountryCode')"
        @update:model-value="onCountryCodeInput"
      />
      <p class="field-hint">{{ t('bizBrand.countryCodeHint') }}</p>
    </el-form-item>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  BIZ_BRAND_COUNTRY_OPTIONS,
  BIZ_BRAND_COUNTRY_OTHER,
  bizBrandCountryOptionLabel,
  bizBrandCountryToSelect,
  filterBizBrandCountryOptions,
  resolveBizBrandCountryCode,
  resolveBizBrandCountryName,
  shouldPreserveBizBrandCountryCode,
  type BizBrandCountryOption
} from '@/constants/bizBrandCountry'

const props = defineProps<{
  country: string
  countryCode: string
}>()

const emit = defineEmits<{
  'update:country': [value: string]
  'update:countryCode': [value: string]
}>()

const { t } = useI18n()

const countrySelect = ref('')
const countryOther = ref('')
const filteredCountryOptions = ref<BizBrandCountryOption[]>([...BIZ_BRAND_COUNTRY_OPTIONS])
const countryCodeTouched = ref(false)
let syncingFromParent = false

function resetCountryFilter() {
  filteredCountryOptions.value = [...BIZ_BRAND_COUNTRY_OPTIONS]
}

function onFilterCountry(query: string) {
  filteredCountryOptions.value = filterBizBrandCountryOptions(query)
}

function onCountryDropdownVisible(visible: boolean) {
  if (visible) resetCountryFilter()
}

function syncAutoCountryCode(countryName: string) {
  if (countryCodeTouched.value) return
  const code = resolveBizBrandCountryCode(countryName)
  if (code) emit('update:countryCode', code)
}

function applyCountrySelection() {
  const name = resolveBizBrandCountryName(countrySelect.value, countryOther.value)
  emit('update:country', name)
  syncAutoCountryCode(name)
}

function onCountrySelectChange() {
  if (syncingFromParent) return
  if (countrySelect.value !== BIZ_BRAND_COUNTRY_OTHER) countryOther.value = ''
  applyCountrySelection()
}

function onCountryOtherInput() {
  if (syncingFromParent) return
  applyCountrySelection()
}

function onCountryCodeInput(value: string) {
  if (syncingFromParent) return
  countryCodeTouched.value = true
  emit('update:countryCode', value)
}

function syncFromParent(country: string, countryCode: string) {
  syncingFromParent = true
  const parsed = bizBrandCountryToSelect(country)
  countrySelect.value = parsed.select
  countryOther.value = parsed.other
  countryCodeTouched.value = shouldPreserveBizBrandCountryCode(country, countryCode)
  syncingFromParent = false
}

watch(
  () => [props.country, props.countryCode] as const,
  ([country, countryCode]) => syncFromParent(country, countryCode),
  { immediate: true }
)

function resetState() {
  countrySelect.value = ''
  countryOther.value = ''
  countryCodeTouched.value = false
  resetCountryFilter()
}

defineExpose({ resetState })
</script>

<style scoped>
.field-hint {
  margin: 6px 0 0;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  line-height: 1.5;
}
</style>
