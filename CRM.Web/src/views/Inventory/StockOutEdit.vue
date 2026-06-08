<template>
  <div class="stockout-edit-page" :class="{ 'pick-only-page': isPickOnlyPage }">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3zM17 14l4 4-4 4M10 17h11" />
            </svg>
          </div>
          <h1 class="page-title">{{ pageTitle }}</h1>
        </div>
      </div>
      <div class="header-right">
        <button class="btn-secondary" @click="goBack">
          {{ isPickOnlyPage ? t('pickCreate.back') : t('stockOutNotifyList.backToNotifyList') }}
        </button>
        <template v-if="!isNotifyDetailPage">
          <button
            v-if="isPickOnlyPage && packingId && form.warehouseId"
            type="button"
            class="btn-primary"
            :disabled="submittingPickingOrder || !pickDraftConfirmed"
            :title="submitPickingOrderBtnTitle"
            @click="submitPickingOrder"
          >
            {{ submittingPickingOrder ? t('pickCreate.submittingPickingOrder') : t('pickCreate.submitPickingOrder') }}
          </button>
          <button
            v-if="!isPickOnlyPage"
            class="btn-picking"
            :disabled="requestAlreadyShipped || hasActivePickingTask"
            :title="generatePickingBtnTitle"
            @click="handleGeneratePicking"
          >
            {{ t('pickCreate.generateTask') }}
          </button>
          <button
            v-if="!isPickOnlyPage"
            class="btn-primary"
            style="margin-left: 8px"
            :disabled="submitting || !canExecuteStockOut"
            :title="executeOutHint"
            @click="handleSubmit"
          >
            {{ submitting ? t('stockOutNotifyList.executing') : t('stockOutNotifyList.executeSubmit') }}
          </button>
        </template>
      </div>
    </div>

    <el-alert
      v-if="!isNotifyDetailPage && !isPickOnlyPage && form.stockOutRequestId"
      class="flow-alert"
      type="info"
      :closable="false"
      show-icon
    >
      <template #title>
        <span class="flow-alert-title">出库流程</span>
      </template>
      <ol class="flow-steps">
        <li :class="{ 'flow-step--done': hasItemsAndWarehouse }">确认仓库与出库明细（可点「从出库通知刷新明细」）</li>
        <li :class="{ 'flow-step--done': pickingTasks.length > 0 }">生成拣货任务（仅建任务壳）</li>
        <li :class="{ 'flow-step--done': pickingLinesSaved }">加载候选并保存拣货明细（按 stockitem），再点「完成拣货」</li>
        <li :class="{ 'flow-step--done': pickingCompleted }">拣货任务状态为已完成</li>
        <li :class="{ 'flow-step--done': requestAlreadyShipped }">执行出库（按拣货明细扣减并关闭出库通知）</li>
      </ol>
      <p v-if="requestAlreadyShipped" class="flow-done-msg">该出库通知已执行出库，请返回列表查看。</p>
      <p v-else-if="!pickingCompleted && pickingTasks.length > 0" class="flow-warn-msg">请先完成拣货任务，再执行出库。</p>
    </el-alert>

    <div v-if="!isPickOnlyPage" class="form-layout">
      <div class="form-card">
        <h3 class="section-title">基础信息</h3>
        <el-descriptions v-if="isNotifyDetailPage" :column="2" border class="notify-detail-desc">
          <el-descriptions-item label="出库通知单号">
            <span class="notify-code-cell">
              <span>{{ notifyRequestCodeDisplay }}</span>
              <el-tooltip
                v-if="notifyDetailIsCustoms && notifyDetailSalesNotifyTooltip"
                :content="notifyDetailSalesNotifyTooltip"
                placement="top"
                :hide-after="0"
              >
                <span class="customs-notify-tag">{{ t('stockOutNotifyList.customsNotifyTag') }}</span>
              </el-tooltip>
            </span>
          </el-descriptions-item>
          <el-descriptions-item :label="t('stockOutNotifyList.columns.status')">{{ notifyDetailStatusLabel }}</el-descriptions-item>
          <el-descriptions-item :label="t('stockOutNotifyList.columns.stockOutType')">
            <StockBizTypeTag biz="out" :type="currentRequest?.stockOutType" />
          </el-descriptions-item>
          <el-descriptions-item :label="t('stockOutNotifyList.columns.customsStatus')">{{ notifyDetailCustomsStatusLabel }}</el-descriptions-item>
          <el-descriptions-item
            v-if="notifyDetailIsCustoms && notifyDetailSalesNotifyId"
            :label="t('stockOutNotifyList.salesNotifyCodeLink')"
          >
            <router-link
              :to="{ name: 'StockOutNotifyDetail', params: { id: notifyDetailSalesNotifyId } }"
              class="cell-link"
            >
              {{ notifyDetailSalesNotifyCode || notifyDetailSalesNotifyId }}
            </router-link>
          </el-descriptions-item>
          <el-descriptions-item :label="t('stockOutNotifyList.columns.materialModel')">{{ currentRequest?.materialModel || '—' }}</el-descriptions-item>
          <el-descriptions-item :label="t('stockOutNotifyList.columns.brand')">{{ currentRequest?.brand || '—' }}</el-descriptions-item>
          <el-descriptions-item :label="t('stockOutNotifyList.columns.outQuantity')">{{ formatQty(currentRequest?.outQuantity) }}</el-descriptions-item>
          <el-descriptions-item :label="t('stockOutNotifyList.columns.regionType')">{{ notifyDetailRegionLabel }}</el-descriptions-item>
          <el-descriptions-item :label="t('stockOutNotifyList.columns.requestDate')">{{ notifyDetailRequestDate }}</el-descriptions-item>
          <el-descriptions-item :label="t('stockOutNotifyList.columns.salesOrderCode')">{{ currentRequest?.salesOrderCode || '—' }}</el-descriptions-item>
          <el-descriptions-item :label="t('stockOutNotifyList.columns.customer')">{{ currentRequest?.customerName || '—' }}</el-descriptions-item>
          <el-descriptions-item :label="t('stockOutNotifyList.columns.salesUserName')">{{ currentRequest?.salesUserName || '—' }}</el-descriptions-item>
          <el-descriptions-item :label="t('stockOutNotifyList.columns.remark')" :span="2">{{ currentRequest?.remark || '—' }}</el-descriptions-item>
          <el-descriptions-item :label="t('stockOutNotifyList.columns.createTime')">{{ notifyDetailCreateTime }}</el-descriptions-item>
          <el-descriptions-item :label="t('stockOutNotifyList.columns.createUser')">
            {{ notifyDetailCreateUser }}
          </el-descriptions-item>
        </el-descriptions>
        <el-form v-else class="basic-info-form" :model="form" label-width="6em">
          <el-form-item label="出库执行单号" required>
            <el-input v-model="form.stockOutCode" placeholder="如：SOUT202603180001" />
          </el-form-item>
          <el-form-item label="仓库名称" required>
            <el-select v-model="form.warehouseId" placeholder="请选择仓库" style="width: 100%">
              <el-option
                v-for="w in warehouses"
                :key="w.id"
                :label="`${w.warehouseName}（${w.warehouseCode}）`"
                :value="w.id"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="出库通知单号">
            <el-input :model-value="notifyRequestCodeDisplay" readonly />
            <div v-if="form.stockOutRequestId" class="form-sub-hint">内部 ID：{{ form.stockOutRequestId }}</div>
          </el-form-item>
          <el-form-item label="操作人">
            <el-input v-model="form.operatorId" placeholder="当前操作人ID（可选）" />
          </el-form-item>
          <el-form-item label="出库日期" required>
            <el-date-picker
              v-model="form.stockOutDate"
              type="datetime"
              format="YYYY-MM-DD HH:mm"
              value-format="YYYY-MM-DDTHH:mm:ss"
              style="width: 100%"
            />
          </el-form-item>
          <el-form-item label="备注">
            <el-input v-model="form.remark" type="textarea" :rows="2" placeholder="备注信息" />
          </el-form-item>
        </el-form>
      </div>

      <template v-if="!isNotifyDetailPage">
      <div class="form-card">
        <div class="section-header">
          <h3 class="section-title">出库明细</h3>
          <button class="btn-secondary btn-sm" @click="loadItemsFromRequest">从出库通知刷新明细</button>
        </div>
        <el-table :data="form.items" class="quantum-table">
          <el-table-column type="index" width="50" />
          <el-table-column label="物料型号" min-width="140" show-overflow-tooltip>
            <template #default="{ row }">
              <span class="stock-out-line-material">{{ (row.materialCode || '').trim() || '—' }}</span>
            </template>
          </el-table-column>
          <el-table-column label="品牌" min-width="160" show-overflow-tooltip>
            <template #default="{ row }">
              <span class="stock-out-line-material">{{ (row.materialName || '').trim() || '—' }}</span>
            </template>
          </el-table-column>
          <el-table-column label="出库数量" width="110" align="right">
            <template #default="{ row }">
              <span class="qty-cell">{{ formatQty(row.quantity) }}</span>
            </template>
          </el-table-column>
          <el-table-column
            label="操作"
            :width="stockOutItemsOpColWidth"
            :min-width="stockOutItemsOpColMinWidth"
            fixed="right"
            align="center"
            class-name="op-col"
            label-class-name="op-col"
          >
            <template #header>
              <div class="list-op-col-header--icon-only">
            <button
              type="button"
              class="op-col-toggle-btn list-op-col-toggle"
              :aria-label="stockOutItemsOpColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
              @click.stop="toggleStockOutItemsOpCol"
            >
              {{ stockOutItemsOpColExpanded ? '>' : '<' }}
            </button>
          </div>
            </template>
            <template #default>
              <div @click.stop @dblclick.stop>
                <div v-if="stockOutItemsOpColExpanded" class="action-btns">
                  <button type="button" class="action-btn" disabled title="明细来自出库通知，不可手工删除">删除</button>
                </div>
                <el-dropdown v-else trigger="click" placement="bottom-end">
                  <div class="op-more-dropdown-trigger">
                    <button type="button" class="op-more-trigger">...</button>
                  </div>
                  <template #dropdown>
                    <el-dropdown-menu>
                      <el-dropdown-item disabled>
                        <span class="op-more-item op-more-item--danger">删除（不可删）</span>
                      </el-dropdown-item>
                    </el-dropdown-menu>
                  </template>
                </el-dropdown>
              </div>
            </template>
          </el-table-column>
        </el-table>
        <div class="table-footer">
          <div class="total">
            合计出库数量：<span>{{ totalQuantity }}</span>
          </div>
        </div>
      </div>
      </template>
    </div>

    <StockOutNotifyDetailTabs v-if="isNotifyDetailPage" :request="currentRequest" />

    <div
      v-if="isPickOnlyPage && packingId"
      class="form-card form-card--pick-warehouse"
    >
      <el-descriptions :column="2" border class="pick-kv-descriptions">
        <el-descriptions-item v-if="pickPage?.packingCode" :label="t('pickCreate.packingCodeLabel')">
          {{ pickPage.packingCode }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('pickCreate.warehouseLabel')">
          <span v-if="pickWarehouseDisplay" class="pick-kv-warehouse-readonly">{{ pickWarehouseDisplay }}</span>
          <span v-else class="picking-empty picking-empty--warn">{{ t('pickCreate.missingPackingWarehouse') }}</span>
        </el-descriptions-item>
      </el-descriptions>
      <p v-if="pickPagePickingCompleted" class="picking-empty picking-empty--done">
        {{ t('pickCreate.pickingCompletedHint') }}
      </p>
      <p v-else-if="!pendingPickingTask && form.warehouseId" class="picking-empty">{{ t('pickCreate.noTaskHint') }}</p>
    </div>

    <div
      v-if="!isNotifyDetailPage && !isPickOnlyPage"
      class="form-card form-card--after-outbound"
      v-show="form.stockOutRequestId"
    >
      <h3 class="section-title">拣货任务</h3>
      <p class="picking-hint">
        每个出库通知仅允许生成<strong>一个</strong>未取消的拣货任务。候选在库明细 = 与本销售行绑定的 stockitem + 符合规则的备货（型号/品牌匹配）；FIFO 仅用于排序与「自动分配」顺序。请在下方「拣货明细」卡片中加载候选、分配数量并保存后，再点「完成拣货」。执行出库时仅按已保存的拣货行扣减。
      </p>
      <p v-if="!pickingTasks.length && !isPickOnlyPage" class="picking-empty">
        暂无拣货任务，请先确认明细与仓库后点击「生成拣货任务」。
      </p>
      <p v-else-if="!pickingTasks.length" class="picking-empty">{{ t('pickCreate.noTask') }}</p>
      <el-table v-else :data="pickingTasks" row-key="id" class="picking-task-table">
        <el-table-column type="expand" width="44">
          <template #default="{ row }">
            <div class="picking-expand-inner">
              <div class="picking-expand-title">拣货明细（备货行已高亮）</div>
              <el-table
                :data="pickingTaskLines(row)"
                size="small"
                border
                class="picking-lines-table"
                :row-class-name="pickingLineRowClassName"
              >
                <el-table-column label="库存明细编号" min-width="140" show-overflow-tooltip>
                  <template #default="{ row: line }">{{ pickingLineStockItemCode(line) }}</template>
                </el-table-column>
                <el-table-column label="入库明细编号" min-width="140" show-overflow-tooltip>
                  <template #default="{ row: line }">{{ pickingLineStockInItemCode(line) }}</template>
                </el-table-column>
                <el-table-column :label="t('inventoryList.columns.stockType')" width="108" align="center" show-overflow-tooltip>
                  <template #default="{ row: line }">{{ pickingLineStockTypeLabel(line) }}</template>
                </el-table-column>
                <el-table-column label="计划数量" width="110" align="right">
                  <template #default="{ row: line }">{{ formatQty(Number(line.planQty)) }}</template>
                </el-table-column>
                <el-table-column label="已拣" width="100" align="right">
                  <template #default="{ row: line }">{{ formatQty(Number(line.pickedQty)) }}</template>
                </el-table-column>
                <el-table-column label="来源" width="120" align="center">
                  <template #default="{ row: line }">
                    <span v-if="isPickingLineStockingSupplement(line)" class="picking-source-stocking">
                      <el-icon class="picking-stock-icon" aria-hidden="true"><Box /></el-icon>
                      <span>备货</span>
                    </span>
                    <span v-else class="picking-source-normal">关联类型</span>
                  </template>
                </el-table-column>
              </el-table>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="taskCode" label="任务号" width="160" />
        <el-table-column :label="t('inventoryList.columns.stockType')" min-width="120" align="center" show-overflow-tooltip>
          <template #default="{ row }">{{ pickingTaskStockTypesDisplay(row) }}</template>
        </el-table-column>
        <el-table-column label="仓库" min-width="160">
          <template #default="{ row }">{{ warehouseLabel(row.warehouseId) }}</template>
        </el-table-column>
        <el-table-column label="计划拣货" min-width="140" align="right" show-overflow-tooltip>
          <template #default="{ row }">{{ formatQty(pickingQty(row, 'plan')) }}</template>
        </el-table-column>
        <el-table-column label="已拣货" min-width="110" align="right" show-overflow-tooltip>
          <template #default="{ row }">{{ formatQty(pickingQty(row, 'picked')) }}</template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="120">
          <template #default="{ row }">{{ pickingStatusText(row.status) }}</template>
        </el-table-column>
        <el-table-column label="创建时间" min-width="168" show-overflow-tooltip>
          <template #default="{ row }">{{ formatTaskTime(row.createTime) }}</template>
        </el-table-column>
        <el-table-column
          label="操作"
          :width="pickingTasksOpColWidth"
          :min-width="pickingTasksOpColMinWidth"
          align="center"
          class-name="op-col"
          label-class-name="op-col"
        >
          <template #header>
            <div class="list-op-col-header--icon-only">
            <button
              type="button"
              class="op-col-toggle-btn list-op-col-toggle"
              :aria-label="pickingTasksOpColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
              @click.stop="togglePickingTasksOpCol"
            >
              {{ pickingTasksOpColExpanded ? '>' : '<' }}
            </button>
          </div>
          </template>
          <template #default="{ row }">
            <div @click.stop @dblclick.stop>
              <div v-if="pickingTasksOpColExpanded" class="action-btns">
                <button
                  v-if="row.status !== 100"
                  type="button"
                  class="action-btn action-btn--warning"
                  @click.stop="completePicking(row.id)"
                >完成拣货</button>
              </div>
              <el-dropdown v-else trigger="click" placement="bottom-end">
                <div class="op-more-dropdown-trigger">
                  <button type="button" class="op-more-trigger">...</button>
                </div>
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item v-if="row.status !== 100" @click.stop="completePicking(row.id)">
                      <span class="op-more-item op-more-item--warning">完成拣货</span>
                    </el-dropdown-item>
                    <el-dropdown-item v-else disabled>
                      <span class="op-more-item">—</span>
                    </el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>
            </div>
          </template>
        </el-table-column>
      </el-table>
    </div>


    <div
      v-if="isPickOnlyPage && packingId"
      class="form-card form-card--after-outbound"
    >
      <h3 class="section-title">{{ t('pickCreate.packingLinesTitle') }}</h3>
      <p v-if="loadingPickPage" class="picking-empty">{{ t('pickCreate.packingLinesLoading') }}</p>
      <p v-else-if="pickPageLoadError" class="picking-empty picking-empty--warn">{{ pickPageLoadError }}</p>
      <p v-else-if="!pickPage?.lines?.length" class="picking-empty">{{ t('pickCreate.packingLinesEmpty') }}</p>
      <el-table
        v-else
        ref="packingLinesTableRef"
        :data="pickPage!.lines"
        row-key="packingItemId"
        border
        size="small"
        highlight-current-row
        class="quantum-table packing-lines-on-pick-table"
        :empty-text="t('pickCreate.packingLinesEmpty')"
        :current-row-key="selectedPackingItemId"
        @row-click="onPackingLineRowClick"
      >
        <el-table-column :label="t('pickCreate.columns.planPick')" width="96" align="right">
          <template #default="{ row }">{{ formatQty(displayPlanQtyForPickLine(row)) }}</template>
        </el-table-column>
        <el-table-column :label="t('pickCreate.columns.picked')" width="88" align="right">
          <template #default="{ row }">{{ formatQty(displayPickedQtyForPickLine(row)) }}</template>
        </el-table-column>
        <el-table-column :label="t('pickCreate.columns.lineStatus')" width="100" align="center">
          <template #default="{ row }">
            <span
              class="pick-line-status"
              :class="`pick-line-status--${displayPickLineStatusForPickLine(row)}`"
            >
              {{ pickLineStatusLabel(displayPickLineStatusForPickLine(row)) }}
            </span>
          </template>
        </el-table-column>
        <el-table-column :label="t('packingDetail.itemCode')" prop="itemCode" min-width="140" show-overflow-tooltip>
          <template #default="{ row }">{{ row.itemCode || '—' }}</template>
        </el-table-column>
        <el-table-column :label="t('packingItemList.columns.pn')" prop="pn" min-width="140" show-overflow-tooltip />
        <el-table-column :label="t('packingItemList.columns.brand')" prop="brand" min-width="120" show-overflow-tooltip />
        <el-table-column :label="t('packingItemList.columns.qty')" prop="qty" width="88" align="right">
          <template #default="{ row }">{{ formatQty(row.qty) }}</template>
        </el-table-column>
        <el-table-column :label="t('packingDetail.unit')" prop="unit" width="72" />
        <el-table-column :label="t('packingItemList.columns.sellOrderCode')" min-width="140" show-overflow-tooltip>
          <template #default="{ row }">{{ row.sellOrderCode || '—' }}</template>
        </el-table-column>
        <el-table-column :label="t('packingItemList.columns.sellOrderItemCode')" min-width="140" show-overflow-tooltip>
          <template #default="{ row }">{{ row.sellOrderItemCode || '—' }}</template>
        </el-table-column>
        <el-table-column :label="t('packingDetail.comment')" prop="comment" min-width="120" show-overflow-tooltip>
          <template #default="{ row }">{{ row.comment || '—' }}</template>
        </el-table-column>
      </el-table>
    </div>


    <div
      v-if="!isNotifyDetailPage && (isPickOnlyPage ? packingId && form.warehouseId : pendingPickingTask && form.stockOutRequestId)"
      class="form-card"
    >
      <h3 class="section-title">{{ isPickOnlyPage ? t('pickCreate.pickingDetailsTitle') : '拣货明细（按在库 stockitem）' }}</h3>
      <p v-if="!isPickOnlyPage && pendingPickingTask" class="picking-hint">
        任务号：<strong>{{ pendingPickingTask.taskCode }}</strong>。合计须等于出库通知数量（{{ notifyTargetQty }}）。
        保存后方可「完成拣货」。
      </p>
      <div class="picking-draft-toolbar">
        <div class="picking-draft-toolbar__left">
          <button
            v-if="!isPickOnlyPage"
            type="button"
            class="btn-secondary btn-sm"
            :disabled="loadingCandidates"
            @click="loadPickingCandidates()"
          >
            {{ loadingCandidates ? '加载中…' : '加载拣货候选' }}
          </button>
          <button
            v-else
            type="button"
            class="btn-secondary btn-sm"
            :disabled="loadingCandidates || !selectedPackingItemId || !form.warehouseId"
            @click="ensurePickCandidatesLoaded(true)"
          >
            {{ loadingCandidates ? '加载中…' : t('pickCreate.refreshCandidates') }}
          </button>
          <button
            type="button"
            class="btn-secondary btn-sm"
            :disabled="!pickingCandidates.length"
            @click="applyFifoToPickDraft"
          >
            按 FIFO 自动分配
          </button>
        </div>
        <el-radio-group
          v-model="pickingSourceFilter"
          class="picking-source-filter"
          size="small"
          aria-label="按来源筛选拣货明细"
        >
          <el-radio-button label="all">全部库存</el-radio-button>
          <el-radio-button label="stocking">备货库存</el-radio-button>
          <el-radio-button label="customer">客单库存</el-radio-button>
        </el-radio-group>
        <div class="picking-draft-toolbar__right">
          <span class="picking-draft-sum">
            <template v-if="isPickOnlyPage">
              {{ t('pickCreate.currentLineAllocated', { current: allocatedPickTotal, target: selectedLineTargetQty }) }}
              · {{ t('pickCreate.wholeSheetAllocated', { current: allocatedPickTotalAllLines, target: pickPageTotalQty }) }}
            </template>
            <template v-else>
              已分配：<strong>{{ allocatedPickTotal }}</strong> / 目标：<strong>{{ notifyTargetQty }}</strong>
            </template>
            <span v-if="pickDraftConfirmed" class="picking-draft-confirmed-tag">{{ t('pickCreate.qtyConfirmed') }}</span>
          </span>
          <button
            type="button"
            class="btn-primary btn-sm picking-draft-save-btn"
            :class="{ 'picking-draft-save-btn--confirmed': pickDraftConfirmed && isPickOnlyPage }"
            :disabled="(isPickOnlyPage ? !form.warehouseId : !pendingPickingTask) || submittingPickingOrder"
            @click="isPickOnlyPage ? confirmPickQuantities() : savePickingDraftToDb()"
          >
            {{
              submittingPickingOrder && !isPickOnlyPage
                ? t('pickCreate.submittingPickingOrder')
                : isPickOnlyPage
                  ? t('pickCreate.confirmQty')
                  : t('pickCreate.savePickingLines')
            }}
          </button>
        </div>
      </div>
      <el-table
        v-if="isPickOnlyPage ? Boolean(selectedPackingItemId) : pickingCandidates.length > 0"
        v-loading="loadingCandidates"
        :data="filteredPickingCandidates"
        class="quantum-table picking-candidates-table"
        max-height="380"
        :empty-text="pickingCandidatesTableEmptyText"
      >
        <el-table-column label="入库日期" width="110" align="center" show-overflow-tooltip>
          <template #default="{ row }">{{ pickingCandidateStockInDate(row) }}</template>
        </el-table-column>
        <el-table-column label="入库明细编号" min-width="140" show-overflow-tooltip>
          <template #default="{ row }">{{ pickingCandidateStockInItemCode(row) }}</template>
        </el-table-column>
        <el-table-column label="库存明细编号" min-width="140" show-overflow-tooltip>
          <template #default="{ row }">{{ pickingCandidateStockItemCode(row) }}</template>
        </el-table-column>
        <el-table-column
          :label="t('common.freightForwarderOrderNo')"
          min-width="140"
          show-overflow-tooltip
        >
          <template #default="{ row }">{{ row.freightForwarderOrderNo?.trim() || '—' }}</template>
        </el-table-column>
        <el-table-column label="型号" min-width="100" show-overflow-tooltip>
          <template #default="{ row }">{{ row.purchasePn || '—' }}</template>
        </el-table-column>
        <el-table-column label="品牌" min-width="88" show-overflow-tooltip>
          <template #default="{ row }">{{ row.purchaseBrand || '—' }}</template>
        </el-table-column>
        <el-table-column
          :label="t('inventoryList.columns.stockType')"
          min-width="132"
          align="center"
          show-overflow-tooltip
        >
          <template #default="{ row }">{{ inventoryStockTypeLabel(row.stockType) }}</template>
        </el-table-column>
        <el-table-column label="来源" min-width="108" align="center" show-overflow-tooltip>
          <template #default="{ row }">
            <span v-if="row.isStockingCandidate" class="picking-source-stocking">备货</span>
            <span v-else class="picking-source-normal">客单</span>
          </template>
        </el-table-column>
        <el-table-column label="可用" min-width="120" align="right" show-overflow-tooltip>
          <template #default="{ row }">{{ formatQty(row.availableQty) }}</template>
        </el-table-column>
        <el-table-column label="本次拣货" width="130" align="center">
          <template #default="{ row }">
            <el-input-number
              :model-value="pickQty(row)"
              :min="0"
              :max="row.availableQty"
              :step="1"
              :precision="0"
              size="small"
              controls-position="right"
              style="width: 110px"
              @update:model-value="(v: number | undefined | null) => setPickQty(row, v)"
            />
          </template>
        </el-table-column>
      </el-table>
      <p v-if="!isPickOnlyPage && !pickingCandidates.length" class="picking-empty subtle">
        请点击「加载拣货候选」获取本仓库可拣在库明细。
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, reactive, ref, watch } from 'vue'
import type { TableInstance } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Box } from '@element-plus/icons-vue'
import { stockOutApi, type StockOutRequestDto } from '@/api/stockOut'
import {
  inventoryCenterApi,
  type PickPageByPacking,
  type PickPagePackingLine,
  type PickingStockItemCandidate,
  type PickingTask,
  type PickingTaskLine,
  type SavePickingTaskItemLine,
  type WarehouseInfo
} from '@/api/inventoryCenter'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime, formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { normalizeRegionType, REGION_TYPE_OVERSEAS } from '@/constants/regionType'
import { STOCK_OUT_REQUEST_STATUS } from '@/constants/stockOutRequestStatus'
import { STOCK_OUT_NOTIFY_CUSTOMS_STATUS } from '@/constants/stockOutNotifyCustomsStatus'
import { StockOutTypeCode } from '@/constants/stockOutType'
import { formatDate as formatDateTimeZh } from '@/utils/date'
import StockOutNotifyDetailTabs from '@/components/Inventory/StockOutNotifyDetailTabs.vue'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import { packingApi } from '@/api/packing'
import {
  PACKING_STOCK_OUT_QUEUE_KEY,
  popNextPackingStockOutQueueEntry
} from '@/utils/packingStockOutQueue'

type ExecuteItem = {
  lineNo: number
  materialCode: string
  materialName: string
  quantity: number
}

type ExecuteForm = {
  stockOutRequestId: string
  stockOutCode: string
  warehouseId: string
  operatorId?: string
  stockOutDate: string
  remark?: string
  items: ExecuteItem[]
}

const router = useRouter()
const route = useRoute()
const { t, locale } = useI18n()
const submitting = ref(false)
const pickingTasks = ref<PickingTask[]>([])
const linkedPackingId = ref('')
const warehouses = ref<WarehouseInfo[]>([])
const currentRequest = ref<StockOutRequestDto | null>(null)
const pickingCandidates = ref<PickingStockItemCandidate[]>([])
const pickingCandidatesLoaded = ref(false)
const lastPickCandidatesKey = ref('')
const packingLinesTableRef = ref<TableInstance>()
/** 拣货明细按「来源」筛选：与表格「备货 / 客单」列一致（isStockingCandidate） */
type PickingSourceFilterKey = 'all' | 'stocking' | 'customer'
const pickingSourceFilter = ref<PickingSourceFilterKey>('all')
const pickDraft = reactive<Record<string, number>>({})
/** 用户已在本页确认拣货数量（仅内存，未写库） */
const pickDraftConfirmed = ref(false)
const loadingCandidates = ref(false)
const submittingPickingOrder = ref(false)
const completingPicking = ref(false)
const pickPage = ref<PickPageByPacking | null>(null)
const loadingPickPage = ref(false)
const pickPageLoadError = ref('')
const selectedPackingItemId = ref('')
/** 按装箱明细行分桶的拣货草稿：packingItemId -> stockItemId -> qty */
const pickDraftByLine = reactive<Record<string, Record<string, number>>>({})
const getYYMMDD = (d: Date) => {
  const yy = String(d.getFullYear()).slice(-2)
  const mm = String(d.getMonth() + 1).padStart(2, '0')
  const dd = String(d.getDate()).padStart(2, '0')
  return `${yy}${mm}${dd}`
}
const random4 = () => String(Math.floor(Math.random() * 10000)).padStart(4, '0')

function resolveStockOutRequestId(): string {
  const fromParam = route.params.id
  if (typeof fromParam === 'string' && fromParam.trim()) return fromParam.trim()
  return String(route.query.requestId || '').trim()
}

function resolvePackingId(): string {
  return String(route.query.packingId || '').trim()
}

const packingId = computed(() => resolvePackingId())

const isNotifyDetailPage = computed(() => route.name === 'StockOutNotifyDetail')
const isPickOnlyPage = computed(() => route.name === 'PickCreate')

const pageTitle = computed(() => {
  if (isPickOnlyPage.value) return t('pickCreate.title')
  if (isNotifyDetailPage.value) return t('stockOutNotifyList.detailTitle')
  return t('stockOutNotifyList.executeTitle')
})

const notifyDetailStatusLabel = computed(() => {
  const s = Number(currentRequest.value?.status)
  if (s === STOCK_OUT_REQUEST_STATUS.PendingCustoms) return t('stockOutNotifyList.status.pendingCustoms')
  if (s === STOCK_OUT_REQUEST_STATUS.PendingPacking) return t('stockOutNotifyList.status.pendingPacking')
  if (s === STOCK_OUT_REQUEST_STATUS.Packed) return t('stockOutNotifyList.status.packed')
  if (s === STOCK_OUT_REQUEST_STATUS.StockedOut) return t('stockOutNotifyList.status.stockedOut')
  if (s === STOCK_OUT_REQUEST_STATUS.Cancelled) return t('stockOutNotifyList.status.cancelled')
  return t('stockOutNotifyList.status.unknown')
})

const notifyDetailIsCustoms = computed(
  () => Number(currentRequest.value?.stockOutType ?? StockOutTypeCode.Sales) === StockOutTypeCode.Customs
)

const notifyDetailCustomsStatusLabel = computed(() => {
  const n = Number(currentRequest.value?.customsStatus ?? 0)
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.NotRequired) return t('stockOutNotifyList.customsStatus.notRequired')
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.PendingCustoms) return t('stockOutNotifyList.customsStatus.pendingCustoms')
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.InCustoms) return t('stockOutNotifyList.customsStatus.inCustoms')
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.Completed) return t('stockOutNotifyList.customsStatus.completed')
  return '—'
})

const notifyDetailSalesNotifyId = computed(() => String(currentRequest.value?.salesStockOutNotifyId ?? '').trim())

const notifyDetailSalesNotifyCode = computed(() => String(currentRequest.value?.salesStockOutNotifyCode ?? '').trim())

const notifyDetailSalesNotifyTooltip = computed(() => {
  const code = notifyDetailSalesNotifyCode.value
  if (!code) return ''
  return t('stockOutNotifyList.salesNotifyCodeTooltip', { code })
})

const notifyDetailRegionLabel = computed(() => {
  const r = currentRequest.value as unknown as Record<string, unknown> | null
  if (!r) return '—'
  const n = normalizeRegionType(r.regionType ?? r.RegionType)
  return n === REGION_TYPE_OVERSEAS ? t('inventoryList.warehouse.regionOverseas') : t('inventoryList.warehouse.regionDomestic')
})

const notifyDetailRequestDate = computed(() => {
  const v = currentRequest.value?.requestDate
  if (v == null || v === '') return '—'
  return formatDateTimeZh(v, 'YYYY-MM-DD HH:mm')
})

const notifyDetailCreateTime = computed(() => {
  const v = currentRequest.value?.createTime
  if (v == null || v === '') return '—'
  return formatDateTimeZh(v, 'YYYY-MM-DD HH:mm')
})

const notifyDetailCreateUser = computed(() => {
  const r = currentRequest.value as StockOutRequestDto & { createUserName?: string }
  return r?.createUserName || r?.requestUserName || '—'
})

const form = reactive<ExecuteForm>({
  stockOutRequestId: resolveStockOutRequestId(),
  stockOutCode: `SOUT${getYYMMDD(new Date())}${random4()}`,
  warehouseId: '',
  operatorId: '',
  stockOutDate: new Date().toISOString(),
  remark: '',
  items: []
})

/** 《列表操作列规范》 */
const STOCK_OUT_OP_COL_COLLAPSED = 43
const STOCK_OUT_OP_COL_EXPANDED = 173
const STOCK_OUT_OP_COL_EXPANDED_MIN = 160
const stockOutItemsOpColExpanded = ref(false)
const stockOutItemsOpColWidth = computed(() =>
  stockOutItemsOpColExpanded.value ? STOCK_OUT_OP_COL_EXPANDED : STOCK_OUT_OP_COL_COLLAPSED
)
const stockOutItemsOpColMinWidth = computed(() =>
  stockOutItemsOpColExpanded.value ? STOCK_OUT_OP_COL_EXPANDED_MIN : STOCK_OUT_OP_COL_COLLAPSED
)
function toggleStockOutItemsOpCol() {
  stockOutItemsOpColExpanded.value = !stockOutItemsOpColExpanded.value
}
const pickingTasksOpColExpanded = ref(false)
const pickingTasksOpColWidth = computed(() =>
  pickingTasksOpColExpanded.value ? STOCK_OUT_OP_COL_EXPANDED : STOCK_OUT_OP_COL_COLLAPSED
)
const pickingTasksOpColMinWidth = computed(() =>
  pickingTasksOpColExpanded.value ? STOCK_OUT_OP_COL_EXPANDED_MIN : STOCK_OUT_OP_COL_COLLAPSED
)
function togglePickingTasksOpCol() {
  pickingTasksOpColExpanded.value = !pickingTasksOpColExpanded.value
}

const totalQuantity = computed(() => form.items.reduce((sum, x) => sum + (x.quantity || 0), 0))
const pickingStatusText = (s: number) => ({ 1: '待拣货', 2: '拣货中', 100: '已完成', [-1]: '已取消' }[s] || '未知')

/** 数字展示（避免 el-input-number 只读在表格内不显示） */
const formatQty = (n: number | undefined | null) => {
  if (n == null || (typeof n === 'number' && Number.isNaN(n))) return '—'
  const v = Number(n)
  if (Number.isNaN(v)) return '—'
  return Number.isInteger(v) ? `${v}` : `${+v.toFixed(4)}`.replace(/\.?0+$/, '')
}

const warehouseLabel = (warehouseId: string) => {
  const w = warehouses.value.find((x) => x.id === warehouseId)
  return w ? `${w.warehouseName}（${w.warehouseCode}）` : warehouseId
}

const pickWarehouseDisplay = computed(() => {
  if (!isPickOnlyPage.value) return ''
  const fromPage = pickPage.value?.warehouseDisplay?.trim()
  if (fromPage) return fromPage
  const whId = form.warehouseId?.trim()
  if (!whId) return ''
  return warehouseLabel(whId)
})

/** 出库通知业务单号（SON），无则退回显式内部 ID */
const notifyRequestCodeDisplay = computed(() => {
  const code = currentRequest.value?.requestCode?.trim()
  if (code) return code
  return form.stockOutRequestId?.trim() || '—'
})

/** 读取拣货汇总数量（兼容 camelCase / PascalCase） */
function pickingTaskLines(row: PickingTask): PickingTaskLine[] {
  const r = row as unknown as Record<string, unknown>
  const raw = row.items ?? r.Items
  return Array.isArray(raw) ? (raw as PickingTaskLine[]) : []
}

function isPickingLineStockingSupplement(line: PickingTaskLine) {
  const x = line as unknown as Record<string, unknown>
  return Boolean(line.isStockingSupplement ?? x.IsStockingSupplement)
}

function pickingLineRowClassName({ row }: { row: PickingTaskLine }) {
  return isPickingLineStockingSupplement(row) ? 'picking-line-row--stocking' : ''
}

function inventoryStockTypeLabel(code: number): string {
  const m: Record<number, string> = {
    1: t('inventoryList.stockTypes.customer'),
    2: t('inventoryList.stockTypes.stocking'),
    3: t('inventoryList.stockTypes.sample')
  }
  return m[code] ?? t('inventoryList.stockTypes.unknown')
}

function pickingLineStockTypeLabel(line: PickingTaskLine): string {
  const x = line as unknown as Record<string, unknown>
  const n = line.stockType ?? x.StockType
  if (n == null || n === '') return t('inventoryList.stockTypes.unknown')
  const num = Number(n)
  return Number.isFinite(num) ? inventoryStockTypeLabel(num) : t('inventoryList.stockTypes.unknown')
}

function pickingTaskStockTypesDisplay(row: PickingTask): string {
  const r = row as unknown as Record<string, unknown>
  const raw = row.distinctStockTypes ?? r.DistinctStockTypes
  if (!Array.isArray(raw) || raw.length === 0) return t('inventoryList.stockTypes.unknown')
  const sep = locale.value === 'zh-CN' ? '、' : ', '
  return (raw as number[])
    .map((c) => Number(c))
    .filter((c) => Number.isFinite(c))
    .map((c) => inventoryStockTypeLabel(c))
    .join(sep)
}

const pickingQty = (row: PickingTask, kind: 'plan' | 'picked') => {
  const r = row as unknown as Record<string, unknown>
  const v =
    kind === 'plan'
      ? (row.planQtyTotal ?? r.PlanQtyTotal)
      : (row.pickedQtyTotal ?? r.PickedQtyTotal)
  if (v == null || v === '') return null
  const n = Number(v)
  return Number.isFinite(n) ? n : null
}

/** 创建时间：按系统展示时区（默认 Asia/Shanghai）格式化，避免原始 ISO 串误解 */
const formatTaskTime = (v?: string) => {
  const r = v as string | undefined
  if (!r) return '--'
  return formatDisplayDateTime(r)
}

const requestAlreadyShipped = computed(
  () => Number(currentRequest.value?.status) === STOCK_OUT_REQUEST_STATUS.StockedOut
)

const notifyTargetQty = computed(() => {
  const r = currentRequest.value as unknown as Record<string, unknown> | null
  const q = r?.outQuantity ?? r?.OutQuantity
  if (typeof q === 'number' && Number.isFinite(q) && q > 0) return Math.round(q)
  return Math.round(totalQuantity.value)
})

function pickingTaskPackingId(t: PickingTask): string {
  return String(t.packingId ?? (t as unknown as Record<string, string>).PackingId ?? '').trim()
}

function pickingTaskMatchesCurrentRequest(t: PickingTask): boolean {
  const pid = linkedPackingId.value.trim()
  return Boolean(pid) && pickingTaskPackingId(t) === pid
}

const pendingPickingTask = computed(() => {
  if (isPickOnlyPage.value) {
    const t = pickPage.value?.pickingTask
    if (!t || t.status === 100 || t.status === -1) return null
    return t
  }
  return (
    pickingTasks.value.find((t) => pickingTaskMatchesCurrentRequest(t) && t.status !== 100 && t.status !== -1) ??
    null
  )
})

const selectedPackingLine = computed(() => {
  const id = selectedPackingItemId.value?.trim()
  if (!id || !pickPage.value?.lines?.length) return null
  return pickPage.value.lines.find((l) => l.packingItemId === id) ?? null
})

const selectedLineTargetQty = computed(() => selectedPackingLine.value?.qty ?? 0)

function pickLineStatusLabel(status: string): string {
  const m: Record<string, string> = {
    pending: t('pickCreate.lineStatus.pending'),
    partial: t('pickCreate.lineStatus.partial'),
    allocated: t('pickCreate.lineStatus.allocated'),
    done: t('pickCreate.lineStatus.done'),
    over: t('pickCreate.lineStatus.over')
  }
  return m[status] ?? status
}

/** 装箱行「本次拣货」草稿合计（随拣货明细输入实时变化） */
function lineDraftPickSum(packingItemId: string): number {
  const draft = pickDraftByLine[packingItemId] ?? {}
  return Object.values(draft).reduce((a, n) => a + (Number(n) || 0), 0)
}

/** 计划拣货：本行须拣数量（与装箱明细「数量」一致，不随本次分配变化） */
function displayPlanQtyForPickLine(row: PickPagePackingLine): number {
  return Number(row.qty) || 0
}

/** 已拣货：编辑中随本次拣货草稿变化；完成拣货后以库中 PickedQty 为准 */
function displayPickedQtyForPickLine(row: PickPagePackingLine): number {
  const draft = lineDraftPickSum(row.packingItemId)
  const savedPlan = Number(row.planQtyTotal) || 0
  const savedPicked = Number(row.pickedQtyTotal) || 0
  if (savedPicked > 0 && savedPlan > 0 && savedPicked >= savedPlan) return savedPicked
  return draft > 0 ? draft : savedPicked
}

function displayPickLineStatusForPickLine(row: PickPagePackingLine): string {
  const target = Number(row.qty) || 0
  const draft = lineDraftPickSum(row.packingItemId)
  const savedPlan = Number(row.planQtyTotal) || 0
  const savedPicked = Number(row.pickedQtyTotal) || 0
  const pickedEffective =
    savedPicked > 0 && savedPlan > 0 && savedPicked >= savedPlan
      ? savedPicked
      : draft > 0
        ? draft
        : savedPicked
  if (pickedEffective <= 0) return 'pending'
  if (pickedEffective > target) return 'over'
  if (pickedEffective < target) return 'partial'
  if (savedPicked >= savedPlan && savedPlan >= target && savedPlan > 0) return 'done'
  return 'allocated'
}

function activePickDraft(): Record<string, number> {
  if (!isPickOnlyPage.value) return pickDraft
  const lid = selectedPackingItemId.value?.trim()
  if (!lid) return {}
  if (!pickDraftByLine[lid]) pickDraftByLine[lid] = {}
  return pickDraftByLine[lid]
}

function clearActivePickDraft() {
  if (!isPickOnlyPage.value) {
    clearPickDraft()
    return
  }
  const lid = selectedPackingItemId.value?.trim()
  if (!lid) return
  pickDraftByLine[lid] = {}
  pickDraftConfirmed.value = false
}

const allocatedPickTotal = computed(() => {
  if (!isPickOnlyPage.value) {
    return pickingCandidates.value.reduce((s, c) => s + (pickDraft[c.stockItemId] ?? 0), 0)
  }
  const draft = activePickDraft()
  return pickingCandidates.value.reduce((s, c) => s + (draft[c.stockItemId] ?? 0), 0)
})

const allocatedPickTotalAllLines = computed(() => {
  if (!pickPage.value?.lines?.length) return 0
  let sum = 0
  for (const line of pickPage.value.lines) {
    const draft = pickDraftByLine[line.packingItemId] ?? {}
    sum += Object.values(draft).reduce((a, n) => a + (Number(n) || 0), 0)
  }
  return sum
})

const pickPageTotalQty = computed(() =>
  (pickPage.value?.lines ?? []).reduce((s, l) => s + (Number(l.qty) || 0), 0)
)

const pickPagePickingCompleted = computed(() => {
  if (!isPickOnlyPage.value) return false
  const st = pickPage.value?.pickingTask?.status
  return st === 100
})

const filteredPickingCandidates = computed((): PickingStockItemCandidate[] => {
  const list = pickingCandidates.value
  if (pickingSourceFilter.value === 'stocking')
    return list.filter((c) => Boolean(c.isStockingCandidate))
  if (pickingSourceFilter.value === 'customer')
    return list.filter((c) => !c.isStockingCandidate)
  return list
})

const pickingCandidatesTableEmptyText = computed(() => {
  if (isPickOnlyPage.value) {
    if (!pickingCandidatesLoaded.value) return ' '
    if (pickingCandidates.value.length === 0) return t('pickCreate.noAvailableStock')
    return t('pickCreate.pickingCandidatesFilterEmpty')
  }
  return t('pickCreate.pickingCandidatesFilterEmpty')
})

const pickingLinesSaved = computed(() => {
  if (!linkedPackingId.value.trim()) return false
  return pickingTasks.value.some((t) => {
    if (!pickingTaskMatchesCurrentRequest(t)) return false
    const n = pickingQty(t, 'plan')
    return n != null && n > 0
  })
})

const pickingCompleted = computed(() => pickingTasks.value.some((t) => t.status === 100))

/** 是否存在未取消的拣货任务（与后端「禁止重复生成」一致） */
const hasActivePickingTask = computed(() => {
  if (isPickOnlyPage.value) {
    const t = pickPage.value?.pickingTask
    return Boolean(t && t.status !== -1)
  }
  return pickingTasks.value.some((t) => t.status !== -1)
})

const generatePickingBtnTitle = computed(() => {
  if (requestAlreadyShipped.value) return '该出库通知已执行出库'
  if (hasActivePickingTask.value) return '该出库通知已有拣货任务，请勿重复生成'
  return ''
})

const hasItemsAndWarehouse = computed(
  () =>
    !!form.warehouseId &&
    form.items.length > 0 &&
    form.items.every((x) => x.materialCode && Number(x.quantity) > 0)
)

const canExecuteStockOut = computed(
  () =>
    !requestAlreadyShipped.value &&
    pickingCompleted.value &&
    !!form.stockOutRequestId &&
    !!form.stockOutCode?.trim() &&
    !!form.warehouseId &&
    form.items.length > 0
)

const executeOutHint = computed(() => {
  if (requestAlreadyShipped.value) return '该出库通知已出库'
  if (!pickingCompleted.value) return '请先生成拣货单并完成拣货任务后再执行出库'
  return '将按已保存的拣货明细扣减在库并标记出库通知为已出库'
})

const submitPickingOrderBtnTitle = computed(() => {
  if (!pickDraftConfirmed.value) return t('pickCreate.confirmQtyFirst')
  if (pickingLinesSaved.value) return t('pickCreate.resubmitPickingOrderHint')
  return ''
})

function pickingLineStockItemCode(line: PickingTaskLine) {
  const x = line as unknown as Record<string, unknown>
  const v = line.stockItemCode ?? x.StockItemCode
  const s = String(v ?? '').trim()
  return s || '—'
}

function pickingLineStockInItemCode(line: PickingTaskLine) {
  const x = line as unknown as Record<string, unknown>
  const v = line.stockInItemCode ?? x.StockInItemCode
  const s = String(v ?? '').trim()
  return s || '—'
}

function pickingCandidateStockItemCode(row: PickingStockItemCandidate) {
  const r = row as unknown as Record<string, unknown>
  const v = row.stockItemCode ?? r.StockItemCode
  const s = String(v ?? '').trim()
  return s || '—'
}

function pickingCandidateStockInDate(row: PickingStockItemCandidate) {
  const r = row as unknown as Record<string, unknown>
  const v = (row.stockInDate ?? r.StockInDate) as string | undefined | null
  const parts = formatDisplayDateTime2DigitYearParts(v)
  return parts?.date ?? '—'
}

function pickingCandidateStockInItemCode(row: PickingStockItemCandidate) {
  const r = row as unknown as Record<string, unknown>
  const v = row.stockInItemCode ?? r.StockInItemCode
  const s = String(v ?? '').trim()
  return s || '—'
}

function pickQty(c: PickingStockItemCandidate) {
  return activePickDraft()[c.stockItemId] ?? 0
}

function setPickQty(c: PickingStockItemCandidate, v: number | undefined | null) {
  const k = c.stockItemId
  const raw = typeof v === 'number' && Number.isFinite(v) ? Math.floor(v) : 0
  const n = Math.max(0, Math.min(raw, Math.max(0, Math.floor(Number(c.availableQty)))))
  const draft = activePickDraft()
  if (n <= 0) delete draft[k]
  else draft[k] = n
  pickDraftConfirmed.value = false
}

function clearPickDraft() {
  for (const k of Object.keys(pickDraft)) delete pickDraft[k]
  pickDraftConfirmed.value = false
}

function buildPickLinesFromDraft() {
  return pickingCandidates.value
    .map((c) => ({
      packingItemId: selectedPackingItemId.value || undefined,
      stockItemId: c.stockItemId,
      stockId: c.stockAggregateId,
      qty: pickQty(c)
    }))
    .filter((l) => l.qty > 0)
}

function buildAllPickLinesFromDraft(): SavePickingTaskItemLine[] {
  const lines: SavePickingTaskItemLine[] = []
  if (!pickPage.value?.lines?.length) return lines
  for (const pl of pickPage.value.lines) {
    const draft = pickDraftByLine[pl.packingItemId] ?? {}
    for (const [stockItemId, qty] of Object.entries(draft)) {
      const n = Math.floor(Number(qty) || 0)
      if (n <= 0) continue
      const cand = pickingCandidatesCache.value[pl.packingItemId]?.find((c) => c.stockItemId === stockItemId)
      const saved = pl.pickingItems?.find((p) => String(p.stockItemId ?? '').trim() === stockItemId)
      const stockId = cand?.stockAggregateId ?? String(saved?.stockId ?? '').trim()
      if (!stockId) continue
      lines.push({
        packingItemId: pl.packingItemId,
        stockItemId,
        stockId,
        qty: n
      })
    }
  }
  return lines
}

/** 装箱行 -> 候选缓存（保存整单时合并各行的 stockitem） */
const pickingCandidatesCache = ref<Record<string, PickingStockItemCandidate[]>>({})

function validatePickDraftTotal(): boolean {
  if (isPickOnlyPage.value) {
    return validateAllPackingLinesDraft()
  }
  const lines = buildPickLinesFromDraft()
  const sum = lines.reduce((a, l) => a + l.qty, 0)
  if (sum !== notifyTargetQty.value) {
    ElMessage.error(`拣货数量合计须等于出库通知数量（${notifyTargetQty.value}），当前为 ${sum}`)
    return false
  }
  if (lines.length === 0) {
    ElMessage.warning(t('pickCreate.allocateQtyFirst'))
    return false
  }
  return true
}

function validateAllPackingLinesDraft(): boolean {
  if (!pickPage.value?.lines?.length) {
    ElMessage.warning(t('pickCreate.packingLinesEmpty'))
    return false
  }
  for (const pl of pickPage.value.lines) {
    const draft = pickDraftByLine[pl.packingItemId] ?? {}
    const sum = Object.values(draft).reduce((a, n) => a + (Number(n) || 0), 0)
    if (sum !== pl.qty) {
      ElMessage.error(
        t('pickCreate.lineQtyMismatch', {
          code: pl.itemCode || pl.packingItemId,
          target: pl.qty,
          actual: sum
        })
      )
      return false
    }
  }
  for (const pl of pickPage.value.lines) {
    const draft = pickDraftByLine[pl.packingItemId] ?? {}
    for (const [stockItemId, qty] of Object.entries(draft)) {
      if (Math.floor(Number(qty) || 0) <= 0) continue
      const cand = pickingCandidatesCache.value[pl.packingItemId]?.find((c) => c.stockItemId === stockItemId)
      const saved = pl.pickingItems?.find((p) => String(p.stockItemId ?? '').trim() === stockItemId)
      const stockId = cand?.stockAggregateId ?? String(saved?.stockId ?? '').trim()
      if (!stockId) {
        ElMessage.error(
          t('pickCreate.reloadCandidatesForLine', { code: pl.itemCode || pl.packingItemId })
        )
        return false
      }
    }
  }
  const allLines = buildAllPickLinesFromDraft()
  if (allLines.length === 0) {
    ElMessage.warning(t('pickCreate.allocateQtyFirst'))
    return false
  }
  return true
}

const loadWarehouses = async () => {
  try {
    warehouses.value = await inventoryCenterApi.getWarehouses()
    if (!isPickOnlyPage.value && !form.warehouseId && warehouses.value.length) {
      form.warehouseId = warehouses.value[0].id || ''
    }
  } catch (e) {
    console.error(e)
    warehouses.value = []
  }
}

const loadRequest = async () => {
  if (!form.stockOutRequestId) return
  try {
    const p = await stockOutApi.getRequestListPaged({ page: 1, pageSize: 2000 })
    const rid = form.stockOutRequestId.trim()
    currentRequest.value =
      p.items.find((x) => x.id === rid || x.id?.toLowerCase?.() === rid.toLowerCase()) || null
  } catch (e) {
    console.error(e)
    currentRequest.value = null
  }
}

const loadItemsFromRequest = async () => {
  if (!form.stockOutRequestId) {
    ElMessage.warning('请先选择出库通知')
    return
  }
  if (!currentRequest.value?.salesOrderId) {
    await loadRequest()
  }
  const r = currentRequest.value
  if (!r?.salesOrderId) {
    ElMessage.warning('出库通知缺少销售订单信息')
    return
  }
  const materialCode = String(r.materialModel ?? '').trim()
  const raw = r as Record<string, unknown>
  const qRaw = raw.outQuantity ?? raw.OutQuantity
  const qty = typeof qRaw === 'number' ? qRaw : Number(qRaw ?? 0)
  if (!materialCode || !Number.isFinite(qty) || qty <= 0) {
    ElMessage.warning('出库通知缺少物料或数量，无法生成出库明细')
    return
  }
  form.items = [
    {
      lineNo: 1,
      materialCode,
      materialName: String(r.brand ?? '').trim() || '物料',
      quantity: qty
    }
  ]
  await pickRecommendedWarehouse()
}

const pickRecommendedWarehouse = async () => {
  if (!form.items.length || !warehouses.value.length) return
  try {
    const overview = await inventoryCenterApi.getOverview()
    const materialSet = new Set(form.items.map(x => x.materialCode))
    const candidates = overview
      .filter(x => materialSet.has(x.materialId) && Number(x.availableQty || 0) > 0)
      .sort((a, b) => Number(b.availableQty || 0) - Number(a.availableQty || 0))
    if (!candidates.length) return
    const bestWarehouseId = candidates[0].warehouseId
    if (!form.warehouseId || !candidates.some(x => x.warehouseId === form.warehouseId)) {
      form.warehouseId = bestWarehouseId
    }
  } catch {
    // 推荐仓库失败不阻断主流程
  }
}

const resolveLinkedPackingId = async () => {
  const rid = form.stockOutRequestId?.trim()
  if (!rid) {
    linkedPackingId.value = ''
    return
  }
  try {
    const packing = await packingApi.getByStockOutRequestId(rid)
    linkedPackingId.value = String(packing?.id ?? '').trim()
  } catch {
    linkedPackingId.value = ''
  }
}

const loadPickingTasks = async () => {
  try {
    await resolveLinkedPackingId()
    const tasks = await inventoryCenterApi.getPickingTasks()
    pickingTasks.value = (tasks || []).filter((t) => pickingTaskMatchesCurrentRequest(t))
  } catch {
    pickingTasks.value = []
  }
}

function hydratePickDraftFromPage(page: PickPageByPacking) {
  for (const k of Object.keys(pickDraftByLine)) delete pickDraftByLine[k]
  for (const line of page.lines) {
    const items = line.pickingItems ?? []
    if (!items.length) continue
    const bucket: Record<string, number> = {}
    for (const pi of items) {
      const sid = String(pi.stockItemId ?? '').trim()
      if (!sid) continue
      const q = Math.max(0, Math.floor(Number(pi.pickedQty ?? pi.planQty) || 0))
      if (q > 0) bucket[sid] = (bucket[sid] ?? 0) + q
    }
    if (Object.keys(bucket).length) pickDraftByLine[line.packingItemId] = bucket
  }
}

function syncPackingLinesTableHighlight() {
  const lines = pickPage.value?.lines
  const lineId = selectedPackingItemId.value?.trim()
  if (!lines?.length || !lineId) return
  const row = lines.find((l) => l.packingItemId === lineId)
  if (row) packingLinesTableRef.value?.setCurrentRow(row)
}

function resolvePickPageWarehouse(page: PickPageByPacking) {
  const fromTask = page.pickingTask?.warehouseId?.trim()
  const fromPacking = page.warehouseId?.trim()
  if (fromTask) {
    form.warehouseId = fromTask
    return
  }
  if (fromPacking) {
    form.warehouseId = fromPacking
    return
  }
  form.warehouseId = ''
}

async function applyDefaultPackingLineSelection(page: PickPageByPacking) {
  if (!page.lines.length) {
    selectedPackingItemId.value = ''
    pickingCandidates.value = []
    pickingCandidatesLoaded.value = false
    lastPickCandidatesKey.value = ''
    return
  }
  const current = selectedPackingItemId.value?.trim()
  const keep = Boolean(current && page.lines.some((l) => l.packingItemId === current))
  selectedPackingItemId.value = keep ? current! : page.lines[0].packingItemId
  pickingCandidatesLoaded.value = false
  lastPickCandidatesKey.value = ''
  if (!form.warehouseId?.trim()) {
    pickingCandidates.value = []
    return
  }
  await ensurePickCandidatesLoaded()
}

async function ensurePickCandidatesLoaded(force = false) {
  if (!isPickOnlyPage.value || loadingPickPage.value) return
  const lineId = selectedPackingItemId.value?.trim()
  const wh = form.warehouseId?.trim()
  if (!lineId || !wh) return
  const key = `${lineId}|${wh}`
  if (!force && lastPickCandidatesKey.value === key && pickingCandidatesLoaded.value) return
  await loadPickingCandidatesForSelectedLine()
  lastPickCandidatesKey.value = key
}

const loadPickPage = async () => {
  if (!isPickOnlyPage.value) return
  const pid = packingId.value
  if (!pid) {
    pickPage.value = null
    pickPageLoadError.value = t('pickCreate.missingPackingId')
    return
  }
  loadingPickPage.value = true
  pickPageLoadError.value = ''
  try {
    const page = await inventoryCenterApi.getPickPageByPacking(pid)
    pickPage.value = page
    resolvePickPageWarehouse(page)
    hydratePickDraftFromPage(page)
    await applyDefaultPackingLineSelection(page)
  } catch (e) {
    console.error(e)
    pickPage.value = null
    pickPageLoadError.value = getApiErrorMessage(e, t('pickCreate.packingLinesLoadFailed'))
  } finally {
    loadingPickPage.value = false
  }
  if (pickPage.value?.lines?.length && selectedPackingItemId.value) {
    await nextTick()
    syncPackingLinesTableHighlight()
  }
}

function onPackingLineRowClick(row: PickPagePackingLine) {
  if (!row?.packingItemId) return
  if (selectedPackingItemId.value === row.packingItemId) return
  selectedPackingItemId.value = row.packingItemId
  pickingCandidatesLoaded.value = false
  lastPickCandidatesKey.value = ''
  void ensurePickCandidatesLoaded(true)
}

const loadPickingCandidatesForSelectedLine = async () => {
  const lineId = selectedPackingItemId.value?.trim()
  if (!lineId || !form.warehouseId?.trim()) {
    pickingCandidates.value = []
    pickingCandidatesLoaded.value = false
    if (lineId || form.warehouseId?.trim()) {
      ElMessage.warning(t('pickCreate.selectLineAndWarehouse'))
    }
    return
  }
  loadingCandidates.value = true
  pickingCandidatesLoaded.value = false
  try {
    const list = await inventoryCenterApi.getPickingCandidatesByPackingItem(lineId, form.warehouseId.trim())
    pickingCandidates.value = list || []
    pickingCandidatesCache.value[lineId] = list || []
    pickingSourceFilter.value = 'all'
  } catch (e) {
    console.error(e)
    pickingCandidates.value = []
    ElMessage.error(getApiErrorMessage(e, '加载拣货候选失败'))
  } finally {
    loadingCandidates.value = false
    pickingCandidatesLoaded.value = true
    if (lineId && form.warehouseId?.trim()) {
      lastPickCandidatesKey.value = `${lineId}|${form.warehouseId.trim()}`
    }
  }
}

watch(
  () =>
    isPickOnlyPage.value
      ? `${form.warehouseId}|${selectedPackingItemId.value}|${pickPage.value?.lines?.length ?? 0}`
      : '',
  () => {
    void ensurePickCandidatesLoaded()
  }
)

const loadPickingCandidates = async () => {
  if (!form.stockOutRequestId?.trim() || !form.warehouseId?.trim()) {
    ElMessage.warning('请先选择出库通知与仓库')
    return
  }
  loadingCandidates.value = true
  try {
    const list = await inventoryCenterApi.getPickingCandidates(form.stockOutRequestId.trim(), form.warehouseId.trim())
    pickingCandidates.value = list || []
    pickingSourceFilter.value = 'all'
    clearPickDraft()
  } catch (e) {
    console.error(e)
    pickingCandidates.value = []
    ElMessage.error(getApiErrorMessage(e, '加载拣货候选失败'))
  } finally {
    loadingCandidates.value = false
  }
}

const applyFifoToPickDraft = () => {
  clearActivePickDraft()
  const target = isPickOnlyPage.value ? selectedLineTargetQty.value : notifyTargetQty.value
  let rem = target
  for (const c of pickingCandidates.value) {
    if (rem <= 0) break
    const avail = Math.max(0, Math.floor(Number(c.availableQty)))
    const take = Math.min(rem, avail)
    if (take > 0) {
      activePickDraft()[c.stockItemId] = take
      rem -= take
    }
  }
  if (rem > 0) ElMessage.warning(`候选可用量不足，尚有 ${rem} 未分配，请补库存或手工调整`)
  else ElMessage.success('已按 FIFO 顺序填满（请核对后点「确认拣货数量」）')
}

/** 仅确认本页拣货数量（内存），不写数据库 */
const confirmPickQuantities = () => {
  if (!isPickOnlyPage.value && !pendingPickingTask.value) return
  if (!validatePickDraftTotal()) return
  pickDraftConfirmed.value = true
  ElMessage.success(t('pickCreate.confirmQtySuccess'))
}

/** 拣货专页：点击「生成拣货单」时创建拣货任务（若尚未创建） */
async function ensurePickPagePickingTask(): Promise<PickingTask | null> {
  const existing = pendingPickingTask.value
  if (existing?.id) return existing
  const pid = packingId.value
  if (!pid || !form.warehouseId?.trim()) return null
  await inventoryCenterApi.generatePickingTaskByPacking({
    packingId: pid,
    warehouseId: form.warehouseId.trim(),
    operatorId: form.operatorId
  })
  await loadPickPage()
  const t = pickPage.value?.pickingTask
  if (!t || t.status === 100 || t.status === -1) return null
  return t
}

/** 将已确认的拣货明细写入数据库（生成拣货单） */
const submitPickingOrder = async () => {
  if (!pickDraftConfirmed.value) {
    ElMessage.warning(t('pickCreate.confirmQtyFirst'))
    return
  }
  if (!validatePickDraftTotal()) return
  const lines = isPickOnlyPage.value ? buildAllPickLinesFromDraft() : buildPickLinesFromDraft()
  submittingPickingOrder.value = true
  try {
    let task: PickingTask | null = pendingPickingTask.value
    if (isPickOnlyPage.value) {
      task = await ensurePickPagePickingTask()
      if (!task?.id) {
        ElMessage.error(t('pickCreate.submitPickingOrderFailed'))
        return
      }
    } else if (!task?.id) {
      return
    }
    await inventoryCenterApi.savePickingTaskItems(task.id, lines)
    pickDraftConfirmed.value = false
    if (isPickOnlyPage.value) {
      await ElMessageBox.alert(t('pickCreate.submitPickingOrderCreatedDialog'), t('pickCreate.submitPickingOrderCreatedTitle'), {
        type: 'success',
        confirmButtonText: t('common.confirm')
      })
      await router.push({ name: 'PickingSlipList' })
    } else {
      ElMessage.success(t('pickCreate.submitPickingOrderSuccess'))
      await loadPickingTasks()
    }
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, t('pickCreate.submitPickingOrderFailed')))
  } finally {
    submittingPickingOrder.value = false
  }
}

/** 执行出库页：明细区按钮仍直接写库 */
const savePickingDraftToDb = async () => {
  const task = pendingPickingTask.value
  if (!task?.id) return
  if (!validatePickDraftTotal()) return
  const lines = buildPickLinesFromDraft()
  submittingPickingOrder.value = true
  try {
    await inventoryCenterApi.savePickingTaskItems(task.id, lines)
    ElMessage.success(t('pickCreate.savePickingLinesSuccess'))
    await loadPickingTasks()
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, t('pickCreate.submitPickingOrderFailed')))
  } finally {
    submittingPickingOrder.value = false
  }
}

async function generatePickingTaskCore(options?: { silent?: boolean }): Promise<boolean> {
  if (requestAlreadyShipped.value) {
    if (!options?.silent) ElMessage.warning('该出库通知已执行出库，无法再次生成拣货任务')
    return false
  }
  if (hasActivePickingTask.value) return true
  if (!form.stockOutRequestId) {
    if (!options?.silent) ElMessage.warning('请先填写出库申请单ID')
    return false
  }
  if (!form.warehouseId || !form.items.length) {
    if (!options?.silent) ElMessage.warning('请先填写仓库和出库明细')
    return false
  }
  if (form.items.some((x) => !x.materialCode || Number(x.quantity || 0) <= 0)) {
    if (!options?.silent) ElMessage.warning('出库明细存在空物料或数量为0，请检查来源数据')
    return false
  }
  try {
    await inventoryCenterApi.generatePickingTask({
      stockOutRequestId: form.stockOutRequestId,
      warehouseId: form.warehouseId,
      operatorId: form.operatorId,
      items: []
    })
    if (!options?.silent) ElMessage.success('拣货任务已生成')
    await loadPickingTasks()
    await loadPickingCandidates()
    return true
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, '生成拣货任务失败'))
    return false
  }
}

const handleGeneratePicking = async () => {
  await generatePickingTaskCore()
}

const completePicking = async (taskId: string) => {
  if (!isPickOnlyPage.value && requestAlreadyShipped.value) {
    ElMessage.warning('该出库通知已出库')
    return
  }
  completingPicking.value = true
  try {
    await inventoryCenterApi.completePickingTask(taskId)
    ElMessage.success('拣货已完成')
    if (isPickOnlyPage.value) await loadPickPage()
    else await loadPickingTasks()
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, '完成拣货失败'))
  } finally {
    completingPicking.value = false
  }
}

const handleSubmit = async () => {
  if (requestAlreadyShipped.value) {
    ElMessage.warning('该出库通知已执行出库')
    return
  }
  if (!pickingCompleted.value) {
    ElMessage.warning('请先完成拣货任务后再执行出库')
    return
  }
  if (!form.stockOutCode || !form.warehouseId) {
    ElMessage.warning('请填写出库单号和仓库')
    return
  }
  if (!form.items.length) {
    ElMessage.warning('请至少添加一条出库明细')
    return
  }

  submitting.value = true
  try {
    const execPackingId = packingId.value || linkedPackingId.value || undefined
    await stockOutApi.execute({
      stockOutRequestId: form.stockOutRequestId,
      packingId: execPackingId,
      stockOutCode: form.stockOutCode,
      warehouseId: form.warehouseId,
      operatorId: form.operatorId,
      stockOutDate: form.stockOutDate,
      remark: form.remark,
      items: form.items
    })
    ElMessage.success('执行出库成功，出库通知已标记为已出库')
    const fromPackingBatch = Boolean(
      execPackingId || sessionStorage.getItem(PACKING_STOCK_OUT_QUEUE_KEY)
    )
    const next = popNextPackingStockOutQueueEntry()
    if (next?.requestId) {
      await router.push({
        path: '/inventory/stock-out/create',
        query: {
          requestId: next.requestId,
          ...(next.packingId ? { packingId: next.packingId } : {})
        }
      })
      return
    }
    if (fromPackingBatch) {
      router.push({ name: 'PackingList' })
    } else {
      router.push({ name: 'StockOutNotifyList' })
    }
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, '执行出库失败'))
  } finally {
    submitting.value = false
  }
}

const goBack = () => {
  if (isPickOnlyPage.value) {
    router.push({ name: 'PackingList' })
    return
  }
  router.push({ name: 'StockOutNotifyList' })
}

const init = async () => {
  await loadWarehouses()
  if (isNotifyDetailPage.value) {
    await loadRequest()
    return
  }
  if (isPickOnlyPage.value) {
    if (!packingId.value) {
      ElMessage.error(t('pickCreate.missingPackingId'))
      return
    }
    await loadPickPage()
    await ensurePickCandidatesLoaded()
    return
  }
  if (!form.stockOutRequestId?.trim() && packingId.value) {
    try {
      const resolved = await packingApi.resolveStockOutRequestIds([packingId.value])
      const rid = resolved.stockOutRequestIds[0]?.trim()
      if (rid) form.stockOutRequestId = rid
    } catch (e) {
      console.error(e)
    }
  }
  await loadRequest()
  if (requestAlreadyShipped.value) {
    ElMessage.info('该出库通知已执行出库，仅可查看信息')
  }
  await loadItemsFromRequest()
  await loadPickingTasks()
  if (pendingPickingTask.value && form.warehouseId?.trim()) {
    await loadPickingCandidates()
  }
}

init()
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.stockout-edit-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
  .header-left { display: flex; align-items: center; gap: 12px; }
  .header-right { display: flex; align-items: center; gap: 8px; }
}
.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
  .page-icon {
    width: 36px;
    height: 36px;
    background: rgba(0, 212, 255, 0.1);
    border: 1px solid rgba(0, 212, 255, 0.25);
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: $cyan-primary;
  }
  .page-title { font-size: 20px; font-weight: 600; color: $text-primary; margin: 0; }
}
.btn-primary,
.btn-secondary,
.btn-picking {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  cursor: pointer;
  border: 1px solid transparent;
}
.btn-primary {
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border-color: rgba(0, 212, 255, 0.4);
  color: #fff;
}
.btn-picking {
  background: linear-gradient(135deg, rgba(0, 140, 120, 0.55), rgba(0, 100, 90, 0.45));
  border-color: rgba(0, 212, 180, 0.4);
  color: #e8fff8;
  &:hover:not(:disabled) {
    filter: brightness(1.08);
  }
}
.btn-secondary {
  background: rgba(255, 255, 255, 0.05);
  border-color: $border-panel;
  color: $text-secondary;
}
.btn-sm {
  padding: 6px 10px;
  font-size: 12px;
}
.form-layout {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
/** 与 .form-layout 内相邻 form-card 的 gap（16px）一致：分隔「出库明细」与「拣货任务」 */
.form-card--after-outbound {
  margin-top: 16px;
}
.pick-only-page .form-card + .form-card {
  margin-top: 16px;
}
.form-card {
  background: $layer-2;
  border-radius: 8px;
  border: 1px solid $border-panel;
  padding: 16px 18px;
}
.pick-kv-descriptions {
  width: 100%;
  :deep(.el-descriptions__label) {
    width: 100px;
    font-weight: 500;
    color: $text-muted;
  }
  :deep(.el-descriptions__content) {
    color: $text-primary;
  }
}
.pick-kv-warehouse-readonly {
  font-weight: 500;
  color: $text-primary;
}
.basic-info-form {
  :deep(.el-form-item__label) {
    width: 6em !important;
    min-width: 6em;
    max-width: 6em;
    text-align: right;
    justify-content: flex-end;
    padding-right: 10px;
    box-sizing: content-box;
    white-space: nowrap;
  }
}
.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
}
.section-title {
  font-size: 14px;
  font-weight: 500;
  color: $text-secondary;
  margin: 0 0 8px;
}
.pick-only-page .section-title {
  font-weight: 700;
  color: $text-primary;
}
.pick-line-status--allocated {
  color: #46bf91;
  font-weight: 500;
}
.table-footer {
  display: flex;
  justify-content: flex-end;
  margin-top: 8px;
  .total {
    font-size: 13px;
    color: $text-secondary;
    span {
      color: $cyan-primary;
      font-weight: 600;
      margin-left: 4px;
    }
  }
}
.action-btn {
  background: transparent;
  border: none;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 12px;
  padding: 2px 6px;
  white-space: nowrap;
  flex-shrink: 0;
  &:hover:not(:disabled) {
    text-decoration: underline;
  }
  &:disabled,
  &[disabled] {
    color: rgba(140, 155, 175, 0.55) !important;
    cursor: not-allowed;
    opacity: 1;
  }
}
.qty-cell {
  font-variant-numeric: tabular-nums;
  color: $text-primary;
  font-size: 13px;
}

/** 出库明细：物料来自通知单，仅展示不可编辑、不抢焦点（避免误认为可点选弹窗） */
.stock-out-line-material {
  display: inline-block;
  max-width: 100%;
  font-size: 13px;
  color: $text-primary;
  line-height: 1.5;
  cursor: default;
  user-select: text;
}

.flow-alert {
  margin-bottom: 16px;
  background: rgba(0, 212, 255, 0.06) !important;
  border: 1px solid rgba(0, 212, 255, 0.2) !important;
}
.flow-alert-title {
  font-weight: 600;
  color: $text-primary;
}
.flow-steps {
  margin: 8px 0 0;
  padding-left: 1.25rem;
  color: rgba(200, 216, 232, 0.85);
  font-size: 13px;
  line-height: 1.7;
  li {
    margin-bottom: 2px;
  }
  .flow-step--done {
    color: #46bf91;
  }
}
.flow-warn-msg,
.flow-done-msg {
  margin: 10px 0 0;
  font-size: 12px;
}
.flow-warn-msg {
  color: #ffc107;
}
.flow-done-msg {
  color: #46bf91;
}
.form-sub-hint {
  margin-top: 6px;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.4;
  word-break: break-all;
}
.picking-hint {
  margin: 0 0 12px;
  font-size: 12px;
  /* 与顶部流程区 .flow-warn-msg（请先完成拣货任务…）同色 */
  color: #ffc107;
  line-height: 1.55;
}
.picking-empty {
  margin: 0 0 12px;
  font-size: 13px;
  color: $text-muted;
}
.picking-expand-inner {
  padding: 8px 12px 14px 40px;
  background: rgba(0, 0, 0, 0.14);
  border-radius: 8px;
  border: 1px solid rgba(0, 212, 255, 0.08);
}
.picking-expand-title {
  font-size: 12px;
  color: $text-muted;
  margin-bottom: 8px;
}
:deep(.picking-lines-table tr.picking-line-row--stocking td.el-table__cell) {
  background: rgba(255, 193, 7, 0.14) !important;
}
:deep(.picking-lines-table tr.picking-line-row--stocking:hover td.el-table__cell) {
  background: rgba(255, 193, 7, 0.22) !important;
}
.picking-source-stocking {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  color: #ffc107;
  font-weight: 600;
  font-size: 12px;
}
.picking-stock-icon {
  font-size: 16px;
}
.picking-source-normal {
  font-size: 12px;
  color: rgba(200, 216, 232, 0.72);
}
.btn-primary:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}
.btn-secondary:disabled,
.btn-picking:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}
.picking-draft-toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
}
.picking-draft-toolbar__left {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}
.picking-source-filter {
  flex: 1 1 220px;
  min-width: 0;
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  align-items: center;
}
.picking-draft-toolbar__right {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px;
  margin-left: auto;
}
.picking-draft-sum {
  font-size: 13px;
  font-weight: 600;
  color: $text-primary;
  letter-spacing: 0.01em;
  strong {
    font-weight: 700;
    color: $text-primary;
    font-variant-numeric: tabular-nums;
  }
}
.picking-draft-confirmed-tag {
  margin-left: 10px;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  color: #9ef0d0;
  background: rgba(0, 160, 120, 0.2);
  border: 1px solid rgba(0, 212, 180, 0.35);
}
.picking-draft-save-btn--confirmed {
  border-color: rgba(0, 212, 180, 0.55);
  box-shadow: 0 0 0 1px rgba(0, 212, 180, 0.15);
}
.picking-draft-save-btn {
  flex-shrink: 0;
}
:deep(.picking-source-filter .el-radio-button__inner) {
  padding: 6px 12px;
  font-size: 12px;
}
.picking-candidates-table {
  margin-top: 4px;
}
.picking-empty.subtle {
  font-size: 12px;
  opacity: 0.92;
}
.picking-empty--warn {
  color: #f5a97a;
}
.picking-empty--done {
  color: #7fd99a;
}
.packing-lines-on-pick-table {
  margin-top: 8px;
}

.notify-code-cell {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.customs-notify-tag {
  flex: 0 0 auto;
  padding: 1px 6px;
  border-radius: 4px;
  font-size: 11px;
  line-height: 1.4;
  color: #ffb84d;
  background: rgba(255, 184, 77, 0.14);
  border: 1px solid rgba(255, 184, 77, 0.45);
  cursor: default;
  user-select: none;
}

.cell-link {
  color: $cyan-primary;
  text-decoration: none;

  &:hover {
    text-decoration: underline;
  }
}
</style>

