<script setup lang="ts">
defineProps<{
  items: {
    key: string
    label: string
    value: string
    /** 原币分档行（如 CNY 1,234.56），显示在折算 USD 下方 */
    currencyLines?: string[]
    /** 原币区小标题（如「原币」） */
    currencyCaption?: string
    /** 主金额上方小标题（如「折算 USD」） */
    valueCaption?: string
    tone?: 'todo' | 'snapshot'
    drillable?: boolean
    /** money：缩小字号并禁止折行，适配 12 位整数金额 */
    valueFormat?: 'money' | 'text'
  }[]
}>()

const emit = defineEmits<{
  'item-click': [key: string]
}>()
</script>

<template>
  <div class="kpi-grid">
    <div
      v-for="item in items"
      :key="item.key"
      class="kpi-card"
      :class="{
        'kpi-card--todo': item.tone === 'todo',
        'kpi-card--clickable': item.drillable
      }"
      :role="item.drillable ? 'button' : undefined"
      :tabindex="item.drillable ? 0 : undefined"
      @click="item.drillable && emit('item-click', item.key)"
      @keydown.enter="item.drillable && emit('item-click', item.key)"
    >
      <span class="kpi-label">{{ item.label }}</span>
      <div class="kpi-amount-block">
        <span v-if="item.valueCaption" class="kpi-value-caption">{{ item.valueCaption }}</span>
        <span
          class="kpi-value"
          :class="{ 'kpi-value--money': item.valueFormat === 'money' }"
        >{{ item.value }}</span>
        <div v-if="item.currencyLines?.length" class="kpi-currencies">
          <span v-if="item.currencyCaption" class="kpi-currency-caption">{{ item.currencyCaption }}</span>
          <span
            v-for="(line, idx) in item.currencyLines"
            :key="`${item.key}-cur-${idx}`"
            class="kpi-currency-line"
          >{{ line }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.kpi-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 12px;
}

.kpi-card {
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  padding: 14px 16px;
}

.kpi-card--todo {
  border-color: var(--el-color-warning-light-5);
  background: var(--el-color-warning-light-9);
}

.kpi-card--clickable {
  cursor: pointer;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;

  &:hover {
    border-color: var(--el-color-primary-light-5);
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
  }
}

.kpi-label {
  display: block;
  font-size: 13px;
  color: var(--el-text-color-secondary);
  margin-bottom: 6px;
}

.kpi-amount-block {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.kpi-value-caption {
  font-size: 11px;
  color: var(--el-text-color-secondary);
  line-height: 1.2;
}

.kpi-value {
  display: block;
  font-size: 22px;
  font-weight: 600;
  color: var(--el-text-color-primary);
  line-height: 1.25;
}

.kpi-value--money {
  font-size: 13px;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
  letter-spacing: -0.02em;
}

.kpi-card--todo .kpi-value--money {
  font-size: 15px;
}

.kpi-currencies {
  display: flex;
  flex-direction: column;
  gap: 2px;
  margin-top: 4px;
  padding-top: 6px;
  border-top: 1px dashed var(--el-border-color-lighter);
}

.kpi-currency-caption {
  font-size: 11px;
  color: var(--el-text-color-secondary);
  margin-bottom: 2px;
}

.kpi-currency-line {
  display: block;
  font-size: 11px;
  font-weight: 500;
  font-variant-numeric: tabular-nums;
  color: var(--el-text-color-secondary);
  white-space: nowrap;
  letter-spacing: -0.01em;
}

.kpi-card--todo .kpi-currency-line {
  color: var(--el-text-color-regular);
}
</style>
