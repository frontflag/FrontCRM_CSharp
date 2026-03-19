// 采购订单模拟数据服务
import { ElMessage } from 'element-plus'

// 模拟数据存储
let mockPurchaseOrders: any[] = [
  {
    id: 'PO001',
    purchaseOrderCode: 'PO20240318001',
    vendorId: 'V001',
    vendorName: '深圳市华强电子有限公司',
    vendorCode: 'HQ001',
    purchaseUserId: 'U003',
    purchaseUserName: '王五',
    status: 1,
    type: 1,
    currency: 2,
    total: 120000.00,
    convertTotal: 120000.00,
    itemRows: 2,
    stockStatus: 0,
    financeStatus: 0,
    deliveryAddress: '深圳市南山区科技园',
    deliveryDate: '2024-04-10',
    comment: '根据销售订单SO20240318001生成',
    innerComment: '供应商要求预付款30%',
    createTime: '2024-03-18 11:00:00',
    items: [
      {
        id: 'POI001',
        purchaseOrderId: 'PO001',
        sellOrderItemId: 'SOI001',
        vendorId: 'V001',
        pn: 'STM32F407VGT6',
        brand: 'ST',
        qty: 1000,
        cost: 18.50,
        currency: 2,
        deliveryDate: '2024-04-10',
        status: 0
      },
      {
        id: 'POI002',
        purchaseOrderId: 'PO001',
        sellOrderItemId: 'SOI002',
        vendorId: 'V001',
        pn: 'TJA1101AHN/0Z',
        brand: 'NXP',
        qty: 500,
        cost: 245.00,
        currency: 2,
        deliveryDate: '2024-04-10',
        status: 0
      }
    ]
  },
  {
    id: 'PO002',
    purchaseOrderCode: 'PO20240318002',
    vendorId: 'V002',
    vendorName: '上海芯片贸易有限公司',
    vendorCode: 'XP002',
    purchaseUserId: 'U004',
    purchaseUserName: '赵六',
    status: 2,
    type: 2,
    currency: 1,
    total: 68000.00,
    convertTotal: 68000.00,
    itemRows: 1,
    stockStatus: 1,
    financeStatus: 1,
    deliveryAddress: '上海市浦东新区张江高科',
    deliveryDate: '2024-03-20',
    comment: '紧急采购',
    innerComment: '已支付50%定金',
    createTime: '2024-03-18 15:30:00',
    items: [
      {
        id: 'POI003',
        purchaseOrderId: 'PO002',
        sellOrderItemId: 'SOI003',
        vendorId: 'V002',
        pn: 'BMI270',
        brand: 'BOSCH',
        qty: 200,
        cost: 340.00,
        currency: 1,
        deliveryDate: '2024-03-20',
        status: 0
      }
    ]
  },
  {
    id: 'PO003',
    purchaseOrderCode: 'PO20240319001',
    vendorId: 'V003',
    vendorName: '北京元器件供应中心',
    vendorCode: 'BJ003',
    purchaseUserId: 'U003',
    purchaseUserName: '王五',
    status: 0,
    type: 1,
    currency: 1,
    total: 250000.00,
    convertTotal: 250000.00,
    itemRows: 2,
    stockStatus: 0,
    financeStatus: 0,
    deliveryAddress: '北京市朝阳区电子城',
    deliveryDate: '2024-04-20',
    comment: '批量采购',
    innerComment: '账期30天',
    createTime: '2024-03-19 10:00:00',
    items: []
  }
]

let nextId = 4

// 模拟采购订单API
export const mockPurchaseOrderApi = {
  // 获取列表
  async getList() {
    await delay(300)
    return { success: true, data: mockPurchaseOrders }
  },

  // 获取详情
  async getById(id: string) {
    await delay(200)
    const order = mockPurchaseOrders.find(o => o.id === id)
    return { success: true, data: order }
  },

  // 创建
  async create(data: any) {
    await delay(500)
    const newOrder = {
      id: `PO${String(nextId++).padStart(3, '0')}`,
      ...data,
      status: 0,
      stockStatus: 0,
      financeStatus: 0,
      createTime: new Date().toLocaleString('zh-CN')
    }
    mockPurchaseOrders.unshift(newOrder)
    ElMessage.success('采购订单创建成功')
    return { success: true, data: newOrder }
  },

  // 更新
  async update(id: string, data: any) {
    await delay(400)
    const index = mockPurchaseOrders.findIndex(o => o.id === id)
    if (index > -1) {
      mockPurchaseOrders[index] = { ...mockPurchaseOrders[index], ...data }
      ElMessage.success('采购订单更新成功')
      return { success: true, data: mockPurchaseOrders[index] }
    }
    throw new Error('订单不存在')
  },

  // 删除
  async delete(id: string) {
    await delay(300)
    const index = mockPurchaseOrders.findIndex(o => o.id === id)
    if (index > -1) {
      mockPurchaseOrders.splice(index, 1)
      ElMessage.success('采购订单删除成功')
      return { success: true }
    }
    throw new Error('订单不存在')
  },

  // 更新状态
  async updateStatus(id: string, status: number) {
    await delay(200)
    const index = mockPurchaseOrders.findIndex(o => o.id === id)
    if (index > -1) {
      mockPurchaseOrders[index].status = status
      ElMessage.success('状态更新成功')
      return { success: true }
    }
    throw new Error('订单不存在')
  },

  // 自动生成(以销定采)
  async autoGenerate(sellOrderId: string) {
    await delay(800)
    ElMessage.success(`已根据销售订单 ${sellOrderId} 生成采购订单`)
    return { success: true, data: [] }
  }
}

function delay(ms: number) {
  return new Promise(resolve => setTimeout(resolve, ms))
}

export default mockPurchaseOrderApi
