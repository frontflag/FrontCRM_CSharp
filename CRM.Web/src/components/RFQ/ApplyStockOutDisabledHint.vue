<template>
  <el-popover placement="left" :width="360" trigger="click" :teleported="true">
    <template #reference>
      <button
        type="button"
        class="apply-stock-out-hint-btn"
        :aria-label="t('salesOrderItemList.messages.applyStockOutHintAria')"
        @click.stop
      >
        <el-icon><QuestionFilled /></el-icon>
      </button>
    </template>
    <div class="apply-stock-out-hint-body">
      <p class="apply-stock-out-hint-text">{{ content.summary }}</p>
      <template v-if="content.details.length">
        <p class="apply-stock-out-hint-subtitle">
          {{ t('salesOrderItemList.messages.applyStockOutHintDetailTitle') }}
        </p>
        <ul class="apply-stock-out-hint-list">
          <li v-for="(line, idx) in content.details" :key="idx">{{ line }}</li>
        </ul>
      </template>
    </div>
  </el-popover>
</template>

<script setup lang="ts">
import { QuestionFilled } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import type { ApplyStockOutDisabledHintContent } from '@/utils/applyStockOutDisabledHint'

defineProps<{
  content: ApplyStockOutDisabledHintContent
}>()

const { t } = useI18n()
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.apply-stock-out-hint-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  margin-left: 4px;
  padding: 0;
  width: 18px;
  height: 18px;
  border: none;
  background: transparent;
  color: $text-muted;
  cursor: pointer;
  vertical-align: middle;
  flex-shrink: 0;

  &:hover {
    color: $cyan-primary;
  }

  .el-icon {
    font-size: 14px;
  }
}

.apply-stock-out-hint-body {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.apply-stock-out-hint-text {
  margin: 0;
  font-size: 13px;
  line-height: 1.55;
  color: $text-secondary;
}

.apply-stock-out-hint-subtitle {
  margin: 0;
  font-size: 12px;
  font-weight: 600;
  line-height: 1.4;
  color: $text-primary;
}

.apply-stock-out-hint-list {
  margin: 0;
  padding-left: 18px;
  font-size: 13px;
  line-height: 1.55;
  color: $text-secondary;

  li + li {
    margin-top: 6px;
  }
}
</style>
