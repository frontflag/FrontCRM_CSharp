/** 将文本写入系统剪贴板（同步 execCommand，兼容 HTTP 与下拉菜单等场景）。 */
export function copyTextToClipboard(text: string): boolean {
  const value = String(text ?? '')
  if (!value) return false

  try {
    const el = document.createElement('textarea')
    el.value = value
    el.setAttribute('readonly', 'true')
    el.style.position = 'fixed'
    el.style.left = '-9999px'
    el.style.top = '0'
    el.style.opacity = '0'
    document.body.appendChild(el)
    el.focus()
    el.select()
    el.setSelectionRange(0, value.length)
    const ok = document.execCommand('copy')
    document.body.removeChild(el)
    return ok
  } catch {
    return false
  }
}
