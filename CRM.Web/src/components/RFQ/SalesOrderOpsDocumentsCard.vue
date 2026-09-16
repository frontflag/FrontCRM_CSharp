<template>
  <section class="ops-card" :class="{ 'ops-card--collapsed': !expanded }">
    <header class="ops-card__head">
      <h3 class="ops-card__title">
        {{ t(`${i18nPrefix}.docsTitle`) }}
        <span v-if="loaded && count > 0" class="ops-docs-count">（<span class="ops-docs-count__n">{{ count }}</span>）</span>
        <span v-else-if="loaded" class="ops-docs-none">{{ t(`${i18nPrefix}.docsNone`) }}</span>
      </h3>
      <button
        type="button"
        class="ops-card__toggle"
        :aria-expanded="expanded"
        :aria-label="expanded ? t(`${i18nPrefix}.docsCollapse`) : t(`${i18nPrefix}.docsExpand`)"
        @click="toggleExpanded"
      >
        <el-icon>
          <ArrowUp v-if="expanded" />
          <ArrowDown v-else />
        </el-icon>
      </button>
    </header>
    <div v-show="expanded" class="ops-card__body ops-card__body--docs">
      <p v-if="loading" class="ops-docs-empty">{{ t(`${i18nPrefix}.docsLoading`) }}</p>
      <p v-else-if="!docs.length" class="ops-docs-empty">
        {{ t(`${i18nPrefix}.docsEmptyExpanded`) }}
      </p>
      <ul v-else class="ops-docs-list">
        <li v-for="doc in docs" :key="doc.id">
          <button
            type="button"
            class="ops-docs-name"
            :title="doc.originalFileName"
            @click="onOpen(doc)"
          >
            {{ doc.originalFileName }}
          </button>
        </li>
      </ul>
    </div>
    <DocumentPreviewDialog v-model="previewVisible" :document-id="previewId" :mime-type="previewMime" />
  </section>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ArrowDown, ArrowUp } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { documentApi, type UploadDocumentDto } from '@/api/document'
import DocumentPreviewDialog from '@/components/Document/DocumentPreviewDialog.vue'
import { getApiErrorMessage } from '@/utils/apiError'
import {
  isInlinePreviewableUpload,
  previewMimeForUpload,
  readOpsDocsExpanded,
  writeOpsDocsExpanded,
  SO_OPS_DOCS_EXPANDED_STORAGE_KEY
} from '@/utils/salesOrderOpsDocuments'

const props = withDefaults(
  defineProps<{
    bizType: string
    bizId: string
    i18nPrefix?: string
    storageKey?: string
  }>(),
  {
    i18nPrefix: 'salesOrderItemList.opsPanel',
    storageKey: SO_OPS_DOCS_EXPANDED_STORAGE_KEY
  }
)

const { t } = useI18n()
const docs = ref<UploadDocumentDto[]>([])
const loading = ref(false)
const loaded = ref(false)
const expanded = ref(readOpsDocsExpanded(props.storageKey))
const previewVisible = ref(false)
const previewId = ref('')
const previewMime = ref('')

const count = computed(() => docs.value.length)
const i18nPrefix = computed(() => props.i18nPrefix)

function toggleExpanded() {
  expanded.value = !expanded.value
  writeOpsDocsExpanded(props.storageKey, expanded.value)
}

async function fetchDocs(orderId: string) {
  if (!orderId) {
    docs.value = []
    loaded.value = true
    return
  }
  loading.value = true
  loaded.value = false
  try {
    const list = await documentApi.getDocuments(props.bizType, orderId)
    docs.value = Array.isArray(list) ? list : []
  } catch {
    docs.value = []
  } finally {
    loading.value = false
    loaded.value = true
  }
}

watch(
  () => [props.bizType, props.bizId] as const,
  ([, id]) => {
    docs.value = []
    loaded.value = false
    void fetchDocs(id)
  },
  { immediate: true }
)

async function onOpen(doc: UploadDocumentDto) {
  if (isInlinePreviewableUpload(doc)) {
    previewId.value = doc.id
    previewMime.value = previewMimeForUpload(doc)
    previewVisible.value = true
    return
  }
  try {
    await ElMessageBox.confirm(
      t(`${props.i18nPrefix}.docsCannotPreview`),
      t(`${props.i18nPrefix}.docsCannotPreviewTitle`),
      {
        confirmButtonText: t(`${props.i18nPrefix}.docsDownload`),
        cancelButtonText: t('common.cancel'),
        type: 'warning'
      }
    )
  } catch {
    return
  }
  try {
    await documentApi.downloadDocument(doc.id, doc.originalFileName)
  } catch (e: unknown) {
    ElMessage.error(getApiErrorMessage(e, t(`${props.i18nPrefix}.docsDownloadFailed`)))
  }
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/so-item-ops-panel.scss';
</style>

