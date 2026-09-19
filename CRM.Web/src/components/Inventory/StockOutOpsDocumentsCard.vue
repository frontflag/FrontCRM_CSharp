<template>
  <section class="ops-card" :class="{ 'ops-card--collapsed': !expanded }">
    <header class="ops-card__head">
      <h3 class="ops-card__title">
        {{ t('stockOutList.opsPanel.docsTitle') }}
        <span v-if="loaded && count > 0" class="ops-docs-count">（<span class="ops-docs-count__n">{{ count }}</span>）</span>
        <span v-else-if="loaded" class="ops-docs-none">{{ t('stockOutList.opsPanel.docsNone') }}</span>
      </h3>
      <button
        type="button"
        class="ops-card__toggle"
        :aria-expanded="expanded"
        :aria-label="expanded ? t('stockOutList.opsPanel.docsCollapse') : t('stockOutList.opsPanel.docsExpand')"
        @click="toggleExpanded"
      >
        <el-icon>
          <ArrowUp v-if="expanded" />
          <ArrowDown v-else />
        </el-icon>
      </button>
    </header>
    <div v-show="expanded" class="ops-card__body ops-card__body--docs">
      <DocumentUploadPanel
        v-if="canWrite && bizId"
        :biz-type="bizType"
        :biz-id="bizId"
        :max-files="20"
        :max-size-mb="20"
        :remark-allowed="false"
        show-category-select
        compact
        :doc-category="UPLOAD_DOC_CATEGORY.ShipPhoto"
        @uploaded="onUploaded"
      />
      <DocumentListPanel
        v-if="bizId"
        ref="listRef"
        :biz-type="bizType"
        :biz-id="bizId"
        view-mode="list"
        hide-toolbar
        compact
        show-category-tag
        :readonly="!canWrite"
        :empty-text="t('stockOutList.opsPanel.docsEmptyGroup')"
        @updated="onListUpdated"
      />
    </div>
  </section>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ArrowDown, ArrowUp } from '@element-plus/icons-vue'
import DocumentUploadPanel from '@/components/Document/DocumentUploadPanel.vue'
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue'
import type { UploadDocumentDto } from '@/api/document'
import { UPLOAD_DOC_CATEGORY } from '@/constants/uploadDocumentCategory'
import {
  readOpsDocsExpanded,
  writeOpsDocsExpanded,
  STOCK_OUT_OPS_DOCS_EXPANDED_STORAGE_KEY
} from '@/utils/salesOrderOpsDocuments'

const DOC_BIZ = 'STOCK_OUT'

const props = defineProps<{
  bizId: string
  canWrite?: boolean
}>()

const { t } = useI18n()
const bizType = DOC_BIZ
const listRef = ref<InstanceType<typeof DocumentListPanel> | null>(null)
const count = ref(0)
const loaded = ref(false)
const expanded = ref(readOpsDocsExpanded(STOCK_OUT_OPS_DOCS_EXPANDED_STORAGE_KEY))

function toggleExpanded() {
  expanded.value = !expanded.value
  writeOpsDocsExpanded(STOCK_OUT_OPS_DOCS_EXPANDED_STORAGE_KEY, expanded.value)
}

function onListUpdated(n: number) {
  count.value = n
  loaded.value = true
}

function onUploaded(_docs: UploadDocumentDto[]) {
  listRef.value?.refresh()
}

watch(
  () => props.bizId,
  () => {
    loaded.value = false
    count.value = 0
  }
)
</script>

<style scoped lang="scss">
@import '@/assets/styles/so-item-ops-panel.scss';

:deep(.document-upload-panel--compact) {
  background: transparent;
}

:deep(.document-list-panel) {
  width: 100%;
}
</style>
