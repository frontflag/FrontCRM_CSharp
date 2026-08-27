/** 收款记录列表看板各面板口径 Tip（对齐 help/pages/收款记录看板） */
export const financeReceiptListBoardDefsZh = {
  kpi: {
    customers: {
      chart: '收款客户数',
      dataSource: '收款记录',
      text: '当前筛选结果中，单头有客户的去重客户数。没有客户的单不计入。'
    },
    amount: {
      chart: '收款金额',
      dataSource: '收款记录',
      text: '按收款单头金额分币别合计，不把不同原币加总，也不折算美金。无金额权限时为「—」。'
    }
  },
  trend: {
    headers: {
      chart: '收款单数',
      dataSource: '收款记录',
      text: '按收款日落入日/周/月，统计收款单头数。没有收款日的单不进趋势。'
    },
    amount: {
      chart: '收款金额',
      dataSource: '收款记录',
      text: '同上时间轴，按原币各画一张图，金额为单头收款金额。无金额权限时为「—」。'
    }
  },
  breakdown: {
    verificationStatus: {
      chart: '核销状态',
      dataSource: '收款记录',
      text: '按整单核销状态统计收款单数：无明细或全部未核销为未核销；全部核销完成为核销完成；其余为部分核销。'
    },
    salesUser: {
      chart: '业务员',
      dataSource: '收款记录',
      text: '按收款单头业务员、分原币汇总收款金额。未填业务员归「未分配业务员」。无金额权限时按单数占比。'
    }
  },
  rankings: {
    customerByAmount: {
      chart: 'Top10客户',
      dataSource: '收款记录',
      text: '每个原币各自按收款金额降序取前 10。没有客户的单归入「未关联客户」。'
    },
    salesUserByAmount: {
      chart: 'Top10业务员',
      dataSource: '收款记录',
      text: '每个原币各自按收款金额降序取前 10。未分配业务员单独一档。'
    }
  }
}
