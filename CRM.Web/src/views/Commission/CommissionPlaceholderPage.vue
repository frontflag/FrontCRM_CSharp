<template>
  <div class="commission-placeholder">
    <div class="section-head">
      <div class="section-title"><span class="title-bar"></span>{{ pageTitle }}</div>
      <p class="section-hint">{{ t('layout.underDevelopmentMessage', { name: pageTitle }) }}</p>
    </div>
    <el-empty :description="t('layout.underDevelopmentTitle')" />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'

const route = useRoute()
const { t } = useI18n()

const titleKeyByName: Record<string, string> = {
  CommissionEstimatedSales: 'layout.menu.commissionEstimatedSales',
  CommissionEstimatedPurchase: 'layout.menu.commissionEstimatedPurchase',
  CommissionOfficialSales: 'layout.menu.commissionOfficialSales',
  CommissionOfficialPurchase: 'layout.menu.commissionOfficialPurchase'
}

const pageTitle = computed(() => {
  const key = titleKeyByName[String(route.name || '')]
  return key ? t(key) : String(route.meta.title || '')
})
</script>

<style scoped>
.commission-placeholder {
  padding: 8px 4px 24px;
}

.section-head {
  margin-bottom: 16px;
}

.section-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 16px;
  font-weight: 600;
}

.title-bar {
  width: 3px;
  height: 16px;
  border-radius: 2px;
  background: var(--el-color-primary);
}

.section-hint {
  margin: 8px 0 0;
  color: var(--el-text-color-secondary);
  font-size: 13px;
  line-height: 1.5;
}
</style>
