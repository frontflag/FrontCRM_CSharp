<template>
  <section
    class="customer-intel-panel"
    :class="{ 'customer-intel-panel--embedded': layout === 'embedded' }"
  >
    <div class="customer-intel-panel__head">
      <div class="customer-intel-panel__head-main">
        <h2 class="customer-intel-panel__title">{{ ti('title') }}</h2>
        <div class="customer-intel-panel__tags">
          <el-tag v-if="fromCache" size="small" type="info">{{ ti('fromCache') }}</el-tag>
          <el-tag v-else-if="hasReport" size="small" type="success">{{ ti('live') }}</el-tag>
        </div>
      </div>
      <div class="customer-intel-panel__head-actions">
        <el-tooltip
          v-if="sections.length"
          :content="allSectionsCollapsed ? ti('expandAllSections') : ti('collapseAllSections')"
          placement="top"
          :show-after="200"
        >
          <el-button
            size="small"
            text
            type="primary"
            class="customer-intel-panel__sections-toggle-all"
            :aria-label="allSectionsCollapsed ? ti('expandAllSections') : ti('collapseAllSections')"
            @click="toggleAllSections"
          >
            <el-icon>
              <ArrowDown v-if="allSectionsCollapsed" />
              <ArrowUp v-else />
            </el-icon>
          </el-button>
        </el-tooltip>
        <el-button v-if="showClose" size="small" text @click="emit('close')">{{ ti('close') }}</el-button>
      </div>
    </div>

    <el-alert
      v-if="hasReport"
      class="customer-intel-panel__disclaimer"
      type="warning"
      :closable="false"
      show-icon
      :title="ti('disclaimerShort')"
    />

    <div v-if="sections.length" class="customer-intel-panel__sections-wrap">
      <div class="customer-intel-panel__sections">
      <article
        v-for="(section, idx) in sections"
        :key="String(section.id ?? idx)"
        class="customer-intel-panel__section-card"
        :class="{ 'customer-intel-panel__section-card--collapsed': isSectionCollapsed(section, idx) }"
      >
        <header
          class="customer-intel-panel__section-head"
          role="button"
          tabindex="0"
          @click="toggleSection(section, idx)"
          @keydown.enter.prevent="toggleSection(section, idx)"
          @keydown.space.prevent="toggleSection(section, idx)"
        >
          <div class="customer-intel-panel__section-head-main">
            <h3 class="customer-intel-panel__section-title">{{ sectionTitle(section) }}</h3>
            <el-tooltip
              v-if="sectionConfidence(section)"
              :content="confidenceTooltip(section)"
              placement="top"
              :show-after="200"
            >
              <el-tag
                size="small"
                :type="confidenceTagType(section)"
                class="customer-intel-panel__conf-tag"
                @click.stop
              >
                {{ sectionConfidenceLabel(section) }}
              </el-tag>
            </el-tooltip>
          </div>
          <el-tooltip
            :content="isSectionCollapsed(section, idx) ? ti('expandSection') : ti('collapseSection')"
            placement="top"
            :show-after="200"
          >
            <el-button
              size="small"
              text
              type="primary"
              class="customer-intel-panel__section-toggle"
              :aria-label="isSectionCollapsed(section, idx) ? ti('expandSection') : ti('collapseSection')"
              @click.stop="toggleSection(section, idx)"
            >
              <el-icon>
                <ArrowDown v-if="isSectionCollapsed(section, idx)" />
                <ArrowUp v-else />
              </el-icon>
            </el-button>
          </el-tooltip>
        </header>

        <div v-show="!isSectionCollapsed(section, idx)" class="customer-intel-panel__section-body">
          <p v-if="sectionSummary(section)" class="customer-intel-panel__summary">{{ sectionSummary(section) }}</p>
          <CustomerIntelSectionContent :content="sectionContent(section)" />
        </div>
      </article>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ArrowDown, ArrowUp } from '@element-plus/icons-vue'
import CustomerIntelSectionContent from '@/components/Customer/CustomerIntelSectionContent.vue'
import {
  CUSTOMER_INTEL_SECTION_LABELS,
  resolveCustomerIntelConfidence,
  type CustomerIntelSectionId,
  extractCustomerIntelSections
} from '@/utils/customerIntelSchema'

const props = withDefaults(
  defineProps<{
    data: Record<string, unknown> | null | undefined
    fromCache?: boolean
    layout?: 'centered' | 'embedded'
    showClose?: boolean
    i18nKeyPrefix?: string
  }>(),
  { layout: 'embedded', showClose: false, i18nKeyPrefix: 'customerIntel' }
)

const emit = defineEmits<{ close: [] }>()

const { t } = useI18n()

function ti(key: string): string {
  return t(`${props.i18nKeyPrefix}.${key}`)
}

const sections = computed(() => extractCustomerIntelSections(props.data))
const hasReport = computed(() => sections.value.length > 0)

const collapsedSectionIds = ref(new Set<string>())

const allSectionsCollapsed = computed(() => {
  const list = sections.value
  if (!list.length) return false
  return list.every((section, idx) => collapsedSectionIds.value.has(sectionKey(section, idx)))
})

watch(
  () => props.data,
  () => {
    collapsedSectionIds.value = new Set()
  }
)

function sectionKey(section: Record<string, unknown>, idx: number): string {
  return String(section.id ?? `section-${idx}`)
}

function isSectionCollapsed(section: Record<string, unknown>, idx: number): boolean {
  return collapsedSectionIds.value.has(sectionKey(section, idx))
}

function toggleSection(section: Record<string, unknown>, idx: number): void {
  const key = sectionKey(section, idx)
  const next = new Set(collapsedSectionIds.value)
  if (next.has(key)) next.delete(key)
  else next.add(key)
  collapsedSectionIds.value = next
}

function toggleAllSections(): void {
  if (allSectionsCollapsed.value) {
    collapsedSectionIds.value = new Set()
    return
  }
  collapsedSectionIds.value = new Set(
    sections.value.map((section, idx) => sectionKey(section, idx))
  )
}

function sectionTitle(section: Record<string, unknown>): string {
  const id = String(section.id ?? '')
  const mapped = CUSTOMER_INTEL_SECTION_LABELS[id as CustomerIntelSectionId]
  return mapped || String(section.title ?? (id || '章节'))
}

function sectionSummary(section: Record<string, unknown>): string {
  return String(section.summary ?? '').trim()
}

function sectionConfidence(section: Record<string, unknown>): string {
  return String(section.confidence ?? '').trim()
}

function sectionConfidenceLabel(section: Record<string, unknown>): string {
  const raw = sectionConfidence(section)
  if (!raw) return ''
  return resolveCustomerIntelConfidence(raw)
}

function confidenceTagType(section: Record<string, unknown>): 'success' | 'warning' | 'info' {
  const raw = sectionConfidence(section).toLowerCase()
  if (raw === 'high' || raw === 'medium-high') return 'success'
  if (raw === 'medium') return 'warning'
  return 'info'
}

function confidenceTooltip(section: Record<string, unknown>): string {
  const raw = sectionConfidence(section).toLowerCase()
  if (raw === 'high') return ti('confidenceTipHigh')
  if (raw === 'medium-high') return ti('confidenceTipMediumHigh')
  if (raw === 'medium') return ti('confidenceTipMedium')
  return ti('confidenceTipLow')
}

function sectionContent(section: Record<string, unknown>): Record<string, unknown> {
  const content = section.content
  if (content && typeof content === 'object' && !Array.isArray(content)) {
    return content as Record<string, unknown>
  }
  return {}
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.customer-intel-panel {
  margin: 0 auto 32px;
  max-width: calc(100% * 2 / 3);
  padding: 20px 22px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: 14px;
  box-shadow: $shadow-md;

  &--embedded {
    margin: 0;
    max-width: none;
    padding: 0;
    background: transparent;
    border: none;
    box-shadow: none;
  }

  &__head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
    margin-bottom: 10px;
  }

  &__head-main {
    display: flex;
    align-items: center;
    gap: 6px;
    min-width: 0;
  }

  &__head-actions {
    display: flex;
    align-items: center;
    gap: 2px;
    flex-shrink: 0;
  }

  &__title {
    margin: 0;
    font-size: 14px;
    font-weight: 600;
    color: $text-primary;
    flex-shrink: 0;
  }

  &:not(&--embedded) &__title {
    font-size: 16px;
  }

  &__tags {
    display: flex;
    gap: 6px;
    flex-shrink: 0;
  }

  &__disclaimer {
    margin-bottom: 10px;
  }

  &__sections-wrap {
    display: flex;
    flex-direction: column;
  }

  &__sections {
    display: flex;
    flex-direction: column;
    gap: 12px;
  }

  &__section-card {
    padding: 12px 14px;
    background: $layer-1;
    border: 1px solid $border-panel;
    border-radius: 10px;

    &--collapsed {
      padding-bottom: 12px;
    }
  }

  &__section-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
    margin-bottom: 0;
    cursor: pointer;
    user-select: none;
  }

  &__section-head-main {
    display: flex;
    align-items: center;
    gap: 6px;
    min-width: 0;
  }

  &__sections-toggle-all,
  &__section-toggle {
    flex-shrink: 0;
    padding: 0 4px;

    .el-icon {
      font-size: 14px;
    }
  }

  &__section-body {
    margin-top: 8px;
  }

  &__section-title {
    margin: 0;
    font-size: 14px;
    font-weight: 600;
    color: $text-primary;
  }

  &__conf-tag {
    flex-shrink: 0;
    cursor: help;
  }

  &__summary {
    margin: 0;
    font-size: 13px;
    color: $text-secondary;
    line-height: 1.6;
  }

  &__summary + :deep(.ci-section-content) {
    margin-top: 1.6em;
  }
}
</style>
