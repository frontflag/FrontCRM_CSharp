/** 采购订单明细看板各面板口径 Tip（对齐 help/pages/采购订单明细看板 + 采购分析·订单趋势） */
export const purchaseOrderItemListBoardDefsZh = {
  kpi: {
    approvedVendors: {
      chart: '成单供应商数',
      dataSource: '采购订单明细',
      text: '当前统计集合中的去重供应商数（筛选模式下表示结果集内汇总，不等于报表「仅审核通过」）。'
    },
    approvedOrders: {
      chart: '成单订单数',
      dataSource: '采购订单明细',
      text: '当前统计集合中的去重采购订单张数。'
    },
    approvedLines: {
      chart: '成单明细数',
      dataSource: '采购订单明细',
      text: '当前统计集合中的明细行数。'
    },
    approvedAmount: {
      chart: '成单金额',
      dataSource: '采购订单明细',
      text: '明细金额合计（美元；可对照原币）。无金额权限时为「—」。'
    },
    inStockVendors: {
      chart: '在库供应商数',
      dataSource: '采购订单明细',
      text: '仍有在库数量的去重供应商数。'
    },
    inStockLines: {
      chart: '在库明细数',
      dataSource: '采购订单明细',
      text: '仍有在库数量的明细行数。'
    },
    inStockAmount: {
      chart: '在库金额',
      dataSource: '采购订单明细',
      text: '在库相关金额合计（美元）；无权限时为「—」。'
    },
    maxStockAge: {
      chart: '最长在库天数',
      dataSource: '采购订单明细',
      text: '上述范围内在库天数的最大值。'
    },
    payableVendors: {
      chart: '应付款供应商数',
      dataSource: '采购订单明细',
      text: '仍有未付清的去重供应商数。'
    },
    payableLines: {
      chart: '应付款明细数',
      dataSource: '采购订单明细',
      text: '仍有未付清的明细行数。'
    },
    payableAmount: {
      chart: '应付款金额',
      dataSource: '采购订单明细',
      text: '未付清金额合计；无金额权限时为「—」。'
    }
  },
  trend: {
    orders: {
      chart: '成单订单数趋势',
      dataSource: '采购订单明细',
      text: '各时段内成单订单张数随时间变化（按订单创建日归入时段）。报表页签为已审核成单硬过滤。'
    },
    lines: {
      chart: '成单明细数趋势',
      dataSource: '采购订单明细',
      text: '各时段内成单明细行数随时间变化（按订单创建日归入时段）。'
    },
    amount: {
      chart: '成单明细金额趋势',
      dataSource: '采购订单明细',
      text: '各时段内成单明细金额（折算美元）随时间变化（按订单创建日归入时段）。'
    }
  },
  breakdown: {
    itemStatus: {
      chart: '明细主状态',
      dataSource: '采购订单明细',
      text: '按采购明细主状态分组计行数（或金额）。'
    },
    paymentRequestProgress: {
      chart: '申请付款状态',
      dataSource: '采购订单明细',
      text: '按明细关联付款申请进度分组计行数或金额。'
    },
    paymentProgress: {
      chart: '付款进度',
      dataSource: '采购订单明细',
      text: '按明细付款进度分组计行数或金额。'
    },
    purchaseProgress: {
      chart: '采购进度',
      dataSource: '采购订单明细',
      text: '按明细采购进度分组计行数或金额。'
    },
    stockInProgress: {
      chart: '入库进度',
      dataSource: '采购订单明细',
      text: '按明细入库进度分组计行数或金额。'
    },
    invoiceProgress: {
      chart: '开票进度',
      dataSource: '采购订单明细',
      text: '按明细开票进度分组计行数或金额。'
    },
    currency: {
      chart: '币别构成',
      dataSource: '采购订单明细',
      text: '按明细币别汇总金额（或行数）。'
    },
    brandQty: {
      chart: '品牌数量（Qty）',
      dataSource: '采购订单明细',
      text: '按品牌汇总明细数量。'
    },
    brandAmount: {
      chart: '品牌金额（USD）',
      dataSource: '采购订单明细',
      text: '按品牌汇总折算美元金额。'
    },
    dateCode: {
      chart: '生产日期/DC',
      dataSource: '采购订单明细',
      text: '按明细生产日期文本分桶，计行数。'
    },
    purchaseUser: {
      chart: '采购员（USD）',
      dataSource: '采购订单明细',
      text: '按订单采购员汇总折算美元金额（或行数）。'
    }
  },
  rankings: {
    vendorByAmount: {
      chart: '供应商 Top10（金额）',
      dataSource: '采购订单明细',
      text: '按供应商汇总明细折算美元金额（或行数）降序前 10；可在金额/数量间切换。'
    },
    pnByAmount: {
      chart: '物料 Top10（金额）',
      dataSource: '采购订单明细',
      text: '按物料型号汇总折算美元金额（或行数）降序前 10。'
    },
    pnByQty: {
      chart: '物料 Top10（数量）',
      dataSource: '采购订单明细',
      text: '按物料型号汇总数量降序前 10。'
    },
    brandByAmount: {
      chart: '品牌 Top10（金额）',
      dataSource: '采购订单明细',
      text: '按品牌汇总折算美元金额（或行数）降序前 10。'
    },
    brandByQty: {
      chart: '品牌 Top10（数量）',
      dataSource: '采购订单明细',
      text: '按品牌汇总数量降序前 10。'
    },
    purchaseUserByAmount: {
      chart: '采购员 Top10（金额）',
      dataSource: '采购订单明细',
      text: '按采购员汇总折算美元金额（或行数）降序前 10。'
    }
  }
}
