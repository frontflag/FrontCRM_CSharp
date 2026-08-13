/** 与后端 CRM.Core.Constants.BusinessLogTypes 一致 */
export const BusinessLogTypes = {
  Customer: 'Customer',
  CustomerContact: 'CustomerContact',
  CustomerAddress: 'CustomerAddress',
  CustomerBank: 'CustomerBank',
  CustomerContactHistory: 'CustomerContactHistory',

  Vendor: 'Vendor',
  VendorContact: 'VendorContact',
  VendorAddress: 'VendorAddress',
  VendorBank: 'VendorBank',
  VendorContactHistory: 'VendorContactHistory',

  Quote: 'Quote',
  QuoteItem: 'QuoteItem',
  Rfq: 'Rfq',
  RfqItem: 'RfqItem',
  SalesOrder: 'SalesOrder',
  SellOrderItem: 'SellOrderItem',
  PurchaseOrder: 'PurchaseOrder',
  PurchaseOrderItem: 'PurchaseOrderItem',
  PurchaseRequisition: 'PurchaseRequisition',

  InventoryStock: 'InventoryStock',
  InventoryStockItem: 'InventoryStockItem',
  PickingTask: 'PickingTask',
  StockIn: 'StockIn',
  StockInBatch: 'StockInBatch',
  StockOutBatch: 'StockOutBatch',
  StockOut: 'StockOut',
  Packing: 'Packing',
  BatchReconciliation: 'BatchReconciliation',

  QcInspection: 'QcInspection',
  CustomsDeclaration: 'CustomsDeclaration',
  CustomsBroker: 'CustomsBroker',
  FreightForwarderCompany: 'FreightForwarderCompany',

  FinancePayment: 'FinancePayment',
  FinanceReceipt: 'FinanceReceipt',
  FinanceSellInvoice: 'FinanceSellInvoice',
  FinancePurchaseInvoice: 'FinancePurchaseInvoice',
  FinanceReceivable: 'FinanceReceivable',
  FinanceCustomerAdvance: 'FinanceCustomerAdvance',
  FinanceFreightForwarderPayable: 'FinanceFreightForwarderPayable',

  Document: 'Document'
} as const

export type BusinessLogType = (typeof BusinessLogTypes)[keyof typeof BusinessLogTypes]
