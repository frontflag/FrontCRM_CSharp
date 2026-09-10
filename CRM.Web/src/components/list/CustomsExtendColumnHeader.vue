<template>
  <div class="customs-extend-col-header" :class="{ 'is-expanded': expanded }">
    <button
      type="button"
      class="customs-extend-col-toggle-btn"
      :aria-label="expanded ? t('common.customsExtendCol.collapse') : t('common.customsExtendCol.expand')"
      @click.stop.prevent="onToggleClick"
      @mousedown.stop
    >
      {{ expanded ? '<' : '>' }}
    </button>
    <template v-if="expanded">
      <div
        class="customs-extend-col-header__cols"
        :style="{ gridTemplateColumns: subColGridTemplateColumns }"
      >
        <div
          v-for="(f, index) in fieldKeys"
          :key="f"
          class="customs-extend-col-header__col-wrap"
        >
          <span class="customs-extend-col-header__col-label">{{ fieldShort(f) }}</span>
          <span
            v-if="index < fieldKeys.length - 1"
            class="customs-extend-sub-col-resizer"
            role="separator"
            :aria-label="t('common.customsExtendCol.resizeSubCol')"
            @mousedown.stop="(e) => startSubColResize(index, e)"
          />
        </div>
      </div>
    </template>
    <template v-else>
      <div class="customs-extend-col-header__title">
        <span class="customs-extend-col-header__label">{{ t('common.customsExtendCol.columnTitle') }}</span>
        <el-dropdown trigger="click" placement="bottom-start" @command="onFieldCommand">
          <button
            type="button"
            class="customs-extend-col-field-picker"
            :aria-label="t('common.customsExtendCol.pickField')"
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
  CUSTOMS_EXTEND_FIELD_KEYS,
  type CustomsExtendFieldKey
} from '@/constants/listCustomsExtendColumnSpec'
import { useCustomsExtendColumn } from '@/composables/useCustomsExtendColumn'

defineProps<{
  activeField: CustomsExtendFieldKey
}>()

const emit = defineEmits<{
  'set-active-field': [field: CustomsExtendFieldKey]
}>()

const { t } = useI18n()
const fieldKeys = CUSTOMS_EXTEND_FIELD_KEYS

const {
  expanded,
  subColGridTemplateColumns,
  toggleExpanded,
  startSubColResize
} = useCustomsExtendColumn()

function onToggleClick() {
  toggleExpanded()
}

function fieldLabel(key: CustomsExtendFieldKey) {
  return t(`common.customsExtendCol.fields.${key}`)
}

function fieldShort(key: CustomsExtendFieldKey) {
  return t(`common.customsExtendCol.fieldShort.${key}`)
}

function onFieldCommand(cmd: string | number | object) {
  const k = String(cmd) as CustomsExtendFieldKey
  if ((fieldKeys as string[]).includes(k)) emit('set-active-field', k)
}
</script>
