/** Purchase order item list board definition tips (aligned with help) */
export const purchaseOrderItemListBoardDefsEn = {
  kpi: {
    approvedVendors: {
      chart: 'Approved vendors',
      dataSource: 'Purchase order items',
      text: 'Distinct vendors in the current set (filter mode is not report approved-only).'
    },
    approvedOrders: {
      chart: 'Approved orders',
      dataSource: 'Purchase order items',
      text: 'Distinct purchase orders in the current set.'
    },
    approvedLines: {
      chart: 'Approved lines',
      dataSource: 'Purchase order items',
      text: 'Purchase order line count in the current set.'
    },
    approvedAmount: {
      chart: 'Approved amount',
      dataSource: 'Purchase order items',
      text: 'Sum of line amounts (USD; original-currency split available). Masked without amount permission.'
    },
    inStockVendors: {
      chart: 'In-stock vendors',
      dataSource: 'Purchase order items',
      text: 'Distinct vendors still holding in-stock qty.'
    },
    inStockLines: {
      chart: 'In-stock lines',
      dataSource: 'Purchase order items',
      text: 'Lines still holding in-stock qty.'
    },
    inStockAmount: {
      chart: 'In-stock amount',
      dataSource: 'Purchase order items',
      text: 'In-stock amount in converted USD. Masked without amount permission.'
    },
    maxStockAge: {
      chart: 'Max stock age',
      dataSource: 'Purchase order items',
      text: 'Maximum in-stock days among those lines.'
    },
    payableVendors: {
      chart: 'Payable vendors',
      dataSource: 'Purchase order items',
      text: 'Distinct vendors with outstanding payable.'
    },
    payableLines: {
      chart: 'Payable lines',
      dataSource: 'Purchase order items',
      text: 'Lines with outstanding payable.'
    },
    payableAmount: {
      chart: 'Payable amount',
      dataSource: 'Purchase order items',
      text: 'Outstanding payable total. Masked without amount permission.'
    }
  },
  trend: {
    orders: {
      chart: 'Approved order trend',
      dataSource: 'Purchase order items',
      text: 'Approved order count per period by order create date (report tab uses approved-only set).'
    },
    lines: {
      chart: 'Approved line trend',
      dataSource: 'Purchase order items',
      text: 'Approved line count per period by order create date.'
    },
    amount: {
      chart: 'Approved line amount trend',
      dataSource: 'Purchase order items',
      text: 'Approved line amount (converted USD) per period by order create date.'
    }
  },
  breakdown: {
    itemStatus: {
      chart: 'Line status',
      dataSource: 'Purchase order items',
      text: 'Lines or amount by purchase line status.'
    },
    paymentRequestProgress: {
      chart: 'Payment request status',
      dataSource: 'Purchase order items',
      text: 'Lines or amount by payment-request progress.'
    },
    paymentProgress: {
      chart: 'Payment progress',
      dataSource: 'Purchase order items',
      text: 'Lines or amount by payment progress.'
    },
    purchaseProgress: {
      chart: 'Purchase progress',
      dataSource: 'Purchase order items',
      text: 'Lines or amount by purchase progress.'
    },
    stockInProgress: {
      chart: 'Stock-in progress',
      dataSource: 'Purchase order items',
      text: 'Lines or amount by stock-in progress.'
    },
    invoiceProgress: {
      chart: 'Invoice progress',
      dataSource: 'Purchase order items',
      text: 'Lines or amount by invoice progress.'
    },
    currency: {
      chart: 'Currency mix',
      dataSource: 'Purchase order items',
      text: 'Amount (or line count) by currency.'
    },
    brandQty: {
      chart: 'Brand qty',
      dataSource: 'Purchase order items',
      text: 'Quantity summed by brand.'
    },
    brandAmount: {
      chart: 'Brand amount (USD)',
      dataSource: 'Purchase order items',
      text: 'Converted USD amount by brand.'
    },
    dateCode: {
      chart: 'Date code / DC',
      dataSource: 'Purchase order items',
      text: 'Line count by date-code text bucket.'
    },
    purchaseUser: {
      chart: 'Buyer (USD)',
      dataSource: 'Purchase order items',
      text: 'Converted USD amount (or line count) by purchase user.'
    }
  },
  rankings: {
    vendorByAmount: {
      chart: 'Top 10 vendors (amount)',
      dataSource: 'Purchase order items',
      text: 'Top 10 vendors by converted USD (or line count); toggle amount / line count.'
    },
    pnByAmount: {
      chart: 'Top 10 MPN (amount)',
      dataSource: 'Purchase order items',
      text: 'Top 10 MPNs by converted USD (or line count); toggle amount / line count.'
    },
    pnByQty: {
      chart: 'Top 10 MPN (qty)',
      dataSource: 'Purchase order items',
      text: 'Top 10 MPNs by quantity; qty column is independent of amount/line toggle.'
    },
    brandByAmount: {
      chart: 'Top 10 brands (amount)',
      dataSource: 'Purchase order items',
      text: 'Top 10 brands by converted USD (or line count); toggle amount / line count.'
    },
    brandByQty: {
      chart: 'Top 10 brands (qty)',
      dataSource: 'Purchase order items',
      text: 'Top 10 brands by quantity; qty column is independent of amount/line toggle.'
    },
    purchaseUserByAmount: {
      chart: 'Top 10 buyers (amount)',
      dataSource: 'Purchase order items',
      text: 'Top 10 purchase users by converted USD (or line count); toggle amount / line count.'
    }
  }
}
