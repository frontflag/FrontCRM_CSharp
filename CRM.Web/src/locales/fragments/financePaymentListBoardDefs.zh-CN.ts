/** 付款记录列表看板各面板口径 Tip（对齐 help/pages/付款记录看板） */
export const financePaymentListBoardDefsZh = {
  kpi: {
    vendors: {
      chart: '付款供应商数',
      dataSource: '付款记录',
      text: '当前筛选结果中，单头有供应商的去重供应商数。没有供应商的单不计入。'
    },
    amount: {
      chart: '付款金额',
      dataSource: '付款记录',
      text: '已付货款：各明细已付金额合计，不含手续费、运费等费用。按付款单原币分行，不把不同原币加总，也不折算美金。未付款明细为 0。无金额权限时为「—」。'
    }
  },
  trend: {
    headers: {
      chart: '付款单数',
      dataSource: '付款记录',
      text: '按付款日落入日/周/月，统计付款单头数。没有付款日的单不进趋势。'
    },
    amount: {
      chart: '付款金额',
      dataSource: '付款记录',
      text: '同上时间轴，按原币各画一张图，金额为明细已付货款合计（不含费用）。无金额权限时为「—」。'
    }
  },
  breakdown: {
    verificationStatus: {
      chart: '核销状态',
      dataSource: '付款记录',
      text: '按整单核销状态统计付款单数：无明细或全部未核销为未核销；全部核销完成为核销完成；其余为部分核销。'
    },
    purchaseUser: {
      chart: '采购员',
      dataSource: '付款记录',
      text: '按付款明细所属采购订单的采购员、分原币汇总已付货款。一张单可拆到多名采购员。未填采购员归「未分配采购员」。无金额权限时按单数占比。'
    }
  },
  rankings: {
    vendorByAmount: {
      chart: 'Top10供应商',
      dataSource: '付款记录',
      text: '每个原币各自按已付货款降序取前 10。没有供应商的单归入「未关联供应商」。'
    },
    purchaseUserByAmount: {
      chart: 'Top10采购员',
      dataSource: '付款记录',
      text: '每个原币各自按已付货款降序取前 10。未分配采购员单独一档。一张付款单可计入多名采购员。'
    }
  }
}
