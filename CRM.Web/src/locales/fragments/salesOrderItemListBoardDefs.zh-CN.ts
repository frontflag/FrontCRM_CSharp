/** 销售订单明细看板各面板口径 Tip（对齐 help/pages/销售订单明细看板 + 销售分析·订单趋势） */
export const salesOrderItemListBoardDefsZh = {
  kpi: {
    approvedCustomers: {
      chart: '客户数',
      dataSource: '销售订单明细',
      text: '当前统计集合中涉及的去重客户数。'
    },
    approvedOrders: {
      chart: '订单数',
      dataSource: '销售订单明细',
      text: '当前统计集合中涉及的去重销售订单张数。'
    },
    approvedLines: {
      chart: '明细数',
      dataSource: '销售订单明细',
      text: '当前统计集合中的明细行数。'
    },
    approvedAmount: {
      chart: '金额',
      dataSource: '销售订单明细',
      text: '明细金额合计（折算美元为主；可对照原币分档）。无金额权限时为「—」。'
    },
    purchaseProfit: {
      chart: '采购利润',
      dataSource: '销售订单明细',
      text: '明细上已沉淀的采购利润汇总（无金额权限时为「—」）。'
    },
    outboundProfit: {
      chart: '出库利润',
      dataSource: '销售订单明细',
      text: '明细上已沉淀的出库利润汇总（无金额权限时为「—」）。'
    },
    inStockCustomers: {
      chart: '在库客户数',
      dataSource: '销售订单明细',
      text: '仍有在库数量的去重客户数。'
    },
    inStockLines: {
      chart: '在库明细数',
      dataSource: '销售订单明细',
      text: '仍有在库数量的明细行数。'
    },
    inStockAmount: {
      chart: '在库金额',
      dataSource: '销售订单明细',
      text: '在库相关金额合计（美元口径）；无权限时为「—」。'
    },
    maxStockAge: {
      chart: '最长在库天数',
      dataSource: '销售订单明细',
      text: '上述范围内在库天数的最大值。'
    },
    receivableCustomers: {
      chart: '应收客户数',
      dataSource: '销售订单明细',
      text: '仍有应收的去重客户数。此处应收来自订单明细执行数据，可能与财务应收台账不同。'
    },
    receivableLines: {
      chart: '应收明细数',
      dataSource: '销售订单明细',
      text: '仍有应收的明细行数。'
    },
    receivableAmount: {
      chart: '应收金额',
      dataSource: '销售订单明细',
      text: '未收齐金额合计（展示单位以页面为准）；无金额权限时为「—」。'
    },
    maxReceivableAge: {
      chart: '最长账龄',
      dataSource: '销售订单明细',
      text: '上述范围内应收账龄的最大值（天）。'
    }
  },
  trend: {
    orders: {
      chart: '订单数趋势',
      dataSource: '销售订单明细',
      text: '各时段内成单订单张数随时间变化（按订单创建日归入时段）。报表页签为已审核成单硬过滤。'
    },
    lines: {
      chart: '明细数趋势',
      dataSource: '销售订单明细',
      text: '各时段内成单明细行数随时间变化（按订单创建日归入时段）。'
    },
    amount: {
      chart: '明细金额趋势',
      dataSource: '销售订单明细',
      text: '各时段内成单明细金额（折算美元）随时间变化（按订单创建日归入时段）。'
    }
  },
  breakdown: {
    itemStatus: {
      chart: '明细主状态',
      dataSource: '销售订单明细',
      text: '按明细主状态分组计行数（正常 / 已取消等）。'
    },
    purchaseProgress: {
      chart: '采购进度',
      dataSource: '销售订单明细',
      text: '按明细采购进度分组（待采购 / 采购中 / 采购完成等），计行数或金额。'
    },
    stockInProgress: {
      chart: '入库进度',
      dataSource: '销售订单明细',
      text: '按明细入库进度分组计行数或金额。'
    },
    stockOutNotifyProgress: {
      chart: '出库通知进度',
      dataSource: '销售订单明细',
      text: '按出库通知进度分组计行数或金额。'
    },
    receiptProgress: {
      chart: '收款进度',
      dataSource: '销售订单明细',
      text: '按收款进度分组计行数或金额。'
    },
    invoiceProgress: {
      chart: '开票进度',
      dataSource: '销售订单明细',
      text: '按开票进度分组计行数或金额。'
    },
    currency: {
      chart: '币别构成',
      dataSource: '销售订单明细',
      text: '按明细交易币别汇总金额（或行数）。'
    },
    brandQty: {
      chart: '品牌数量（Qty）',
      dataSource: '销售订单明细',
      text: '按品牌汇总明细数量。'
    },
    brandAmount: {
      chart: '品牌金额（USD）',
      dataSource: '销售订单明细',
      text: '按品牌汇总明细折算美元金额。'
    },
    dateCode: {
      chart: '生产日期/DC',
      dataSource: '销售订单明细',
      text: '按明细生产日期文本分桶，计行数。'
    },
    salesUser: {
      chart: '销售员（USD）',
      dataSource: '销售订单明细',
      text: '按订单销售员汇总明细折算美元金额（或行数）。'
    }
  },
  rankings: {
    customerByAmount: {
      chart: '客户 Top10（金额）',
      dataSource: '销售订单明细',
      text: '按客户汇总明细折算美元金额（或明细数）降序前 10；可在金额/明细数间切换。'
    },
    pnByAmount: {
      chart: '物料 Top10（金额）',
      dataSource: '销售订单明细',
      text: '按物料型号汇总折算美元金额（或明细数）降序前 10；可在金额/明细数间切换。'
    },
    pnByQty: {
      chart: '物料 Top10（数量）',
      dataSource: '销售订单明细',
      text: '按物料型号汇总数量降序前 10；列显示数量，不受金额/明细数切换影响。'
    },
    brandByAmount: {
      chart: '品牌 Top10（金额）',
      dataSource: '销售订单明细',
      text: '按品牌汇总折算美元金额（或明细数）降序前 10；可在金额/明细数间切换。'
    },
    brandByQty: {
      chart: '品牌 Top10（数量）',
      dataSource: '销售订单明细',
      text: '按品牌汇总数量降序前 10；列显示数量，不受金额/明细数切换影响。'
    },
    salesUserByAmount: {
      chart: '销售员 Top10（金额）',
      dataSource: '销售订单明细',
      text: '按销售员汇总折算美元金额（或明细数）降序前 10；可在金额/明细数间切换。'
    }
  }
}
