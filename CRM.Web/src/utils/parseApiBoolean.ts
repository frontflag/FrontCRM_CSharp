/**
 * 将接口/表格行中的「是否」字段规范为 boolean。
 * 避免 `Boolean("false") === true`（非空字符串为真）等问题。
 */
export function parseApiBoolean(val: unknown): boolean {
  if (val === true || val === 1) return true;
  if (val === false || val === 0) return false;
  if (val == null) return false;
  if (typeof val === 'string') {
    const s = val.trim().toLowerCase();
    if (s === 'true' || s === '1' || s === 'yes' || s === 'y') return true;
    return false;
  }
  return false;
}
