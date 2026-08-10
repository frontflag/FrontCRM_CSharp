/** Sales analytics panel definition tips (aligned with help) */
export const salesAnalyticsDefsEn = {
  snapshot: {
    rfqItems: {
      chart: 'RFQ line items',
      dataSource: 'RFQ items',
      text: 'Count of RFQ line items created in the date range.'
    },
    rfqCustomers: {
      chart: 'RFQ customers',
      dataSource: 'RFQ items',
      text: 'Distinct customers on those RFQ lines.'
    },
    conversion: {
      chart: 'RFQ→Sales conversion',
      dataSource: 'RFQ items',
      text: 'RFQ lines linked to a sales order line ÷ RFQ lines × 100%. Not by customer or order.'
    },
    soItems: {
      chart: 'Sales order lines',
      dataSource: 'Sales order items',
      text: 'Valid sales order lines created in the range (including not yet approved).'
    },
    soCustomers: {
      chart: 'Sales customers',
      dataSource: 'Sales orders',
      text: 'Distinct customers on sales orders in the range (including not yet approved).'
    },
    amount: {
      chart: 'Approved order amount',
      dataSource: 'Sales orders',
      text: 'Sum of convert_total for orders created in range and approved or above. Draft/pending excluded.'
    },
    stockOut: {
      chart: 'Stock-out amount',
      dataSource: 'Sales order items',
      text: 'Converted USD (actual stock-out qty × convert price). Use “view local currency” for original-currency split.'
    },
    received: {
      chart: 'Received amount',
      dataSource: 'Sales order items',
      text: 'Converted USD total; original-currency split via “view local currency”. Do not sum original currencies across codes.'
    }
  },
  trend: {
    amount: {
      chart: 'Approved amount trend',
      dataSource: 'Sales orders',
      text: 'Converted USD approved amount per period by order create date.'
    },
    stockOut: {
      chart: 'Stock-out amount trend',
      dataSource: 'Sales order items',
      text: 'Converted USD stock-out amount per period by order create date.'
    },
    received: {
      chart: 'Received amount trend',
      dataSource: 'Sales order items',
      text: 'Converted USD received amount per period by order create date.'
    },
    rfqCustomers: {
      chart: 'RFQ customers trend',
      dataSource: 'RFQ items',
      text: 'Distinct customers per period by RFQ line create date.'
    },
    salesCustomers: {
      chart: 'Sales customers trend',
      dataSource: 'Sales orders',
      text: 'Distinct customers per period by order create date.'
    },
    rfq: {
      chart: 'RFQ lines trend',
      dataSource: 'RFQ items',
      text: 'RFQ line count per period by create date.'
    },
    items: {
      chart: 'Order lines trend',
      dataSource: 'Sales order items',
      text: 'Valid sales order lines per period by order create date.'
    },
    conversion: {
      chart: 'Conversion trend',
      dataSource: 'RFQ items',
      text: 'Same algorithm as overview conversion, computed per period.'
    },
    receivable: {
      chart: 'Receivable trend',
      dataSource: 'Sales order items',
      text: 'Open receipt amount on approved lines, converted to USD per period. Not the todo open-receivable KPI.'
    }
  },
  breakdown: {
    orderStatus: {
      chart: 'Order status',
      dataSource: 'Sales orders',
      text: 'Non-cancelled/failed orders in range, sum convert_total by status; by order count when amounts masked. Includes unapproved.'
    },
    currency: {
      chart: 'Currency mix (approved)',
      dataSource: 'Sales orders',
      text: 'Approved orders only: converted USD share by original currency; by order count when amounts masked.'
    },
    pipelineStage: {
      chart: 'Pipeline stage',
      dataSource: 'Sales order items',
      text: 'Each approved line in one bottleneck stage: pending purchase → stock-in → stock-out → receipt → invoice → done.'
    },
    stockOutProgress: {
      chart: 'Stock-out progress',
      dataSource: 'Sales order items',
      text: 'Valid lines in range counted as pending / partial / complete stock-out (approval not required).'
    }
  },
  rankings: {
    primary: {
      chart: 'Sales Top 10',
      dataSource: 'Sales orders',
      text: 'Approved orders in range; sort by converted USD when allowed, else by order count. Company: departments; department: salespeople.'
    }
  },
  customer: {
    approvedCustomers: {
      chart: 'Approved customers',
      dataSource: 'Sales orders',
      text: 'Distinct customers with approved orders in the range.'
    },
    repeatCustomers: {
      chart: 'Repeat customers',
      dataSource: 'Sales orders',
      text: 'Approved customers with ≥ 2 approved orders.'
    },
    customerType: {
      chart: 'Customer type',
      dataSource: 'Sales orders',
      text: 'Mix by converted USD approved amount; by order count when amounts masked.'
    },
    customerLevel: {
      chart: 'Customer level',
      dataSource: 'Sales orders',
      text: 'Mix by converted USD approved amount; by order count when amounts masked.'
    },
    customerIndustry: {
      chart: 'Customer industry',
      dataSource: 'Sales orders',
      text: 'Mix by converted USD approved amount; by order count when amounts masked.'
    },
    byAmount: {
      chart: 'Customer Top 10 (amount)',
      dataSource: 'Sales orders',
      text: 'Top customers by converted USD approved amount in the range.'
    },
    byOrderCount: {
      chart: 'Customer Top 10 (orders)',
      dataSource: 'Sales orders',
      text: 'Top customers by approved order count in the range.'
    },
    byRepeat: {
      chart: 'Customer Top 10 (repeat orders)',
      dataSource: 'Sales orders',
      text: 'Repeat order count = approved orders − 1; ranks repeat intensity.'
    }
  }
} as const
