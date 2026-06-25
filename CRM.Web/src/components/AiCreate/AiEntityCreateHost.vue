<template>
  <AiTextParseDialog
    v-model:visible="textVisible"
    :loading="parsing"
    :placeholder="textPlaceholder"
    @generate="onGenerate"
  />
  <AiEntityParseConfirmDialog
    v-model:visible="confirmVisible"
    :entity-type="entityType"
    :customer-data="parsedCustomer"
    :rfq-data="parsedRfq"
    :vendor-data="parsedVendor"
    :customer-contact-data="parsedCustomerContact"
    :vendor-contact-data="parsedVendorContact"
    :customer-address-data="parsedCustomerAddress"
    :vendor-address-data="parsedVendorAddress"
    @confirm="onConfirm"
  />
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter, type RouteLocationRaw } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import AiTextParseDialog from './AiTextParseDialog.vue'
import AiEntityParseConfirmDialog from './AiEntityParseConfirmDialog.vue'
import {
  aiApi,
  AI_SCENARIO_ENTITY_PARSE_CUSTOMER,
  AI_SCENARIO_ENTITY_PARSE_CUSTOMER_ADDRESS,
  AI_SCENARIO_ENTITY_PARSE_CUSTOMER_CONTACT,
  AI_SCENARIO_ENTITY_PARSE_RFQ,
  AI_SCENARIO_ENTITY_PARSE_VENDOR,
  AI_SCENARIO_ENTITY_PARSE_VENDOR_ADDRESS,
  AI_SCENARIO_ENTITY_PARSE_VENDOR_CONTACT
} from '@/api/ai'
import { parseAiJsonObject } from '@/utils/aiJson'
import { getApiErrorMessage } from '@/utils/apiError'
import {
  customerAddressPrefillToFormPayload,
  customerContactPrefillToFormPayload,
  customerPrefillToFormPayload,
  emptyParsedCustomer,
  emptyParsedCustomerAddress,
  emptyParsedCustomerContact,
  emptyParsedRfq,
  emptyParsedVendor,
  emptyParsedVendorAddress,
  emptyParsedVendorContact,
  normalizeCustomerAddressParseResult,
  normalizeCustomerContactParseResult,
  normalizeCustomerParseResult,
  normalizeRfqParseResult,
  normalizeVendorAddressParseResult,
  normalizeVendorContactParseResult,
  normalizeVendorParseResult,
  rfqPrefillToFormPayload,
  vendorAddressPrefillToFormPayload,
  vendorContactPrefillToFormPayload,
  vendorPrefillToFormPayload,
  type ParsedCustomerAddressFields,
  type ParsedCustomerContactFields,
  type ParsedCustomerFields,
  type ParsedRfqFields,
  type ParsedVendorAddressFields,
  type ParsedVendorContactFields,
  type ParsedVendorFields
} from '@/utils/entityParseSchema'
import { setAiPrefill, type AiPrefillEntityType } from '@/utils/aiPrefill'

const props = defineProps<{
  entityType: AiPrefillEntityType
  targetRoute: RouteLocationRaw
  /** 详情页子实体（联系人/地址）时传父级 id，写入 parse log parent_biz_id */
  parentBizId?: string
}>()

const router = useRouter()
const { t } = useI18n()

const textVisible = ref(false)
const confirmVisible = ref(false)
const parsing = ref(false)
const entityParseLogId = ref<string | null>(null)
const parsedCustomer = ref<ParsedCustomerFields | null>(null)
const parsedRfq = ref<ParsedRfqFields | null>(null)
const parsedVendor = ref<ParsedVendorFields | null>(null)
const parsedCustomerContact = ref<ParsedCustomerContactFields | null>(null)
const parsedVendorContact = ref<ParsedVendorContactFields | null>(null)
const parsedCustomerAddress = ref<ParsedCustomerAddressFields | null>(null)
const parsedVendorAddress = ref<ParsedVendorAddressFields | null>(null)

const scenarioCode = computed(() => {
  const map: Record<AiPrefillEntityType, string> = {
    CUSTOMER: AI_SCENARIO_ENTITY_PARSE_CUSTOMER,
    RFQ: AI_SCENARIO_ENTITY_PARSE_RFQ,
    VENDOR: AI_SCENARIO_ENTITY_PARSE_VENDOR,
    CUSTOMER_CONTACT: AI_SCENARIO_ENTITY_PARSE_CUSTOMER_CONTACT,
    VENDOR_CONTACT: AI_SCENARIO_ENTITY_PARSE_VENDOR_CONTACT,
    CUSTOMER_ADDRESS: AI_SCENARIO_ENTITY_PARSE_CUSTOMER_ADDRESS,
    VENDOR_ADDRESS: AI_SCENARIO_ENTITY_PARSE_VENDOR_ADDRESS
  }
  return map[props.entityType]
})

const textPlaceholder = computed(() => {
  const keyMap: Record<AiPrefillEntityType, string> = {
    CUSTOMER: 'aiEntityCreate.textDialog.placeholderCustomer',
    RFQ: 'aiEntityCreate.textDialog.placeholderRfq',
    VENDOR: 'aiEntityCreate.textDialog.placeholderVendor',
    CUSTOMER_CONTACT: 'aiEntityCreate.textDialog.placeholderCustomerContact',
    VENDOR_CONTACT: 'aiEntityCreate.textDialog.placeholderVendorContact',
    CUSTOMER_ADDRESS: 'aiEntityCreate.textDialog.placeholderCustomerAddress',
    VENDOR_ADDRESS: 'aiEntityCreate.textDialog.placeholderVendorAddress'
  }
  return t(keyMap[props.entityType])
})

function open() {
  entityParseLogId.value = null
  textVisible.value = true
}

function resolveRawObject(result: Awaited<ReturnType<typeof aiApi.invoke>>) {
  if (result.entityParseLogId && result.data && typeof result.data === 'object' && !Array.isArray(result.data)) {
    return result.data as Record<string, unknown>
  }
  return parseAiJsonObject(result.data, result.content)
}

async function onGenerate(rawText: string) {
  parsing.value = true
  try {
    const result = await aiApi.invoke({
      scenarioCode: scenarioCode.value,
      input: { raw_text: rawText },
      bizType: props.entityType,
      bizId: props.parentBizId
    })
    entityParseLogId.value = result.entityParseLogId ?? null

    const obj = resolveRawObject(result)
    if (!obj) {
      ElMessage.error(t('aiEntityCreate.errors.parseFailed'))
      return
    }

    const useBackendNormalized = !!result.entityParseLogId

    if (props.entityType === 'CUSTOMER') {
      parsedCustomer.value = useBackendNormalized
        ? (obj as unknown as ParsedCustomerFields)
        : normalizeCustomerParseResult(obj)
      if (!parsedCustomer.value.customerName.trim()) {
        ElMessage.warning(t('aiEntityCreate.errors.noCustomerName'))
        return
      }
    } else if (props.entityType === 'RFQ') {
      parsedRfq.value = useBackendNormalized
        ? (obj as unknown as ParsedRfqFields)
        : normalizeRfqParseResult(obj)
    } else if (props.entityType === 'VENDOR') {
      parsedVendor.value = useBackendNormalized
        ? (obj as unknown as ParsedVendorFields)
        : normalizeVendorParseResult(obj)
      if (!parsedVendor.value.officialName.trim()) {
        ElMessage.warning(t('aiEntityCreate.errors.noOfficialName'))
        return
      }
    } else if (props.entityType === 'CUSTOMER_CONTACT') {
      parsedCustomerContact.value = useBackendNormalized
        ? (obj as unknown as ParsedCustomerContactFields)
        : normalizeCustomerContactParseResult(obj)
      if (!parsedCustomerContact.value.contactName.trim()) {
        ElMessage.warning(t('aiEntityCreate.errors.noContactName'))
        return
      }
    } else if (props.entityType === 'VENDOR_CONTACT') {
      parsedVendorContact.value = useBackendNormalized
        ? (obj as unknown as ParsedVendorContactFields)
        : normalizeVendorContactParseResult(obj)
      if (!parsedVendorContact.value.cName.trim() && !parsedVendorContact.value.eName.trim()) {
        ElMessage.warning(t('aiEntityCreate.errors.noContactName'))
        return
      }
    } else if (props.entityType === 'CUSTOMER_ADDRESS') {
      parsedCustomerAddress.value = useBackendNormalized
        ? (obj as unknown as ParsedCustomerAddressFields)
        : normalizeCustomerAddressParseResult(obj)
      if (!parsedCustomerAddress.value.streetAddress.trim()) {
        ElMessage.warning(t('aiEntityCreate.errors.noStreetAddress'))
        return
      }
    } else {
      parsedVendorAddress.value = useBackendNormalized
        ? (obj as unknown as ParsedVendorAddressFields)
        : normalizeVendorAddressParseResult(obj)
      if (!parsedVendorAddress.value.address.trim()) {
        ElMessage.warning(t('aiEntityCreate.errors.noStreetAddress'))
        return
      }
    }

    textVisible.value = false
    confirmVisible.value = true
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('aiEntityCreate.errors.invokeFailed')))
  } finally {
    parsing.value = false
  }
}

function onConfirm(
  payload:
    | ParsedCustomerFields
    | ParsedRfqFields
    | ParsedVendorFields
    | ParsedCustomerContactFields
    | ParsedVendorContactFields
    | ParsedCustomerAddressFields
    | ParsedVendorAddressFields
) {
  const logId = entityParseLogId.value
  if (logId) {
    void aiApi.confirmEntityParseLog(logId, payload as unknown as Record<string, unknown>).catch(() => {
      /* 确认日志失败不阻断建单预填 */
    })
  }

  let formPayload: Record<string, unknown>
  if (props.entityType === 'CUSTOMER') {
    formPayload = customerPrefillToFormPayload(payload as ParsedCustomerFields)
  } else if (props.entityType === 'RFQ') {
    formPayload = rfqPrefillToFormPayload(payload as ParsedRfqFields)
  } else if (props.entityType === 'VENDOR') {
    formPayload = vendorPrefillToFormPayload(payload as ParsedVendorFields)
  } else if (props.entityType === 'CUSTOMER_CONTACT') {
    formPayload = customerContactPrefillToFormPayload(payload as ParsedCustomerContactFields)
  } else if (props.entityType === 'VENDOR_CONTACT') {
    formPayload = vendorContactPrefillToFormPayload(payload as ParsedVendorContactFields)
  } else if (props.entityType === 'CUSTOMER_ADDRESS') {
    formPayload = customerAddressPrefillToFormPayload(payload as ParsedCustomerAddressFields)
  } else {
    formPayload = vendorAddressPrefillToFormPayload(payload as ParsedVendorAddressFields)
  }

  const token = setAiPrefill(props.entityType, formPayload, entityParseLogId.value)
  confirmVisible.value = false
  entityParseLogId.value = null
  parsedCustomer.value = emptyParsedCustomer()
  parsedRfq.value = emptyParsedRfq()
  parsedVendor.value = emptyParsedVendor()
  parsedCustomerContact.value = emptyParsedCustomerContact()
  parsedVendorContact.value = emptyParsedVendorContact()
  parsedCustomerAddress.value = emptyParsedCustomerAddress()
  parsedVendorAddress.value = emptyParsedVendorAddress()

  const tr = props.targetRoute
  if (typeof tr === 'string') {
    router.push({ path: tr, query: { aiPrefill: token } })
    return
  }
  router.push({
    ...tr,
    query: { ...(tr.query as Record<string, string> | undefined), aiPrefill: token }
  })
}

defineExpose({ open })
</script>
