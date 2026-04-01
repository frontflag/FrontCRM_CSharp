import { ref, computed } from 'vue'

/** 持久化键：仅存 'dark' | 'light'；未设置时 index.html 默认明版 light */
export const UI_THEME_STORAGE_KEY = 'crm-ui-theme'
export type UiTheme = 'dark' | 'light'

function readThemeFromDom(): UiTheme {
  return document.documentElement.getAttribute('data-theme') === 'light' ? 'light' : 'dark'
}

/** 与 index.html 内联脚本一致：写入 html[data-theme] 并持久化 */
export function applyUiTheme(theme: UiTheme): void {
  document.documentElement.setAttribute('data-theme', theme)
  try {
    localStorage.setItem(UI_THEME_STORAGE_KEY, theme)
  } catch {
    /* ignore */
  }
}

export function useUiTheme() {
  const theme = ref<UiTheme>(readThemeFromDom())
  const isDark = computed(() => theme.value === 'dark')

  function setTheme(next: UiTheme) {
    theme.value = next
    applyUiTheme(next)
  }

  function toggleTheme() {
    setTheme(theme.value === 'dark' ? 'light' : 'dark')
  }

  return { theme, isDark, setTheme, toggleTheme }
}
