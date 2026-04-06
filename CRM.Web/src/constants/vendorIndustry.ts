/**
 * 供应商「所属行业」可选值（存库/API 使用英文键；展示文案见 locales vendorList.industry.*）
 * 顺序已按中文名称汉语拼音排序（与 zh-CN localeCompare 一致）
 */
export const VENDOR_INDUSTRY_VALUES = [
  'Semiconductors',
  'TestMeasurement',
  'CircuitProtection',
  'WiresCables',
  'CathodePower',
  'ToolsEquipment',
  'IndustrialControl',
  'MechatronicEncoders',
  'ComputerPeripheralsMech',
  'StructuralParts',
  'DevKitsTools',
  'ThermalManagement',
  'NetworkCommDevices',
  'DisplayMarket',
  'IGUS',
  'LedLightingOptoDisplay'
] as const;

export type VendorIndustryValue = (typeof VENDOR_INDUSTRY_VALUES)[number];

/** 与列表/导入允许的行业编码一致（与字典 VendorIndustry 对齐） */
export const VENDOR_INDUSTRY_FILTER_VALUES = VENDOR_INDUSTRY_VALUES;
