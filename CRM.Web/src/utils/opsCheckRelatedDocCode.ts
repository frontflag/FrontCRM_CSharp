/** 运维检查「关联单号」：Debug 是建议入口，不是业务单号。 */
export function displayOpsCheckRelatedDocCode(
  code?: string | null,
  docType?: string | null
): string {
  const type = String(docType ?? '').trim().toLowerCase()
  if (type === 'debug') return ''
  const s = String(code ?? '').trim()
  if (!s || s.toLowerCase() === 'debug') return ''
  return s
}
