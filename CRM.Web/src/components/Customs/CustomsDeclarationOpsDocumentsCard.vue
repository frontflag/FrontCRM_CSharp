<template>
  <section class="ops-card" :class="{ 'ops-card--collapsed': !expanded }">
    <header class="ops-card__head">
      <h3 class="ops-card__title">
        {{ t('customsPages.declarations.opsPanel.docsTitle') }}
        <span v-if="loaded && count > 0" class="ops-docs-count">（<span class="ops-docs-count__n">{{ count }}</span>）</span>
        <span v-else-if="loaded" class="ops-docs-none">{{ t('customsPages.declarations.opsPanel.docsNone') }}</span>
      </h3>
      <button
        type="button"
        class="ops-card__toggle"
        :aria-expanded="expanded"
        :aria-label="expanded ? t('customsPages.declarations.opsPanel.docsCollapse') : t('customsPages.declarations.opsPanel.docsExpand')"
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
        :category-options="CUSTOMS_DECLARATION_DOC_CATEGORIES"
        :doc-category="UPLOAD_DOC_CATEGORY.Contract"
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
        :empty-text="t('customsPages.declarations.opsPanel.docsEmpty')"
        @updated="onListUpdated"
      />
    </div>
  </section>
</template>

<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ArrowDown, ArrowUp } from '@element-plus/icons-vue'
import DocumentUploadPanel from '@/components/Document/DocumentUploadPanel.vue'
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue'
import type { UploadDocumentDto } from '@/api/document'
import {
  CUSTOMS_DECLARATION_DOC_BIZ,
  CUSTOMS_DECLARATION_DOC_CATEGORIES,
  UPLOAD_DOC_CATEGORY
} from '@/constants/uploadDocumentCategory'
import {
  readOpsDocsExpanded,
  writeOpsDocsExpanded,
  emitCustomsDeclarationDocsChanged,
  CUSTOMS_DECLARATION_DOCS_CHANGED,
  CUSTOMS_DECLARATION_OPS_DOCS_EXPANDED_STORAGE_KEY
} from '@/utils/salesOrderOpsDocuments'

const props = defineProps<{
  bizId: string
  canWrite?: boolean
}>()

const { t } = useI18n()
const bizType = CUSTOMS_DECLARATION_DOC_BIZ
const listRef = ref<InstanceType<typeof DocumentListPanel> | null>(null)
const count = ref(0)
const loaded = ref(false)
const expanded = ref(readOpsDocsExpanded(CUSTOMS_DECLARATION_OPS_DOCS_EXPANDED_STORAGE_KEY))
let silentRefresh = false

function onDocsChanged(event: Event) {
  const id = String((event as CustomEvent<string>).detail ?? '').trim()
  if (!id || id !== props.bizId.trim()) return
  silentRefresh = true
  listRef.value?.refresh()
}

function toggleExpanded() {
  expanded.value = !expanded.value
  writeOpsDocsExpanded(CUSTOMS_DECLARATION_OPS_DOCS_EXPANDED_STORAGE_KEY, expanded.value)
}

function onListUpdated(n: number) {
  const prevLoaded = loaded.value
  const prev = count.value
  count.value = n
  loaded.value = true
  if (silentRefresh) {
    silentRefresh = false
    return
  }
  if (prevLoaded && prev !== n) emitCustomsDeclarationDocsChanged(props.bizId)
}

onMounted(() => window.addEventListener(CUSTOMS_DECLARATION_DOCS_CHANGED, onDocsChanged))
onBeforeUnmount(() => window.removeEventListener(CUSTOMS_DECLARATION_DOCS_CHANGED, onDocsChanged))

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
