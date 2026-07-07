<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import customsDeclarationIcon from '@/assets/images/customs-declaration-icon.png'

const props = defineProps<{
  declarationId?: string | null
  declarationCode?: string | null
}>()

const router = useRouter()
const { t } = useI18n()

const declarationIdTrimmed = computed(() => (props.declarationId || '').trim())
const declarationCodeTrimmed = computed(() => (props.declarationCode || '').trim())
const visible = computed(() => !!declarationIdTrimmed.value)

const tooltipContent = computed(() => {
  if (declarationCodeTrimmed.value) {
    return t('stockInList.customsDeclarationIconTooltip', { code: declarationCodeTrimmed.value })
  }
  return t('stockInList.customsDeclarationIconTooltipLabel')
})

function navigateToDetail() {
  if (!declarationIdTrimmed.value) return
  router.push({ name: 'CustomsDeclarationDetail', params: { id: declarationIdTrimmed.value } })
}
</script>

<template>
  <el-tooltip v-if="visible" :content="tooltipContent" placement="top" :hide-after="0">
    <button
      type="button"
      class="customs-declaration-icon-btn"
      :aria-label="tooltipContent"
      @click.stop="navigateToDetail"
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

  &:hover .customs-declaration-icon {
    opacity: 0.85;
    transform: scale(1.05);
  }
}

.customs-declaration-icon {
  width: 18px;
  height: 18px;
  object-fit: contain;
  transition: opacity 0.15s ease, transform 0.15s ease;
}
</style>
