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
      <div
        class="crm-list-copy-tooltip__body"
        :class="{ 'crm-list-copy-tooltip__body--with-hint': !!hintText }"
      >
        <div class="crm-list-copy-tooltip__main">
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
        <div v-if="hintText" class="crm-list-copy-tooltip__hint">{{ hintText }}</div>
      </div>
    </template>
    <span
      class="crm-list-copyable-text-cell__value"
      :class="{ 'crm-list-copyable-text-cell__value--danger': tone === 'danger' }"
    >{{ cellText }}</span>
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
    /** danger：红字（如报价型号与需求原型号不一致） */
    tone?: 'default' | 'danger'
    /** 附加提示（如不一致说明），显示在复制 tooltip 内 */
    hint?: string | null
  }>(),
  { emptyText: '—', tone: 'default' }
)

const { t } = useI18n()

const rawText = computed(() => String(props.text ?? '').trim())

const cellText = computed(() => rawText.value || props.emptyText)

const canCopy = computed(() => rawText.value.length > 0)

const hintText = computed(() => String(props.hint ?? '').trim())

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

  &--danger {
    color: var(--el-color-danger);
    font-weight: 600;
  }
}
</style>

<style lang="scss">
/* tooltip 挂到 body；覆盖列表页横向 flex，改为纵向以容纳不一致提示 */
.crm-list-copy-tooltip {
  .crm-list-copy-tooltip__body--with-hint {
    flex-direction: column;
    align-items: stretch;
    gap: 6px;
  }

  .crm-list-copy-tooltip__main {
    display: flex;
    align-items: flex-start;
    gap: 8px;
  }

  .crm-list-copy-tooltip__hint {
    max-width: 280px;
    line-height: 1.4;
    font-size: 12px;
    opacity: 0.92;
    white-space: normal;
    word-break: break-word;
  }
}
</style>
