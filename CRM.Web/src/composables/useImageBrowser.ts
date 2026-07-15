import { storeToRefs } from 'pinia'
import { useImageBrowserStore } from '@/stores/imageBrowser'
import type { ImageBrowserOpenOptions } from '@/types/imageBrowser'

export function useImageBrowser() {
  const store = useImageBrowserStore()
  const { visible, title, items, activeIndex } = storeToRefs(store)

  return {
    visible,
    title,
    items,
    activeIndex,
    openImageBrowser: (options: ImageBrowserOpenOptions) => store.open(options),
    closeImageBrowser: () => store.close()
  }
}
