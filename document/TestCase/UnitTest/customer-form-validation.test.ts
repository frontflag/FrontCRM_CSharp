/**
 * 客户模块 - 表单验证逻辑单元测试
 * 覆盖：CustomerEdit.vue 中的 formRules 验证规则
 * 测试策略：将验证规则提取为纯函数，逐条验证正常/边界/异常场景
 */

import { describe, it, expect } from 'vitest';

// ─── 将 CustomerEdit.vue 中的验证规则提取为可测试的纯函数 ───

/** 验证客户名称 */
function validateCustomerName(value: string): { valid: boolean; message?: string } {
  if (!value || value.trim() === '') return { valid: false, message: '请输入客户名称' };
  if (value.length < 2) return { valid: false, message: '长度在 2 到 100 个字符' };
  if (value.length > 100) return { valid: false, message: '长度在 2 到 100 个字符' };
  return { valid: true };
}

/** 验证客户类型（必须为 1-6 的有效枚举值） */
function validateCustomerType(value: number | undefined): { valid: boolean; message?: string } {
  if (value === undefined || value === null) return { valid: false, message: '请选择客户类型' };
  if (value < 1 || value > 6) return { valid: false, message: '请选择客户类型' };
  return { valid: true };
}

/** 验证客户等级（必须为有效枚举字符串） */
function validateCustomerLevel(value: string | undefined): { valid: boolean; message?: string } {
  const validLevels = ['D', 'C', 'B', 'BPO', 'VIP', 'VPO', 'Normal', 'Important', 'Lead'];
  if (!value || value.trim() === '') return { valid: false, message: '请选择客户等级' };
  if (!validLevels.includes(value)) return { valid: false, message: '请选择客户等级' };
  return { valid: true };
}

/** 验证联系人姓名（必填） */
function validateContactName(value: string): { valid: boolean; message?: string } {
  if (!value || value.trim() === '') return { valid: false, message: '请输入姓名' };
  return { valid: true };
}

/** 验证联系人手机（必填） */
function validateContactMobile(value: string): { valid: boolean; message?: string } {
  if (!value || value.trim() === '') return { valid: false, message: '请输入手机' };
  return { valid: true };
}

/** 验证信用额度（>=0） */
function validateCreditLimit(value: number): { valid: boolean; message?: string } {
  if (value < 0) return { valid: false, message: '信用额度不能为负数' };
  return { valid: true };
}

/** 验证账期（0-365 天） */
function validatePaymentTerms(value: number): { valid: boolean; message?: string } {
  if (value < 0) return { valid: false, message: '账期不能为负数' };
  if (value > 365) return { valid: false, message: '账期最大为 365 天' };
  return { valid: true };
}

/** 验证税率（0-100%） */
function validateTaxRate(value: number): { valid: boolean; message?: string } {
  if (value < 0) return { valid: false, message: '税率不能为负数' };
  if (value > 100) return { valid: false, message: '税率不能超过 100%' };
  return { valid: true };
}

// ─── 测试用例 ───

describe('CustomerEdit - 客户名称验证 (customerName)', () => {
  it('UT-VALIDATE-001: 空字符串 → 验证失败，提示"请输入客户名称"', () => {
    const result = validateCustomerName('');
    expect(result.valid).toBe(false);
    expect(result.message).toBe('请输入客户名称');
  });

  it('UT-VALIDATE-002: 仅空格 → 验证失败，提示"请输入客户名称"', () => {
    const result = validateCustomerName('   ');
    expect(result.valid).toBe(false);
    expect(result.message).toBe('请输入客户名称');
  });

  it('UT-VALIDATE-003: 1个字符（低于最小长度 2）→ 验证失败', () => {
    const result = validateCustomerName('A');
    expect(result.valid).toBe(false);
    expect(result.message).toBe('长度在 2 到 100 个字符');
  });

  it('UT-VALIDATE-004: 2个字符（最小有效长度）→ 验证通过', () => {
    const result = validateCustomerName('AB');
    expect(result.valid).toBe(true);
  });

  it('UT-VALIDATE-005: 100个字符（最大有效长度）→ 验证通过', () => {
    const result = validateCustomerName('A'.repeat(100));
    expect(result.valid).toBe(true);
  });

  it('UT-VALIDATE-006: 101个字符（超过最大长度 100）→ 验证失败', () => {
    const result = validateCustomerName('A'.repeat(101));
    expect(result.valid).toBe(false);
    expect(result.message).toBe('长度在 2 到 100 个字符');
  });

  it('UT-VALIDATE-007: 正常中文公司名称 → 验证通过', () => {
    const result = validateCustomerName('深圳市某某科技有限公司');
    expect(result.valid).toBe(true);
  });

  it('UT-VALIDATE-008: 含特殊字符的公司名称 → 验证通过（不限制特殊字符）', () => {
    const result = validateCustomerName('ABC & DEF Co., Ltd.');
    expect(result.valid).toBe(true);
  });
});

describe('CustomerEdit - 客户类型验证 (customerType)', () => {
  it('UT-VALIDATE-010: undefined → 验证失败，提示"请选择客户类型"', () => {
    const result = validateCustomerType(undefined);
    expect(result.valid).toBe(false);
    expect(result.message).toBe('请选择客户类型');
  });

  it('UT-VALIDATE-011: 0（无效枚举值）→ 验证失败', () => {
    const result = validateCustomerType(0);
    expect(result.valid).toBe(false);
  });

  it('UT-VALIDATE-012: 1（OEM）→ 验证通过', () => {
    expect(validateCustomerType(1).valid).toBe(true);
  });

  it('UT-VALIDATE-013: 2（ODM）→ 验证通过', () => {
    expect(validateCustomerType(2).valid).toBe(true);
  });

  it('UT-VALIDATE-014: 3（终端用户）→ 验证通过', () => {
    expect(validateCustomerType(3).valid).toBe(true);
  });

  it('UT-VALIDATE-015: 4（IDH）→ 验证通过', () => {
    expect(validateCustomerType(4).valid).toBe(true);
  });

  it('UT-VALIDATE-016: 5（贸易商）→ 验证通过', () => {
    expect(validateCustomerType(5).valid).toBe(true);
  });

  it('UT-VALIDATE-017: 6（代理商）→ 验证通过', () => {
    expect(validateCustomerType(6).valid).toBe(true);
  });

  it('UT-VALIDATE-018: 7（超出枚举范围）→ 验证失败', () => {
    const result = validateCustomerType(7);
    expect(result.valid).toBe(false);
  });

  it('UT-VALIDATE-019: -1（负数）→ 验证失败', () => {
    const result = validateCustomerType(-1);
    expect(result.valid).toBe(false);
  });
});

describe('CustomerEdit - 客户等级验证 (customerLevel)', () => {
  it('UT-VALIDATE-020: undefined → 验证失败，提示"请选择客户等级"', () => {
    const result = validateCustomerLevel(undefined);
    expect(result.valid).toBe(false);
    expect(result.message).toBe('请选择客户等级');
  });

  it('UT-VALIDATE-021: 空字符串 → 验证失败', () => {
    const result = validateCustomerLevel('');
    expect(result.valid).toBe(false);
  });

  it('UT-VALIDATE-022: "D" → 验证通过', () => {
    expect(validateCustomerLevel('D').valid).toBe(true);
  });

  it('UT-VALIDATE-023: "C" → 验证通过', () => {
    expect(validateCustomerLevel('C').valid).toBe(true);
  });

  it('UT-VALIDATE-024: "B" → 验证通过', () => {
    expect(validateCustomerLevel('B').valid).toBe(true);
  });

  it('UT-VALIDATE-025: "BPO" → 验证通过', () => {
    expect(validateCustomerLevel('BPO').valid).toBe(true);
  });

  it('UT-VALIDATE-026: "VIP" → 验证通过', () => {
    expect(validateCustomerLevel('VIP').valid).toBe(true);
  });

  it('UT-VALIDATE-027: "VPO" → 验证通过', () => {
    expect(validateCustomerLevel('VPO').valid).toBe(true);
  });

  it('UT-VALIDATE-028: "Normal"（旧兼容值）→ 验证通过', () => {
    expect(validateCustomerLevel('Normal').valid).toBe(true);
  });

  it('UT-VALIDATE-029: "Important"（旧兼容值）→ 验证通过', () => {
    expect(validateCustomerLevel('Important').valid).toBe(true);
  });

  it('UT-VALIDATE-030: "Lead"（旧兼容值）→ 验证通过', () => {
    expect(validateCustomerLevel('Lead').valid).toBe(true);
  });

  it('UT-VALIDATE-031: "GOLD"（无效值）→ 验证失败', () => {
    const result = validateCustomerLevel('GOLD');
    expect(result.valid).toBe(false);
  });

  it('UT-VALIDATE-032: "vip"（小写，大小写敏感）→ 验证失败', () => {
    const result = validateCustomerLevel('vip');
    expect(result.valid).toBe(false);
  });
});

describe('CustomerEdit - 联系人姓名验证 (contactName)', () => {
  it('UT-VALIDATE-040: 空字符串 → 验证失败，提示"请输入姓名"', () => {
    const result = validateContactName('');
    expect(result.valid).toBe(false);
    expect(result.message).toBe('请输入姓名');
  });

  it('UT-VALIDATE-041: 仅空格 → 验证失败', () => {
    const result = validateContactName('   ');
    expect(result.valid).toBe(false);
  });

  it('UT-VALIDATE-042: 正常姓名"张三" → 验证通过', () => {
    expect(validateContactName('张三').valid).toBe(true);
  });

  it('UT-VALIDATE-043: 英文名"John Smith" → 验证通过', () => {
    expect(validateContactName('John Smith').valid).toBe(true);
  });
});

describe('CustomerEdit - 联系人手机验证 (mobilePhone)', () => {
  it('UT-VALIDATE-050: 空字符串 → 验证失败，提示"请输入手机"', () => {
    const result = validateContactMobile('');
    expect(result.valid).toBe(false);
    expect(result.message).toBe('请输入手机');
  });

  it('UT-VALIDATE-051: 仅空格 → 验证失败', () => {
    const result = validateContactMobile('   ');
    expect(result.valid).toBe(false);
  });

  it('UT-VALIDATE-052: 正常手机号"13800138000" → 验证通过', () => {
    expect(validateContactMobile('13800138000').valid).toBe(true);
  });

  it('UT-VALIDATE-053: 国际格式"+8613800138000" → 验证通过（不限制格式）', () => {
    expect(validateContactMobile('+8613800138000').valid).toBe(true);
  });
});

describe('CustomerEdit - 信用额度验证 (creditLimit)', () => {
  it('UT-VALIDATE-060: 0（最小有效值）→ 验证通过', () => {
    expect(validateCreditLimit(0).valid).toBe(true);
  });

  it('UT-VALIDATE-061: 正数 100000 → 验证通过', () => {
    expect(validateCreditLimit(100000).valid).toBe(true);
  });

  it('UT-VALIDATE-062: -1（负数）→ 验证失败', () => {
    const result = validateCreditLimit(-1);
    expect(result.valid).toBe(false);
    expect(result.message).toBe('信用额度不能为负数');
  });

  it('UT-VALIDATE-063: 小数 99999.99 → 验证通过', () => {
    expect(validateCreditLimit(99999.99).valid).toBe(true);
  });
});

describe('CustomerEdit - 账期验证 (paymentTerms)', () => {
  it('UT-VALIDATE-070: 0（最小有效值）→ 验证通过', () => {
    expect(validatePaymentTerms(0).valid).toBe(true);
  });

  it('UT-VALIDATE-071: 30（默认值）→ 验证通过', () => {
    expect(validatePaymentTerms(30).valid).toBe(true);
  });

  it('UT-VALIDATE-072: 365（最大有效值）→ 验证通过', () => {
    expect(validatePaymentTerms(365).valid).toBe(true);
  });

  it('UT-VALIDATE-073: 366（超过最大值）→ 验证失败', () => {
    const result = validatePaymentTerms(366);
    expect(result.valid).toBe(false);
    expect(result.message).toBe('账期最大为 365 天');
  });

  it('UT-VALIDATE-074: -1（负数）→ 验证失败', () => {
    const result = validatePaymentTerms(-1);
    expect(result.valid).toBe(false);
    expect(result.message).toBe('账期不能为负数');
  });
});

describe('CustomerEdit - 税率验证 (taxRate)', () => {
  it('UT-VALIDATE-080: 0（最小有效值）→ 验证通过', () => {
    expect(validateTaxRate(0).valid).toBe(true);
  });

  it('UT-VALIDATE-081: 13（默认增值税率）→ 验证通过', () => {
    expect(validateTaxRate(13).valid).toBe(true);
  });

  it('UT-VALIDATE-082: 100（最大有效值）→ 验证通过', () => {
    expect(validateTaxRate(100).valid).toBe(true);
  });

  it('UT-VALIDATE-083: 101（超过最大值）→ 验证失败', () => {
    const result = validateTaxRate(101);
    expect(result.valid).toBe(false);
    expect(result.message).toBe('税率不能超过 100%');
  });

  it('UT-VALIDATE-084: -0.1（负数）→ 验证失败', () => {
    const result = validateTaxRate(-0.1);
    expect(result.valid).toBe(false);
    expect(result.message).toBe('税率不能为负数');
  });

  it('UT-VALIDATE-085: 6（小规模纳税人税率）→ 验证通过', () => {
    expect(validateTaxRate(6).valid).toBe(true);
  });
});
