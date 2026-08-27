/** 入库单列表看板各面板口径 Tip（对齐 help/pages/入库单看板） */
export const stockInListBoardDefsZh = {
  kpi: {
    vendors: {
      chart: '入库供应商数',
      dataSource: '入库单',
      text: '当前筛选结果中，单头有供应商的去重供应商数。没有供应商的单不计入。'
    },
    headers: {
      chart: '入库单数',
      dataSource: '入库单',
      text: '当前筛选结果的入库单头数（全量，不是当前页），与列表「共 N 条」同一套条件。'
    },
    amount: {
      chart: '入库金额',
      dataSource: '入库单',
      text: '入库采购单价×入库数量。主数字为折算美金：优先入库过账快照，无快照则按入库数量×采购订单行折算单价，再无则用查询日汇率。可对照原币分档。无金额权限时为「—」。'
    }
  },
  trend: {
    headers: {
      chart: '入库单数',
      dataSource: '入库单',
      text: '按入库日落入日/周/月，统计入库单头数。没有入库日的单不进趋势。'
    },
    amount: {
      chart: '入库金额',
      dataSource: '入库单',
      text: '同上时间轴，入库金额折算美金合计。无金额权限时为「—」。'
    }
  },
  breakdown: {
    stockInType: {
      chart: '入库类型',
      dataSource: '入库单',
      text: '按入库类型汇总入库金额（折算美金）。历史采购类型与采购入库合并。无金额权限时按单数占比。'
    },
    purchaseUser: {
      chart: '采购员',
      dataSource: '入库单',
      text: '按入库单关联采购订单的主采购员汇总入库金额（折算美金）。无金额权限时按单数占比。'
    }
  },
  rankings: {
    vendorByAmount: {
      chart: 'Top10供应商',
      dataSource: '入库单',
      text: '按供应商汇总入库金额（折算美金）降序前 10。没有供应商的单归入「未关联供应商」。'
    },
    purchaseUserByAmount: {
      chart: 'Top10采购员',
      dataSource: '入库单',
      text: '按采购员汇总入库金额（折算美金）降序前 10。未分配采购员单独一档。'
    }
  }
}
