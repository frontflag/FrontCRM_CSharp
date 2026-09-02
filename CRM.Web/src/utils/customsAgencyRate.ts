/** 报关代理费率：1+纯费率，须为有限数字且 ≥ 1（不设上限）。 */
export function isValidCustomsAgencyRate(value: unknown): boolean {
  const n = Number(value)
  return Number.isFinite(n) && n >= 1
}
