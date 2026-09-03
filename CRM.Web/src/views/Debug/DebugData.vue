<template>
  <div class="debug-page">
    <div class="debug-header">
      <h1>Debug 模拟数据</h1>
      <div class="debug-sub muted">
        业务链路模拟写入数据库，需登录后使用。
        <router-link class="debug-link" to="/debug">返回 Debug</router-link>
        <router-link class="debug-link" to="/debug/tools">打开 Debug 工具</router-link>
      </div>
    </div>

    <section class="debug-panel panel-simulate">
      <h2 class="panel-title">业务链路模拟数据</h2>
      <div class="panel-body simulate-form simulate-form--row1">
        <div class="simulate-form__group">
          <span class="simulate-form__inline-label">数据来源：</span>
          <el-select v-model="simulateForm.dataOrigin" placeholder="数据起源" style="width: 140px">
            <el-option v-for="opt in dataOriginOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
          <el-input
            v-if="simulateForm.dataOrigin !== 'ignore'"
            v-model="simulateForm.originReferenceCode"
            :placeholder="originCodePlaceholder"
            clearable
            style="width: 220px"
          />
        </div>
        <div class="simulate-form__group simulate-form__group--generate">
          <span class="simulate-form__inline-label">生成：</span>
          <el-select v-model="simulateForm.businessNode" placeholder="选择业务节点" style="width: 220px">
            <el-option v-for="opt in businessNodeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
          </el-select>
          <el-select v-model="simulateForm.status" placeholder="选择状态" style="width: 260px">
            <el-option
              v-for="opt in currentStatusOptions"
              :key="`${simulateForm.businessNode}-${opt.value}`"
              :label="`${opt.value} - ${opt.label}`"
              :value="opt.value"
            />
          </el-select>
        </div>
        <el-button type="primary" :loading="simulating" @click="onSimulate">生成链路数据</el-button>
      </div>
      <div class="simulate-tip">
        按业务节点选择状态枚举值，系统将自动补齐当前节点前序链路。「数据起源」选「忽略」时与旧版一致；选客户/供应商/订单时需填写对应编号，并从该实体衔接后续模拟数据。
      </div>
      <div v-if="simulateResult" class="simulate-result">
        <div>链路号：<span class="mono">{{ simulateResult.chainNo }}</span></div>
        <div>节点：{{ simulateResult.businessNode }}，状态：{{ simulateResult.targetStatus }}</div>
        <div>创建结果：{{ simulateResult.createdNodes.join(' -> ') }}</div>
      </div>
    </section>

    <section class="debug-panel panel-chain">
      <h2 class="panel-title">删除数据链（按需求单号）</h2>
      <p class="chain-tip">
        输入 <strong>RFQ 需求单号</strong>（<span class="mono">rfq.rfq_code</span>），查询从该需求产生的下游业务节点与编号；删除将移除这些关联数据（含库存/出库/拣货等与造数链路一致的表）。操作不可恢复，仅限调试环境使用。
      </p>
      <div class="panel-body chain-toolbar">
        <span class="simulate-form__inline-label">需求单号：</span>
        <el-input
          v-model="rfqChainCode"
          clearable
          placeholder="例如 RFQ2504180001"
          style="width: 280px"
          @keyup.enter="onPreviewRfqChain"
        />
        <el-button type="primary" plain :loading="chainLoading" @click="onPreviewRfqChain">查询下游链</el-button>
        <el-button type="danger" :loading="chainDeleting" :disabled="!chainPreview?.nodes?.length" @click="onDeleteRfqChain">
          删除下游数据
        </el-button>
      </div>
      <div v-if="chainError" class="chain-error">{{ chainError }}</div>
      <el-table
        v-if="chainPreview && chainPreview.nodes.length"
        :data="chainPreview.nodes"
        border
        stripe
        size="small"
        class="chain-table"
        max-height="420"
      >
        <el-table-column prop="node" label="业务节点" width="180" />
        <el-table-column prop="code" label="数据编号（业务号）" min-width="200" show-overflow-tooltip>
          <template #default="{ row }">
            <span class="mono">{{ row.code }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="id" label="主键 Id" min-width="280" show-overflow-tooltip>
          <template #default="{ row }">
            <span class="mono">{{ row.id }}</span>
          </template>
        </el-table-column>
      </el-table>
      <div v-else-if="chainSearched && !chainLoading && !chainError" class="chain-empty">未查询到数据或无下游记录</div>
    </section>

    <section class="debug-panel panel-temp">
      <h2 class="panel-title">临时</h2>
      <div class="panel-body agency-rate-refresh">
        <span class="simulate-form__inline-label">报关公司：</span>
        <el-select
          v-model="agencyRateBrokerId"
          filterable
          clearable
          placeholder="选择报关公司"
          style="width: 320px"
          :loading="agencyRateBrokersLoading"
        >
          <el-option
            v-for="b in agencyRateBrokers"
            :key="b.id"
            :label="`${b.brokerCode} ${b.cname}`"
            :value="b.id"
          />
        </el-select>
        <span class="simulate-form__inline-label">代理费率：</span>
        <el-input-number
          v-model="agencyRateInput"
          :min="1"
          :precision="6"
          :step="0.001"
          :controls="false"
          style="width: 160px"
        />
        <el-button
          type="warning"
          :loading="refreshingCustomsAgencyRate"
          :disabled="!agencyRateBrokerId"
          @click="onRefreshCustomsAgencyRate"
        >
          刷新报关代理费率
        </el-button>
      </div>
      <div class="refresh-hint">
        「刷新报关代理费率」：按输入费率（1+纯费率，如 1.025000 = 2.5%）重算该公司系统模式报关单的代理费与 P1，并回写报关到货/入库成本。已结关锁定单也会强制重算。不改报关公司资料；手工费率单跳过。仅调试使用。
      </div>
      <div v-if="customsAgencyRateRefreshResult" class="simulate-result">
        <div>扫描报关单：{{ customsAgencyRateRefreshResult.totalDeclarations }} 条</div>
        <div>已刷新：{{ customsAgencyRateRefreshResult.refreshedDeclarations }} 条</div>
        <div>费用有变化：{{ customsAgencyRateRefreshResult.feesChangedDeclarations }} 条</div>
        <div>跳过作废：{{ customsAgencyRateRefreshResult.skippedVoided }} 条</div>
        <div>跳过手工：{{ customsAgencyRateRefreshResult.skippedManual }} 条</div>
        <div>跳过无费用快照：{{ customsAgencyRateRefreshResult.skippedNoFees }} 条</div>
        <div>到货成本更新：{{ customsAgencyRateRefreshResult.arrivalNoticesUpdated }} 条</div>
        <div>入库明细更新：{{ customsAgencyRateRefreshResult.stockInItemsUpdated }} 条</div>
        <div>在库层成本更新：{{ customsAgencyRateRefreshResult.stockItemLayersUpdated }} 条</div>
        <div>失败：{{ customsAgencyRateRefreshResult.failedCount }} 条</div>
        <div v-if="customsAgencyRateRefreshResult.refreshedDeclarationCodes.length">
          已刷新单号（最多 50 条）：{{ customsAgencyRateRefreshResult.refreshedDeclarationCodes.join('，') }}
        </div>
        <div v-if="customsAgencyRateRefreshResult.failedMessages.length">
          失败明细：{{ customsAgencyRateRefreshResult.failedMessages.join('；') }}
        </div>
      </div>
      <div class="panel-body refresh-actions">
        <el-button type="danger" :loading="refreshingStockLedger" @click="onRefreshStockLedger">
          刷新stockledger
        </el-button>
        <el-button type="danger" :loading="recalculatingStock" @click="onRecalculateStockAggregates">
          重算库存
        </el-button>
        <el-button type="warning" :loading="refreshingSellOrderComments" @click="onRefreshSellOrderCommentSplit">
          刷新Sellorder
        </el-button>
        <el-button type="primary" :loading="refreshingSellOrderMainStatus" @click="onRefreshSellOrderMainStatus">
          刷新销售订单状态
        </el-button>
        <el-button
          type="primary"
          :loading="refreshingSellOrderItemExtendOutboundProfit"
          @click="onRefreshSellOrderItemExtendOutboundProfit"
        >
          刷新出库利润
        </el-button>
        <el-button type="primary" :loading="refreshingPurchaseOrderMainStatus" @click="onRefreshPurchaseOrderMainStatus">
          刷新采购订单状态
        </el-button>
        <el-button type="success" :loading="refreshingArrivalNoticeStatuses" @click="onRefreshArrivalNoticeStatuses">
          刷新到货通知状态
        </el-button>
        <el-button type="warning" :loading="refreshingSellOrderItemCustomerPn" @click="onRefreshSellOrderItemCustomerPn">
          刷新sellorderitem
        </el-button>
        <el-button type="primary" :loading="refreshingFinancePaymentRemark" @click="onRefreshFinancePaymentRemark">
          刷新付款备注
        </el-button>
        <el-button type="success" :loading="refreshingFinanceReceivables" @click="onRefreshFinanceReceivables">
          刷新应收款
        </el-button>
        <el-button type="primary" :loading="refreshingRfqMaterialIntel" @click="onRefreshRfqMaterialIntel">
          刷AI需求物料
        </el-button>
      </div>
      <div class="refresh-hint">回填 STOCK_OUT / STOCK_OUT_REVERSE 的 UnitCost、Amount、currency（调试临时工具）。</div>
      <div class="refresh-hint refresh-hint--second">
        「重算库存」：按未软删 <span class="mono">stock_item</span> 全库回写 <span class="mono">stock</span> 汇总桶数量（修复删除入库/明细后汇总滞后；库存中心与出货通知「在库可用」将同步为真实值）。仅调试使用。
      </div>
      <div v-if="stockRecalculateResult" class="simulate-result">
        <div>扫描汇总桶：{{ stockRecalculateResult.totalBuckets }} 个</div>
        <div>修正桶数：{{ stockRecalculateResult.bucketsUpdated }} 个</div>
        <div>可用量高估合计：{{ stockRecalculateResult.totalAvailOverstatement }}</div>
        <div v-if="stockRecalculateResult.updatedStockCodes.length">
          已修正编号（最多 30 条）：{{ stockRecalculateResult.updatedStockCodes.join('，') }}
        </div>
      </div>
      <div v-if="stockLedgerRefreshResult" class="simulate-result">
        <div>STOCK_OUT 更新：{{ stockLedgerRefreshResult.stockOutUpdated }} 条</div>
        <div>STOCK_OUT_REVERSE 更新：{{ stockLedgerRefreshResult.stockOutReverseUpdated }} 条</div>
        <div>币别兜底（currency<=0）：{{ stockLedgerRefreshResult.currencyDefaulted }} 条</div>
      </div>
      <div class="refresh-hint refresh-hint--second">
        「刷新Sellorder」：仅当 <span class="mono">comment</span> 仍为历史多行前缀格式时，拆入
        <span class="mono">product_kind</span> 等列并将自由段写回 <span class="mono">comment</span>；普通一句话备注不会改（含软删行）。仅调试使用。
      </div>
      <div v-if="sellOrderCommentSplitResult" class="simulate-result">
        <div>扫描（comment 非空）：{{ sellOrderCommentSplitResult.totalWithComment }} 条</div>
        <div>已执行 legacy 拆分：{{ sellOrderCommentSplitResult.rowsProcessed }} 条</div>
      </div>
      <div class="refresh-hint refresh-hint--second">
        「刷新销售订单状态」：遍历未软删销售订单，逐单调用详情页「刷新状态」同源接口，重算明细扩展并按规则同步主状态（审核通过→进行中、全部收款完成→完成等）。不覆盖售价。跳过取消/审核失败单。耗时可较长，请勿重复点击。
      </div>
      <div v-if="sellOrderMainStatusResult" class="simulate-result">
        <div>扫描订单：{{ sellOrderMainStatusResult.totalOrders }} 条</div>
        <div>主状态变更：{{ sellOrderMainStatusResult.changedOrders }} 条</div>
        <div>跳过终态（取消/审核失败）：{{ sellOrderMainStatusResult.skippedTerminalOrders }} 条</div>
        <div>失败：{{ sellOrderMainStatusResult.failedCount }} 条</div>
        <div v-if="sellOrderMainStatusResult.changedOrderCodes.length">
          变更单号（最多 50 条）：{{ sellOrderMainStatusResult.changedOrderCodes.join('，') }}
        </div>
        <div v-if="sellOrderMainStatusResult.failedMessages.length">
          失败明细：{{ sellOrderMainStatusResult.failedMessages.join('；') }}
        </div>
      </div>
      <div class="refresh-hint refresh-hint--second">
        「刷新出库利润」：遍历未软删销售明细扩展，逐行重算出库利润（<span class="mono">ProfitOutBizUsd</span> /
        <span class="mono">ProfitOutRateBiz</span>）；成本优先取 <span class="mono">stock_out_item_extend</span>
        真实采购价，无批次快照时回退 PO 加权均价。与绩效面板、离线 SQL 脚本同源，不刷新订单主状态。耗时可较长，请勿重复点击。
      </div>
      <div v-if="sellOrderItemExtendOutboundProfitResult" class="simulate-result">
        <div>扫描明细：{{ sellOrderItemExtendOutboundProfitResult.totalLines }} 条</div>
        <div>有已出库数量：{{ sellOrderItemExtendOutboundProfitResult.linesWithOutboundQty }} 条</div>
        <div>出库利润变更：{{ sellOrderItemExtendOutboundProfitResult.profitChangedCount }} 条</div>
        <div>失败：{{ sellOrderItemExtendOutboundProfitResult.failedCount }} 条</div>
        <div v-if="sellOrderItemExtendOutboundProfitResult.changedLineCodes.length">
          变更明细号（最多 50 条）：{{ sellOrderItemExtendOutboundProfitResult.changedLineCodes.join('，') }}
        </div>
        <div v-if="sellOrderItemExtendOutboundProfitResult.failedMessages.length">
          失败明细：{{ sellOrderItemExtendOutboundProfitResult.failedMessages.join('；') }}
        </div>
      </div>
      <div class="refresh-hint refresh-hint--second">
        「刷新采购订单状态」：遍历全部采购订单，逐单调用详情页「刷新扩展」同源接口，重算明细扩展、同步明细状态与主状态（部分付款/部分入库→进行中，全部采购完成→采购完成）。跳过取消/审核失败单。耗时可较长，请勿重复点击。
      </div>
      <div v-if="purchaseOrderMainStatusResult" class="simulate-result">
        <div>扫描订单：{{ purchaseOrderMainStatusResult.totalOrders }} 条</div>
        <div>扫描明细：{{ purchaseOrderMainStatusResult.totalItems }} 条</div>
        <div>明细有变更：{{ purchaseOrderMainStatusResult.changedItems }} 条</div>
        <div>主状态变更：{{ purchaseOrderMainStatusResult.changedOrders }} 条</div>
        <div>跳过终态（取消/审核失败）：{{ purchaseOrderMainStatusResult.skippedTerminalOrders }} 条</div>
        <div>失败：{{ purchaseOrderMainStatusResult.failedCount }} 条</div>
        <div v-if="purchaseOrderMainStatusResult.changedOrderCodes.length">
          主状态变更单号（最多 50 条）：{{ purchaseOrderMainStatusResult.changedOrderCodes.join('，') }}
        </div>
        <div v-if="purchaseOrderMainStatusResult.failedMessages.length">
          失败明细：{{ purchaseOrderMainStatusResult.failedMessages.join('；') }}
        </div>
      </div>
      <div class="refresh-hint refresh-hint--second">
        「刷新到货通知状态」：遍历全部未软删到货通知，按已过账采购入库单 / 质检 / 收货量重算
        <span class="mono">stockin_notify.Status</span>（10/20/30/100）。用于修正「已有入库仍显示已质检」等历史不同步；与采购扩展重算内到货状态逻辑同源，不刷新采购扩展表。仅调试使用。
      </div>
      <div v-if="arrivalNoticeStatusRefreshResult" class="simulate-result">
        <div>扫描到货通知：{{ arrivalNoticeStatusRefreshResult.totalNotices }} 条</div>
        <div>状态变更：{{ arrivalNoticeStatusRefreshResult.changedCount }} 条</div>
        <div>修正为已入库(100)：{{ arrivalNoticeStatusRefreshResult.toStockedInCount }} 条</div>
        <div v-if="arrivalNoticeStatusRefreshResult.changedNoticeCodes.length">
          变更单号（最多 50 条）：{{ arrivalNoticeStatusRefreshResult.changedNoticeCodes.join('，') }}
        </div>
      </div>
      <div class="refresh-hint refresh-hint--second">
        「刷新sellorderitem」：从 <span class="mono">sellorderitem.comment</span> 首行「客户物料型号：」等前缀解析，写入
        <span class="mono">customer_pn</span>（仅 <span class="mono">customer_pn</span> 为空时写入；不改 comment；含软删行）。
      </div>
      <div v-if="sellOrderItemCustomerPnResult" class="simulate-result">
        <div>扫描（comment 非空）：{{ sellOrderItemCustomerPnResult.totalWithComment }} 条</div>
        <div>已回填 customer_pn：{{ sellOrderItemCustomerPnResult.rowsFilled }} 条</div>
      </div>
      <div class="refresh-hint refresh-hint--second">
        「刷新付款备注」：识别采购请款旧版写入 <span class="mono">financepayment.Remark</span> 的管道串；<span class="mono">供应商银行:</span> 后为名称或历史占位串时，按 <span class="mono">financepaymentbank.BankName</span> 匹配写入真实主键。随后会扫描全表已填写的 <span class="mono">FinancePaymentBankId</span>：若非表内主键则再按名称解析一次（用于纠正已误写入的「名称/slug」）。仅调试使用。
      </div>
      <div v-if="financePaymentRemarkLegacyResult" class="simulate-result">
        <div>Remark 非空付款单：{{ financePaymentRemarkLegacyResult.totalPaymentsRemarkNonEmpty }} 条</div>
        <div>命中旧版打包形态：{{ financePaymentRemarkLegacyResult.legacyPackedCandidates }} 条</div>
        <div>已解析并写库：{{ financePaymentRemarkLegacyResult.parsedAndApplied }} 条</div>
        <div>形态命中但费用段无法解析：{{ financePaymentRemarkLegacyResult.skippedMalformed }} 条</div>
        <div>明细 LineRemark 更新：{{ financePaymentRemarkLegacyResult.itemsLineRemarkUpdated }} 条</div>
        <div>付款银行 Id 按名称纠正：{{ financePaymentRemarkLegacyResult.bankIdsResolvedFromName }} 条</div>
      </div>
      <div class="refresh-hint refresh-hint--second">
        「刷新应收款」：仅扫描销售出库（含历史类型 1 与现行 10）且状态为「出库完成」(4) 的单据并补生成应收；同时软删出库单尚未完成时误生成的历史应收。仅调试使用。
      </div>
      <div v-if="financeReceivableRefreshResult" class="simulate-result">
        <div>出库完成(4) 销售出库：{{ financeReceivableRefreshResult.totalCompletedSalesStockOuts }} 条</div>
        <div>已有应收款：{{ financeReceivableRefreshResult.alreadyHasReceivableCount }} 条</div>
        <div>待补生成候选：{{ financeReceivableRefreshResult.candidateCount }} 条</div>
        <div>新建应收款：{{ financeReceivableRefreshResult.createdCount }} 条</div>
        <div>补写出库日期：{{ financeReceivableRefreshResult.stockOutDatesSyncedCount }} 条</div>
        <div>移除过早应收：{{ financeReceivableRefreshResult.prematureReceivablesRemovedCount }} 条</div>
        <div>跳过（不满足条件）：{{ financeReceivableRefreshResult.skippedIneligibleCount }} 条</div>
        <div>失败：{{ financeReceivableRefreshResult.failedCount }} 条</div>
        <div v-if="financeReceivableRefreshResult.createdStockOutCodes.length">
          新建出库单号：{{ financeReceivableRefreshResult.createdStockOutCodes.join('，') }}
        </div>
        <div v-if="financeReceivableRefreshResult.skippedIneligibleStockOutCodes.length">
          跳过出库单号：{{ financeReceivableRefreshResult.skippedIneligibleStockOutCodes.join('，') }}
        </div>
        <div v-if="financeReceivableRefreshResult.stockOutDatesSyncedStockOutCodes.length">
          补写出库日期单号：{{ financeReceivableRefreshResult.stockOutDatesSyncedStockOutCodes.join('，') }}
        </div>
        <div v-if="financeReceivableRefreshResult.prematureReceivablesRemovedStockOutCodes.length">
          移除过早应收单号：{{ financeReceivableRefreshResult.prematureReceivablesRemovedStockOutCodes.join('，') }}
        </div>
        <div v-if="financeReceivableRefreshResult.failedMessages.length">
          失败明细：{{ financeReceivableRefreshResult.failedMessages.join('；') }}
        </div>
      </div>
      <div class="refresh-hint refresh-hint--second">
        「刷AI需求物料」：遍历未软删 <span class="mono">rfqitem.mpn</span> 去重；对尚无
        <span class="mono">material.intel.lookup</span> AI 缓存的物料型号后台触发查询（执行类型=系统补刷）。耗时可较长，请勿重复点击。
      </div>
      <div v-if="rfqMaterialIntelRefreshResult" class="simulate-result">
        <div>需求明细行数：{{ rfqMaterialIntelRefreshResult.totalRfqItemRows }}</div>
        <div>去重物料型号：{{ rfqMaterialIntelRefreshResult.distinctPnCount }}</div>
        <div>已有 AI 缓存：{{ rfqMaterialIntelRefreshResult.alreadyCachedCount }}</div>
        <div>新触发查询：{{ rfqMaterialIntelRefreshResult.invokedCount }}</div>
        <div>失败：{{ rfqMaterialIntelRefreshResult.failedCount }}</div>
        <div v-if="rfqMaterialIntelRefreshResult.invokedPns.length">
          已触发 PN（最多 30 条）：{{ rfqMaterialIntelRefreshResult.invokedPns.join('，') }}
        </div>
        <div v-if="rfqMaterialIntelRefreshResult.failedMessages.length">
          失败明细：{{ rfqMaterialIntelRefreshResult.failedMessages.join('；') }}
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  simulateBusinessChain,
  getRfqChainPreview,
  deleteRfqChain,
  refreshStockLedger,
  refreshSellOrderCommentSplit,
  refreshSellOrderMainStatus,
  refreshSellOrderItemExtendOutboundProfit,
  refreshPurchaseOrderMainStatus,
  refreshArrivalNoticeStatuses,
  refreshSellOrderItemCustomerPnFromComment,
  refreshFinancePaymentRemarkFromLegacy,
  refreshFinanceReceivablesFromStockOuts,
  recalculateStockAggregates,
  refreshRfqMaterialIntelCache,
  refreshCustomsAgencyRate,
  type SimulateBusinessChainResponse,
  type SimulateDataOrigin,
  type RfqChainPreview,
  type RefreshStockLedgerResult,
  type RecalculateStockAggregatesResult,
  type RefreshSellOrderCommentSplitResult,
  type RefreshSellOrderMainStatusResult,
  type RefreshSellOrderItemExtendOutboundProfitResult,
  type RefreshPurchaseOrderMainStatusResult,
  type RefreshArrivalNoticeStatusesResult,
  type RefreshSellOrderItemCustomerPnFromCommentResult,
  type RefreshFinancePaymentLegacyRemarkResult,
  type RefreshFinanceReceivablesFromStockOutsResult,
  type RefreshRfqMaterialIntelCacheResult,
  type RefreshCustomsAgencyRateResult
} from '@/api/debug'
import { fetchCustomsBrokersAdmin, type CustomsBrokerDto } from '@/api/customs'
import { isValidCustomsAgencyRate } from '@/utils/customsAgencyRate'
import { getApiErrorMessage } from '@/utils/apiError'

const simulating = ref(false)
const simulateResult = ref<SimulateBusinessChainResponse | null>(null)
const dataOriginOptions: { label: string; value: SimulateDataOrigin }[] = [
  { label: '忽略', value: 'ignore' },
  { label: '客户', value: 'customer' },
  { label: '供应商', value: 'vendor' },
  { label: '销售订单', value: 'salesorder' },
  { label: '采购订单', value: 'purchaseorder' }
]

const simulateForm = ref({
  businessNode: 'stockin',
  status: 2,
  dataOrigin: 'ignore' as SimulateDataOrigin,
  originReferenceCode: ''
})

const originCodePlaceholder = computed(() => {
  switch (simulateForm.value.dataOrigin) {
    case 'customer':
      return '客户编号'
    case 'vendor':
      return '供应商编码'
    case 'salesorder':
      return '销售订单编号'
    case 'purchaseorder':
      return '采购订单编号'
    default:
      return ''
  }
})

const businessNodeOptions = [
  { label: 'RFQ', value: 'rfq' },
  { label: 'Quote', value: 'quote' },
  { label: 'SalesOrder', value: 'salesorder' },
  { label: 'PurchaseRequisition', value: 'purchaserequisition' },
  { label: 'PurchaseOrder', value: 'purchaseorder' },
  { label: 'StockInNotify', value: 'stockinnotify' },
  { label: 'QC', value: 'qc' },
  { label: 'StockIn', value: 'stockin' },
  { label: 'StockOutRequest', value: 'stockoutrequest' }
]

type StatusOption = { value: number; label: string }

const statusOptionsByNode: Record<string, StatusOption[]> = {
  rfq: [
    { value: 0, label: '待分配' },
    { value: 1, label: '已分配' },
    { value: 2, label: '报价中' },
    { value: 3, label: '已报价' },
    { value: 4, label: '已选价' },
    { value: 5, label: '已转订单' },
    { value: 6, label: '已关闭' }
  ],
  quote: [
    { value: 0, label: '新建' },
    { value: 1, label: '成单' },
    { value: 2, label: '关闭' }
  ],
  salesorder: [
    { value: 1, label: '新建' },
    { value: 2, label: '待审核' },
    { value: 10, label: '审核通过' },
    { value: 20, label: '进行中' },
    { value: 100, label: '完成' },
    { value: -1, label: '审核失败' },
    { value: -2, label: '取消' }
  ],
  purchaserequisition: [
    { value: 0, label: '新建' },
    { value: 1, label: '部分完成' },
    { value: 2, label: '全部完成' },
    { value: 3, label: '已取消' }
  ],
  purchaseorder: [
    { value: 1, label: '新建' },
    { value: 2, label: '待审核' },
    { value: 10, label: '审核通过' },
    { value: 20, label: '待确认' },
    { value: 30, label: '已确认' },
    { value: 50, label: '进行中' },
    { value: 100, label: '采购完成' },
    { value: -1, label: '审核失败' },
    { value: -2, label: '取消' }
  ],
  stockinnotify: [
    { value: 1, label: '新建' },
    { value: 10, label: '未到货' },
    { value: 20, label: '到货待检' },
    { value: 30, label: '已质检' },
    { value: 100, label: '已入库' }
  ],
  qc: [
    { value: -1, label: '未通过' },
    { value: 10, label: '部分通过' },
    { value: 100, label: '已通过' }
  ],
  stockin: [
    { value: 0, label: '草稿' },
    { value: 1, label: '待入库' },
    { value: 2, label: '已入库' },
    { value: 3, label: '已取消' }
  ],
  stockoutrequest: [
    { value: 10, label: '待装箱' },
    { value: 20, label: '已装箱' },
    { value: 100, label: '已出库' },
    { value: -1, label: '已取消' }
  ]
}

const currentStatusOptions = computed<StatusOption[]>(
  () => statusOptionsByNode[simulateForm.value.businessNode] ?? [{ value: 0, label: '默认' }]
)

watch(
  () => simulateForm.value.dataOrigin,
  (origin) => {
    if (origin === 'ignore') simulateForm.value.originReferenceCode = ''
  }
)

watch(
  () => simulateForm.value.businessNode,
  (node) => {
    const first = (statusOptionsByNode[node] ?? [])[0]
    if (!first) return
    const exists = (statusOptionsByNode[node] ?? []).some(x => x.value === simulateForm.value.status)
    if (!exists) simulateForm.value.status = first.value
  },
  { immediate: true }
)

const rfqChainCode = ref('')
const chainLoading = ref(false)
const chainDeleting = ref(false)
const chainPreview = ref<RfqChainPreview | null>(null)
const chainError = ref<string | null>(null)
const chainSearched = ref(false)
const refreshingStockLedger = ref(false)
const stockLedgerRefreshResult = ref<RefreshStockLedgerResult | null>(null)
const recalculatingStock = ref(false)
const stockRecalculateResult = ref<RecalculateStockAggregatesResult | null>(null)
const refreshingSellOrderComments = ref(false)
const sellOrderCommentSplitResult = ref<RefreshSellOrderCommentSplitResult | null>(null)
const refreshingSellOrderMainStatus = ref(false)
const sellOrderMainStatusResult = ref<RefreshSellOrderMainStatusResult | null>(null)
const refreshingSellOrderItemExtendOutboundProfit = ref(false)
const sellOrderItemExtendOutboundProfitResult = ref<RefreshSellOrderItemExtendOutboundProfitResult | null>(null)
const refreshingPurchaseOrderMainStatus = ref(false)
const purchaseOrderMainStatusResult = ref<RefreshPurchaseOrderMainStatusResult | null>(null)
const refreshingArrivalNoticeStatuses = ref(false)
const arrivalNoticeStatusRefreshResult = ref<RefreshArrivalNoticeStatusesResult | null>(null)
const refreshingSellOrderItemCustomerPn = ref(false)
const sellOrderItemCustomerPnResult = ref<RefreshSellOrderItemCustomerPnFromCommentResult | null>(null)
const refreshingFinancePaymentRemark = ref(false)
const financePaymentRemarkLegacyResult = ref<RefreshFinancePaymentLegacyRemarkResult | null>(null)
const refreshingFinanceReceivables = ref(false)
const financeReceivableRefreshResult = ref<RefreshFinanceReceivablesFromStockOutsResult | null>(null)
const refreshingRfqMaterialIntel = ref(false)
const rfqMaterialIntelRefreshResult = ref<RefreshRfqMaterialIntelCacheResult | null>(null)
const agencyRateBrokers = ref<CustomsBrokerDto[]>([])
const agencyRateBrokersLoading = ref(false)
const agencyRateBrokerId = ref('')
const agencyRateInput = ref(1)
const refreshingCustomsAgencyRate = ref(false)
const customsAgencyRateRefreshResult = ref<RefreshCustomsAgencyRateResult | null>(null)

onMounted(async () => {
  agencyRateBrokersLoading.value = true
  try {
    agencyRateBrokers.value = await fetchCustomsBrokersAdmin()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '加载报关公司失败'))
  } finally {
    agencyRateBrokersLoading.value = false
  }
})

watch(agencyRateBrokerId, (id) => {
  const broker = agencyRateBrokers.value.find((b) => b.id === id)
  if (!broker) return
  const rate = Number(broker.agencyRate)
  agencyRateInput.value = Number.isFinite(rate) && rate >= 1 ? rate : 1
})

const onPreviewRfqChain = async () => {
  const code = rfqChainCode.value.trim()
  if (!code) {
    ElMessage.warning('请输入需求单号')
    return
  }
  chainLoading.value = true
  chainError.value = null
  chainSearched.value = true
  try {
    chainPreview.value = await getRfqChainPreview(code)
    if (!chainPreview.value.nodes.length) {
      ElMessage.info('未找到下游数据（需求可能不存在或无关联单据）')
    }
  } catch (e) {
    chainPreview.value = null
    chainError.value = getApiErrorMessage(e, '查询失败')
    ElMessage.error(chainError.value)
  } finally {
    chainLoading.value = false
  }
}

const onDeleteRfqChain = async () => {
  const code = rfqChainCode.value.trim()
  if (!code) {
    ElMessage.warning('请输入需求单号')
    return
  }
  if (!chainPreview.value?.nodes?.length) {
    ElMessage.warning('请先查询下游链')
    return
  }
  try {
    await ElMessageBox.confirm(
      `将永久删除需求「${code}」及其下游全部关联数据（见上表），不可恢复。是否继续？`,
      '确认删除',
      { type: 'warning', confirmButtonText: '删除', cancelButtonText: '取消' }
    )
  } catch {
    return
  }
  chainDeleting.value = true
  chainError.value = null
  try {
    await deleteRfqChain(code)
    ElMessage.success('已删除')
    chainPreview.value = null
    chainSearched.value = false
    rfqChainCode.value = ''
  } catch (e) {
    chainError.value = getApiErrorMessage(e, '删除失败')
    ElMessage.error(chainError.value)
  } finally {
    chainDeleting.value = false
  }
}

const onSimulate = async () => {
  const origin = simulateForm.value.dataOrigin
  if (origin !== 'ignore') {
    const code = simulateForm.value.originReferenceCode.trim()
    if (!code) {
      ElMessage.warning(`请填写${originCodePlaceholder.value || '业务编号'}`)
      return
    }
  }
  simulating.value = true
  try {
    const payload: Parameters<typeof simulateBusinessChain>[0] = {
      businessNode: simulateForm.value.businessNode,
      status: Number(simulateForm.value.status ?? 0)
    }
    if (simulateForm.value.dataOrigin !== 'ignore') {
      payload.dataOrigin = simulateForm.value.dataOrigin
      payload.originReferenceCode = simulateForm.value.originReferenceCode.trim()
    }
    simulateResult.value = await simulateBusinessChain(payload)
    ElMessage.success('模拟数据生成成功')
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '模拟数据生成失败'))
  } finally {
    simulating.value = false
  }
}

const onRefreshStockLedger = async () => {
  if (refreshingStockLedger.value) return
  refreshingStockLedger.value = true
  try {
    const result = await refreshStockLedger()
    stockLedgerRefreshResult.value = result
    ElMessage.success(
      `刷新完成：STOCK_OUT ${result.stockOutUpdated} 条，REVERSE ${result.stockOutReverseUpdated} 条，币别兜底 ${result.currencyDefaulted} 条`
    )
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '刷新 stockledger 失败'))
  } finally {
    refreshingStockLedger.value = false
  }
}

const onRecalculateStockAggregates = async () => {
  if (recalculatingStock.value) return
  try {
    await ElMessageBox.confirm(
      '将按未软删 stock_item 全库重算 stock 汇总桶数量，修正删除入库/明细后的汇总滞后。库存中心与出货通知可用量可能下降为真实值。是否继续？',
      '确认重算库存',
      { type: 'warning', confirmButtonText: '继续', cancelButtonText: '取消' }
    )
  } catch {
    return
  }
  recalculatingStock.value = true
  try {
    const result = await recalculateStockAggregates()
    stockRecalculateResult.value = result
    ElMessage.success(
      `重算完成：扫描 ${result.totalBuckets} 个桶，修正 ${result.bucketsUpdated} 个，可用量高估合计 ${result.totalAvailOverstatement}`
    )
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '重算库存失败'))
  } finally {
    recalculatingStock.value = false
  }
}

const onRefreshSellOrderCommentSplit = async () => {
  if (refreshingSellOrderComments.value) return
  try {
    await ElMessageBox.confirm(
      '将扫描 sellorder.comment 非空行；仅 legacy 多行前缀格式会拆入结构化列并回写自由段（含软删订单）。是否继续？',
      '确认拆分销售订单备注',
      { type: 'warning', confirmButtonText: '继续', cancelButtonText: '取消' }
    )
  } catch {
    return
  }
  refreshingSellOrderComments.value = true
  try {
    const result = await refreshSellOrderCommentSplit()
    sellOrderCommentSplitResult.value = result
    ElMessage.success(`已处理 ${result.rowsProcessed} 条（待处理 ${result.totalWithComment} 条）`)
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '拆分 sellorder.comment 失败'))
  } finally {
    refreshingSellOrderComments.value = false
  }
}

const onRefreshSellOrderMainStatus = async () => {
  if (refreshingSellOrderMainStatus.value) return
  try {
    await ElMessageBox.confirm(
      '将遍历全部未软删销售订单，逐单重算明细扩展并同步主状态（与详情页「刷新扩展」一致）。订单较多时可能耗时较长，是否继续？',
      '确认刷新销售订单状态',
      { type: 'warning', confirmButtonText: '继续', cancelButtonText: '取消' }
    )
  } catch {
    return
  }
  refreshingSellOrderMainStatus.value = true
  try {
    const result = await refreshSellOrderMainStatus()
    sellOrderMainStatusResult.value = result
    ElMessage.success(
      `刷新完成：扫描 ${result.totalOrders} 条，主状态变更 ${result.changedOrders} 条，` +
        `跳过终态 ${result.skippedTerminalOrders} 条，失败 ${result.failedCount} 条`
    )
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '刷新销售订单状态失败'))
  } finally {
    refreshingSellOrderMainStatus.value = false
  }
}

const onRefreshSellOrderItemExtendOutboundProfit = async () => {
  if (refreshingSellOrderItemExtendOutboundProfit.value) return
  try {
    await ElMessageBox.confirm(
      '将遍历全部未软删销售明细扩展，逐行重算出库利润（优先 stock_out_item_extend 真实采购价，无快照时回退 PO 加权均价）。' +
        '不刷新订单主状态。明细较多时可能耗时较长，是否继续？',
      '确认刷新出库利润',
      { type: 'warning', confirmButtonText: '继续', cancelButtonText: '取消' }
    )
  } catch {
    return
  }
  refreshingSellOrderItemExtendOutboundProfit.value = true
  try {
    const result = await refreshSellOrderItemExtendOutboundProfit()
    sellOrderItemExtendOutboundProfitResult.value = result
    ElMessage.success(
      `刷新完成：扫描 ${result.totalLines} 条，有出库 ${result.linesWithOutboundQty} 条，` +
        `利润变更 ${result.profitChangedCount} 条，失败 ${result.failedCount} 条`
    )
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '刷新出库利润失败'))
  } finally {
    refreshingSellOrderItemExtendOutboundProfit.value = false
  }
}

const onRefreshPurchaseOrderMainStatus = async () => {
  if (refreshingPurchaseOrderMainStatus.value) return
  try {
    await ElMessageBox.confirm(
      '将遍历全部采购订单，逐单重算明细扩展、同步明细状态与主状态（与详情页「刷新扩展」一致）。订单较多时可能耗时较长，是否继续？',
      '确认刷新采购订单状态',
      { type: 'warning', confirmButtonText: '继续', cancelButtonText: '取消' }
    )
  } catch {
    return
  }
  refreshingPurchaseOrderMainStatus.value = true
  try {
    const result = await refreshPurchaseOrderMainStatus()
    purchaseOrderMainStatusResult.value = result
    ElMessage.success(
      `刷新完成：扫描 ${result.totalOrders} 条，明细 ${result.totalItems} 条，` +
        `明细变更 ${result.changedItems} 条，主状态变更 ${result.changedOrders} 条，` +
        `跳过终态 ${result.skippedTerminalOrders} 条，失败 ${result.failedCount} 条`
    )
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '刷新采购订单状态失败'))
  } finally {
    refreshingPurchaseOrderMainStatus.value = false
  }
}

const onRefreshArrivalNoticeStatuses = async () => {
  if (refreshingArrivalNoticeStatuses.value) return
  try {
    await ElMessageBox.confirm(
      '将遍历全部到货通知，按已过账采购入库 / 质检 / 收货量重算 Status（修正「已入库仍显示已质检」等）。是否继续？',
      '确认刷新到货通知状态',
      { type: 'warning', confirmButtonText: '继续', cancelButtonText: '取消' }
    )
  } catch {
    return
  }
  refreshingArrivalNoticeStatuses.value = true
  try {
    const result = await refreshArrivalNoticeStatuses()
    arrivalNoticeStatusRefreshResult.value = result
    ElMessage.success(
      result.changedCount > 0
        ? `刷新完成：扫描 ${result.totalNotices} 条，变更 ${result.changedCount} 条（已入库 ${result.toStockedInCount} 条）`
        : `扫描 ${result.totalNotices} 条，状态已是最新，无需变更`
    )
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '刷新到货通知状态失败'))
  } finally {
    refreshingArrivalNoticeStatuses.value = false
  }
}

const onRefreshSellOrderItemCustomerPn = async () => {
  if (refreshingSellOrderItemCustomerPn.value) return
  try {
    await ElMessageBox.confirm(
      '将扫描 sellorderitem.comment 非空行；仅当 customer_pn 为空且 comment 以「客户物料型号：」等前缀开头时写入 customer_pn（含软删行）。是否继续？',
      '确认刷新销售明细 customer_pn',
      { type: 'warning', confirmButtonText: '继续', cancelButtonText: '取消' }
    )
  } catch {
    return
  }
  refreshingSellOrderItemCustomerPn.value = true
  try {
    const result = await refreshSellOrderItemCustomerPnFromComment()
    sellOrderItemCustomerPnResult.value = result
    ElMessage.success(`已回填 ${result.rowsFilled} 条（comment 非空 ${result.totalWithComment} 条）`)
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '刷新 sellorderitem.customer_pn 失败'))
  } finally {
    refreshingSellOrderItemCustomerPn.value = false
  }
}

const onRefreshFinancePaymentRemark = async () => {
  if (refreshingFinancePaymentRemark.value) return
  try {
    await ElMessageBox.confirm(
      '将把旧版打包在 financepayment.Remark 中的请款信息拆入结构化列并清空 Remark。已拆过的单据（Remark 已空）不会再次处理。是否继续？',
      '确认刷新付款备注',
      { type: 'warning', confirmButtonText: '继续', cancelButtonText: '取消' }
    )
  } catch {
    return
  }
  refreshingFinancePaymentRemark.value = true
  try {
    const result = await refreshFinancePaymentRemarkFromLegacy()
    financePaymentRemarkLegacyResult.value = result
    ElMessage.success(
      `已写库 ${result.parsedAndApplied} 条付款单，明细行备注 ${result.itemsLineRemarkUpdated} 条，付款银行 Id 纠正 ${result.bankIdsResolvedFromName} 条（无法解析 ${result.skippedMalformed}）`
    )
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '刷新付款备注失败'))
  } finally {
    refreshingFinancePaymentRemark.value = false
  }
}

const onRefreshFinanceReceivables = async () => {
  if (refreshingFinanceReceivables.value) return
  try {
    await ElMessageBox.confirm(
      '将扫描出库完成(4) 的销售出库并补生成应收，同时移除准备出库阶段误生成的历史应收。是否继续？',
      '确认刷新应收款',
      { type: 'warning', confirmButtonText: '继续', cancelButtonText: '取消' }
    )
  } catch {
    return
  }
  refreshingFinanceReceivables.value = true
  try {
    const result = await refreshFinanceReceivablesFromStockOuts()
    financeReceivableRefreshResult.value = result
    ElMessage.success(
      `刷新完成：出库完成 ${result.totalCompletedSalesStockOuts} 条，` +
        `候选 ${result.candidateCount} 条，新建 ${result.createdCount} 条，` +
        `补写出库日期 ${result.stockOutDatesSyncedCount} 条，` +
        `移除过早应收 ${result.prematureReceivablesRemovedCount} 条，` +
        `跳过 ${result.skippedIneligibleCount} 条，失败 ${result.failedCount} 条`
    )
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '刷新应收款失败'))
  } finally {
    refreshingFinanceReceivables.value = false
  }
}

const onRefreshRfqMaterialIntel = async () => {
  if (refreshingRfqMaterialIntel.value) return
  try {
    await ElMessageBox.confirm(
      '将遍历全部未软删 RFQ 需求明细的物料型号，对尚无 AI 物料情报缓存的 PN 依次触发查询（系统补刷）。可能耗时较长并消耗 AI 配额，是否继续？',
      '确认刷 AI 需求物料',
      { type: 'warning', confirmButtonText: '继续', cancelButtonText: '取消' }
    )
  } catch {
    return
  }
  refreshingRfqMaterialIntel.value = true
  try {
    const result = await refreshRfqMaterialIntelCache()
    rfqMaterialIntelRefreshResult.value = result
    ElMessage.success(
      `刷 AI 需求物料完成：去重 ${result.distinctPnCount} 个 PN，已有缓存 ${result.alreadyCachedCount} 个，新触发 ${result.invokedCount} 个，失败 ${result.failedCount} 个`
    )
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '刷 AI 需求物料失败'))
  } finally {
    refreshingRfqMaterialIntel.value = false
  }
}

const onRefreshCustomsAgencyRate = async () => {
  if (refreshingCustomsAgencyRate.value) return
  const brokerId = agencyRateBrokerId.value.trim()
  if (!brokerId) {
    ElMessage.warning('请选择报关公司')
    return
  }
  if (!isValidCustomsAgencyRate(agencyRateInput.value)) {
    ElMessage.warning('代理费率须为 1+纯费率，不能小于 1')
    return
  }
  const broker = agencyRateBrokers.value.find((b) => b.id === brokerId)
  const brokerLabel = broker ? `${broker.brokerCode} ${broker.cname}` : brokerId
  const rateText = Number(agencyRateInput.value).toFixed(6)
  try {
    await ElMessageBox.confirm(
      `将按代理费率 ${rateText} 重算「${brokerLabel}」下系统模式报关单的代理费与 P1，并回写报关入库成本（含已结关锁定单）。不改报关公司资料；手工费率单跳过。是否继续？`,
      '确认刷新报关代理费率',
      { type: 'warning', confirmButtonText: '继续', cancelButtonText: '取消' }
    )
  } catch {
    return
  }
  refreshingCustomsAgencyRate.value = true
  try {
    const result = await refreshCustomsAgencyRate(brokerId, Number(agencyRateInput.value))
    customsAgencyRateRefreshResult.value = result
    ElMessage.success(
      `刷新完成：扫描 ${result.totalDeclarations} 单，刷新 ${result.refreshedDeclarations} 单，` +
        `费用变化 ${result.feesChangedDeclarations} 单，入库层 ${result.stockItemLayersUpdated} 条，` +
        `跳过手工 ${result.skippedManual} 单，失败 ${result.failedCount} 单`
    )
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '刷新报关代理费率失败'))
  } finally {
    refreshingCustomsAgencyRate.value = false
  }
}

</script>

<style lang="scss" scoped>
/* 本页在 AppLayout 浅色主内容区内渲染，使用深色文字与 Element 变量以保证对比度 */
.debug-page {
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 20px;
  color: #303133;
}

.debug-header h1 {
  margin: 0;
  font-size: 20px;
  font-weight: 700;
  color: #303133;
}

.debug-sub {
  margin-top: 6px;
  font-size: 13px;
  color: #606266;
  line-height: 1.6;

  &.muted {
    margin-top: 4px;
  }
}

.debug-link {
  margin-left: 10px;
  color: var(--el-color-primary);
  text-decoration: none;
  font-weight: 600;
  white-space: nowrap;

  &:hover {
    text-decoration: underline;
    color: var(--el-color-primary-light-3);
  }
}

.debug-panel {
  padding: 16px 18px;
  border-radius: 10px;
  border: 1px solid var(--el-border-color-lighter);
  background: var(--el-bg-color);
  box-shadow: var(--el-box-shadow-light);
}

.panel-title {
  margin: 0 0 12px;
  font-size: 15px;
  font-weight: 600;
  color: #303133;
}

.panel-body {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px;
}

.mono {
  font-family: ui-monospace, 'Cascadia Code', 'Consolas', monospace;
}

.simulate-form {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px 16px;
}

.simulate-form__group {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
}

.simulate-form__group--generate {
  margin-left: 8px;
  padding-left: 16px;
  border-left: 1px solid var(--el-border-color-lighter);
}

.simulate-form__inline-label {
  font-size: 13px;
  font-weight: 600;
  color: #606266;
  white-space: nowrap;
}

.simulate-tip {
  margin-top: 8px;
  color: #909399;
  font-size: 12px;
  line-height: 1.55;
}

.simulate-result {
  margin-top: 10px;
  border: 1px solid var(--el-border-color-lighter);
  background: var(--el-fill-color-light);
  border-radius: 8px;
  padding: 10px 12px;
  color: #303133;
  font-size: 13px;
  display: grid;
  gap: 4px;
}

.panel-chain .chain-tip {
  margin: 0 0 14px;
  font-size: 12px;
  color: #909399;
  line-height: 1.55;
}

.chain-toolbar {
  flex-wrap: wrap;
  gap: 10px 12px;
}

.chain-error {
  margin-top: 10px;
  padding: 10px 12px;
  border-radius: 8px;
  border: 1px solid var(--el-color-danger-light-5);
  background: var(--el-color-danger-light-9);
  color: var(--el-color-danger);
  font-size: 13px;
}

.chain-empty {
  margin-top: 12px;
  padding: 12px;
  font-size: 13px;
  color: #909399;
  border: 1px dashed var(--el-border-color);
  border-radius: 8px;
  text-align: center;
}

.chain-table {
  margin-top: 12px;
  width: 100%;
}

.agency-rate-refresh {
  margin-bottom: 8px;
}

.refresh-hint--second {
  margin-top: 8px;
}

</style>
