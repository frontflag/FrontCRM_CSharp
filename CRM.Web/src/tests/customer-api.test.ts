/**
 * 接口集成测试：customerApi
 * 使用 vi.mock 模拟 apiClient，验证请求参数构建和响应处理是否正确
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'

// Mock apiClient 模块
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

beforeEach(() => {
  vi.clearAllMocks()
})

// ─── searchCustomers ───────────────────────────────────────────────────────────
describe('searchCustomers - 列表查询接口', () => {
  it('无筛选条件时，URL 只包含分页参数', async () => {
    mockGet.mockResolvedValue({ items: [], totalCount: 0 })
    await customerApi.searchCustomers({ pageNumber: 1, pageSize: 20 })
    const url = mockGet.mock.calls[0][0] as string
    expect(url).toContain('/api/v1/customers')
    expect(url).toContain('pageNumber=1')
    expect(url).toContain('pageSize=20')
    expect(url).not.toContain('searchTerm')
  })

  it('有关键字时，URL 包含 searchTerm 参数', async () => {
    mockGet.mockResolvedValue({ items: [], totalCount: 0 })
    await customerApi.searchCustomers({ pageNumber: 1, pageSize: 20, searchTerm: '测试客户' })
    const url = mockGet.mock.calls[0][0] as string
    expect(url).toContain('searchTerm=%E6%B5%8B%E8%AF%95%E5%AE%A2%E6%88%B7')
  })

  it('有 customerType 筛选时，URL 包含 customerType 参数', async () => {
    mockGet.mockResolvedValue({ items: [], totalCount: 0 })
    await customerApi.searchCustomers({ pageNumber: 1, pageSize: 20, customerType: 1 })
    const url = mockGet.mock.calls[0][0] as string
    expect(url).toContain('customerType=1')
  })

  it('后端返回 null/undefined 时，返回空列表', async () => {
    mockGet.mockResolvedValue(null)
    const result = await customerApi.searchCustomers({ pageNumber: 1, pageSize: 20 })
    expect(result).toEqual({ items: [], totalCount: 0 })
  })

  it('正常返回时，透传后端数据', async () => {
    const mockData = { items: [{ id: '1', customerName: '测试公司' }], totalCount: 1 }
    mockGet.mockResolvedValue(mockData)
    const result = await customerApi.searchCustomers({ pageNumber: 1, pageSize: 20 })
    expect(result.items).toHaveLength(1)
    expect(result.totalCount).toBe(1)
  })
})

// ─── createCustomer ────────────────────────────────────────────────────────────
describe('createCustomer - P1/P2/P3 修复验证', () => {
  it('P1: 包含 contacts 数组', async () => {
    mockPost.mockResolvedValue({ id: 'new-1', customerName: '新客户' })
    const contacts = [{ contactName: '张三', mobilePhone: '13800138000', isDefault: true }]
    await customerApi.createCustomer({
      customerName: '新客户', customerType: 1, customerLevel: 'VIP',
      contacts
    } as any)
    const body = mockPost.mock.calls[0][1]
    expect(body.contacts).toEqual(contacts)
  })

  it('P1: contacts 为空时，发送空数组而非 undefined', async () => {
    mockPost.mockResolvedValue({ id: 'new-2' })
    await customerApi.createCustomer({
      customerName: '无联系人客户', customerType: 1, customerLevel: 'B'
    } as any)
    const body = mockPost.mock.calls[0][1]
    expect(body.contacts).toEqual([])
  })

  it('P2: customerLevel="VIP" 映射为 level=5', async () => {
    mockPost.mockResolvedValue({ id: 'new-3' })
    await customerApi.createCustomer({
      customerName: 'VIP客户', customerType: 1, customerLevel: 'VIP'
    } as any)
    const body = mockPost.mock.calls[0][1]
    expect(body.level).toBe(5)
  })

  it('P2: customerLevel="D" 映射为 level=1', async () => {
    mockPost.mockResolvedValue({ id: 'new-6' })
    await customerApi.createCustomer({
      customerName: 'D级客户', customerType: 1, customerLevel: 'D'
    } as any)
    const body = mockPost.mock.calls[0][1]
    expect(body.level).toBe(1)
  })

  it('P3: customerType=1 (OEM) 直接传递 type=1', async () => {
    mockPost.mockResolvedValue({ id: 'new-7' })
    await customerApi.createCustomer({
      customerName: 'OEM客户', customerType: 1, customerLevel: 'B'
    } as any)
    const body = mockPost.mock.calls[0][1]
    expect(body.type).toBe(1)
  })

  it('P3: customerType=0 (无效) 修正为 type=1', async () => {
    mockPost.mockResolvedValue({ id: 'new-8' })
    await customerApi.createCustomer({
      customerName: '默认类型客户', customerType: 0, customerLevel: 'B'
    } as any)
    const body = mockPost.mock.calls[0][1]
    expect(body.type).toBe(1)
  })

  it('字段映射: customerName -> officialName', async () => {
    mockPost.mockResolvedValue({ id: 'new-9' })
    await customerApi.createCustomer({
      customerName: '正式名称', customerShortName: '简称', customerType: 1, customerLevel: 'B'
    } as any)
    const body = mockPost.mock.calls[0][1]
    expect(body.officialName).toBe('正式名称')
    expect(body.nickName).toBe('简称')
  })

  it('请求发送到正确的 URL: POST /api/v1/customers', async () => {
    mockPost.mockResolvedValue({ id: 'new-10' })
    await customerApi.createCustomer({
      customerName: '测试', customerType: 1, customerLevel: 'B'
    } as any)
    expect(mockPost.mock.calls[0][0]).toBe('/api/v1/customers')
  })
})

// ─── updateCustomer ────────────────────────────────────────────────────────────
describe('updateCustomer - P1/P2/P3 修复验证', () => {
  it('P1: 更新时包含 contacts 数组', async () => {
    mockPut.mockResolvedValue({ id: 'cust-1' })
    const contacts = [{ contactName: '李四', mobilePhone: '13900139000', isDefault: true }]
    await customerApi.updateCustomer('cust-1', {
      customerName: '更新客户', customerType: 2, customerLevel: 'C',
      contacts
    } as any)
    const body = mockPut.mock.calls[0][1]
    expect(body.contacts).toEqual(contacts)
  })

  it('P2: customerLevel="BPO" 映射为 level=4', async () => {
    mockPut.mockResolvedValue({ id: 'cust-2' })
    await customerApi.updateCustomer('cust-2', {
      customerName: 'BPO客户', customerType: 1, customerLevel: 'BPO'
    } as any)
    const body = mockPut.mock.calls[0][1]
    expect(body.level).toBe(4)
  })

  it('P3: customerType=3 (EndUser) 直接传递 type=3', async () => {
    mockPut.mockResolvedValue({ id: 'cust-4' })
    await customerApi.updateCustomer('cust-4', {
      customerName: '终端用户', customerType: 3, customerLevel: 'B'
    } as any)
    const body = mockPut.mock.calls[0][1]
    expect(body.type).toBe(3)
  })

  it('请求发送到正确的 URL: PUT /api/v1/customers/:id', async () => {
    mockPut.mockResolvedValue({ id: 'cust-5' })
    await customerApi.updateCustomer('cust-5', {
      customerName: '测试', customerType: 1, customerLevel: 'B'
    } as any)
    expect(mockPut.mock.calls[0][0]).toBe('/api/v1/customers/cust-5')
  })
})

// ─── deleteCustomer ────────────────────────────────────────────────────────────
describe('deleteCustomer - 删除接口', () => {
  it('调用正确的 DELETE URL', async () => {
    mockDelete.mockResolvedValue(undefined)
    await customerApi.deleteCustomer('del-1')
    expect(mockDelete.mock.calls[0][0]).toBe('/api/v1/customers/del-1')
  })
})

// ─── activateCustomer / deactivateCustomer ─────────────────────────────────────
describe('activateCustomer / deactivateCustomer - 状态切换接口', () => {
  it('激活: 调用 POST /api/v1/customers/:id/activate', async () => {
    mockPost.mockResolvedValue(undefined)
    await customerApi.activateCustomer('act-1')
    expect(mockPost.mock.calls[0][0]).toBe('/api/v1/customers/act-1/activate')
  })

  it('停用: 调用 POST /api/v1/customers/:id/deactivate', async () => {
    mockPost.mockResolvedValue(undefined)
    await customerApi.deactivateCustomer('act-2')
    expect(mockPost.mock.calls[0][0]).toBe('/api/v1/customers/act-2/deactivate')
  })
})

// ─── getCustomerById ───────────────────────────────────────────────────────────
describe('getCustomerById - 详情查询接口', () => {
  it('调用正确的 GET URL', async () => {
    mockGet.mockResolvedValue({ id: 'cust-1', customerName: '测试公司' })
    const result = await customerApi.getCustomerById('cust-1')
    expect(mockGet.mock.calls[0][0]).toBe('/api/v1/customers/cust-1')
    expect(result.id).toBe('cust-1')
  })
})
