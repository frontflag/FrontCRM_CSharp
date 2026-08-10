export const logisticsAnalyticsDefsZh = {
  todo: {
    pendingStockInQty: {
      chart: '待入库数量(PCS)',
      dataSource: '采购订单明细',
      text: '有效采购订单明细上「订购数量 − 已收货数量」的剩余之和（只计仍有剩余的行）。与截至日期无关。'
    }
  },
  snapshot: {
    onHandQty: {
      chart: '在库商品数量',
      dataSource: '库存明细',
      text: '符合筛选条件的在库现存数量合计（件数）。截至所选日期的时点存量。'
    },
    onHandAmountUsd: {
      chart: '在库商品金额',
      dataSource: '库存明细',
      text: '上述数量按折算美元采购成本合计；无金额权限时为「—」。'
    },
    weightedAvgAgeDays: {
      chart: '加权平均库龄',
      dataSource: '库存明细',
      text: '按数量加权的平均在库天数；库龄从入库日算到截至日。'
    },
    customerCount: {
      chart: '客户数',
      dataSource: '库存明细',
      text: '有客户归属的在库行涉及的去重客户数（无客户的备货行不计入）。'
    },
    salespersonCount: {
      chart: '销售员数',
      dataSource: '库存明细',
      text: '在库行销售员字段的去重主体数（空值不计入）。'
    },
    vendorCount: {
      chart: '供应商数',
      dataSource: '库存明细',
      text: '在库行供应商字段的去重主体数（空值不计入）。'
    },
    purchaserCount: {
      chart: '采购员数',
      dataSource: '库存明细',
      text: '在库行采购员字段的去重主体数（空值不计入）。'
    },
    brandCount: {
      chart: '品牌数',
      dataSource: '库存明细',
      text: '在库行品牌字段的去重主体数（空值不计入）。'
    }
  },
  breakdown: {
    ageBucket: {
      chart: '库龄分布',
      dataSource: '库存明细',
      text: '按数量（不是金额）把在库商品分到库龄桶。各桶数量之和应等于「在库商品数量」。'
    }
  },
  trend: {
    stockInQty: {
      chart: '入库数量趋势',
      dataSource: '库存明细',
      text: '按日/周/月，把趋势区间拆成多个时段；每个点是该时段内按入库日归入的库存数量合计。表示「进来了多少」，不是期末存量。'
    }
  },
  matrix: {
    customer: {
      chart: '客户 × 统计科目',
      dataSource: '库存明细',
      text: '以客户为父行，可按销售员/供应商/采购员/品牌展开子行；列为在库数量、折算美元金额、加权平均库龄。'
    }
  },
  rankings: {
    primary: {
      chart: '在库 Top10',
      dataSource: '库存明细',
      text: '截至日在库 Top10；有金额权限时多按金额排序，否则按数量。公司：客户；部门：销售员；个人：供应商。'
    }
  }
} as const
