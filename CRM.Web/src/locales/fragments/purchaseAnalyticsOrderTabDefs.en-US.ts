/** Purchase analytics · Orders tab ranking definitions (approved lines; amount ⇄ transactions) */
export const purchaseAnalyticsOrderTabDefsEn = {
  rankings: {
    vendorByAmount: {
      chart: 'Top 10 vendors',
      dataSource: 'Purchase order lines',
      text:
        'Approved scope (report lens + date range; order status ≥ approved and active lines). Grouped by vendor. Default: top 10 by converted USD amount; switch to transaction count (distinct purchase orders per group). Without amount permission, always ranked by transaction count.'
    },
    pnByAmount: {
      chart: 'Top 10 MPN',
      dataSource: 'Purchase order lines',
      text:
        'Same approved scope. Grouped by MPN. Default: top 10 by converted USD; switch to distinct order count per group. Without amount permission, always by transaction count.'
    },
    brandByAmount: {
      chart: 'Top 10 brands',
      dataSource: 'Purchase order lines',
      text:
        'Same approved scope. Grouped by brand. Default: top 10 by converted USD; switch to distinct order count per group. Without amount permission, always by transaction count.'
    },
    purchaseUserByAmount: {
      chart: 'Top 10 purchasers',
      dataSource: 'Purchase order lines',
      text:
        'Same approved scope. Grouped by purchaser on the order header. Default: top 10 by converted USD; switch to distinct order count per group. Without amount permission, always by transaction count.'
    }
  }
} as const
