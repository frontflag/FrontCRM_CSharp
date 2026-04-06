import { VENDOR_LEVEL_OPTIONS, VENDOR_IDENTITY_OPTIONS } from '@/constants/vendorEnums';
import { VENDOR_INDUSTRY_FILTER_VALUES } from '@/constants/vendorIndustry';
import type { VendorFormDictResponse } from '@/api/dictionary';

/** 与 zh-CN vendorList.industry 一致（API 不可用时中文下拉/展示） */
const INDUSTRY_ZH: Record<string, string> = {
  Semiconductors: '半导体',
  TestMeasurement: '测试和测量',
  CircuitProtection: '电路保护',
  WiresCables: '电线及电缆',
  CathodePower: '负极、电源',
  ToolsEquipment: '工具及设备',
  IndustrialControl: '工控',
  MechatronicEncoders: '机电编码器',
  ComputerPeripheralsMech: '计算机外设、机电',
  StructuralParts: '结构件',
  DevKitsTools: '开发套件和工具',
  ThermalManagement: '热管理',
  NetworkCommDevices: '网络通讯器件',
  DisplayMarket: '显示市场',
  IGUS: 'IGUS',
  LedLightingOptoDisplay: 'LED照明、光电设备及显示器',
  Electronics: '电子/半导体',
  Machinery: '机械/设备',
  Chemical: '化工/材料',
  Textile: '纺织/服装',
  Food: '食品/农业',
  Construction: '建筑/工程',
  Trading: '贸易/零售',
  Technology: '科技/IT',
  Healthcare: '医疗/健康',
  Other: '其他'
};

export function buildVendorFormDictFallback(): VendorFormDictResponse {
  const industry = VENDOR_INDUSTRY_FILTER_VALUES.map((code) => ({
    code,
    label: INDUSTRY_ZH[code] ?? code
  }));

  return {
    VendorIndustry: industry,
    VendorLevel: VENDOR_LEVEL_OPTIONS.map((o) => ({ code: String(o.value), label: o.label })),
    VendorIdentity: VENDOR_IDENTITY_OPTIONS.map((o) => ({ code: String(o.value), label: o.label })),
    VendorPaymentMethod: [
      { code: 'Prepaid', label: '预付款' },
      { code: 'COD', label: '货到付款' },
      { code: 'Monthly', label: '月结' },
      { code: 'Credit', label: '账期' },
      { code: 'TT', label: '电汇' },
      { code: 'LC', label: '信用证' }
    ]
  };
}
