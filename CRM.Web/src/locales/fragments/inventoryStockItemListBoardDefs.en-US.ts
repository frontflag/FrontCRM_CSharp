/** Stock item list board definition tips */
export const inventoryStockItemListBoardDefsEn = {
  kpi: {
    onHandQty: {
      chart: 'On-hand qty',
      dataSource: 'Stock items',
      text: 'Total on-hand PCS for the current filter as of today; matches the list on-hand qty card.'
    },
    amount: {
      chart: 'Inventory amount',
      dataSource: 'Stock items',
      text: 'Qty × purchase unit price per original currency; no mixed-currency total. Shows "—" without amount permission.'
    },
    turnoverDays: {
      chart: 'Turnover days',
      dataSource: 'Stock items + outbound history',
      text: 'On-hand qty ÷ last-30-day outbound qty × 30, 1 decimal. Shows "—" when on-hand or last-30-day outbound is 0. Outbound uses completed/partial stock-out lines, actual qty preferred.'
    },
    stagnantQty: {
      chart: 'Stagnant qty (>90d)',
      dataSource: 'Stock items',
      text: 'On-hand PCS whose stock-in date is over 90 days ago (or missing). Click the KPI to return to the list with the stagnant filter.'
    }
  },
  trend: {
    qty: {
      chart: 'On-hand qty trend',
      dataSource: 'Stock items + outbound history',
      text: 'Point-in-time on-hand PCS at each period end. Defaults: 30 days / 12 weeks / 12 months. Layers later cleared still count on historical points. Stock-in date filters clip the window.'
    },
    amount: {
      chart: 'Inventory amount trend',
      dataSource: 'Stock items + outbound history',
      text: 'Same timeline: period-end on-hand amount per original currency (qty × purchase unit price); 0 when none in that currency.'
    }
  },
  breakdown: {
    stockType: {
      chart: 'Stock type',
      dataSource: 'Stock items',
      text: 'Customer / stocking / sample mix. Amount mode is per original currency.'
    },
    warehouse: {
      chart: 'Warehouse',
      dataSource: 'Stock items',
      text: 'On-hand qty or original-currency amount by warehouse.'
    },
    salesUser: {
      chart: 'Salesperson',
      dataSource: 'Stock items',
      text: 'Unassigned rows go to "Unassigned salesperson".'
    },
    ageBucket: {
      chart: 'Age buckets',
      dataSource: 'Stock items',
      text: 'From stock-in date, same buckets as logistics analytics: 0–30 / 31–90 / 91–180 / 181–365 / 365+ days. No stock-in date is omitted from the pie.'
    }
  },
  rankings: {
    customerByQty: {
      chart: 'Top10 customers (qty)',
      dataSource: 'Stock items',
      text: 'On-hand PCS by customer; stocking goes to "No customer / stocking". Click a row to return to the list.'
    },
    salesUserByQty: {
      chart: 'Top10 salespeople (qty)',
      dataSource: 'Stock items',
      text: 'On-hand PCS by salesperson. Click a row to return to the list.'
    },
    materialByQty: {
      chart: 'Top10 materials (qty)',
      dataSource: 'Stock items',
      text: 'Model + brand on-hand PCS. Click a row to return to the list.'
    },
    brandByQty: {
      chart: 'Top10 brands (qty)',
      dataSource: 'Stock items',
      text: 'Purchase brand on-hand PCS. Click a row to return to the list.'
    },
    customerByAmount: {
      chart: 'Top10 customers (amount)',
      dataSource: 'Stock items',
      text: 'On-hand amount by customer and original currency. Click a row to return to the list for that currency.'
    },
    salesUserByAmount: {
      chart: 'Top10 salespeople (amount)',
      dataSource: 'Stock items',
      text: 'On-hand amount by salesperson and original currency. Click a row to return to the list for that currency.'
    },
    materialByAmount: {
      chart: 'Top10 materials (amount)',
      dataSource: 'Stock items',
      text: 'Model + brand on-hand amount by original currency. Click a row to return to the list for that currency.'
    },
    brandByAmount: {
      chart: 'Top10 brands (amount)',
      dataSource: 'Stock items',
      text: 'Brand on-hand amount by original currency. Click a row to return to the list for that currency.'
    }
  }
}
