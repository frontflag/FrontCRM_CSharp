/** 采购分析 · 订单 Tab 排行口径（报表成单；金额 ⇄ 交易频次） */
export const purchaseAnalyticsOrderTabDefsZh = {
  rankings: {
    vendorByAmount: {
      chart: '供应商 Top10',
      dataSource: '采购订单明细',
      text:
        '成单口径（报表透镜 + 日期范围内，主单审核通过及以上且明细有效）。按供应商汇总。默认按折算美元金额降序取前 10；可切换为按交易频次（分组内去重采购订单张数）降序。无金额权限时固定按交易频次。'
    },
    pnByAmount: {
      chart: '物料 Top10',
      dataSource: '采购订单明细',
      text:
        '成单口径同上。按物料型号（PN）汇总。默认按折算美元金额降序取前 10；可切换为按交易频次（分组内去重采购订单张数）降序。无金额权限时固定按交易频次。'
    },
    brandByAmount: {
      chart: '品牌 Top10',
      dataSource: '采购订单明细',
      text:
        '成单口径同上。按品牌汇总。默认按折算美元金额降序取前 10；可切换为按交易频次（分组内去重采购订单张数）降序。无金额权限时固定按交易频次。'
    },
    purchaseUserByAmount: {
      chart: '采购员 Top10',
      dataSource: '采购订单明细',
      text:
        '成单口径同上。按采购订单主单采购员汇总。默认按折算美元金额降序取前 10；可切换为按交易频次（分组内去重采购订单张数）降序。无金额权限时固定按交易频次。'
    }
  }
} as const
