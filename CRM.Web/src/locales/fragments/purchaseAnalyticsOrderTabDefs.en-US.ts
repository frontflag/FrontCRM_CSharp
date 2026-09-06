/** Purchase analytics · Orders tab ranking definitions (approved lines; amount ⇄ transactions) */
export const purchaseAnalyticsOrderTabDefsEn = {
  rankings: {
    vendorByAmount: {
      chart: 'Top 10 vendors',
      dataSource: 'Purchase order lines',
      text:
        'Approved scope (report lens + date range; order status ≥ approved and active lines). Grouped by vendor. Default: top 10 by converted USD amount; switch to transaction frequency (line count per group). Without amount permission, always ranked by transaction frequency.'
    },
    pnByAmount: {
      chart: 'Top 10 MPN',
      dataSource: 'Purchase order lines',
      text:
        'Same approved scope. Grouped by MPN. Default: top 10 by converted USD; switch to transaction frequency (line count per group). Without amount permission, always by transaction frequency.'
    },
    brandByAmount: {
      chart: 'Top 10 brands',
      dataSource: 'Purchase order lines',
      text:
        'Same approved scope. Grouped by brand. Default: top 10 by converted USD; switch to transaction frequency (line count per group). Without amount permission, always by transaction frequency.'
    },
    purchaseUserByAmount: {
      chart: 'Top 10 purchasers',
      dataSource: 'Purchase order lines',
      text:
        'Same approved scope. Grouped by purchaser on the order header. Default: top 10 by converted USD; switch to transaction frequency (line count per group). Without amount permission, always by transaction frequency.'
    }
  }
} as const
