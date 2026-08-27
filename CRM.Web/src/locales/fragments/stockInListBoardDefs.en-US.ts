/** Stock-in list board definition tips */
export const stockInListBoardDefsEn = {
  kpi: {
    vendors: {
      chart: 'Inbound vendors',
      dataSource: 'Stock-in',
      text: 'Distinct vendors on stock-in headers in the current filter. Documents without a vendor are excluded.'
    },
    headers: {
      chart: 'Stock-in documents',
      dataSource: 'Stock-in',
      text: 'Number of stock-in headers in the current filter (full set, not the current page). Same conditions as the list total.'
    },
    amount: {
      chart: 'Inbound amount',
      dataSource: 'Stock-in',
      text: 'Purchase unit price × inbound qty. USD uses the posting snapshot first, then PO-line convert price × qty, then the query-day FX rate. Original-currency split is available. Shows "—" without amount permission.'
    }
  },
  trend: {
    headers: {
      chart: 'Stock-in documents',
      dataSource: 'Stock-in',
      text: 'Header count bucketed by stock-in date (day / week / month). Documents without a stock-in date are omitted from trends.'
    },
    amount: {
      chart: 'Inbound amount',
      dataSource: 'Stock-in',
      text: 'Same time axis; inbound amount in converted USD. Shows "—" without amount permission.'
    }
  },
  breakdown: {
    stockInType: {
      chart: 'Stock-in type',
      dataSource: 'Stock-in',
      text: 'Inbound USD by stock-in type. Legacy purchase type is merged into purchase. Falls back to document-count share when amounts are masked.'
    },
    purchaseUser: {
      chart: 'Purchaser',
      dataSource: 'Stock-in',
      text: 'Inbound USD by the linked purchase-order purchaser. Falls back to document-count share when amounts are masked.'
    }
  },
  rankings: {
    vendorByAmount: {
      chart: 'Top 10 vendors',
      dataSource: 'Stock-in',
      text: 'Top 10 vendors by inbound USD. Documents without a vendor are grouped as Unlinked vendor.'
    },
    purchaseUserByAmount: {
      chart: 'Top 10 purchasers',
      dataSource: 'Stock-in',
      text: 'Top 10 purchasers by inbound USD. Unassigned purchasers are a separate bucket.'
    }
  }
}
