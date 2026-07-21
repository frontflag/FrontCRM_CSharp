namespace CRM.Core.Constants;

/// <summary>
/// 埋点分析「解释」文案（后端常量，开发维护）。未命中返回 null，前端显示「—」。
/// </summary>
public static class TelemetryCatalog
{
    private static readonly Dictionary<string, string> Pages =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Login"] = "登录",
            ["Register"] = "注册",
            ["ReleaseNotes"] = "版本更新日志",
            ["Dashboard"] = "工作台",
            ["SalesAnalytics"] = "销售统计",
            ["PurchaseAnalytics"] = "采购统计",
            ["LogisticsAnalytics"] = "物流统计",
            ["FinanceAnalytics"] = "财务统计",
            ["CustomerHome"] = "客户首页",
            ["CustomerList"] = "客户列表",
            ["CustomerCreate"] = "新建客户",
            ["CustomerRecycleBin"] = "客户回收站",
            ["CustomerBlacklist"] = "客户黑名单",
            ["CustomerFreezeManagement"] = "客户冻结管理",
            ["CustomerDetail"] = "客户详情",
            ["CustomerEdit"] = "编辑客户",
            ["CustomerWarrantyReport"] = "客户保函报告",
            ["CustomerContactCreate"] = "新建客户联系人",
            ["CustomerContactEdit"] = "编辑客户联系人",
            ["CustomerAddressCreate"] = "新建客户地址",
            ["RFQHome"] = "需求首页",
            ["RFQList"] = "需求列表",
            ["PnList"] = "料号列表",
            ["RFQItemList"] = "需求明细列表",
            ["RFQCreate"] = "新建需求",
            ["RFQDetail"] = "需求详情",
            ["RFQEdit"] = "编辑需求",
            ["BOMList"] = "BOM 列表",
            ["BOMCreate"] = "新建 BOM",
            ["BOMDetail"] = "BOM 详情",
            ["VendorHome"] = "供应商首页",
            ["VendorList"] = "供应商列表",
            ["VendorCreate"] = "新建供应商",
            ["VendorRecycleBin"] = "供应商回收站",
            ["VendorBlacklist"] = "供应商黑名单",
            ["VendorFreezeManagement"] = "供应商冻结管理",
            ["VendorWarrantyReport"] = "供应商保函报告",
            ["VendorDetail"] = "供应商详情",
            ["VendorEdit"] = "编辑供应商",
            ["VendorContactCreate"] = "新建供应商联系人",
            ["VendorContactEdit"] = "编辑供应商联系人",
            ["VendorAddressCreate"] = "新建供应商地址",
            ["InventoryList"] = "库存列表",
            ["WarehouseManage"] = "仓库管理",
            ["InventoryStockItemList"] = "库存物料明细",
            ["InventoryStockDetail"] = "库存详情",
            ["InventoryTrace"] = "库存追溯",
            ["StockInList"] = "入库单列表",
            ["BatchReconciliation"] = "批次对账",
            ["StockInCreate"] = "新建入库单",
            ["StockInDetail"] = "入库单详情",
            ["CustomsBrokerList"] = "报关行",
            ["CustomsPendlistList"] = "报关待办",
            ["CustomsDeclarationList"] = "报关单列表",
            ["CustomsDeclarationDetail"] = "报关单详情",
            ["CustomsDeclarationItemList"] = "报关明细",
            ["StockTransferList"] = "移库列表",
            ["StockOutList"] = "出库单列表",
            ["StockOutItemList"] = "出库明细",
            ["StockOutCreate"] = "新建出库单",
            ["PickCreate"] = "拣货创建",
            ["StockOutInvoiceReport"] = "出库发票报表",
            ["PackingInvoiceReport"] = "装箱发票报表",
            ["PackingReport"] = "装箱报表",
            ["StockOutDetail"] = "出库单详情",
            ["StockOutNotifyDetail"] = "出库通知详情",
            ["InventoryStockOutNotifyList"] = "出库通知列表",
            ["PackingList"] = "装箱单列表",
            ["PackingCreate"] = "新建装箱单",
            ["PackingDetail"] = "装箱单详情",
            ["PackingItemList"] = "装箱明细",
            ["PickingSlipList"] = "拣货单列表",
            ["PickingSlipDetail"] = "拣货单详情",
            ["InventoryTransfersManual"] = "手工移库",
            ["InventoryCheck"] = "盘点",
            ["QuoteList"] = "报价列表",
            ["QuoteCreate"] = "新建报价",
            ["QuoteEdit"] = "编辑报价",
            ["QuoteDetail"] = "报价详情",
            ["PurchaseOrderList"] = "采购订单列表",
            ["PurchaseRequisitionList"] = "采购申请列表",
            ["PurchaseRequisitionCreate"] = "新建采购申请",
            ["PurchaseRequisitionDetail"] = "采购申请详情",
            ["PurchaseOrderCreate"] = "新建采购订单",
            ["PurchaseOrderReport"] = "采购订单报表",
            ["PurchaseOrderEdit"] = "编辑采购订单",
            ["PurchaseOrderDetail"] = "采购订单详情",
            ["SalesOrderList"] = "销售订单列表",
            ["SalesOrderItemList"] = "销售订单明细",
            ["StockOutNotifyList"] = "出库通知列表",
            ["PurchaseOrderItemList"] = "采购订单明细",
            ["ArrivalNoticeList"] = "到货通知列表",
            ["QcList"] = "质检列表",
            ["QcCreate"] = "新建质检",
            ["SalesOrderCreate"] = "新建销售订单",
            ["SalesOrderReport"] = "销售订单报表",
            ["SalesOrderEdit"] = "编辑销售订单",
            ["SalesOrderDetail"] = "销售订单详情",
            ["Settings"] = "系统设置",
            ["Profile"] = "个人资料",
            ["WechatBinding"] = "微信绑定",
            ["DraftList"] = "草稿箱",
            ["PendingApprovals"] = "待审批",
            ["BrandList"] = "品牌管理",
            ["UserList"] = "用户管理",
            ["UserCreate"] = "新建用户",
            ["UserEdit"] = "编辑用户",
            ["RoleList"] = "角色管理",
            ["RoleCreate"] = "新建角色",
            ["RoleEdit"] = "编辑角色",
            ["PermissionList"] = "权限管理",
            ["PermissionCreate"] = "新建权限",
            ["PermissionEdit"] = "编辑权限",
            ["UserConfig"] = "用户配置",
            ["DepartmentList"] = "部门管理",
            ["DepartmentCreate"] = "新建部门",
            ["DepartmentEdit"] = "编辑部门",
            ["DepartmentDetail"] = "部门详情",
            ["CompanyInfo"] = "公司信息",
            ["DictItemList"] = "数据字典",
            ["PurchaseAssigneeCount"] = "采购参数-分配人数",
            ["PurchaseDemandProtection"] = "采购参数-需求保护",
            ["PurchaseQuoterPool"] = "采购参数-报价人池",
            ["PurchaseDefaultAssignMethod"] = "采购参数-默认分配方式",
            ["PurchaseRefreshVendor"] = "采购参数-刷新供应商",
            ["SalesRefreshCustomer"] = "销售参数-刷新客户",
            ["FinanceExchangeRates"] = "财务参数-汇率",
            ["FinancePurchaseCostParams"] = "财务参数-采购成本",
            ["FinancePaymentBanks"] = "财务参数-付款银行",
            ["LoginLogList"] = "登录日志",
            ["OperationLogList"] = "操作日志",
            ["AiConfig"] = "AI 配置",
            ["UserFeedbackList"] = "用户反馈",
            ["SystemErrorList"] = "系统错误",
            ["TelemetryAnalytics"] = "埋点分析",
            ["FinancePaymentList"] = "付款管理",
            ["FinancePaymentDetail"] = "付款单详情",
            ["FinanceReceiptList"] = "收款管理",
            ["FinanceReceiptDetail"] = "收款单详情",
            ["FinanceReceivableList"] = "应收管理",
            ["FinanceReceivableDetail"] = "应收详情",
            ["FinanceCustomerAdvanceList"] = "客户预收款",
            ["FinanceFreightForwarderPayableList"] = "货代应付",
            ["FinanceFreightForwarderPayableDetail"] = "货代应付详情",
            ["FreightForwarderCompanyManage"] = "货代公司",
            ["FinanceReceiptWriteOff"] = "收款核销",
            ["FinanceReceiptWriteOffLedger"] = "核销台账",
            ["FinancePurchaseInvoiceList"] = "采购发票",
            ["FinancePurchaseInvoiceDetail"] = "采购发票详情",
            ["FinanceSellInvoiceList"] = "销售发票",
            ["FinanceSellInvoiceDetail"] = "销售发票详情",
            ["FinanceStockAccumulatedList"] = "库存滚存",
            ["FinanceStockAccumulatedItemList"] = "库存滚存明细",
            ["FinanceCustomerAccumulatedList"] = "客户滚存",
            ["FinanceCustomerAccumulatedItemList"] = "客户滚存明细",
            ["FinanceVendorAccumulatedList"] = "供应商滚存",
            ["FinanceVendorAccumulatedItemList"] = "供应商滚存明细",
            ["DocumentDemo"] = "文档演示",
            ["DebugInternalVersionLog"] = "内部版本日志",
            ["DebugData"] = "调试数据",
            ["DebugTools"] = "调试工具",
            ["DebugAi"] = "调试 AI",
            ["DebugMaterialIntel"] = "调试物料情报",
            ["DebugCustomerIntel"] = "调试客户情报",
            ["DebugList"] = "调试入口",
            // 路径型 pageKey（无 name 或侧栏捕获时）
            ["/dashboard"] = "工作台",
            ["/ops/user-feedback"] = "用户反馈",
            ["/ops/system-errors"] = "系统错误",
            ["/ops/telemetry-analytics"] = "埋点分析",
            ["/customerlist"] = "客户列表",
            ["/customers/:id"] = "客户详情",
            ["/rfq-items"] = "需求明细列表",
            ["/rfqs/:id"] = "需求详情",
        };

    private static readonly Dictionary<string, string> Actions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["menu.ops.user_feedback"] = "菜单：用户反馈",
            ["menu.ops.system_errors"] = "菜单：系统错误",
            ["menu.ops.telemetry_analytics"] = "菜单：埋点分析",
            ["menu.查询"] = "菜单点击：查询",
            ["menu.重置"] = "菜单点击：重置",
        };

    /// <summary>常见按钮文案 → 解释（用于自动生成的 btn.*.文案）。</summary>
    private static readonly Dictionary<string, string> CommonActionLabels =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["查询"] = "点击：查询",
            ["重置"] = "点击：重置",
            ["保存"] = "点击：保存",
            ["提交"] = "点击：提交",
            ["新建"] = "点击：新建",
            ["新增"] = "点击：新增",
            ["删除"] = "点击：删除",
            ["编辑"] = "点击：编辑",
            ["取消"] = "点击：取消",
            ["确认"] = "点击：确认",
            ["确定"] = "点击：确定",
            ["导出"] = "点击：导出",
            ["导入"] = "点击：导入",
            ["刷新"] = "点击：刷新",
            ["返回"] = "点击：返回",
            ["搜索"] = "点击：搜索",
            ["审核"] = "点击：审核",
            ["通过"] = "点击：通过",
            ["驳回"] = "点击：驳回",
            ["打印"] = "点击：打印",
            ["下载"] = "点击：下载",
            ["上传"] = "点击：上传",
            ["关闭"] = "点击：关闭",
            ["详情"] = "点击：详情",
            ["复制"] = "点击：复制",
            ["标记已处理"] = "点击：标记已处理",
            ["query"] = "点击：查询",
            ["reset"] = "点击：重置",
            ["save"] = "点击：保存",
            ["submit"] = "点击：提交",
            ["create"] = "点击：新建",
            ["delete"] = "点击：删除",
            ["edit"] = "点击：编辑",
            ["cancel"] = "点击：取消",
            ["export"] = "点击：导出",
        };

    private static readonly Dictionary<string, string> Apis =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["GET /api/v1/rfqs"] = "需求列表/查询",
            ["GET /api/v1/rfqs/:id"] = "需求单详情",
            ["POST /api/v1/rfqs"] = "创建需求单",
            ["PUT /api/v1/rfqs/:id"] = "更新需求单",
            ["GET /api/v1/rfq-items"] = "需求明细查询",
            ["POST /api/v1/rfq-items/search"] = "需求明细搜索",
            ["GET /api/v1/customers"] = "客户列表/查询",
            ["GET /api/v1/customers/:id"] = "客户详情",
            ["POST /api/v1/customers"] = "创建客户",
            ["PUT /api/v1/customers/:id"] = "更新客户",
            ["GET /api/v1/customers/:id/intel-reports"] = "客户情报报告",
            ["GET /api/v1/vendors"] = "供应商列表/查询",
            ["GET /api/v1/vendors/:id"] = "供应商详情",
            ["GET /api/v1/quotes"] = "报价列表/查询",
            ["GET /api/v1/quotes/:id"] = "报价详情",
            ["GET /api/v1/quotes/aggregate/quote-counts-by-rfq-item-ids"] = "按需求明细汇总报价数",
            ["GET /api/v1/sales-orders"] = "销售订单列表",
            ["GET /api/v1/sales-orders/:id"] = "销售订单详情",
            ["GET /api/v1/purchase-orders"] = "采购订单列表",
            ["GET /api/v1/purchase-orders/:id"] = "采购订单详情",
            ["GET /api/v1/favorites"] = "收藏夹",
            ["POST /api/v1/favorites"] = "添加收藏",
            ["DELETE /api/v1/favorites/:id"] = "取消收藏",
            ["GET /api/v1/system/display"] = "显示时区/展示设置",
            ["GET /api/v1/debug/simulation-banner"] = "调试：模拟登录横幅",
            ["GET /api/v1/auth/me"] = "当前登录用户",
            ["GET /api/v1/auth/permission-summary"] = "权限摘要",
            ["GET /api/v1/auth/sales-users-tree"] = "销售人员树（选择器）",
            ["GET /api/v1/auth/purchase-users"] = "采购人员列表（选择器）",
            ["POST /api/v1/auth/login"] = "登录",
            ["POST /api/v1/auth/logout"] = "登出",
            ["GET /api/v1/dictionaries"] = "数据字典",
            ["GET /api/v1/biz/brands"] = "品牌列表",
            ["GET /api/v1/stock-ins"] = "入库单列表",
            ["GET /api/v1/stock-ins/:id"] = "入库单详情",
            ["GET /api/v1/stock-outs"] = "出库单列表",
            ["GET /api/v1/stock-outs/:id"] = "出库单详情",
            ["GET /api/v1/packings"] = "装箱单列表",
            ["GET /api/v1/packings/:id"] = "装箱单详情",
            ["GET /api/v1/error-logs"] = "系统错误列表",
            ["GET /api/v1/error-logs/:id"] = "系统错误详情",
            ["GET /api/v1/user-feedback"] = "用户反馈列表",
            ["GET /api/v1/user-feedback/:id"] = "用户反馈详情",
            ["GET /api/v1/telemetry/analytics/top-pages"] = "埋点：高频页面",
            ["GET /api/v1/telemetry/analytics/top-actions"] = "埋点：高频操作",
            ["GET /api/v1/telemetry/analytics/top-apis"] = "埋点：API 耗时",
            ["POST /api/v1/telemetry/events"] = "埋点事件上报",
            ["GET /api/v1/login-logs"] = "登录日志",
            ["GET /api/v1/operation-logs"] = "操作日志",
            ["GET /api/v1/finance/payments"] = "付款单列表",
            ["GET /api/v1/finance/receipts"] = "收款单列表",
            ["GET /api/v1/ai/assistant/sessions"] = "AI 助手会话",
            ["POST /api/v1/ai/assistant/sessions"] = "开启 AI 助手会话",
            ["POST /api/v1/ai/assistant/sessions/:id/messages"] = "AI 助手发消息",
        };

    public static string? DescribePage(string? pageKey)
    {
        if (string.IsNullOrWhiteSpace(pageKey)) return null;
        var key = pageKey.Trim();
        if (Pages.TryGetValue(key, out var d)) return d;
        // 路径末尾去查询串
        var q = key.IndexOf('?', StringComparison.Ordinal);
        if (q >= 0) key = key[..q];
        return Pages.TryGetValue(key, out d) ? d : null;
    }

    public static string? DescribeAction(string? actionId, string? pageKey = null)
    {
        if (string.IsNullOrWhiteSpace(actionId)) return null;
        var id = actionId.Trim();
        if (Actions.TryGetValue(id, out var d)) return d;

        // menu.xxx / btn.page.label → 取最后一段常见文案
        var parts = id.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length > 0)
        {
            var last = parts[^1];
            if (CommonActionLabels.TryGetValue(last, out var labelDesc))
            {
                if (parts[0].Equals("menu", StringComparison.OrdinalIgnoreCase))
                    return "菜单：" + labelDesc.Replace("点击：", "", StringComparison.Ordinal);
                return labelDesc;
            }

            if (parts[0].Equals("menu", StringComparison.OrdinalIgnoreCase) && parts.Length >= 2)
                return "菜单：" + last;
        }

        _ = pageKey;
        return null;
    }

    public static string? DescribeApi(string? method, string? pathTemplate)
    {
        if (string.IsNullOrWhiteSpace(pathTemplate)) return null;
        var m = string.IsNullOrWhiteSpace(method) ? "GET" : method.Trim().ToUpperInvariant();
        var path = pathTemplate.Trim();
        if (!path.StartsWith('/')) path = "/" + path;
        var key = $"{m} {path}";
        return Apis.TryGetValue(key, out var d) ? d : null;
    }
}
