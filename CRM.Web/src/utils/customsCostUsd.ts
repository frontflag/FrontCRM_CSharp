/** 报关采购美金价：须为有限数字且 &gt; 0（6 位小数，无上限）。 */
export function isValidCustomsCostUsd(value: unknown): boolean {
  const n = Number(value)
  return Number.isFinite(n) && n > 0
}
