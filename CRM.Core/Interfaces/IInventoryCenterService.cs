using System.Collections.Generic;
using CRM.Core.Constants;
using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces
{
    public interface IInventoryCenterService
    {
        Task PostStockInAsync(string stockInId);
        Task RecordStockOutAsync(string stockOutId);

        Task<IEnumerable<InventoryMaterialOverviewDto>> GetMaterialOverviewAsync(string? warehouseId);
        /// <summary>按销售明细解析物料键（含关联采购行），汇总全仓库可用库存；用于出库申请展示。</summary>
        Task<SellOrderLineAvailableQtyDto> GetAvailableQtyForSellOrderItemAsync(string sellOrderItemId);
        Task<IEnumerable<InventoryMaterialTraceDto>> GetMaterialTraceAsync(string materialId);
        /// <summary>某条汇总库存（<c>stock.StockId</c>）下的全部在库明细 <c>stockitem</c>。</summary>
        Task<IEnumerable<InventoryStockItemRowDto>> GetStockItemsForAggregateAsync(string stockAggregateId);

        /// <summary>全库 <c>stockitem</c> 列表，支持入库单、日期、型号、品牌、出库状态及采销人员等筛选。</summary>
        Task<IEnumerable<InventoryStockItemListRowDto>> GetStockItemsListAsync(InventoryStockItemListQuery? query);
        Task<InventoryFinanceSummaryDto> GetFinanceSummaryAsync(int stagnantDays = 90);

        Task<IEnumerable<WarehouseInfo>> GetWarehousesAsync();
        Task<WarehouseInfo> SaveWarehouseAsync(WarehouseInfo warehouse);

        Task<IEnumerable<PickingTaskSummaryDto>> GetPickingTasksAsync(short? status = null);
        /// <summary>仅创建拣货任务壳；明细由 <see cref="SavePickingTaskItemsAsync"/> 写入。</summary>
        Task<PickingTask> GeneratePickingTaskAsync(GeneratePickingTaskRequest request);
        Task CompletePickingTaskAsync(string taskId);

        /// <summary>待拣货候选 <c>stockitem</c>（客单绑定 + 备货 PN/品牌匹配），FIFO 仅排序。</summary>
        Task<IReadOnlyList<PickingStockItemCandidateDto>> GetPickingCandidateStockItemsAsync(string stockOutRequestId, string warehouseId);

        /// <summary>未完成拣货前保存/覆盖拣货明细；数量之和须等于出库通知数量。</summary>
        Task SavePickingTaskItemsAsync(string pickingTaskId, IReadOnlyList<SavePickingTaskItemLineRequest> lines);

        /// <summary>拣货单列表行（出库通知 + 销售订单 + 仓库等展示字段）。</summary>
        Task<IReadOnlyList<PickingTaskListItemDto>> GetPickingTaskListRowsAsync();

        /// <summary>拣货单详情（列表头字段 + 明细行）。</summary>
        Task<PickingTaskDetailViewDto?> GetPickingTaskDetailForUiAsync(string pickingTaskId);

        Task<IEnumerable<InventoryCountPlan>> GetCountPlansAsync();
        Task<InventoryCountPlan> CreateMonthlyCountPlanAsync(CreateCountPlanRequest request);
        Task SubmitCountPlanAsync(SubmitCountPlanRequest request);
    }

    public class SellOrderLineAvailableQtyDto
    {
        public int AvailableQty { get; set; }
    }

    public class InventoryMaterialOverviewDto
    {
        /// <summary>库存行主键（<c>stock.StockId</c>）</summary>
        public string StockId { get; set; } = string.Empty;
        /// <summary>库存业务编号（历史行可能为空）</summary>
        public string? StockCode { get; set; }
        public string MaterialId { get; set; } = string.Empty;
        /// <summary>规格型号（优先 <c>stock.purchase_pn</c>，缺省再物料主数据 / 订单行 PN）</summary>
        public string? MaterialModel { get; set; }
        /// <summary>品牌展示（优先 <c>stock.purchase_brand</c>，缺省再物料名称 / 订单行 Brand）</summary>
        public string? MaterialName { get; set; }
        public string WarehouseId { get; set; } = string.Empty;
        /// <summary>仓库编码（由 WarehouseId 解析；无档案时前端可回退显示 WarehouseId）</summary>
        public string? WarehouseCode { get; set; }
        /// <summary>库存类型 1=客单库存 2=备货库存 3=样品库存</summary>
        public short StockType { get; set; } = 1;
        /// <summary>地域（与 <c>stock.RegionType</c> 一致；10=境内 20=境外）</summary>
        public short RegionType { get; set; } = RegionTypeCode.Domestic;
        public int OnHandQty { get; set; }
        public int AvailableQty { get; set; }
        public int LockedQty { get; set; }
        public decimal InventoryAmount { get; set; }
        /// <summary>库存金额币别（与最近一次采购入库台账关联的采购单一致；缺省 1=RMB）</summary>
        public short Currency { get; set; } = 1;
        public DateTime? LastMoveTime { get; set; }
    }

    /// <summary>库存汇总行下钻：单条 <c>stockitem</c> 展示行。</summary>
    public class InventoryStockItemRowDto
    {
        public string StockItemId { get; set; } = string.Empty;
        public string StockInItemId { get; set; } = string.Empty;
        public string StockInId { get; set; } = string.Empty;
        public string? StockInCode { get; set; }
        public string MaterialId { get; set; } = string.Empty;
        public string? LocationId { get; set; }
        public string? BatchNo { get; set; }
        public DateTime? ProductionDate { get; set; }
        public string? PurchasePn { get; set; }
        public string? PurchaseBrand { get; set; }
        public string? SellOrderItemCode { get; set; }
        public int QtyInbound { get; set; }
        public int QtyStockOut { get; set; }
        public int QtyRepertory { get; set; }
        public int QtyRepertoryAvailable { get; set; }
        public int QtyOccupy { get; set; }
        public int QtySales { get; set; }
        public decimal PurchasePrice { get; set; }
        /// <summary>采购单价币别（<see cref="CRM.Core.Constants.CurrencyCode"/>）</summary>
        public short PurchaseCurrency { get; set; }
        /// <summary>采购单价折合 USD</summary>
        public decimal PurchasePriceUsd { get; set; }
        public decimal? SalesPrice { get; set; }
        /// <summary>销售单价币别；无销售行时为 null</summary>
        public short? SalesCurrency { get; set; }
        /// <summary>销售单价折合 USD；无销售行时为 null</summary>
        public decimal? SalesPriceUsd { get; set; }
        public string? VendorName { get; set; }
        public string? CustomerName { get; set; }
        public DateTime CreateTime { get; set; }
    }

    /// <summary>全库库存明细列表行（在 <see cref="InventoryStockItemRowDto"/> 基础上扩展展示/筛选字段）。</summary>
    public class InventoryStockItemListRowDto : InventoryStockItemRowDto
    {
        public DateTime? StockInDate { get; set; }
        public string? PurchaserName { get; set; }
        public string? SalespersonName { get; set; }
        public string StockAggregateId { get; set; } = string.Empty;
        public string WarehouseId { get; set; } = string.Empty;
        public string? WarehouseCode { get; set; }
        /// <summary>仓库名称（由 <see cref="WarehouseId"/> 解析；列表展示用，避免把 Guid 暴露给用户）</summary>
        public string? WarehouseName { get; set; }
        /// <summary>出库状态：1=未出库 2=部分出库 3=出库完成（仅 <c>QtyInbound&gt;0</c> 时有意义；否则为 0）</summary>
        public short OutboundStatus { get; set; }

        /// <summary>入库 USD 价差快照（<c>SalesPriceUsd</c>、<c>PurchasePriceUsd</c> × <c>QtyInbound</c>）；出库利润见 <c>stockoutitemextend</c>。</summary>
        public decimal ProfitOutBizUsd { get; set; }
    }

    /// <summary>全库库存明细查询条件（字段为空则不作为筛选）。</summary>
    public class InventoryStockItemListQuery
    {
        public string? StockInCode { get; set; }
        public DateTime? StockInDateFrom { get; set; }
        public DateTime? StockInDateTo { get; set; }
        public string? PurchasePn { get; set; }
        public string? PurchaseBrand { get; set; }
        /// <summary>0 或不传=全部；1=未出库；2=部分出库；3=出库完成</summary>
        public short? OutboundStatus { get; set; }
        public string? CustomerName { get; set; }
        public string? VendorName { get; set; }
        public string? SalespersonName { get; set; }
        public string? PurchaserName { get; set; }
        /// <summary>业务员用户主键（与 <c>stockitem</c> 冗余 <c>SalespersonId</c> 一致）；优先于 <see cref="SalespersonName"/> 模糊匹配。</summary>
        public string? SalespersonUserId { get; set; }
        /// <summary>采购员用户主键（与 <c>stockitem</c> 冗余 <c>PurchaserId</c> 一致）；优先于 <see cref="PurchaserName"/> 模糊匹配。</summary>
        public string? PurchaserUserId { get; set; }
    }

    public class InventoryMaterialTraceDto
    {
        public DateTime? StockInTime { get; set; }
        public string? StockInCode { get; set; }
        public string? BatchNo { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? PurchaseOrderCode { get; set; }
        public string? PurchaseUserName { get; set; }
        public short? QcStatus { get; set; }
        public string? QcCode { get; set; }
        public string? WarehouseId { get; set; }
        /// <summary>仓库名称（由 WarehouseId 解析）</summary>
        public string? WarehouseName { get; set; }
        public string? LocationId { get; set; }
    }

    public class InventoryFinanceSummaryDto
    {
        public decimal InventoryCapital { get; set; }
        public decimal MonthlyOutCost { get; set; }
        public decimal AverageInventoryCapital { get; set; }
        public decimal TurnoverRate { get; set; }
        public decimal TurnoverDays { get; set; }
        public int StagnantMaterialCount { get; set; }
    }

    /// <summary>拣货任务明细行（前端可展示备货补充标记）</summary>
    public class PickingTaskLineDto
    {
        public string Id { get; set; } = string.Empty;
        public string MaterialId { get; set; } = string.Empty;
        public string? StockId { get; set; }
        /// <summary>在库明细 <c>stockitem</c> 主键（新流程）。</summary>
        public string? StockItemId { get; set; }
        /// <summary>对应库存行类型 1客单 2备货 3样品；无库存记录时可为空</summary>
        public short? StockType { get; set; }
        public int PlanQty { get; set; }
        public int PickedQty { get; set; }
        public bool IsStockingSupplement { get; set; }
    }

    /// <summary>拣货任务列表（含明细汇总数量，供前端展示）</summary>
    public class PickingTaskSummaryDto
    {
        public string Id { get; set; } = string.Empty;
        public string TaskCode { get; set; } = string.Empty;
        public string StockOutRequestId { get; set; } = string.Empty;
        public string WarehouseId { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        public short Status { get; set; }
        public string? Remark { get; set; }
        public DateTime CreateTime { get; set; }
        /// <summary>本任务拣货明细计划数量合计</summary>
        public int PlanQtyTotal { get; set; }
        /// <summary>本任务拣货明细已拣数量合计</summary>
        public int PickedQtyTotal { get; set; }
        /// <summary>本任务涉及的库存类型（去重排序，1客单 2备货 3样品）</summary>
        public List<short> DistinctStockTypes { get; set; } = new();
        public List<PickingTaskLineDto> Items { get; set; } = new();
    }

    /// <summary>拣货单列表（主从拣货任务 + 出库通知/订单展示列）。</summary>
    public class PickingTaskListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public short Status { get; set; }
        public string WarehouseId { get; set; } = string.Empty;
        /// <summary>仓库名称（编码）展示</summary>
        public string? WarehouseDisplay { get; set; }
        public string? MaterialModel { get; set; }
        public string? Brand { get; set; }
        public string? CustomerName { get; set; }
        public string? SalesUserName { get; set; }
        /// <summary>拣货计划数量合计</summary>
        public int PlanQtyTotal { get; set; }
        public int LineCount { get; set; }
        public string? StockOutRequestCode { get; set; }
        public string TaskCode { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        /// <summary>生成拣货任务时的操作人（OperatorId 解析）</summary>
        public string? CreateUserDisplay { get; set; }
    }

    /// <summary>拣货单详情（含明细行）。</summary>
    public class PickingTaskDetailViewDto : PickingTaskListItemDto
    {
        public string? Remark { get; set; }
        public List<short> DistinctStockTypes { get; set; } = new();
        public List<PickingTaskLineDto> Items { get; set; } = new();
    }

    public class GeneratePickingTaskRequest
    {
        public string StockOutRequestId { get; set; } = string.Empty;
        public string WarehouseId { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        /// <summary>已废弃自动拆行；可传空列表，明细仅由 <see cref="IInventoryCenterService.SavePickingTaskItemsAsync"/> 维护。</summary>
        public List<GeneratePickingTaskItemRequest> Items { get; set; } = new();
    }

    public class GeneratePickingTaskItemRequest
    {
        public string MaterialId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    /// <summary>拣货候选在库明细行（FIFO 排序用字段由服务端填充）。</summary>
    public class PickingStockItemCandidateDto
    {
        public string StockItemId { get; set; } = string.Empty;
        public string StockAggregateId { get; set; } = string.Empty;
        public string MaterialId { get; set; } = string.Empty;
        public int AvailableQty { get; set; }
        public short StockType { get; set; }
        public string? PurchasePn { get; set; }
        public string? PurchaseBrand { get; set; }
        public string? LocationId { get; set; }
        public string? BatchNo { get; set; }
        public string WarehouseId { get; set; } = string.Empty;
        public DateTime? ProductionDate { get; set; }
        public DateTime CreateTime { get; set; }
        /// <summary>true 表示备货池命中（非本销售行强绑定的备货桶）。</summary>
        public bool IsStockingCandidate { get; set; }
    }

    public class SavePickingTaskItemLineRequest
    {
        public string StockItemId { get; set; } = string.Empty;
        /// <summary>汇总桶 <c>stock.Id</c>，须与 <c>stockitem.StockAggregateId</c> 一致。</summary>
        public string StockId { get; set; } = string.Empty;
        public int Qty { get; set; }
    }

    public class CreateCountPlanRequest
    {
        public string PlanMonth { get; set; } = string.Empty; // yyyy-MM
        public string WarehouseId { get; set; } = string.Empty;
        public string CreatorId { get; set; } = string.Empty;
        public string? Remark { get; set; }
    }

    public class SubmitCountPlanRequest
    {
        public string PlanId { get; set; } = string.Empty;
        public string SubmitterId { get; set; } = string.Empty;
        public List<SubmitCountPlanItemRequest> Items { get; set; } = new();
    }

    public class SubmitCountPlanItemRequest
    {
        public string MaterialId { get; set; } = string.Empty;
        public string? LocationId { get; set; }
        public int CountQty { get; set; }
        public decimal CountAmount { get; set; }
    }
}

