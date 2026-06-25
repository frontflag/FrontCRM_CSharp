<template>
  <div class="labeled-rows">
    <p v-for="row in rows" :key="row.key" class="labeled-rows__line">
      <span class="labeled-rows__label">{{ row.label }}：</span>
      <a
        v-if="row.isUrl"
        :href="row.value"
        target="_blank"
        rel="noopener noreferrer"
        class="labeled-rows__text ext-link"
      >{{ row.value }}</a>
      <span v-else class="labeled-rows__text">{{ row.value }}</span>
    </p>
  </div>
</template>

<script setup lang="ts">
export type LabeledRow = {
  key: string
  label: string
  value: string
  isUrl: boolean
}

defineProps<{
  rows: LabeledRow[]
}>()
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.labeled-rows {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.labeled-rows__line {
  margin: 0;
  font-size: 13px;
  line-height: 1.55;
  color: $text-primary;
  word-break: break-word;
}

.labeled-rows__label {
  font-weight: 600;
  color: $text-secondary;
}

.ext-link {
  color: $cyan-primary;
  text-decoration: none;

  &:hover {
    text-decoration: underline;
  }
}
</style>
