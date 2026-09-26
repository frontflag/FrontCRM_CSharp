const EDITABLE_SELECTOR = [
  'input',
  'textarea',
  'select',
  '[contenteditable="true"]',
  '.el-input',
  '.el-textarea',
  '.el-select',
  '.el-date-editor',
  '.el-cascader',
  '.el-autocomplete',
  '.el-input-number'
].join(',')

const ACTIVATION_ROLES = new Set([
  'button',
  'link',
  'checkbox',
  'radio',
  'menuitem',
  'tab',
  'option',
  'switch'
])

export function isEditableTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) return false
  const tag = target.tagName
  if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return true
  if (target.isContentEditable) return true
  return !!target.closest(EDITABLE_SELECTOR)
}

export function isActivationTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) return false
  const tag = target.tagName
  if (tag === 'BUTTON' || tag === 'A' || tag === 'SUMMARY') return true
  const role = target.getAttribute('role')
  if (role && ACTIVATION_ROLES.has(role)) return true
  return !!target.closest('button, a, summary, [role="button"], [role="checkbox"], [role="radio"], [role="switch"]')
}

function isShown(el: HTMLElement): boolean {
  if (el.style.display === 'none' || el.style.visibility === 'hidden') return false
  if (typeof window.getComputedStyle !== 'function') return true
  const style = window.getComputedStyle(el)
  return style.display !== 'none' && style.visibility !== 'hidden'
}

/** 只认正在显示的弹层。关闭的抽屉仍会把 .el-overlay 留在页面上。 */
export function hasForeignOverlay(): boolean {
  if (typeof document === 'undefined') return false
  return Array.from(document.querySelectorAll<HTMLElement>('.el-overlay, .el-message-box')).some(isShown)
}

/** 空格开启新一轮。输入框、按钮、已打开的弹层、组字和长按都不抢。 */
export function shouldStartAiRound(event: KeyboardEvent): boolean {
  if (event.code !== 'Space') return false
  if (event.repeat || event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) return false
  if (event.isComposing) return false
  if (isEditableTarget(event.target)) return false
  if (isActivationTarget(event.target)) return false
  if (hasForeignOverlay()) return false
  return true
}
