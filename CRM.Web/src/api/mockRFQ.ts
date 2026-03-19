// RFQ需求单模拟数据服务
import { ElMessage } from 'element-plus'

// 模拟数据存储
let mockRFQList: any[] = [
  {
    id: 'RFQ001',
    rfqCode: 'RF20240318001',
    customerId: 'C001',
    customerName: '深圳华为技术有限公司',
    contactEmail: 'procurement@huawei.com',
    salesUserId: 'U001',
    salesUserName: '张三',
    rfqType: 1,
    quoteMethod: 4,
    assignMethod: 2,
    industry: '通信设备',
    product: '5G基站芯片',
    targetType: 3,
    importance: 9,
    isLastInquiry: false,
    projectBackground: '新一代5G基站项目，急需芯片样品验证',
    competitor: '中兴、爱立信',
    status: 2,
    itemCount: 3,
    remark: '请优先处理，客户是VIP',
    rfqDate: '2024-03-18',
    createTime: '2024-03-18 09:30:00',
    items: [
      {
        id: 'RFQI001',
        rfqId: 'RFQ001',
        lineNo: 1,
        customerMpn: 'HW-5G-001',
        mpn: 'REF3430QDBVRQ1',
        customerBrand: 'TI',
        brand: 'TEXAS INSTRUMENTS',
        targetPrice: 2.5,
        priceCurrency: 2,
        quantity: 1000,
        productionDate: '2年内',
        alternatives: 'REF3430QDBVR,REF3430QDBVT',
        status: 0
      },
      {
        id: 'RFQI002',
        rfqId: 'RFQ001',
        lineNo: 2,
        customerMpn: 'HW-5G-002',
        mpn: 'STM32F407VGT6',
        customerBrand: 'ST',
        brand: 'STMicroelectronics',
        targetPrice: 18.5,
        priceCurrency: 2,
        quantity: 500,
        productionDate: '2024+',
        alternatives: '',
        status: 0
      },
      {
        id: 'RFQI003',
        rfqId: 'RFQ001',
        lineNo: 3,
        customerMpn: 'HW-5G-003',
        mpn: 'TJA1101AHN/0Z',
        customerBrand: 'NXP',
        brand: 'NXP Semiconductors',
        targetPrice: 320,
        priceCurrency: 2,
        quantity: 200,
        productionDate: '最新批次',
        alternatives: '',
        status: 0
      }
    ]
  },
  {
    id: 'RFQ002',
    rfqCode: 'RF20240318002',
    customerId: 'C002',
    customerName: '小米科技有限公司',
    contactEmail: 'sourcing@xiaomi.com',
    salesUserId: 'U002',
    salesUserName: '李四',
    rfqType: 3,
    quoteMethod: 3,
    assignMethod: 1,
    industry: '消费电子',
    product: '智能手表传感器',
    targetType: 2,
    importance: 7,
    isLastInquiry: true,
    projectBackground: '小米手表4代项目，需要样品测试',
    competitor: '苹果、三星',
    status: 3,
    itemCount: 2,
    remark: '独家需求，不要透露给竞争对手',
    rfqDate: '2024-03-18',
    createTime: '2024-03-18 14:20:00',
    items: [
      {
        id: 'RFQI004',
        rfqId: 'RFQ002',
        lineNo: 1,
        customerMpn: 'XM-SENSOR-001',
        mpn: 'BMI270',
        customerBrand: 'BOSCH',
        brand: 'Bosch Sensortec',
        targetPrice: 380,
        priceCurrency: 1,
        quantity: 200,
        productionDate: '不限',
        alternatives: 'BMI160',
        status: 1
      },
      {
        id: 'RFQI005',
        rfqId: 'RFQ002',
        lineNo: 2,
        customerMpn: 'XM-SENSOR-002',
        mpn: 'BMP388',
        customerBrand: 'BOSCH',
        brand: 'Bosch Sensortec',
        targetPrice: 45,
        priceCurrency: 1,
        quantity: 200,
        productionDate: '不限',
        alternatives: '',
        status: 1
      }
    ]
  },
  {
    id: 'RFQ003',
    rfqCode: 'RF20240319001',
    customerId: 'C003',
    customerName: '比亚迪股份有限公司',
    contactEmail: 'purchase@byd.com',
    salesUserId: 'U001',
    salesUserName: '张三',
    rfqType: 4,
    quoteMethod: 4,
    assignMethod: 3,
    industry: '汽车电子',
    product: '车载电源管理芯片',
    targetType: 1,
    importance: 8,
    isLastInquiry: false,
    projectBackground: '新能源汽车电控系统批量采购',
    competitor: '特斯拉、蔚来',
    status: 0,
    itemCount: 5,
    remark: '比价需求，至少3家供应商报价',
    rfqDate: '2024-03-19',
    createTime: '2024-03-19 10:00:00',
    items: []
  }
]

let nextId = 4

// 模拟RFQ API
export const mockRFQApi = {
  // 获取列表
  async getList(params?: any) {
    await delay(300)
    let result = [...mockRFQList]
    if (params?.keyword) {
      const kw = params.keyword.toLowerCase()
      result = result.filter(r => 
        r.rfqCode.toLowerCase().includes(kw) ||
        r.customerName?.toLowerCase().includes(kw) ||
        r.product?.toLowerCase().includes(kw)
      )
    }
    if (params?.status !== undefined && params?.status !== null) {
      result = result.filter(r => r.status === params.status)
    }
    return { success: true, data: result, total: result.length }
  },

  // 获取详情
  async getById(id: string) {
    await delay(200)
    const rfq = mockRFQList.find(r => r.id === id)
    return { success: true, data: rfq }
  },

  // 创建
  async create(data: any) {
    await delay(500)
    const newRFQ = {
      id: `RFQ${String(nextId++).padStart(3, '0')}`,
      rfqCode: `RF${new Date().toISOString().slice(0, 10).replace(/-/g, '')}${String(Math.random()).slice(2, 5)}`,
      ...data,
      status: 0,
      itemCount: data.items?.length || 0,
      createTime: new Date().toLocaleString('zh-CN')
    }
    mockRFQList.unshift(newRFQ)
    ElMessage.success('需求单创建成功')
    return { success: true, data: newRFQ }
  },

  // 更新
  async update(id: string, data: any) {
    await delay(400)
    const index = mockRFQList.findIndex(r => r.id === id)
    if (index > -1) {
      mockRFQList[index] = { 
        ...mockRFQList[index], 
        ...data,
        itemCount: data.items?.length || mockRFQList[index].itemCount
      }
      ElMessage.success('需求单更新成功')
      return { success: true, data: mockRFQList[index] }
    }
    throw new Error('需求单不存在')
  },

  // 删除
  async delete(id: string) {
    await delay(300)
    const index = mockRFQList.findIndex(r => r.id === id)
    if (index > -1) {
      mockRFQList.splice(index, 1)
      ElMessage.success('需求单删除成功')
      return { success: true }
    }
    throw new Error('需求单不存在')
  },

  // 更新状态
  async updateStatus(id: string, status: number) {
    await delay(200)
    const index = mockRFQList.findIndex(r => r.id === id)
    if (index > -1) {
      mockRFQList[index].status = status
      ElMessage.success('状态更新成功')
      return { success: true }
    }
    throw new Error('需求单不存在')
  },

  // 获取统计
  async getStats() {
    await delay(200)
    return {
      success: true,
      data: {
        total: mockRFQList.length,
        pending: mockRFQList.filter(r => r.status === 0).length,
        processing: mockRFQList.filter(r => r.status === 1 || r.status === 2).length,
        quoted: mockRFQList.filter(r => r.status >= 3).length
      }
    }
  }
}

function delay(ms: number) {
  return new Promise(resolve => setTimeout(resolve, ms))
}

export default mockRFQApi
