<script setup lang="ts">
import { reactive, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()

const form = reactive({
  keyword: '',
  status: undefined as number | undefined
})

function syncFromRoute() {
  if (route.name !== 'RFQList') return
  form.keyword = typeof route.query.keyword === 'string' ? route.query.keyword : ''
  const s = route.query.status
  if (s === undefined || s === null || s === '') {
    form.status = undefined
    return
  }
  const n = Number(s)
  form.status = Number.isNaN(n) ? undefined : n
}

watch(
  () => [route.name, route.query] as const,
  () => syncFromRoute(),
  { deep: true, immediate: true }
)

function handleReset() {
  router.push({ name: 'RFQList', query: {} })
}

function handleSearch() {
  const query: Record<string, string> = {}
  const kw = form.keyword.trim()
  if (kw) query.keyword = kw
  if (form.status !== undefined && form.status !== null) query.status = String(form.status)
  router.push({ name: 'RFQList', query })
}
</script>

<template>
  <div class="rfq-search-panel">
    <div class="rfq-search-panel__head">{{ t('leftPanel.rfqSearchTitle') }}</div>

    <div class="rfq-search-panel__fields">
      <div class="field-col">
        <label class="field-label">{{ t('rfqList.filters.search') }}</label>
        <div class="field-control">
          <input
            v-model="form.keyword"
            type="text"
            class="field-input"
            :placeholder="t('leftPanel.rfqKeywordPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
      </div>

      <div class="field-col">
        <label class="field-label">{{ t('rfqList.filters.status') }}</label>
        <el-select
          v-model="form.status"
          :placeholder="t('rfqList.filters.allStatus')"
          clearable
          class="field-select"
          filterable
          :teleported="false"
        >
          <el-option :label="t('rfqList.status.pending')" :value="0" />
          <el-option :label="t('rfqList.status.assigned')" :value="1" />
          <el-option :label="t('rfqList.status.processing')" :value="2" />
          <el-option :label="t('rfqList.status.quoted')" :value="3" />
          <el-option :label="t('rfqList.status.selected')" :value="4" />
          <el-option :label="t('rfqList.status.converted')" :value="5" />
          <el-option :label="t('rfqList.status.closed')" :value="7" />
          <el-option :label="t('rfqList.status.cancelled')" :value="8" />
        </el-select>
      </div>
    </div>

    <div class="rfq-search-panel__actions">
      <button type="button" class="btn-search" @click="handleSearch">{{ t('rfqList.filters.query') }}</button>
      <button type="button" class="btn-reset" @click="handleReset">{{ t('rfqList.filters.reset') }}</button>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.rfq-search-panel {
  min-height: 80px;
  font-size: 12px;
  color: $text-secondary;
}

.rfq-search-panel__head {
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 12px;
  font-size: 13px;
}

.rfq-search-panel__fields {
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

.field-select {
  width: 100%;
}

.rfq-search-panel__actions {
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
