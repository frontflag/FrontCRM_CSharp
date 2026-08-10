/** RFQ item list board definition tips (aligned with help) */
export const rfqItemListBoardDefsEn = {
  kpi: {
    publishedCustomers: {
      chart: 'Publishing customers',
      dataSource: 'RFQ items',
      text: 'Distinct customers with RFQ lines in the current set.'
    },
    repeatCustomers: {
      chart: 'Repeat-inquiry customers',
      dataSource: 'RFQ items',
      text: 'Distinct customers with 2+ RFQ headers.'
    },
    repeatRfqs: {
      chart: 'Repeat inquiries',
      dataSource: 'RFQ items',
      text: 'RFQ headers counting as repeat inquiries (2nd+ per customer).'
    },
    rfqCount: {
      chart: 'RFQ count',
      dataSource: 'RFQ items',
      text: 'Distinct RFQ headers in the set.'
    },
    rfqItemCount: {
      chart: 'RFQ line count',
      dataSource: 'RFQ items',
      text: 'Count of RFQ line items.'
    },
    convertedLines: {
      chart: 'Converted lines',
      dataSource: 'RFQ items',
      text: 'RFQ lines linked to an approved sales order line.'
    },
    conversionRate: {
      chart: 'RFQ conversion rate',
      dataSource: 'RFQ items',
      text: 'Converted lines ÷ RFQ lines excluding no-quote-found × 100% (hidden if denominator is 0).'
    }
  },
  trend: {
    customers: {
      chart: 'Publishing customers trend',
      dataSource: 'RFQ items',
      text: 'Distinct customers per period by RFQ create date. Report tab uses company/dept/user + date and excludes cancelled RFQs.'
    },
    rfqs: {
      chart: 'RFQ count trend',
      dataSource: 'RFQ items',
      text: 'RFQ headers per period by create date. Report tab excludes cancelled RFQs.'
    },
    items: {
      chart: 'RFQ line count trend',
      dataSource: 'RFQ items',
      text: 'RFQ lines per period by create date. Report tab excludes cancelled RFQs.'
    }
  },
  breakdown: {
    rfqStatus: {
      chart: 'RFQ status',
      dataSource: 'RFQ items',
      text: 'Distinct RFQ headers grouped by header status.'
    },
    rfqType: {
      chart: 'RFQ type',
      dataSource: 'RFQ items',
      text: 'Distinct RFQ headers grouped by RFQ type.'
    },
    targetType: {
      chart: 'Target type',
      dataSource: 'RFQ items',
      text: 'Distinct RFQ headers grouped by target type.'
    },
    industry: {
      chart: 'Industry',
      dataSource: 'RFQ items',
      text: 'Distinct RFQ headers by customer industry; unset shown as Unset.'
    },
    currency: {
      chart: 'Currency',
      dataSource: 'RFQ items',
      text: 'Line count by line currency.'
    },
    brand: {
      chart: 'Brand distribution',
      dataSource: 'RFQ items',
      text: 'Line count by brand; top brands + Other; unset shown as Unset.'
    },
    assignedPurchaser: {
      chart: 'Assigned purchaser',
      dataSource: 'RFQ items',
      text: 'Line count merging both assigned-purchaser slots; unassigned shown separately.'
    },
    quoteDistribution: {
      chart: 'Quote distribution',
      dataSource: 'RFQ items',
      text: 'Lines in Quoted / No quote found / Pending (purchasing).'
    }
  },
  rankings: {
    customerByLineCount: {
      chart: 'Top 10 customers (by lines)',
      dataSource: 'RFQ items',
      text: 'Top 10 customers by RFQ line count. Names may be masked.'
    },
    salesUserByLineCount: {
      chart: 'Top 10 sales users (by lines)',
      dataSource: 'RFQ items',
      text: 'Top 10 sales users by RFQ line count.'
    },
    mpnByLineCount: {
      chart: 'Top 10 MPNs (by lines)',
      dataSource: 'RFQ items',
      text: 'Top 10 MPNs by RFQ line count; empty as Unset.'
    },
    mpnByQty: {
      chart: 'Top 10 MPNs (by quantity)',
      dataSource: 'RFQ items',
      text: 'Top 10 MPNs by requested quantity.'
    },
    brandByLineCount: {
      chart: 'Top 10 brands (by lines)',
      dataSource: 'RFQ items',
      text: 'Top 10 brands by RFQ line count; empty as Unset.'
    },
    brandByQty: {
      chart: 'Top 10 brands (by quantity)',
      dataSource: 'RFQ items',
      text: 'Top 10 brands by requested quantity.'
    }
  }
}
