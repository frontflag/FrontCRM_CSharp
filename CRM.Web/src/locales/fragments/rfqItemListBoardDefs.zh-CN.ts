/** 需求明细看板各面板口径 Tip（对齐 help/pages/需求明细看板 + 销售分析·需求趋势） */
export const rfqItemListBoardDefsZh = {
  kpi: {
    publishedCustomers: {
      chart: '发布需求客户数',
      dataSource: '需求明细',
      text: '当前统计集合中有需求的去重客户数。'
    },
    repeatCustomers: {
      chart: '复询需求客户数',
      dataSource: '需求明细',
      text: '发布过 2 个及以上需求主单的去重客户数。'
    },
    repeatRfqs: {
      chart: '复询需求数',
      dataSource: '需求明细',
      text: '复询强度相关主单数量（同一客户第 2 张及以后的需求主单，以页面统计为准）。'
    },
    rfqCount: {
      chart: '需求数',
      dataSource: '需求明细',
      text: '涉及的需求主单数。'
    },
    rfqItemCount: {
      chart: '需求明细数',
      dataSource: '需求明细',
      text: '需求明细行数。'
    },
    convertedLines: {
      chart: '成单明细数',
      dataSource: '需求明细',
      text: '已挂上已审核销售订单明细的需求明细行数。'
    },
    conversionRate: {
      chart: '需求成单率',
      dataSource: '需求明细',
      text: '成单明细数 ÷（需求明细中非查无报价部分）× 100%（分母为 0 时不显示）。'
    }
  },
  trend: {
    customers: {
      chart: '发布客户数趋势',
      dataSource: '需求明细',
      text: '按需求创建日归入各时段；客户数为时段内去重。报表页签按公司/部门/个人 + 日期，并排除已取消主单。'
    },
    rfqs: {
      chart: '需求数趋势',
      dataSource: '需求明细',
      text: '按需求创建日归入各时段的需求主单数。报表页签按公司/部门/个人 + 日期，并排除已取消主单。'
    },
    items: {
      chart: '需求明细数趋势',
      dataSource: '需求明细',
      text: '按需求创建日归入各时段的需求明细行数。报表页签按公司/部门/个人 + 日期，并排除已取消主单。'
    }
  },
  breakdown: {
    rfqStatus: {
      chart: '需求主状态',
      dataSource: '需求明细',
      text: '按需求主单去重后，依主单状态分组计张数（如待分配、报价中、已关闭等）。'
    },
    rfqType: {
      chart: '需求类型',
      dataSource: '需求明细',
      text: '按需求主单去重，依需求类型分组计张数。'
    },
    targetType: {
      chart: '目标类型',
      dataSource: '需求明细',
      text: '按需求主单去重，依目标类型分组计张数。'
    },
    industry: {
      chart: '行业',
      dataSource: '需求明细',
      text: '按需求主单去重，依客户行业分组；未填显示「未设置」。'
    },
    currency: {
      chart: '币别',
      dataSource: '需求明细',
      text: '按需求明细行计行数，依明细币别分组。'
    },
    brand: {
      chart: '品牌分布',
      dataSource: '需求明细',
      text: '按需求明细行计行数，依品牌分组；未填显示「未设置」；常见为前若干品牌 +「其他」。'
    },
    assignedPurchaser: {
      chart: '分配采购员',
      dataSource: '需求明细',
      text: '按需求明细行计行数；明细上两个分配采购员槽位合并计数；未分配显示「未分配采购员」。'
    },
    quoteDistribution: {
      chart: '报价分布',
      dataSource: '需求明细',
      text: '按需求明细行归入三档：有报价 / 查无报价 / 采购未处理（明细为待处理且尚无报价）。'
    }
  },
  rankings: {
    customerByLineCount: {
      chart: '客户 Top10（需求明细数）',
      dataSource: '需求明细',
      text: '按客户汇总需求明细行数，降序取前 10。客户名可能按权限脱敏。'
    },
    salesUserByLineCount: {
      chart: '业务员 Top10（需求明细数）',
      dataSource: '需求明细',
      text: '按需求主单业务员汇总需求明细行数，降序取前 10。'
    },
    mpnByLineCount: {
      chart: 'MPN Top10（需求明细数）',
      dataSource: '需求明细',
      text: '按物料型号汇总需求明细行数，降序取前 10；空型号显示「未设置」。'
    },
    mpnByQty: {
      chart: 'MPN Top10（需求数量）',
      dataSource: '需求明细',
      text: '按物料型号汇总明细需求数量，降序取前 10。'
    },
    brandByLineCount: {
      chart: '品牌 Top10（需求明细数）',
      dataSource: '需求明细',
      text: '按品牌汇总需求明细行数，降序取前 10；空品牌显示「未设置」。'
    },
    brandByQty: {
      chart: '品牌 Top10（需求数量）',
      dataSource: '需求明细',
      text: '按品牌汇总明细需求数量，降序取前 10。'
    }
  }
}
