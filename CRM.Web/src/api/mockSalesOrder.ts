// 销售订单模拟数据服务
import { ElMessage } from 'element-plus'

// ===== 销售订单明细状态枚举 =====
// 审核状态
export enum ItemAuditStatus {
  New = 0,        // 新建
  Pending = 1,    // 待审核
  Approved = 2    // 已审核
}

// 货运状态
export enum ShippingStatus {
  PendingShipment = 0,  // 待发货
  InTransit = 1,        // 在途
  PartialDelivered = 2, // 部分送达
  DeliveryComplete = 3  // 货运完成
}

// 款项状态
export enum PaymentStatus {
  PartialPaid = 0,    // 部分付款
  PaymentComplete = 1 // 付款完成
}

// 票据状态
export enum InvoiceStatus {
  PendingInvoice = 0,  // 待开票
  PartialInvoiced = 1, // 部分开票
  InvoiceComplete = 2  // 开票完成
}

// 模拟数据存储
let mockSalesOrders: any[] = [
  {
    id: 'SO001',
    sellOrderCode: 'SO20240318001',
    customerId: 'C001',
    customerName: '深圳华为技术有限公司',
    salesUserId: 'U001',
    salesUserName: '张三',
    status: 1,
    type: 1,
    currency: 2,
    total: 158000.00,
    convertTotal: 158000.00,
    itemRows: 2,
    purchaseOrderStatus: 1,
    stockOutStatus: 0,
    financeReceiptStatus: 0,
    deliveryAddress: '深圳市龙岗区华为基地',
    deliveryDate: '2024-04-15',
    comment: '紧急订单，请优先处理',
    createTime: '2024-03-18 10:30:00',
    items: [
      {
        id: 'SOI001',
        sellOrderId: 'SO001',
        pn: 'STM32F407VGT6',
        brand: 'ST',
        customerPnNo: 'HW-CHIP-001',
        qty: 1000,
        price: 25.50,
        currency: 2,
        dateCode: '2024+',
        deliveryDate: '2024-04-15',
        status: 0,
        itemAuditStatus: 1,   // 待审核
        shippingStatus: 1,    // 在途
        paymentStatus: 0,     // 部分付款
        invoiceStatus: 1      // 部分开票
      },
      {
        id: 'SOI002',
        sellOrderId: 'SO001',
        pn: 'TJA1101AHN/0Z',
        brand: 'NXP',
        customerPnNo: 'HW-CHIP-002',
        qty: 500,
        price: 311.00,
        currency: 2,
        dateCode: '2024+',
        deliveryDate: '2024-04-15',
        status: 0,
        itemAuditStatus: 0,   // 新建
        shippingStatus: 0,    // 待发货
        paymentStatus: 0,     // 部分付款
        invoiceStatus: 0      // 待开票
      }
    ]
  },
  {
    id: 'SO002',
    sellOrderCode: 'SO20240318002',
    customerId: 'C002',
    customerName: '小米科技有限公司',
    salesUserId: 'U002',
    salesUserName: '李四',
    status: 2,
    type: 2,
    currency: 1,
    total: 85000.00,
    convertTotal: 85000.00,
    itemRows: 1,
    purchaseOrderStatus: 2,
    stockOutStatus: 1,
    financeReceiptStatus: 1,
    deliveryAddress: '北京市海淀区小米科技园',
    deliveryDate: '2024-03-25',
    comment: '样品订单',
    createTime: '2024-03-18 14:20:00',
    items: [
      {
        id: 'SOI003',
        sellOrderId: 'SO002',
        pn: 'BMI270',
        brand: 'BOSCH',
        customerPnNo: 'XM-SENSOR-001',
        qty: 200,
        price: 425.00,
        currency: 1,
        dateCode: '2024+',
        deliveryDate: '2024-03-25',
        status: 0,
        itemAuditStatus: 2,   // 已审核
        shippingStatus: 3,    // 货运完成
        paymentStatus: 1,     // 付款完成
        invoiceStatus: 2      // 开票完成
      }
    ]
  },
  {
    id: 'SO003',
    sellOrderCode: 'SO20240319001',
    customerId: 'C003',
    customerName: '比亚迪股份有限公司',
    salesUserId: 'U001',
    salesUserName: '张三',
    status: 0,
    type: 1,
    currency: 1,
    total: 320000.00,
    convertTotal: 320000.00,
    itemRows: 3,
    purchaseOrderStatus: 0,
    stockOutStatus: 0,
    financeReceiptStatus: 0,
    deliveryAddress: '深圳市坪山区比亚迪路',
    deliveryDate: '2024-04-30',
    comment: '批量采购',
    createTime: '2024-03-19 09:00:00',
    items: []
  }
]

let nextId = 4

// 模拟销售订单API
export const mockSalesOrderApi = {
  // 获取列表
  async getList() {
    await delay(300)
    return { success: true, data: mockSalesOrders }
  },

  // 获取详情
  async getById(id: string) {
    await delay(200)
    const order = mockSalesOrders.find(o => o.id === id)
    return { success: true, data: order }
  },

  // 创建
  async create(data: any) {
    await delay(500)
    const newOrder = {
      id: `SO${String(nextId++).padStart(3, '0')}`,
      ...data,
      status: 0,
      purchaseOrderStatus: 0,
      stockOutStatus: 0,
      financeReceiptStatus: 0,
      createTime: new Date().toLocaleString('zh-CN')
    }
    mockSalesOrders.unshift(newOrder)
    ElMessage.success('销售订单创建成功')
    return { success: true, data: newOrder }
  },

  // 更新
  async update(id: string, data: any) {
    await delay(400)
    const index = mockSalesOrders.findIndex(o => o.id === id)
    if (index > -1) {
      mockSalesOrders[index] = { ...mockSalesOrders[index], ...data }
      ElMessage.success('销售订单更新成功')
      return { success: true, data: mockSalesOrders[index] }
    }
    throw new Error('订单不存在')
  },

  // 删除
  async delete(id: string) {
    await delay(300)
    const index = mockSalesOrders.findIndex(o => o.id === id)
    if (index > -1) {
      mockSalesOrders.splice(index, 1)
      ElMessage.success('销售订单删除成功')
      return { success: true }
    }
    throw new Error('订单不存在')
  },

  // 更新状态
  async updateStatus(id: string, status: number) {
    await delay(200)
    const index = mockSalesOrders.findIndex(o => o.id === id)
    if (index > -1) {
      mockSalesOrders[index].status = status
      ElMessage.success('状态更新成功')
      return { success: true }
    }
    throw new Error('订单不存在')
  }
}

function delay(ms: number) {
  return new Promise(resolve => setTimeout(resolve, ms))
}

export default mockSalesOrderApi
