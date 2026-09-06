/** Sales analytics · Orders tab ranking definitions (approved lines; amount ⇄ transactions) */
export const salesAnalyticsOrderTabDefsEn = {
  rankings: {
    customerByAmount: {
      chart: 'Top 10 customers',
      dataSource: 'Sales order lines',
      text:
        'Approved scope (report lens + date range; order status ≥ approved and active lines). Grouped by customer. Default: top 10 by converted USD amount; switch to transaction frequency (line count per group). Without amount permission, always ranked by transaction frequency.'
    },
    pnByAmount: {
      chart: 'Top 10 MPN',
      dataSource: 'Sales order lines',
      text:
        'Same approved scope. Grouped by MPN. Default: top 10 by converted USD; switch to transaction frequency (line count per group). Without amount permission, always by transaction frequency.'
    },
    brandByAmount: {
      chart: 'Top 10 brands',
      dataSource: 'Sales order lines',
      text:
        'Same approved scope. Grouped by brand. Default: top 10 by converted USD; switch to transaction frequency (line count per group). Without amount permission, always by transaction frequency.'
    },
    salesUserByAmount: {
      chart: 'Top 10 sales reps',
      dataSource: 'Sales order lines',
      text:
        'Same approved scope. Grouped by sales rep on the order header. Default: top 10 by converted USD; switch to transaction frequency (line count per group). Without amount permission, always by transaction frequency.'
    }
  }
} as const
