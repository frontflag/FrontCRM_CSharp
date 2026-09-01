/** Payment list board definition tips (aligned with the help page). */
export const financePaymentListBoardDefsEn = {
  kpi: {
    vendors: {
      chart: 'Payment vendors',
      dataSource: 'Payments',
      text: 'Distinct vendors on payment headers in the current filter. Headers without a vendor are excluded.'
    },
    amount: {
      chart: 'Payment amount',
      dataSource: 'Payments',
      text: 'Paid goods: sum of line paid amounts, excluding bank/freight fees. Split by original payment currency; currencies are not added together and are not converted to USD. Unpaid lines are 0. Hidden when you cannot view amounts.'
    }
  },
  trend: {
    headers: {
      chart: 'Payments',
      dataSource: 'Payments',
      text: 'Payment header count by payment date (day / week / month). Headers without a payment date are omitted from the trend.'
    },
    amount: {
      chart: 'Payment amount',
      dataSource: 'Payments',
      text: 'Same time axis; one chart per original currency using paid goods (no fees). Hidden when you cannot view amounts.'
    }
  },
  breakdown: {
    verificationStatus: {
      chart: 'Write-off status',
      dataSource: 'Payments',
      text: 'Payment count by header write-off status: no items or all pending = pending; all complete = complete; otherwise partial.'
    },
    purchaseUser: {
      chart: 'Buyer',
      dataSource: 'Payments',
      text: 'Paid goods by the purchase-order buyer on each payment line, split by original currency. One payment can split across buyers. Missing buyer is “Unassigned”. Count is used when amounts are hidden.'
    }
  },
  rankings: {
    vendorByAmount: {
      chart: 'Top 10 vendors',
      dataSource: 'Payments',
      text: 'Top 10 vendors by paid goods within each original currency. Headers without a vendor are grouped as “Unlinked vendor”.'
    },
    purchaseUserByAmount: {
      chart: 'Top 10 buyers',
      dataSource: 'Payments',
      text: 'Top 10 buyers by paid goods within each original currency. Unassigned is a separate bucket. One payment may count toward multiple buyers.'
    }
  }
}
