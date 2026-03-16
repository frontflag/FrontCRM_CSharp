/**
 * 客户模块接口测试 - 客户主体 API
 * 覆盖范围（依据 PRD 第四章）：
 *   - searchCustomers   GET  /api/v1/customers
 *   - getCustomerById   GET  /api/v1/customers/:id
 *   - createCustomer    POST /api/v1/customers
 *   - updateCustomer    PUT  /api/v1/customers/:id
 *   - deleteCustomer    DELETE /api/v1/customers/:id
 *   - activateCustomer  POST /api/v1/customers/:id/activate
 *   - deactivateCustomer POST /api/v1/customers/:id/deactivate
 *   - getCustomerStatistics GET /api/v1/customers/statistics
 *   - getCustomerContactHistory GET /api/v1/customers/:id/contact-history
 *   - addContactHistory POST /api/v1/customers/:id/contact-history
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
import { customerApi } from '@/api/customer'

const mockGet = apiClient.get as ReturnType<typeof vi.fn>
const mockPost = apiClient.post as ReturnType<typeof vi.fn>
const mockPut = apiClient.put as ReturnType<typeof vi.fn>
const mockDelete = apiClient.delete as ReturnType<typeof vi.fn>

// ─── 测试数据工厂 ─────────────────────────────────────────────────────────────
const makeCustomer = (overrides = {}): any => ({
  id: 'cust-001',
  customerCode: 'C-2024-001',
  customerName: '深圳测试科技有限公司',
  customerShortName: '测试科技',
  customerType: 1,
  customerLevel: 'VIP',
  industry: 'Technology',
  region: '广东省深圳市南山区',
  province: '广东省',
  city: '深圳市',
  district: '南山区',
  country: '中国',
  unifiedSocialCreditCode: '91440300MA5FXXXXXX',
  salesPersonId: 'user-001',
  salesPersonName: '张三',
  creditLimit: 500000,
  paymentTerms: 30,
  currency: 1,
  taxRate: 13,
  invoiceType: 1,
  isActive: true,
  balance: 12500.00,
  remarks: '重要客户，优先处理',
  contacts: [
    {
      id: 'contact-001',
      contactName: '李四',
      gender: 0,
      department: '采购部',
      position: '采购经理',
      mobilePhone: '13800138000',
      email: 'lisi@test.com',
      isDefault: true,
      isDecisionMaker: true
    }
  ],
  addresses: [],
  bankAccounts: [],
  createdAt: '2024-01-15T08:00:00Z',
  updatedAt: '2024-03-10T14:30:00Z',
  ...overrides
})

const makeStatistics = (): any => ({
  totalCustomers: 256,
  activeCustomers: 198,
  newThisMonth: 12,
  totalBalance: 3850000.00,
  byLevel: { VIP: 30, BPO: 45, B: 80, C: 60, D: 41 },
  byIndustry: { Technology: 90, Manufacturing: 70, Trading: 60, Other: 36 }
})

beforeEach(() => {
  vi.clearAllMocks()
})

// ═══════════════════════════════════════════════════════════════════════════════
// 1. searchCustomers - 分页查询客户列表
// ═══════════════════════════════════════════════════════════════════════════════
describe('searchCustomers - 分页查询客户列表', () => {

  describe('正常流程', () => {
    it('TC-SEARCH-001: 无筛选条件，仅传分页参数，URL 正确', async () => {
      mockGet.mockResolvedValue({ items: [], totalCount: 0 })
      await customerApi.searchCustomers({ pageNumber: 1, pageSize: 20 })
      const url = mockGet.mock.calls[0][0] as string
      expect(url).toContain('/api/v1/customers')
      expect(url).toContain('pageNumber=1')
      expect(url).toContain('pageSize=20')
    })

    it('TC-SEARCH-002: 关键字搜索，URL 包含 searchTerm 参数（URL 编码）', async () => {
      mockGet.mockResolvedValue({ items: [], totalCount: 0 })
      await customerApi.searchCustomers({ pageNumber: 1, pageSize: 20, searchTerm: '深圳科技' })
      const url = mockGet.mock.calls[0][0] as string
      expect(url).toContain('searchTerm=')
      expect(decodeURIComponent(url)).toContain('searchTerm=深圳科技')
    })

    it('TC-SEARCH-003: 按客户类型筛选（OEM=1），URL 包含 customerType=1', async () => {
      mockGet.mockResolvedValue({ items: [], totalCount: 0 })
      await customerApi.searchCustomers({ pageNumber: 1, pageSize: 20, customerType: 1 })
      const url = mockGet.mock.calls[0][0] as string
      expect(url).toContain('customerType=1')
    })

    it('TC-SEARCH-004: 按客户等级筛选（VIP），URL 包含 customerLevel=VIP', async () => {
      mockGet.mockResolvedValue({ items: [], totalCount: 0 })
      await customerApi.searchCustomers({ pageNumber: 1, pageSize: 20, customerLevel: 'VIP' })
      const url = mockGet.mock.calls[0][0] as string
      expect(url).toContain('customerLevel=VIP')
    })

    it('TC-SEARCH-005: 按行业筛选（Technology），URL 包含 industry=Technology', async () => {
      mockGet.mockResolvedValue({ items: [], totalCount: 0 })
      await customerApi.searchCustomers({ pageNumber: 1, pageSize: 20, industry: 'Technology' })
      const url = mockGet.mock.calls[0][0] as string
      expect(url).toContain('industry=Technology')
    })

    it('TC-SEARCH-006: 按状态筛选（启用），URL 包含 isActive=true', async () => {
      mockGet.mockResolvedValue({ items: [], totalCount: 0 })
      await customerApi.searchCustomers({ pageNumber: 1, pageSize: 20, isActive: true })
      const url = mockGet.mock.calls[0][0] as string
      expect(url).toContain('isActive=true')
    })

    it('TC-SEARCH-007: 按状态筛选（停用），URL 包含 isActive=false', async () => {
      mockGet.mockResolvedValue({ items: [], totalCount: 0 })
      await customerApi.searchCustomers({ pageNumber: 1, pageSize: 20, isActive: false })
      const url = mockGet.mock.calls[0][0] as string
      expect(url).toContain('isActive=false')
    })

    it('TC-SEARCH-008: 多条件组合筛选，URL 包含所有参数', async () => {
      mockGet.mockResolvedValue({ items: [], totalCount: 0 })
      await customerApi.searchCustomers({
        pageNumber: 2, pageSize: 50,
        searchTerm: '测试', customerType: 2, customerLevel: 'B',
        industry: 'Manufacturing', isActive: true,
        sortBy: 'createdAt', sortDescending: true
      })
      const url = mockGet.mock.calls[0][0] as string
      expect(url).toContain('pageNumber=2')
      expect(url).toContain('pageSize=50')
      expect(url).toContain('customerType=2')
      expect(url).toContain('customerLevel=B')
      expect(url).toContain('industry=Manufacturing')
      expect(url).toContain('isActive=true')
      expect(url).toContain('sortBy=createdAt')
      expect(url).toContain('sortDescending=true')
    })

    it('TC-SEARCH-009: 正常返回时，透传后端 items 和 totalCount', async () => {
      const customer = makeCustomer()
      mockGet.mockResolvedValue({ items: [customer], totalCount: 1, pageNumber: 1, pageSize: 20, totalPages: 1 })
      const result = await customerApi.searchCustomers({ pageNumber: 1, pageSize: 20 })
      expect(result.items).toHaveLength(1)
      expect(result.items[0].customerName).toBe('深圳测试科技有限公司')
      expect(result.totalCount).toBe(1)
    })

    it('TC-SEARCH-010: 第二页查询，pageNumber=2 正确传递', async () => {
      mockGet.mockResolvedValue({ items: [], totalCount: 100, pageNumber: 2, pageSize: 20, totalPages: 5 })
      const result = await customerApi.searchCustomers({ pageNumber: 2, pageSize: 20 })
      expect(mockGet.mock.calls[0][0]).toContain('pageNumber=2')
      expect(result.totalCount).toBe(100)
    })

    it('TC-SEARCH-011: 每页 100 条，pageSize=100 正确传递', async () => {
      mockGet.mockResolvedValue({ items: [], totalCount: 0 })
      await customerApi.searchCustomers({ pageNumber: 1, pageSize: 100 })
      expect(mockGet.mock.calls[0][0]).toContain('pageSize=100')
    })
  })

  describe('边界条件', () => {
    it('TC-SEARCH-012: 后端返回 null，容错返回空列表', async () => {
      mockGet.mockResolvedValue(null)
      const result = await customerApi.searchCustomers({ pageNumber: 1, pageSize: 20 })
      expect(result).toEqual({ items: [], totalCount: 0 })
    })

    it('TC-SEARCH-013: 后端返回 undefined，容错返回空列表', async () => {
      mockGet.mockResolvedValue(undefined)
      const result = await customerApi.searchCustomers({ pageNumber: 1, pageSize: 20 })
      expect(result).toEqual({ items: [], totalCount: 0 })
    })

    it('TC-SEARCH-014: searchTerm 为空字符串时，不附加 searchTerm 参数', async () => {
      mockGet.mockResolvedValue({ items: [], totalCount: 0 })
      await customerApi.searchCustomers({ pageNumber: 1, pageSize: 20, searchTerm: '' })
      const url = mockGet.mock.calls[0][0] as string
      expect(url).not.toContain('searchTerm')
    })

    it('TC-SEARCH-015: customerType 为 0 时，不附加 customerType 参数（0 为无效值）', async () => {
      mockGet.mockResolvedValue({ items: [], totalCount: 0 })
      await customerApi.searchCustomers({ pageNumber: 1, pageSize: 20, customerType: 0 })
      const url = mockGet.mock.calls[0][0] as string
      // customerType=0 不应被附加（PRD 规定枚举从 1 开始）
      expect(url).not.toContain('customerType=0')
    })
  })

  describe('异常场景', () => {
    it('TC-SEARCH-016: 网络错误时，抛出异常', async () => {
      mockGet.mockRejectedValue(new Error('Network Error'))
      await expect(customerApi.searchCustomers({ pageNumber: 1, pageSize: 20 }))
        .rejects.toThrow('Network Error')
    })

    it('TC-SEARCH-017: 服务器 500 错误时，抛出异常', async () => {
      mockGet.mockRejectedValue({ response: { status: 500, data: { message: '服务器内部错误' } } })
      await expect(customerApi.searchCustomers({ pageNumber: 1, pageSize: 20 }))
        .rejects.toBeDefined()
    })
  })
})

// ═══════════════════════════════════════════════════════════════════════════════
// 2. getCustomerById - 获取客户详情
// ═══════════════════════════════════════════════════════════════════════════════
describe('getCustomerById - 获取客户详情', () => {

  describe('正常流程', () => {
    it('TC-GET-001: 调用正确的 GET URL', async () => {
      mockGet.mockResolvedValue(makeCustomer())
      await customerApi.getCustomerById('cust-001')
      expect(mockGet.mock.calls[0][0]).toBe('/api/v1/customers/cust-001')
    })

    it('TC-GET-002: 返回完整的客户对象，包含 contacts/addresses/bankAccounts', async () => {
      const customer = makeCustomer()
      mockGet.mockResolvedValue(customer)
      const result = await customerApi.getCustomerById('cust-001')
      expect(result.id).toBe('cust-001')
      expect(result.customerName).toBe('深圳测试科技有限公司')
      expect(result.contacts).toHaveLength(1)
      expect(result.contacts[0].contactName).toBe('李四')
    })

    it('TC-GET-003: 返回 isActive=true 的启用状态客户', async () => {
      mockGet.mockResolvedValue(makeCustomer({ isActive: true }))
      const result = await customerApi.getCustomerById('cust-001')
      expect(result.isActive).toBe(true)
    })

    it('TC-GET-004: 返回 isActive=false 的停用状态客户', async () => {
      mockGet.mockResolvedValue(makeCustomer({ isActive: false }))
      const result = await customerApi.getCustomerById('cust-001')
      expect(result.isActive).toBe(false)
    })

    it('TC-GET-005: 返回负数 balance（欠款状态）', async () => {
      mockGet.mockResolvedValue(makeCustomer({ balance: -5000.00 }))
      const result = await customerApi.getCustomerById('cust-001')
      expect(result.balance).toBeLessThan(0)
    })

    it('TC-GET-006: UUID 格式 ID 正确传递到 URL', async () => {
      const uuid = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'
      mockGet.mockResolvedValue(makeCustomer({ id: uuid }))
      await customerApi.getCustomerById(uuid)
      expect(mockGet.mock.calls[0][0]).toBe(`/api/v1/customers/${uuid}`)
    })
  })

  describe('异常场景', () => {
    it('TC-GET-007: 客户不存在时（404），抛出异常', async () => {
      mockGet.mockRejectedValue({ response: { status: 404, data: { message: '客户不存在' } } })
      await expect(customerApi.getCustomerById('non-existent-id'))
        .rejects.toBeDefined()
    })

    it('TC-GET-008: 未授权访问时（401），抛出异常', async () => {
      mockGet.mockRejectedValue({ response: { status: 401 } })
      await expect(customerApi.getCustomerById('cust-001'))
        .rejects.toBeDefined()
    })
  })
})

// ═══════════════════════════════════════════════════════════════════════════════
// 3. createCustomer - 新建客户
// ═══════════════════════════════════════════════════════════════════════════════
describe('createCustomer - 新建客户', () => {

  describe('正常流程 - 请求 URL 和方法', () => {
    it('TC-CREATE-001: 请求发送到正确的 POST URL', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '新客户', customerType: 1, customerLevel: 'B' } as any)
      expect(mockPost.mock.calls[0][0]).toBe('/api/v1/customers')
    })

    it('TC-CREATE-002: 成功创建后返回包含 id 的客户对象', async () => {
      mockPost.mockResolvedValue(makeCustomer({ id: 'new-cust-001' }))
      const result = await customerApi.createCustomer({ customerName: '新客户', customerType: 1, customerLevel: 'B' } as any)
      expect(result.id).toBe('new-cust-001')
    })
  })

  describe('字段映射验证', () => {
    it('TC-CREATE-003: customerName 映射为 officialName', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '正式名称', customerType: 1, customerLevel: 'B' } as any)
      const body = mockPost.mock.calls[0][1]
      expect(body.officialName).toBe('正式名称')
    })

    it('TC-CREATE-004: customerShortName 映射为 nickName', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '全称', customerShortName: '简称', customerType: 1, customerLevel: 'B' } as any)
      const body = mockPost.mock.calls[0][1]
      expect(body.nickName).toBe('简称')
    })

    it('TC-CREATE-005: salesPersonId 映射为 salesUserId', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 1, customerLevel: 'B', salesPersonId: 'user-001' } as any)
      const body = mockPost.mock.calls[0][1]
      expect(body.salesUserId).toBe('user-001')
    })

    it('TC-CREATE-006: creditLimit 映射为 creditLine', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 1, customerLevel: 'B', creditLimit: 100000 } as any)
      const body = mockPost.mock.calls[0][1]
      expect(body.creditLine).toBe(100000)
    })

    it('TC-CREATE-007: paymentTerms 映射为 payment', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 1, customerLevel: 'B', paymentTerms: 60 } as any)
      const body = mockPost.mock.calls[0][1]
      expect(body.payment).toBe(60)
    })

    it('TC-CREATE-008: currency 映射为 tradeCurrency', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 1, customerLevel: 'B', currency: 2 } as any)
      const body = mockPost.mock.calls[0][1]
      expect(body.tradeCurrency).toBe(2)
    })

    it('TC-CREATE-009: unifiedSocialCreditCode 映射为 creditCode', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 1, customerLevel: 'B', unifiedSocialCreditCode: '91440300MA5FXXXXXX' } as any)
      const body = mockPost.mock.calls[0][1]
      expect(body.creditCode).toBe('91440300MA5FXXXXXX')
    })

    it('TC-CREATE-010: remarks 映射为 remark', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 1, customerLevel: 'B', remarks: '重要备注' } as any)
      const body = mockPost.mock.calls[0][1]
      expect(body.remark).toBe('重要备注')
    })
  })

  describe('P1 修复验证 - contacts 字段', () => {
    it('TC-CREATE-011: 包含联系人时，contacts 数组正确传递', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      const contacts = [{ contactName: '张三', gender: 0, mobilePhone: '13800138000', isDefault: true, isDecisionMaker: false }]
      await customerApi.createCustomer({ customerName: '测试', customerType: 1, customerLevel: 'B', contacts } as any)
      const body = mockPost.mock.calls[0][1]
      expect(body.contacts).toHaveLength(1)
      expect(body.contacts[0].contactName).toBe('张三')
      expect(body.contacts[0].mobilePhone).toBe('13800138000')
    })

    it('TC-CREATE-012: 包含多个联系人时，全部传递', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      const contacts = [
        { contactName: '张三', gender: 0, mobilePhone: '13800138000', isDefault: true, isDecisionMaker: true },
        { contactName: '李四', gender: 1, mobilePhone: '13900139000', isDefault: false, isDecisionMaker: false }
      ]
      await customerApi.createCustomer({ customerName: '测试', customerType: 1, customerLevel: 'B', contacts } as any)
      const body = mockPost.mock.calls[0][1]
      expect(body.contacts).toHaveLength(2)
    })

    it('TC-CREATE-013: 无联系人时，发送空数组 [] 而非 undefined', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 1, customerLevel: 'B' } as any)
      const body = mockPost.mock.calls[0][1]
      expect(body.contacts).toEqual([])
      expect(body.contacts).not.toBeUndefined()
    })
  })

  describe('P2 修复验证 - customerLevel 枚举映射', () => {
    it('TC-CREATE-014: customerLevel="D" → level=1', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 1, customerLevel: 'D' } as any)
      expect(mockPost.mock.calls[0][1].level).toBe(1)
    })

    it('TC-CREATE-015: customerLevel="C" → level=2', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 1, customerLevel: 'C' } as any)
      expect(mockPost.mock.calls[0][1].level).toBe(2)
    })

    it('TC-CREATE-016: customerLevel="B" → level=3', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 1, customerLevel: 'B' } as any)
      expect(mockPost.mock.calls[0][1].level).toBe(3)
    })

    it('TC-CREATE-017: customerLevel="BPO" → level=4', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 1, customerLevel: 'BPO' } as any)
      expect(mockPost.mock.calls[0][1].level).toBe(4)
    })

    it('TC-CREATE-018: customerLevel="VIP" → level=5', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 1, customerLevel: 'VIP' } as any)
      expect(mockPost.mock.calls[0][1].level).toBe(5)
    })

    it('TC-CREATE-019: customerLevel="VPO" → level=6', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 1, customerLevel: 'VPO' } as any)
      expect(mockPost.mock.calls[0][1].level).toBe(6)
    })

    it('TC-CREATE-020: 旧值 customerLevel="Normal" 兼容映射 → level=3', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 1, customerLevel: 'Normal' } as any)
      expect(mockPost.mock.calls[0][1].level).toBe(3)
    })

    it('TC-CREATE-021: 旧值 customerLevel="Important" 兼容映射 → level=5', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 1, customerLevel: 'Important' } as any)
      expect(mockPost.mock.calls[0][1].level).toBe(5)
    })

    it('TC-CREATE-022: 旧值 customerLevel="Lead" 兼容映射 → level=1', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 1, customerLevel: 'Lead' } as any)
      expect(mockPost.mock.calls[0][1].level).toBe(1)
    })

    it('TC-CREATE-023: customerLevel 未传时，默认 level=3（B级）', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 1 } as any)
      expect(mockPost.mock.calls[0][1].level).toBe(3)
    })
  })

  describe('P3 修复验证 - customerType 枚举', () => {
    it('TC-CREATE-024: customerType=1 (OEM) → type=1', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 1, customerLevel: 'B' } as any)
      expect(mockPost.mock.calls[0][1].type).toBe(1)
    })

    it('TC-CREATE-025: customerType=2 (ODM) → type=2', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 2, customerLevel: 'B' } as any)
      expect(mockPost.mock.calls[0][1].type).toBe(2)
    })

    it('TC-CREATE-026: customerType=3 (终端用户) → type=3', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 3, customerLevel: 'B' } as any)
      expect(mockPost.mock.calls[0][1].type).toBe(3)
    })

    it('TC-CREATE-027: customerType=4 (IDH) → type=4', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 4, customerLevel: 'B' } as any)
      expect(mockPost.mock.calls[0][1].type).toBe(4)
    })

    it('TC-CREATE-028: customerType=5 (贸易商) → type=5', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 5, customerLevel: 'B' } as any)
      expect(mockPost.mock.calls[0][1].type).toBe(5)
    })

    it('TC-CREATE-029: customerType=6 (代理商) → type=6', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 6, customerLevel: 'B' } as any)
      expect(mockPost.mock.calls[0][1].type).toBe(6)
    })

    it('TC-CREATE-030: customerType=0（无效）→ type 修正为 1', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerType: 0, customerLevel: 'B' } as any)
      expect(mockPost.mock.calls[0][1].type).toBe(1)
    })

    it('TC-CREATE-031: customerType 未传 → type 默认为 1', async () => {
      mockPost.mockResolvedValue(makeCustomer())
      await customerApi.createCustomer({ customerName: '测试', customerLevel: 'B' } as any)
      expect(mockPost.mock.calls[0][1].type).toBe(1)
    })
  })

  describe('异常场景', () => {
    it('TC-CREATE-032: 服务器返回 400（参数校验失败），抛出异常', async () => {
      mockPost.mockRejectedValue({ response: { status: 400, data: { message: '客户名称不能为空' } } })
      await expect(customerApi.createCustomer({ customerName: '', customerType: 1, customerLevel: 'B' } as any))
        .rejects.toBeDefined()
    })

    it('TC-CREATE-033: 服务器返回 409（客户编号重复），抛出异常', async () => {
      mockPost.mockRejectedValue({ response: { status: 409, data: { message: '客户编号已存在' } } })
      await expect(customerApi.createCustomer({ customerName: '重复客户', customerType: 1, customerLevel: 'B' } as any))
        .rejects.toBeDefined()
    })
  })
})

// ═══════════════════════════════════════════════════════════════════════════════
// 4. updateCustomer - 更新客户
// ═══════════════════════════════════════════════════════════════════════════════
describe('updateCustomer - 更新客户', () => {

  describe('正常流程', () => {
    it('TC-UPDATE-001: 请求发送到正确的 PUT URL（含 ID）', async () => {
      mockPut.mockResolvedValue(makeCustomer())
      await customerApi.updateCustomer('cust-001', { customerName: '更新名称', customerType: 1, customerLevel: 'VIP' } as any)
      expect(mockPut.mock.calls[0][0]).toBe('/api/v1/customers/cust-001')
    })

    it('TC-UPDATE-002: 成功更新后返回更新后的客户对象', async () => {
      mockPut.mockResolvedValue(makeCustomer({ customerName: '更新后名称' }))
      const result = await customerApi.updateCustomer('cust-001', { customerName: '更新后名称', customerType: 1, customerLevel: 'VIP' } as any)
      expect(result.customerName).toBe('更新后名称')
    })

    it('TC-UPDATE-003: P1 - 更新时 contacts 数组正确传递', async () => {
      mockPut.mockResolvedValue(makeCustomer())
      const contacts = [{ contactName: '王五', gender: 0, mobilePhone: '13700137000', isDefault: true, isDecisionMaker: false }]
      await customerApi.updateCustomer('cust-001', { customerName: '测试', customerType: 1, customerLevel: 'B', contacts } as any)
      const body = mockPut.mock.calls[0][1]
      expect(body.contacts).toHaveLength(1)
      expect(body.contacts[0].contactName).toBe('王五')
    })

    it('TC-UPDATE-004: P1 - 更新时无联系人，发送空数组 []', async () => {
      mockPut.mockResolvedValue(makeCustomer())
      await customerApi.updateCustomer('cust-001', { customerName: '测试', customerType: 1, customerLevel: 'B' } as any)
      const body = mockPut.mock.calls[0][1]
      expect(body.contacts).toEqual([])
    })

    it('TC-UPDATE-005: P2 - customerLevel="BPO" → level=4', async () => {
      mockPut.mockResolvedValue(makeCustomer())
      await customerApi.updateCustomer('cust-001', { customerName: '测试', customerType: 1, customerLevel: 'BPO' } as any)
      expect(mockPut.mock.calls[0][1].level).toBe(4)
    })

    it('TC-UPDATE-006: P2 - customerLevel="VPO" → level=6', async () => {
      mockPut.mockResolvedValue(makeCustomer())
      await customerApi.updateCustomer('cust-001', { customerName: '测试', customerType: 1, customerLevel: 'VPO' } as any)
      expect(mockPut.mock.calls[0][1].level).toBe(6)
    })

    it('TC-UPDATE-007: P3 - customerType=3 (终端用户) → type=3', async () => {
      mockPut.mockResolvedValue(makeCustomer())
      await customerApi.updateCustomer('cust-001', { customerName: '测试', customerType: 3, customerLevel: 'B' } as any)
      expect(mockPut.mock.calls[0][1].type).toBe(3)
    })

    it('TC-UPDATE-008: 更新信用额度，creditLimit 映射为 creditLine', async () => {
      mockPut.mockResolvedValue(makeCustomer())
      await customerApi.updateCustomer('cust-001', { customerName: '测试', customerType: 1, customerLevel: 'B', creditLimit: 200000 } as any)
      expect(mockPut.mock.calls[0][1].creditLine).toBe(200000)
    })

    it('TC-UPDATE-009: 更新账期，paymentTerms 映射为 payment', async () => {
      mockPut.mockResolvedValue(makeCustomer())
      await customerApi.updateCustomer('cust-001', { customerName: '测试', customerType: 1, customerLevel: 'B', paymentTerms: 45 } as any)
      expect(mockPut.mock.calls[0][1].payment).toBe(45)
    })
  })

  describe('异常场景', () => {
    it('TC-UPDATE-010: 客户不存在时（404），抛出异常', async () => {
      mockPut.mockRejectedValue({ response: { status: 404, data: { message: '客户不存在' } } })
      await expect(customerApi.updateCustomer('non-existent', { customerName: '测试', customerType: 1, customerLevel: 'B' } as any))
        .rejects.toBeDefined()
    })
  })
})

// ═══════════════════════════════════════════════════════════════════════════════
// 5. deleteCustomer - 删除客户
// ═══════════════════════════════════════════════════════════════════════════════
describe('deleteCustomer - 删除客户', () => {

  it('TC-DELETE-001: 调用正确的 DELETE URL', async () => {
    mockDelete.mockResolvedValue(undefined)
    await customerApi.deleteCustomer('cust-001')
    expect(mockDelete.mock.calls[0][0]).toBe('/api/v1/customers/cust-001')
  })

  it('TC-DELETE-002: 删除成功，返回 void（无返回值）', async () => {
    mockDelete.mockResolvedValue(undefined)
    const result = await customerApi.deleteCustomer('cust-001')
    expect(result).toBeUndefined()
  })

  it('TC-DELETE-003: UUID 格式 ID 正确传递', async () => {
    mockDelete.mockResolvedValue(undefined)
    const uuid = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'
    await customerApi.deleteCustomer(uuid)
    expect(mockDelete.mock.calls[0][0]).toBe(`/api/v1/customers/${uuid}`)
  })

  it('TC-DELETE-004: 客户不存在时（404），抛出异常', async () => {
    mockDelete.mockRejectedValue({ response: { status: 404 } })
    await expect(customerApi.deleteCustomer('non-existent'))
      .rejects.toBeDefined()
  })

  it('TC-DELETE-005: 客户存在关联订单时（409），抛出异常', async () => {
    mockDelete.mockRejectedValue({ response: { status: 409, data: { message: '客户存在关联订单，无法删除' } } })
    await expect(customerApi.deleteCustomer('cust-with-orders'))
      .rejects.toBeDefined()
  })
})

// ═══════════════════════════════════════════════════════════════════════════════
// 6. activateCustomer / deactivateCustomer - 状态切换
// ═══════════════════════════════════════════════════════════════════════════════
describe('activateCustomer - 启用客户', () => {

  it('TC-ACTIVATE-001: 调用正确的 POST URL', async () => {
    mockPost.mockResolvedValue(undefined)
    await customerApi.activateCustomer('cust-001')
    expect(mockPost.mock.calls[0][0]).toBe('/api/v1/customers/cust-001/activate')
  })

  it('TC-ACTIVATE-002: 请求体为空（无需传参）', async () => {
    mockPost.mockResolvedValue(undefined)
    await customerApi.activateCustomer('cust-001')
    // 不应传递请求体，或请求体为 undefined
    expect(mockPost.mock.calls[0][1]).toBeUndefined()
  })

  it('TC-ACTIVATE-003: 激活成功，返回 void', async () => {
    mockPost.mockResolvedValue(undefined)
    const result = await customerApi.activateCustomer('cust-001')
    expect(result).toBeUndefined()
  })

  it('TC-ACTIVATE-004: 客户不存在时（404），抛出异常', async () => {
    mockPost.mockRejectedValue({ response: { status: 404 } })
    await expect(customerApi.activateCustomer('non-existent'))
      .rejects.toBeDefined()
  })
})

describe('deactivateCustomer - 停用客户', () => {

  it('TC-DEACTIVATE-001: 调用正确的 POST URL', async () => {
    mockPost.mockResolvedValue(undefined)
    await customerApi.deactivateCustomer('cust-001')
    expect(mockPost.mock.calls[0][0]).toBe('/api/v1/customers/cust-001/deactivate')
  })

  it('TC-DEACTIVATE-002: 请求体为空（无需传参）', async () => {
    mockPost.mockResolvedValue(undefined)
    await customerApi.deactivateCustomer('cust-001')
    expect(mockPost.mock.calls[0][1]).toBeUndefined()
  })

  it('TC-DEACTIVATE-003: 停用成功，返回 void', async () => {
    mockPost.mockResolvedValue(undefined)
    const result = await customerApi.deactivateCustomer('cust-001')
    expect(result).toBeUndefined()
  })

  it('TC-DEACTIVATE-004: 客户已停用时再次停用，不抛出异常（幂等）', async () => {
    mockPost.mockResolvedValue(undefined)
    await expect(customerApi.deactivateCustomer('already-inactive'))
      .resolves.toBeUndefined()
  })
})

// ═══════════════════════════════════════════════════════════════════════════════
// 7. getCustomerStatistics - 获取统计数据
// ═══════════════════════════════════════════════════════════════════════════════
describe('getCustomerStatistics - 获取统计数据', () => {

  it('TC-STATS-001: 调用正确的 GET URL', async () => {
    mockGet.mockResolvedValue(makeStatistics())
    await customerApi.getCustomerStatistics()
    expect(mockGet.mock.calls[0][0]).toBe('/api/v1/customers/statistics')
  })

  it('TC-STATS-002: 返回包含 totalCustomers 字段', async () => {
    mockGet.mockResolvedValue(makeStatistics())
    const result = await customerApi.getCustomerStatistics()
    expect(result.totalCustomers).toBe(256)
  })

  it('TC-STATS-003: 返回包含 activeCustomers 字段', async () => {
    mockGet.mockResolvedValue(makeStatistics())
    const result = await customerApi.getCustomerStatistics()
    expect(result.activeCustomers).toBe(198)
  })

  it('TC-STATS-004: 返回包含 newThisMonth 字段', async () => {
    mockGet.mockResolvedValue(makeStatistics())
    const result = await customerApi.getCustomerStatistics()
    expect(result.newThisMonth).toBe(12)
  })

  it('TC-STATS-005: 返回包含 totalBalance 字段（金额）', async () => {
    mockGet.mockResolvedValue(makeStatistics())
    const result = await customerApi.getCustomerStatistics()
    expect(result.totalBalance).toBe(3850000.00)
  })

  it('TC-STATS-006: 返回包含 byLevel 分组数据', async () => {
    mockGet.mockResolvedValue(makeStatistics())
    const result = await customerApi.getCustomerStatistics()
    expect(result.byLevel).toBeDefined()
    expect(result.byLevel['VIP']).toBe(30)
  })

  it('TC-STATS-007: 返回包含 byIndustry 分组数据', async () => {
    mockGet.mockResolvedValue(makeStatistics())
    const result = await customerApi.getCustomerStatistics()
    expect(result.byIndustry).toBeDefined()
    expect(result.byIndustry['Technology']).toBe(90)
  })
})

// ═══════════════════════════════════════════════════════════════════════════════
// 8. getCustomerContactHistory / addContactHistory - 联系历史
// ═══════════════════════════════════════════════════════════════════════════════
describe('getCustomerContactHistory - 获取联系历史', () => {

  it('TC-HISTORY-001: 调用正确的 GET URL', async () => {
    mockGet.mockResolvedValue([])
    await customerApi.getCustomerContactHistory('cust-001')
    expect(mockGet.mock.calls[0][0]).toBe('/api/v1/customers/cust-001/contact-history')
  })

  it('TC-HISTORY-002: 返回联系历史数组', async () => {
    const history = [
      { id: 'h-001', type: 'quote', content: '创建报价单 QT-2024-001', time: '2024-03-15T10:30:00Z' },
      { id: 'h-002', type: 'order', content: '销售订单 SO-2024-003 已发货', time: '2024-03-10T14:20:00Z' }
    ]
    mockGet.mockResolvedValue(history)
    const result = await customerApi.getCustomerContactHistory('cust-001')
    expect(result).toHaveLength(2)
    expect(result[0].content).toContain('报价单')
  })

  it('TC-HISTORY-003: 无历史记录时，返回空数组', async () => {
    mockGet.mockResolvedValue([])
    const result = await customerApi.getCustomerContactHistory('new-cust')
    expect(result).toEqual([])
  })
})

describe('addContactHistory - 添加联系记录', () => {

  it('TC-HISTORY-004: 调用正确的 POST URL', async () => {
    mockPost.mockResolvedValue({ id: 'h-new' })
    await customerApi.addContactHistory('cust-001', { type: 'call', content: '电话沟通' })
    expect(mockPost.mock.calls[0][0]).toBe('/api/v1/customers/cust-001/contact-history')
  })

  it('TC-HISTORY-005: 请求体包含联系记录内容', async () => {
    mockPost.mockResolvedValue({ id: 'h-new' })
    const record = { type: 'visit', content: '上门拜访，确认合作意向', time: '2024-03-16T09:00:00Z' }
    await customerApi.addContactHistory('cust-001', record)
    const body = mockPost.mock.calls[0][1]
    expect(body.content).toBe('上门拜访，确认合作意向')
  })
})
