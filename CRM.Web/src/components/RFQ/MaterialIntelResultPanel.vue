<template>
  <section class="material-intel-panel">
    <div class="material-intel-panel__head">
      <h2 class="material-intel-panel__title">{{ t('materialIntel.title') }}</h2>
      <div class="material-intel-panel__tags">
        <el-tag v-if="fromCache" size="small" type="info">{{ t('materialIntel.fromCache') }}</el-tag>
        <el-tag v-else size="small" type="success">{{ t('materialIntel.live') }}</el-tag>
      </div>
      <el-button v-if="showClose" size="small" text @click="emit('close')">{{ t('materialIntel.close') }}</el-button>
    </div>

    <el-alert
      v-if="showDisclaimer"
      class="material-intel-panel__disclaimer"
      type="warning"
      :closable="false"
      show-icon
      :title="t('materialIntel.disclaimerShort')"
    />

    <div v-if="renderBody" class="material-intel-panel__stack">
      <JsonValueRenderer root :value="renderBody" />
    </div>

    <div class="material-intel-panel__footer">
      <el-button size="small" @click="copyJson">{{ t('materialIntel.copyJson') }}</el-button>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import JsonValueRenderer from '@/components/RFQ/JsonValueRenderer.vue'
import { copyTextToClipboard } from '@/utils/clipboard'
import { isPlainObject, visibleEntries } from '@/utils/jsonDisplay'

const props = defineProps<{
  data: Record<string, unknown> | null | undefined
  fromCache?: boolean
  showClose?: boolean
}>()

const emit = defineEmits<{ close: [] }>()

const { t } = useI18n()

const showDisclaimer = computed(
  () => isPlainObject(props.data) && visibleEntries(props.data).length > 0
)

/** 渲染主体：排除 disclaimer（顶部固定提示，不重复展示 AI 长文） */
const renderBody = computed((): Record<string, unknown> | null => {
  if (!isPlainObject(props.data)) return null
  const entries = visibleEntries(props.data).filter(({ key }) => key !== 'disclaimer')
  if (!entries.length) return null
  return Object.fromEntries(entries.map(({ key, value }) => [key, value]))
})

async function copyJson() {
  if (!props.data) return
  const text = JSON.stringify(props.data, null, 2)
  if (copyTextToClipboard(text)) {
    ElMessage.success(t('materialIntel.copyOk'))
    return
  }
  if (typeof navigator !== 'undefined' && navigator.clipboard?.writeText) {
    try {
      await navigator.clipboard.writeText(text)
      ElMessage.success(t('materialIntel.copyOk'))
      return
    } catch {
      /* fall through */
    }
  }
  ElMessage.error(t('materialIntel.copyFail'))
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.material-intel-panel {
  margin: 0 auto 32px;
  max-width: calc(100% * 2 / 3);
  padding: 20px 22px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: 14px;
  box-shadow: $shadow-md;
}

.material-intel-panel__head {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 12px;
}

.material-intel-panel__title {
  margin: 0;
  flex: 1;
  font-size: 16px;
  font-weight: 600;
  color: $text-primary;
}

.material-intel-panel__disclaimer {
  margin-bottom: 14px;
}

.material-intel-panel__stack {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.material-intel-panel__footer {
  margin-top: 14px;
  padding-top: 12px;
  border-top: 1px solid $border-panel;
}
</style>
