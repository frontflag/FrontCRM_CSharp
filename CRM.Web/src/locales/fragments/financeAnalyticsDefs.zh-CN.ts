export const financeAnalyticsDefsZh = {
  todo: {
    payable: {
      chart: '应付款',
      dataSource: '付款记录',
      text: '有效采购订单明细上尚未付清的金额合计（与采购分析待办应付同源一类字段）。多为查询时刻余额。'
    },
    receivable: {
      chart: '应收款',
      dataSource: '应收款',
      text: '财务应收台账上的待核销余额（通常对应已出库后形成的应收）。不是销售分析里「订单未收款」的全部口径。'
    },
    pendingPurchaseInvoice: {
      chart: '待开进项发票',
      dataSource: '进项发票',
      text: '采购明细上尚未开完的进项票金额合计。'
    },
    pendingSellInvoice: {
      chart: '待开销项发票',
      dataSource: '销项发票',
      text: '销售明细上尚未开完的销项票金额合计。'
    }
  },
  completed: {
    paid: {
      chart: '已付款',
      dataSource: '付款记录',
      text: '区间内、状态为付款完成的付款单金额合计（按付款日落入区间），再折算美元。'
    },
    received: {
      chart: '已收款',
      dataSource: '收款记录',
      text: '区间内、状态为已收款的收款单金额合计（按收款日落入区间），再折算美元。'
    },
    issuedPurchaseInvoice: {
      chart: '已开进项发票',
      dataSource: '进项发票',
      text: '区间内开具的进项发票金额合计（按发票日），再折算美元。'
    },
    issuedSellInvoice: {
      chart: '已开销项发票',
      dataSource: '销项发票',
      text: '区间内开具的销项发票金额合计（按开票日），再折算美元。'
    }
  },
  trend: {
    paid: {
      chart: '已付款趋势',
      dataSource: '付款记录',
      text: '与「已完成 → 已付款」同口径，按日/周/月落桶后的折算美元合计。'
    },
    received: {
      chart: '已收款趋势',
      dataSource: '收款记录',
      text: '与「已完成 → 已收款」同口径，按收款日归入各时段后的折算美元合计。'
    }
  },
  breakdown: {
    currency: {
      chart: '待办原币构成',
      dataSource: '付款记录 / 应收款 / 进项发票 / 销项发票',
      text: '对待办四项分别查看各原币金额占比。扇区为原币合计（非折算美元）；可切换下拉选择看哪一项待办。数据源与对应待办指标相同。'
    }
  }
} as const
