<template>
  <el-config-provider :locale="elementPlusLocale">
    <div id="app">
      <router-view />
    </div>
  </el-config-provider>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { fetchDisplaySettings } from '@/api/systemDisplay'
import { setDisplayTimeZoneId } from '@/utils/displayTimeZone'
import { useAuthStore } from '@/stores'
import { getElementPlusLocale } from '@/plugins/elementPlusLocale'

const authStore = useAuthStore()
const { locale } = useI18n()
const elementPlusLocale = computed(() => getElementPlusLocale(locale.value))

onMounted(async () => {
  try {
    const s = await fetchDisplaySettings()
    setDisplayTimeZoneId(s.displayTimeZoneId)
  } catch {
    /* 保持默认 Asia/Shanghai */
  }

  // 菜单隔离依赖 identityType（来自 permission-summary）
  // 兼容旧 localStorage：若缺失则在应用启动时补齐，避免菜单隔离失效
  try {
    if (authStore.isAuthenticated && (authStore.user?.identityType === undefined || authStore.user?.identityType === null)) {
      await authStore.fetchCurrentUser()
    }
    if (authStore.isAuthenticated) {
      await authStore.loadSimulationBanner()
    }
  } catch {
    /* 不阻断主流程 */
  }
})
</script>

<style scoped>
#app {
  width: 100%;
  height: 100vh;
}
</style>
