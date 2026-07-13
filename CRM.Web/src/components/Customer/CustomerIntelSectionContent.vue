<template>
  <div class="ci-section-content">
    <dl v-if="rows.length" class="ci-kv-list">
      <div v-for="row in rows" :key="row.key" class="ci-kv-row">
        <dt class="ci-kv-row__key">{{ row.label }}</dt>
        <dd class="ci-kv-row__val">
          <a
            v-if="row.isUrl"
            :href="row.value"
            target="_blank"
            rel="noopener noreferrer"
            class="ci-kv-row__link"
          >{{ row.value }}</a>
          <span v-else>{{ row.value }}</span>
        </dd>
      </div>
    </dl>

    <div v-for="block in listBlocks" :key="block.title" class="ci-list-block">
      <h4 class="ci-list-block__title">{{ block.title }}</h4>
      <dl class="ci-kv-list">
        <div v-for="row in block.rows" :key="`${block.title}-${row.key}`" class="ci-kv-row">
          <dt class="ci-kv-row__key">{{ row.label }}</dt>
          <dd class="ci-kv-row__val">
            <a
              v-if="row.isUrl"
              :href="row.value"
              target="_blank"
              rel="noopener noreferrer"
              class="ci-kv-row__link"
            >{{ row.value }}</a>
            <span v-else>{{ row.value }}</span>
          </dd>
        </div>
      </dl>
    </div>

    <p v-if="!rows.length && !listBlocks.length" class="ci-section-content__empty">—</p>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { buildCustomerIntelContentView } from '@/utils/customerIntelSchema'

const props = defineProps<{
  content: Record<string, unknown>
}>()

const view = computed(() => buildCustomerIntelContentView(props.content))
const rows = computed(() => view.value.rows)
const listBlocks = computed(() => view.value.listBlocks)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.ci-section-content {
  &__empty {
    margin: 0;
    font-size: 13px;
    color: $text-muted;
  }
}

.ci-kv-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin: 0;
}

.ci-kv-row {
  display: grid;
  grid-template-columns: minmax(88px, 34%) 1fr;
  gap: 8px 12px;
  align-items: start;
  padding-bottom: 10px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);

  &:last-child {
    padding-bottom: 0;
    border-bottom: none;
  }

  &__key {
    margin: 0;
    font-size: 12px;
    font-weight: 500;
    color: $text-muted;
    line-height: 1.5;
  }

  &__val {
    margin: 0;
    font-size: 13px;
    color: $text-primary;
    line-height: 1.55;
    word-break: break-word;
    white-space: pre-wrap;
  }

  &__link {
    color: $cyan-primary;
    text-decoration: none;

    &:hover {
      text-decoration: underline;
    }
  }
}

.ci-list-block {
  margin-top: 12px;
  padding-top: 12px;
  border-top: 1px dashed $border-panel;

  &__title {
    margin: 0 0 10px;
    font-size: 12px;
    font-weight: 600;
    color: $text-secondary;
  }
}
</style>
