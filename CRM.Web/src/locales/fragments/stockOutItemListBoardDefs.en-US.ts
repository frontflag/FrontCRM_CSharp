/** Stock-out item list board definition tips */
export const stockOutItemListBoardDefsEn = {
  kpi: {
    customers: {
      chart: 'Outbound customers',
      dataSource: 'Stock-out lines',
      text: 'Distinct customers on stock-out headers in the current filter. Rows without a customer are excluded.'
    },
    lines: {
      chart: 'Outbound lines',
      dataSource: 'Stock-out lines',
      text: 'Number of stock-out item rows in the current filter (full set, not the current page).'
    },
    amount: {
      chart: 'Outbound amount',
      dataSource: 'Stock-out lines',
      text: 'Sales unit price × outbound qty. USD uses the posting snapshot first, then order-line convert price × qty, then the query-day FX rate. Original-currency split is available. Shows "—" without amount permission.'
    }
  },
  trend: {
    lines: {
      chart: 'Outbound lines',
      dataSource: 'Stock-out lines',
      text: 'Line count bucketed by stock-out date (day / week / month). Rows without a stock-out date are omitted from trends.'
    },
    amount: {
      chart: 'Outbound amount',
      dataSource: 'Stock-out lines',
      text: 'Same time axis; outbound amount in converted USD. Shows "—" without amount permission.'
    }
  },
  breakdown: {
    stockOutType: {
      chart: 'Stock-out type',
      dataSource: 'Stock-out lines',
      text: 'Outbound USD by stock-out type. Legacy sales type is merged into sales. Falls back to line-count share when amounts are masked.'
    },
    salesUser: {
      chart: 'Salesperson',
      dataSource: 'Stock-out lines',
      text: 'Outbound USD by the sales order salesperson. Falls back to line-count share when amounts are masked.'
    }
  },
  rankings: {
    customerByAmount: {
      chart: 'Top 10 customers',
      dataSource: 'Stock-out lines',
      text: 'Top 10 customers by outbound USD. Rows without a customer are grouped as Unlinked customer.'
    },
    salesUserByAmount: {
      chart: 'Top 10 salespeople',
      dataSource: 'Stock-out lines',
      text: 'Top 10 salespeople by outbound USD. Unassigned salespeople are a separate bucket.'
    }
  }
}
