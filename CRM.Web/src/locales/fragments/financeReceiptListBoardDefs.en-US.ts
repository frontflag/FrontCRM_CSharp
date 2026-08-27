/** Receipt list board definition tips (aligned with the help page). */
export const financeReceiptListBoardDefsEn = {
  kpi: {
    customers: {
      chart: 'Receipt customers',
      dataSource: 'Receipts',
      text: 'Distinct customers on receipt headers in the current filter. Headers without a customer are excluded.'
    },
    amount: {
      chart: 'Receipt amount',
      dataSource: 'Receipts',
      text: 'Header receipt amount totaled by original currency. Currencies are not added together and are not converted to USD. Hidden when you cannot view amounts.'
    }
  },
  trend: {
    headers: {
      chart: 'Receipts',
      dataSource: 'Receipts',
      text: 'Receipt header count by receipt date (day / week / month). Headers without a receipt date are omitted from the trend.'
    },
    amount: {
      chart: 'Receipt amount',
      dataSource: 'Receipts',
      text: 'Same time axis; one chart per original currency using header receipt amount. Hidden when you cannot view amounts.'
    }
  },
  breakdown: {
    verificationStatus: {
      chart: 'Write-off status',
      dataSource: 'Receipts',
      text: 'Receipt count by header write-off status: no items or all pending = pending; all complete = complete; otherwise partial.'
    },
    salesUser: {
      chart: 'Salesperson',
      dataSource: 'Receipts',
      text: 'Header receipt amount by salesperson, split by original currency. Missing salesperson is “Unassigned”. Count is used when amounts are hidden.'
    }
  },
  rankings: {
    customerByAmount: {
      chart: 'Top 10 customers',
      dataSource: 'Receipts',
      text: 'Top 10 customers by receipt amount within each original currency. Headers without a customer are grouped as “Unlinked customer”.'
    },
    salesUserByAmount: {
      chart: 'Top 10 salespeople',
      dataSource: 'Receipts',
      text: 'Top 10 salespeople by receipt amount within each original currency. Unassigned is a separate bucket.'
    }
  }
}
