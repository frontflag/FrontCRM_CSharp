<template>
  <div class="customer-extend-col-header" :class="{ 'is-expanded': expanded }">
    <button
      type="button"
      class="customer-extend-col-toggle-btn"
      :aria-label="expanded ? t('financeReceiptWriteOff.receiptDateExtendCol.collapse') : t('financeReceiptWriteOff.receiptDateExtendCol.expand')"
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
            :aria-label="t('financeReceiptWriteOff.receiptDateExtendCol.resizeSubCol')"
            @mousedown.stop="(e) => startSubColResize(index, e)"
          />
        </div>
      </div>
    </template>
    <template v-else>
      <div class="customer-extend-col-header__title">
        <span class="customer-extend-col-header__label">{{ t('financeReceiptWriteOff.receiptDateExtendCol.columnTitle') }}</span>
        <el-dropdown trigger="click" placement="bottom-start" @command="onFieldCommand">
          <button
            type="button"
            class="customer-extend-col-field-picker"
            :aria-label="t('financeReceiptWriteOff.receiptDateExtendCol.pickField')"
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
  WRITE_OFF_RECEIPT_DATE_FIELD_KEYS,
  type WriteOffReceiptDateFieldKey
} from '@/constants/writeOffReceiptDateExtendColumnSpec'
import { useWriteOffReceiptDateExtendColumn } from '@/composables/useWriteOffReceiptDateExtendColumn'

defineProps<{
  activeField: WriteOffReceiptDateFieldKey
}>()

const emit = defineEmits<{
  'set-active-field': [field: WriteOffReceiptDateFieldKey]
}>()

const { t } = useI18n()
const fieldKeys = WRITE_OFF_RECEIPT_DATE_FIELD_KEYS

const {
  expanded,
  subColGridTemplateColumns,
  toggleExpanded,
  startSubColResize
} = useWriteOffReceiptDateExtendColumn()

function onToggleClick() {
  toggleExpanded()
}

function fieldLabel(key: WriteOffReceiptDateFieldKey) {
  return t(`financeReceiptWriteOff.receiptDateExtendCol.fields.${key}`)
}

function fieldShort(key: WriteOffReceiptDateFieldKey) {
  return t(`financeReceiptWriteOff.receiptDateExtendCol.fieldShort.${key}`)
}

function onFieldCommand(cmd: string | number | object) {
  const k = String(cmd) as WriteOffReceiptDateFieldKey
  if ((fieldKeys as string[]).includes(k)) emit('set-active-field', k)
}
</script>
