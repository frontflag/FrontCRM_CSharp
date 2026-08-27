/** 出库明细看板各面板口径 Tip（对齐 help/pages/出库明细看板） */
export const stockOutItemListBoardDefsZh = {
  kpi: {
    customers: {
      chart: '出库客户数',
      dataSource: '出库明细',
      text: '当前筛选结果中，出库单头有客户的去重客户数。没有客户的行不计入。'
    },
    lines: {
      chart: '出库条目数',
      dataSource: '出库明细',
      text: '当前筛选结果的出库明细行数（全量，不是当前页）。'
    },
    amount: {
      chart: '出库金额',
      dataSource: '出库明细',
      text: '销售单价×出库数量。主数字为折算美金：优先出库过账快照，无快照则按出库数量×订单行折算单价，再无则用查询日汇率。可对照原币分档。无金额权限时为「—」。'
    }
  },
  trend: {
    lines: {
      chart: '出库条目数',
      dataSource: '出库明细',
      text: '按出库日落入日/周/月，统计明细行数。没有出库日的行不进趋势。'
    },
    amount: {
      chart: '出库金额',
      dataSource: '出库明细',
      text: '同上时间轴，出库金额折算美金合计。无金额权限时为「—」。'
    }
  },
  breakdown: {
    stockOutType: {
      chart: '出库类型',
      dataSource: '出库明细',
      text: '按出库类型汇总出库金额（折算美金）。历史销售类型与销售出库合并。无金额权限时按行数占比。'
    },
    salesUser: {
      chart: '业务员',
      dataSource: '出库明细',
      text: '按出库单关联销售订单的业务员汇总出库金额（折算美金）。无金额权限时按行数占比。'
    }
  },
  rankings: {
    customerByAmount: {
      chart: 'Top10客户',
      dataSource: '出库明细',
      text: '按客户汇总出库金额（折算美金）降序前 10。没有客户的行归入「未关联客户」。'
    },
    salesUserByAmount: {
      chart: 'Top10业务员',
      dataSource: '出库明细',
      text: '按业务员汇总出库金额（折算美金）降序前 10。未分配业务员单独一档。'
    }
  }
}
