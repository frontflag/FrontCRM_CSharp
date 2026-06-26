<template>
  <AiBusinessCardUploadDialog
    v-model:visible="uploadVisible"
    :loading="parsing"
    @parse="onParse"
  />
  <AiBusinessCardConfirmDialog
    v-model:visible="confirmVisible"
    :mode="mode"
    :preview-urls="previewUrls"
    :customer-data="parsedCustomer"
    :vendor-data="parsedVendor"
    @confirm="onConfirm"
  />
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, type RouteLocationRaw } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import AiBusinessCardUploadDialog, { type BusinessCardUploadPayload } from './AiBusinessCardUploadDialog.vue'
import AiBusinessCardConfirmDialog from './AiBusinessCardConfirmDialog.vue'
import {
  aiApi,
  AI_SCENARIO_ENTITY_PARSE_CUSTOMER_BUSINESS_CARD,
  AI_SCENARIO_ENTITY_PARSE_VENDOR_BUSINESS_CARD
} from '@/api/ai'
import { getApiErrorMessage } from '@/utils/apiError'
import { compressBusinessCardImage } from '@/utils/compressBusinessCardImage'
import { storeBusinessCardFiles } from '@/utils/businessCardFileStore'
import { setAiPrefill } from '@/utils/aiPrefill'
import {
  customerBusinessCardPrefillToFormPayload,
  hydrateCustomerBusinessCardBundle,
  normalizeVendorBusinessCardParseResult,
  type ParsedCustomerBusinessCardFields,
  type ParsedVendorBusinessCardFields,
  vendorBusinessCardPrefillToFormPayload
} from '@/utils/entityParseSchema'

const props = defineProps<{
  mode: 'customer' | 'vendor'
  targetRoute: RouteLocationRaw
}>()

const router = useRouter()
const { t } = useI18n()

const uploadVisible = ref(false)
const confirmVisible = ref(false)
const parsing = ref(false)
const previewUrls = ref<string[]>([])
const cardFiles = ref<File[]>([])
const entityParseLogId = ref<string | null>(null)
const parsedCustomer = ref<ParsedCustomerBusinessCardFields | null>(null)
const parsedVendor = ref<ParsedVendorBusinessCardFields | null>(null)

function newContactKey() {
  return typeof crypto !== 'undefined' && crypto.randomUUID
    ? crypto.randomUUID()
    : `bc-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`
}

function open() {
  entityParseLogId.value = null
  parsedCustomer.value = null
  parsedVendor.value = null
  cardFiles.value = []
  revokePreviewUrls()
  uploadVisible.value = true
}

function revokePreviewUrls() {
  previewUrls.value.forEach((url) => URL.revokeObjectURL(url))
  previewUrls.value = []
}

async function onParse(payload: BusinessCardUploadPayload) {
  parsing.value = true
  try {
    const front = await compressBusinessCardImage(payload.front)
    const back = payload.back ? await compressBusinessCardImage(payload.back) : null
    cardFiles.value = back ? [front, back] : [front]
    revokePreviewUrls()
    previewUrls.value = cardFiles.value.map((f) => URL.createObjectURL(f))

    const scenarioCode =
      props.mode === 'customer'
        ? AI_SCENARIO_ENTITY_PARSE_CUSTOMER_BUSINESS_CARD
        : AI_SCENARIO_ENTITY_PARSE_VENDOR_BUSINESS_CARD
    const bizType = props.mode === 'customer' ? 'CUSTOMER_BUSINESS_CARD' : 'VENDOR_BUSINESS_CARD'

    const result = await aiApi.invokeBusinessCard({
      scenarioCode,
      front,
      back: back ?? undefined,
      bizType
    })
    entityParseLogId.value = result.entityParseLogId ?? null

    const data = result.data
    if (!data || typeof data !== 'object' || Array.isArray(data)) {
      ElMessage.error(t('aiEntityCreate.errors.parseFailed'))
      return
    }

    if (props.mode === 'customer') {
      parsedCustomer.value = hydrateCustomerBusinessCardBundle(data as Record<string, unknown>)
      if (!parsedCustomer.value.customer.customerName.trim()) {
        ElMessage.warning(t('aiEntityCreate.errors.noCustomerName'))
        return
      }
    } else {
      parsedVendor.value = result.entityParseLogId
        ? (data as unknown as ParsedVendorBusinessCardFields)
        : normalizeVendorBusinessCardParseResult(data as Record<string, unknown>)
      if (!parsedVendor.value.vendor.officialName.trim()) {
        ElMessage.warning(t('aiEntityCreate.errors.noOfficialName'))
        return
      }
    }

    uploadVisible.value = false
    confirmVisible.value = true
  } catch (err) {
    ElMessage.error(getApiErrorMessage(err, t('aiBusinessCard.errors.parseFailed')))
  } finally {
    parsing.value = false
  }
}

async function onConfirm(confirmed: Record<string, unknown>) {
  if (entityParseLogId.value) {
    try {
      await aiApi.confirmEntityParseLog(entityParseLogId.value, confirmed)
    } catch {
      // 确认日志失败不阻断预填
    }
  }

  const contactKey = newContactKey()
  let payload: Record<string, unknown>
  const entityType = props.mode === 'customer' ? 'CUSTOMER' : 'VENDOR'

  if (props.mode === 'customer') {
    const bundle = confirmed as unknown as ParsedCustomerBusinessCardFields
    parsedCustomer.value = bundle
    payload = customerBusinessCardPrefillToFormPayload(bundle, contactKey)
  } else {
    const bundle = confirmed as unknown as ParsedVendorBusinessCardFields
    parsedVendor.value = bundle
    payload = vendorBusinessCardPrefillToFormPayload(bundle, contactKey)
  }

  const token = setAiPrefill(entityType, payload, entityParseLogId.value)
  if (cardFiles.value.length) {
    storeBusinessCardFiles(token, cardFiles.value)
  }

  confirmVisible.value = false
  await router.push({
    ...(typeof props.targetRoute === 'string' ? { path: props.targetRoute } : props.targetRoute),
    query: { aiPrefill: token }
  })
}

defineExpose({ open })
</script>
