/** 库存明细列表看板各面板口径 Tip */
export const inventoryStockItemListBoardDefsZh = {
  kpi: {
    onHandQty: {
      chart: '在库数量',
      dataSource: '库存明细',
      text: '当前筛选结果全量在库 PCS 合计（时间点=今天），与列表标题下「在库数量」卡一致。'
    },
    amount: {
      chart: '库存金额',
      dataSource: '库存明细',
      text: '在库数量×采购原币单价，按原币分列；不折算混加。无金额权限时为「—」。'
    },
    turnoverDays: {
      chart: '周转天数',
      dataSource: '库存明细 + 出库流水',
      text: '在库数量 ÷ 近 30 天出库数量 × 30，保留 1 位小数。在库为 0 或近 30 天出库为 0 时显示「—」。出库数量取已完成/部分完成出库单明细，优先实出数量。'
    },
    stagnantQty: {
      chart: '呆滞料数量（>90天）',
      dataSource: '库存明细',
      text: '入库日起算超过 90 天（或无入库日）的在库 PCS 合计。点击 KPI 切回列表并带上呆滞筛选。'
    }
  },
  trend: {
    qty: {
      chart: '在库数量趋势',
      dataSource: '库存明细 + 出库流水',
      text: '时间点存量：各周期期末仍在库 PCS。默认日 30 点 / 周 12 点 / 月 12 点。已出清层仍计入历史时间点。若搜索栏填了入库日起止，趋势窗与该区间相交。'
    },
    amount: {
      chart: '库存金额趋势',
      dataSource: '库存明细 + 出库流水',
      text: '同上时间轴，各原币期末在库金额（在库数量×采购单价）；无该原币库存时为 0。'
    }
  },
  breakdown: {
    stockType: {
      chart: '库存类型',
      dataSource: '库存明细',
      text: '客单/备货/样品构成。可按数量（PCS）、金额（各原币或折算USD）或库存条目（当前筛选明细行数）查看。金额下拉「USD」只含采购原币为美元的行；「折算USD」把各原币按入库当时折算美金单价换成美元再加总。'
    },
    warehouse: {
      chart: '仓库',
      dataSource: '库存明细',
      text: '按仓库汇总在库数量、原币金额、折算USD 或库存条目。'
    },
    salesUser: {
      chart: '业务员',
      dataSource: '库存明细',
      text: '无业务员归「未分配业务员」。'
    },
    ageBucket: {
      chart: '库龄分布',
      dataSource: '库存明细',
      text: '按入库日起算，分桶与物流分析一致：0–30 / 31–90 / 91–180 / 181–365 / 365+ 天。无入库日不进饼图。'
    }
  },
  rankings: {
    customerByQty: {
      chart: 'Top10 客户（数量）',
      dataSource: '库存明细',
      text: '按客户在库 PCS 降序；备货归「无客户 / 备货」。点击行切回列表。'
    },
    salesUserByQty: {
      chart: 'Top10 业务员（数量）',
      dataSource: '库存明细',
      text: '按业务员在库 PCS 降序。点击行切回列表。'
    },
    materialByQty: {
      chart: 'Top10 物料（数量）',
      dataSource: '库存明细',
      text: '型号+品牌在库 PCS 降序。点击行切回列表。'
    },
    brandByQty: {
      chart: 'Top10 品牌（数量）',
      dataSource: '库存明细',
      text: '采购品牌在库 PCS 降序。点击行切回列表。'
    },
    customerByLayer: {
      chart: 'Top10 客户（库存条目）',
      dataSource: '库存明细',
      text: '按客户库存明细行数（每条计 1）降序；备货归「无客户 / 备货」。含已出清行（若当前筛选包含）。点击行切回列表，条数应与该行一致。'
    },
    salesUserByLayer: {
      chart: 'Top10 业务员（库存条目）',
      dataSource: '库存明细',
      text: '按业务员库存明细行数降序。点击行切回列表。'
    },
    materialByLayer: {
      chart: 'Top10 物料（库存条目）',
      dataSource: '库存明细',
      text: '型号+品牌库存明细行数降序。点击行切回列表。'
    },
    brandByLayer: {
      chart: 'Top10 品牌（库存条目）',
      dataSource: '库存明细',
      text: '采购品牌库存明细行数降序。点击行切回列表。'
    },
    customerByAmount: {
      chart: 'Top10 客户（金额）',
      dataSource: '库存明细',
      text: '按客户各原币或折算USD 在库金额降序。原币档切回列表与当前原币一致；折算USD 档不过滤原币。'
    },
    salesUserByAmount: {
      chart: 'Top10 业务员（金额）',
      dataSource: '库存明细',
      text: '按业务员各原币或折算USD 在库金额降序。原币档切回列表与当前原币一致；折算USD 档不过滤原币。'
    },
    materialByAmount: {
      chart: 'Top10 物料（金额）',
      dataSource: '库存明细',
      text: '型号+品牌各原币或折算USD 在库金额降序。原币档切回列表与当前原币一致；折算USD 档不过滤原币。'
    },
    brandByAmount: {
      chart: 'Top10 品牌（金额）',
      dataSource: '库存明细',
      text: '品牌各原币或折算USD 在库金额降序。原币档切回列表与当前原币一致；折算USD 档不过滤原币。'
    }
  }
}
