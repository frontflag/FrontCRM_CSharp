<template>
  <div class="industry-news-item">
    <p v-if="item.title" class="industry-news-item__line industry-news-item__title">{{ item.title }}</p>
    <p v-if="item.summary" class="industry-news-item__line">{{ item.summary }}</p>
    <p v-if="item.url" class="industry-news-item__line">
      <a :href="item.url" target="_blank" rel="noopener noreferrer" class="ext-link">{{ item.url }}</a>
    </p>
  </div>
</template>

<script setup lang="ts">
import type { IndustryNewsItem } from '@/utils/jsonDisplay'

defineProps<{
  item: IndustryNewsItem
}>()
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.industry-news-item {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.industry-news-item__line {
  margin: 0;
  font-size: 13px;
  line-height: 1.55;
  color: $text-primary;
  word-break: break-word;
}

.industry-news-item__title {
  font-weight: 600;
}

.ext-link {
  color: $cyan-primary;
  text-decoration: none;

  &:hover {
    text-decoration: underline;
  }
}
</style>
