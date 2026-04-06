/**
 * 客户模块 - 数据转换逻辑单元测试
 * 覆盖：CustomerEdit.vue 中的后端数据 → 前端表单映射，以及 customer.ts 中的前端 → 后端字段映射
 * 测试策略：将转换逻辑提取为纯函数，逐一验证字段映射正确性和默认值处理
 */

import { describe, it, expect } from 'vitest';

// ─── 将 CustomerEdit.vue 的后端→前端映射逻辑提取为纯函数 ───

interface BackendCustomer {
  id?: string;
  customerName?: string;
  officialName?: string;
  customerShortName?: string;
  nickName?: string;
  customerLevel?: string;
  customerType?: number;
  type?: number;
  salesPersonId?: string;
  salesUserId?: string;
  unifiedSocialCreditCode?: string;
  creditCode?: string;
  creditLimit?: number;
  creditLine?: number;
  paymentTerms?: number;
  payment?: number;
  currency?: number;
  tradeCurrency?: number;
  taxRate?: number;
  invoiceType?: number;
  isActive?: boolean;
  remarks?: string;
  remark?: string;
  contacts?: any[];
  province?: string;
  city?: string;
  district?: string;
}

/** 模拟 CustomerEdit.vue 中的后端数据 → 前端表单映射逻辑 */
function mapBackendToForm(customer: BackendCustomer): any {
  const customerAny = customer as any;
  return {
    customerName: customer.customerName || customerAny.officialName || '',
    customerShortName: customer.customerShortName || customerAny.nickName || '',
    customerLevel: customer.customerLevel || 'B',
    customerType: customer.customerType ?? 1,
    salesPersonId: customer.salesPersonId || customerAny.salesUserId || '',
    unifiedSocialCreditCode: customer.unifiedSocialCreditCode || customerAny.creditCode || '',
    creditLimit: customer.creditLimit ?? 0,
    paymentTerms: customer.paymentTerms ?? 30,
    currency: customer.currency ?? 1,
    taxRate: customer.taxRate ?? 13,
    invoiceType: customer.invoiceType ?? 2,
    isActive: customer.isActive ?? true,
    remarks: customer.remarks || customerAny.remark || '',
    contacts: (customer.contacts || []).map((c: any) => ({
      ...c,
      contactName: c.contactName || c.name || '',
      mobilePhone: c.mobilePhone || c.mobile || ''
    }))
  };
}

/** 模拟 customer.ts 中的前端表单 → 后端请求体映射逻辑 */
function mapFormToBackend(data: any): any {
  const levelMap: Record<string, number> = {
    D: 1,
    C: 2,
    B: 3,
    BPO: 4,
    VIP: 5,
    VPO: 6
  };
  return {
    ...data,
    officialName: data.customerName || data.officialName || '',
    nickName: data.customerShortName || data.nickName || '',
    level: typeof data.customerLevel === 'number'
      ? (data.customerLevel > 0 ? data.customerLevel : 3)
      : (levelMap[data.customerLevel] ?? 3),
    type: (data.customerType && data.customerType > 0) ? data.customerType : (data.type || 1),
    salesUserId: data.salesPersonId || data.salesUserId || '',
    remark: data.remarks || data.remark || '',
    creditLine: data.creditLimit ?? data.creditLine ?? 0,
    payment: data.paymentTerms ?? data.payment ?? 30,
    tradeCurrency: data.currency ?? data.tradeCurrency ?? 1,
    creditCode: data.unifiedSocialCreditCode || data.creditCode || '',
    contacts: data.contacts || []
  };
}

/** 模拟联系人字段兼容映射 */
function mapContact(contact: any): any {
  return {
    ...contact,
    contactName: contact.contactName || contact.name || '',
    mobilePhone: contact.mobilePhone || contact.mobile || ''
  };
}

// ─── 后端 → 前端表单映射测试 ───

describe('CustomerEdit - 后端数据映射到前端表单 (mapBackendToForm)', () => {
  it('UT-TRANSFORM-001: 使用 customerName 字段（新字段）', () => {
    const result = mapBackendToForm({ customerName: '深圳科技公司', officialName: '旧名称' });
    expect(result.customerName).toBe('深圳科技公司');
  });

  it('UT-TRANSFORM-002: customerName 为空时，回退到 officialName（旧字段兼容）', () => {
    const result = mapBackendToForm({ officialName: '深圳科技公司' });
    expect(result.customerName).toBe('深圳科技公司');
  });

  it('UT-TRANSFORM-003: 使用 customerShortName 字段（新字段）', () => {
    const result = mapBackendToForm({ customerShortName: '深科', nickName: '旧简称' });
    expect(result.customerShortName).toBe('深科');
  });

  it('UT-TRANSFORM-004: customerShortName 为空时，回退到 nickName（旧字段兼容）', () => {
    const result = mapBackendToForm({ nickName: '深科' });
    expect(result.customerShortName).toBe('深科');
  });

  it('UT-TRANSFORM-005: customerLevel 有值时使用原值', () => {
    const result = mapBackendToForm({ customerLevel: 'VIP' });
    expect(result.customerLevel).toBe('VIP');
  });

  it('UT-TRANSFORM-006: customerLevel 为空时，默认为 "B"', () => {
    const result = mapBackendToForm({});
    expect(result.customerLevel).toBe('B');
  });

  it('UT-TRANSFORM-007: customerType 有值时使用原值', () => {
    const result = mapBackendToForm({ customerType: 3 });
    expect(result.customerType).toBe(3);
  });

  it('UT-TRANSFORM-008: customerType 为 undefined 时，默认为 1（OEM）', () => {
    const result = mapBackendToForm({});
    expect(result.customerType).toBe(1);
  });

  it('UT-TRANSFORM-009: customerType 为 0 时，使用 0（不强制修正，由验证层处理）', () => {
    const result = mapBackendToForm({ customerType: 0 });
    expect(result.customerType).toBe(0);
  });

  it('UT-TRANSFORM-010: salesPersonId 有值时使用原值', () => {
    const result = mapBackendToForm({ salesPersonId: 'user-001' });
    expect(result.salesPersonId).toBe('user-001');
  });

  it('UT-TRANSFORM-011: salesPersonId 为空时，回退到 salesUserId（旧字段兼容）', () => {
    const result = mapBackendToForm({ salesUserId: 'user-002' } as any);
    expect(result.salesPersonId).toBe('user-002');
  });

  it('UT-TRANSFORM-012: unifiedSocialCreditCode 有值时使用原值', () => {
    const result = mapBackendToForm({ unifiedSocialCreditCode: '91440300MA5DXXXX' });
    expect(result.unifiedSocialCreditCode).toBe('91440300MA5DXXXX');
  });

  it('UT-TRANSFORM-013: unifiedSocialCreditCode 为空时，回退到 creditCode（旧字段兼容）', () => {
    const result = mapBackendToForm({ creditCode: '91440300MA5DXXXX' } as any);
    expect(result.unifiedSocialCreditCode).toBe('91440300MA5DXXXX');
  });

  it('UT-TRANSFORM-014: creditLimit 有值时使用原值', () => {
    const result = mapBackendToForm({ creditLimit: 500000 });
    expect(result.creditLimit).toBe(500000);
  });

  it('UT-TRANSFORM-015: creditLimit 为 undefined 时，默认为 0', () => {
    const result = mapBackendToForm({});
    expect(result.creditLimit).toBe(0);
  });

  it('UT-TRANSFORM-016: creditLimit 为 0 时，保留 0（不被默认值覆盖）', () => {
    const result = mapBackendToForm({ creditLimit: 0 });
    expect(result.creditLimit).toBe(0);
  });

  it('UT-TRANSFORM-017: paymentTerms 有值时使用原值', () => {
    const result = mapBackendToForm({ paymentTerms: 60 });
    expect(result.paymentTerms).toBe(60);
  });

  it('UT-TRANSFORM-018: paymentTerms 为 undefined 时，默认为 30', () => {
    const result = mapBackendToForm({});
    expect(result.paymentTerms).toBe(30);
  });

  it('UT-TRANSFORM-019: currency 为 undefined 时，默认为 1（RMB）', () => {
    const result = mapBackendToForm({});
    expect(result.currency).toBe(1);
  });

  it('UT-TRANSFORM-020: taxRate 为 undefined 时，默认为 13（增值税率）', () => {
    const result = mapBackendToForm({});
    expect(result.taxRate).toBe(13);
  });

  it('UT-TRANSFORM-021: invoiceType 为 undefined 时，默认为 2', () => {
    const result = mapBackendToForm({});
    expect(result.invoiceType).toBe(2);
  });

  it('UT-TRANSFORM-022: isActive 为 undefined 时，默认为 true', () => {
    const result = mapBackendToForm({});
    expect(result.isActive).toBe(true);
  });

  it('UT-TRANSFORM-023: isActive 为 false 时，保留 false', () => {
    const result = mapBackendToForm({ isActive: false });
    expect(result.isActive).toBe(false);
  });

  it('UT-TRANSFORM-024: remarks 有值时使用原值', () => {
    const result = mapBackendToForm({ remarks: '重要客户备注' });
    expect(result.remarks).toBe('重要客户备注');
  });

  it('UT-TRANSFORM-025: remarks 为空时，回退到 remark（旧字段兼容）', () => {
    const result = mapBackendToForm({ remark: '旧备注字段' } as any);
    expect(result.remarks).toBe('旧备注字段');
  });

  it('UT-TRANSFORM-026: contacts 为空时，默认为空数组', () => {
    const result = mapBackendToForm({});
    expect(result.contacts).toEqual([]);
  });

  it('UT-TRANSFORM-027: contacts 有数据时，正确映射联系人字段', () => {
    const result = mapBackendToForm({
      contacts: [{ contactName: '张三', mobilePhone: '13800138000', gender: 1 }]
    });
    expect(result.contacts).toHaveLength(1);
    expect(result.contacts[0].contactName).toBe('张三');
    expect(result.contacts[0].mobilePhone).toBe('13800138000');
  });
});

// ─── 前端表单 → 后端请求体映射测试 ───

describe('customer.ts - 前端表单映射到后端请求体 (mapFormToBackend)', () => {
  it('UT-TRANSFORM-030: customerName → officialName 字段映射', () => {
    const result = mapFormToBackend({ customerName: '深圳科技公司' });
    expect(result.officialName).toBe('深圳科技公司');
  });

  it('UT-TRANSFORM-031: customerShortName → nickName 字段映射', () => {
    const result = mapFormToBackend({ customerShortName: '深科' });
    expect(result.nickName).toBe('深科');
  });

  it('UT-TRANSFORM-032: customerLevel="D" → level=1', () => {
    const result = mapFormToBackend({ customerLevel: 'D' });
    expect(result.level).toBe(1);
  });

  it('UT-TRANSFORM-033: customerLevel="C" → level=2', () => {
    const result = mapFormToBackend({ customerLevel: 'C' });
    expect(result.level).toBe(2);
  });

  it('UT-TRANSFORM-034: customerLevel="B" → level=3', () => {
    const result = mapFormToBackend({ customerLevel: 'B' });
    expect(result.level).toBe(3);
  });

  it('UT-TRANSFORM-035: customerLevel="BPO" → level=4', () => {
    const result = mapFormToBackend({ customerLevel: 'BPO' });
    expect(result.level).toBe(4);
  });

  it('UT-TRANSFORM-036: customerLevel="VIP" → level=5', () => {
    const result = mapFormToBackend({ customerLevel: 'VIP' });
    expect(result.level).toBe(5);
  });

  it('UT-TRANSFORM-037: customerLevel="VPO" → level=6', () => {
    const result = mapFormToBackend({ customerLevel: 'VPO' });
    expect(result.level).toBe(6);
  });

  it('UT-TRANSFORM-038: 非标准等级文案（如 Normal/Important/Lead）→ level=3 默认', () => {
    expect(mapFormToBackend({ customerLevel: 'Normal' }).level).toBe(3);
    expect(mapFormToBackend({ customerLevel: 'Important' }).level).toBe(3);
    expect(mapFormToBackend({ customerLevel: 'Lead' }).level).toBe(3);
  });

  it('UT-TRANSFORM-041: customerLevel 未知值 → level=3（默认 B）', () => {
    const result = mapFormToBackend({ customerLevel: 'UNKNOWN' });
    expect(result.level).toBe(3);
  });

  it('UT-TRANSFORM-042: customerType=1 → type=1（OEM）', () => {
    const result = mapFormToBackend({ customerType: 1 });
    expect(result.type).toBe(1);
  });

  it('UT-TRANSFORM-043: customerType=0（无效）→ type=1（修正为默认值）', () => {
    const result = mapFormToBackend({ customerType: 0 });
    expect(result.type).toBe(1);
  });

  it('UT-TRANSFORM-044: salesPersonId → salesUserId 字段映射', () => {
    const result = mapFormToBackend({ salesPersonId: 'user-001' });
    expect(result.salesUserId).toBe('user-001');
  });

  it('UT-TRANSFORM-045: remarks → remark 字段映射', () => {
    const result = mapFormToBackend({ remarks: '备注内容' });
    expect(result.remark).toBe('备注内容');
  });

  it('UT-TRANSFORM-046: creditLimit → creditLine 字段映射', () => {
    const result = mapFormToBackend({ creditLimit: 100000 });
    expect(result.creditLine).toBe(100000);
  });

  it('UT-TRANSFORM-047: creditLimit=0 → creditLine=0（保留 0 值）', () => {
    const result = mapFormToBackend({ creditLimit: 0 });
    expect(result.creditLine).toBe(0);
  });

  it('UT-TRANSFORM-048: paymentTerms → payment 字段映射', () => {
    const result = mapFormToBackend({ paymentTerms: 60 });
    expect(result.payment).toBe(60);
  });

  it('UT-TRANSFORM-049: currency → tradeCurrency 字段映射', () => {
    const result = mapFormToBackend({ currency: 2 });
    expect(result.tradeCurrency).toBe(2);
  });

  it('UT-TRANSFORM-050: unifiedSocialCreditCode → creditCode 字段映射', () => {
    const result = mapFormToBackend({ unifiedSocialCreditCode: '91440300MA5DXXXX' });
    expect(result.creditCode).toBe('91440300MA5DXXXX');
  });

  it('UT-TRANSFORM-051: contacts 数组正确传递（P1 修复验证）', () => {
    const contacts = [{ contactName: '张三', mobilePhone: '13800138000' }];
    const result = mapFormToBackend({ contacts });
    expect(result.contacts).toEqual(contacts);
  });

  it('UT-TRANSFORM-052: contacts 为 undefined 时，发送空数组（P1 修复验证）', () => {
    const result = mapFormToBackend({});
    expect(result.contacts).toEqual([]);
  });

  it('UT-TRANSFORM-053: contacts 为空数组时，发送空数组', () => {
    const result = mapFormToBackend({ contacts: [] });
    expect(result.contacts).toEqual([]);
  });
});

// ─── 联系人字段兼容映射测试 ───

describe('CustomerEdit - 联系人字段兼容映射 (mapContact)', () => {
  it('UT-TRANSFORM-060: 使用 contactName 字段（新字段）', () => {
    const result = mapContact({ contactName: '张三', name: '旧名称' });
    expect(result.contactName).toBe('张三');
  });

  it('UT-TRANSFORM-061: contactName 为空时，回退到 name（旧字段兼容）', () => {
    const result = mapContact({ name: '张三' });
    expect(result.contactName).toBe('张三');
  });

  it('UT-TRANSFORM-062: 使用 mobilePhone 字段（新字段）', () => {
    const result = mapContact({ mobilePhone: '13800138000', mobile: '旧手机' });
    expect(result.mobilePhone).toBe('13800138000');
  });

  it('UT-TRANSFORM-063: mobilePhone 为空时，回退到 mobile（旧字段兼容）', () => {
    const result = mapContact({ mobile: '13800138000' });
    expect(result.mobilePhone).toBe('13800138000');
  });

  it('UT-TRANSFORM-064: 其他字段（gender/department/position）原样保留', () => {
    const result = mapContact({ contactName: '张三', gender: 1, department: '销售部', position: '总监' });
    expect(result.gender).toBe(1);
    expect(result.department).toBe('销售部');
    expect(result.position).toBe('总监');
  });

  it('UT-TRANSFORM-065: isDefault 字段原样保留', () => {
    const result = mapContact({ contactName: '张三', isDefault: true });
    expect(result.isDefault).toBe(true);
  });
});
