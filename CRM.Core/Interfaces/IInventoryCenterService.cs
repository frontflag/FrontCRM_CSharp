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
        Task<InventoryFinanceSummaryDto> GetFinanceSummaryAsync(int stagnantDays = 90);

        Task<IEnumerable<WarehouseInfo>> GetWarehousesAsync();
        Task<WarehouseInfo> SaveWarehouseAsync(WarehouseInfo warehouse);

        Task<IEnumerable<PickingTaskSummaryDto>> GetPickingTasksAsync(short? status = null);
        Task<PickingTask> GeneratePickingTaskAsync(GeneratePickingTaskRequest request);
        Task CompletePickingTaskAsync(string taskId);

        Task<IEnumerable<InventoryCountPlan>> GetCountPlansAsync();
        Task<InventoryCountPlan> CreateMonthlyCountPlanAsync(CreateCountPlanRequest request);
        Task SubmitCountPlanAsync(SubmitCountPlanRequest request);
    }

    public class SellOrderLineAvailableQtyDto
    {
        public decimal AvailableQty { get; set; }
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
        public decimal OnHandQty { get; set; }
        public decimal AvailableQty { get; set; }
        public decimal LockedQty { get; set; }
        public decimal InventoryAmount { get; set; }
        /// <summary>库存金额币别（与最近一次采购入库台账关联的采购单一致；缺省 1=RMB）</summary>
        public short Currency { get; set; } = 1;
        public DateTime? LastMoveTime { get; set; }
    }

    public class InventoryMaterialTraceDto
    {
        public DateTime? StockInTime { get; set; }
        public string? StockInCode { get; set; }
        public string? BatchNo { get; set; }
        public decimal Quantity { get; set; }
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
        /// <summary>对应库存行类型 1客单 2备货 3样品；无库存记录时可为空</summary>
        public short? StockType { get; set; }
        public decimal PlanQty { get; set; }
        public decimal PickedQty { get; set; }
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
        public decimal PlanQtyTotal { get; set; }
        /// <summary>本任务拣货明细已拣数量合计</summary>
        public decimal PickedQtyTotal { get; set; }
        /// <summary>本任务涉及的库存类型（去重排序，1客单 2备货 3样品）</summary>
        public List<short> DistinctStockTypes { get; set; } = new();
        public List<PickingTaskLineDto> Items { get; set; } = new();
    }

    public class GeneratePickingTaskRequest
    {
        public string StockOutRequestId { get; set; } = string.Empty;
        public string WarehouseId { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        public List<GeneratePickingTaskItemRequest> Items { get; set; } = new();
    }

    public class GeneratePickingTaskItemRequest
    {
        public string MaterialId { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
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
        public decimal CountQty { get; set; }
        public decimal CountAmount { get; set; }
    }
}

