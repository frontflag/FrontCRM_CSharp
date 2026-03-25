<template>
  <div id="app">
    <router-view />
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { fetchDisplaySettings } from '@/api/systemDisplay'
import { setDisplayTimeZoneId } from '@/utils/displayTimeZone'

onMounted(async () => {
  try {
    const s = await fetchDisplaySettings()
    setDisplayTimeZoneId(s.displayTimeZoneId)
  } catch {
    /* 保持默认 Asia/Shanghai */
  }
})
</script>

<style scoped>
#app {
  width: 100%;
  height: 100vh;
}
</style>
