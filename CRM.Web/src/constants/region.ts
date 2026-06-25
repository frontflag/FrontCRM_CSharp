import { regionData } from '@/data/regions';

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

function stripAdminSuffix(name: string): string {
  let s = name.trim();
  if (!s) return s;
  for (const suf of ['特别行政区', '自治区', '省', '市', '区', '县']) {
    if (s.endsWith(suf) && s.length > suf.length) {
      return s.slice(0, -suf.length);
    }
  }
  return s;
}

function regionNamesEqual(a: string, b: string): boolean {
  const ta = stripAdminSuffix(a);
  const tb = stripAdminSuffix(b);
  return ta.length > 0 && ta === tb;
}

/** 根据市/区在中国行政区划数据中反查省 */
export function lookupProvinceFromCityDistrict(
  city: string | null | undefined,
  district?: string | null | undefined
): string {
  const cityRaw = (city ?? '').trim();
  if (!cityRaw) return '';

  const districtRaw = (district ?? '').trim();
  type Match = { province: string; score: number };
  const matches: Match[] = [];

  for (const province of regionData) {
    const pName = province.value;
    if (regionNamesEqual(cityRaw, pName)) {
      matches.push({ province: pName, score: districtRaw ? 2 : 3 });
    }
    for (const cityNode of province.children ?? []) {
      if (!regionNamesEqual(cityRaw, cityNode.value)) continue;
      let score = 3;
      if (districtRaw) {
        const distMatch = (cityNode.children ?? []).some((d) => regionNamesEqual(districtRaw, d.value));
        score = distMatch ? 5 : 1;
      }
      matches.push({ province: pName, score });
    }
  }

  if (matches.length === 0) return '';
  matches.sort((a, b) => b.score - a.score);
  return matches[0]!.province;
}

/** 将省/市/区规范化为 regionData 中的标准 label */
export function lookupCanonicalRegionLabels(
  province: string,
  city: string,
  district?: string | null
): { province: string; city: string; district?: string } | null {
  const pRaw = province.trim();
  const cRaw = city.trim();
  const dRaw = (district ?? '').trim();
  if (!pRaw && !cRaw) return null;

  for (const p of regionData) {
    if (pRaw && !regionNamesEqual(pRaw, p.value)) continue;
    for (const c of p.children ?? []) {
      if (!regionNamesEqual(cRaw, c.value)) continue;
      let districtOut = dRaw || undefined;
      if (dRaw) {
        const found = (c.children ?? []).find((d) => regionNamesEqual(dRaw, d.value));
        if (found) districtOut = found.value;
      }
      return { province: p.value, city: c.value, district: districtOut };
    }
    if (regionNamesEqual(cRaw, p.value)) {
      let districtOut = dRaw || undefined;
      const cityNode = p.children?.[0];
      if (dRaw && cityNode?.children) {
        const found = cityNode.children.find((d) => regionNamesEqual(dRaw, d.value));
        if (found) districtOut = found.value;
      }
      return { province: p.value, city: p.value, district: districtOut };
    }
  }

  if (!pRaw && cRaw) {
    for (const p of regionData) {
      for (const c of p.children ?? []) {
        if (!regionNamesEqual(cRaw, c.value)) continue;
        let districtOut = dRaw || undefined;
        if (dRaw) {
          const found = (c.children ?? []).find((d) => regionNamesEqual(dRaw, d.value));
          if (found) districtOut = found.value;
        }
        return { province: p.value, city: c.value, district: districtOut };
      }
      if (regionNamesEqual(cRaw, p.value)) {
        return { province: p.value, city: p.value, district: dRaw || undefined };
      }
    }
  }

  return null;
}

/** AI 客户解析后补全省/市/区标准名（如 深圳+福田 → 广东省） */
export function enrichCustomerRegionFields<T extends {
  province: string;
  city: string;
  district: string;
  country: string;
}>(fields: T): T {
  let province = fields.province.trim();
  let city = fields.city.trim();
  let district = fields.district.trim();
  let country = fields.country.trim();

  if (!province && city) {
    province = lookupProvinceFromCityDistrict(city, district);
  }

  const canonical = lookupCanonicalRegionLabels(province, city, district);
  if (canonical) {
    province = canonical.province;
    city = canonical.city;
    if (canonical.district) district = canonical.district;
  }

  if (!country && province) {
    country = '中国';
  }

  return { ...fields, province, city, district, country };
}
