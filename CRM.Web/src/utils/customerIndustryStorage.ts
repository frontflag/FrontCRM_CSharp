import { buildCustomerFormDictFallback } from '@/constants/customerDictFallback'

const fallbackIndustryRows = buildCustomerFormDictFallback().CustomerIndustry

/** English code → 库表/筛选统一使用的中文名称（与字典 label 一致） */
export const CUSTOMER_INDUSTRY_CODE_TO_LABEL: Record<string, string> = Object.fromEntries(
  fallbackIndustryRows.map((x) => [x.code, x.label])
)

const LABEL_SET = new Set(fallbackIndustryRows.map((x) => x.label))

/**
 * Excel/旧链接等：中文别名 → English code（与历史导入映射一致）
 * 再经 {@link CUSTOMER_INDUSTRY_CODE_TO_LABEL} 转为字典标准中文 label。
 */
export const CUSTOMER_INDUSTRY_CN_ALIAS_TO_CODE: Record<string, string> = {
  金融设备: 'FinanceEquipment',
  通讯: 'Telecom',
  轨道交通: 'RailTransit',
  航空航天: 'Aerospace',
  网络安全: 'CyberSecurity',
  电竞: 'Esports',
  电源: 'PowerSupply',
  电子元器件贸易: 'ElectronicComponentsTrading',
  电子元器件制造: 'ElectronicComponentsManufacturing',
  电动工具: 'PowerTools',
  电力电气: 'PowerElectrical',
  物联网: 'IoT',
  消费电子: 'ConsumerElectronics',
  机器人: 'Robotics',
  智能安防: 'SmartSecurity',
  智慧城市: 'SmartCity',
  无人机: 'UAV',
  新能源汽车: 'NewEnergyVehicles',
  新能源: 'NewEnergy',
  工业控制: 'IndustrialControl',
  医疗设备: 'MedicalEquipment',
  军工: 'DefenseMilitary',
  传统车辆: 'TraditionalVehicles',
  仪器仪表: 'Instrumentation',
  人工智能: 'ArtificialIntelligence',
  '云计算IDC': 'CloudComputingIDC',
  制造业: 'Manufacturing',
  '科技/IT': 'Technology',
  '贸易/零售': 'Trading',
  '建筑/工程': 'Construction',
  医疗: 'Healthcare',
  教育: 'Education',
  金融: 'Finance',
  其他: 'Other'
}

/**
 * 将任意来源（英文 code、标准中文 label、常见中文别名）转为写入 customerinfo.industry 的中文 label。
 */
export function industryCellToStorageLabel(raw: string): string {
  const s = raw.trim()
  if (!s) return ''
  const fromCode = CUSTOMER_INDUSTRY_CODE_TO_LABEL[s]
  if (fromCode) return fromCode
  if (LABEL_SET.has(s)) return s
  const code = CUSTOMER_INDUSTRY_CN_ALIAS_TO_CODE[s]
  if (code && CUSTOMER_INDUSTRY_CODE_TO_LABEL[code]) return CUSTOMER_INDUSTRY_CODE_TO_LABEL[code]
  return s
}

/** 列表 URL query：旧书签中的英文 code 转为与库表一致的中文后再参与筛选 */
export function normalizeIndustryQueryParam(raw: string | undefined): string | undefined {
  if (raw == null || raw === '') return undefined
  const v = industryCellToStorageLabel(raw)
  return v || undefined
}
