/**
 * 客户模块接口测试 - 子资料 API（联系人 / 地址 / 银行账户）
 * 覆盖范围（依据 PRD 第四章 4.3 / 4.4 / 4.5）：
 *
 * 联系人 API（customerContactApi）:
 *   - getContactsByCustomerId  GET  /api/v1/customers/:customerId/contacts
 *   - createContact            POST /api/v1/customers/:customerId/contacts
 *   - updateContact            PUT  /api/v1/contacts/:contactId
 *   - deleteContact            DELETE /api/v1/contacts/:contactId
 *   - setDefaultContact        POST /api/v1/contacts/:contactId/set-default
 *
 * 地址 API（customerAddressApi）:
 *   - getAddressesByCustomerId GET  /api/v1/customers/:customerId/addresses
 *   - createAddress            POST /api/v1/customers/:customerId/addresses
 *   - updateAddress            PUT  /api/v1/addresses/:addressId
 *   - deleteAddress            DELETE /api/v1/addresses/:addressId
 *   - setDefaultAddress        POST /api/v1/addresses/:addressId/set-default
 *
 * 银行账户 API（customerBankApi）:
 *   - getBanksByCustomerId     GET  /api/v1/customers/:customerId/banks
 *   - createBank               POST /api/v1/customers/:customerId/banks
 *   - updateBank               PUT  /api/v1/banks/:bankId
 *   - deleteBank               DELETE /api/v1/banks/:bankId
 *   - setDefaultBank           POST /api/v1/banks/:bankId/set-default
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'

vi.mock('@/api/client', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn()
  },
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn()
  }
}))

import apiClient from '@/api/client'
import { customerContactApi, customerAddressApi, customerBankApi } from '@/api/customer'

const mockGet = apiClient.get as ReturnType<typeof vi.fn>
const mockPost = apiClient.post as ReturnType<typeof vi.fn>
const mockPut = apiClient.put as ReturnType<typeof vi.fn>
const mockDelete = apiClient.delete as ReturnType<typeof vi.fn>

// ─── 测试数据工厂 ─────────────────────────────────────────────────────────────
const makeContact = (overrides = {}): any => ({
  id: 'contact-001',
  contactName: '张三',
  gender: 0,
  department: '采购部',
  position: '采购经理',
  mobilePhone: '13800138000',
  phone: '0755-12345678',
  email: 'zhangsan@test.com',
  fax: '0755-87654321',
  socialAccount: 'wechat:zhangsan',
  isDefault: true,
  isDecisionMaker: true,
  remarks: '主要决策人',
  ...overrides
})

const makeAddress = (overrides = {}): any => ({
  id: 'addr-001',
  addressType: 'Office',
  country: '中国',
  province: '广东省',
  city: '深圳市',
  district: '南山区',
  streetAddress: '科技园南区深南大道10000号',
  zipCode: '518057',
  contactPerson: '李四',
  contactPhone: '13900139000',
  isDefault: true,
  ...overrides
})

const makeBank = (overrides = {}): any => ({
  id: 'bank-001',
  accountName: '深圳测试科技有限公司',
  bankName: '中国工商银行',
  bankBranch: '深圳南山支行',
  accountNumber: '6222021234567890123',
  currency: 1,
  swiftCode: 'ICBKCNBJ',
  isDefault: true,
  ...overrides
})

beforeEach(() => {
  vi.clearAllMocks()
})

// ═══════════════════════════════════════════════════════════════════════════════
// 一、联系人 API（customerContactApi）
// ═══════════════════════════════════════════════════════════════════════════════

describe('customerContactApi - 联系人接口', () => {

  // ─── getContactsByCustomerId ───────────────────────────────────────────────
  describe('getContactsByCustomerId - 获取联系人列表', () => {

    it('TC-CONTACT-001: 调用正确的 GET URL', async () => {
      mockGet.mockResolvedValue([])
      await customerContactApi.getContactsByCustomerId('cust-001')
      expect(mockGet.mock.calls[0][0]).toBe('/api/v1/customers/cust-001/contacts')
    })

    it('TC-CONTACT-002: 返回联系人数组', async () => {
      mockGet.mockResolvedValue([makeContact()])
      const result = await customerContactApi.getContactsByCustomerId('cust-001')
      expect(result).toHaveLength(1)
      expect(result[0].contactName).toBe('张三')
    })

    it('TC-CONTACT-003: 返回多个联系人', async () => {
      mockGet.mockResolvedValue([
        makeContact({ id: 'c-001', contactName: '张三', isDefault: true }),
        makeContact({ id: 'c-002', contactName: '李四', isDefault: false }),
        makeContact({ id: 'c-003', contactName: '王五', isDefault: false })
      ])
      const result = await customerContactApi.getContactsByCustomerId('cust-001')
      expect(result).toHaveLength(3)
    })

    it('TC-CONTACT-004: 无联系人时，返回空数组', async () => {
      mockGet.mockResolvedValue([])
      const result = await customerContactApi.getContactsByCustomerId('cust-new')
      expect(result).toEqual([])
    })

    it('TC-CONTACT-005: 返回的联系人包含所有必要字段', async () => {
      mockGet.mockResolvedValue([makeContact()])
      const result = await customerContactApi.getContactsByCustomerId('cust-001')
      const contact = result[0]
      expect(contact).toHaveProperty('id')
      expect(contact).toHaveProperty('contactName')
      expect(contact).toHaveProperty('mobilePhone')
      expect(contact).toHaveProperty('isDefault')
      expect(contact).toHaveProperty('isDecisionMaker')
    })

    it('TC-CONTACT-006: 客户不存在时（404），抛出异常', async () => {
      mockGet.mockRejectedValue({ response: { status: 404 } })
      await expect(customerContactApi.getContactsByCustomerId('non-existent'))
        .rejects.toBeDefined()
    })
  })

  // ─── createContact ─────────────────────────────────────────────────────────
  describe('createContact - 创建联系人', () => {

    it('TC-CONTACT-007: 调用正确的 POST URL', async () => {
      mockPost.mockResolvedValue(makeContact())
      await customerContactApi.createContact('cust-001', { contactName: '张三', gender: 0, mobilePhone: '13800138000', isDefault: true, isDecisionMaker: false })
      expect(mockPost.mock.calls[0][0]).toBe('/api/v1/customers/cust-001/contacts')
    })

    it('TC-CONTACT-008: 请求体包含必填字段（contactName, mobilePhone）', async () => {
      mockPost.mockResolvedValue(makeContact())
      await customerContactApi.createContact('cust-001', { contactName: '张三', gender: 0, mobilePhone: '13800138000', isDefault: true, isDecisionMaker: false })
      const body = mockPost.mock.calls[0][1]
      expect(body.contactName).toBe('张三')
      expect(body.mobilePhone).toBe('13800138000')
    })

    it('TC-CONTACT-009: 请求体包含可选字段（department, position, email）', async () => {
      mockPost.mockResolvedValue(makeContact())
      await customerContactApi.createContact('cust-001', {
        contactName: '张三', gender: 0, mobilePhone: '13800138000',
        department: '采购部', position: '经理', email: 'test@test.com',
        isDefault: true, isDecisionMaker: true
      })
      const body = mockPost.mock.calls[0][1]
      expect(body.department).toBe('采购部')
      expect(body.position).toBe('经理')
      expect(body.email).toBe('test@test.com')
    })

    it('TC-CONTACT-010: isDefault=true 正确传递', async () => {
      mockPost.mockResolvedValue(makeContact({ isDefault: true }))
      await customerContactApi.createContact('cust-001', { contactName: '张三', gender: 0, mobilePhone: '13800138000', isDefault: true, isDecisionMaker: false })
      expect(mockPost.mock.calls[0][1].isDefault).toBe(true)
    })

    it('TC-CONTACT-011: isDecisionMaker=true 正确传递', async () => {
      mockPost.mockResolvedValue(makeContact({ isDecisionMaker: true }))
      await customerContactApi.createContact('cust-001', { contactName: '张三', gender: 0, mobilePhone: '13800138000', isDefault: true, isDecisionMaker: true })
      expect(mockPost.mock.calls[0][1].isDecisionMaker).toBe(true)
    })

    it('TC-CONTACT-012: 创建成功，返回包含 id 的联系人对象', async () => {
      mockPost.mockResolvedValue(makeContact({ id: 'new-contact-001' }))
      const result = await customerContactApi.createContact('cust-001', { contactName: '张三', gender: 0, mobilePhone: '13800138000', isDefault: true, isDecisionMaker: false })
      expect(result.id).toBe('new-contact-001')
    })

    it('TC-CONTACT-013: 性别枚举：0=男，1=女，2=保密，正确传递', async () => {
      mockPost.mockResolvedValue(makeContact({ gender: 1 }))
      await customerContactApi.createContact('cust-001', { contactName: '李四', gender: 1, mobilePhone: '13900139000', isDefault: false, isDecisionMaker: false })
      expect(mockPost.mock.calls[0][1].gender).toBe(1)
    })

    it('TC-CONTACT-014: 服务器 400 错误（手机号格式错误），抛出异常', async () => {
      mockPost.mockRejectedValue({ response: { status: 400, data: { message: '手机号格式不正确' } } })
      await expect(customerContactApi.createContact('cust-001', { contactName: '张三', gender: 0, mobilePhone: 'invalid', isDefault: false, isDecisionMaker: false }))
        .rejects.toBeDefined()
    })
  })

  // ─── updateContact ─────────────────────────────────────────────────────────
  describe('updateContact - 更新联系人', () => {

    it('TC-CONTACT-015: 调用正确的 PUT URL（使用 contactId）', async () => {
      mockPut.mockResolvedValue(makeContact())
      await customerContactApi.updateContact('contact-001', { contactName: '张三（更新）' })
      expect(mockPut.mock.calls[0][0]).toBe('/api/v1/contacts/contact-001')
    })

    it('TC-CONTACT-016: 请求体包含更新字段', async () => {
      mockPut.mockResolvedValue(makeContact({ position: '总监' }))
      await customerContactApi.updateContact('contact-001', { position: '总监' })
      expect(mockPut.mock.calls[0][1].position).toBe('总监')
    })

    it('TC-CONTACT-017: 更新成功，返回更新后的联系人对象', async () => {
      mockPut.mockResolvedValue(makeContact({ contactName: '张三（已更新）' }))
      const result = await customerContactApi.updateContact('contact-001', { contactName: '张三（已更新）' })
      expect(result.contactName).toBe('张三（已更新）')
    })

    it('TC-CONTACT-018: 联系人不存在时（404），抛出异常', async () => {
      mockPut.mockRejectedValue({ response: { status: 404 } })
      await expect(customerContactApi.updateContact('non-existent', { contactName: '测试' }))
        .rejects.toBeDefined()
    })
  })

  // ─── deleteContact ─────────────────────────────────────────────────────────
  describe('deleteContact - 删除联系人', () => {

    it('TC-CONTACT-019: 调用正确的 DELETE URL', async () => {
      mockDelete.mockResolvedValue(undefined)
      await customerContactApi.deleteContact('contact-001')
      expect(mockDelete.mock.calls[0][0]).toBe('/api/v1/contacts/contact-001')
    })

    it('TC-CONTACT-020: 删除成功，返回 void', async () => {
      mockDelete.mockResolvedValue(undefined)
      const result = await customerContactApi.deleteContact('contact-001')
      expect(result).toBeUndefined()
    })

    it('TC-CONTACT-021: 联系人不存在时（404），抛出异常', async () => {
      mockDelete.mockRejectedValue({ response: { status: 404 } })
      await expect(customerContactApi.deleteContact('non-existent'))
        .rejects.toBeDefined()
    })
  })

  // ─── setDefaultContact ─────────────────────────────────────────────────────
  describe('setDefaultContact - 设为默认联系人', () => {

    it('TC-CONTACT-022: 调用正确的 POST URL', async () => {
      mockPost.mockResolvedValue(undefined)
      await customerContactApi.setDefaultContact('contact-001')
      expect(mockPost.mock.calls[0][0]).toBe('/api/v1/contacts/contact-001/set-default')
    })

    it('TC-CONTACT-023: 请求体为空（无需传参）', async () => {
      mockPost.mockResolvedValue(undefined)
      await customerContactApi.setDefaultContact('contact-001')
      expect(mockPost.mock.calls[0][1]).toBeUndefined()
    })

    it('TC-CONTACT-024: 设置成功，返回 void', async () => {
      mockPost.mockResolvedValue(undefined)
      const result = await customerContactApi.setDefaultContact('contact-001')
      expect(result).toBeUndefined()
    })

    it('TC-CONTACT-025: 联系人不存在时（404），抛出异常', async () => {
      mockPost.mockRejectedValue({ response: { status: 404 } })
      await expect(customerContactApi.setDefaultContact('non-existent'))
        .rejects.toBeDefined()
    })
  })
})

// ═══════════════════════════════════════════════════════════════════════════════
// 二、地址 API（customerAddressApi）
// ═══════════════════════════════════════════════════════════════════════════════

describe('customerAddressApi - 地址接口', () => {

  // ─── getAddressesByCustomerId ───────────────────────────────────────────────
  describe('getAddressesByCustomerId - 获取地址列表', () => {

    it('TC-ADDR-001: 调用正确的 GET URL', async () => {
      mockGet.mockResolvedValue([])
      await customerAddressApi.getAddressesByCustomerId('cust-001')
      expect(mockGet.mock.calls[0][0]).toBe('/api/v1/customers/cust-001/addresses')
    })

    it('TC-ADDR-002: 返回地址数组', async () => {
      mockGet.mockResolvedValue([makeAddress()])
      const result = await customerAddressApi.getAddressesByCustomerId('cust-001')
      expect(result).toHaveLength(1)
      expect(result[0].streetAddress).toBe('科技园南区深南大道10000号')
    })

    it('TC-ADDR-003: 返回多种类型的地址（Office/Billing/Shipping/Registered）', async () => {
      mockGet.mockResolvedValue([
        makeAddress({ id: 'a-001', addressType: 'Office', isDefault: true }),
        makeAddress({ id: 'a-002', addressType: 'Billing', isDefault: false }),
        makeAddress({ id: 'a-003', addressType: 'Shipping', isDefault: false }),
        makeAddress({ id: 'a-004', addressType: 'Registered', isDefault: false })
      ])
      const result = await customerAddressApi.getAddressesByCustomerId('cust-001')
      expect(result).toHaveLength(4)
      const types = result.map(a => a.addressType)
      expect(types).toContain('Office')
      expect(types).toContain('Billing')
      expect(types).toContain('Shipping')
      expect(types).toContain('Registered')
    })

    it('TC-ADDR-004: 无地址时，返回空数组', async () => {
      mockGet.mockResolvedValue([])
      const result = await customerAddressApi.getAddressesByCustomerId('cust-new')
      expect(result).toEqual([])
    })
  })

  // ─── createAddress ─────────────────────────────────────────────────────────
  describe('createAddress - 创建地址', () => {

    it('TC-ADDR-005: 调用正确的 POST URL', async () => {
      mockPost.mockResolvedValue(makeAddress())
      await customerAddressApi.createAddress('cust-001', {
        addressType: 'Office', streetAddress: '深南大道10000号', isDefault: true
      } as any)
      expect(mockPost.mock.calls[0][0]).toBe('/api/v1/customers/cust-001/addresses')
    })

    it('TC-ADDR-006: 请求体包含必填字段（addressType, streetAddress）', async () => {
      mockPost.mockResolvedValue(makeAddress())
      await customerAddressApi.createAddress('cust-001', {
        addressType: 'Billing', streetAddress: '福田区中心三路8号', isDefault: false
      } as any)
      const body = mockPost.mock.calls[0][1]
      expect(body.addressType).toBe('Billing')
      expect(body.streetAddress).toBe('福田区中心三路8号')
    })

    it('TC-ADDR-007: 请求体包含省市区字段', async () => {
      mockPost.mockResolvedValue(makeAddress())
      await customerAddressApi.createAddress('cust-001', {
        addressType: 'Shipping', province: '广东省', city: '深圳市', district: '福田区',
        streetAddress: '中心三路8号', isDefault: false
      } as any)
      const body = mockPost.mock.calls[0][1]
      expect(body.province).toBe('广东省')
      expect(body.city).toBe('深圳市')
      expect(body.district).toBe('福田区')
    })

    it('TC-ADDR-008: 请求体包含联系人和联系电话', async () => {
      mockPost.mockResolvedValue(makeAddress())
      await customerAddressApi.createAddress('cust-001', {
        addressType: 'Shipping', streetAddress: '测试地址',
        contactPerson: '收货人', contactPhone: '13800138000', isDefault: false
      } as any)
      const body = mockPost.mock.calls[0][1]
      expect(body.contactPerson).toBe('收货人')
      expect(body.contactPhone).toBe('13800138000')
    })

    it('TC-ADDR-009: 请求体包含邮编字段', async () => {
      mockPost.mockResolvedValue(makeAddress())
      await customerAddressApi.createAddress('cust-001', {
        addressType: 'Office', streetAddress: '测试地址', zipCode: '518057', isDefault: true
      } as any)
      expect(mockPost.mock.calls[0][1].zipCode).toBe('518057')
    })

    it('TC-ADDR-010: 创建成功，返回包含 id 的地址对象', async () => {
      mockPost.mockResolvedValue(makeAddress({ id: 'new-addr-001' }))
      const result = await customerAddressApi.createAddress('cust-001', {
        addressType: 'Office', streetAddress: '测试地址', isDefault: true
      } as any)
      expect(result.id).toBe('new-addr-001')
    })

    it('TC-ADDR-011: 地址类型为 Registered（注册地址），正确传递', async () => {
      mockPost.mockResolvedValue(makeAddress({ addressType: 'Registered' }))
      await customerAddressApi.createAddress('cust-001', {
        addressType: 'Registered', streetAddress: '注册地址', isDefault: false
      } as any)
      expect(mockPost.mock.calls[0][1].addressType).toBe('Registered')
    })
  })

  // ─── updateAddress ─────────────────────────────────────────────────────────
  describe('updateAddress - 更新地址', () => {

    it('TC-ADDR-012: 调用正确的 PUT URL（使用 addressId）', async () => {
      mockPut.mockResolvedValue(makeAddress())
      await customerAddressApi.updateAddress('addr-001', { streetAddress: '新地址' })
      expect(mockPut.mock.calls[0][0]).toBe('/api/v1/addresses/addr-001')
    })

    it('TC-ADDR-013: 请求体包含更新字段', async () => {
      mockPut.mockResolvedValue(makeAddress({ streetAddress: '更新后地址' }))
      await customerAddressApi.updateAddress('addr-001', { streetAddress: '更新后地址', zipCode: '518000' })
      const body = mockPut.mock.calls[0][1]
      expect(body.streetAddress).toBe('更新后地址')
      expect(body.zipCode).toBe('518000')
    })

    it('TC-ADDR-014: 更新成功，返回更新后的地址对象', async () => {
      mockPut.mockResolvedValue(makeAddress({ streetAddress: '已更新地址' }))
      const result = await customerAddressApi.updateAddress('addr-001', { streetAddress: '已更新地址' })
      expect(result.streetAddress).toBe('已更新地址')
    })

    it('TC-ADDR-015: 地址不存在时（404），抛出异常', async () => {
      mockPut.mockRejectedValue({ response: { status: 404 } })
      await expect(customerAddressApi.updateAddress('non-existent', { streetAddress: '测试' }))
        .rejects.toBeDefined()
    })
  })

  // ─── deleteAddress ─────────────────────────────────────────────────────────
  describe('deleteAddress - 删除地址', () => {

    it('TC-ADDR-016: 调用正确的 DELETE URL', async () => {
      mockDelete.mockResolvedValue(undefined)
      await customerAddressApi.deleteAddress('addr-001')
      expect(mockDelete.mock.calls[0][0]).toBe('/api/v1/addresses/addr-001')
    })

    it('TC-ADDR-017: 删除成功，返回 void', async () => {
      mockDelete.mockResolvedValue(undefined)
      const result = await customerAddressApi.deleteAddress('addr-001')
      expect(result).toBeUndefined()
    })

    it('TC-ADDR-018: 地址不存在时（404），抛出异常', async () => {
      mockDelete.mockRejectedValue({ response: { status: 404 } })
      await expect(customerAddressApi.deleteAddress('non-existent'))
        .rejects.toBeDefined()
    })
  })

  // ─── setDefaultAddress ─────────────────────────────────────────────────────
  describe('setDefaultAddress - 设为默认地址', () => {

    it('TC-ADDR-019: 调用正确的 POST URL', async () => {
      mockPost.mockResolvedValue(undefined)
      await customerAddressApi.setDefaultAddress('addr-001')
      expect(mockPost.mock.calls[0][0]).toBe('/api/v1/addresses/addr-001/set-default')
    })

    it('TC-ADDR-020: 请求体为空（无需传参）', async () => {
      mockPost.mockResolvedValue(undefined)
      await customerAddressApi.setDefaultAddress('addr-001')
      expect(mockPost.mock.calls[0][1]).toBeUndefined()
    })

    it('TC-ADDR-021: 设置成功，返回 void', async () => {
      mockPost.mockResolvedValue(undefined)
      const result = await customerAddressApi.setDefaultAddress('addr-001')
      expect(result).toBeUndefined()
    })

    it('TC-ADDR-022: 地址不存在时（404），抛出异常', async () => {
      mockPost.mockRejectedValue({ response: { status: 404 } })
      await expect(customerAddressApi.setDefaultAddress('non-existent'))
        .rejects.toBeDefined()
    })
  })
})

// ═══════════════════════════════════════════════════════════════════════════════
// 三、银行账户 API（customerBankApi）
// ═══════════════════════════════════════════════════════════════════════════════

describe('customerBankApi - 银行账户接口', () => {

  // ─── getBanksByCustomerId ───────────────────────────────────────────────────
  describe('getBanksByCustomerId - 获取银行账户列表', () => {

    it('TC-BANK-001: 调用正确的 GET URL', async () => {
      mockGet.mockResolvedValue([])
      await customerBankApi.getBanksByCustomerId('cust-001')
      expect(mockGet.mock.calls[0][0]).toBe('/api/v1/customers/cust-001/banks')
    })

    it('TC-BANK-002: 返回银行账户数组', async () => {
      mockGet.mockResolvedValue([makeBank()])
      const result = await customerBankApi.getBanksByCustomerId('cust-001')
      expect(result).toHaveLength(1)
      expect(result[0].bankName).toBe('中国工商银行')
    })

    it('TC-BANK-003: 返回多个银行账户（不同币种）', async () => {
      mockGet.mockResolvedValue([
        makeBank({ id: 'b-001', currency: 1, isDefault: true }),   // CNY
        makeBank({ id: 'b-002', currency: 2, isDefault: false }),  // USD
        makeBank({ id: 'b-003', currency: 3, isDefault: false })   // EUR
      ])
      const result = await customerBankApi.getBanksByCustomerId('cust-001')
      expect(result).toHaveLength(3)
      const currencies = result.map(b => b.currency)
      expect(currencies).toContain(1)
      expect(currencies).toContain(2)
      expect(currencies).toContain(3)
    })

    it('TC-BANK-004: 无银行账户时，返回空数组', async () => {
      mockGet.mockResolvedValue([])
      const result = await customerBankApi.getBanksByCustomerId('cust-new')
      expect(result).toEqual([])
    })

    it('TC-BANK-005: 返回的银行账户包含所有必要字段', async () => {
      mockGet.mockResolvedValue([makeBank()])
      const result = await customerBankApi.getBanksByCustomerId('cust-001')
      const bank = result[0]
      expect(bank).toHaveProperty('id')
      expect(bank).toHaveProperty('accountName')
      expect(bank).toHaveProperty('bankName')
      expect(bank).toHaveProperty('bankBranch')
      expect(bank).toHaveProperty('accountNumber')
      expect(bank).toHaveProperty('currency')
      expect(bank).toHaveProperty('isDefault')
    })
  })

  // ─── createBank ────────────────────────────────────────────────────────────
  describe('createBank - 创建银行账户', () => {

    it('TC-BANK-006: 调用正确的 POST URL', async () => {
      mockPost.mockResolvedValue(makeBank())
      await customerBankApi.createBank('cust-001', {
        accountName: '深圳测试科技有限公司', bankName: '工商银行',
        bankBranch: '南山支行', accountNumber: '6222021234567890123',
        currency: 1, isDefault: true
      })
      expect(mockPost.mock.calls[0][0]).toBe('/api/v1/customers/cust-001/banks')
    })

    it('TC-BANK-007: 请求体包含必填字段（accountName, bankName, bankBranch, accountNumber）', async () => {
      mockPost.mockResolvedValue(makeBank())
      await customerBankApi.createBank('cust-001', {
        accountName: '公司名称', bankName: '建设银行',
        bankBranch: '福田支行', accountNumber: '6227001234567890',
        currency: 1, isDefault: false
      })
      const body = mockPost.mock.calls[0][1]
      expect(body.accountName).toBe('公司名称')
      expect(body.bankName).toBe('建设银行')
      expect(body.bankBranch).toBe('福田支行')
      expect(body.accountNumber).toBe('6227001234567890')
    })

    it('TC-BANK-008: 币种枚举 CNY=1 正确传递', async () => {
      mockPost.mockResolvedValue(makeBank({ currency: 1 }))
      await customerBankApi.createBank('cust-001', {
        accountName: '公司', bankName: '工行', bankBranch: '支行',
        accountNumber: '123456', currency: 1, isDefault: true
      })
      expect(mockPost.mock.calls[0][1].currency).toBe(1)
    })

    it('TC-BANK-009: 币种枚举 USD=2 正确传递', async () => {
      mockPost.mockResolvedValue(makeBank({ currency: 2 }))
      await customerBankApi.createBank('cust-001', {
        accountName: '公司', bankName: '工行', bankBranch: '支行',
        accountNumber: '123456', currency: 2, isDefault: false
      })
      expect(mockPost.mock.calls[0][1].currency).toBe(2)
    })

    it('TC-BANK-010: 币种枚举 EUR=3 正确传递', async () => {
      mockPost.mockResolvedValue(makeBank({ currency: 3 }))
      await customerBankApi.createBank('cust-001', {
        accountName: '公司', bankName: '工行', bankBranch: '支行',
        accountNumber: '123456', currency: 3, isDefault: false
      })
      expect(mockPost.mock.calls[0][1].currency).toBe(3)
    })

    it('TC-BANK-011: 包含可选字段 swiftCode', async () => {
      mockPost.mockResolvedValue(makeBank())
      await customerBankApi.createBank('cust-001', {
        accountName: '公司', bankName: '工行', bankBranch: '支行',
        accountNumber: '123456', currency: 2, swiftCode: 'ICBKCNBJ', isDefault: false
      })
      expect(mockPost.mock.calls[0][1].swiftCode).toBe('ICBKCNBJ')
    })

    it('TC-BANK-012: 创建成功，返回包含 id 的银行账户对象', async () => {
      mockPost.mockResolvedValue(makeBank({ id: 'new-bank-001' }))
      const result = await customerBankApi.createBank('cust-001', {
        accountName: '公司', bankName: '工行', bankBranch: '支行',
        accountNumber: '123456', currency: 1, isDefault: true
      })
      expect(result.id).toBe('new-bank-001')
    })

    it('TC-BANK-013: 服务器 400 错误（账号格式错误），抛出异常', async () => {
      mockPost.mockRejectedValue({ response: { status: 400, data: { message: '银行账号格式不正确' } } })
      await expect(customerBankApi.createBank('cust-001', {
        accountName: '公司', bankName: '工行', bankBranch: '支行',
        accountNumber: 'invalid', currency: 1, isDefault: true
      })).rejects.toBeDefined()
    })
  })

  // ─── updateBank ────────────────────────────────────────────────────────────
  describe('updateBank - 更新银行账户', () => {

    it('TC-BANK-014: 调用正确的 PUT URL（使用 bankId）', async () => {
      mockPut.mockResolvedValue(makeBank())
      await customerBankApi.updateBank('bank-001', { bankBranch: '新支行' })
      expect(mockPut.mock.calls[0][0]).toBe('/api/v1/banks/bank-001')
    })

    it('TC-BANK-015: 请求体包含更新字段', async () => {
      mockPut.mockResolvedValue(makeBank({ bankBranch: '新支行名称' }))
      await customerBankApi.updateBank('bank-001', { bankBranch: '新支行名称', swiftCode: 'NEWSWIFT' })
      const body = mockPut.mock.calls[0][1]
      expect(body.bankBranch).toBe('新支行名称')
      expect(body.swiftCode).toBe('NEWSWIFT')
    })

    it('TC-BANK-016: 更新成功，返回更新后的银行账户对象', async () => {
      mockPut.mockResolvedValue(makeBank({ bankBranch: '已更新支行' }))
      const result = await customerBankApi.updateBank('bank-001', { bankBranch: '已更新支行' })
      expect(result.bankBranch).toBe('已更新支行')
    })

    it('TC-BANK-017: 银行账户不存在时（404），抛出异常', async () => {
      mockPut.mockRejectedValue({ response: { status: 404 } })
      await expect(customerBankApi.updateBank('non-existent', { bankBranch: '测试' }))
        .rejects.toBeDefined()
    })
  })

  // ─── deleteBank ────────────────────────────────────────────────────────────
  describe('deleteBank - 删除银行账户', () => {

    it('TC-BANK-018: 调用正确的 DELETE URL', async () => {
      mockDelete.mockResolvedValue(undefined)
      await customerBankApi.deleteBank('bank-001')
      expect(mockDelete.mock.calls[0][0]).toBe('/api/v1/banks/bank-001')
    })

    it('TC-BANK-019: 删除成功，返回 void', async () => {
      mockDelete.mockResolvedValue(undefined)
      const result = await customerBankApi.deleteBank('bank-001')
      expect(result).toBeUndefined()
    })

    it('TC-BANK-020: 银行账户不存在时（404），抛出异常', async () => {
      mockDelete.mockRejectedValue({ response: { status: 404 } })
      await expect(customerBankApi.deleteBank('non-existent'))
        .rejects.toBeDefined()
    })
  })

  // ─── setDefaultBank ────────────────────────────────────────────────────────
  describe('setDefaultBank - 设为默认银行账户', () => {

    it('TC-BANK-021: 调用正确的 POST URL', async () => {
      mockPost.mockResolvedValue(undefined)
      await customerBankApi.setDefaultBank('bank-001')
      expect(mockPost.mock.calls[0][0]).toBe('/api/v1/banks/bank-001/set-default')
    })

    it('TC-BANK-022: 请求体为空（无需传参）', async () => {
      mockPost.mockResolvedValue(undefined)
      await customerBankApi.setDefaultBank('bank-001')
      expect(mockPost.mock.calls[0][1]).toBeUndefined()
    })

    it('TC-BANK-023: 设置成功，返回 void', async () => {
      mockPost.mockResolvedValue(undefined)
      const result = await customerBankApi.setDefaultBank('bank-001')
      expect(result).toBeUndefined()
    })

    it('TC-BANK-024: 银行账户不存在时（404），抛出异常', async () => {
      mockPost.mockRejectedValue({ response: { status: 404 } })
      await expect(customerBankApi.setDefaultBank('non-existent'))
        .rejects.toBeDefined()
    })

    it('TC-BANK-025: 已是默认账户再次设置，不抛出异常（幂等）', async () => {
      mockPost.mockResolvedValue(undefined)
      await expect(customerBankApi.setDefaultBank('already-default'))
        .resolves.toBeUndefined()
    })
  })
})
