<template>
  <div class="customs-extend-cell" :class="{ 'is-expanded': expanded }">
    <template v-if="expanded">
      <div
        class="customs-extend-cell__cols"
        :style="{ gridTemplateColumns: subColGridTemplateColumns }"
      >
        <span
          v-for="f in fieldKeys"
          :key="f"
          class="customs-extend-cell__col"
          :class="{ 'customs-extend-cell__col--icon': f === 'icon' }"
          :title="fieldTitle(f)"
        >
          <template v-if="f === 'icon'">
            <CustomsDeclarationIconLink
              v-if="declarationId"
              :declaration-id="declarationId"
              :declaration-code="declarationCode"
            />
            <span v-else>{{ emptyDisplay }}</span>
          </template>
          <a
            v-else-if="f === 'declarationCode' && declarationHref"
            class="customs-extend-code-link"
            :href="declarationHref"
            target="_blank"
            rel="noopener noreferrer"
            @click.stop
          >
            {{ declarationCode }}
          </a>
          <span v-else>{{ displayValue(f) }}</span>
        </span>
      </div>
    </template>
    <template v-else>
      <span
        class="customs-extend-cell__value customs-extend-cell__value--single"
        :class="{ 'customs-extend-cell__col--icon': activeField === 'icon' }"
        :title="fieldTitle(activeField)"
      >
        <template v-if="activeField === 'icon'">
          <CustomsDeclarationIconLink
            v-if="declarationId"
            :declaration-id="declarationId"
            :declaration-code="declarationCode"
          />
          <span v-else>{{ emptyDisplay }}</span>
        </template>
        <a
          v-else-if="activeField === 'declarationCode' && declarationHref"
          class="customs-extend-code-link"
          :href="declarationHref"
          target="_blank"
          rel="noopener noreferrer"
          @click.stop
        >
          {{ declarationCode }}
        </a>
        <span v-else>{{ displayValue(activeField) }}</span>
      </span>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import CustomsDeclarationIconLink from '@/components/Customs/CustomsDeclarationIconLink.vue'
import { STOCK_OUT_NOTIFY_CUSTOMS_STATUS } from '@/constants/stockOutNotifyCustomsStatus'
import {
  CUSTOMS_EXTEND_FIELD_KEYS,
  pickCustomsBrokerName,
  pickCustomsDeclarationCode,
  pickCustomsDeclarationId,
  pickCustomsExtendStatusCode,
  pickCustomsExtendStatusKind,
  type CustomsExtendFieldKey,
  type CustomsExtendRowSlice
} from '@/constants/listCustomsExtendColumnSpec'
import { useCustomsExtendColumn } from '@/composables/useCustomsExtendColumn'

const props = defineProps<{
  row: CustomsExtendRowSlice
  activeField: CustomsExtendFieldKey
  emptyText?: string
}>()

const router = useRouter()
const { t } = useI18n()
const fieldKeys = CUSTOMS_EXTEND_FIELD_KEYS
const { expanded, subColGridTemplateColumns } = useCustomsExtendColumn()

const emptyDisplay = computed(() => props.emptyText ?? '—')
const declarationId = computed(() => pickCustomsDeclarationId(props.row))
const declarationCode = computed(() => pickCustomsDeclarationCode(props.row))
const declarationHref = computed(() => {
  if (!declarationId.value || !declarationCode.value) return ''
  return router.resolve({ name: 'CustomsDeclarationDetail', params: { id: declarationId.value } }).href
})

function statusDisplay(): string {
  const kind = pickCustomsExtendStatusKind(props.row)
  const code = pickCustomsExtendStatusCode(props.row)
  if (kind == null || code == null) return ''
  if (kind === 'notify') {
    if (code === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.PendingCustoms) {
      return t('stockOutNotifyList.customsStatus.pendingCustoms')
    }
    if (code === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.InCustoms) {
      return t('stockOutNotifyList.customsStatus.inCustoms')
    }
    if (code === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.Completed) {
      return t('stockOutNotifyList.customsStatus.completed')
    }
    return ''
  }
  if (code === 0) return t('customsPages.declarations.clearanceNone')
  if (code === 10) return t('customsPages.declarations.clearanceReleased')
  if (code === 100) return t('customsPages.declarations.clearanceCleared')
  return ''
}

function displayValue(field: CustomsExtendFieldKey): string {
  if (field === 'icon') {
    return declarationId.value ? '' : emptyDisplay.value
  }
  if (field === 'status') {
    return statusDisplay() || emptyDisplay.value
  }
  if (field === 'declarationCode') {
    return declarationCode.value || emptyDisplay.value
  }
  return pickCustomsBrokerName(props.row) || emptyDisplay.value
}

function fieldTitle(field: CustomsExtendFieldKey): string {
  if (field === 'icon') {
    return declarationCode.value || declarationId.value || emptyDisplay.value
  }
  if (field === 'declarationCode') {
    return declarationCode.value || emptyDisplay.value
  }
  if (field === 'status') {
    return statusDisplay() || emptyDisplay.value
  }
  return pickCustomsBrokerName(props.row) || emptyDisplay.value
}
</script>
