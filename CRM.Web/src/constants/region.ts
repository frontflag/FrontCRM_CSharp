/** 未选择「区」时写入后端的占位（U+2014 一字线，避免与普通连字符混淆） */
export const REGION_DISTRICT_PLACEHOLDER = '—';

export function isDistrictPlaceholder(v: string | null | undefined): boolean {
  return v == null || v === '' || v === REGION_DISTRICT_PLACEHOLDER;
}

/** 级联控件 v-model：有真实区则三级，否则仅省/市 */
export function regionCascaderValueFromFields(
  province: string | null | undefined,
  city: string | null | undefined,
  district: string | null | undefined
): string[] {
  const p = (province ?? '').trim();
  const c = (city ?? '').trim();
  if (!p || !c) return [];
  if (isDistrictPlaceholder(district)) return [p, c];
  const d = (district ?? '').trim();
  if (!d) return [p, c];
  return [p, c, d];
}
