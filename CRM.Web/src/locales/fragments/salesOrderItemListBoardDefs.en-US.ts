/** Sales order item list board definition tips (aligned with help) */
export const salesOrderItemListBoardDefsEn = {
  kpi: {
    approvedCustomers: {
      chart: 'Customers',
      dataSource: 'Sales order items',
      text: 'Distinct customers in the current set.'
    },
    approvedOrders: {
      chart: 'Orders',
      dataSource: 'Sales order items',
      text: 'Distinct sales orders in the current set.'
    },
    approvedLines: {
      chart: 'Lines',
      dataSource: 'Sales order items',
      text: 'Sales order line count in the current set.'
    },
    approvedAmount: {
      chart: 'Amount',
      dataSource: 'Sales order items',
      text: 'Sum of line amounts (converted USD; original-currency split available). Masked without amount permission.'
    },
    purchaseProfit: {
      chart: 'Purchase profit',
      dataSource: 'Sales order items',
      text: 'Sum of stored purchase profit on lines. Masked without amount permission.'
    },
    outboundProfit: {
      chart: 'Outbound profit',
      dataSource: 'Sales order items',
      text: 'Sum of stored outbound profit on lines. Masked without amount permission.'
    },
    inStockCustomers: {
      chart: 'In-stock customers',
      dataSource: 'Sales order items',
      text: 'Distinct customers still holding in-stock qty.'
    },
    inStockLines: {
      chart: 'In-stock lines',
      dataSource: 'Sales order items',
      text: 'Lines still holding in-stock qty.'
    },
    inStockAmount: {
      chart: 'In-stock amount',
      dataSource: 'Sales order items',
      text: 'In-stock amount in converted USD. Masked without amount permission.'
    },
    maxStockAge: {
      chart: 'Max stock age',
      dataSource: 'Sales order items',
      text: 'Maximum in-stock days among those lines.'
    },
    receivableCustomers: {
      chart: 'Receivable customers',
      dataSource: 'Sales order items',
      text: 'Distinct customers with outstanding receivable on order lines (may differ from finance AR ledger).'
    },
    receivableLines: {
      chart: 'Receivable lines',
      dataSource: 'Sales order items',
      text: 'Lines with outstanding receivable.'
    },
    receivableAmount: {
      chart: 'Receivable amount',
      dataSource: 'Sales order items',
      text: 'Outstanding receivable total. Masked without amount permission.'
    },
    maxReceivableAge: {
      chart: 'Max receivable age',
      dataSource: 'Sales order items',
      text: 'Maximum receivable aging (days) in the set.'
    }
  },
  trend: {
    orders: {
      chart: 'Order trend',
      dataSource: 'Sales order items',
      text: 'Approved order count per period by order create date (report tab uses approved-only set).'
    },
    lines: {
      chart: 'Line trend',
      dataSource: 'Sales order items',
      text: 'Approved line count per period by order create date.'
    },
    amount: {
      chart: 'Line amount trend',
      dataSource: 'Sales order items',
      text: 'Approved line amount (converted USD) per period by order create date.'
    }
  },
  breakdown: {
    itemStatus: {
      chart: 'Line status',
      dataSource: 'Sales order items',
      text: 'Line count by line status (active / cancelled).'
    },
    purchaseProgress: {
      chart: 'Purchase progress',
      dataSource: 'Sales order items',
      text: 'Lines or amount by purchase progress.'
    },
    stockInProgress: {
      chart: 'Inbound progress',
      dataSource: 'Sales order items',
      text: 'Lines or amount by inbound progress.'
    },
    stockOutNotifyProgress: {
      chart: 'Outbound notify progress',
      dataSource: 'Sales order items',
      text: 'Lines or amount by outbound-notify progress.'
    },
    receiptProgress: {
      chart: 'Receipt progress',
      dataSource: 'Sales order items',
      text: 'Lines or amount by receipt progress.'
    },
    invoiceProgress: {
      chart: 'Invoice progress',
      dataSource: 'Sales order items',
      text: 'Lines or amount by invoice progress.'
    },
    currency: {
      chart: 'Currency mix',
      dataSource: 'Sales order items',
      text: 'Amount (or line count) by transaction currency.'
    },
    brandQty: {
      chart: 'Brand qty',
      dataSource: 'Sales order items',
      text: 'Quantity summed by brand.'
    },
    brandAmount: {
      chart: 'Brand amount (USD)',
      dataSource: 'Sales order items',
      text: 'Converted USD amount by brand.'
    },
    dateCode: {
      chart: 'Date code / DC',
      dataSource: 'Sales order items',
      text: 'Line count by date-code text bucket.'
    },
    salesUser: {
      chart: 'Sales rep (USD)',
      dataSource: 'Sales order items',
      text: 'Converted USD amount (or line count) by sales user.'
    }
  },
  rankings: {
    customerByAmount: {
      chart: 'Top 10 customers (amount)',
      dataSource: 'Sales order items',
      text: 'Top 10 customers by converted USD (or line count); metric toggle available.'
    },
    pnByAmount: {
      chart: 'Top 10 MPN (amount)',
      dataSource: 'Sales order items',
      text: 'Top 10 MPNs by converted USD (or line count).'
    },
    pnByQty: {
      chart: 'Top 10 MPN (qty)',
      dataSource: 'Sales order items',
      text: 'Top 10 MPNs by quantity.'
    },
    brandByAmount: {
      chart: 'Top 10 brands (amount)',
      dataSource: 'Sales order items',
      text: 'Top 10 brands by converted USD (or line count).'
    },
    brandByQty: {
      chart: 'Top 10 brands (qty)',
      dataSource: 'Sales order items',
      text: 'Top 10 brands by quantity.'
    },
    salesUserByAmount: {
      chart: 'Top 10 sales reps (amount)',
      dataSource: 'Sales order items',
      text: 'Top 10 sales users by converted USD (or line count).'
    }
  }
}
