<template>
  <div class="customer-extend-col-header dock-quote-extend-col-header" :class="{ 'is-expanded': expanded }">
    <button
      type="button"
      class="customer-extend-col-toggle-btn"
      :aria-label="
        expanded
          ? t('common.dockQuoteExtendCol.collapse')
          : t('common.dockQuoteExtendCol.expand')
      "
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
            :aria-label="t('common.dockQuoteExtendCol.resizeSubCol')"
            @mousedown.stop="(e) => startSubColResize(index, e)"
          />
        </div>
      </div>
    </template>
    <template v-else>
      <div class="customer-extend-col-header__title">
        <span class="customer-extend-col-header__label">{{
          t('common.dockQuoteExtendCol.columnTitle')
        }}</span>
        <el-dropdown trigger="click" placement="bottom-start" @command="onFieldCommand">
          <button
            type="button"
            class="customer-extend-col-field-picker"
            :aria-label="t('common.dockQuoteExtendCol.pickField')"
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
  DOCK_QUOTE_EXTEND_FIELD_KEYS,
  type DockQuoteExtendFieldKey
} from '@/constants/listDockQuoteExtendColumnSpec'
import { useDockQuoteExtendColumn } from '@/composables/useDockQuoteExtendColumn'

defineProps<{
  activeField: DockQuoteExtendFieldKey
}>()

const emit = defineEmits<{
  'set-active-field': [field: DockQuoteExtendFieldKey]
}>()

const { t } = useI18n()
const fieldKeys = DOCK_QUOTE_EXTEND_FIELD_KEYS

const {
  expanded,
  subColGridTemplateColumns,
  toggleExpanded,
  startSubColResize
} = useDockQuoteExtendColumn()

function onToggleClick() {
  toggleExpanded()
}

function fieldLabel(key: DockQuoteExtendFieldKey) {
  return t(`common.dockQuoteExtendCol.fields.${key}`)
}

function fieldShort(key: DockQuoteExtendFieldKey) {
  return t(`common.dockQuoteExtendCol.fieldShort.${key}`)
}

function onFieldCommand(cmd: string | number | object) {
  const k = String(cmd) as DockQuoteExtendFieldKey
  if ((fieldKeys as string[]).includes(k)) emit('set-active-field', k)
}
</script>
