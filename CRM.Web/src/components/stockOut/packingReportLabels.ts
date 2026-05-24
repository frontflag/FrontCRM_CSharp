/** Packing List 打印报表固定英文标签（不随 UI 语言切换） */
export const PACKING_REPORT_LABELS = {
  date: 'Date: ',
  packingNo: 'Packing No.: ',
  shipMethod: 'Shipping Method: ',
  noItems: 'No items',
  blankBelow: 'Blank below',
  total: 'Total',
  outboundInspection: 'Outbound Inspection',
  no: 'No.',
  pn: 'PN',
  brand: 'Brand',
  qty: 'Qty',
  upUsd: 'UP (USD)',
  amountUsd: 'Amount (USD)',
  remark: 'Remark',
  item: 'Item',
  result: 'Result',
  remarks: 'Remarks',
  attn: 'Attn: ',
  tel: 'Tel: ',
  shipperSign: 'Shipper (Signature/Stamp)',
  exporterSign: 'Exporter (Signature/Stamp)',
  consigneeSign: 'Consignee (Signature/Stamp)',
  bankDetails: 'Bank details',
  bankName: 'Bank Name: ',
  accountName: 'Account Name: ',
  accountNo: 'Account No.: ',
  bankAddress: 'Bank Address: ',
  currency: 'Currency: ',
  docTitle: 'PACKING LIST',
  invoiceDocTitle: 'INVOICE',
  invoiceNo: 'Invoice No.: ',
  qcInspector: 'Inspector: ',
  qcDate: 'Inspection Date: ',
  qcItems: [
    '(1) Match incoming goods: PN, QTY, DC',
    '(2) Packaging fit for transport: vacuum/ESD bag, fill, stain/damage, reinforcement',
    '(3) Shipment label vs requirements: customized / non-customized',
    '(4) Accompanying docs vs requirements: quality, packing list, booking/shipping',
    '(5) Express waybill vs requirements: correctness of waybill data, match to goods'
  ] as const
} as const

export type InvoiceReportLang = 'zh' | 'en'

/** Invoice 打印报表标签（独立于 UI 语言，由报表页「中文/英文」切换） */
export interface InvoiceReportLabels {
  date: string
  invoiceNo: string
  invoiceDocTitle: string
  billTo: string
  shipTo: string
  no: string
  pn: string
  brand: string
  qty: string
  upUsd: string
  amountUsd: string
  remark: string
  noItems: string
  blankBelow: string
  total: string
  attn: string
  tel: string
  exporterSign: string
  consigneeSign: string
  bankDetails: string
  bankName: string
  accountName: string
  accountNo: string
  bankAddress: string
  currency: string
  swift: string
  iban: string
}

export const INVOICE_REPORT_LABELS_EN: InvoiceReportLabels = {
  date: PACKING_REPORT_LABELS.date,
  invoiceNo: PACKING_REPORT_LABELS.invoiceNo,
  invoiceDocTitle: PACKING_REPORT_LABELS.invoiceDocTitle,
  billTo: 'Bill To',
  shipTo: 'Ship To',
  no: PACKING_REPORT_LABELS.no,
  pn: PACKING_REPORT_LABELS.pn,
  brand: PACKING_REPORT_LABELS.brand,
  qty: PACKING_REPORT_LABELS.qty,
  upUsd: PACKING_REPORT_LABELS.upUsd,
  amountUsd: PACKING_REPORT_LABELS.amountUsd,
  remark: PACKING_REPORT_LABELS.remark,
  noItems: PACKING_REPORT_LABELS.noItems,
  blankBelow: PACKING_REPORT_LABELS.blankBelow,
  total: PACKING_REPORT_LABELS.total,
  attn: PACKING_REPORT_LABELS.attn,
  tel: PACKING_REPORT_LABELS.tel,
  exporterSign: PACKING_REPORT_LABELS.exporterSign,
  consigneeSign: PACKING_REPORT_LABELS.consigneeSign,
  bankDetails: PACKING_REPORT_LABELS.bankDetails,
  bankName: PACKING_REPORT_LABELS.bankName,
  accountName: PACKING_REPORT_LABELS.accountName,
  accountNo: PACKING_REPORT_LABELS.accountNo,
  bankAddress: PACKING_REPORT_LABELS.bankAddress,
  currency: PACKING_REPORT_LABELS.currency,
  swift: 'SWIFT: ',
  iban: 'IBAN: '
}

export const INVOICE_REPORT_LABELS_ZH: InvoiceReportLabels = {
  date: '日期：',
  invoiceNo: '发票号：',
  invoiceDocTitle: '发票',
  billTo: '账单寄送',
  shipTo: '收货地址',
  no: '序号',
  pn: '料号',
  brand: '品牌',
  qty: '数量',
  upUsd: '单价(USD)',
  amountUsd: '金额(USD)',
  remark: '备注',
  noItems: '无明细',
  blankBelow: '以下为空白',
  total: '合计',
  attn: '联系人：',
  tel: '电话：',
  exporterSign: '出口方（签章）',
  consigneeSign: '收货方（签章）',
  bankDetails: '银行资料',
  bankName: '银行名称：',
  accountName: '账户名称：',
  accountNo: '账号：',
  bankAddress: '银行地址：',
  currency: '币别：',
  swift: 'SWIFT：',
  iban: 'IBAN：'
}

export function getInvoiceReportLabels(lang: InvoiceReportLang): InvoiceReportLabels {
  return lang === 'zh' ? INVOICE_REPORT_LABELS_ZH : INVOICE_REPORT_LABELS_EN
}

/** Packing List 打印报表标签（独立于 UI 语言，由报表页「中文/英文」切换） */
export interface PackingReportLabels {
  date: string
  packingNo: string
  shipMethod: string
  docTitle: string
  billTo: string
  shipTo: string
  no: string
  pn: string
  brand: string
  qty: string
  carton: string
  remark: string
  noItems: string
  blankBelow: string
  total: string
  outboundInspection: string
  item: string
  result: string
  remarks: string
  attn: string
  tel: string
  shipperSign: string
  consigneeSign: string
  qcInspector: string
  qcDate: string
  qcItems: readonly string[]
}

export const PACKING_LIST_REPORT_LABELS_EN: PackingReportLabels = {
  date: PACKING_REPORT_LABELS.date,
  packingNo: PACKING_REPORT_LABELS.packingNo,
  shipMethod: PACKING_REPORT_LABELS.shipMethod,
  docTitle: PACKING_REPORT_LABELS.docTitle,
  billTo: 'Bill To',
  shipTo: 'Ship To',
  no: PACKING_REPORT_LABELS.no,
  pn: PACKING_REPORT_LABELS.pn,
  brand: PACKING_REPORT_LABELS.brand,
  qty: PACKING_REPORT_LABELS.qty,
  carton: 'Carton',
  remark: PACKING_REPORT_LABELS.remark,
  noItems: PACKING_REPORT_LABELS.noItems,
  blankBelow: PACKING_REPORT_LABELS.blankBelow,
  total: PACKING_REPORT_LABELS.total,
  outboundInspection: PACKING_REPORT_LABELS.outboundInspection,
  item: PACKING_REPORT_LABELS.item,
  result: PACKING_REPORT_LABELS.result,
  remarks: PACKING_REPORT_LABELS.remarks,
  attn: PACKING_REPORT_LABELS.attn,
  tel: PACKING_REPORT_LABELS.tel,
  shipperSign: PACKING_REPORT_LABELS.shipperSign,
  consigneeSign: PACKING_REPORT_LABELS.consigneeSign,
  qcInspector: PACKING_REPORT_LABELS.qcInspector,
  qcDate: PACKING_REPORT_LABELS.qcDate,
  qcItems: PACKING_REPORT_LABELS.qcItems
}

export const PACKING_LIST_REPORT_LABELS_ZH: PackingReportLabels = {
  date: '日期：',
  packingNo: '装箱单号：',
  shipMethod: '出货方式：',
  docTitle: '装箱单',
  billTo: '账单寄送',
  shipTo: '收货地址',
  no: '序号',
  pn: '料号',
  brand: '品牌',
  qty: '数量',
  carton: '箱号',
  remark: '备注',
  noItems: '无明细',
  blankBelow: '以下为空白',
  total: '合计',
  outboundInspection: '出库检验',
  item: '检验项目',
  result: '结果',
  remarks: '备注',
  attn: '联系人：',
  tel: '电话：',
  shipperSign: '发货方（签章）',
  consigneeSign: '收货方（签章）',
  qcInspector: '检验员：',
  qcDate: '检验日期：',
  qcItems: [
    '（1）是否与来货相符：PN、QTY、DC',
    '（2）包装是否符合运输条件：真空/静电袋、填充、污损、加固',
    '（3）出货标签是否与要求相符：定制、非定制',
    '（4）随货单据是否与要求相符：品质类、箱单类、订舱类',
    '（5）出货快递单与要求是否相符：快递单本身信息是否正确、是否与货物相符'
  ]
}

export function getPackingReportLabels(lang: InvoiceReportLang): PackingReportLabels {
  return lang === 'zh' ? PACKING_LIST_REPORT_LABELS_ZH : PACKING_LIST_REPORT_LABELS_EN
}
