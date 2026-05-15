using System.Text;
using System.Text.RegularExpressions;
using CRM.API.Models.DTOs;
using CRM.API.Services;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Quote;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Sales;
using CRM.Core.Models.System;
using CRM.Core.Models.Vendor;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/debug")]
    public class DebugController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IStockInService _stockInService;
        private readonly IFinanceExchangeRateService _financeExchangeRateService;
        private readonly ISellOrderExtendLineSeqService _sellLineSeq;
        private readonly IPurchaseOrderExtendLineSeqService _poLineSeq;
        private readonly IStockInExtendLineSeqService _stockInLineSeq;
        private const short PoStatusInProgress = 50;
        private const short PoStatusCompleted = 100;
        private const short PoStatusAuditFailed = -1;
        private const short PoStatusCancelled = -2;
        private const short PoStatusNew = 1;
        private const short PoStatusPendingAudit = 2;
        private const short PoStatusApproved = 10;
        private const short PoStatusPendingConfirm = 20;
        private const short PoStatusConfirmed = 30;

        public DebugController(
            ApplicationDbContext context,
            IConfiguration configuration,
            IStockInService stockInService,
            IFinanceExchangeRateService financeExchangeRateService,
            ISellOrderExtendLineSeqService sellLineSeq,
            IPurchaseOrderExtendLineSeqService poLineSeq,
            IStockInExtendLineSeqService stockInLineSeq)
        {
            _context = context;
            _configuration = configuration;
            _stockInService = stockInService;
            _financeExchangeRateService = financeExchangeRateService;
            _sellLineSeq = sellLineSeq;
            _poLineSeq = poLineSeq;
            _stockInLineSeq = stockInLineSeq;
        }

        public class DebugItemDto
        {
            public string Name { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }

        /// <summary>
        /// Debug 页数据：数据库连接（敏感字段按位脱敏）、debug 表记录。版本号由前端 package.json 注入。
        /// </summary>
        public class DebugPageDto
        {
            /// <summary>供界面展示的数据库连接串（Host/Database/Username/Password 的值：从第 1 位起奇数位保留、偶数位为 *）</summary>
            public string DatabaseConnectionDisplay { get; set; } = string.Empty;
            public List<DebugItemDto> Items { get; set; } = new();
        }

        public class SimulateBusinessChainRequest
        {
            public string BusinessNode { get; set; } = string.Empty;
            public short Status { get; set; }
            /// <summary>数据起源：ignore（忽略）| customer | vendor | salesorder | purchaseorder</summary>
            public string? DataOrigin { get; set; }
            /// <summary>业务编号：客户编号 / 供应商编码 / 销售单号 / 采购单号</summary>
            public string? OriginReferenceCode { get; set; }
        }

        public class SimulateBusinessChainResponse
        {
            public string ChainNo { get; set; } = string.Empty;
            public string BusinessNode { get; set; } = string.Empty;
            public short TargetStatus { get; set; }
            public List<string> CreatedNodes { get; set; } = new();
            public bool StockInPostedThroughRealFlow { get; set; }
        }

        public class RefreshStockLedgerResultDto
        {
            public int StockOutUpdated { get; set; }
            public int StockOutReverseUpdated { get; set; }
            public int CurrencyDefaulted { get; set; }
        }

        public class RefreshPurchaseOrderMainStatusResultDto
        {
            public int TotalOrders { get; set; }
            public int ChangedOrders { get; set; }
            public List<string> ChangedOrderCodes { get; set; } = new();
            public int SkippedTerminalOrders { get; set; }
        }

        public class RefreshSellOrderCommentSplitResultDto
        {
            /// <summary>执行前 <c>comment</c> 非空的行数（含软删）。</summary>
            public int TotalWithComment { get; set; }

            /// <summary>已拆分并清空 <c>comment</c> 的行数。</summary>
            public int RowsProcessed { get; set; }
        }

        public class RefreshSellOrderItemCustomerPnFromCommentResultDto
        {
            /// <summary>执行前 <c>comment</c> 非空的明细行数（含软删）。</summary>
            public int TotalWithComment { get; set; }

            /// <summary>已向 <c>customer_pn</c> 写入解析值的行数（仅原 <c>customer_pn</c> 为空的行）。</summary>
            public int RowsFilled { get; set; }
        }

        /// <summary>Debug：将历史打包在 <c>financepayment.Remark</c> 中的请款信息拆回结构化列并清空 Remark。</summary>
        public class RefreshFinancePaymentLegacyRemarkResultDto
        {
            public int TotalPaymentsRemarkNonEmpty { get; set; }
            /// <summary>命中「供应商银行 + 费用(」形态的主表条数。</summary>
            public int LegacyPackedCandidates { get; set; }
            /// <summary>解析费用段成功并已写库的主表条数。</summary>
            public int ParsedAndApplied { get; set; }
            /// <summary>形态像打包备注但费用段无法解析的条数。</summary>
            public int SkippedMalformed { get; set; }
            public int ItemsLineRemarkUpdated { get; set; }
            /// <summary>主表 <c>FinancePaymentBankId</c> 原非表内主键，经 <c>BankName</c> 匹配后写回真实 Id 的条数（含仅走本段修正的历史行）。</summary>
            public int BankIdsResolvedFromName { get; set; }
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<DebugPageDto>>> GetAll()
        {
            var rawCs = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(rawCs))
            {
                // 与运行时 DbContext 实际使用的一致（例如仅通过环境变量注入时）
                rawCs = _context.Database.GetConnectionString();
            }

            // Debug 页可直接访问：即使数据库账号没有 debug 表权限，也不应整体 500。
            // 这里将 debug 表读取降级为“尽力而为”，失败则 Items 为空。
            List<DebugItemDto> items = new();
            try
            {
                items = await _context.DebugRecords
                    .OrderBy(x => x.Name)
                    .Select(x => new DebugItemDto
                    {
                        Name = x.Name,
                        Value = x.Value
                    })
                    .ToListAsync();
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InsufficientPrivilege)
            {
                // 42501: permission denied
                items = new List<DebugItemDto>();
            }
            catch (Exception)
            {
                // 其它异常也不阻断页面（例如表不存在/连接串可读但库不可达等），保持降级。
                items = new List<DebugItemDto>();
            }

            var page = new DebugPageDto
            {
                DatabaseConnectionDisplay = MaskConnectionStringForDebugDisplay(rawCs),
                Items = items
            };

            return Ok(ApiResponse<DebugPageDto>.Ok(page));
        }

        /// <summary>
        /// Debug: 生成指定业务节点/状态的模拟数据，并自动补齐此前链条数据。
        /// 支持节点：rfq / quote / salesorder / purchaserequisition / purchaseorder / stockinnotify / qc / stockin / stockoutrequest
        /// </summary>
        [Authorize]
        [HttpPost("simulate-business-chain")]
        public async Task<ActionResult<ApiResponse<SimulateBusinessChainResponse>>> SimulateBusinessChain(
            [FromBody] SimulateBusinessChainRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.BusinessNode))
            {
                return BadRequest(ApiResponse<SimulateBusinessChainResponse>.Fail("businessNode 不能为空"));
            }

            var normalizedNode = request.BusinessNode.Trim().ToLowerInvariant();
            var nodeOrder = new[]
            {
                "rfq",
                "quote",
                "salesorder",
                "purchaserequisition",
                "purchaseorder",
                "stockinnotify",
                "qc",
                "stockin",
                "stockoutrequest"
            };
            var targetIdx = Array.IndexOf(nodeOrder, normalizedNode);
            if (targetIdx < 0)
            {
                return BadRequest(ApiResponse<SimulateBusinessChainResponse>.Fail("不支持的 businessNode"));
            }

            var chainNo = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var codeSuffix = DateTime.UtcNow.ToString("yyMMdd") + Random.Shared.Next(0, 10000).ToString("D4");
            var materialId = Guid.NewGuid().ToString();
            var customerId = Guid.NewGuid().ToString();
            var vendorId = Guid.NewGuid().ToString();
            var salesUserId = Guid.NewGuid().ToString();
            var purchaseUserId = Guid.NewGuid().ToString();
            var requestUserId = Guid.NewGuid().ToString();
            var warehouseId = (await _context.Warehouses.AsNoTracking().OrderBy(x => x.WarehouseCode).Select(x => x.Id).FirstOrDefaultAsync())
                              ?? Guid.NewGuid().ToString();

            var createdNodes = new List<string>();
            var now = DateTime.UtcNow;
            var stockInPostedThroughRealFlow = false;

            RFQ? rfq = null;
            RFQItem? rfqItem = null;
            Quote? quote = null;
            QuoteItem? quoteItem = null;
            SellOrder? salesOrder = null;
            SellOrderItem? salesOrderItem = null;
            PurchaseRequisition? pr = null;
            PurchaseOrder? po = null;
            PurchaseOrderItem? poItem = null;
            StockInNotify? notify = null;
            QCInfo? qc = null;
            QCItem? qcItem = null;
            StockIn? stockIn = null;
            StockInItem? stockInItem = null;
            StockOutRequest? stockOutRequest = null;

            var dataOrigin = (request.DataOrigin ?? string.Empty).Trim().ToLowerInvariant();
            var originCode = (request.OriginReferenceCode ?? string.Empty).Trim();
            if (dataOrigin is not ("" or "ignore"))
            {
                if (string.IsNullOrWhiteSpace(originCode))
                {
                    return BadRequest(ApiResponse<SimulateBusinessChainResponse>.Fail("已选择数据起源时请输入业务编号", 400));
                }
            }

            var firstCreateIdx = 0;
            var customerDisplayName = "DEBUG-CUSTOMER";
            var vendorDisplayName = "DEBUG-VENDOR";
            var vendorDisplayCode = "DBGV";

            if (dataOrigin is "customer")
            {
                var cust = await _context.Customers.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CustomerCode == originCode);
                if (cust == null)
                {
                    return BadRequest(ApiResponse<SimulateBusinessChainResponse>.Fail($"未找到客户编号: {originCode}", 400));
                }

                customerId = cust.Id;
                customerDisplayName = cust.OfficialName ?? cust.CustomerCode ?? originCode;
            }
            else if (dataOrigin is "vendor")
            {
                var vend = await _context.Vendors.AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Code == originCode);
                if (vend == null)
                {
                    return BadRequest(ApiResponse<SimulateBusinessChainResponse>.Fail($"未找到供应商编号: {originCode}", 400));
                }

                vendorId = vend.Id;
                vendorDisplayName = vend.OfficialName ?? vend.NickName ?? vend.Code ?? originCode;
                vendorDisplayCode = vend.Code ?? originCode;
            }
            else if (dataOrigin is "salesorder")
            {
                if (targetIdx < 2)
                {
                    return BadRequest(ApiResponse<SimulateBusinessChainResponse>.Fail(
                        "数据起源为销售订单时，业务节点请选择「销售订单」或之后环节", 400));
                }

                salesOrder = await _context.SellOrders.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SellOrderCode == originCode);
                if (salesOrder == null)
                {
                    return BadRequest(ApiResponse<SimulateBusinessChainResponse>.Fail($"未找到销售订单编号: {originCode}", 400));
                }

                salesOrderItem = await _context.SellOrderItems.AsNoTracking()
                    .Where(i => i.SellOrderId == salesOrder.Id && i.Status == 0)
                    .OrderBy(i => i.Id)
                    .FirstOrDefaultAsync();
                if (salesOrderItem == null)
                {
                    return BadRequest(ApiResponse<SimulateBusinessChainResponse>.Fail("该销售订单没有可用的正常明细行", 400));
                }

                customerId = salesOrder.CustomerId;
                customerDisplayName = salesOrder.CustomerName ?? customerDisplayName;
                if (!string.IsNullOrWhiteSpace(salesOrder.SalesUserId))
                {
                    salesUserId = salesOrder.SalesUserId!;
                }

                if (!string.IsNullOrWhiteSpace(salesOrderItem.ProductId))
                {
                    materialId = salesOrderItem.ProductId!;
                }

                firstCreateIdx = 3;
                createdNodes.Add($"[现有]SalesOrder:{salesOrder.SellOrderCode}");
            }
            else if (dataOrigin is "purchaseorder")
            {
                if (targetIdx < 4)
                {
                    return BadRequest(ApiResponse<SimulateBusinessChainResponse>.Fail(
                        "数据起源为采购订单时，业务节点请选择「采购订单」或之后环节", 400));
                }

                po = await _context.PurchaseOrders.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PurchaseOrderCode == originCode);
                if (po == null)
                {
                    return BadRequest(ApiResponse<SimulateBusinessChainResponse>.Fail($"未找到采购订单编号: {originCode}", 400));
                }

                poItem = await _context.PurchaseOrderItems.AsNoTracking()
                    .Where(i => i.PurchaseOrderId == po.Id)
                    .OrderBy(i => i.Id)
                    .FirstOrDefaultAsync();
                if (poItem == null)
                {
                    return BadRequest(ApiResponse<SimulateBusinessChainResponse>.Fail("该采购订单没有明细行", 400));
                }

                salesOrderItem = await _context.SellOrderItems.AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == poItem.SellOrderItemId);
                if (salesOrderItem == null)
                {
                    return BadRequest(ApiResponse<SimulateBusinessChainResponse>.Fail("采购订单明细关联的销售订单行不存在", 400));
                }

                salesOrder = await _context.SellOrders.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == salesOrderItem.SellOrderId);
                if (salesOrder == null)
                {
                    return BadRequest(ApiResponse<SimulateBusinessChainResponse>.Fail("销售订单不存在", 400));
                }

                customerId = salesOrder.CustomerId;
                vendorId = po.VendorId;
                customerDisplayName = salesOrder.CustomerName ?? customerDisplayName;
                vendorDisplayName = po.VendorName ?? vendorDisplayName;
                vendorDisplayCode = po.VendorCode ?? vendorDisplayCode;
                if (!string.IsNullOrWhiteSpace(po.PurchaseUserId))
                {
                    purchaseUserId = po.PurchaseUserId!;
                }

                if (!string.IsNullOrWhiteSpace(salesOrder.SalesUserId))
                {
                    salesUserId = salesOrder.SalesUserId!;
                }

                if (!string.IsNullOrWhiteSpace(poItem.ProductId))
                {
                    materialId = poItem.ProductId!;
                }

                firstCreateIdx = 5;
                createdNodes.Add($"[现有]PurchaseOrder:{po.PurchaseOrderCode}");
            }

            if (targetIdx >= 0 && firstCreateIdx <= 0)
            {
                rfq = new RFQ
                {
                    Id = Guid.NewGuid().ToString(),
                    RfqCode = $"RFQ{codeSuffix}",
                    CustomerId = customerId,
                    SalesUserId = salesUserId,
                    Status = normalizedNode == "rfq" ? request.Status : (short)5,
                    CreateTime = now,
                    ItemCount = 1,
                    Remark = $"DEBUG链路:{chainNo}"
                };
                rfqItem = new RFQItem
                {
                    Id = Guid.NewGuid().ToString(),
                    RfqId = rfq.Id,
                    LineNo = 1,
                    Mpn = $"DEBUG-MPN-{chainNo[^6..]}",
                    CustomerBrand = "DEBUG",
                    Brand = "DEBUG",
                    Quantity = 10,
                    Status = normalizedNode == "rfq" ? request.Status : (short)2,
                    CreateTime = now
                };
                _context.RFQs.Add(rfq);
                _context.RFQItems.Add(rfqItem);
                createdNodes.Add($"RFQ:{rfq.RfqCode}");
            }

            if (targetIdx >= 1 && firstCreateIdx <= 1 && rfq != null && rfqItem != null)
            {
                quote = new Quote
                {
                    Id = Guid.NewGuid().ToString(),
                    QuoteCode = $"QT{codeSuffix}",
                    RFQId = rfq.Id,
                    RFQItemId = rfqItem.Id,
                    Mpn = rfqItem.Mpn,
                    CustomerId = customerId,
                    SalesUserId = salesUserId,
                    PurchaseUserId = purchaseUserId,
                    Status = normalizedNode == "quote" ? request.Status : (short)4,
                    QuoteDate = now,
                    CreateTime = now,
                    Remark = $"DEBUG链路:{chainNo}"
                };
                quoteItem = new QuoteItem
                {
                    Id = Guid.NewGuid().ToString(),
                    QuoteId = quote.Id,
                    VendorId = vendorId,
                    VendorName = vendorDisplayName,
                    VendorCode = vendorDisplayCode,
                    Mpn = rfqItem.Mpn,
                    Brand = rfqItem.Brand,
                    Currency = 1,
                    Quantity = 10,
                    UnitPrice = 12.3456m,
                    Status = 0,
                    CreateTime = now
                };
                _context.Quotes.Add(quote);
                _context.QuoteItems.Add(quoteItem);
                createdNodes.Add($"Quote:{quote.QuoteCode}");
            }

            if (targetIdx >= 2 && firstCreateIdx <= 2 && quote != null && rfqItem != null)
            {
                var fxDto = await _financeExchangeRateService.GetCurrentAsync();
                var soStatus = normalizedNode == "salesorder" ? request.Status : (short)20;
                salesOrder = new SellOrder
                {
                    Id = Guid.NewGuid().ToString(),
                    SellOrderCode = $"SO{codeSuffix}",
                    CustomerId = customerId,
                    CustomerName = customerDisplayName,
                    SalesUserId = salesUserId,
                    SalesUserName = "DEBUG-SALES",
                    Status = (SellOrderMainStatus)soStatus,
                    Total = 123.46m,
                    ConvertTotal = 123.46m,
                    ItemRows = 1,
                    CreateTime = now,
                    Comment = $"DEBUG链路:{chainNo}"
                };
                _context.SellOrders.Add(salesOrder);
                await _context.SaveChangesAsync();
                var soSeq = await _sellLineSeq.ReserveNextSequenceBlockAsync(salesOrder.Id, 1);
                salesOrderItem = new SellOrderItem
                {
                    Id = Guid.NewGuid().ToString(),
                    SellOrderId = salesOrder.Id,
                    SellOrderItemCode = OrderLineItemCodes.Sell(salesOrder.SellOrderCode, soSeq),
                    QuoteId = quote.Id,
                    ProductId = materialId,
                    PN = rfqItem.Mpn,
                    Brand = rfqItem.Brand,
                    Qty = 10,
                    Price = 12.3456m,
                    Currency = 1,
                    Status = 0,
                    CreateTime = now
                };
                salesOrderItem.ConvertPrice = ExchangeRateToUsdConverter.UnitLocalToUsd(
                    salesOrderItem.Price, salesOrderItem.Currency, fxDto.UsdToCny, fxDto.UsdToHkd, fxDto.UsdToEur);
                _context.SellOrderItems.Add(salesOrderItem);
                // 先落库销售订单主表再写明细（外键 + 明细编号水位）
                await _context.SaveChangesAsync();
                createdNodes.Add($"SalesOrder:{salesOrder.SellOrderCode}");
            }

            if (targetIdx >= 3 && firstCreateIdx <= 3 && salesOrder != null && salesOrderItem != null)
            {
                pr = new PurchaseRequisition
                {
                    Id = Guid.NewGuid().ToString(),
                    BillCode = $"PR{codeSuffix}",
                    SellOrderId = salesOrder.Id,
                    SellOrderItemId = salesOrderItem.Id,
                    Qty = salesOrderItem.Qty,
                    ExpectedPurchaseTime = now.AddDays(3),
                    Status = normalizedNode == "purchaserequisition" ? request.Status : (short)2,
                    PurchaseUserId = purchaseUserId,
                    SalesUserId = salesUserId,
                    PN = salesOrderItem.PN,
                    Brand = salesOrderItem.Brand,
                    QuoteCost = 10.1234m,
                    CreateTime = now,
                    Remark = $"DEBUG链路:{chainNo}"
                };
                _context.PurchaseRequisitions.Add(pr);
                await _context.SaveChangesAsync();
                createdNodes.Add($"PurchaseRequisition:{pr.BillCode}");
            }

            if (targetIdx >= 4 && firstCreateIdx <= 4 && salesOrderItem != null)
            {
                var fxPo = await _financeExchangeRateService.GetCurrentAsync();
                po = new PurchaseOrder
                {
                    Id = Guid.NewGuid().ToString(),
                    PurchaseOrderCode = $"PO{codeSuffix}",
                    VendorId = vendorId,
                    VendorName = vendorDisplayName,
                    VendorCode = vendorDisplayCode,
                    PurchaseUserId = purchaseUserId,
                    PurchaseUserName = "DEBUG-PUR",
                    Status = normalizedNode == "purchaseorder" ? request.Status : (short)50,
                    Total = 101.23m,
                    ConvertTotal = 101.23m,
                    ItemRows = 1,
                    StockStatus = 1,
                    CreateTime = now,
                    Comment = $"DEBUG链路:{chainNo}"
                };
                _context.PurchaseOrders.Add(po);
                await _context.SaveChangesAsync();
                var poSeq = await _poLineSeq.ReserveNextSequenceBlockAsync(po.Id, 1);
                poItem = new PurchaseOrderItem
                {
                    Id = Guid.NewGuid().ToString(),
                    PurchaseOrderId = po.Id,
                    PurchaseOrderItemCode = OrderLineItemCodes.Purchase(po.PurchaseOrderCode, poSeq),
                    SellOrderItemId = salesOrderItem.Id,
                    VendorId = vendorId,
                    ProductId = materialId,
                    PN = salesOrderItem.PN,
                    Brand = salesOrderItem.Brand,
                    Qty = salesOrderItem.Qty,
                    Cost = 10.1234m,
                    Currency = 1,
                    Status = normalizedNode == "purchaseorder" ? request.Status : (short)60,
                    StockInStatus = 1,
                    CreateTime = now
                };
                poItem.ConvertPrice = ExchangeRateToUsdConverter.UnitLocalToUsd(
                    poItem.Cost, poItem.Currency, fxPo.UsdToCny, fxPo.UsdToHkd, fxPo.UsdToEur);
                _context.PurchaseOrderItems.Add(poItem);
                await _context.SaveChangesAsync();
                createdNodes.Add($"PurchaseOrder:{po.PurchaseOrderCode}");
            }

            if (targetIdx >= 5 && firstCreateIdx <= 5 && po != null && poItem != null)
            {
                var expectQty = InventoryQuantity.RoundFromDecimal(poItem.Qty);
                var expectTotal = Math.Round(expectQty * poItem.Cost, 2, MidpointRounding.AwayFromZero);
                notify = new StockInNotify
                {
                    Id = Guid.NewGuid().ToString(),
                    NoticeCode = $"SN{codeSuffix}",
                    PurchaseOrderId = po.Id,
                    PurchaseOrderCode = po.PurchaseOrderCode,
                    PurchaseOrderItemId = poItem.Id,
                    SellOrderItemId = poItem.SellOrderItemId,
                    VendorId = vendorId,
                    VendorName = po.VendorName,
                    PurchaseUserName = po.PurchaseUserName,
                    Status = normalizedNode == "stockinnotify" ? request.Status : (short)30,
                    RegionType = RegionTypeCode.Domestic,
                    Pn = poItem.PN,
                    Brand = poItem.Brand,
                    ExpectQty = expectQty,
                    ReceiveQty = expectQty,
                    PassedQty = expectQty,
                    Cost = poItem.Cost,
                    ExpectTotal = expectTotal,
                    ReceiveTotal = expectTotal,
                    CreateTime = now
                };
                _context.StockInNotifies.Add(notify);
                await _context.SaveChangesAsync();
                createdNodes.Add($"StockInNotify:{notify.NoticeCode}");
            }

            if (targetIdx >= 6 && firstCreateIdx <= 6 && notify != null)
            {
                qc = new QCInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    QcCode = $"QC{codeSuffix}",
                    StockInNotifyId = notify.Id,
                    StockInNotifyCode = notify.NoticeCode,
                    Status = normalizedNode == "qc" ? request.Status : (short)100,
                    StockInStatus = normalizedNode == "qc" ? (short)1 : (short)100,
                    PassQty = notify.ExpectQty,
                    RejectQty = 0,
                    CreateTime = now
                };
                qcItem = new QCItem
                {
                    Id = Guid.NewGuid().ToString(),
                    QcInfoId = qc.Id,
                    ArrivalStockInNotifyId = notify.Id,
                    ArrivedQty = notify.ExpectQty,
                    PassedQty = notify.PassedQty,
                    RejectQty = 0,
                    CreateTime = now
                };
                _context.QCInfos.Add(qc);
                _context.QCItems.Add(qcItem);
                await _context.SaveChangesAsync();
                createdNodes.Add($"QC:{qc.QcCode}");
            }

            if (targetIdx >= 7 && firstCreateIdx <= 7 && po != null && poItem != null && qc != null)
            {
                var stockInQtyInt = InventoryQuantity.RoundFromDecimal(poItem.Qty);
                var desiredStockInStatus = normalizedNode == "stockin" ? request.Status : (short)2;
                var initialStockInStatus = desiredStockInStatus == 2 ? (short)1 : desiredStockInStatus;
                SellOrderItem? soLineForPo = null;
                if (!string.IsNullOrWhiteSpace(poItem.SellOrderItemId))
                    soLineForPo = await _context.SellOrderItems.FindAsync(poItem.SellOrderItemId);
                stockIn = new StockIn
                {
                    Id = Guid.NewGuid().ToString(),
                    StockInCode = $"STI{codeSuffix}",
                    StockInType = 1,
                    SourceId = notify != null ? notify.Id : null,
                    SourceCode = notify != null && !string.IsNullOrWhiteSpace(notify.NoticeCode)
                        ? notify.NoticeCode.Trim()
                        : null,
                    QcId = qc.Id,
                    QcCode = string.IsNullOrWhiteSpace(qc.QcCode) ? null : qc.QcCode.Trim(),
                    WarehouseId = warehouseId,
                    VendorId = vendorId,
                    StockInDate = now,
                    TotalQuantity = stockInQtyInt,
                    TotalAmount = Math.Round(poItem.Qty * poItem.Cost, 2),
                    Status = initialStockInStatus,
                    InspectStatus = qc.Status == 100 ? (short)1 : (short)0,
                    CreatedBy = purchaseUserId,
                    CreateTime = now,
                    Remark = $"DEBUG链路:{chainNo}"
                };
                qc.StockInId = stockIn.Id;
                qc.StockInStatus = initialStockInStatus == 2 ? (short)100 : qc.StockInStatus;
                _context.StockIns.Add(stockIn);
                await _context.SaveChangesAsync();

                var stockInLineSeq = await _stockInLineSeq.ReserveNextSequenceBlockAsync(stockIn.Id, 1);
                stockInItem = new StockInItem
                {
                    Id = Guid.NewGuid().ToString(),
                    StockInId = stockIn.Id,
                    StockInItemCode = OrderLineItemCodes.StockIn(stockIn.StockInCode, stockInLineSeq),
                    MaterialId = materialId,
                    PurchasePn = string.IsNullOrWhiteSpace(poItem.PN) ? null : poItem.PN.Trim(),
                    PurchaseBrand = string.IsNullOrWhiteSpace(poItem.Brand) ? null : poItem.Brand.Trim(),
                    Currency = poItem.Currency,
                    Quantity = stockInQtyInt,
                    OrderQty = stockInQtyInt,
                    QtyReceived = stockInQtyInt,
                    Price = poItem.Cost,
                    Amount = Math.Round(poItem.Qty * poItem.Cost, 2),
                    BatchNo = $"B{chainNo[^6..]}",
                    CreateTime = now,
                    Remark = "DEBUG自动生成"
                };
                _context.StockInItems.Add(stockInItem);
                await _context.SaveChangesAsync();

                _context.StockInItemExtends.Add(new StockInItemExtend
                {
                    Id = stockInItem.Id,
                    StockInId = stockIn.Id,
                    PurchaseOrderItemId = poItem.Id,
                    PurchaseOrderItemCode = string.IsNullOrWhiteSpace(poItem.PurchaseOrderItemCode)
                        ? null
                        : poItem.PurchaseOrderItemCode.Trim(),
                    SellOrderItemId = string.IsNullOrWhiteSpace(poItem.SellOrderItemId)
                        ? null
                        : poItem.SellOrderItemId.Trim(),
                    SellOrderItemCode = string.IsNullOrWhiteSpace(soLineForPo?.SellOrderItemCode)
                        ? null
                        : soLineForPo!.SellOrderItemCode.Trim(),
                    CreateTime = now
                });
                await _context.SaveChangesAsync();

                // Debug 造数必须走真实“待入库 -> 已入库”业务动作，确保库存汇总与流水一致写入。
                if (desiredStockInStatus == 2)
                {
                    await _stockInService.UpdateStatusAsync(stockIn.Id, 2);
                    stockInPostedThroughRealFlow = true;
                }
                createdNodes.Add($"StockIn:{stockIn.StockInCode}");
            }

            if (targetIdx >= 8 && salesOrder != null && salesOrderItem != null)
            {
                stockOutRequest = new StockOutRequest
                {
                    Id = Guid.NewGuid().ToString(),
                    RequestCode = $"SOR{codeSuffix}",
                    SalesOrderId = salesOrder.Id,
                    SalesOrderItemId = salesOrderItem.Id,
                    MaterialCode = salesOrderItem.PN ?? string.Empty,
                    MaterialName = salesOrderItem.Brand,
                    Quantity = InventoryQuantity.RoundFromDecimal(salesOrderItem.Qty),
                    CustomerId = customerId,
                    RequestUserId = requestUserId,
                    RequestDate = now,
                    Status = normalizedNode == "stockoutrequest" ? request.Status : (short)0,
                    RegionType = RegionTypeCode.Domestic,
                    CreateTime = now,
                    Remark = $"DEBUG链路:{chainNo}"
                };
                _context.StockOutRequests.Add(stockOutRequest);
                await _context.SaveChangesAsync();
                createdNodes.Add($"StockOutRequest:{stockOutRequest.RequestCode}");
            }

            var response = new SimulateBusinessChainResponse
            {
                ChainNo = chainNo,
                BusinessNode = normalizedNode,
                TargetStatus = request.Status,
                CreatedNodes = createdNodes,
                StockInPostedThroughRealFlow = stockInPostedThroughRealFlow
            };

            return Ok(ApiResponse<SimulateBusinessChainResponse>.Ok(response, "模拟数据生成成功"));
        }

        public class RfqChainNodeApiDto
        {
            public string Node { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
            public string Id { get; set; } = string.Empty;
        }

        public class RfqChainPreviewDto
        {
            public string? RfqCode { get; set; }
            public List<RfqChainNodeApiDto> Nodes { get; set; } = new();
        }

        /// <summary>
        /// Debug：按需求单号（RFQ.RfqCode）列出从该需求产生的下游业务节点与数据主键/业务编号。
        /// </summary>
        [Authorize]
        [HttpGet("rfq-chain")]
        public async Task<ActionResult<ApiResponse<RfqChainPreviewDto>>> GetRfqChain([FromQuery] string rfqCode)
        {
            var code = (rfqCode ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(code))
                return BadRequest(ApiResponse<RfqChainPreviewDto>.Fail("rfqCode 不能为空", 400));

            var snap = await RfqDebugChainHelper.LoadChainAsync(_context, code);
            if (snap == null)
                return NotFound(ApiResponse<RfqChainPreviewDto>.Fail($"未找到需求单号: {code}", 404));

            var nodes = snap.ToDisplayNodes()
                .Select(n => new RfqChainNodeApiDto { Node = n.Node, Code = n.Code, Id = n.Id })
                .ToList();
            var dto = new RfqChainPreviewDto { RfqCode = snap.Rfq.RfqCode, Nodes = nodes };
            return Ok(ApiResponse<RfqChainPreviewDto>.Ok(dto, "ok"));
        }

        /// <summary>
        /// Debug：删除指定需求单号关联的下游数据（与造数链路一致）。危险操作，仅登录用户可用。
        /// </summary>
        [Authorize]
        [HttpDelete("rfq-chain")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteRfqChain([FromQuery] string rfqCode)
        {
            var code = (rfqCode ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(code))
                return BadRequest(ApiResponse<object>.Fail("rfqCode 不能为空", 400));

            var snap = await RfqDebugChainHelper.LoadChainAsync(_context, code);
            if (snap == null)
                return NotFound(ApiResponse<object>.Fail($"未找到需求单号: {code}", 404));

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                await RfqDebugChainHelper.DeleteChainAsync(_context, snap);
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, ApiResponse<object>.Fail($"删除失败: {ex.Message}", 500));
            }

            return Ok(ApiResponse<object>.Ok(null, "已删除该需求单及其下游数据"));
        }

        /// <summary>
        /// 临时调试工具：回填 stockledger 的出库成本与币别（避免手工 SQL 受大小写列名影响）。
        /// </summary>
        [Authorize]
        [HttpPost("refresh-purchase-order-main-status")]
        public async Task<ActionResult<ApiResponse<RefreshPurchaseOrderMainStatusResultDto>>> RefreshPurchaseOrderMainStatus()
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var orders = await _context.PurchaseOrders.ToListAsync();
                if (orders.Count == 0)
                {
                    var empty = new RefreshPurchaseOrderMainStatusResultDto();
                    return Ok(ApiResponse<RefreshPurchaseOrderMainStatusResultDto>.Ok(empty, "无采购订单可刷新"));
                }

                var orderIds = orders.Select(o => o.Id).ToList();
                var items = await _context.PurchaseOrderItems
                    .Where(i => orderIds.Contains(i.PurchaseOrderId))
                    .ToListAsync();
                var itemGroups = items
                    .GroupBy(i => i.PurchaseOrderId)
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                var now = DateTime.UtcNow;
                var result = new RefreshPurchaseOrderMainStatusResultDto
                {
                    TotalOrders = orders.Count
                };

                foreach (var order in orders)
                {
                    if (order.Status is PoStatusCancelled or PoStatusAuditFailed)
                    {
                        result.SkippedTerminalOrders++;
                        continue;
                    }

                    if (!itemGroups.TryGetValue(order.Id, out var orderItems) || orderItems.Count == 0)
                        continue;

                    var activeItems = orderItems.Where(i => i.Status != PoStatusCancelled).ToList();
                    if (activeItems.Count == 0)
                        continue;

                    var next = ComputePurchaseOrderMainStatusFromItems(activeItems);

                    if (next == order.Status)
                        continue;

                    order.Status = next;
                    order.ModifyTime = now;
                    result.ChangedOrders++;
                    result.ChangedOrderCodes.Add(order.PurchaseOrderCode ?? order.Id);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(ApiResponse<RefreshPurchaseOrderMainStatusResultDto>.Ok(result, "采购订单主状态刷新完成"));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, ApiResponse<RefreshPurchaseOrderMainStatusResultDto>.Fail($"刷新采购订单主状态失败: {ex.Message}", 500));
            }
        }

        private static short ComputePurchaseOrderMainStatusFromItems(IReadOnlyList<PurchaseOrderItem> activeItems)
        {
            // 与业务语义对齐：主状态只取主流程节点，不映射明细的 40(已付款)/60(已入库)。
            if (activeItems.All(i => i.Status >= PoStatusCompleted))
                return PoStatusCompleted;
            if (activeItems.All(i => i.Status >= PoStatusInProgress))
                return PoStatusInProgress;
            if (activeItems.All(i => i.Status >= PoStatusConfirmed))
                return PoStatusConfirmed;
            if (activeItems.All(i => i.Status >= PoStatusPendingConfirm))
                return PoStatusPendingConfirm;
            if (activeItems.All(i => i.Status >= PoStatusApproved))
                return PoStatusApproved;
            if (activeItems.All(i => i.Status >= PoStatusPendingAudit))
                return PoStatusPendingAudit;
            return PoStatusNew;
        }

        [Authorize]
        [HttpPost("refresh-stockledger")]
        public async Task<ActionResult<ApiResponse<RefreshStockLedgerResultDto>>> RefreshStockLedger()
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var one = (short)CurrencyCode.RMB;
                var result = new RefreshStockLedgerResultDto();

                var stockOutLedgers = await _context.InventoryLedgers
                    .Where(x => x.BizType == "STOCK_OUT"
                                && !string.IsNullOrWhiteSpace(x.BizLineId)
                                && (x.UnitCost == 0m || x.Amount == 0m || x.Currency <= 0))
                    .ToListAsync();

                var outLineIds = stockOutLedgers
                    .Select(x => x.BizLineId!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var outItems = await _context.StockOutItems
                    .Where(x => outLineIds.Contains(x.Id))
                    .Select(x => new { x.Id, x.StockItemId })
                    .ToListAsync();

                var stockItemIds = outItems
                    .Where(x => !string.IsNullOrWhiteSpace(x.StockItemId))
                    .Select(x => x.StockItemId!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var stockItems = await _context.StockItems
                    .Where(x => stockItemIds.Contains(x.Id))
                    .Select(x => new { x.Id, x.PurchasePrice, x.PurchaseCurrency })
                    .ToListAsync();

                var outItemById = outItems.ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);
                var stockItemById = stockItems.ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);

                foreach (var ledger in stockOutLedgers)
                {
                    var lineId = ledger.BizLineId!.Trim();
                    if (!outItemById.TryGetValue(lineId, out var outItem))
                        continue;

                    decimal unitCost = 0m;
                    short currency = one;
                    if (!string.IsNullOrWhiteSpace(outItem.StockItemId)
                        && stockItemById.TryGetValue(outItem.StockItemId.Trim(), out var stockItem))
                    {
                        unitCost = stockItem.PurchasePrice;
                        currency = stockItem.PurchaseCurrency > 0 ? stockItem.PurchaseCurrency : one;
                    }

                    var newAmount = -Math.Abs(ledger.QtyOut) * unitCost;
                    if (ledger.UnitCost != unitCost || ledger.Amount != newAmount || ledger.Currency != currency)
                    {
                        ledger.UnitCost = unitCost;
                        ledger.Amount = newAmount;
                        ledger.Currency = currency;
                        result.StockOutUpdated++;
                    }
                }

                var outLedgerMap = await _context.InventoryLedgers
                    .Where(x => x.BizType == "STOCK_OUT" && !string.IsNullOrWhiteSpace(x.BizLineId))
                    .OrderByDescending(x => x.CreateTime)
                    .ThenByDescending(x => x.Id)
                    .ToListAsync();
                var latestOutByLine = new Dictionary<string, InventoryLedger>(StringComparer.OrdinalIgnoreCase);
                foreach (var outLedger in outLedgerMap)
                {
                    var key = outLedger.BizLineId!.Trim();
                    if (!latestOutByLine.ContainsKey(key))
                        latestOutByLine[key] = outLedger;
                }

                var reverseLedgers = await _context.InventoryLedgers
                    .Where(x => x.BizType == "STOCK_OUT_REVERSE"
                                && !string.IsNullOrWhiteSpace(x.BizLineId)
                                && (x.UnitCost == 0m || x.Amount == 0m || x.Currency <= 0))
                    .ToListAsync();

                foreach (var reverse in reverseLedgers)
                {
                    var lineId = reverse.BizLineId!.Trim();
                    if (!latestOutByLine.TryGetValue(lineId, out var source))
                        continue;

                    var unitCost = source.UnitCost;
                    var currency = source.Currency > 0 ? source.Currency : one;
                    var newAmount = Math.Abs(reverse.QtyIn) * unitCost;
                    if (reverse.UnitCost != unitCost || reverse.Amount != newAmount || reverse.Currency != currency)
                    {
                        reverse.UnitCost = unitCost;
                        reverse.Amount = newAmount;
                        reverse.Currency = currency;
                        result.StockOutReverseUpdated++;
                    }
                }

                var invalidCurrencyRows = await _context.InventoryLedgers
                    .Where(x => x.Currency <= 0)
                    .ToListAsync();
                foreach (var row in invalidCurrencyRows)
                {
                    row.Currency = one;
                    result.CurrencyDefaulted++;
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(ApiResponse<RefreshStockLedgerResultDto>.Ok(result, "刷新 stockledger 成功"));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, ApiResponse<RefreshStockLedgerResultDto>.Fail($"刷新 stockledger 失败: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Debug：将仍为历史多行前缀格式的 <c>sellorder.comment</c> 拆入结构化列（仅填空）；自由段写回 <c>comment</c>。非 legacy 整段不修改。含软删行。
        /// </summary>
        [Authorize]
        [HttpPost("refresh-sellorder-comment-split")]
        public async Task<ActionResult<ApiResponse<RefreshSellOrderCommentSplitResultDto>>> RefreshSellOrderCommentSplit()
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var orders = await _context.SellOrders
                    .IgnoreQueryFilters()
                    .Where(o => o.Comment != null && o.Comment != "")
                    .ToListAsync();

                var result = new RefreshSellOrderCommentSplitResultDto { TotalWithComment = orders.Count };
                foreach (var o in orders)
                {
                    if (SellOrderHeaderRemarkCodec.TrySplitCommentOntoStructuredColumns(o))
                        result.RowsProcessed++;
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(ApiResponse<RefreshSellOrderCommentSplitResultDto>.Ok(
                    result,
                    $"已处理 {result.RowsProcessed} 条销售订单备注拆分（共 {result.TotalWithComment} 条待处理）"));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500,
                    ApiResponse<RefreshSellOrderCommentSplitResultDto>.Fail($"拆分 sellorder.comment 失败: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Debug：从 <c>sellorderitem.comment</c> 解析「客户物料型号」前缀行写入 <c>customer_pn</c>（仅填空；含软删行）。
        /// </summary>
        [Authorize]
        [HttpPost("refresh-sellorderitem-customer-pn-from-comment")]
        public async Task<ActionResult<ApiResponse<RefreshSellOrderItemCustomerPnFromCommentResultDto>>>
            RefreshSellOrderItemCustomerPnFromComment()
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var items = await _context.SellOrderItems
                    .IgnoreQueryFilters()
                    .Where(i => i.Comment != null && i.Comment != "")
                    .ToListAsync();

                var result = new RefreshSellOrderItemCustomerPnFromCommentResultDto
                {
                    TotalWithComment = items.Count
                };

                foreach (var line in items)
                {
                    if (!string.IsNullOrWhiteSpace(line.CustomerPn))
                        continue;
                    var parsed = SellOrderItemCommentCodec.TryParseCustomerMaterialModelFromComment(line.Comment);
                    if (string.IsNullOrWhiteSpace(parsed))
                        continue;
                    line.CustomerPn = parsed;
                    result.RowsFilled++;
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(ApiResponse<RefreshSellOrderItemCustomerPnFromCommentResultDto>.Ok(
                    result,
                    $"已回填 customer_pn {result.RowsFilled} 条（comment 非空共 {result.TotalWithComment} 条）"));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500,
                    ApiResponse<RefreshSellOrderItemCustomerPnFromCommentResultDto>.Fail(
                        $"刷新 sellorderitem.customer_pn 失败: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Debug：将采购请款旧版写入 <c>financepayment.Remark</c> 的管道串（供应商银行 / 费用 / 承担方 / 明细备注）拆入
        /// <c>RequestRemark</c>、费用列、<c>FinancePaymentBankId</c>、明细 <c>LineRemark</c>，并清空主表 <c>Remark</c>。
        /// </summary>
        [Authorize]
        [HttpPost("refresh-financepayment-remark-from-legacy")]
        public async Task<ActionResult<ApiResponse<RefreshFinancePaymentLegacyRemarkResultDto>>> RefreshFinancePaymentRemarkFromLegacy()
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var payments = await _context.FinancePayments
                    .Include(p => p.Items)
                    .Where(p => p.Remark != null && p.Remark != "")
                    .ToListAsync();

                var poIds = payments
                    .SelectMany(p => p.Items)
                    .Select(i => i.PurchaseOrderId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => id!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var poCodeById = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                if (poIds.Count > 0)
                {
                    var rows = await _context.PurchaseOrders.AsNoTracking()
                        .IgnoreQueryFilters()
                        .Where(po => poIds.Contains(po.Id))
                        .Select(po => new { po.Id, po.PurchaseOrderCode })
                        .ToListAsync();
                    foreach (var row in rows)
                        poCodeById[row.Id] = row.PurchaseOrderCode;
                }

                var paymentBanks = await _context.FinancePaymentBanks.AsNoTracking().ToListAsync();
                var validBankIds = new HashSet<string>(paymentBanks.Select(b => b.Id), StringComparer.OrdinalIgnoreCase);

                var dto = new RefreshFinancePaymentLegacyRemarkResultDto
                {
                    TotalPaymentsRemarkNonEmpty = payments.Count
                };

                foreach (var p in payments)
                {
                    if (!FinancePaymentLegacyRemarkCodec.LooksLikePackedRemark(p.Remark))
                        continue;
                    dto.LegacyPackedCandidates++;
                    if (!FinancePaymentLegacyRemarkCodec.TryParse(p.Remark, out var parsed))
                    {
                        dto.SkippedMalformed++;
                        continue;
                    }

                    var itemsList = p.Items.ToList();
                    dto.ItemsLineRemarkUpdated += FinancePaymentLegacyRemarkCodec.ApplyToEntities(
                        p, itemsList, poCodeById, parsed, paymentBanks);
                    dto.ParsedAndApplied++;
                }

                var now = DateTime.UtcNow;
                var allPaymentsForBank = await _context.FinancePayments
                    .Where(x => x.FinancePaymentBankId != null && x.FinancePaymentBankId != "")
                    .ToListAsync();
                foreach (var p in allPaymentsForBank)
                {
                    var raw = p.FinancePaymentBankId!.Trim();
                    if (validBankIds.Contains(raw))
                        continue;
                    var resolved = FinancePaymentBankIdResolver.ResolveFromToken(raw, paymentBanks);
                    if (resolved == null)
                        continue;
                    p.FinancePaymentBankId = resolved;
                    p.ModifyTime = now;
                    dto.BankIdsResolvedFromName++;
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(ApiResponse<RefreshFinancePaymentLegacyRemarkResultDto>.Ok(
                    dto,
                    $"已处理 {dto.ParsedAndApplied} 条付款单（候选 {dto.LegacyPackedCandidates}，无法解析 {dto.SkippedMalformed}，明细行备注更新 {dto.ItemsLineRemarkUpdated}，付款银行 Id 按名称纠正 {dto.BankIdsResolvedFromName}）"));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500,
                    ApiResponse<RefreshFinancePaymentLegacyRemarkResultDto>.Fail(
                        $"刷新付款备注失败: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Debug：对 Host、Database、Username、Password 的值脱敏——从第 1 位起算，奇数位保留、偶数位替换为 *。
        /// </summary>
        private static string MaskConnectionStringForDebugDisplay(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return "(无法获取数据库连接串，请检查配置)";

            var s = connectionString.Trim();

            static string MaskEvenPositionsToStar(string? value)
            {
                if (string.IsNullOrEmpty(value))
                    return value ?? string.Empty;
                var sb = new StringBuilder(value.Length);
                for (var i = 0; i < value.Length; i++)
                {
                    // 第 (i+1) 位：奇数位保留，偶数位为 *
                    if ((i + 1) % 2 == 0)
                        sb.Append('*');
                    else
                        sb.Append(value[i]);
                }
                return sb.ToString();
            }

            static string ReplaceKeyValue(string input, string pattern)
            {
                return Regex.Replace(
                    input,
                    pattern,
                    m => $"{m.Groups[1].Value}={MaskEvenPositionsToStar(m.Groups[2].Value)}",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }

            // PostgreSQL / 常见别名
            s = ReplaceKeyValue(s, @"\b(Host|Server)\s*=\s*([^;]*)");
            s = ReplaceKeyValue(s, @"\b(Database)\s*=\s*([^;]*)");
            // SQL Server
            s = ReplaceKeyValue(s, @"\b(Initial\s+Catalog)\s*=\s*([^;]*)");
            s = ReplaceKeyValue(s, @"\b(Username|User\s+Id|User\s+ID|UID)\s*=\s*([^;]*)");
            s = ReplaceKeyValue(s, @"\b(Password|Pwd)\s*=\s*([^;]*)");

            return s;
        }
    }
}

