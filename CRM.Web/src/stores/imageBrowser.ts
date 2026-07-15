import { ref } from 'vue'
import { defineStore } from 'pinia'
import type { ImageBrowserItem, ImageBrowserOpenOptions } from '@/types/imageBrowser'

export const useImageBrowserStore = defineStore('imageBrowser', () => {
  const visible = ref(false)
  const title = ref('')
  const items = ref<ImageBrowserItem[]>([])
  const activeIndex = ref(0)

  function open(options: ImageBrowserOpenOptions) {
    const list = (options.items ?? []).filter((x) => x && String(x.id || '').trim())
    if (list.length === 0) return
    items.value = list
    const maxIdx = list.length - 1
    const idx = Math.min(Math.max(0, options.initialIndex ?? 0), maxIdx)
    activeIndex.value = idx
    title.value = String(options.title ?? '').trim()
    visible.value = true
  }

  function close() {
    visible.value = false
    items.value = []
    activeIndex.value = 0
    title.value = ''
  }

  return {
    visible,
    title,
    items,
    activeIndex,
    open,
    close
  }
})
