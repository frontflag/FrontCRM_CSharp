/** 报价列表看板各面板口径 Tip（对齐 help/pages/报价列表看板 + 采购分析·报价趋势） */
export const quoteListBoardDefsZh = {
  kpi: {
    quoteVendors: {
      chart: '提供报价的供应商数',
      dataSource: '报价列表',
      text: '当前统计集合中有报价的去重供应商数。'
    },
    validQuotes: {
      chart: '有效报价条目数',
      dataSource: '报价列表',
      text: '有效报价条数（非查无类，以页面统计为准）。'
    },
    noQuoteFound: {
      chart: '查无报价条目数',
      dataSource: '报价列表',
      text: '查无报价相关条目数。'
    },
    rfqQuoteRate: {
      chart: '需求报价率',
      dataSource: '报价列表',
      text: '有报价的需求明细占比（分母为 0 时不显示）。'
    },
    avgResponse: {
      chart: '报价平均响应时间',
      dataSource: '报价列表',
      text: '从需求到首条报价的平均耗时（分钟）。'
    },
    avgQuotesPerItem: {
      chart: '报价平均数',
      dataSource: '报价列表',
      text: '平均每条需求明细对应的报价条数。'
    },
    convertedLines: {
      chart: '成单数',
      dataSource: '报价列表',
      text: '已转入成单链路的明细数（分母规则以页面为准）。'
    },
    quoteConversionRate: {
      chart: '报价成单率',
      dataSource: '报价列表',
      text: '成单数 ÷ 有报价需求明细数 × 100%（分母为 0 时不显示）。'
    }
  },
  trend: {
    vendors: {
      chart: '报价供应商数趋势',
      dataSource: '报价列表',
      text: '按报价创建日归入各时段；供应商数为时段内去重。报表页签按公司/部门/个人 + 报价创建日。'
    },
    items: {
      chart: '需求明细数趋势',
      dataSource: '报价列表',
      text: '按报价创建日归入各时段关联的需求明细数。'
    },
    validQuotes: {
      chart: '有效报价数趋势',
      dataSource: '报价列表',
      text: '按报价创建日归入各时段的有效报价条数。'
    }
  },
  breakdown: {
    quoteStatus: {
      chart: '报价主状态',
      dataSource: '报价列表',
      text: '按报价主单计条数，依状态分组（新建 / 成单 / 关闭等）。筛了状态时本图随筛选收缩。'
    },
    quoteDistribution: {
      chart: '报价分布',
      dataSource: '报价列表',
      text: '按并联需求明细归入三档：有报价 / 查无报价 / 采购未处理。状态筛选一般不裁剪这组需求分母。'
    },
    labelType: {
      chart: '涂标',
      dataSource: '报价列表',
      text: '按报价主单；取该报价首条报价行的涂标（不涂标 / 涂标 / 待确定）。'
    },
    waferOrigin: {
      chart: '晶圆产地',
      dataSource: '报价列表',
      text: '按报价主单；取首条报价行的晶圆产地（如美产 / 非美产 / 待确定）。'
    },
    packageOrigin: {
      chart: '封装产地',
      dataSource: '报价列表',
      text: '按报价主单；取首条报价行的封装产地。'
    },
    freeShipping: {
      chart: '包邮',
      dataSource: '报价列表',
      text: '按报价主单；取首条报价行是否包邮（是 / 否）。'
    },
    brand: {
      chart: '品牌分布',
      dataSource: '报价列表',
      text: '按报价主单；取首条报价行品牌；常见为前 20 品牌 +「其他」；未填显示「未设置」。'
    },
    assignedPurchaser: {
      chart: '分配采购员',
      dataSource: '报价列表',
      text: '按并联需求明细计行数；两个分配采购员槽位合并；未分配显示「未分配采购员」。'
    },
    quotePurchaser: {
      chart: '报价采购员',
      dataSource: '报价列表',
      text: '按报价主单计条数，依报价单上的采购员分组。'
    }
  },
  rankings: {
    vendorByRfqItemCount: {
      chart: '供应商 Top10（需求明细数）',
      dataSource: '报价列表',
      text: '经报价行关联供应商，按去重需求明细数降序取前 10。供应商名可能脱敏。'
    },
    purchaserByQuoteCount: {
      chart: '报价采购员 Top10（条目数）',
      dataSource: '报价列表',
      text: '按报价单采购员汇总报价主单条数，降序取前 10。'
    },
    purchaserByQuoteRate: {
      chart: '报价采购员 Top10（报价率）',
      dataSource: '报价列表',
      text: '报价率 = 该采购员报价条数 ÷ 其被分配到的需求明细数 × 100%（明细上两个分配槽各计 1）；按报价率降序取前 10。'
    },
    mpnByQuoteCount: {
      chart: 'MPN Top10（报价条数）',
      dataSource: '报价列表',
      text: '按报价上的物料型号汇总报价主单条数，降序取前 10。'
    },
    mpnByQty: {
      chart: 'MPN Top10（需求数量）',
      dataSource: '报价列表',
      text: '按型号关联需求明细的需求数量汇总，降序取前 10。'
    },
    brandByQuoteCount: {
      chart: '品牌 Top10（报价条数）',
      dataSource: '报价列表',
      text: '按报价首条行品牌汇总报价主单条数，降序取前 10。'
    },
    brandByQty: {
      chart: '品牌 Top10（需求数量）',
      dataSource: '报价列表',
      text: '按首条品牌关联需求明细的需求数量汇总，降序取前 10。'
    }
  }
}
