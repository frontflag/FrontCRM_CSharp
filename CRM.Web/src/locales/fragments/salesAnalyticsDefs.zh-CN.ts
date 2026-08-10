/** 销售分析页各面板口径 Tip（对齐 help/pages/销售分析_MENU_SALES_ANALYTICS.md） */
export const salesAnalyticsDefsZh = {
  snapshot: {
    rfqItems: {
      chart: '需求条目数',
      dataSource: '需求明细',
      text: '日期区间内创建的需求明细行数量。'
    },
    rfqCustomers: {
      chart: '需求客户数',
      dataSource: '需求明细',
      text: '上述需求涉及的去重客户数。'
    },
    conversion: {
      chart: '需求→销售转化率',
      dataSource: '需求明细',
      text: '区间内已挂上销售订单明细的需求明细数 ÷ 需求条目数 × 100%。分母为 0 时不显示比率。按明细行计，不是按客户或订单张数。'
    },
    soItems: {
      chart: '销售订单条目数',
      dataSource: '销售订单明细',
      text: '区间内创建的销售订单有效明细行数（含尚未审核的订单）。'
    },
    soCustomers: {
      chart: '销售客户数',
      dataSource: '销售订单',
      text: '区间内销售订单涉及的去重客户数（含尚未审核）。'
    },
    amount: {
      chart: '成单金额（已审核）',
      dataSource: '销售订单',
      text: '区间内创建、且已审核通过及以上的订单，美元折算总额合计。新建/待审核不计入。'
    },
    stockOut: {
      chart: '已出库金额',
      dataSource: '销售订单明细',
      text: '主数字为折算美金（实际出库数量 × 折算单价）；可点「查看本币」看各原币分档。'
    },
    received: {
      chart: '已收款金额',
      dataSource: '销售订单明细',
      text: '主数字为折算美金；可点「查看本币」看各原币已收款分档（勿跨币种直接相加）。'
    }
  },
  trend: {
    amount: {
      chart: '成单金额趋势',
      dataSource: '销售订单',
      text: '各时段内已审核订单的美元成单额（折算美金）。按订单创建日归入时段。'
    },
    stockOut: {
      chart: '已出库金额趋势',
      dataSource: '销售订单明细',
      text: '各时段内已审核明细的出库额（折算美金）。按订单创建日归入时段。'
    },
    received: {
      chart: '已收款金额趋势',
      dataSource: '销售订单明细',
      text: '各时段内已审核明细的已收款额（折算美金）。按订单创建日归入时段。'
    },
    rfqCustomers: {
      chart: '需求客户数趋势',
      dataSource: '需求明细',
      text: '按需求明细创建日归入时段；客户数为时段内去重。'
    },
    salesCustomers: {
      chart: '销售客户数趋势',
      dataSource: '销售订单',
      text: '按订单创建日归入时段；客户看主单去重。'
    },
    rfq: {
      chart: '需求条目趋势',
      dataSource: '需求明细',
      text: '按需求明细创建日归入时段的明细行数。'
    },
    items: {
      chart: '订单条目趋势',
      dataSource: '销售订单明细',
      text: '按订单创建日归入时段的有效明细行数。'
    },
    conversion: {
      chart: '转化率趋势',
      dataSource: '需求明细',
      text: '与概览转化率同算法，按每个时段单独计算。'
    },
    receivable: {
      chart: '应收趋势',
      dataSource: '销售订单明细',
      text: '各时段内、已审核订单明细的未收款按行折算美金后合计。不是待办「待核销应收款」。'
    }
  },
  breakdown: {
    orderStatus: {
      chart: '订单主状态',
      dataSource: '销售订单',
      text: '区间内非取消/失败订单，按主单状态汇总折算美金（convert_total）金额；无金额权限时改为按订单张数占比。含未审核订单。'
    },
    currency: {
      chart: '币别构成（成单）',
      dataSource: '销售订单',
      text: '仅已审核成单，按订单原币别看折算美金金额占比；无金额权限时按订单数占比。'
    },
    pipelineStage: {
      chart: '全链路环节',
      dataSource: '销售订单明细',
      text: '已审核订单的每条明细只归入一个「当前卡点」：待采购 → 待入库 → 待出库 → 待收款 → 待开票 → 已完结。'
    },
    stockOutProgress: {
      chart: '出库进度',
      dataSource: '销售订单明细',
      text: '区间内有效明细按「待出库 / 部分出库 / 出库完成」计行数（不限是否已审核）。'
    }
  },
  rankings: {
    primary: {
      chart: '销售额 Top10',
      dataSource: '销售订单',
      text: '仅统计区间内已审核成单；有金额权限时按折算美金排序，否则按订单数。公司视角为部门 Top10，部门视角为业务员 Top10。'
    }
  },
  customer: {
    approvedCustomers: {
      chart: '成单客户数',
      dataSource: '销售订单',
      text: '区间内已审核成单订单涉及的去重客户数（只计成单客户）。'
    },
    repeatCustomers: {
      chart: '复购客户数',
      dataSource: '销售订单',
      text: '上述成单客户中，成单订单张数 ≥ 2 的客户数。'
    },
    customerType: {
      chart: '客户类型',
      dataSource: '销售订单',
      text: '按成单金额（折算美金）看构成；无金额权限时改为按订单数占比。'
    },
    customerLevel: {
      chart: '客户等级',
      dataSource: '销售订单',
      text: '按成单金额（折算美金）看构成；无金额权限时改为按订单数占比。'
    },
    customerIndustry: {
      chart: '客户行业',
      dataSource: '销售订单',
      text: '按成单金额（折算美金）看构成；无金额权限时改为按订单数占比。'
    },
    byAmount: {
      chart: '客户 Top10（成单金额）',
      dataSource: '销售订单',
      text: '区间内成单金额（折算美金）最高的客户。'
    },
    byOrderCount: {
      chart: '客户 Top10（成单数）',
      dataSource: '销售订单',
      text: '区间内成单订单张数最多的客户。'
    },
    byRepeat: {
      chart: '客户 Top10（复购订单数）',
      dataSource: '销售订单',
      text: '复购订单数 = 成单订单数 − 1；用于观察重复下单强度。'
    }
  }
} as const
