/** 销售分析 · 订单 Tab 排行口径（报表成单；金额 ⇄ 交易频次） */
export const salesAnalyticsOrderTabDefsZh = {
  rankings: {
    customerByAmount: {
      chart: '客户 Top10',
      dataSource: '销售订单明细',
      text:
        '成单口径（报表透镜 + 日期范围内，主单审核通过及以上且明细有效）。按客户汇总。默认按折算美元金额降序取前 10；可切换为按交易频次（分组内去重销售订单张数）降序。无金额权限时固定按交易频次。'
    },
    pnByAmount: {
      chart: '物料 Top10',
      dataSource: '销售订单明细',
      text:
        '成单口径同上。按物料型号（PN）汇总。默认按折算美元金额降序取前 10；可切换为按交易频次（分组内去重销售订单张数）降序。无金额权限时固定按交易频次。'
    },
    brandByAmount: {
      chart: '品牌 Top10',
      dataSource: '销售订单明细',
      text:
        '成单口径同上。按品牌汇总。默认按折算美元金额降序取前 10；可切换为按交易频次（分组内去重销售订单张数）降序。无金额权限时固定按交易频次。'
    },
    salesUserByAmount: {
      chart: '销售员 Top10',
      dataSource: '销售订单明细',
      text:
        '成单口径同上。按销售订单主单销售员汇总。默认按折算美元金额降序取前 10；可切换为按交易频次（分组内去重销售订单张数）降序。无金额权限时固定按交易频次。'
    }
  }
} as const
