/**
 * 《列表字段宽度规范》§3.3 — 按列标题估算最小列宽，保证表头完整显示、不换行、不省略。
 * 表头按 12px 字号、th 左右 padding 约 28px 估算；实现新列表时 minWidth 应 ≥ 本函数返回值。
 */
export function estimateListColumnHeaderMinWidth(
  label: string,
  options?: { align?: 'left' | 'center' | 'right'; extra?: number }
): number {
  const text = String(label ?? '').trim()
  if (!text) return 80

  let textWidth = 0
  let cjkCount = 0
  for (const ch of text) {
    const code = ch.codePointAt(0) ?? 0
    if (code > 0x7f) {
      cjkCount += 1
      textWidth += 13
    } else {
      textWidth += 7
    }
  }

  const cellPadding = 28
  const alignBuffer = options?.align === 'right' ? 8 : 4
  const extra = options?.extra ?? 0
  const estimated = Math.ceil(textWidth + cellPadding + alignBuffer + extra)

  // 与《列表字段宽度规范》§3.3 推荐下限对齐
  let specFloor = 80
  if (text.includes('(USD)') || text.includes('（USD）')) {
    specFloor = 130
  } else if (cjkCount >= 6) {
    specFloor = 150
  } else if (cjkCount >= 4) {
    specFloor = 120
  }

  return Math.max(specFloor, estimated)
}
