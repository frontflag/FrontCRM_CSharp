// Quote报价单模拟数据服务
import { ElMessage } from 'element-plus'

// 模拟数据存储
let mockQuoteList: any[] = [
  {
    id: 'QT001',
    quoteCode: 'QT20240318001',
    rfqId: 'RFQ001',
    rfqItemId: 'RFQI001',
    mpn: 'REF3430QDBVRQ1',
    customerId: 'C001',
    salesUserId: 'U001',
    salesUserName: '张三',
    purchaseUserId: 'U003',
    purchaseUserName: '王五',
    quoteDate: '2024-03-18',
    status: 2,
    remark: '多家供应商报价对比中',
    createTime: '2024-03-18 11:00:00',
    items: [
      {
        id: 'QTI001',
        quoteId: 'QT001',
        vendorId: 'V001',
        vendorName: 'Digikey',
        vendorCode: 'DIG001',
        contactName: 'International Sales',
        priceType: '现货价',
        mpn: 'REF3430QDBVRQ1',
        brand: 'TEXAS INSTRUMENTS',
        brandOrigin: '美国',
        dateCode: '2024+',
        leadTime: '现货',
        labelType: 0,
        waferOrigin: 0,
        packageOrigin: 1,
        freeShipping: true,
        currency: 2,
        quantity: 1000,
        unitPrice: 2.267910,
        convertedPrice: 16.35,
        minPackageQty: 3000,
        minPackageUnit: 'Reel',
        stockQty: 15000,
        moq: 1000,
        remark: '散料',
        status: 0
      },
      {
        id: 'QTI002',
        quoteId: 'QT001',
        vendorId: 'V002',
        vendorName: 'Mouser',
        vendorCode: 'MOU001',
        contactName: 'Sales Team',
        priceType: '现货价',
        mpn: 'REF3430QDBVRQ1',
        brand: 'TEXAS INSTRUMENTS',
        brandOrigin: '美国',
        dateCode: '2024+',
        leadTime: '3-5天',
        labelType: 1,
        waferOrigin: 0,
        packageOrigin: 0,
        freeShipping: false,
        currency: 2,
        quantity: 1000,
        unitPrice: 2.189500,
        convertedPrice: 15.76,
        minPackageQty: 3000,
        minPackageUnit: 'Reel',
        stockQty: 8000,
        moq: 1000,
        remark: '',
        status: 0
      }
    ]
  },
  {
    id: 'QT002',
    quoteCode: 'QT20240318002',
    rfqId: 'RFQ001',
    rfqItemId: 'RFQI002',
    mpn: 'STM32F407VGT6',
    customerId: 'C001',
    salesUserId: 'U001',
    salesUserName: '张三',
    purchaseUserId: 'U003',
    purchaseUserName: '王五',
    quoteDate: '2024-03-18',
    status: 4,
    remark: '客户已接受此报价',
    createTime: '2024-03-18 16:30:00',
    items: [
      {
        id: 'QTI003',
        quoteId: 'QT002',
        vendorId: 'V003',
        vendorName: 'Arrow',
        vendorCode: 'ARR001',
        contactName: 'APAC Sales',
        priceType: '期货价',
        mpn: 'STM32F407VGT6',
        brand: 'STMicroelectronics',
        brandOrigin: '瑞士',
        dateCode: '2024+',
        leadTime: '8-10周',
        labelType: 2,
        waferOrigin: 1,
        packageOrigin: 1,
        freeShipping: true,
        currency: 2,
        quantity: 500,
        unitPrice: 16.850000,
        convertedPrice: 121.32,
        minPackageQty: 1000,
        minPackageUnit: 'Tray',
        stockQty: 0,
        moq: 500,
        remark: '期货订单',
        status: 0
      }
    ]
  },
  {
    id: 'QT003',
    quoteCode: 'QT20240319001',
    rfqId: 'RFQ002',
    rfqItemId: 'RFQI004',
    mpn: 'BMI270',
    customerId: 'C002',
    salesUserId: 'U002',
    salesUserName: '李四',
    purchaseUserId: 'U004',
    purchaseUserName: '赵六',
    quoteDate: '2024-03-19',
    status: 1,
    remark: '独家需求，谨慎报价',
    createTime: '2024-03-19 09:00:00',
    items: [
      {
        id: 'QTI004',
        quoteId: 'QT003',
        vendorId: 'V004',
        vendorName: 'Avnet',
        vendorCode: 'AVN001',
        contactName: 'China Sales',
        priceType: '样品价',
        mpn: 'BMI270',
        brand: 'Bosch Sensortec',
        brandOrigin: '德国',
        dateCode: '最新',
        leadTime: '现货',
        labelType: 0,
        waferOrigin: 1,
        packageOrigin: 1,
        freeShipping: true,
        currency: 1,
        quantity: 200,
        unitPrice: 365.00,
        convertedPrice: 365.00,
        minPackageQty: 100,
        minPackageUnit: 'Tube',
        stockQty: 500,
        moq: 100,
        remark: '样品订单',
        status: 0
      }
    ]
  }
]

let nextId = 4

// 模拟Quote API
export const mockQuoteApi = {
  // 获取列表
  async getList(params?: any) {
    await delay(300)
    let result = [...mockQuoteList]
    if (params?.keyword) {
      const kw = params.keyword.toLowerCase()
      result = result.filter(q => 
        q.quoteCode.toLowerCase().includes(kw) ||
        q.mpn?.toLowerCase().includes(kw) ||
        q.customerName?.toLowerCase().includes(kw)
      )
    }
    if (params?.status !== undefined && params?.status !== null) {
      result = result.filter(q => q.status === params.status)
    }
    return { success: true, data: result, total: result.length }
  },

  // 获取详情
  async getById(id: string) {
    await delay(200)
    const quote = mockQuoteList.find(q => q.id === id)
    return { success: true, data: quote }
  },

  // 创建
  async create(data: any) {
    await delay(500)
    const newQuote = {
      id: `QT${String(nextId++).padStart(3, '0')}`,
      quoteCode: `QT${new Date().toISOString().slice(0, 10).replace(/-/g, '')}${String(Math.random()).slice(2, 5)}`,
      ...data,
      status: 0,
      createTime: new Date().toLocaleString('zh-CN')
    }
    mockQuoteList.unshift(newQuote)
    ElMessage.success('报价单创建成功')
    return { success: true, data: newQuote }
  },

  // 更新
  async update(id: string, data: any) {
    await delay(400)
    const index = mockQuoteList.findIndex(q => q.id === id)
    if (index > -1) {
      mockQuoteList[index] = { ...mockQuoteList[index], ...data }
      ElMessage.success('报价单更新成功')
      return { success: true, data: mockQuoteList[index] }
    }
    throw new Error('报价单不存在')
  },

  // 删除
  async delete(id: string) {
    await delay(300)
    const index = mockQuoteList.findIndex(q => q.id === id)
    if (index > -1) {
      mockQuoteList.splice(index, 1)
      ElMessage.success('报价单删除成功')
      return { success: true }
    }
    throw new Error('报价单不存在')
  },

  // 更新状态
  async updateStatus(id: string, status: number) {
    await delay(200)
    const index = mockQuoteList.findIndex(q => q.id === id)
    if (index > -1) {
      mockQuoteList[index].status = status
      ElMessage.success('状态更新成功')
      return { success: true }
    }
    throw new Error('报价单不存在')
  }
}

function delay(ms: number) {
  return new Promise(resolve => setTimeout(resolve, ms))
}

export default mockQuoteApi
