import type { VendorFormDictResponse } from '@/api/dictionary';

/** 与后端 SysDictCustomerSeedRows 一致（API 不可用时） */
export function buildCustomerFormDictFallback(): VendorFormDictResponse {
  return {
    CustomerType: [
      { code: '1', label: 'OEM' },
      { code: '2', label: 'ODM' },
      { code: '3', label: '终端' },
      { code: '4', label: 'IDH' },
      { code: '5', label: '贸易商' },
      { code: '6', label: '代理商' },
      { code: '7', label: 'EMS' },
      { code: '8', label: '非行业' },
      { code: '9', label: '科研机构' },
      { code: '10', label: '供应链' },
      { code: '11', label: '原厂' }
    ],
    CustomerLevel: [
      { code: 'D', label: 'D级' },
      { code: 'C', label: 'C级' },
      { code: 'B', label: 'B级' },
      { code: 'BPO', label: 'BPO' },
      { code: 'VIP', label: 'VIP' },
      { code: 'VPO', label: 'VPO' }
    ],
    CustomerIndustry: [
      { code: 'FinanceEquipment', label: '金融设备' },
      { code: 'Telecom', label: '通讯' },
      { code: 'RailTransit', label: '轨道交通' },
      { code: 'Aerospace', label: '航空航天' },
      { code: 'CyberSecurity', label: '网络安全' },
      { code: 'Esports', label: '电竞' },
      { code: 'PowerSupply', label: '电源' },
      { code: 'ElectronicComponentsTrading', label: '电子元器件贸易' },
      { code: 'ElectronicComponentsManufacturing', label: '电子元器件制造' },
      { code: 'PowerTools', label: '电动工具' },
      { code: 'PowerElectrical', label: '电力电气' },
      { code: 'IoT', label: '物联网' },
      { code: 'ConsumerElectronics', label: '消费电子' },
      { code: 'Robotics', label: '机器人' },
      { code: 'SmartSecurity', label: '智能安防' },
      { code: 'SmartCity', label: '智慧城市' },
      { code: 'UAV', label: '无人机' },
      { code: 'NewEnergyVehicles', label: '新能源汽车' },
      { code: 'NewEnergy', label: '新能源' },
      { code: 'IndustrialControl', label: '工业控制' },
      { code: 'MedicalEquipment', label: '医疗设备' },
      { code: 'DefenseMilitary', label: '军工' },
      { code: 'TraditionalVehicles', label: '传统车辆' },
      { code: 'Instrumentation', label: '仪器仪表' },
      { code: 'ArtificialIntelligence', label: '人工智能' },
      { code: 'CloudComputingIDC', label: '云计算IDC' },
      { code: 'Manufacturing', label: '制造业' },
      { code: 'Trading', label: '贸易/零售' },
      { code: 'Technology', label: '科技/IT' },
      { code: 'Construction', label: '建筑/工程' },
      { code: 'Healthcare', label: '医疗/健康' },
      { code: 'Education', label: '教育' },
      { code: 'Finance', label: '金融' },
      { code: 'Other', label: '其他' }
    ],
    CustomerTaxRate: [
      { code: '0', label: '0%' },
      { code: '1', label: '1%' },
      { code: '3', label: '3%' },
      { code: '6', label: '6%' },
      { code: '9', label: '9%' },
      { code: '13', label: '13%' }
    ],
    CustomerInvoiceType: [
      { code: '0', label: '无需开票' },
      { code: '1', label: '增值税专用发票' },
      { code: '2', label: '增值税普通发票' },
      { code: '3', label: '电子发票' }
    ]
  };
}
