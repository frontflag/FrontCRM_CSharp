<script setup lang="ts">
import { reactive, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'

const { t } = useI18n()

const route = useRoute()
const router = useRouter()

const form = reactive({
  keyword: ''
})

function syncFromRoute() {
  if (route.name !== 'StockOutList') return
  const q = route.query
  form.keyword = typeof q.keyword === 'string' ? q.keyword : ''
}

watch(
  () => [route.name, route.query] as const,
  () => syncFromRoute(),
  { deep: true, immediate: true }
)

function handleReset() {
  router.push({ name: 'StockOutList', query: {} })
}

function handleSearch() {
  const k = form.keyword.trim()
  router.push({ name: 'StockOutList', query: k ? { keyword: k } : {} })
}
</script>

<template>
  <div class="so-search-panel">
    <div class="so-search-panel__head">{{ t('stockOutList.leftPanel.title') }}</div>

    <div class="so-search-panel__fields">
      <div class="field-col">
        <label class="field-label">{{ t('stockOutList.leftPanel.keyword') }}</label>
        <div class="field-control">
          <input
            v-model="form.keyword"
            type="text"
            class="field-input"
            :placeholder="t('stockOutList.leftPanel.keywordPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
      </div>
    </div>

    <div class="so-search-panel__actions">
      <button type="button" class="btn-search" @click="handleSearch">{{ t('stockOutList.leftPanel.search') }}</button>
      <button type="button" class="btn-reset" @click="handleReset">{{ t('stockOutList.leftPanel.reset') }}</button>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.so-search-panel {
  min-height: 80px;
  font-size: 12px;
  color: $text-secondary;
}

.so-search-panel__head {
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 12px;
  font-size: 13px;
}

.so-search-panel__fields {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.field-col {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.field-label {
  font-size: 11px;
  font-weight: 500;
  color: $text-muted;
}

.field-control {
  width: 100%;
}

.field-input {
  width: 100%;
  box-sizing: border-box;
  padding: 7px 10px;
  font-size: 12px;
  color: $text-primary;
  background: $layer-3;
  border: 1px solid $border-panel;
  border-radius: 6px;
  outline: none;

  &::placeholder {
    color: $text-placeholder;
  }

  &:focus {
    border-color: var(--crm-accent-06);
  }
}

.so-search-panel__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 16px;
  padding-top: 12px;
  border-top: 1px solid $border-panel;
}

.btn-search {
  flex: 1;
  min-width: 72px;
  padding: 8px 12px;
  font-size: 12px;
  font-weight: 500;
  color: #fff;
  background: linear-gradient(135deg, $blue-primary, $cyan-primary);
  border: 1px solid var(--crm-action-primary-border);
  border-radius: 6px;
  cursor: pointer;
  transition: box-shadow 0.15s, transform 0.12s;

  &:hover {
    box-shadow: var(--crm-shadow-glow);
    transform: translateY(-1px);
  }
}

.btn-reset {
  padding: 8px 12px;
  font-size: 12px;
  color: $text-secondary;
  background: $layer-3;
  border: 1px solid $border-panel;
  border-radius: 6px;
  cursor: pointer;

  &:hover {
    background: var(--crm-accent-008);
    border-color: var(--crm-accent-018);
  }
}
</style>
