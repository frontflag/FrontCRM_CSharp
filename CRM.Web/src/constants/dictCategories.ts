/** 与后端 DictCategories 一致，用于字典管理页联动筛选 */
export const DICT_CUSTOMER_CATEGORIES = [
  'CustomerType',
  'CustomerLevel',
  'CustomerIndustry',
  'CustomerTaxRate',
  'CustomerInvoiceType'
] as const;

export const DICT_VENDOR_CATEGORIES = [
  'VendorIndustry',
  'VendorLevel',
  'VendorIdentity',
  'VendorPaymentMethod'
] as const;

/** 物料业务：控件「生产日期」对应 Category MaterialProductionDate */
export const DICT_MATERIAL_CATEGORIES = ['MaterialProductionDate'] as const;

/** 物流业务：到货通知「来货方式」「快递方式」 */
export const DICT_LOGISTICS_CATEGORIES = ['LogisticsArrivalMethod', 'LogisticsExpressMethod'] as const;

export type DictBizKind = 'customer' | 'vendor' | 'material' | 'logistics';
