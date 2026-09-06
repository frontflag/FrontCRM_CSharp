export interface CustomerQuoteReportLine {
  lineNo: number
  mpn: string
  customerMpn: string
  brand: string
  customerBrand: string
  qty: string
  unitPrice: string
  currency: string
  amount: string
  leadTime: string
  dateCode: string
  remark: string
}

export interface CustomerQuoteReportTotal {
  currency: string
  amount: string
  qty: string
}

export interface CustomerQuoteReportDocProps {
  logoUrl: string
  headerCompanyName: string
  quotationNo: string
  quoteDate: string
  billToLines: string[]
  quoterLines: string[]
  lines: CustomerQuoteReportLine[]
  totals: CustomerQuoteReportTotal[]
  totalQty: string
  headerRemark?: string
  signDate: string
  reportLang: 'zh' | 'en'
}
