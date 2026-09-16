<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import customsDeclarationIcon from '@/assets/images/customs-declaration-icon.png'

const props = withDefaults(
  defineProps<{
    declarationId?: string | null
    declarationCode?: string | null
    brokerName?: string | null
    /** 无报关单 ID 时仍显示灰色图标（销售明细流程卡） */
    showPending?: boolean
    /** 有报关单时新开页进详情；默认当前页 router.push */
    openInNewTab?: boolean
  }>(),
  {
    showPending: false,
    openInNewTab: false
  }
)

const router = useRouter()
const { t } = useI18n()

const declarationIdTrimmed = computed(() => (props.declarationId || '').trim())
const declarationCodeTrimmed = computed(() => (props.declarationCode || '').trim())
const brokerNameTrimmed = computed(() => (props.brokerName || '').trim())
const hasDeclaration = computed(() => !!declarationIdTrimmed.value)
const isPending = computed(() => !hasDeclaration.value)
const visible = computed(() => hasDeclaration.value || props.showPending)

const declarationHref = computed(() => {
  if (!declarationIdTrimmed.value) return ''
  return router.resolve({
    name: 'CustomsDeclarationDetail',
    params: { id: declarationIdTrimmed.value }
  }).href
})

const pendingTooltip = computed(() => t('common.customsExtendCol.pendingDeclaration'))

const ariaLabel = computed(() => {
  if (isPending.value) return pendingTooltip.value
  const code = declarationCodeTrimmed.value || '—'
  const broker = brokerNameTrimmed.value || '—'
  if (props.showPending || props.brokerName != null) {
    return `${t('common.customsExtendCol.fields.declarationCode')}：${code}；${t('common.customsExtendCol.fields.broker')}：${broker}`
  }
  if (declarationCodeTrimmed.value) {
    return t('stockInList.customsDeclarationIconTooltip', { code: declarationCodeTrimmed.value })
  }
  return t('stockInList.customsDeclarationIconTooltipLabel')
})

const useRichTooltip = computed(() => props.showPending || props.brokerName !== undefined)

function navigateSameTab() {
  if (!declarationIdTrimmed.value) return
  router.push({ name: 'CustomsDeclarationDetail', params: { id: declarationIdTrimmed.value } })
}
</script>

<template>
  <el-tooltip v-if="visible" placement="top" :hide-after="0">
    <template #content>
      <div v-if="isPending">{{ pendingTooltip }}</div>
      <div v-else-if="useRichTooltip" class="customs-declaration-icon-tooltip">
        <div>
          {{ t('common.customsExtendCol.fields.declarationCode') }}：{{ declarationCodeTrimmed || '—' }}
        </div>
        <div>
          {{ t('common.customsExtendCol.fields.broker') }}：{{ brokerNameTrimmed || '—' }}
        </div>
      </div>
      <div v-else>{{ ariaLabel }}</div>
    </template>
    <span v-if="isPending" class="customs-declaration-icon-btn is-pending" :aria-label="ariaLabel">
      <img :src="customsDeclarationIcon" alt="" class="customs-declaration-icon" />
    </span>
    <a
      v-else-if="openInNewTab && declarationHref"
      class="customs-declaration-icon-btn"
      :href="declarationHref"
      target="_blank"
      rel="noopener noreferrer"
      :aria-label="ariaLabel"
      @click.stop
    >
      <img :src="customsDeclarationIcon" alt="" class="customs-declaration-icon" />
    </a>
    <button
      v-else
      type="button"
      class="customs-declaration-icon-btn"
      :aria-label="ariaLabel"
      @click.stop="navigateSameTab"
    >
      <img :src="customsDeclarationIcon" alt="" class="customs-declaration-icon" />
    </button>
  </el-tooltip>
</template>

<style scoped lang="scss">
.customs-declaration-icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  padding: 0;
  margin: 0;
  border: none;
  background: transparent;
  cursor: pointer;
  vertical-align: middle;
  line-height: 0;
  text-decoration: none;

  &:hover .customs-declaration-icon {
    opacity: 0.85;
    transform: scale(1.05);
  }

  &.is-pending {
    cursor: default;

    .customs-declaration-icon {
      filter: grayscale(1);
      opacity: 0.42;
    }

    &:hover .customs-declaration-icon {
      opacity: 0.42;
      transform: none;
    }
  }
}

.customs-declaration-icon {
  width: 18px;
  height: 18px;
  object-fit: contain;
  transition: opacity 0.15s ease, transform 0.15s ease, filter 0.15s ease;
}

.customs-declaration-icon-tooltip {
  line-height: 1.5;
}
</style>
