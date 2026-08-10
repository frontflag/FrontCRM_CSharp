using System;
using System.Collections.Generic;
using CRM.Core.Models.Sales;

namespace CRM.Core.Interfaces
{
    /// <summary>销售订单服务接口</summary>
    public interface ISalesOrderService
    {
        /// <param name="actingUserId">当前登录用户 ID（写入 create_by_user_id）</param>
        Task<SellOrder> CreateAsync(CreateSalesOrderRequest request, string? actingUserId = null);
        Task<SellOrder?> GetByIdAsync(string id);
        Task<IEnumerable<SellOrder>> GetAllAsync();
        /// <param name="actingUserId">当前登录用户 ID（写入 modify_by_user_id）</param>
        Task<SellOrder> UpdateAsync(string id, UpdateSalesOrderRequest request, string? actingUserId = null);
        /// <param name="actingUserId">当前登录用户 ID（写入 log_operation 与明细 deleted_by）</param>
        Task DeleteAsync(string id, string? actingUserId = null);
        /// <param name="auditRemark">审核拒绝时写入的原因（仅 AuditFailed 时有效）</param>
        /// <param name="actingUserId">当前登录用户 ID（写入 modify_by_user_id）</param>
        Task UpdateStatusAsync(string id, SellOrderMainStatus status, string? auditRemark = null, string? actingUserId = null);
        Task<PagedResult<SellOrder>> GetPagedAsync(SalesOrderQueryRequest request);
        /// <summary>根据客户ID获取销售订单列表</summary>
        Task<IEnumerable<SellOrder>> GetByCustomerIdAsync(string customerId);
        /// <summary>获取销售订单关联的采购订单列表</summary>
        Task<IEnumerable<object>> GetRelatedPurchaseOrdersAsync(string sellOrderId);

        /// <summary>分页查询销售订单明细行（含订单头字段），用于明细列表</summary>
        Task<PagedResult<SellOrderItemLineDto>> GetSellOrderItemLinesPagedAsync(SellOrderItemLineQueryRequest request);

        /// <summary>按销售明细 Id 批量加载列表行（含扩展表/门闸等 enrich），供业务详情嵌入场景。</summary>
        Task<List<SellOrderItemLineDto>> GetSellOrderItemLinesByIdsAsync(
            IReadOnlyList<string> sellOrderItemIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 销售明细是否满足「已下采购且采购单主状态已达供应商确认(≥30)」——用于申请出库按钮与门闸。
        /// Key 为销售明细 Id（大小写不敏感字典）。
        /// </summary>
        Task<IReadOnlyDictionary<string, bool>> GetStockOutApplyPurchaseGateBySellLineIdsAsync(IEnumerable<string> sellOrderItemIds);

        /// <summary>
        /// 申请出库采购门闸明细（未满足时含阻塞采购单列表），Key 为销售明细 Id。
        /// </summary>
        Task<IReadOnlyDictionary<string, StockOutApplyPurchaseGateDetailDto>> GetStockOutApplyPurchaseGateDetailsBySellLineIdsAsync(
            IEnumerable<string> sellOrderItemIds);

        /// <summary>按销售订单批量重算明细扩展并返回变更结果。</summary>
        Task<SalesOrderItemExtendRefreshResult> RefreshItemExtendsAsync(string salesOrderId, CancellationToken cancellationToken = default);

        /// <summary>销售订单主表字段变更日志（<c>log_change_fldval</c>，BizType=<see cref="Constants.BusinessLogTypes.SalesOrder"/>）。</summary>
        Task<IReadOnlyList<SalesOrderFieldChangeLogDto>> GetFieldChangeLogsAsync(string sellOrderId);

        /// <summary>已软删除的销售订单明细行（含 <c>is_deleted=true</c>，忽略全局查询过滤器）。</summary>
        Task<IReadOnlyList<SalesOrderDeletedItemLogDto>> GetDeletedOrderItemsAsync(string sellOrderId);
    }

    /// <summary>销售订单字段变更日志行。</summary>
    public class SalesOrderFieldChangeLogDto
    {
        public string Id { get; set; } = string.Empty;
        public string SellOrderId { get; set; } = string.Empty;
        public string? SellOrderCode { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string? FieldLabel { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? ChangedByUserId { get; set; }
        public string? ChangedByUserName { get; set; }
        public DateTime ChangedAt { get; set; }
        /// <summary>主表为「主表」；明细为行号如 SO00001-1。</summary>
        public string? ObjectLabel { get; set; }
    }

    /// <summary>已软删除的销售订单明细。</summary>
    public class SalesOrderDeletedItemLogDto
    {
        public string SellOrderItemId { get; set; } = string.Empty;
        public string? SellOrderItemCode { get; set; }
        public string? PN { get; set; }
        public string? Brand { get; set; }
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public short Currency { get; set; }
        public string? Comment { get; set; }
        public DateTime? CreateTime { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByUserId { get; set; }
        public string? DeletedByUserName { get; set; }
    }

    /// <summary>销售订单明细列表查询</summary>
    public class SellOrderItemLineQueryRequest
    {
        /// <summary>订单创建时间起（含）</summary>
        public DateTime? OrderCreateStart { get; set; }
        /// <summary>订单创建时间止（含当日）</summary>
        public DateTime? OrderCreateEnd { get; set; }
        public string? CustomerName { get; set; }
        public string? SalesUserName { get; set; }
        /// <summary>销售订单/明细号：主单号或明细编号任一模糊匹配（OR）。</summary>
        public string? SellOrderCode { get; set; }
        /// <summary>销售订单明细编号（兼容旧深链；列表 UI 已并入 <see cref="SellOrderCode"/>）。</summary>
        public string? SellOrderItemCode { get; set; }
        public string? Pn { get; set; }
        /// <summary>交易币别筛选：rmb=人民币，foreign=外币（非人民币）。</summary>
        public string? TransactionCurrency { get; set; }
        /// <summary>仅待出库/部分出库明细（与看板待办口径一致）。</summary>
        public bool StockOutPending { get; set; }
        /// <summary>仅待开票明细（invoice_amount_not &gt; 0 或开票进度未完成）。</summary>
        public bool InvoicePending { get; set; }
        /// <summary>销售订单业务员主键（精确匹配）。</summary>
        public string? SalesUserId { get; set; }
        /// <summary>采购员账号/姓名关键词（纯 PO 口径：关联有效采购订单主表采购员模糊匹配）。</summary>
        public string? PurchaseUserAccount { get; set; }
        /// <summary>客户主键（精确匹配）。</summary>
        public string? CustomerId { get; set; }
        /// <summary>客户订单号（模糊匹配 <c>customer_so</c>）。</summary>
        public string? CustomerSo { get; set; }
        /// <summary>客户型号（模糊匹配 <c>customer_pn</c>）。</summary>
        public string? CustomerPn { get; set; }
        /// <summary>关联采购订单明细单号（模糊匹配 <c>purchaseorderitem.purchase_order_item_code</c>）。</summary>
        public string? PurchaseOrderItemCode { get; set; }
        /// <summary>采购进度 0/1/2（扩展表）；多选为 OR，查询串可重复同名参数。</summary>
        public List<short>? PurchaseProgressStatus { get; set; }
        /// <summary>入库进度 0/1/2（扩展表）；多选为 OR。</summary>
        public List<short>? StockInProgressStatus { get; set; }
        /// <summary>出库通知进度 0/1/2（由扩展表通知数量与销售数量推算）；多选为 OR。</summary>
        public List<short>? StockOutNotifyProgressStatus { get; set; }
        /// <summary>出库进度 0/1/2（扩展表）；多选为 OR。</summary>
        public List<short>? StockOutProgressStatus { get; set; }
        /// <summary>收款进度 0/1/2（扩展表）；多选为 OR。</summary>
        public List<short>? ReceiptProgressStatus { get; set; }
        /// <summary>开票进度 0/1/2（扩展表）；多选为 OR。</summary>
        public List<short>? InvoiceProgressStatus { get; set; }
        /// <summary>左栏快捷检索（见 <see cref="SellOrderItemListQuickFilterCodes"/>）；与手动进度筛选互斥。</summary>
        public string? QuickFilter { get; set; }
        /// <summary>
        /// 分析数据集：<c>listFilter</c>（默认，跟列表筛选）或 <c>reportApproved</c>（报表成单）。
        /// </summary>
        public string? AnalyticsDataset { get; set; }
        /// <summary>报表透镜：company / department / personal（仅 reportApproved）。</summary>
        public string? AnalyticsViewLevel { get; set; }
        /// <summary>报表部门透镜主键（仅 department）。</summary>
        public string? AnalyticsDepartmentId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? CurrentUserId { get; set; }
    }

    /// <summary>销售订单明细列表行 DTO</summary>
    public class SellOrderItemLineDto
    {
        public string SellOrderItemId { get; set; } = string.Empty;
        public string SellOrderId { get; set; } = string.Empty;
        public string SellOrderCode { get; set; } = string.Empty;
        /// <summary>销售订单明细编号（完整字符串）</summary>
        public string? SellOrderItemCode { get; set; }
        public short OrderStatus { get; set; }
        public DateTime OrderCreateTime { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        /// <summary>客户编号（主单快照 <c>sell_order.customer_code</c>；空时可由客户主数据补齐）。</summary>
        public string? CustomerCode { get; set; }
        /// <summary>客户英文全称（<c>CustomerInfo.EnglishOfficialName</c>）。</summary>
        public string? CustomerEnglishName { get; set; }
        public string? SalesUserName { get; set; }
        /// <summary>关联有效采购订单采购员登录账号（按 PO 创建时间升序、去重后以中文逗号拼接；无 PO 为空）。</summary>
        public string? PurchaseUserAccountDisplay { get; set; }
        public string? PN { get; set; }
        public string? Brand { get; set; }
        /// <summary>客户订单号（库列 <c>customer_so</c>）。</summary>
        public string? CustomerSo { get; set; }
        /// <summary>客户型号（库列 <c>customer_pn</c>）。</summary>
        public string? CustomerPn { get; set; }
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public decimal LineTotal { get; set; }
        public short Currency { get; set; }
        /// <summary>折算美金单价：来自明细 <c>ConvertPrice</c>；USD 币别时含 0（不回填单价）</summary>
        public decimal? UsdUnitPrice { get; set; }
        /// <summary>折算美金行金额：<c>Qty × UsdUnitPrice</c></summary>
        public decimal? UsdLineTotal { get; set; }
        public short ItemStatus { get; set; }

        /// <summary>扩展表：采购进度 0/1/2</summary>
        public short PurchaseProgressStatus { get; set; }

        /// <summary>扩展表：入库进度 0/1/2</summary>
        public short StockInProgressStatus { get; set; }

        /// <summary>扩展表：出库进度 0/1/2</summary>
        public short StockOutProgressStatus { get; set; }

        /// <summary>扩展表：出库通知进度 0=未通知 1=部分通知 2=通知完成</summary>
        public short StockOutNotifyProgressStatus { get; set; }

        /// <summary>扩展表：收款进度 0/1/2</summary>
        public short ReceiptProgressStatus { get; set; }

        /// <summary>扩展表：销项开票进度 0/1/2</summary>
        public short InvoiceProgressStatus { get; set; }

        /// <summary>
        /// 已存在关联采购明细，且关联采购单主表状态 ≥ 已确认(30)（供应商确认及之后）。
        /// </summary>
        public bool StockOutApplyPurchaseGateOk { get; set; }

        /// <summary>申请出库采购门闸明细（列表/详情提示用）。</summary>
        public StockOutApplyPurchaseGateDetailDto? StockOutApplyPurchaseGateDetail { get; set; }

        /// <summary>扩展表：同 PN+品牌备货库存可用量之和（<c>PurchasedStock_AvailableQty</c>）。</summary>
        public int PurchasedStockAvailableQty { get; set; }

        /// <summary>
        /// 剩余可采数量（订单量 − 已下采购 − 进行中采购申请）；与 <see cref="IPurchaseRequisitionService.GetSellOrderLineOptionsAsync"/> 口径一致。
        /// 列表计算失败时为 null，前端不因缺字段误禁用。
        /// </summary>
        public decimal? PurchaseRemainingQty { get; set; }

        /// <summary>扩展表：预计销售利润 USD（销售折 USD − 已确认采购折 USD）</summary>
        public decimal SalesProfitExpected { get; set; }

        /// <summary>扩展表：出库利润（业务 USD）</summary>
        public decimal ProfitOutBizUsd { get; set; }

        /// <summary>扩展表：出库利润率（出库销售收入 USD / 出库成本 USD）；成本为 0 时为 null（展示 —）</summary>
        public decimal? ProfitOutRateBiz { get; set; }
    }

    /// <summary>申请出库采购门闸：阻塞中的关联采购单。</summary>
    public class StockOutApplyPurchaseGateBlockingPoDto
    {
        public string PurchaseOrderId { get; set; } = string.Empty;
        public string? OrderCode { get; set; }
        public short Status { get; set; }
        /// <summary>关联采购单主表记录不存在时为 true。</summary>
        public bool Missing { get; set; }
    }

    /// <summary>申请出库采购门闸明细。</summary>
    public class StockOutApplyPurchaseGateDetailDto
    {
        public bool Ok { get; set; }
        public bool HasPoItems { get; set; }
        public IReadOnlyList<StockOutApplyPurchaseGateBlockingPoDto> BlockingPurchaseOrders { get; set; }
            = Array.Empty<StockOutApplyPurchaseGateBlockingPoDto>();
    }

    public class CreateSalesOrderRequest
    {
        /// <summary>销售单号</summary>
        public string SellOrderCode { get; set; } = string.Empty;
        /// <summary>客户ID</summary>
        public string CustomerId { get; set; } = string.Empty;
        /// <summary>客户名称</summary>
        public string? CustomerName { get; set; }
        /// <summary>业务员ID</summary>
        public string? SalesUserId { get; set; }
        /// <summary>业务员名称</summary>
        public string? SalesUserName { get; set; }
        /// <summary>销售助理用户 ID</summary>
        public string? Assistor { get; set; }
        /// <summary>订单类型 1=客单采购 2=备货采购 3=样品采购</summary>
        public short Type { get; set; } = 1;
        /// <summary>币别 1=RMB 2=USD 3=EUR</summary>
        public short Currency { get; set; } = 1;
        /// <summary>交货日期</summary>
        public DateTime? DeliveryDate { get; set; }
        /// <summary>送货地址</summary>
        public string? DeliveryAddress { get; set; }

        /// <summary>产品类型（现货/期货等）。</summary>
        public string? ProductKind { get; set; }

        /// <summary>客户联系人。</summary>
        public string? CustomerContactName { get; set; }

        /// <summary>发票信息。</summary>
        public string? InvoiceInfo { get; set; }

        /// <summary>账期/付款条款展示文案。</summary>
        public string? PaymentTermsText { get; set; }

        /// <summary>主表 <c>comment</c>：自由备注；若为多行历史前缀格式则由服务端解析并合并进结构化列。</summary>
        public string? Comment { get; set; }

        /// <summary>明细行</summary>
        public List<CreateSalesOrderItemRequest> Items { get; set; } = new();
    }

    public class CreateSalesOrderItemRequest
    {
        /// <summary>编辑保存时传入已有明细主键（SellOrderItemId）；新建行省略。</summary>
        public string? Id { get; set; }

        /// <summary>报价ID(来源)</summary>
        public string? QuoteId { get; set; }
        /// <summary>商品/物料ID</summary>
        public string? ProductId { get; set; }
        /// <summary>物料型号(PN)</summary>
        public string? PN { get; set; }
        /// <summary>品牌</summary>
        public string? Brand { get; set; }
        /// <summary>客户订单号码（原 <c>customer_pn_no</c>，现库列 <c>customer_so</c>）。</summary>
        public string? CustomerSo { get; set; }

        /// <summary>客户物料型号（库列 <c>customer_pn</c>）。</summary>
        public string? CustomerPn { get; set; }

        /// <summary>客户品牌（库列 <c>customer_brand</c>）。</summary>
        public string? CustomerBrand { get; set; }

        /// <summary>销售数量</summary>
        public decimal Qty { get; set; }
        /// <summary>销售单价</summary>
        public decimal Price { get; set; }
        /// <summary>币别</summary>
        public short Currency { get; set; } = 1;
        /// <summary>生产日期要求</summary>
        public string? DateCode { get; set; }
        /// <summary>交货日期</summary>
        public DateTime? DeliveryDate { get; set; }
        /// <summary>备注</summary>
        public string? Comment { get; set; }
    }

    public class UpdateSalesOrderRequest
    {
        /// <summary>更换客户时须同时提交；服务端按客户主数据同步 <see cref="CustomerName"/> 快照。</summary>
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        /// <summary>业务员用户 ID</summary>
        public string? SalesUserId { get; set; }
        public string? SalesUserName { get; set; }
        /// <summary>销售助理用户 ID</summary>
        public string? Assistor { get; set; }
        public short? Type { get; set; }
        public short? Currency { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? DeliveryAddress { get; set; }

        public string? ProductKind { get; set; }
        public string? CustomerContactName { get; set; }
        public string? InvoiceInfo { get; set; }
        public string? PaymentTermsText { get; set; }

        /// <summary>主表 <c>comment</c>：自由备注；历史多行前缀格式可解析合并进结构化列。</summary>
        public string? Comment { get; set; }

        public List<CreateSalesOrderItemRequest>? Items { get; set; }
    }

    public class SalesOrderQueryRequest
    {
        /// <summary>兼容旧版：单关键字匹配销售单号或客户名称（OR）。若同时传 <see cref="SellOrderCodeFilter"/> / <see cref="CustomerNameFilter"/> 则忽略本字段。</summary>
        public string? Keyword { get; set; }

        /// <summary>销售单号包含（与 <see cref="CustomerNameFilter"/>、状态等为 AND）。</summary>
        public string? SellOrderCodeFilter { get; set; }

        /// <summary>客户名称包含。</summary>
        public string? CustomerNameFilter { get; set; }

        /// <summary>业务员姓名包含。</summary>
        public string? SalesUserNameFilter { get; set; }

        /// <summary>备注（主表 Comment）包含。</summary>
        public string? CommentFilter { get; set; }

        /// <summary>主状态多选（空/null 表示不限）。取值见 <see cref="Models.Sales.SellOrderMainStatus"/>。</summary>
        public List<short>? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? CurrentUserId { get; set; }
    }
}
