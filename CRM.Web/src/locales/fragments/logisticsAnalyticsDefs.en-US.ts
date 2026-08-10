export const logisticsAnalyticsDefsEn = {
  todo: {
    pendingStockInQty: {
      chart: 'Pending stock-in qty (PCS)',
      dataSource: 'PO items',
      text: 'Sum of order qty − received qty on open PO lines. Not limited by as-of date.'
    }
  },
  snapshot: {
    onHandQty: {
      chart: 'On-hand qty',
      dataSource: 'Inventory',
      text: 'Sum of on-hand quantity as of the selected date.'
    },
    onHandAmountUsd: {
      chart: 'On-hand amount',
      dataSource: 'Inventory',
      text: 'On-hand qty × USD purchase cost; “—” when amounts masked.'
    },
    weightedAvgAgeDays: {
      chart: 'Weighted avg age',
      dataSource: 'Inventory',
      text: 'Quantity-weighted average age in days from stock-in date to as-of date.'
    },
    customerCount: {
      chart: 'Customers',
      dataSource: 'Inventory',
      text: 'Distinct customers on on-hand rows with customer ownership.'
    },
    salespersonCount: {
      chart: 'Salespeople',
      dataSource: 'Inventory',
      text: 'Distinct salespeople on on-hand rows.'
    },
    vendorCount: {
      chart: 'Vendors',
      dataSource: 'Inventory',
      text: 'Distinct vendors on on-hand rows.'
    },
    purchaserCount: {
      chart: 'Purchasers',
      dataSource: 'Inventory',
      text: 'Distinct purchasers on on-hand rows.'
    },
    brandCount: {
      chart: 'Brands',
      dataSource: 'Inventory',
      text: 'Distinct brands on on-hand rows.'
    }
  },
  breakdown: {
    ageBucket: {
      chart: 'Age distribution',
      dataSource: 'Inventory',
      text: 'On-hand qty split into age buckets (not amount). Bucket qtys sum to on-hand qty.'
    }
  },
  trend: {
    stockInQty: {
      chart: 'Stock-in qty trend',
      dataSource: 'Inventory',
      text: 'Inflow qty per period by stock-in date. Flow in the trend window, not ending on-hand.'
    }
  },
  matrix: {
    customer: {
      chart: 'Customer × subject',
      dataSource: 'Inventory',
      text: 'Customer parent rows with optional salesperson/vendor/purchaser/brand children; qty, USD amount, weighted age.'
    }
  },
  rankings: {
    primary: {
      chart: 'On-hand Top 10',
      dataSource: 'Inventory',
      text: 'As-of on-hand Top 10; by amount when allowed, else qty. Company: customers; department: salespeople; personal: vendors.'
    }
  }
} as const
