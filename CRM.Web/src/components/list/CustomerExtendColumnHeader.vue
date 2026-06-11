<template>
  <div class="customer-extend-col-header" :class="{ 'is-expanded': expanded }">
    <button
      type="button"
      class="customer-extend-col-toggle-btn"
      :aria-label="expanded ? t('common.customerExtendCol.collapse') : t('common.customerExtendCol.expand')"
      @click.stop.prevent="onToggleClick"
      @mousedown.stop
    >
      {{ expanded ? '<' : '>' }}
    </button>
    <template v-if="expanded">
      <div
        class="customer-extend-col-header__cols"
        :style="{ gridTemplateColumns: subColGridTemplateColumns }"
      >
        <div
          v-for="(f, index) in fieldKeys"
          :key="f"
          class="customer-extend-col-header__col-wrap"
        >
          <span class="customer-extend-col-header__col-label">{{ fieldShort(f) }}</span>
          <span
            v-if="index < fieldKeys.length - 1"
            class="customer-extend-sub-col-resizer"
            role="separator"
            :aria-label="t('common.customerExtendCol.resizeSubCol')"
            @mousedown.stop="(e) => startSubColResize(index, e)"
          />
        </div>
      </div>
    </template>
    <template v-else>
      <div class="customer-extend-col-header__title">
        <span class="customer-extend-col-header__label">{{ t('common.customerExtendCol.columnTitle') }}</span>
        <el-dropdown trigger="click" placement="bottom-start" @command="onFieldCommand">
          <button
            type="button"
            class="customer-extend-col-field-picker"
            :aria-label="t('common.customerExtendCol.pickField')"
            @click.stop
          >
            ▾
          </button>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item
                v-for="f in fieldKeys"
                :key="f"
                :command="f"
                :class="{ 'is-active': activeField === f }"
              >
                {{ fieldLabel(f) }}
              </el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import {
  CUSTOMER_EXTEND_FIELD_KEYS,
  type CustomerExtendFieldKey
} from '@/constants/listCustomerExtendColumnSpec'
import { useCustomerExtendColumn } from '@/composables/useCustomerExtendColumn'

defineProps<{
  activeField: CustomerExtendFieldKey
}>()

const emit = defineEmits<{
  'set-active-field': [field: CustomerExtendFieldKey]
}>()

const { t } = useI18n()
const fieldKeys = CUSTOMER_EXTEND_FIELD_KEYS

const {
  expanded,
  subColGridTemplateColumns,
  toggleExpanded,
  startSubColResize
} = useCustomerExtendColumn()

function onToggleClick() {
  toggleExpanded()
}

function fieldLabel(key: CustomerExtendFieldKey) {
  return t(`common.customerExtendCol.fields.${key}`)
}

function fieldShort(key: CustomerExtendFieldKey) {
  return t(`common.customerExtendCol.fieldShort.${key}`)
}

function onFieldCommand(cmd: string | number | object) {
  const k = String(cmd) as CustomerExtendFieldKey
  if ((fieldKeys as string[]).includes(k)) emit('set-active-field', k)
}
</script>
