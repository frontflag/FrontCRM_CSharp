/** 采购分析页各面板口径 Tip（对齐 help/pages/采购分析_MENU_PURCHASE_ANALYTICS.md） */
export const purchaseAnalyticsDefsZh = {
  todo: {
    payable: {
      chart: '应付款金额',
      dataSource: '采购订单明细',
      text: '当前权限范围内，有效采购明细上「尚未付齐」的金额按行折算美金后合计。与所选日期无关。'
    },
    pendingStockIn: {
      chart: '待入库明细数',
      dataSource: '采购订单明细',
      text: '入库进度仍为「待入库」或「部分入库」的明细行数（不是订单张数）。与所选日期无关。'
    }
  },
  snapshot: {
    quoteItems: {
      chart: '报价条目数',
      dataSource: '报价列表',
      text: '日期区间内创建的报价明细行数量。'
    },
    quoteVendors: {
      chart: '报价供应商数',
      dataSource: '报价列表',
      text: '上述报价涉及的去重供应商数。'
    },
    conversion: {
      chart: '报价→采购转化率',
      dataSource: '报价列表',
      text: '区间内已关联到采购订单明细的报价明细数 ÷ 报价条目数 × 100%。按明细行计。'
    },
    poItems: {
      chart: '采购订单条目数',
      dataSource: '采购订单明细',
      text: '区间内创建的采购订单有效明细行数（含尚未审核的订单）。'
    },
    poVendors: {
      chart: '采购供应商数',
      dataSource: '采购订单',
      text: '区间内采购订单涉及的去重供应商数（含尚未审核）。'
    },
    amount: {
      chart: '成单金额（已审核）',
      dataSource: '采购订单',
      text: '区间内创建、且已审核通过及以上的订单，美元折算总额合计。新建/待审核不计入。'
    },
    stockIn: {
      chart: '已入库金额',
      dataSource: '采购订单明细',
      text: '已审核订单明细上，已入库数量 × 折算单价的合计（折算美金）。'
    },
    paid: {
      chart: '已付款金额',
      dataSource: '采购订单明细',
      text: '已审核订单明细上「已付款」金额按行折算美金后合计。'
    }
  },
  trend: {
    amount: {
      chart: '成单金额趋势',
      dataSource: '采购订单',
      text: '各时段内已审核采购订单的折算美元成单额。'
    },
    stockIn: {
      chart: '已入库金额趋势',
      dataSource: '采购订单明细',
      text: '各时段内已审核明细入库额（数量 × 折算单价，折算美元）。'
    },
    paid: {
      chart: '已付款金额趋势',
      dataSource: '采购订单明细',
      text: '各时段内已审核明细已付款额按行折算美金后合计。'
    },
    quoteVendors: {
      chart: '报价供应商数趋势',
      dataSource: '报价列表',
      text: '按报价创建日归入时段；供应商数为时段内去重。'
    },
    poVendors: {
      chart: '采购供应商数趋势',
      dataSource: '采购订单',
      text: '按采购订单创建日归入时段；供应商看主单去重。'
    },
    quote: {
      chart: '报价条目趋势',
      dataSource: '报价列表',
      text: '按报价创建日归入时段的报价明细行数。'
    },
    items: {
      chart: '订单条目趋势',
      dataSource: '采购订单明细',
      text: '按采购订单创建日归入时段的有效明细行数。'
    },
    conversion: {
      chart: '转化率趋势',
      dataSource: '报价列表',
      text: '与概览转化率同算法，按每个时段单独计算。'
    },
    payable: {
      chart: '应付趋势',
      dataSource: '采购订单明细',
      text: '各时段内、已审核采购明细的未付款按行折算美金后合计。不是待办应付（待办为全量存量）。'
    }
  },
  breakdown: {
    orderStatus: {
      chart: '订单主状态',
      dataSource: '采购订单',
      text: '区间内非取消/失败订单，按主单状态汇总金额；无金额权限时改为按订单张数占比。含未审核订单。'
    },
    currency: {
      chart: '币别构成（成单）',
      dataSource: '采购订单',
      text: '仅已审核成单，按订单原币别看金额占比；无金额权限时按订单数占比。'
    },
    pipelineStage: {
      chart: '全链路环节',
      dataSource: '采购订单明细',
      text: '已审核订单的每条明细只归入一个「当前卡点」（以页面图例为准）。'
    },
    stockInProgress: {
      chart: '入库进度',
      dataSource: '采购订单明细',
      text: '区间内有效明细按入库进度计行数。'
    }
  },
  rankings: {
    primary: {
      chart: '采购额 Top10',
      dataSource: '采购订单',
      text: '仅统计区间内已审核成单；有金额权限时按折算美金排序，否则按订单数。公司视角为部门 Top10，部门视角为采购员 Top10。'
    }
  },
  vendor: {
    approvedVendors: {
      chart: '成单供应商数',
      dataSource: '采购订单',
      text: '区间内已审核成单订单涉及的去重供应商数（只计成单供应商）。'
    },
    repeatVendors: {
      chart: '复采供应商数',
      dataSource: '采购订单',
      text: '上述成单供应商中，成单订单张数 ≥ 2 的供应商数。'
    },
    vendorCredit: {
      chart: '供应商身份',
      dataSource: '采购订单',
      text: '按成单金额（折算美金）看构成；无金额权限时改为按订单数占比。'
    },
    vendorLevel: {
      chart: '供应商等级',
      dataSource: '采购订单',
      text: '按成单金额（折算美金）看构成；无金额权限时改为按订单数占比。'
    },
    vendorIndustry: {
      chart: '供应商行业',
      dataSource: '采购订单',
      text: '按成单金额（折算美金）看构成；无金额权限时改为按订单数占比。'
    },
    byAmount: {
      chart: '供应商 Top10（成单金额）',
      dataSource: '采购订单',
      text: '区间内成单金额（折算美金）最高的供应商。'
    },
    byOrderCount: {
      chart: '供应商 Top10（成单数）',
      dataSource: '采购订单',
      text: '区间内成单订单张数最多的供应商。'
    },
    byRepeat: {
      chart: '供应商 Top10（复采订单数）',
      dataSource: '采购订单',
      text: '复采订单数 = 成单订单数 − 1；用于观察重复下单强度。'
    }
  }
} as const
