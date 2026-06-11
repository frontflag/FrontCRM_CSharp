<template>
  <div class="vendor-extend-col-header" :class="{ 'is-expanded': expanded }">
    <button
      type="button"
      class="vendor-extend-col-toggle-btn"
      :aria-label="expanded ? t('common.vendorExtendCol.collapse') : t('common.vendorExtendCol.expand')"
      @click.stop.prevent="onToggleClick"
      @mousedown.stop
    >
      {{ expanded ? '<' : '>' }}
    </button>
    <template v-if="expanded">
      <div
        class="vendor-extend-col-header__cols"
        :style="{ gridTemplateColumns: subColGridTemplateColumns }"
      >
        <div
          v-for="(f, index) in fieldKeys"
          :key="f"
          class="vendor-extend-col-header__col-wrap"
        >
          <span class="vendor-extend-col-header__col-label">{{ fieldShort(f) }}</span>
          <span
            v-if="index < fieldKeys.length - 1"
            class="vendor-extend-sub-col-resizer"
            role="separator"
            :aria-label="t('common.vendorExtendCol.resizeSubCol')"
            @mousedown.stop="(e) => startSubColResize(index, e)"
          />
        </div>
      </div>
    </template>
    <template v-else>
      <div class="vendor-extend-col-header__title">
        <span class="vendor-extend-col-header__label">{{ t('common.vendorExtendCol.columnTitle') }}</span>
        <el-dropdown trigger="click" placement="bottom-start" @command="onFieldCommand">
          <button
            type="button"
            class="vendor-extend-col-field-picker"
            :aria-label="t('common.vendorExtendCol.pickField')"
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
  VENDOR_EXTEND_FIELD_KEYS,
  type VendorExtendFieldKey
} from '@/constants/listVendorExtendColumnSpec'
import { useVendorExtendColumn } from '@/composables/useVendorExtendColumn'

defineProps<{
  activeField: VendorExtendFieldKey
}>()

const emit = defineEmits<{
  'set-active-field': [field: VendorExtendFieldKey]
}>()

const { t } = useI18n()
const fieldKeys = VENDOR_EXTEND_FIELD_KEYS

const {
  expanded,
  subColGridTemplateColumns,
  toggleExpanded,
  startSubColResize
} = useVendorExtendColumn()

function onToggleClick() {
  toggleExpanded()
}

function fieldLabel(key: VendorExtendFieldKey) {
  return t(`common.vendorExtendCol.fields.${key}`)
}

function fieldShort(key: VendorExtendFieldKey) {
  return t(`common.vendorExtendCol.fieldShort.${key}`)
}

function onFieldCommand(cmd: string | number | object) {
  const k = String(cmd) as VendorExtendFieldKey
  if ((fieldKeys as string[]).includes(k)) emit('set-active-field', k)
}
</script>
