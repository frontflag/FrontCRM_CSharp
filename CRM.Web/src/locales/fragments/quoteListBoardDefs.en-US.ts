/** Quote list board definition tips (aligned with help) */
export const quoteListBoardDefsEn = {
  kpi: {
    quoteVendors: {
      chart: 'Quoting vendors',
      dataSource: 'Quote list',
      text: 'Distinct vendors with quotes in the current set.'
    },
    validQuotes: {
      chart: 'Valid quote entries',
      dataSource: 'Quote list',
      text: 'Valid quote count (excluding no-quote-found style entries).'
    },
    noQuoteFound: {
      chart: 'No-quote-found lines',
      dataSource: 'Quote list',
      text: 'Entries marked as no quote found.'
    },
    rfqQuoteRate: {
      chart: 'RFQ quote rate',
      dataSource: 'Quote list',
      text: 'Share of RFQ lines that have a quote (hidden if denominator is 0).'
    },
    avgResponse: {
      chart: 'Avg response time',
      dataSource: 'Quote list',
      text: 'Average minutes from RFQ to first quote.'
    },
    avgQuotesPerItem: {
      chart: 'Avg quotes per line',
      dataSource: 'Quote list',
      text: 'Average quote count per RFQ line.'
    },
    convertedLines: {
      chart: 'Converted lines',
      dataSource: 'Quote list',
      text: 'Lines that entered the conversion path.'
    },
    quoteConversionRate: {
      chart: 'Quote conversion rate',
      dataSource: 'Quote list',
      text: 'Converted lines ÷ quoted RFQ lines × 100% (hidden if denominator is 0).'
    }
  },
  trend: {
    vendors: {
      chart: 'Quoting vendors trend',
      dataSource: 'Quote list',
      text: 'Distinct vendors per period by quote create date (report tab uses company/dept/user + date).'
    },
    items: {
      chart: 'RFQ line items trend',
      dataSource: 'Quote list',
      text: 'Related RFQ lines per period by quote create date.'
    },
    validQuotes: {
      chart: 'Valid quotes trend',
      dataSource: 'Quote list',
      text: 'Valid quotes per period by quote create date.'
    }
  },
  breakdown: {
    quoteStatus: {
      chart: 'Quote status',
      dataSource: 'Quote list',
      text: 'Quote headers by status (new / won / closed). Shrinks with status filters.'
    },
    quoteDistribution: {
      chart: 'Quote distribution',
      dataSource: 'Quote list',
      text: 'Linked RFQ lines in Quoted / No quote found / Pending. Status filters usually do not shrink this denominator.'
    },
    labelType: {
      chart: 'Label type',
      dataSource: 'Quote list',
      text: 'Quote headers by first-line label type.'
    },
    waferOrigin: {
      chart: 'Wafer origin',
      dataSource: 'Quote list',
      text: 'Quote headers by first-line wafer origin.'
    },
    packageOrigin: {
      chart: 'Package origin',
      dataSource: 'Quote list',
      text: 'Quote headers by first-line package origin.'
    },
    freeShipping: {
      chart: 'Free shipping',
      dataSource: 'Quote list',
      text: 'Quote headers by first-line free-shipping flag.'
    },
    brand: {
      chart: 'Brand',
      dataSource: 'Quote list',
      text: 'Quote headers by first-line brand; top 20 + Other; unset shown as Unset.'
    },
    assignedPurchaser: {
      chart: 'Assigned purchaser',
      dataSource: 'Quote list',
      text: 'Linked RFQ lines merging both assigned-purchaser slots.'
    },
    quotePurchaser: {
      chart: 'Quote purchaser',
      dataSource: 'Quote list',
      text: 'Quote headers grouped by quote purchaser.'
    }
  },
  rankings: {
    vendorByRfqItemCount: {
      chart: 'Top 10 vendors (RFQ lines)',
      dataSource: 'Quote list',
      text: 'Top 10 vendors by distinct linked RFQ lines. Names may be masked.'
    },
    purchaserByQuoteCount: {
      chart: 'Top 10 purchasers (quote count)',
      dataSource: 'Quote list',
      text: 'Top 10 purchasers by quote header count.'
    },
    purchaserByQuoteRate: {
      chart: 'Top 10 purchasers (quote rate)',
      dataSource: 'Quote list',
      text: 'Quote rate = quotes ÷ assigned RFQ lines × 100% (both assign slots count). Top 10 by rate.'
    },
    mpnByQuoteCount: {
      chart: 'Top 10 MPNs (quotes)',
      dataSource: 'Quote list',
      text: 'Top 10 MPNs by quote header count.'
    },
    mpnByQty: {
      chart: 'Top 10 MPNs (RFQ qty)',
      dataSource: 'Quote list',
      text: 'Top 10 MPNs by linked RFQ requested quantity.'
    },
    brandByQuoteCount: {
      chart: 'Top 10 brands (quotes)',
      dataSource: 'Quote list',
      text: 'Top 10 brands by quote header count (first line brand).'
    },
    brandByQty: {
      chart: 'Top 10 brands (RFQ qty)',
      dataSource: 'Quote list',
      text: 'Top 10 brands by linked RFQ requested quantity.'
    }
  }
}
