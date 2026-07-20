<template>
  <el-drawer
    v-model="visible"
    :title="t('aiAssistant.title')"
    direction="rtl"
    size="420px"
    class="ai-assistant-drawer"
    :close-on-click-modal="true"
    @closed="onClosed"
  >
    <div class="ai-assistant-body">
      <div class="ai-assistant-chips">
        <button
          type="button"
          class="chip"
          :class="{ active: preferredCategory === 'bug' }"
          :disabled="busy || sessionEnded"
          @click="startWithCategory('bug')"
        >
          {{ t('aiAssistant.chipBug') }}
        </button>
        <button
          type="button"
          class="chip"
          :class="{ active: preferredCategory === 'suggestion' }"
          :disabled="busy || sessionEnded"
          @click="startWithCategory('suggestion')"
        >
          {{ t('aiAssistant.chipSuggestion') }}
        </button>
      </div>

      <div ref="listRef" class="ai-assistant-messages" v-loading="starting">
        <div
          v-for="m in messages"
          :key="m.id"
          class="msg"
          :class="m.role === 'assistant' ? 'msg--assistant' : 'msg--user'"
        >
          <div class="msg-bubble">
            <p class="msg-text">{{ m.content || '' }}</p>
            <span v-if="m.attachmentDocumentId || m._localHasImage" class="msg-attach">{{ t('aiAssistant.hasImage') }}</span>
          </div>
        </div>
        <div v-if="busy" class="msg msg--assistant">
          <div class="msg-bubble msg-bubble--thinking">
            <span class="thinking-dots" aria-hidden="true">
              <i /><i /><i />
            </span>
            <span class="thinking-text">{{ t('aiAssistant.thinking') }}</span>
          </div>
        </div>
        <div v-if="!starting && !busy && messages.length === 0" class="empty-hint">
          {{ t('aiAssistant.emptyHint') }}
        </div>
      </div>

      <div v-if="pendingImagePreview" class="pending-image">
        <img :src="pendingImagePreview" alt="" />
        <button type="button" class="pending-clear" @click="clearPendingImage">×</button>
      </div>

      <div class="ai-assistant-input">
        <el-input
          v-model="draft"
          type="textarea"
          :rows="3"
          :placeholder="sessionEnded ? t('aiAssistant.endedPlaceholder') : t('aiAssistant.inputPlaceholder')"
          :disabled="busy || sessionEnded || starting"
          @keydown.enter.exact.prevent="onSend"
          @paste="onPaste"
        />
        <div class="input-actions">
          <label class="file-btn" :class="{ disabled: busy || sessionEnded }">
            <input
              type="file"
              accept="image/*"
              :disabled="busy || sessionEnded"
              hidden
              @change="onFileChange"
            />
            {{ t('aiAssistant.uploadImage') }}
          </label>
          <el-button
            v-if="sessionEnded"
            size="small"
            @click="restartSession()"
          >
            {{ t('aiAssistant.newSession') }}
          </el-button>
          <el-button
            type="primary"
            size="small"
            :loading="busy"
            :disabled="sessionEnded || starting || (!draft.trim() && !pendingImageBase64)"
            @click="onSend"
          >
            {{ t('aiAssistant.send') }}
          </el-button>
        </div>
      </div>
    </div>
  </el-drawer>
</template>

<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import {
  aiAssistantApi,
  type AiAssistantMessage
} from '@/api/aiAssistant'
import { getApiErrorMessage } from '@/utils/apiError'

type UiMessage = AiAssistantMessage & { _localHasImage?: boolean }

const props = defineProps<{ modelValue: boolean }>()
const emit = defineEmits<{ 'update:modelValue': [boolean] }>()

const { t } = useI18n()
const route = useRoute()

const visible = computed({
  get: () => props.modelValue,
  set: (v: boolean) => emit('update:modelValue', v)
})

const starting = ref(false)
const busy = ref(false)
const sessionId = ref<string | null>(null)
const sessionStatus = ref('open')
const preferredCategory = ref<string | null>(null)
const messages = ref<UiMessage[]>([])
const draft = ref('')
const listRef = ref<HTMLElement | null>(null)
const pendingImageBase64 = ref<string | null>(null)
const pendingImageMime = ref<string | null>(null)
const pendingImageName = ref<string | null>(null)
const pendingImagePreview = ref<string | null>(null)

const sessionEnded = computed(() => {
  const s = (sessionStatus.value || '').toLowerCase()
  return s === 'submitted' || s === 'abandoned'
})

watch(visible, async (open) => {
  if (open && !sessionId.value) {
    await restartSession()
  }
})

async function restartSession(category?: string | null) {
  starting.value = true
  busy.value = false
  draft.value = ''
  clearPendingImage()
  messages.value = []
  sessionId.value = null
  sessionStatus.value = 'open'
  if (category !== undefined) preferredCategory.value = category
  try {
    const params = route.params && Object.keys(route.params).length
      ? JSON.stringify(route.params)
      : undefined
    const query = route.query && Object.keys(route.query).length
      ? JSON.stringify(route.query)
      : undefined
    const res = await aiAssistantApi.createSession({
      pageUrl: typeof window !== 'undefined' ? window.location.href : route.fullPath,
      routeName: typeof route.name === 'string' ? route.name : undefined,
      routeParamsJson: params,
      routeQueryJson: query,
      userAgent: typeof navigator !== 'undefined' ? navigator.userAgent : undefined,
      preferredCategory: preferredCategory.value
    })
    sessionId.value = res.sessionId
    sessionStatus.value = res.status
    messages.value = [
      {
        id: `welcome-${res.sessionId}`,
        role: 'assistant',
        content: res.welcomeMessage,
        createTime: new Date().toISOString()
      }
    ]
    await scrollToBottom()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('aiAssistant.startFailed')))
  } finally {
    starting.value = false
  }
}

async function startWithCategory(cat: 'bug' | 'suggestion') {
  preferredCategory.value = cat
  await restartSession(cat)
}

async function onSend() {
  if (busy.value || sessionEnded.value || !sessionId.value) return
  const text = draft.value.trim()
  const hasImage = !!pendingImageBase64.value
  if (!text && !hasImage) return

  const payload = {
    text: text || undefined,
    imageBase64: pendingImageBase64.value || undefined,
    imageMimeType: pendingImageMime.value || undefined,
    imageFileName: pendingImageName.value || undefined
  }

  const optimisticId = `local-user-${Date.now()}`
  messages.value = [
    ...messages.value,
    {
      id: optimisticId,
      role: 'user',
      content: text || (hasImage ? t('aiAssistant.hasImage') : ''),
      createTime: new Date().toISOString(),
      _localHasImage: hasImage && !text
    }
  ]
  draft.value = ''
  clearPendingImage()
  busy.value = true
  await scrollToBottom()

  try {
    const res = await aiAssistantApi.sendMessage(sessionId.value, payload)
    sessionStatus.value = res.status
    messages.value = res.messages?.length
      ? res.messages
      : [
          ...messages.value.filter(m => m.id !== optimisticId),
          {
            id: optimisticId,
            role: 'user',
            content: text || t('aiAssistant.hasImage'),
            createTime: new Date().toISOString()
          },
          {
            id: `a-${Date.now()}`,
            role: 'assistant',
            content: res.assistantMessage,
            createTime: new Date().toISOString()
          }
        ]
    await scrollToBottom()
  } catch (e) {
    messages.value = messages.value.filter(m => m.id !== optimisticId)
    draft.value = text
    ElMessage.error(getApiErrorMessage(e, t('aiAssistant.sendFailed')))
  } finally {
    busy.value = false
  }
}

function onPaste(e: ClipboardEvent) {
  const items = e.clipboardData?.items
  if (!items) return
  for (const item of items) {
    if (item.type.startsWith('image/')) {
      e.preventDefault()
      const file = item.getAsFile()
      if (file) void readImageFile(file)
      break
    }
  }
}

function onFileChange(e: Event) {
  const input = e.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (file) void readImageFile(file)
}

function readImageFile(file: File) {
  if (!file.type.startsWith('image/')) {
    ElMessage.warning(t('aiAssistant.imageOnly'))
    return
  }
  if (file.size > 8 * 1024 * 1024) {
    ElMessage.warning(t('aiAssistant.imageTooLarge'))
    return
  }
  const reader = new FileReader()
  reader.onload = () => {
    const result = String(reader.result || '')
    const comma = result.indexOf(',')
    pendingImagePreview.value = result
    pendingImageBase64.value = comma > 0 ? result.slice(comma + 1) : result
    pendingImageMime.value = file.type
    pendingImageName.value = file.name
  }
  reader.readAsDataURL(file)
}

function clearPendingImage() {
  pendingImageBase64.value = null
  pendingImageMime.value = null
  pendingImageName.value = null
  pendingImagePreview.value = null
}

async function scrollToBottom() {
  await nextTick()
  const el = listRef.value
  if (el) el.scrollTop = el.scrollHeight
}

function onClosed() {
  // 保留会话，再次打开可继续；结束态可点「新会话」
}
</script>

<style scoped lang="scss">
.ai-assistant-body {
  display: flex;
  flex-direction: column;
  height: 100%;
  gap: 10px;
  min-height: 0;
}

.ai-assistant-chips {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.chip {
  border: 1px solid var(--el-border-color);
  background: var(--el-fill-color-blank);
  color: var(--el-text-color-regular);
  border-radius: 999px;
  padding: 4px 12px;
  font-size: 12px;
  cursor: pointer;
  &.active {
    border-color: var(--el-color-primary);
    color: var(--el-color-primary);
    background: var(--el-color-primary-light-9);
  }
  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }
}

.ai-assistant-messages {
  flex: 1;
  min-height: 220px;
  overflow: auto;
  padding: 8px 4px;
  display: flex;
  flex-direction: column;
  gap: 10px;
  background: var(--el-fill-color-lighter);
  border-radius: 8px;
}

.msg {
  display: flex;
  &--user {
    justify-content: flex-end;
    .msg-bubble {
      background: var(--el-color-primary-light-8);
    }
  }
  &--assistant {
    justify-content: flex-start;
    .msg-bubble {
      background: var(--el-bg-color);
      border: 1px solid var(--el-border-color-lighter);
    }
  }
}

.msg-bubble {
  max-width: 92%;
  border-radius: 10px;
  padding: 8px 10px;
}

.msg-bubble--thinking {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  color: var(--el-text-color-secondary);
}

.thinking-text {
  font-size: 12px;
}

.thinking-dots {
  display: inline-flex;
  align-items: center;
  gap: 2px;
  i {
    width: 3px;
    height: 3px;
    border-radius: 50%;
    background: var(--el-text-color-secondary);
    opacity: 0.35;
    animation: ai-thinking-bounce 1.2s infinite ease-in-out;
    &:nth-child(2) {
      animation-delay: 0.15s;
    }
    &:nth-child(3) {
      animation-delay: 0.3s;
    }
  }
}

@keyframes ai-thinking-bounce {
  0%,
  80%,
  100% {
    opacity: 0.3;
    transform: translateY(0);
  }
  40% {
    opacity: 1;
    transform: translateY(-2px);
  }
}

.msg-text {
  margin: 0;
  white-space: pre-wrap;
  word-break: break-word;
  font-size: 13px;
  line-height: 1.5;
}

.msg-attach {
  display: inline-block;
  margin-top: 4px;
  font-size: 11px;
  color: var(--el-text-color-secondary);
}

.empty-hint {
  margin: auto;
  color: var(--el-text-color-secondary);
  font-size: 13px;
}

.pending-image {
  position: relative;
  width: 96px;
  height: 72px;
  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    border-radius: 6px;
    border: 1px solid var(--el-border-color);
  }
  .pending-clear {
    position: absolute;
    top: -6px;
    right: -6px;
    width: 20px;
    height: 20px;
    border-radius: 50%;
    border: none;
    background: var(--el-color-danger);
    color: #fff;
    cursor: pointer;
    line-height: 1;
  }
}

.ai-assistant-input {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.input-actions {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 8px;
}

.file-btn {
  font-size: 12px;
  color: var(--el-color-primary);
  cursor: pointer;
  margin-right: auto;
  &.disabled {
    opacity: 0.5;
    pointer-events: none;
  }
}
</style>
