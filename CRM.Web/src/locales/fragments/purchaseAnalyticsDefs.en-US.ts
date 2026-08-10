export const purchaseAnalyticsDefsEn = {
  todo: {
    payable: {
      chart: 'Payable amount',
      dataSource: 'PO items',
      text: 'Open payable on valid PO lines in scope, converted to USD per line. Not limited by the date range.'
    },
    pendingStockIn: {
      chart: 'Pending stock-in lines',
      dataSource: 'PO items',
      text: 'Lines still pending or partial stock-in. Line count, not orders. Not limited by the date range.'
    }
  },
  snapshot: {
    quoteItems: {
      chart: 'Quote lines',
      dataSource: 'Quotes',
      text: 'Quote line items created in the date range.'
    },
    quoteVendors: {
      chart: 'Quote vendors',
      dataSource: 'Quotes',
      text: 'Distinct vendors on those quotes.'
    },
    conversion: {
      chart: 'Quote→PO conversion',
      dataSource: 'Quotes',
      text: 'Quote lines linked to a PO line ÷ quote lines × 100%.'
    },
    poItems: {
      chart: 'PO lines',
      dataSource: 'PO items',
      text: 'Valid PO lines created in the range (including not yet approved).'
    },
    poVendors: {
      chart: 'PO vendors',
      dataSource: 'Purchase orders',
      text: 'Distinct vendors on POs in the range (including not yet approved).'
    },
    amount: {
      chart: 'Approved PO amount',
      dataSource: 'Purchase orders',
      text: 'Sum of convert_total for POs created in range and approved or above.'
    },
    stockIn: {
      chart: 'Stock-in amount',
      dataSource: 'PO items',
      text: 'Approved lines: stock-in qty × convert price, summed in USD.'
    },
    paid: {
      chart: 'Paid amount',
      dataSource: 'PO items',
      text: 'Paid amount on approved lines, converted to USD per line.'
    }
  },
  trend: {
    amount: {
      chart: 'Approved amount trend',
      dataSource: 'Purchase orders',
      text: 'Converted USD approved PO amount per period.'
    },
    stockIn: {
      chart: 'Stock-in amount trend',
      dataSource: 'PO items',
      text: 'Converted USD stock-in amount per period on approved lines.'
    },
    paid: {
      chart: 'Paid amount trend',
      dataSource: 'PO items',
      text: 'Converted USD paid amount per period on approved lines.'
    },
    quoteVendors: {
      chart: 'Quote vendors trend',
      dataSource: 'Quotes',
      text: 'Distinct vendors per period by quote create date.'
    },
    poVendors: {
      chart: 'PO vendors trend',
      dataSource: 'Purchase orders',
      text: 'Distinct vendors per period by PO create date.'
    },
    quote: {
      chart: 'Quote lines trend',
      dataSource: 'Quotes',
      text: 'Quote line count per period by create date.'
    },
    items: {
      chart: 'PO lines trend',
      dataSource: 'PO items',
      text: 'Valid PO lines per period by PO create date.'
    },
    conversion: {
      chart: 'Conversion trend',
      dataSource: 'Quotes',
      text: 'Same algorithm as overview conversion, per period.'
    },
    payable: {
      chart: 'Payable trend',
      dataSource: 'PO items',
      text: 'Open payable on approved lines in each period, converted to USD. Not the todo payable stock.'
    }
  },
  breakdown: {
    orderStatus: {
      chart: 'Order status',
      dataSource: 'Purchase orders',
      text: 'Non-cancelled/failed POs in range by status amount; by order count when masked. Includes unapproved.'
    },
    currency: {
      chart: 'Currency mix (approved)',
      dataSource: 'Purchase orders',
      text: 'Approved POs only: amount share by original currency; by order count when masked.'
    },
    pipelineStage: {
      chart: 'Pipeline stage',
      dataSource: 'PO items',
      text: 'Each approved line in one bottleneck stage (see chart legend).'
    },
    stockInProgress: {
      chart: 'Stock-in progress',
      dataSource: 'PO items',
      text: 'Valid lines in range counted by stock-in progress.'
    }
  },
  rankings: {
    primary: {
      chart: 'Purchase Top 10',
      dataSource: 'Purchase orders',
      text: 'Approved POs in range; sort by converted USD when allowed, else by order count. Company: departments; department: buyers.'
    }
  },
  vendor: {
    approvedVendors: {
      chart: 'Approved vendors',
      dataSource: 'Purchase orders',
      text: 'Distinct vendors with approved POs in the range.'
    },
    repeatVendors: {
      chart: 'Repeat vendors',
      dataSource: 'Purchase orders',
      text: 'Approved vendors with ≥ 2 approved orders.'
    },
    vendorCredit: {
      chart: 'Vendor identity',
      dataSource: 'Purchase orders',
      text: 'Mix by converted USD approved amount; by order count when masked.'
    },
    vendorLevel: {
      chart: 'Vendor level',
      dataSource: 'Purchase orders',
      text: 'Mix by converted USD approved amount; by order count when masked.'
    },
    vendorIndustry: {
      chart: 'Vendor industry',
      dataSource: 'Purchase orders',
      text: 'Mix by converted USD approved amount; by order count when masked.'
    },
    byAmount: {
      chart: 'Vendor Top 10 (amount)',
      dataSource: 'Purchase orders',
      text: 'Top vendors by converted USD approved amount.'
    },
    byOrderCount: {
      chart: 'Vendor Top 10 (orders)',
      dataSource: 'Purchase orders',
      text: 'Top vendors by approved order count.'
    },
    byRepeat: {
      chart: 'Vendor Top 10 (repeat orders)',
      dataSource: 'Purchase orders',
      text: 'Repeat order count = approved orders − 1.'
    }
  }
} as const
