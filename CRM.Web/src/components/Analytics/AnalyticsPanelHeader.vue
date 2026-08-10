<script setup lang="ts">
import AnalyticsDefinitionButton from './AnalyticsDefinitionButton.vue'

withDefaults(
  defineProps<{
    title: string
    /** h3（趋势/排行区块）或 h4（与饼图标题同级） */
    titleTag?: 'h3' | 'h4'
    unitCaption?: string
    showDefinition?: boolean
    definitionLabel?: string
    definitionChart?: string
    definitionDataSource?: string
    definitionText?: string
    tipKind?: 'chart' | 'metric'
  }>(),
  { titleTag: 'h3', tipKind: 'chart', showDefinition: false }
)
</script>

<template>
  <div class="panel-header">
    <component :is="titleTag" class="title">{{ title }}</component>
    <div v-if="unitCaption || showDefinition" class="title-right">
      <span v-if="unitCaption" class="unit-caption">{{ unitCaption }}</span>
      <AnalyticsDefinitionButton
        v-if="showDefinition"
        :label="definitionLabel"
        :tip-kind="tipKind"
        :chart="definitionChart"
        :data-source="definitionDataSource"
        :text="definitionText"
      />
    </div>
  </div>
</template>

<style scoped lang="scss">
.panel-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 12px;
}

.title {
  margin: 0;
  font-size: 15px;
  font-weight: 600;
}

h4.title {
  font-size: 14px;
}

.title-right {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.unit-caption {
  font-size: 12px;
  font-weight: 400;
  color: var(--el-text-color-secondary);
  white-space: nowrap;
}
</style>
