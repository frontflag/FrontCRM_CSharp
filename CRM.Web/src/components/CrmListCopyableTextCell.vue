<template>
  <el-tooltip
    placement="top"
    :show-after="200"
    :hide-after="120"
    :enterable="true"
    effect="dark"
    popper-class="crm-list-copy-tooltip"
  >
    <template #content>
      <div class="crm-list-copy-tooltip__body">
        <span class="crm-list-copy-tooltip__text">{{ cellText }}</span>
        <button
          type="button"
          class="crm-list-copy-tooltip__btn"
          :disabled="!canCopy"
          :aria-label="t('common.copy')"
          @click.stop="onCopyClick"
        >
          <el-icon :size="14"><CopyDocument /></el-icon>
        </button>
      </div>
    </template>
    <span class="crm-list-copyable-text-cell__value">{{ cellText }}</span>
  </el-tooltip>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { ElMessage } from 'element-plus'
import { CopyDocument } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import { copyTextToClipboard } from '@/utils/clipboard'

const props = withDefaults(
  defineProps<{
    /** 字段原文；空时单元格显示 emptyText */
    text?: string | null | number
    emptyText?: string
  }>(),
  { emptyText: '—' }
)

const { t } = useI18n()

const rawText = computed(() => String(props.text ?? '').trim())

const cellText = computed(() => rawText.value || props.emptyText)

const canCopy = computed(() => rawText.value.length > 0)

function onCopyClick() {
  if (!canCopy.value) return
  const ok = copyTextToClipboard(rawText.value)
  if (ok) {
    ElMessage.success(t('common.copySuccess'))
  } else {
    ElMessage.error(t('common.copyFailed'))
  }
}
</script>

<style scoped lang="scss">
.crm-list-copyable-text-cell__value {
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  width: 100%;
}
</style>
