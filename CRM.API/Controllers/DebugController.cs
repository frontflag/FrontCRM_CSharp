using System.Text;
using System.Text.RegularExpressions;
using CRM.API.Models.DTOs;
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

        public DebugController(
            ApplicationDbContext context,
            IConfiguration configuration,
            IStockInService stockInService,
            IFinanceExchangeRateService financeExchangeRateService,
            ISellOrderExtendLineSeqService sellLineSeq,
            IPurchaseOrderExtendLineSeqService poLineSeq)
        {
            _context = context;
            _configuration = configuration;
            _stockInService = stockInService;
            _financeExchangeRateService = financeExchangeRateService;
            _sellLineSeq = sellLineSeq;
            _poLineSeq = poLineSeq;
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
                var expectQty = poItem.Qty;
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
                    SourceId = notify != null ? notify.Id : null,
                    SourceCode = notify != null && !string.IsNullOrWhiteSpace(notify.NoticeCode)
                        ? notify.NoticeCode.Trim()
                        : null,
                    QcId = qc.Id,
                    QcCode = string.IsNullOrWhiteSpace(qc.QcCode) ? null : qc.QcCode.Trim(),
                    WarehouseId = warehouseId,
                    VendorId = vendorId,
                    StockInDate = now,
                    TotalQuantity = poItem.Qty,
                    TotalAmount = Math.Round(poItem.Qty * poItem.Cost, 2),
                    Status = initialStockInStatus,
                    InspectStatus = qc.Status == 100 ? (short)1 : (short)0,
                    CreatedBy = purchaseUserId,
                    CreateTime = now,
                    Remark = $"DEBUG链路:{chainNo}"
                };
                stockInItem = new StockInItem
                {
                    Id = Guid.NewGuid().ToString(),
                    StockInId = stockIn.Id,
                    MaterialId = materialId,
                    Quantity = poItem.Qty,
                    OrderQty = poItem.Qty,
                    QtyReceived = poItem.Qty,
                    Price = poItem.Cost,
                    Amount = Math.Round(poItem.Qty * poItem.Cost, 2),
                    BatchNo = $"B{chainNo[^6..]}",
                    CreateTime = now,
                    Remark = "DEBUG自动生成"
                };
                qc.StockInId = stockIn.Id;
                qc.StockInStatus = initialStockInStatus == 2 ? (short)100 : qc.StockInStatus;
                _context.StockIns.Add(stockIn);
                _context.StockInItems.Add(stockInItem);
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
                    Quantity = salesOrderItem.Qty,
                    CustomerId = customerId,
                    RequestUserId = requestUserId,
                    RequestDate = now,
                    Status = normalizedNode == "stockoutrequest" ? request.Status : (short)0,
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

