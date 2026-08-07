<template>
  <img
    class="so-item-flow-here-mark"
    :src="youAreHereHandUrl"
    :alt="label"
    :title="label"
  />
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import youAreHereHandUrl from '@/assets/icons/flow-you-are-here-hand.png'

const props = defineProps<{
  /** 悬停 / 无障碍文案；默认「当前业务节点」 */
  label?: string
}>()

const { t } = useI18n()

const label = computed(
  () => props.label?.trim() || t('layout.flowPanel.youAreHere')
)
</script>

<style scoped lang="scss">
/* 透明底橙色左指手势；与 UI规范「流程页签.当前业务指示」一致 */
.so-item-flow-here-mark {
  display: inline-block;
  width: 28px;
  height: 22px;
  object-fit: contain;
  flex-shrink: 0;
  vertical-align: middle;
  margin-left: 2px;
}
</style>
