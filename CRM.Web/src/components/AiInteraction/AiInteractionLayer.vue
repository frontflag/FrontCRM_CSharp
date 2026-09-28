<template>
  <Teleport to="body">
    <div
      v-if="open"
      class="ai-interaction-layer"
      role="dialog"
      aria-modal="true"
      :aria-label="t('aiInteraction.title')"
    >
      <div class="ai-interaction-dim" />
      <div class="ai-exit-anchor">
        <button type="button" class="ai-exit" @click="exitLikeEsc">
          <span>{{ t('aiInteraction.exit') }}</span>
          <svg viewBox="0 0 16 16" aria-hidden="true">
            <path d="M4 4l8 8M12 4l-8 8" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" />
          </svg>
        </button>
        <p class="ai-exit-hint">{{ t('aiInteraction.exitHint') }}</p>
      </div>
      <div class="ai-interaction-stage" :class="{ 'is-chatting': chatting }">
        <div ref="transcriptRef" class="ai-interaction-transcript">
          <div
            v-for="turn in turns"
            :key="turn.id"
            class="turn"
            :class="`turn--${turn.kind}`"
          >
            <p v-if="turn.kind === 'marker'" class="marker">
              {{ t('aiInteraction.switchedTo', { name: skillName(turn.skill) }) }}
            </p>
            <div v-else-if="turn.kind === 'create'" class="create-card" :aria-busy="createBusyId === turn.id">
              <p class="bubble-text">{{ turn.text }}</p>
              <div class="reply-panel">
                <template v-if="createBusyId !== turn.id">
                  <p class="create-title">{{ t('aiInteraction.createTitle') }}</p>
                  <div class="create-actions">
                    <button
                      v-for="kind in createActions"
                      :key="kind"
                      type="button"
                      class="skill"
                      :disabled="createBusy"
                      @click="startCreate(turn.id, kind, turn.text)"
                    >
                      {{ createLabel(kind) }}
                    </button>
                  </div>
                </template>
                <p v-else-if="createBusyKind" class="create-parsing">
                  <span class="create-spinner" aria-hidden="true" />
                  {{ t('aiInteraction.createParsing', { name: createLabel(createBusyKind) }) }}
                </p>
              </div>
            </div>
            <div v-else-if="turn.kind === 'user'" class="bubble bubble--user">
              <p class="bubble-text">{{ turn.text }}</p>
            </div>
            <div v-else class="bubble bubble--assistant">
              <div class="assistant-mark" aria-hidden="true">
                <span class="ai-face"></span>
              </div>
              <p class="bubble-text">{{ turn.text }}</p>
              <div v-if="turn.kind === 'assistant' && turn.chart" class="data-chart">
                <p v-if="turn.chart.type === 'number'" class="data-number">
                  {{ formatDataValue(turn.chart.points[0]?.value) }}
                  <span>{{ turn.chart.unit }}</span>
                </p>
                <div v-else-if="turn.chart.type === 'line'">
                  <svg class="data-line" viewBox="0 0 280 96" role="img">
                    <polyline :points="linePoints(turn.chart.points)" fill="none" stroke="#2563eb" stroke-width="2" />
                  </svg>
                  <div class="data-line-labels">
                    <span v-for="point in turn.chart.points" :key="point.label">{{ point.label }}</span>
                  </div>
                </div>
                <div v-else class="data-bars">
                  <div v-for="point in turn.chart.points" :key="point.label" class="data-bar-row">
                    <span class="data-bar-label">{{ point.label }}</span>
                    <span class="data-bar-track">
                      <span class="data-bar-fill" :style="{ width: barWidth(turn.chart.points, point.value) }" />
                    </span>
                  </div>
                </div>
                <p v-if="turn.chart.type !== 'number' && turn.chart.unit" class="data-unit">{{ turn.chart.unit }}</p>
              </div>
              <table v-if="turn.kind === 'assistant' && turn.rows?.length" class="data-table">
                <tbody>
                  <tr v-for="row in turn.rows" :key="row.label">
                    <td>
                      <router-link
                        v-if="row.routeName"
                        :to="dataRoute(row.routeName, row.routeQueryKey, row.routeQueryValue)"
                        target="_blank"
                        rel="noopener"
                      >
                        {{ row.label }}
                      </router-link>
                      <template v-else>{{ row.label }}</template>
                    </td>
                    <td>{{ row.valueText || '—' }}</td>
                  </tr>
                </tbody>
              </table>
              <p v-if="turn.kind === 'assistant' && turn.basisNote" class="data-basis">{{ turn.basisNote }}</p>
              <router-link
                v-if="turn.kind === 'assistant' && turn.link"
                class="data-link"
                :to="dataRoute(turn.link.routeName, turn.link.queryKey, turn.link.queryValue)"
                target="_blank"
                rel="noopener"
              >
                {{ turn.link.label }}
              </router-link>
              <div v-if="turn.kind === 'assistant' && turn.options?.length && isLatestDataTurn(turn)" class="data-options">
                <button
                  v-for="option in turn.options"
                  :key="option.id"
                  type="button"
                  class="chip"
                  :disabled="busy"
                  @click="sendOption(option.label)"
                >
                  {{ option.label }}
                </button>
              </div>
              <div v-if="assistantCitations(turn).length" class="reply-panel">
                <p class="create-title">{{ t('aiInteraction.refsTitle') }}</p>
                <ul class="refs">
                  <li v-for="item in assistantCitations(turn)" :key="item.anchor">
                    <a :href="readHref(assistantVersion(turn), item.anchor)" target="_blank" rel="noopener">
                      {{ item.heading || item.anchor }}
                    </a>
                  </li>
                </ul>
              </div>
            </div>
          </div>
          <div v-if="busy" class="turn turn--assistant">
            <div class="bubble bubble--assistant bubble--thinking">
              <div class="assistant-mark" aria-hidden="true">
                <span class="ai-face"></span>
              </div>
              <span>{{ t('aiInteraction.thinking') }}</span>
            </div>
          </div>
        </div>

        <div class="ai-interaction-dock">
          <div class="composer">
            <div v-if="showCategory" class="category-row">
              <button
                type="button"
                class="chip"
                :class="{ active: preferredCategory === 'bug' }"
                :disabled="busy || feedbackEnded"
                @click="preferredCategory = 'bug'"
              >
                {{ t('aiAssistant.chipBug') }}
              </button>
              <button
                type="button"
                class="chip"
                :class="{ active: preferredCategory === 'suggestion' }"
                :disabled="busy || feedbackEnded"
                @click="preferredCategory = 'suggestion'"
              >
                {{ t('aiAssistant.chipSuggestion') }}
              </button>
            </div>
            <div v-if="pendingImagePreview" class="pending-image">
              <img :src="pendingImagePreview" alt="" />
              <button type="button" class="pending-clear" @click="clearPendingImage">×</button>
            </div>
            <textarea
              ref="inputRef"
              v-model="draft"
              rows="3"
              :placeholder="composerPlaceholder"
              :disabled="composerDisabled"
              @keydown="onComposerKeydown"
              @paste="onPaste"
            />
            <div class="composer-actions">
              <label v-if="allowImage" class="file-btn" :class="{ disabled: composerDisabled }">
                <input type="file" accept="image/*" hidden :disabled="composerDisabled" @change="onFileChange" />
                {{ t('aiAssistant.uploadImage') }}
              </label>
              <button type="button" class="send" :disabled="!canSend" @click="onSend">
                {{ t('aiInteraction.send') }}
              </button>
            </div>
            <p v-if="errorText" class="error">{{ errorText }}</p>
          </div>

          <div class="skills" role="toolbar" :aria-label="t('aiInteraction.skills')">
            <button
              v-for="skill in skills"
              :key="skill"
              type="button"
              class="skill"
              :class="{ active: selectedSkill === skill }"
              :aria-pressed="selectedSkill === skill"
              :disabled="busy"
              @click="selectSkill(skill)"
            >
              {{ skillName(skill) }}
            </button>
          </div>
          <p v-if="autoCaption" class="auto-caption">{{ autoCaption }}</p>
        </div>
      </div>
    </div>
    <AiEntityCreateHost
      v-if="open"
      ref="createHostRef"
      :entity-type="createEntityType"
      :target-route="createRoute"
    />
  </Teleport>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, type RouteLocationRaw } from 'vue-router'
import { useI18n } from 'vue-i18n'
import {
  AI_PERMISSION_ENTITY_PARSE_CUSTOMER,
  AI_PERMISSION_ENTITY_PARSE_RFQ,
  AI_PERMISSION_ENTITY_PARSE_VENDOR
} from '@/api/ai'
import { aiAssistantApi, type AiDataQueryLink, type AiDataQueryRow, type AiDataQueryState } from '@/api/aiAssistant'
import { knowledgeBaseApi, type KbCitation } from '@/api/knowledgeBase'
import AiEntityCreateHost from '@/components/AiCreate/AiEntityCreateHost.vue'
import { useAuthStore } from '@/stores'
import { getApiErrorMessage } from '@/utils/apiError'
import { hasForeignOverlay, shouldStartAiRound } from '@/utils/aiInteractionHotkey'
import {
  chooseAiSkill,
  formatAiTranscript,
  isPasteCreateSource,
  type AiInteractionSkill,
  type PasteCreateKind
} from '@/utils/aiInteractionTranscript'
import type { AiPrefillEntityType } from '@/utils/aiPrefill'

type Turn =
  | { kind: 'user'; id: string; text: string; skill: AiInteractionSkill }
  | {
      kind: 'assistant'
      id: string
      text: string
      skill: AiInteractionSkill
      citations?: KbCitation[]
      versionId?: string | null
      basisNote?: string | null
      options?: { id: string; label: string }[]
      chart?: { type: string; unit?: string; points: { label: string; value?: number | null }[] } | null
      rows?: AiDataQueryRow[]
      link?: AiDataQueryLink | null
      dataState?: AiDataQueryState | null
    }
  | { kind: 'marker'; id: string; skill: AiInteractionSkill }
  | { kind: 'create'; id: string; text: string }

const { t } = useI18n()
const route = useRoute()
const authStore = useAuthStore()

const open = ref(false)
const draft = ref('')
const busy = ref(false)
const errorText = ref('')
const turns = ref<Turn[]>([])
const selectedSkill = ref<AiInteractionSkill | null>(null)
const routeMode = ref<'user' | 'auto' | null>(null)
const preferredCategory = ref<'bug' | 'suggestion' | null>(null)
const sessionId = ref<string | null>(null)
const sessionStatus = ref('open')
const pendingImageBase64 = ref<string | null>(null)
const pendingImageMime = ref<string | null>(null)
const pendingImageName = ref<string | null>(null)
const pendingImagePreview = ref<string | null>(null)
const inputRef = ref<HTMLTextAreaElement | null>(null)
const transcriptRef = ref<HTMLElement | null>(null)
const createHostRef = ref<InstanceType<typeof AiEntityCreateHost> | null>(null)
const createBusy = ref(false)
const createBusyId = ref<string | null>(null)
const createBusyKind = ref<PasteCreateKind | null>(null)
const createEntityType = ref<AiPrefillEntityType>('CUSTOMER')
const createRoute = ref<RouteLocationRaw>({ name: 'CustomerCreate' })

const createActions = computed<PasteCreateKind[]>(() => {
  const list: PasteCreateKind[] = []
  if (authStore.hasPermission(AI_PERMISSION_ENTITY_PARSE_CUSTOMER)) list.push('customer')
  if (authStore.hasPermission(AI_PERMISSION_ENTITY_PARSE_VENDOR)) list.push('vendor')
  if (authStore.hasPermission(AI_PERMISSION_ENTITY_PARSE_RFQ)) list.push('rfq')
  return list
})
/** 反馈 / 培训教材 / 操作手册：任意已登录用户可用 */
const canFeedback = computed(() => !!authStore.user)
const canHandbook = computed(() => !!authStore.user)
const canOps = computed(() => !!authStore.user)
const canData = computed(() => authStore.hasPermission('biz.ai.data.query'))
const skills = computed<AiInteractionSkill[]>(() => {
  const list: AiInteractionSkill[] = []
  if (canFeedback.value) list.push('feedback')
  if (canHandbook.value) list.push('handbook')
  if (canOps.value) list.push('ops')
  if (canData.value) list.push('data')
  return list
})
const chatting = computed(() => turns.value.length > 0 || busy.value)
const feedbackEnded = computed(() => {
  const status = (sessionStatus.value || '').toLowerCase()
  return status === 'submitted' || status === 'abandoned'
})
const showCategory = computed(() => selectedSkill.value === 'feedback' && !sessionId.value)
const allowImage = computed(
  () =>
    selectedSkill.value !== 'handbook' &&
    selectedSkill.value !== 'ops' &&
    selectedSkill.value !== 'data' &&
    canFeedback.value
)
const composerDisabled = computed(
  () => busy.value || (selectedSkill.value === 'feedback' && feedbackEnded.value)
)
const composerPlaceholder = computed(() => {
  if (selectedSkill.value === 'feedback' && feedbackEnded.value) return t('aiInteraction.feedbackEnded')
  if (selectedSkill.value) return t('aiInteraction.skillPlaceholder', { name: skillName(selectedSkill.value) })
  return t('aiInteraction.placeholder')
})
const canSend = computed(() => {
  if (composerDisabled.value) return false
  return !!draft.value.trim() || !!pendingImageBase64.value
})
const autoCaption = computed(() => {
  if (routeMode.value !== 'auto' || !selectedSkill.value) return ''
  return t('aiInteraction.autoSkill', { name: skillName(selectedSkill.value) })
})

function assistantCitations(turn: Turn): KbCitation[] {
  return turn.kind === 'assistant' ? (turn.citations ?? []) : []
}

function assistantVersion(turn: Turn) {
  return turn.kind === 'assistant' ? turn.versionId : null
}

function createLabel(kind: PasteCreateKind) {
  if (kind === 'customer') return t('aiInteraction.createCustomer')
  if (kind === 'vendor') return t('aiInteraction.createVendor')
  return t('aiInteraction.createRfq')
}

function shouldOfferCreate(text: string) {
  return selectedSkill.value == null && createActions.value.length > 0 && isPasteCreateSource(text)
}

function offerCreate(text: string) {
  turns.value = [
    ...turns.value,
    {
      kind: 'create',
      id: `create-${Date.now()}`,
      text
    }
  ]
  draft.value = ''
  void scrollToBottom()
}

async function startCreate(cardId: string, kind: PasteCreateKind, text: string) {
  if (createBusy.value) return
  createBusy.value = true
  createBusyId.value = cardId
  createBusyKind.value = kind
  errorText.value = ''
  if (kind === 'customer') {
    createEntityType.value = 'CUSTOMER'
    createRoute.value = { name: 'CustomerCreate' }
  } else if (kind === 'vendor') {
    createEntityType.value = 'VENDOR'
    createRoute.value = { name: 'VendorCreate' }
  } else {
    createEntityType.value = 'RFQ'
    createRoute.value = { name: 'RFQCreate' }
  }
  await nextTick()
  try {
    await scrollToBottom()
    await createHostRef.value?.openWithText(text)
  } finally {
    createBusy.value = false
    createBusyId.value = null
    createBusyKind.value = null
  }
}

function skillName(skill: AiInteractionSkill) {
  if (skill === 'feedback') return t('aiInteraction.skillFeedback')
  if (skill === 'ops') return t('aiInteraction.skillOps')
  if (skill === 'data') return t('aiInteraction.skillData')
  return t('aiInteraction.skillHandbook')
}

function readHref(versionId: string | null | undefined, anchor: string) {
  const version = versionId ? `?version=${encodeURIComponent(versionId)}` : ''
  return `/knowledge/handbook/read${version}#${anchor}`
}

function resetRound() {
  draft.value = ''
  errorText.value = ''
  turns.value = []
  selectedSkill.value = null
  routeMode.value = null
  preferredCategory.value = null
  sessionId.value = null
  sessionStatus.value = 'open'
  busy.value = false
  createBusy.value = false
  createBusyId.value = null
  createBusyKind.value = null
  clearPendingImage()
}

function beginRound() {
  resetRound()
  open.value = true
  void focusInput()
}

function close() {
  open.value = false
  resetRound()
}

function exitLikeEsc() {
  if (!open.value || hasForeignOverlay()) return
  close()
}

async function focusInput() {
  await nextTick()
  inputRef.value?.focus()
}

async function scrollToBottom() {
  await nextTick()
  const el = transcriptRef.value
  if (el) el.scrollTop = el.scrollHeight
}

function selectSkill(next: AiInteractionSkill) {
  if (busy.value) return
  if (selectedSkill.value === next) {
    selectedSkill.value = null
    routeMode.value = null
    return
  }
  if (next === 'handbook') clearPendingImage()
  const prev = selectedSkill.value
  const hadConversation = turns.value.some((turn) => turn.kind !== 'marker')
  selectedSkill.value = next
  routeMode.value = 'user'
  if (hadConversation && prev !== next) {
    turns.value = [
      ...turns.value,
      { kind: 'marker', id: `marker-${Date.now()}`, skill: next }
    ]
    void scrollToBottom()
  }
}

function onKeydown(event: KeyboardEvent) {
  if (open.value && event.key === 'Escape' && !event.isComposing) {
    if (hasForeignOverlay()) return
    event.preventDefault()
    event.stopPropagation()
    exitLikeEsc()
    return
  }
  if (!shouldStartAiRound(event)) return
  if (skills.value.length === 0) return
  event.preventDefault()
  event.stopPropagation()
  beginRound()
}

function onComposerKeydown(event: KeyboardEvent) {
  if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
    event.preventDefault()
    void onSend()
  }
}

async function onSend() {
  if (!canSend.value) return
  const text = draft.value.trim()
  const image = pendingImageBase64.value
    ? {
        imageBase64: pendingImageBase64.value,
        imageMimeType: pendingImageMime.value || undefined,
        imageFileName: pendingImageName.value || undefined
      }
    : null
  errorText.value = ''

  if (!image && shouldOfferCreate(text)) {
    offerCreate(text)
    return
  }

  let skill = selectedSkill.value
  let mode = routeMode.value
  if (!text && image) {
    if (!canFeedback.value) {
      errorText.value = t('aiInteraction.imageNeedsFeedback')
      return
    }
    skill = 'feedback'
    if (mode !== 'user') mode = 'auto'
  } else if (!skill) {
    skill = await resolveSkill(text)
    if (!skill) return
    mode = skills.value.length === 1 ? 'user' : 'auto'
  }

  if ((skill === 'handbook' || skill === 'ops' || skill === 'data') && !text) {
    errorText.value = skill === 'data' ? t('aiInteraction.dataNeedsText') : t('aiInteraction.handbookNeedsText')
    return
  }
  if ((skill === 'handbook' || skill === 'ops' || skill === 'data') && text.length > 500) {
    errorText.value = t('aiInteraction.questionTooLong')
    return
  }
  if (skill === 'feedback' && feedbackEnded.value) return

  const background = formatAiTranscript(turns.value, skillName)
  const userTurn: Turn = {
    kind: 'user',
    id: `user-${Date.now()}`,
    text: text || t('aiAssistant.hasImage'),
    skill
  }
  selectedSkill.value = skill
  routeMode.value = mode
  turns.value = [...turns.value, userTurn]
  draft.value = ''
  if (skill === 'feedback') clearPendingImage()
  busy.value = true
  await scrollToBottom()

  try {
    if (skill === 'feedback') await sendFeedback(text, image, background)
    else if (skill === 'data') await sendData(text)
    else await sendHandbook(text, background, skill)
  } catch (error) {
    turns.value = turns.value.filter((turn) => turn.id !== userTurn.id)
    draft.value = text
    errorText.value = getApiErrorMessage(
      error,
      skill === 'feedback' ? t('aiAssistant.sendFailed') : t('aiInteraction.askFailed')
    )
  } finally {
    busy.value = false
    await scrollToBottom()
    void focusInput()
  }
}

async function resolveSkill(text: string): Promise<AiInteractionSkill | null> {
  const allowed = skills.value
  if (allowed.length === 1) return allowed[0]
  try {
    const routed = await aiAssistantApi.routeSkill(text)
    const skill =
      routed.skill === 'handbook'
        ? 'handbook'
        : routed.skill === 'ops'
          ? 'ops'
          : routed.skill === 'data'
            ? 'data'
            : routed.skill === 'feedback'
              ? 'feedback'
              : null
    if (skill && allowed.includes(skill)) return skill
  } catch (error) {
    const status = (error as { httpStatus?: number }).httpStatus
    if (status && status !== 404) {
      errorText.value = getApiErrorMessage(error, t('aiInteraction.routeFailed'))
      return null
    }
  }
  return chooseAiSkill(text, allowed)
}

function latestDataState(): AiDataQueryState | null {
  for (let i = turns.value.length - 1; i >= 0; i -= 1) {
    const turn = turns.value[i]
    if (turn.kind === 'assistant' && turn.skill === 'data' && turn.dataState) return turn.dataState
  }
  return null
}

function isLatestDataTurn(turn: Turn) {
  for (let i = turns.value.length - 1; i >= 0; i -= 1) {
    const item = turns.value[i]
    if (item.kind === 'assistant' && item.skill === 'data') return item.id === turn.id
  }
  return false
}

function sendOption(label: string) {
  if (busy.value) return
  selectedSkill.value = 'data'
  routeMode.value = 'user'
  draft.value = label
  void onSend()
}

function dataRoute(name: string, key?: string | null, value?: string | null) {
  if (key && value) return { name, query: { [key]: value } }
  return { name }
}

function formatDataValue(value?: number | null) {
  if (value == null || Number.isNaN(value)) return '—'
  return value.toLocaleString('zh-CN', { maximumFractionDigits: 2 })
}

function barWidth(points: { value?: number | null }[], value?: number | null) {
  const max = Math.max(...points.map((point) => Number(point.value) || 0), 0)
  if (!max || value == null) return '0%'
  return `${Math.max(4, Math.round((Number(value) / max) * 100))}%`
}

function linePoints(points: { value?: number | null }[]) {
  if (points.length === 0) return ''
  const values = points.map((point) => Number(point.value) || 0)
  const max = Math.max(...values, 0)
  const min = Math.min(...values, 0)
  const span = max - min || 1
  return points
    .map((point, index) => {
      const x = points.length === 1 ? 140 : (index / (points.length - 1)) * 264 + 8
      const y = 88 - ((Number(point.value) || 0) - min) / span * 76
      return `${x},${y}`
    })
    .join(' ')
}

async function sendData(text: string) {
  const result = await aiAssistantApi.queryData({
    question: text,
    state: latestDataState()
  })
  turns.value = [
    ...turns.value,
    {
      kind: 'assistant',
      id: `data-${Date.now()}`,
      text: result.summary,
      skill: 'data',
      basisNote: result.basisNote,
      options: result.options,
      chart: result.chart,
      rows: result.rows,
      link: result.link,
      dataState: result.state
    }
  ]
}

async function sendFeedback(
  text: string,
  image: { imageBase64: string; imageMimeType?: string; imageFileName?: string } | null,
  background: string
) {
  if (!sessionId.value) {
    const params = route.params && Object.keys(route.params).length ? JSON.stringify(route.params) : undefined
    const query = route.query && Object.keys(route.query).length ? JSON.stringify(route.query) : undefined
    const created = await aiAssistantApi.createSession({
      pageUrl: window.location.href,
      routeName: typeof route.name === 'string' ? route.name : undefined,
      routeParamsJson: params,
      routeQueryJson: query,
      userAgent: navigator.userAgent,
      preferredCategory: preferredCategory.value
    })
    sessionId.value = created.sessionId
    sessionStatus.value = created.status
  }
  const res = await aiAssistantApi.sendMessage(sessionId.value, {
    text: text || undefined,
    imageBase64: image?.imageBase64,
    imageMimeType: image?.imageMimeType,
    imageFileName: image?.imageFileName,
    backgroundContext: background || undefined
  })
  sessionStatus.value = res.status
  turns.value = [
    ...turns.value,
    {
      kind: 'assistant',
      id: `assistant-${Date.now()}`,
      text: res.assistantMessage,
      skill: 'feedback'
    }
  ]
}

async function sendHandbook(text: string, background: string, skill: AiInteractionSkill) {
  const result = await knowledgeBaseApi.ask(
    text,
    background || undefined,
    skill === 'ops' ? 'ops.manual' : undefined
  )
  turns.value = [
    ...turns.value,
    {
      kind: 'assistant',
      id: `assistant-${Date.now()}`,
      text: result.answer,
      skill: skill === 'ops' ? 'ops' : 'handbook',
      citations: result.citations,
      versionId: result.versionId
    }
  ]
}

function onPaste(event: ClipboardEvent) {
  const text = event.clipboardData?.getData('text/plain') ?? ''
  if (shouldOfferCreate(text)) {
    event.preventDefault()
    offerCreate(text.trim())
    return
  }
  if (!allowImage.value) return
  const items = event.clipboardData?.items
  if (!items) return
  for (const item of items) {
    if (item.type.startsWith('image/')) {
      event.preventDefault()
      const file = item.getAsFile()
      if (file) readImageFile(file)
      break
    }
  }
}

function onFileChange(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (file) readImageFile(file)
}

function readImageFile(file: File) {
  if (!file.type.startsWith('image/')) {
    errorText.value = t('aiAssistant.imageOnly')
    return
  }
  if (file.size > 8 * 1024 * 1024) {
    errorText.value = t('aiAssistant.imageTooLarge')
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

watch(open, (value) => {
  document.body.style.overflow = value ? 'hidden' : ''
  document.body.classList.toggle('ai-interaction-open', value)
})

watch(
  () => route.fullPath,
  () => {
    if (open.value) close()
  }
)

onMounted(() => {
  window.addEventListener('keydown', onKeydown, true)
})

onBeforeUnmount(() => {
  window.removeEventListener('keydown', onKeydown, true)
  document.body.style.overflow = ''
  document.body.classList.remove('ai-interaction-open')
})
</script>

<style scoped lang="scss">
.ai-interaction-layer {
  position: fixed;
  inset: 0;
  z-index: 4000;
  background: rgba(15, 23, 42, 0.5);
  backdrop-filter: blur(6px);
  -webkit-backdrop-filter: blur(6px);
}

.ai-interaction-dim {
  position: absolute;
  inset: 0;
  background: transparent;
}

.ai-exit-anchor {
  position: absolute;
  top: 16px;
  right: 20px;
  z-index: 2;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.ai-exit {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  border: 0;
  border-radius: 999px;
  padding: 6px 12px;
  background: #2563eb;
  color: #fff;
  font: inherit;
  font-size: 13px;
  line-height: 1.2;
  white-space: nowrap;
  cursor: pointer;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.18);
}

.ai-exit-hint {
  margin: 6px 0 0;
  color: #d1d5db;
  font-size: 12px;
  line-height: 1.4;
  white-space: nowrap;
  text-align: center;
}

.ai-exit svg {
  width: 14px;
  height: 14px;
}

.ai-interaction-stage {
  position: relative;
  z-index: 1;
  height: 100%;
  display: flex;
  flex-direction: column;
  justify-content: center;
  box-sizing: border-box;
}

.ai-interaction-stage.is-chatting {
  justify-content: flex-start;
}

.ai-interaction-stage.is-chatting .ai-interaction-dock {
  margin-top: 20px;
}

.ai-interaction-stage:not(.is-chatting) .ai-interaction-dock {
  margin-bottom: 0;
}

.ai-interaction-transcript {
  flex: 1;
  min-height: 0;
  overflow: auto;
  padding: 28px 16px 12px;
}

.ai-interaction-stage:not(.is-chatting) .ai-interaction-transcript {
  display: none;
}

.turn {
  width: min(800px, 100%);
  margin: 0 auto 16px;
}

.marker {
  margin: 8px 0;
  text-align: center;
  color: #64748b;
  font-size: 13px;
}

.bubble {
  padding: 10px 14px;
  border-radius: 12px;
  white-space: pre-wrap;
}

.bubble--user {
  width: fit-content;
  max-width: 100%;
  margin-left: auto;
  background: #2563eb;
  color: #fff;
  font-size: 14px;
  line-height: 1.6;
}

.bubble--assistant {
  width: 100%;
  box-sizing: border-box;
  padding: 16px 18px;
  background: #fff;
  color: #1f2937;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.12);
  font-size: 14px;
  line-height: 1.75;
}

.assistant-mark {
  display: flex;
  margin-bottom: 10px;
}

.ai-face {
  width: 22px;
  height: 22px;
  border-radius: 50%;
  background: radial-gradient(circle at 50% 42%, #9fd0ff 0%, #5aa6f3 58%, #3b86e8 100%);
  position: relative;
  flex-shrink: 0;
}

.ai-face::before,
.ai-face::after {
  content: '';
  position: absolute;
  top: 6px;
  width: 2.5px;
  height: 6px;
  border-radius: 999px;
  background: #fff;
}

.ai-face::before {
  left: 7px;
}

.ai-face::after {
  right: 7px;
}

.bubble-text {
  margin: 0;
}

.reply-panel {
  margin-top: 14px;
  padding: 12px 14px;
  border-radius: 10px;
  background: #f3f5f8;
  border: 1px solid #e5eaf1;
}

.refs {
  margin: 0;
  padding-left: 18px;
  font-size: 14px;
  line-height: 1.6;
}

.refs a {
  color: #2563eb;
  text-decoration: none;
}

.refs a:hover {
  text-decoration: underline;
}

.data-chart,
.data-table,
.data-basis,
.data-link,
.data-options {
  margin-top: 12px;
}

.data-number {
  margin: 0;
  font-size: 28px;
  line-height: 1.2;
  font-weight: 650;
  color: #0f172a;
}

.data-number span,
.data-unit,
.data-basis {
  font-size: 13px;
  font-weight: 400;
  color: #64748b;
}

.data-line {
  width: 100%;
  height: 96px;
}

.data-line-labels {
  display: flex;
  justify-content: space-between;
  gap: 8px;
  font-size: 12px;
  color: #64748b;
}

.data-bars {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.data-bar-row {
  display: grid;
  grid-template-columns: 96px 1fr;
  gap: 8px;
  align-items: center;
  font-size: 13px;
}

.data-bar-label {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.data-bar-track {
  height: 8px;
  border-radius: 99px;
  background: #e8eef6;
}

.data-bar-fill {
  display: block;
  height: 100%;
  border-radius: 99px;
  background: #2563eb;
}

.data-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
}

.data-table td {
  padding: 4px 0;
  border-bottom: 1px solid #e5eaf1;
}

.data-table td:last-child {
  text-align: right;
  white-space: nowrap;
}

.data-table a,
.data-link {
  color: #2563eb;
  text-decoration: none;
}

.data-options {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.bubble--thinking {
  width: fit-content;
  display: flex;
  align-items: center;
  gap: 8px;
  color: #64748b;
  font-size: 14px;
  line-height: 1.4;
}

.bubble--thinking .assistant-mark {
  margin-bottom: 0;
}

.ai-interaction-dock {
  width: min(800px, calc(100% - 32px));
  margin: 0 auto 24px;
}

.composer {
  background: #fff;
  border-radius: 16px;
  box-shadow: 0 16px 40px rgba(15, 23, 42, 0.18);
  padding: 12px;
}

.category-row,
.composer-actions,
.skills {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.category-row {
  margin-bottom: 8px;
}

.category-row .chip {
  font-size: 14px;
}

textarea {
  width: 100%;
  border: 0;
  resize: none;
  outline: none;
  font: inherit;
  font-size: 14px;
  line-height: 1.5;
  box-sizing: border-box;
  background: transparent;
}

.composer-actions {
  justify-content: flex-end;
  align-items: center;
}

.chip,
.skill,
.file-btn,
.send {
  border: 1px solid #dbe1ea;
  background: #fff;
  border-radius: 999px;
  padding: 6px 12px;
  cursor: pointer;
  font: inherit;
}

.chip.active,
.skill.active {
  border-color: #2563eb;
  color: #2563eb;
  background: #eff6ff;
}

.send {
  background: #2563eb;
  border-color: #2563eb;
  color: #fff;
}

.send:disabled,
.chip:disabled,
.skill:disabled,
.file-btn.disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.skills {
  margin-top: 10px;
}

.skills .skill {
  font-size: 14px;
}

.auto-caption,
.error {
  margin: 8px 4px 0;
  font-size: 13px;
}

.auto-caption {
  color: #475569;
}

.error {
  color: #b91c1c;
}

.pending-image {
  position: relative;
  display: inline-block;
  margin-bottom: 8px;
}

.pending-image img {
  max-height: 72px;
  border-radius: 8px;
}

.pending-clear {
  position: absolute;
  top: -6px;
  right: -6px;
  width: 20px;
  height: 20px;
  border: 0;
  border-radius: 50%;
  background: #111827;
  color: #fff;
  cursor: pointer;
}

.create-card {
  width: 100%;
  box-sizing: border-box;
  padding: 16px 18px;
  border-radius: 12px;
  background: #fff;
  color: #1f2937;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.12);
  font-size: 14px;
  line-height: 1.75;
}

.create-title {
  margin: 0 0 8px;
  font-size: 13px;
  line-height: 1.4;
  color: #475569;
}

.create-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.create-actions .skill {
  font-size: 14px;
}

.skill.is-busy {
  border-color: #2563eb;
  color: #2563eb;
  background: #eff6ff;
}

.skill.is-busy {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.skill.is-busy:disabled {
  opacity: 1;
  cursor: wait;
}

.create-parsing {
  display: flex;
  align-items: center;
  gap: 8px;
  margin: 0;
  color: #2563eb;
  font-size: 13px;
  line-height: 1.4;
}

.create-spinner {
  width: 14px;
  height: 14px;
  border: 2px solid #bfdbfe;
  border-top-color: #2563eb;
  border-radius: 50%;
  animation: ai-create-spin 0.7s linear infinite;
  flex: none;
}

@keyframes ai-create-spin {
  to {
    transform: rotate(360deg);
  }
}
</style>

<style lang="scss">
body.ai-interaction-open .ai-parse-confirm-overlay {
  z-index: 4600 !important;
}

body.ai-interaction-open .el-message {
  z-index: 5000 !important;
}
</style>
