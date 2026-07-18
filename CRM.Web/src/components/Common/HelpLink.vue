<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { getExternalHelpUrlById } from '@/utils/externalHelpUrl'

interface Props {
  pageId: string
  label?: string
}

const props = withDefaults(defineProps<Props>(), {
  label: ''
})

const { locale: i18nLocale } = useI18n()
const href = computed(() => getExternalHelpUrlById(props.pageId, i18nLocale.value))
const title = computed(() => props.label || '查看帮助')
</script>

<template>
  <a
    :href="href"
    target="_blank"
    rel="noopener noreferrer"
    class="help-link"
    :title="title"
    :aria-label="title"
  >
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
      <circle cx="12" cy="12" r="10"/>
      <path d="M9.09 9a3 3 0 015.83 1c0 2-3 3-3 3"/>
      <line x1="12" y1="17" x2="12.01" y2="17"/>
    </svg>
  </a>
</template>

<style scoped lang="scss">
.help-link {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 18px;
  height: 18px;
  color: var(--el-text-color-secondary);
  text-decoration: none;
  transition: color 0.2s;

  &:hover {
    color: var(--el-color-primary);
  }

  svg {
    width: 100%;
    height: 100%;
  }
}
</style>
