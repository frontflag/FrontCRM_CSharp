export const financeAnalyticsDefsEn = {
  todo: {
    payable: {
      chart: 'Payables',
      dataSource: 'Payments',
      text: 'Open payable on valid PO lines (same family as purchase todo payable). Usually as-of query time.'
    },
    receivable: {
      chart: 'Receivables',
      dataSource: 'Receivables',
      text: 'Open balance on finance receivable ledger (typically after stock-out); USD uses the linked sales-order line convert price stored at order time. Not the full sales-order open-receipt scope.'
    },
    pendingPurchaseInvoice: {
      chart: 'Pending purchase invoices',
      dataSource: 'Purchase invoices',
      text: 'Open purchase-invoice amount on PO lines.'
    },
    pendingSellInvoice: {
      chart: 'Pending sales invoices',
      dataSource: 'Sales invoices',
      text: 'Open sales-invoice amount on SO lines.'
    }
  },
  completed: {
    paid: {
      chart: 'Paid',
      dataSource: 'Payments',
      text: 'Completed payment vouchers in the period by payment date, converted to USD. In finance company view, each original-currency row has View to the payment list (completed + that payment currency + period start through as-of); the whole card is not clickable.'
    },
    received: {
      chart: 'Received',
      dataSource: 'Receipts',
      text: 'Confirmed receipt vouchers in the period by receipt date, converted to USD. In finance company view, each original-currency row has View to the receipt list (confirmed + that receipt currency + receipt date = period start through as-of); the whole card is not clickable. The list date picker remains create-time and is not used for this drill.'
    },
    issuedPurchaseInvoice: {
      chart: 'Issued purchase invoices',
      dataSource: 'Purchase invoices',
      text: 'Purchase invoices issued in the period by invoice date, converted to USD.'
    },
    issuedSellInvoice: {
      chart: 'Issued sales invoices',
      dataSource: 'Sales invoices',
      text: 'Sales invoices issued in the period by issue date, converted to USD.'
    }
  },
  trend: {
    paid: {
      chart: 'Paid trend',
      dataSource: 'Payments',
      text: 'Same as completed paid, bucketed by day/week/month in converted USD.'
    },
    received: {
      chart: 'Received trend',
      dataSource: 'Receipts',
      text: 'Same as completed received, bucketed by receipt date in converted USD.'
    }
  },
  breakdown: {
    currency: {
      chart: 'Open items by currency',
      dataSource: 'Payments / Receivables / Invoices',
      text: 'Original-currency mix for the selected todo metric (not converted USD). Dropdown picks which todo metric.'
    }
  }
} as const
