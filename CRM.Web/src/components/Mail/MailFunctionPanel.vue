<template>
  <div class="mail-fn-panel">
    <div class="mail-fn-panel__head">{{ t('myMails.fn.title') }}</div>
    <section class="mail-fn-panel__block">
      <h4 class="mail-fn-panel__title">{{ t('myMails.fn.extractTitle') }}</h4>
      <p class="mail-fn-panel__hint">{{ t('myMails.fn.extractHint') }}</p>
      <el-tooltip
        :disabled="canExtract"
        :content="extractDisabledReason"
        placement="top"
      >
        <span>
          <el-button type="primary" :disabled="!canExtract" :loading="extracting" @click="onExtract">
            {{ t('myMails.fn.extract') }}
          </el-button>
        </span>
      </el-tooltip>
    </section>
    <section v-if="currentRemark" class="mail-fn-panel__block mail-fn-panel__block--remark">
      <h4 class="mail-fn-panel__title">{{ t('myMails.remark.title') }}</h4>
      <p class="mail-fn-panel__remark">{{ currentRemark }}</p>
    </section>
    <AiEntityCreateHost
      ref="aiHostRef"
      entity-type="RFQ"
      :target-route="{ name: 'RFQCreate' }"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import AiEntityCreateHost from '@/components/AiCreate/AiEntityCreateHost.vue'
import { fetchMyMailDetail } from '@/api/myMails'
import { AI_PERMISSION_ENTITY_PARSE_RFQ } from '@/api/ai'
import { useAuthStore } from '@/stores/auth'
import { getApiErrorMessage } from '@/utils/apiError'
import { useMyMailsWorkspace } from '@/composables/useMyMailsWorkspace'

const { t } = useI18n()
const authStore = useAuthStore()
const { detail, selectedId, rows } = useMyMailsWorkspace()
const aiHostRef = ref<InstanceType<typeof AiEntityCreateHost> | null>(null)
const extracting = ref(false)

const canAi = computed(() => authStore.hasPermission(AI_PERMISSION_ENTITY_PARSE_RFQ))
const hasMail = computed(() => !!(detail.value?.id || selectedId.value))
const canExtract = computed(() => canAi.value && hasMail.value)
const currentRemark = computed(() => {
  const id = selectedId.value || detail.value?.id
  if (!id) return ''
  if (detail.value?.id === id) return detail.value.remark?.trim() || ''
  return rows.value.find((r) => r.id === id)?.remark?.trim() || ''
})

const extractDisabledReason = computed(() => {
  if (!canAi.value) return t('myMails.fn.noPermission')
  if (!hasMail.value) return t('myMails.fn.needMail')
  return ''
})

function mailTextFrom(html?: string | null, text?: string | null, snippet?: string | null) {
  const plain = text?.trim()
  if (plain) return plain
  const raw = html?.trim()
  if (raw) {
    return raw.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim()
  }
  return snippet?.trim() || ''
}

async function onExtract() {
  if (!canExtract.value) return
  extracting.value = true
  try {
    const id = selectedId.value || detail.value?.id
    if (!id) {
      ElMessage.warning(t('myMails.fn.needMail'))
      return
    }
    let text = ''
    if (detail.value?.id === id) {
      text = mailTextFrom(detail.value.bodyHtml, detail.value.bodyText, detail.value.snippet)
    }
    if (!text) {
      const full = await fetchMyMailDetail(id)
      text = mailTextFrom(full.bodyHtml, full.bodyText, full.snippet)
    }
    if (!text) {
      ElMessage.warning(t('myMails.fn.emptyBody'))
      return
    }
    await aiHostRef.value?.openWithText(text)
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('myMails.messages.loadFailed')))
  } finally {
    extracting.value = false
  }
}
</script>

<style scoped lang="scss">
@use '@/assets/styles/variables' as *;

.mail-fn-panel {
  padding: 4px 2px 12px;
}

.mail-fn-panel__head {
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 12px;
  font-size: 13px;
}

.mail-fn-panel__block {
  padding: 10px 12px;
  border: 1px solid $border-panel;
  border-radius: 8px;
  background: $layer-3;
}

.mail-fn-panel__block--remark {
  margin-top: 12px;
}

.mail-fn-panel__remark {
  margin: 0;
  font-size: 12px;
  line-height: 1.6;
  color: $text-primary;
  white-space: pre-wrap;
  word-break: break-word;
}

.mail-fn-panel__title {
  margin: 0 0 6px;
  font-size: 13px;
  color: $text-primary;
}

.mail-fn-panel__hint {
  margin: 0 0 12px;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.5;
}
</style>
