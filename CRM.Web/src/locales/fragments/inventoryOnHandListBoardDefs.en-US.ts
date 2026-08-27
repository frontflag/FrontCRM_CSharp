/** Inventory center list board definition tips */
export const inventoryOnHandListBoardDefsEn = {
  kpi: {
    onHandQty: {
      chart: 'On-hand qty',
      dataSource: 'Stock items',
      text: 'Total on-hand PCS for the current filter as of today; matches the list summary card.'
    },
    amount: {
      chart: 'Inventory amount',
      dataSource: 'Stock items',
      text: 'Qty × purchase unit price per original currency; no mixed-currency total. Shows "—" without amount permission.'
    },
    weightedAvgAgeDays: {
      chart: 'Weighted avg age',
      dataSource: 'Stock items',
      text: 'Qty-weighted mean: Σ(age days × on-hand qty) ÷ Σ(on-hand qty), 1 decimal. Age from stock-in date; layers without stock-in date are excluded (same as age pie).'
    },
    stagnantLayers: {
      chart: 'Stagnant inventory entries (>90d)',
      dataSource: 'Stock items',
      text: 'On-hand inventory entries whose stock-in date is over 90 days ago (one entry = one stock_item). Entries without stock-in date are included. Click the KPI to open the stock item list in a new tab.'
    }
  },
  trend: {
    qty: {
      chart: 'On-hand qty trend',
      dataSource: 'Stock items + outbound history',
      text: 'Point-in-time on-hand PCS at each calendar month-end (last 12 months). Layers cleared out later still count on historical points.'
    },
    amount: {
      chart: 'Inventory amount trend',
      dataSource: 'Stock items + outbound history',
      text: 'Same timeline: month-end on-hand amount in RMB and USD (qty × purchase unit price); 0 when no on-hand in that currency.'
    }
  },
  breakdown: {
    stockType: {
      chart: 'Stock type',
      dataSource: 'Stock items',
      text: 'Customer order / stocking / sample mix. Amount mode is per original currency.'
    },
    warehouse: {
      chart: 'Warehouse',
      dataSource: 'Stock items',
      text: 'On-hand qty or original-currency amount by warehouse.'
    },
    salesUser: {
      chart: 'Salesperson',
      dataSource: 'Stock items',
      text: 'Rows without a salesperson are grouped as Unassigned salesperson.'
    },
    ageBucket: {
      chart: 'Age buckets',
      dataSource: 'Stock items',
      text: 'From stock-in date; buckets align with logistics analytics: 0–30 / 31–90 / 91–180 / 181–365 / 365+ days. No stock-in date is omitted.'
    }
  },
  rankings: {
    customerByQty: {
      chart: 'Top 10 customers (qty)',
      dataSource: 'Stock items',
      text: 'By on-hand PCS; stocking rows grouped as No customer / stocking. Click a row to open the stock item list in a new tab.'
    },
    salesUserByQty: {
      chart: 'Top 10 salespersons (qty)',
      dataSource: 'Stock items',
      text: 'By on-hand PCS. Click a row to open the stock item list in a new tab.'
    },
    materialByQty: {
      chart: 'Top 10 materials (qty)',
      dataSource: 'Stock items',
      text: 'Model + brand by on-hand PCS. Click a row to open the stock item list in a new tab.'
    },
    brandByQty: {
      chart: 'Top 10 brands (qty)',
      dataSource: 'Stock items',
      text: 'Purchase brand by on-hand PCS. Click a row to open the stock item list in a new tab.'
    },
    customerByAmount: {
      chart: 'Top 10 customers (amount)',
      dataSource: 'Stock items',
      text: 'By on-hand amount per original currency. Click a row to open the stock item list in a new tab (same currency as selected).'
    },
    salesUserByAmount: {
      chart: 'Top 10 salespersons (amount)',
      dataSource: 'Stock items',
      text: 'By on-hand amount per original currency. Click a row to open the stock item list in a new tab (same currency as selected).'
    },
    materialByAmount: {
      chart: 'Top 10 materials (amount)',
      dataSource: 'Stock items',
      text: 'Model + brand by on-hand amount per original currency. Click a row to open the stock item list in a new tab (same currency as selected).'
    },
    brandByAmount: {
      chart: 'Top 10 brands (amount)',
      dataSource: 'Stock items',
      text: 'Purchase brand by on-hand amount per original currency. Click a row to open the stock item list in a new tab (same currency as selected).'
    }
  }
}
