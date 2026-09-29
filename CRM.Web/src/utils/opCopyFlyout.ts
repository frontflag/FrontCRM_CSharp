import { nextTick } from 'vue'

/** 右侧不够时只翻到左侧，避免落到主菜单下方 */
export const OP_COPY_FLYOUT_FALLBACK_PLACEMENTS = ['left-start', 'left-end'] as const

export const OP_COPY_FLYOUT_POPPER_CLASS = 'op-copy-flyout-popper'

/** 子菜单在左侧时给触发条加 is-left，箭头改为朝左 */
export function syncOpCopyFlyoutPlacementClass() {
  void nextTick(() => {
    document.querySelectorAll(`.${OP_COPY_FLYOUT_POPPER_CLASS}`).forEach((p) => {
      const el = p as HTMLElement
      const hidden = el.getAttribute('aria-hidden') === 'true' || el.style.display === 'none'
      const placement = el.getAttribute('data-popper-placement') || ''
      const id = el.getAttribute('id')
      if (!id) return
      const trigger =
        document.querySelector(`[aria-describedby="${id}"]`) ||
        document.querySelector(`[aria-controls="${id}"]`)
      const row = trigger?.closest('.op-copy-flyout__trigger')
      if (!(row instanceof HTMLElement)) return
      row.classList.toggle('is-left', !hidden && placement.startsWith('left'))
    })
  })
}
