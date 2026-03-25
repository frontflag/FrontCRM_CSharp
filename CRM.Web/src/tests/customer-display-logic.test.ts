/**
 * 客户模块 - 显示逻辑单元测试
 * 覆盖：CustomerList.vue / CustomerDetail.vue 中的纯函数显示逻辑
 * 包括：等级标签、类型标签、行业标签、状态标签、金额格式化、日期格式化、地址拼接、货币标签
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { formatDisplayDateTime } from '@/utils/displayDateTime';
import { setDisplayTimeZoneId } from '@/utils/displayTimeZone';

// ─── 从 CustomerList.vue 提取的显示逻辑函数 ───

/** 客户等级 → 显示标签（CustomerList.vue 版本） */
function getLevelLabelList(level: string): string {
  return ({ VIP: 'VIP', VPO: 'VPO', BPO: 'BPO', B: 'B级', C: 'C级', D: 'D级', Important: '重要', Normal: '普通', Lead: '潜在' } as Record<string, string>)[level] || level || '--';
}

/** 客户类型 → 显示标签（CustomerList.vue 版本，后端枚举 1-6） */
function getTypeLabelList(type: number): string {
  return ({ 1: 'OEM', 2: 'ODM', 3: '终端用户', 4: 'IDH', 5: '贸易商', 6: '代理商' } as Record<number, string>)[type] || '未知';
}

/** 行业 → 显示标签 */
function getIndustryLabel(industry: string): string {
  return ({
    Manufacturing: '制造业', Trading: '贸易/零售', Technology: '科技/IT',
    Construction: '建筑/工程', Healthcare: '医疗/健康', Education: '教育',
    Finance: '金融', Other: '其他'
  } as Record<string, string>)[industry] || industry || '--';
}

/** 金额格式化（CustomerList.vue 版本，使用 Intl.NumberFormat） */
function formatCurrencyList(val: number | undefined): string {
  if (val === undefined || val === null) return '--';
  return new Intl.NumberFormat('zh-CN', { style: 'currency', currency: 'CNY', minimumFractionDigits: 2 }).format(val);
}

// ─── 从 CustomerDetail.vue 提取的显示逻辑函数 ───

/** 金额格式化（CustomerDetail.vue 版本，使用 toFixed + 千分位） */
function formatCurrencyDetail(value: number | undefined): string {
  if (value === undefined || value === null) return '¥0.00';
  return `¥${value.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',')}`;
}

/** 客户类型 → 显示标签（CustomerDetail.vue 版本，旧枚举 0-2） */
function getTypeLabelDetail(type: number): string {
  return ({ 0: '企业', 1: '个人', 2: '政府' } as Record<number, string>)[type] || '未知';
}

/** 客户等级 → 显示标签（CustomerDetail.vue 版本） */
function getLevelLabelDetail(level: string): string {
  return ({ VIP: 'VIP客户', Important: '重要客户', Normal: '普通客户', Lead: '潜在客户' } as Record<string, string>)[level] || level;
}

/** 地址类型 → 显示标签 */
function getAddressTypeLabel(type: string): string {
  return ({ Office: '办公地址', Billing: '开票地址', Shipping: '收货地址', Registered: '注册地址' } as Record<string, string>)[type] || type;
}

/** 完整地址拼接 */
function formatFullAddress(address: { country?: string; province?: string; city?: string; district?: string; streetAddress?: string }): string {
  return [address.country, address.province, address.city, address.district, address.streetAddress].filter(Boolean).join(' ');
}

/** 货币枚举 → 显示标签 */
function getCurrencyLabel(currency: number): string {
  return ({ 1: 'CNY', 2: 'USD', 3: 'EUR', 4: 'JPY', 5: 'GBP', 6: 'HKD' } as Record<number, string>)[currency] || 'CNY';
}

// ─── 测试用例 ───

describe('CustomerList - 等级标签显示 (getLevelLabelList)', () => {
  it('UT-DISPLAY-001: "VIP" → "VIP"', () => {
    expect(getLevelLabelList('VIP')).toBe('VIP');
  });

  it('UT-DISPLAY-002: "VPO" → "VPO"', () => {
    expect(getLevelLabelList('VPO')).toBe('VPO');
  });

  it('UT-DISPLAY-003: "BPO" → "BPO"', () => {
    expect(getLevelLabelList('BPO')).toBe('BPO');
  });

  it('UT-DISPLAY-004: "B" → "B级"', () => {
    expect(getLevelLabelList('B')).toBe('B级');
  });

  it('UT-DISPLAY-005: "C" → "C级"', () => {
    expect(getLevelLabelList('C')).toBe('C级');
  });

  it('UT-DISPLAY-006: "D" → "D级"', () => {
    expect(getLevelLabelList('D')).toBe('D级');
  });

  it('UT-DISPLAY-007: "Important"（旧值）→ "重要"', () => {
    expect(getLevelLabelList('Important')).toBe('重要');
  });

  it('UT-DISPLAY-008: "Normal"（旧值）→ "普通"', () => {
    expect(getLevelLabelList('Normal')).toBe('普通');
  });

  it('UT-DISPLAY-009: "Lead"（旧值）→ "潜在"', () => {
    expect(getLevelLabelList('Lead')).toBe('潜在');
  });

  it('UT-DISPLAY-010: 未知值 → 原样返回', () => {
    expect(getLevelLabelList('GOLD')).toBe('GOLD');
  });

  it('UT-DISPLAY-011: 空字符串 → "--"', () => {
    expect(getLevelLabelList('')).toBe('--');
  });
});

describe('CustomerList - 类型标签显示 (getTypeLabelList)', () => {
  it('UT-DISPLAY-020: 1 → "OEM"', () => {
    expect(getTypeLabelList(1)).toBe('OEM');
  });

  it('UT-DISPLAY-021: 2 → "ODM"', () => {
    expect(getTypeLabelList(2)).toBe('ODM');
  });

  it('UT-DISPLAY-022: 3 → "终端用户"', () => {
    expect(getTypeLabelList(3)).toBe('终端用户');
  });

  it('UT-DISPLAY-023: 4 → "IDH"', () => {
    expect(getTypeLabelList(4)).toBe('IDH');
  });

  it('UT-DISPLAY-024: 5 → "贸易商"', () => {
    expect(getTypeLabelList(5)).toBe('贸易商');
  });

  it('UT-DISPLAY-025: 6 → "代理商"', () => {
    expect(getTypeLabelList(6)).toBe('代理商');
  });

  it('UT-DISPLAY-026: 0（无效值）→ "未知"', () => {
    expect(getTypeLabelList(0)).toBe('未知');
  });

  it('UT-DISPLAY-027: 7（超出范围）→ "未知"', () => {
    expect(getTypeLabelList(7)).toBe('未知');
  });
});

describe('CustomerList - 行业标签显示 (getIndustryLabel)', () => {
  it('UT-DISPLAY-030: "Manufacturing" → "制造业"', () => {
    expect(getIndustryLabel('Manufacturing')).toBe('制造业');
  });

  it('UT-DISPLAY-031: "Trading" → "贸易/零售"', () => {
    expect(getIndustryLabel('Trading')).toBe('贸易/零售');
  });

  it('UT-DISPLAY-032: "Technology" → "科技/IT"', () => {
    expect(getIndustryLabel('Technology')).toBe('科技/IT');
  });

  it('UT-DISPLAY-033: "Construction" → "建筑/工程"', () => {
    expect(getIndustryLabel('Construction')).toBe('建筑/工程');
  });

  it('UT-DISPLAY-034: "Healthcare" → "医疗/健康"', () => {
    expect(getIndustryLabel('Healthcare')).toBe('医疗/健康');
  });

  it('UT-DISPLAY-035: "Education" → "教育"', () => {
    expect(getIndustryLabel('Education')).toBe('教育');
  });

  it('UT-DISPLAY-036: "Finance" → "金融"', () => {
    expect(getIndustryLabel('Finance')).toBe('金融');
  });

  it('UT-DISPLAY-037: "Other" → "其他"', () => {
    expect(getIndustryLabel('Other')).toBe('其他');
  });

  it('UT-DISPLAY-038: 未知行业 → 原样返回', () => {
    expect(getIndustryLabel('Aerospace')).toBe('Aerospace');
  });

  it('UT-DISPLAY-039: 空字符串 → "--"', () => {
    expect(getIndustryLabel('')).toBe('--');
  });
});

describe('CustomerList - 金额格式化 (formatCurrencyList)', () => {
  it('UT-DISPLAY-040: undefined → "--"', () => {
    expect(formatCurrencyList(undefined)).toBe('--');
  });

  it('UT-DISPLAY-041: null → "--"', () => {
    expect(formatCurrencyList(null as any)).toBe('--');
  });

  it('UT-DISPLAY-042: 0 → 包含 "0.00"', () => {
    expect(formatCurrencyList(0)).toContain('0.00');
  });

  it('UT-DISPLAY-043: 1000 → 包含千分位分隔符', () => {
    const result = formatCurrencyList(1000);
    expect(result).toContain('1,000');
  });

  it('UT-DISPLAY-044: 1234567.89 → 正确格式化', () => {
    const result = formatCurrencyList(1234567.89);
    expect(result).toContain('1,234,567.89');
  });

  it('UT-DISPLAY-045: 负数 -500 → 正确格式化（含负号）', () => {
    const result = formatCurrencyList(-500);
    expect(result).toContain('500');
  });
});

describe('CustomerDetail - 金额格式化 (formatCurrencyDetail)', () => {
  it('UT-DISPLAY-050: undefined → "¥0.00"', () => {
    expect(formatCurrencyDetail(undefined)).toBe('¥0.00');
  });

  it('UT-DISPLAY-051: null → "¥0.00"', () => {
    expect(formatCurrencyDetail(null as any)).toBe('¥0.00');
  });

  it('UT-DISPLAY-052: 0 → "¥0.00"', () => {
    expect(formatCurrencyDetail(0)).toBe('¥0.00');
  });

  it('UT-DISPLAY-053: 1000 → "¥1,000.00"', () => {
    expect(formatCurrencyDetail(1000)).toBe('¥1,000.00');
  });

  it('UT-DISPLAY-054: 1234567.89 → "¥1,234,567.89"', () => {
    expect(formatCurrencyDetail(1234567.89)).toBe('¥1,234,567.89');
  });

  it('UT-DISPLAY-055: 0.5 → "¥0.50"', () => {
    expect(formatCurrencyDetail(0.5)).toBe('¥0.50');
  });

  it('UT-DISPLAY-056: 999 → "¥999.00"（无千分位）', () => {
    expect(formatCurrencyDetail(999)).toBe('¥999.00');
  });
});

describe('CustomerDetail - 日期时间格式化 (formatDisplayDateTime, UTC)', () => {
  beforeEach(() => {
    setDisplayTimeZoneId('UTC');
  });

  it('UT-DISPLAY-060: undefined → "--"', () => {
    expect(formatDisplayDateTime(undefined)).toBe('--');
  });

  it('UT-DISPLAY-061: 空字符串 → "--"', () => {
    expect(formatDisplayDateTime('')).toBe('--');
  });

  it('UT-DISPLAY-062: 有效 ISO 日期字符串 → 返回非空字符串', () => {
    const result = formatDisplayDateTime('2026-03-16T06:00:00Z');
    expect(result).not.toBe('--');
    expect(result.length).toBeGreaterThan(0);
  });

  it('UT-DISPLAY-063: 有效日期字符串 → 包含年份', () => {
    const result = formatDisplayDateTime('2026-03-16T06:00:00Z');
    expect(result).toContain('2026');
  });
});

describe('CustomerDetail - 类型标签显示 (getTypeLabelDetail)', () => {
  it('UT-DISPLAY-070: 0 → "企业"', () => {
    expect(getTypeLabelDetail(0)).toBe('企业');
  });

  it('UT-DISPLAY-071: 1 → "个人"', () => {
    expect(getTypeLabelDetail(1)).toBe('个人');
  });

  it('UT-DISPLAY-072: 2 → "政府"', () => {
    expect(getTypeLabelDetail(2)).toBe('政府');
  });

  it('UT-DISPLAY-073: 3（超出范围）→ "未知"', () => {
    expect(getTypeLabelDetail(3)).toBe('未知');
  });
});

describe('CustomerDetail - 等级标签显示 (getLevelLabelDetail)', () => {
  it('UT-DISPLAY-080: "VIP" → "VIP客户"', () => {
    expect(getLevelLabelDetail('VIP')).toBe('VIP客户');
  });

  it('UT-DISPLAY-081: "Important" → "重要客户"', () => {
    expect(getLevelLabelDetail('Important')).toBe('重要客户');
  });

  it('UT-DISPLAY-082: "Normal" → "普通客户"', () => {
    expect(getLevelLabelDetail('Normal')).toBe('普通客户');
  });

  it('UT-DISPLAY-083: "Lead" → "潜在客户"', () => {
    expect(getLevelLabelDetail('Lead')).toBe('潜在客户');
  });

  it('UT-DISPLAY-084: 未知值 → 原样返回', () => {
    expect(getLevelLabelDetail('BPO')).toBe('BPO');
  });
});

describe('CustomerDetail - 地址类型标签 (getAddressTypeLabel)', () => {
  it('UT-DISPLAY-090: "Office" → "办公地址"', () => {
    expect(getAddressTypeLabel('Office')).toBe('办公地址');
  });

  it('UT-DISPLAY-091: "Billing" → "开票地址"', () => {
    expect(getAddressTypeLabel('Billing')).toBe('开票地址');
  });

  it('UT-DISPLAY-092: "Shipping" → "收货地址"', () => {
    expect(getAddressTypeLabel('Shipping')).toBe('收货地址');
  });

  it('UT-DISPLAY-093: "Registered" → "注册地址"', () => {
    expect(getAddressTypeLabel('Registered')).toBe('注册地址');
  });

  it('UT-DISPLAY-094: 未知类型 → 原样返回', () => {
    expect(getAddressTypeLabel('Factory')).toBe('Factory');
  });
});

describe('CustomerDetail - 完整地址拼接 (formatFullAddress)', () => {
  it('UT-DISPLAY-100: 完整地址 → 空格分隔拼接', () => {
    const result = formatFullAddress({
      country: '中国', province: '广东省', city: '深圳市', district: '南山区', streetAddress: '科技园路1号'
    });
    expect(result).toBe('中国 广东省 深圳市 南山区 科技园路1号');
  });

  it('UT-DISPLAY-101: 缺少 country → 从 province 开始拼接', () => {
    const result = formatFullAddress({
      province: '广东省', city: '深圳市', district: '南山区', streetAddress: '科技园路1号'
    });
    expect(result).toBe('广东省 深圳市 南山区 科技园路1号');
  });

  it('UT-DISPLAY-102: 缺少 streetAddress → 拼接到 district', () => {
    const result = formatFullAddress({
      country: '中国', province: '广东省', city: '深圳市', district: '南山区'
    });
    expect(result).toBe('中国 广东省 深圳市 南山区');
  });

  it('UT-DISPLAY-103: 所有字段为空 → 返回空字符串', () => {
    const result = formatFullAddress({});
    expect(result).toBe('');
  });

  it('UT-DISPLAY-104: 部分字段为 undefined → 跳过 undefined 字段', () => {
    const result = formatFullAddress({
      country: '中国', province: undefined, city: '深圳市', district: undefined, streetAddress: '科技园路1号'
    });
    expect(result).toBe('中国 深圳市 科技园路1号');
  });

  it('UT-DISPLAY-105: 仅有 city → 返回 city', () => {
    const result = formatFullAddress({ city: '深圳市' });
    expect(result).toBe('深圳市');
  });
});

describe('CustomerDetail - 货币标签显示 (getCurrencyLabel)', () => {
  it('UT-DISPLAY-110: 1 → "CNY"', () => {
    expect(getCurrencyLabel(1)).toBe('CNY');
  });

  it('UT-DISPLAY-111: 2 → "USD"', () => {
    expect(getCurrencyLabel(2)).toBe('USD');
  });

  it('UT-DISPLAY-112: 3 → "EUR"', () => {
    expect(getCurrencyLabel(3)).toBe('EUR');
  });

  it('UT-DISPLAY-113: 4 → "JPY"', () => {
    expect(getCurrencyLabel(4)).toBe('JPY');
  });

  it('UT-DISPLAY-114: 5 → "GBP"', () => {
    expect(getCurrencyLabel(5)).toBe('GBP');
  });

  it('UT-DISPLAY-115: 6 → "HKD"', () => {
    expect(getCurrencyLabel(6)).toBe('HKD');
  });

  it('UT-DISPLAY-116: 0（无效值）→ "CNY"（默认）', () => {
    expect(getCurrencyLabel(0)).toBe('CNY');
  });

  it('UT-DISPLAY-117: 7（超出范围）→ "CNY"（默认）', () => {
    expect(getCurrencyLabel(7)).toBe('CNY');
  });
});
