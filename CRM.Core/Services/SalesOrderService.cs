using CRM.Core.Constants;
using System.Collections.Generic;
using System.Text.Json;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Quote;
using CRM.Core.Models.Sales;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.System;
using CRM.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services
{
    /// <summary>销售订单服务实现</summary>
    public class SalesOrderService : ISalesOrderService
    {
        private readonly IRepository<SellOrder> _soRepo;
        private readonly IRepository<SellOrderItem> _soItemRepo;
        private readonly IRepository<SellOrderItemExtend> _soItemExtendRepo;
        private readonly IRepository<PurchaseOrder> _poRepo;
        private readonly IRepository<PurchaseOrderItem> _poItemRepo;
        private readonly IRepository<PurchaseRequisition> _prRepo;
        private readonly IRepository<CustomerInfo> _customerRepo;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;
        private readonly IFinanceExchangeRateService _financeExchangeRateService;
        private readonly IOrderJourneyLogService _orderJourneyLog;
        private readonly IRepository<QuoteItem> _quoteItemRepo;
        private readonly ISellOrderItemExtendSyncService _soItemExtendSync;
        private readonly ISellOrderMainStatusSyncService _mainStatusSync;
        private readonly ISellOrderItemPurchasedStockAvailableSyncService _purchasedStockAvailableSync;
        private readonly ISellOrderExtendLineSeqService _soLineSeq;
        private readonly IUserService _userService;
        private readonly ISalesOrderListQuery _salesOrderListQuery;
        private readonly ISalesOrderItemLineListQuery _salesOrderItemLineListQuery;
        private readonly ILogOperationAppendService _logOperationAppend;
        private readonly ILogger<SalesOrderService> _logger;
        private readonly IQuoteStatusSyncService _quoteStatusSync;
        private readonly ISalesOrderCustomerDownstreamSyncService? _customerDownstreamSyncService;
        private readonly ISalesOrderSalesPriceDownstreamSyncService? _salesPriceDownstreamSync;
        private readonly ISalesOrderRefreshCompletedGateService? _refreshCompletedGate;
        private readonly ISalesOrderIdentityDownstreamSyncService? _identityDownstreamSync;

        public SalesOrderService(
            IRepository<SellOrder> soRepo,
            IRepository<SellOrderItem> soItemRepo,
            IRepository<SellOrderItemExtend> soItemExtendRepo,
            IRepository<PurchaseOrder> poRepo,
            IRepository<PurchaseOrderItem> poItemRepo,
            IRepository<PurchaseRequisition> prRepo,
            IRepository<CustomerInfo> customerRepo,
            IRepository<QuoteItem> quoteItemRepo,
            IDataPermissionService dataPermissionService,
            ISerialNumberService serialNumberService,
            IFinanceExchangeRateService financeExchangeRateService,
            IOrderJourneyLogService orderJourneyLog,
            ISellOrderItemExtendSyncService soItemExtendSync,
            ISellOrderMainStatusSyncService mainStatusSync,
            ISellOrderItemPurchasedStockAvailableSyncService purchasedStockAvailableSync,
            ISellOrderExtendLineSeqService soLineSeq,
            IUserService userService,
            ISalesOrderListQuery salesOrderListQuery,
            ISalesOrderItemLineListQuery salesOrderItemLineListQuery,
            ILogOperationAppendService logOperationAppend,
            IUnitOfWork unitOfWork,
            ILogger<SalesOrderService> logger,
            IQuoteStatusSyncService quoteStatusSync,
            ISalesOrderCustomerDownstreamSyncService? customerDownstreamSyncService = null,
            ISalesOrderSalesPriceDownstreamSyncService? salesPriceDownstreamSync = null,
            ISalesOrderRefreshCompletedGateService? refreshCompletedGate = null,
            ISalesOrderIdentityDownstreamSyncService? identityDownstreamSync = null)
        {
            _soRepo = soRepo;
            _soItemRepo = soItemRepo;
            _soItemExtendRepo = soItemExtendRepo;
            _poRepo = poRepo;
            _poItemRepo = poItemRepo;
            _prRepo = prRepo;
            _customerRepo = customerRepo;
            _quoteItemRepo = quoteItemRepo;
            _dataPermissionService = dataPermissionService;
            _serialNumberService = serialNumberService;
            _financeExchangeRateService = financeExchangeRateService;
            _orderJourneyLog = orderJourneyLog;
            _soItemExtendSync = soItemExtendSync;
            _mainStatusSync = mainStatusSync;
            _purchasedStockAvailableSync = purchasedStockAvailableSync;
            _soLineSeq = soLineSeq;
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _salesOrderListQuery = salesOrderListQuery ?? throw new ArgumentNullException(nameof(salesOrderListQuery));
            _salesOrderItemLineListQuery = salesOrderItemLineListQuery ?? throw new ArgumentNullException(nameof(salesOrderItemLineListQuery));
            _logOperationAppend = logOperationAppend ?? throw new ArgumentNullException(nameof(logOperationAppend));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger;
            _quoteStatusSync = quoteStatusSync ?? throw new ArgumentNullException(nameof(quoteStatusSync));
            _customerDownstreamSyncService = customerDownstreamSyncService;
            _salesPriceDownstreamSync = salesPriceDownstreamSync;
            _refreshCompletedGate = refreshCompletedGate;
            _identityDownstreamSync = identityDownstreamSync;
        }

        private static IEnumerable<string?> CollectQuoteIds(IEnumerable<SellOrderItem> items) =>
            items.Select(i => i.QuoteId).Where(id => !string.IsNullOrWhiteSpace(id));

        private static string? NormalizeActingUserId(string? actingUserId) =>
            string.IsNullOrWhiteSpace(actingUserId) ? null : actingUserId.Trim();

        private static string? NormalizeOptionalUserId(string? userId) =>
            string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();

        public async Task<SellOrder> CreateAsync(CreateSalesOrderRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(request.CustomerId))
                throw new ArgumentException("客户ID不能为空", nameof(request.CustomerId));

            SellOrderItemLinkRules.ValidateCustomerOrderItems(
                request.Type,
                request.Items.Select(i => i.QuoteId).ToList());

            var sellOrderCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.SalesOrder);

            var order = new SellOrder
            {
                Id = Guid.NewGuid().ToString(),
                SellOrderCode = sellOrderCode,
                CustomerId = request.CustomerId,
                CustomerName = request.CustomerName,
                SalesUserId = request.SalesUserId,
                SalesUserName = request.SalesUserName,
                Assistor = NormalizeOptionalUserId(request.Assistor),
                Type = request.Type,
                Currency = request.Currency,
                DeliveryDate = PostgreSqlDateTime.ToUtc(request.DeliveryDate),
                DeliveryAddress = request.DeliveryAddress,
                Status = SellOrderMainStatus.New,
                ItemRows = request.Items.Count,
                CreateTime = DateTime.UtcNow,
                CreateByUserId = NormalizeActingUserId(actingUserId)
            };
            ApplySalesOrderHeaderRemarksForCreate(order, request);
            await _soRepo.AddAsync(order);
            // 先落库主表，避免 sellorderextend 外键及明细外键失败；且序号预留 SQL 需能读到已提交的 sellorder 行
            await _unitOfWork.SaveChangesAsync();

            var fx = await _financeExchangeRateService.GetCurrentAsync();
            var firstSeq = 0;
            if (request.Items.Count > 0)
                firstSeq = await _soLineSeq.ReserveNextSequenceBlockAsync(order.Id, request.Items.Count);
            var lineIndex = 0;
            decimal total = 0m;
            decimal convertTotalUsd = 0m;
            var createdLines = new List<SellOrderItem>();
            foreach (var item in request.Items)
            {
                var seq = firstSeq + lineIndex++;
                var soItem = new SellOrderItem
                {
                    Id = Guid.NewGuid().ToString(),
                    SellOrderId = order.Id,
                    SellOrderItemCode = OrderLineItemCodes.Sell(order.SellOrderCode, seq),
                    QuoteId = item.QuoteId,
                    ProductId = item.ProductId,
                    PN = item.PN,
                    Brand = item.Brand,
                    CustomerSo = item.CustomerSo,
                    CustomerPn = string.IsNullOrWhiteSpace(item.CustomerPn) ? null : item.CustomerPn.Trim(),
                    CustomerBrand = string.IsNullOrWhiteSpace(item.CustomerBrand) ? null : item.CustomerBrand.Trim(),
                    Qty = item.Qty,
                    Price = item.Price,
                    Currency = item.Currency,
                    DateCode = item.DateCode,
                    DeliveryDate = PostgreSqlDateTime.ToUtc(item.DeliveryDate),
                    Comment = item.Comment,
                    CreateTime = DateTime.UtcNow
                };
                soItem.ConvertPrice = ExchangeRateToUsdConverter.UnitLocalToUsd(
                    soItem.Price, soItem.Currency, fx.UsdToCny, fx.UsdToHkd, fx.UsdToEur);
                await _soItemRepo.AddAsync(soItem);
                createdLines.Add(soItem);
                await AddSellOrderItemExtendAsync(soItem, fx);
                total += item.Qty * item.Price;
                convertTotalUsd += ExchangeRateToUsdConverter.LineAmountUsd(soItem.Qty, soItem.ConvertPrice);
            }
            order.Total = total;
            order.ConvertTotal = convertTotalUsd;
            await _soRepo.UpdateAsync(order);

            await _unitOfWork.SaveChangesAsync();

            foreach (var line in createdLines)
                await _soItemExtendSync.RecalculateAsync(line.Id);

            await TryRefreshPurchasedStockAvailableForSellLinesAsync(createdLines);
            await _unitOfWork.SaveChangesAsync();

            var journeyTime = DateTime.UtcNow;
            await _orderJourneyLog.AppendAsync(new OrderJourneyLog
            {
                EntityKind = OrderJourneyEntityKinds.SellOrder,
                EntityId = order.Id,
                DocumentCode = order.SellOrderCode,
                EventCode = OrderJourneyEventCodes.SoCreated,
                EventTime = journeyTime,
                Amount = order.Total,
                Currency = order.Currency,
                ActorKind = OrderJourneyActorKinds.System,
                Source = nameof(SalesOrderService)
            });
            foreach (var line in createdLines)
            {
                var lineTotal = Math.Round(line.Qty * line.Price, 2, MidpointRounding.AwayFromZero);
                await _orderJourneyLog.AppendAsync(new OrderJourneyLog
                {
                    EntityKind = OrderJourneyEntityKinds.SellOrderItem,
                    EntityId = line.Id,
                    ParentEntityKind = OrderJourneyEntityKinds.SellOrder,
                    ParentEntityId = order.Id,
                    DocumentCode = order.SellOrderCode,
                    LineHint = JourneyLineHint(line.PN, line.Brand),
                    EventCode = OrderJourneyEventCodes.SoItemCreated,
                    EventTime = journeyTime,
                    Quantity = line.Qty,
                    Amount = lineTotal,
                    Currency = line.Currency,
                    ActorKind = OrderJourneyActorKinds.System,
                    Source = nameof(SalesOrderService)
                });
            }

            await _quoteStatusSync.MarkQuotesWonAsync(CollectQuoteIds(createdLines));

            return order;
        }

        private static string? JourneyLineHint(string? pn, string? brand)
        {
            var s = $"{pn ?? ""} / {brand ?? ""}".Trim();
            if (s == "/") return null;
            return s.Length <= 200 ? s : s[..200];
        }

        private static string? TrimHeaderField(string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private static void ApplySalesOrderHeaderRemarksForCreate(SellOrder order, CreateSalesOrderRequest request)
        {
            order.ProductKind = TrimHeaderField(request.ProductKind);
            order.CustomerContactName = TrimHeaderField(request.CustomerContactName);
            order.InvoiceInfo = TrimHeaderField(request.InvoiceInfo);
            order.PaymentTermsText = TrimHeaderField(request.PaymentTermsText);

            var incoming = TrimHeaderField(request.Comment);
            if (string.IsNullOrWhiteSpace(incoming))
            {
                order.Comment = null;
                return;
            }

            if (SellOrderHeaderRemarkCodec.LooksLikeLegacyHeaderBlob(incoming))
            {
                var b = SellOrderHeaderRemarkCodec.ParseLegacyComment(incoming);
                if (order.ProductKind == null && b.ProductKind != null) order.ProductKind = b.ProductKind;
                if (order.CustomerContactName == null && b.CustomerContactName != null) order.CustomerContactName = b.CustomerContactName;
                if (order.InvoiceInfo == null && b.InvoiceInfo != null) order.InvoiceInfo = b.InvoiceInfo;
                if (order.PaymentTermsText == null && b.PaymentTermsText != null) order.PaymentTermsText = b.PaymentTermsText;
                order.Comment = TrimHeaderField(b.LooseRemark);
            }
            else
            {
                order.Comment = incoming;
            }
        }

        private static bool HeaderRemarksTouched(UpdateSalesOrderRequest request) =>
            request.Comment != null
            || request.ProductKind != null
            || request.CustomerContactName != null
            || request.InvoiceInfo != null
            || request.PaymentTermsText != null;

        private static void PatchSalesOrderHeaderRemarksFromRequest(SellOrder order, UpdateSalesOrderRequest request)
        {
            if (request.Comment != null)
            {
                var trimmed = TrimHeaderField(request.Comment);
                if (string.IsNullOrWhiteSpace(trimmed))
                    order.Comment = null;
                else if (SellOrderHeaderRemarkCodec.LooksLikeLegacyHeaderBlob(trimmed))
                {
                    var b = SellOrderHeaderRemarkCodec.ParseLegacyComment(trimmed);
                    SellOrderHeaderRemarkCodec.MergeNonNullFromBlocks(order, b);
                    order.Comment = TrimHeaderField(b.LooseRemark);
                }
                else
                    order.Comment = trimmed;
            }

            if (request.ProductKind != null) order.ProductKind = TrimHeaderField(request.ProductKind);
            if (request.CustomerContactName != null) order.CustomerContactName = TrimHeaderField(request.CustomerContactName);
            if (request.InvoiceInfo != null) order.InvoiceInfo = TrimHeaderField(request.InvoiceInfo);
            if (request.PaymentTermsText != null) order.PaymentTermsText = TrimHeaderField(request.PaymentTermsText);
        }

        /// <summary>
        /// 新建/替换销售明细后：按 PN+品牌重算备货可用量快照（先备货后建单场景）。
        /// </summary>
        private async Task TryRefreshPurchasedStockAvailableForSellLinesAsync(IReadOnlyList<SellOrderItem> lines)
        {
            var keys = new HashSet<(string Pn, string Br)>();
            foreach (var line in lines)
            {
                var pn = string.IsNullOrWhiteSpace(line.PN) ? string.Empty : line.PN.Trim();
                var br = string.IsNullOrWhiteSpace(line.Brand) ? string.Empty : line.Brand.Trim();
                if (pn.Length == 0 || br.Length == 0)
                    continue;
                keys.Add((pn, br));
            }

            foreach (var (pn, br) in keys)
            {
                try
                {
                    await _purchasedStockAvailableSync.RecalculateByPurchasePnAndBrandAsync(pn, br);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "[PurchasedStockAvail] RecalculateByPurchasePnAndBrand failed after sell lines created/updated Pn={Pn} Brand={Br}",
                        pn, br);
                }
            }
        }

        /// <inheritdoc />
        public async Task<PurchasedStockAvailableRefreshDto> RefreshPurchasedStockAvailableForLineAsync(
            string salesOrderId,
            string sellOrderItemId,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            if (string.IsNullOrWhiteSpace(salesOrderId))
                throw new ArgumentException("销售订单ID不能为空", nameof(salesOrderId));
            if (string.IsNullOrWhiteSpace(sellOrderItemId))
                throw new ArgumentException("销售订单明细ID不能为空", nameof(sellOrderItemId));

            var orderId = salesOrderId.Trim();
            var lineId = sellOrderItemId.Trim();
            var order = await _soRepo.GetByIdAsync(orderId)
                ?? throw new InvalidOperationException("销售订单不存在");
            var line = await _soItemRepo.GetByIdAsync(lineId)
                ?? throw new InvalidOperationException("销售订单明细不存在");
            if (!string.Equals(line.SellOrderId, order.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("销售订单明细不属于该订单");
            if (line.Status == 1)
                throw new InvalidOperationException("该销售订单明细已取消，不能刷新备货可用量");
            if (string.IsNullOrWhiteSpace(line.PN) || string.IsNullOrWhiteSpace(line.Brand))
                throw new InvalidOperationException("物料型号或品牌为空，无法刷新备货可用量");

            var ext = await _soItemExtendRepo.GetByIdAsync(line.Id)
                ?? throw new InvalidOperationException("销售明细扩展不存在");
            var before = ext.PurchasedStock_AvailableQty;

            await _purchasedStockAvailableSync.RecalculateByPurchasePnAndBrandAsync(line.PN, line.Brand);
            await _unitOfWork.SaveChangesAsync();

            var afterExt = await _soItemExtendRepo.GetByIdAsync(line.Id);
            return new PurchasedStockAvailableRefreshDto
            {
                SellOrderItemId = line.Id,
                BeforeQty = before,
                AfterQty = afterExt?.PurchasedStock_AvailableQty ?? before
            };
        }

        private async Task AddSellOrderItemExtendAsync(SellOrderItem soItem, FinanceExchangeRateDto fx)
        {
            QuoteItem? qItem = null;
            if (!string.IsNullOrWhiteSpace(soItem.QuoteId))
            {
                var qItems = (await _quoteItemRepo.FindAsync(x => x.QuoteId == soItem.QuoteId)).ToList();
                qItem = QuoteItemForPrResolver.PickSingleLine(qItems);
            }

            var quoteCost = 0m;
            var quoteCur = (short)1;
            var quoteConvert = 0m;
            string? quoteItemId = null;
            if (qItem != null)
            {
                quoteCost = qItem.UnitPrice;
                quoteCur = qItem.Currency;
                quoteConvert = ExchangeRateToUsdConverter.UnitLocalToUsd(
                    quoteCost, quoteCur, fx.UsdToCny, fx.UsdToHkd, fx.UsdToEur);
                quoteItemId = qItem.Id;
            }

            var sellUnitUsd = soItem.ConvertPrice;
            var sellLineUsd = Math.Round(soItem.Qty * sellUnitUsd, 2, MidpointRounding.AwayFromZero);
            var quoteLineCostUsd = Math.Round(soItem.Qty * quoteConvert, 2, MidpointRounding.AwayFromZero);
            var quoteProfit = Math.Round(sellLineUsd - quoteLineCostUsd, 2, MidpointRounding.AwayFromZero);
            var quoteRate = quoteLineCostUsd > 0m
                ? Math.Round(sellLineUsd / quoteLineCostUsd, 6, MidpointRounding.AwayFromZero)
                : 0m;

            var lineTotal = Math.Round(soItem.Qty * soItem.Price, 2, MidpointRounding.AwayFromZero);
            await _soItemExtendRepo.AddAsync(new SellOrderItemExtend
            {
                Id = soItem.Id,
                QtyAlreadyPurchased = 0m,
                QtyNotPurchase = soItem.Qty,
                QtyStockOutNotify = 0m,
                QtyStockOutNotifyNot = soItem.Qty,
                QtyStockOutActual = 0m,
                InvoiceAmount = lineTotal,
                InvoiceAmountNot = lineTotal,
                ReceiptAmount = lineTotal,
                ReceiptAmountNot = lineTotal,
                PaymentAmountToBe = lineTotal,
                QuoteItemId = quoteItemId,
                QuoteCost = quoteCost,
                QuoteCurrency = quoteCur,
                QuoteConvertCost = quoteConvert,
                FxUsdToCnySnapshot = fx.UsdToCny,
                FxUsdToHkdSnapshot = fx.UsdToHkd,
                FxUsdToEurSnapshot = fx.UsdToEur,
                SellConvertUsdUnitSnapshot = sellUnitUsd,
                SellLineAmountUsdSnapshot = sellLineUsd,
                QuoteProfitExpected = quoteProfit,
                QuoteProfitRateExpected = quoteRate,
                CreateTime = DateTime.UtcNow
            });
        }

        public async Task<SellOrder?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            var order = await _soRepo.GetByIdAsync(id);
            if (order == null) return null;
            if (SellOrderHeaderRemarkCodec.TryMaterializeFromLegacyComment(order))
            {
                await _soRepo.UpdateAsync(order);
                await _unitOfWork.SaveChangesAsync();
            }

            var items = await _soItemRepo.FindAsync(i => i.SellOrderId == id);
            order.Items = items.Where(i => !i.IsDeleted).ToList();
            await HydrateSellOrderSalesUserRealNameAsync(order);
            return order;
        }

        /// <summary>详情：填充业务员中文名（质保书等打印用，不落库）。</summary>
        private async Task HydrateSellOrderSalesUserRealNameAsync(SellOrder order)
        {
            if (string.IsNullOrWhiteSpace(order.SalesUserId)) return;
            var user = await _userService.GetByIdAsync(order.SalesUserId.Trim());
            var real = user?.RealName?.Trim();
            if (!string.IsNullOrWhiteSpace(real))
                order.SalesUserRealName = real;
        }

        public async Task<IEnumerable<SellOrder>> GetAllAsync()
        {
            return await _soRepo.GetAllAsync();
        }

        public async Task<PagedResult<SellOrder>> GetPagedAsync(SalesOrderQueryRequest request)
        {
            var result = await _salesOrderListQuery.GetPagedAsync(request, CancellationToken.None);
            var list = result.Items.ToList();
            if (list.Count > 0)
            {
                await HydrateSellOrderListSalesLoginAsync(list);
                await EnrichCustomerExtendFieldsAsync(list);
            }

            return new PagedResult<SellOrder>
            {
                Items = list,
                TotalCount = result.TotalCount,
                PageIndex = result.PageIndex,
                PageSize = result.PageSize
            };
        }

        /// <summary>列表接口：业务员列展示登录账号（不落库，仅响应填充）。</summary>
        private async Task HydrateSellOrderListSalesLoginAsync(List<SellOrder> orders)
        {
            if (orders.Count == 0) return;
            var users = (await _userService.GetAllAsync())
                .ToDictionary(u => u.Id, u => u, StringComparer.OrdinalIgnoreCase);
            foreach (var o in orders)
            {
                if (string.IsNullOrWhiteSpace(o.SalesUserId)) continue;
                if (!users.TryGetValue(o.SalesUserId.Trim(), out var u)) continue;
                var login = EntityLookupService.FormatUserLoginName(u);
                if (!string.IsNullOrWhiteSpace(login))
                    o.SalesUserName = login;
            }
        }

        private async Task EnrichCustomerExtendFieldsAsync(List<SellOrder> orders)
        {
            if (orders.Count == 0) return;
            var ids = orders
                .Select(o => o.CustomerId)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (ids.Length == 0) return;

            var customers = (await _customerRepo.FindAsNoTrackingAsync(c => ids.Contains(c.Id))).ToList();
            var byId = customers
                .Where(c => !string.IsNullOrWhiteSpace(c.Id))
                .GroupBy(c => c.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var o in orders)
            {
                if (string.IsNullOrWhiteSpace(o.CustomerId)) continue;
                if (!byId.TryGetValue(o.CustomerId.Trim(), out var cust)) continue;

                var nameZh = ResolveCustomerDisplayName(cust);
                if (!string.IsNullOrWhiteSpace(nameZh))
                    o.CustomerName = nameZh;
                if (!string.IsNullOrWhiteSpace(cust.EnglishOfficialName))
                    o.CustomerEnglishName = cust.EnglishOfficialName.Trim();
                if (!string.IsNullOrWhiteSpace(cust.CustomerCode))
                    o.CustomerCode = cust.CustomerCode.Trim();
            }
        }

        private static string? ResolveCustomerDisplayName(CustomerInfo cust)
        {
            var nameZh = string.IsNullOrWhiteSpace(cust.OfficialName) ? cust.CustomerName : cust.OfficialName;
            return string.IsNullOrWhiteSpace(nameZh) ? null : nameZh.Trim();
        }

        private async Task ApplyCustomerHeaderFromMasterAsync(SellOrder order, string customerId)
        {
            var id = customerId.Trim();
            var cust = await _customerRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"客户 {id} 不存在");
            order.CustomerId = id;
            order.CustomerName = ResolveCustomerDisplayName(cust);
        }

        private async Task HydrateSellOrderLineListCustomerEnglishAsync(List<SellOrderItemLineDto> rows)
        {
            if (rows.Count == 0) return;

            var customerIds = rows
                .Select(x => x.CustomerId)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (customerIds.Count == 0) return;

            var customers = (await _customerRepo.FindAsync(c => customerIds.Contains(c.Id))).ToList();
            var byId = customers
                .Where(c => !string.IsNullOrWhiteSpace(c.Id))
                .GroupBy(c => c.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.CustomerId)) continue;
                if (!byId.TryGetValue(row.CustomerId.Trim(), out var cust)) continue;
                if (!string.IsNullOrWhiteSpace(cust.EnglishOfficialName))
                    row.CustomerEnglishName = cust.EnglishOfficialName.Trim();
                if (string.IsNullOrWhiteSpace(row.CustomerCode) && !string.IsNullOrWhiteSpace(cust.CustomerCode))
                    row.CustomerCode = cust.CustomerCode.Trim();
            }
        }

        private async Task HydrateSellOrderLineListSalesLoginAsync(
            List<SellOrderItemLineDto> rows,
            Dictionary<string, SellOrder> orderDict)
        {
            if (rows.Count == 0) return;
            var users = (await _userService.GetAllAsync())
                .ToDictionary(u => u.Id, u => u, StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                if (!orderDict.TryGetValue(row.SellOrderId, out var o)) continue;
                if (string.IsNullOrWhiteSpace(o.SalesUserId)) continue;
                if (!users.TryGetValue(o.SalesUserId.Trim(), out var u)) continue;
                var login = EntityLookupService.FormatUserLoginName(u);
                if (!string.IsNullOrWhiteSpace(login))
                    row.SalesUserName = login;
            }
        }

        public async Task<IEnumerable<SellOrder>> GetByCustomerIdAsync(string customerId)
        {
            var all = await _soRepo.GetAllAsync();
            return all.Where(o => o.CustomerId == customerId);
        }

        public async Task<SellOrder> UpdateAsync(string id, UpdateSalesOrderRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID不能为空");
            var order = await _soRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"销售订单 {id} 不存在");

            SellOrderHeaderRemarkCodec.TryMaterializeFromLegacyComment(order);
            var headerBefore = CaptureSalesOrderHeaderSnapshot(order);
            if (HeaderRemarksTouched(request))
                PatchSalesOrderHeaderRemarksFromRequest(order, request);

            if (!string.IsNullOrWhiteSpace(request.CustomerId))
            {
                var newCustomerId = request.CustomerId.Trim();
                if (!string.Equals(order.CustomerId?.Trim(), newCustomerId, StringComparison.OrdinalIgnoreCase))
                {
                    // 方案 A：换客户时头+未完结下游同一次 SaveChanges；阻断则整单失败
                    if (_customerDownstreamSyncService != null)
                    {
                        await _customerDownstreamSyncService.ApplyAsync(
                            order,
                            actingUserId,
                            newCustomerId,
                            saveChanges: false);
                    }
                    else
                    {
                        await ApplyCustomerHeaderFromMasterAsync(order, newCustomerId);
                    }
                }
                else if (request.CustomerName != null)
                    order.CustomerName = request.CustomerName.Trim();
            }
            else if (request.CustomerName != null)
            {
                order.CustomerName = request.CustomerName;
            }

            if (request.SalesUserId != null) order.SalesUserId = request.SalesUserId;
            if (request.SalesUserName != null) order.SalesUserName = request.SalesUserName;
            if (request.Assistor != null) order.Assistor = NormalizeOptionalUserId(request.Assistor);
            if (request.Type.HasValue) order.Type = request.Type.Value;
            if (request.Currency.HasValue) order.Currency = request.Currency.Value;
            if (request.DeliveryDate.HasValue) order.DeliveryDate = PostgreSqlDateTime.ToUtc(request.DeliveryDate.Value);
            if (request.DeliveryAddress != null) order.DeliveryAddress = request.DeliveryAddress;

            var replacedItemCount = 0;
            List<SellOrderItem>? insertedLines = null;
            List<SellOrderItem>? updatedLines = null;
            List<SellOrderItem>? deletedLines = null;
            SellOrderItemSyncResult? syncResult = null;
            if (request.Items != null && request.Items.Count > 0)
            {
                var effectiveType = request.Type ?? order.Type;
                SellOrderItemLinkRules.ValidateCustomerOrderItems(
                    effectiveType,
                    request.Items.Select(i => i.QuoteId).ToList());

                syncResult = await SyncSellOrderItemsOnUpdateAsync(order, id, request.Items, actingUserId);
                insertedLines = syncResult.Inserted;
                updatedLines = syncResult.Updated;
                deletedLines = syncResult.Deleted;
                order.Total = syncResult.Total;
                order.ConvertTotal = syncResult.ConvertTotal;
                order.ItemRows = request.Items.Count;
                replacedItemCount = request.Items.Count;
            }

            order.ModifyTime = DateTime.UtcNow;
            order.ModifyByUserId = NormalizeActingUserId(actingUserId);
            await _soRepo.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            await LogSalesOrderHeaderChangesAsync(order, headerBefore, actingUserId);

            if (syncResult != null)
            {
                foreach (var audit in syncResult.ItemUpdateAudits)
                    await LogSellOrderItemFieldChangesAsync(audit.Item, audit.Before, actingUserId);
                foreach (var ins in syncResult.Inserted)
                    await LogSellOrderItemAddedAsync(ins, actingUserId);
            }

            if (deletedLines is { Count: > 0 })
            {
                await AppendSellOrderItemDeleteOperationLogsAsync(
                    order,
                    deletedLines,
                    actingUserId,
                    OperationLogActionTypes.SellOrderItemDelete,
                    $"编辑销售订单 {order.SellOrderCode} 时删除明细行");
            }

            var quoteIdsToSync = CollectQuoteIds(deletedLines ?? [])
                .Concat(CollectQuoteIds(insertedLines ?? []))
                .Concat(CollectQuoteIds(updatedLines ?? []));
            await _quoteStatusSync.ReconcileQuotesAfterSalesOrderChangeAsync(quoteIdsToSync);
            if (insertedLines is { Count: > 0 })
                await _quoteStatusSync.MarkQuotesWonAsync(CollectQuoteIds(insertedLines));

            var touchedLines = new List<SellOrderItem>();
            if (insertedLines != null) touchedLines.AddRange(insertedLines);
            if (updatedLines != null) touchedLines.AddRange(updatedLines);
            if (touchedLines.Count > 0)
            {
                foreach (var line in touchedLines)
                    await _soItemExtendSync.RecalculateAsync(line.Id);
                await TryRefreshPurchasedStockAvailableForSellLinesAsync(touchedLines);
                await _unitOfWork.SaveChangesAsync();
            }

            if (replacedItemCount > 0 && insertedLines is { Count: > 0 })
            {
                var t = DateTime.UtcNow;
                await _orderJourneyLog.AppendAsync(new OrderJourneyLog
                {
                    EntityKind = OrderJourneyEntityKinds.SellOrder,
                    EntityId = order.Id,
                    DocumentCode = order.SellOrderCode,
                    EventCode = OrderJourneyEventCodes.SoUpdated,
                    EventTime = t,
                    Amount = order.Total,
                    Currency = order.Currency,
                    PayloadJson = $"{{\"itemRows\":{replacedItemCount}}}",
                    ActorKind = OrderJourneyActorKinds.System,
                    Source = nameof(SalesOrderService)
                });
                foreach (var line in insertedLines)
                {
                    var lineTotal = Math.Round(line.Qty * line.Price, 2, MidpointRounding.AwayFromZero);
                    await _orderJourneyLog.AppendAsync(new OrderJourneyLog
                    {
                        EntityKind = OrderJourneyEntityKinds.SellOrderItem,
                        EntityId = line.Id,
                        ParentEntityKind = OrderJourneyEntityKinds.SellOrder,
                        ParentEntityId = order.Id,
                        DocumentCode = order.SellOrderCode,
                        LineHint = JourneyLineHint(line.PN, line.Brand),
                        EventCode = OrderJourneyEventCodes.SoItemCreated,
                        EventTime = t,
                        Quantity = line.Qty,
                        Amount = lineTotal,
                        Currency = line.Currency,
                        ActorKind = OrderJourneyActorKinds.System,
                        Source = nameof(SalesOrderService)
                    });
                }
            }

            return order;
        }

        public async Task DeleteAsync(string id, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID不能为空");
            var order = await _soRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"销售订单 {id} 不存在");

            var (actorId, actorName) = await ResolveActorAsync(actingUserId);
            var itemsToDelete = (await _soItemRepo.GetAllAsync())
                .Where(i => i.SellOrderId == id)
                .ToList();

            foreach (var item in itemsToDelete)
            {
                item.IsDeleted = true;
                item.ModifyTime = DateTime.UtcNow;
                item.DeletedByUserId = actorId;
                item.DeletedByUserName = actorName;
                await _soItemRepo.UpdateAsync(item);
                await _soItemExtendRepo.DeleteAsync(item.Id);
            }

            await SoftDeleteSellOrderExtendAsync(id);

            order.IsDeleted = true;
            order.ModifyTime = DateTime.UtcNow;
            order.ModifyByUserId = NormalizeActingUserId(actingUserId);
            await _soRepo.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            await _orderJourneyLog.AppendAsync(new OrderJourneyLog
            {
                EntityKind = OrderJourneyEntityKinds.SellOrder,
                EntityId = order.Id,
                DocumentCode = order.SellOrderCode,
                EventCode = OrderJourneyEventCodes.SoDeleted,
                EventTime = DateTime.UtcNow,
                ActorKind = OrderJourneyActorKinds.System,
                Source = nameof(SalesOrderService)
            });

            await AppendSellOrderWholeDeleteOperationLogsAsync(order, itemsToDelete, actingUserId);

            await _quoteStatusSync.ReconcileQuotesAfterSalesOrderChangeAsync(CollectQuoteIds(itemsToDelete));
        }

        public async Task UpdateStatusAsync(string id, SellOrderMainStatus status, string? auditRemark = null, string? actingUserId = null)
        {
            if (!Enum.IsDefined(typeof(SellOrderMainStatus), status))
                throw new ArgumentException($"无效的销售订单主状态: {(short)status}", nameof(status));

            var order = await _soRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"销售订单 {id} 不存在");

            if (status == SellOrderMainStatus.PendingAudit && order.Status != SellOrderMainStatus.New)
                throw new InvalidOperationException("仅「新建」状态可提交审核");

            if (status == SellOrderMainStatus.Approved && order.Status != SellOrderMainStatus.PendingAudit)
                throw new InvalidOperationException("仅「待审核」状态可审核通过");

            if (status == SellOrderMainStatus.AuditFailed && order.Status != SellOrderMainStatus.PendingAudit)
                throw new InvalidOperationException("仅「待审核」状态可审核拒绝");

            var fromStatus = (short)order.Status;
            var statusBefore = fromStatus;
            order.Status = status;
            if (status == SellOrderMainStatus.AuditFailed && !string.IsNullOrWhiteSpace(auditRemark))
                order.AuditRemark = auditRemark.Trim();
            else if (status == SellOrderMainStatus.Approved)
                order.AuditRemark = null;

            order.ModifyTime = DateTime.UtcNow;
            order.ModifyByUserId = NormalizeActingUserId(actingUserId);
            await _soRepo.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            if (statusBefore != (short)status)
            {
                await AddSalesOrderFieldChangeLogAsync(
                    order,
                    "status",
                    "订单状态",
                    FormatSellOrderStatus(statusBefore),
                    FormatSellOrderStatus((short)status),
                    actingUserId);
            }

            await _orderJourneyLog.AppendAsync(new OrderJourneyLog
            {
                EntityKind = OrderJourneyEntityKinds.SellOrder,
                EntityId = order.Id,
                DocumentCode = order.SellOrderCode,
                EventCode = OrderJourneyEventCodes.SoStatusChanged,
                FromState = fromStatus.ToString(),
                ToState = ((short)status).ToString(),
                EventTime = DateTime.UtcNow,
                Remark = auditRemark,
                ActorKind = OrderJourneyActorKinds.System,
                Source = nameof(SalesOrderService)
            });

            if (status == SellOrderMainStatus.Cancelled)
            {
                var orderItems = (await _soItemRepo.GetAllAsync())
                    .Where(i => i.SellOrderId == id && !i.IsDeleted)
                    .ToList();
                await _quoteStatusSync.ReconcileQuotesAfterSalesOrderChangeAsync(CollectQuoteIds(orderItems));
            }
        }

        public async Task RequestStockOutAsync(string id, string requestedBy)
        {
            // 申请出库后进入「进行中」；完成=Completed 由全部明细收款完成自动同步
            await UpdateStatusAsync(id, SellOrderMainStatus.InProgress, null, NormalizeActingUserId(requestedBy));
        }

        public async Task<IEnumerable<object>> GetRelatedPurchaseOrdersAsync(string sellOrderId)
        {
            var soItems = await _soItemRepo.GetAllAsync();
            var sellItemIds = soItems.Where(i => i.SellOrderId == sellOrderId)
                                     .Select(i => i.Id).ToHashSet();
            var poItems = await _poItemRepo.GetAllAsync();
            var relatedPoIds = poItems.Where(i => i.SellOrderItemId != null && sellItemIds.Contains(i.SellOrderItemId))
                                       .Select(i => i.PurchaseOrderId).Distinct().ToList();
            var allPo = await _poRepo.GetAllAsync();
            return allPo.Where(p => relatedPoIds.Contains(p.Id)).Cast<object>();
        }

        public async Task<PagedResult<SellOrderItemLineDto>> GetSellOrderItemLinesPagedAsync(SellOrderItemLineQueryRequest request)
        {
            var pageResult = await _salesOrderItemLineListQuery.GetPagedAsync(request, CancellationToken.None);
            var list = pageResult.Items.ToList();
            if (list.Count == 0)
                return pageResult;

            var orderIds = list
                .Select(x => x.SellOrderId)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var orders = (await _soRepo.FindAsync(o => orderIds.Contains(o.Id))).ToList();
            var orderDict = orders.ToDictionary(o => o.Id, StringComparer.OrdinalIgnoreCase);

            await HydrateSellOrderLineListSalesLoginAsync(list, orderDict);
            await HydrateSellOrderLineListCustomerEnglishAsync(list);
            await EnrichSellOrderItemLineListAsync(list);

            return new PagedResult<SellOrderItemLineDto>
            {
                Items = list,
                TotalCount = pageResult.TotalCount,
                PageIndex = pageResult.PageIndex,
                PageSize = pageResult.PageSize
            };
        }

        public async Task<List<SellOrderItemLineDto>> GetSellOrderItemLinesByIdsAsync(
            IReadOnlyList<string> sellOrderItemIds,
            CancellationToken cancellationToken = default)
        {
            var list = await _salesOrderItemLineListQuery.GetByIdsAsync(sellOrderItemIds, cancellationToken);
            if (list.Count == 0)
                return list;

            var orderIds = list
                .Select(x => x.SellOrderId)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var orders = (await _soRepo.FindAsync(o => orderIds.Contains(o.Id))).ToList();
            var orderDict = orders.ToDictionary(o => o.Id, StringComparer.OrdinalIgnoreCase);

            await HydrateSellOrderLineListSalesLoginAsync(list, orderDict);
            await HydrateSellOrderLineListCustomerEnglishAsync(list);
            await EnrichSellOrderItemLineListAsync(list);

            return list;
        }

        /// <summary>扩展表、出库门闸、剩余可采等（仅当前页行）。</summary>
        private async Task EnrichSellOrderItemLineListAsync(List<SellOrderItemLineDto> list)
        {
            if (list.Count == 0)
                return;

            try
            {
                var lineIds = list.Select(x => x.SellOrderItemId).Distinct().ToList();
                var extendRows = (await _soItemExtendRepo.FindAsync(e => lineIds.Contains(e.Id))).ToList();
                var extById = extendRows.ToDictionary(e => e.Id, e => e, StringComparer.OrdinalIgnoreCase);
                foreach (var row in list)
                {
                    if (!extById.TryGetValue(row.SellOrderItemId, out var ext))
                        continue;
                    row.PurchaseProgressStatus = ext.PurchaseProgressStatus;
                    row.StockInProgressStatus = ext.StockInProgressStatus;
                    row.StockOutProgressStatus = ext.StockOutProgressStatus;
                    var notifyQty = ext.QtyStockOutNotify;
                    if (notifyQty <= 0m)
                        row.StockOutNotifyProgressStatus = 0;
                    else if (notifyQty + 1e-9m >= row.Qty)
                        row.StockOutNotifyProgressStatus = 2;
                    else
                        row.StockOutNotifyProgressStatus = 1;
                    row.ReceiptProgressStatus = ext.ReceiptProgressStatus;
                    row.InvoiceProgressStatus = ext.InvoiceProgressStatus;
                    row.SalesProfitExpected = ext.SalesProfitExpected;
                    row.ProfitOutBizUsd = ext.ProfitOutBizUsd;
                    row.ProfitOutRateBiz = SellOrderItemProfitDisplay.ResolveProfitOutRateBizForDisplay(
                        ext.ProfitOutRateBiz,
                        ext.ProfitOutBizUsd);
                    row.PurchasedStockAvailableQty = ext.PurchasedStock_AvailableQty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[SellLineStockOutSync] GetSellOrderItemLinesPagedAsync merge sellorderitemextend failed; progress columns left at default 0. LineIdCount={Count}",
                    list.Count);
            }

            try
            {
                var gate = await GetStockOutApplyPurchaseGateDetailsBySellLineIdsAsync(list.Select(x => x.SellOrderItemId));
                foreach (var row in list)
                {
                    var key = row.SellOrderItemId?.Trim() ?? string.Empty;
                    if (!string.IsNullOrEmpty(key) && gate.TryGetValue(key, out var detail))
                    {
                        row.StockOutApplyPurchaseGateDetail = detail;
                        row.StockOutApplyPurchaseGateOk = detail.Ok;
                    }
                    else
                    {
                        row.StockOutApplyPurchaseGateOk = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[SellLineStockOutGate] GetSellOrderItemLinesPagedAsync merge stock-out apply purchase gate failed; gate columns left at default. LineIdCount={Count}",
                    list.Count);
                foreach (var row in list)
                    row.StockOutApplyPurchaseGateOk = false;
            }

            try
            {
                var idsForQty = list
                    .Select(x => x.SellOrderItemId)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (idsForQty.Count > 0)
                {
                    var poItemsForQty = (await _poItemRepo.FindAsync(i => i.SellOrderItemId != null && idsForQty.Contains(i.SellOrderItemId!)))
                        .ToList();
                    var purchasedByLine = poItemsForQty
                        .Where(i => !string.IsNullOrWhiteSpace(i.SellOrderItemId))
                        .GroupBy(i => i.SellOrderItemId!.Trim(), StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty), StringComparer.OrdinalIgnoreCase);

                    var prForQty = (await _prRepo.FindAsync(r => idsForQty.Contains(r.SellOrderItemId)))
                        .ToList();
                    var openPrByLine = prForQty
                        .Where(r => r.Status == 0 || r.Status == 1)
                        .Where(r => !string.IsNullOrWhiteSpace(r.SellOrderItemId))
                        .GroupBy(r => r.SellOrderItemId.Trim(), StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty), StringComparer.OrdinalIgnoreCase);

                    foreach (var row in list)
                    {
                        var id = row.SellOrderItemId?.Trim() ?? string.Empty;
                        if (string.IsNullOrEmpty(id))
                        {
                            row.PurchaseRemainingQty = 0m;
                            continue;
                        }

                        var purchased = purchasedByLine.TryGetValue(id, out var pv) ? pv : 0m;
                        var openPr = openPrByLine.TryGetValue(id, out var ov) ? ov : 0m;
                        row.PurchaseRemainingQty = row.Qty - purchased - openPr;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[SellLinePurchaseRemaining] GetSellOrderItemLinesPagedAsync merge purchase remaining qty failed; PurchaseRemainingQty left unset. LineIdCount={Count}",
                    list.Count);
            }

            try
            {
                await EnrichPurchaseUserAccountDisplayAsync(list);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[SellLinePurchaseUser] merge purchase user account display failed; column left empty. LineIdCount={Count}",
                    list.Count);
            }
        }

        /// <summary>按有效 PO 主表采购员汇总登录账号（PO 创建时间升序、去重、中文逗号拼接）。</summary>
        private async Task EnrichPurchaseUserAccountDisplayAsync(List<SellOrderItemLineDto> list)
        {
            if (list.Count == 0)
                return;

            var lineIds = list
                .Select(x => x.SellOrderItemId)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (lineIds.Count == 0)
                return;

            var poItems = (await _poItemRepo.FindAsync(i =>
                    i.SellOrderItemId != null && lineIds.Contains(i.SellOrderItemId)))
                .Where(i => PurchaseRequisitionPoLinkHelper.IsActivePoItem(i.Status))
                .ToList();
            if (poItems.Count == 0)
                return;

            var poIds = poItems
                .Select(i => i.PurchaseOrderId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (poIds.Count == 0)
                return;

            var poHeaders = (await _poRepo.FindAsync(p => poIds.Contains(p.Id)))
                .Where(p => PurchaseRequisitionPoLinkHelper.IsActivePurchaseOrderHeader(p.Status))
                .ToDictionary(p => p.Id, p => p, StringComparer.OrdinalIgnoreCase);
            if (poHeaders.Count == 0)
                return;

            var purchasersByLine = new Dictionary<string, List<(DateTime CreateTime, string UserId)>>(StringComparer.OrdinalIgnoreCase);
            foreach (var poi in poItems)
            {
                var sellId = poi.SellOrderItemId?.Trim();
                if (string.IsNullOrEmpty(sellId))
                    continue;
                if (!poHeaders.TryGetValue(poi.PurchaseOrderId, out var po))
                    continue;
                if (string.IsNullOrWhiteSpace(po.PurchaseUserId))
                    continue;

                var userId = po.PurchaseUserId.Trim();
                if (!purchasersByLine.TryGetValue(sellId, out var bucket))
                {
                    bucket = new List<(DateTime, string)>();
                    purchasersByLine[sellId] = bucket;
                }

                bucket.Add((po.CreateTime, userId));
            }

            if (purchasersByLine.Count == 0)
                return;

            var userIds = purchasersByLine.Values
                .SelectMany(x => x.Select(t => t.UserId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var accountByUserId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var userId in userIds)
            {
                var user = await _userService.GetByIdAsync(userId);
                var account = EntityLookupService.FormatUserLoginName(user);
                if (!string.IsNullOrWhiteSpace(account))
                    accountByUserId[userId] = account;
            }

            foreach (var row in list)
            {
                var sellId = row.SellOrderItemId?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(sellId) || !purchasersByLine.TryGetValue(sellId, out var entries))
                    continue;

                var accounts = entries
                    .OrderBy(e => e.CreateTime)
                    .ThenBy(e => e.UserId, StringComparer.Ordinal)
                    .Select(e => e.UserId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(uid => accountByUserId.TryGetValue(uid, out var acct) ? acct : null)
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .Cast<string>()
                    .ToList();

                if (accounts.Count > 0)
                    row.PurchaseUserAccountDisplay = string.Join("，", accounts);
            }
        }

        public Task<SalesOrderItemExtendRefreshResult> RefreshItemExtendsAsync(
            string salesOrderId,
            CancellationToken cancellationToken = default,
            string? actingUserId = null)
            => RefreshDownstreamAsync(
                salesOrderId,
                SalesOrderRefreshFacet.Price,
                cancellationToken,
                actingUserId,
                confirmCompleted: true);

        public async Task<SalesOrderItemExtendRefreshResult> RefreshDownstreamAsync(
            string salesOrderId,
            SalesOrderRefreshFacet facet,
            CancellationToken cancellationToken = default,
            string? actingUserId = null,
            bool confirmCompleted = false)
        {
            if (string.IsNullOrWhiteSpace(salesOrderId))
                throw new ArgumentException("销售订单ID不能为空", nameof(salesOrderId));

            if (facet == SalesOrderRefreshFacet.Customer)
                return await MapCustomerRefreshResultAsync(salesOrderId, actingUserId, confirmCompleted, cancellationToken);

            if (_refreshCompletedGate != null && facet != SalesOrderRefreshFacet.Status)
                await _refreshCompletedGate.EnsureAllowedAsync(
                    salesOrderId.Trim(),
                    facet,
                    confirmCompleted,
                    cancellationToken);

            var orderId = salesOrderId.Trim();
            var order = await _soRepo.GetByIdAsync(orderId)
                ?? throw new InvalidOperationException($"销售订单 {orderId} 不存在");

            var items = (await _soItemRepo.FindAsync(x => x.SellOrderId == orderId)).ToList();
            var result = new SalesOrderItemExtendRefreshResult
            {
                Facet = facet.ToApiValue(),
                SalesOrderId = orderId,
                TotalItems = items.Count,
                RefreshedAt = DateTime.UtcNow
            };

            SalesOrderSalesPriceDownstreamSyncResult? priceSync = null;
            SalesOrderIdentityDownstreamSyncResult? identitySync = null;

            if (facet == SalesOrderRefreshFacet.Price
                && _salesPriceDownstreamSync != null
                && items.Count > 0)
            {
                priceSync = await _salesPriceDownstreamSync.ApplyAsync(items, cancellationToken);
                ApplyPriceSyncToResult(result, priceSync);
            }
            else if ((facet == SalesOrderRefreshFacet.Pn || facet == SalesOrderRefreshFacet.Brand)
                && _identityDownstreamSync != null
                && items.Count > 0)
            {
                var field = facet == SalesOrderRefreshFacet.Pn
                    ? SalesOrderIdentitySnapshotField.Pn
                    : SalesOrderIdentitySnapshotField.Brand;
                identitySync = await _identityDownstreamSync.ApplyAsync(items, field, cancellationToken);
                result.StockOutNotifiesUpdated = identitySync.StockOutNotifiesUpdated;
                result.PackingItemsUpdated = identitySync.PackingItemsUpdated;
                result.PackingItemExtendsUpdated = identitySync.PackingItemExtendsUpdated;
                result.ReceivablesUpdated = identitySync.ReceivablesUpdated;
                result.IdentityChanges = identitySync.Changes;
            }
            else if (facet == SalesOrderRefreshFacet.Qty)
            {
                foreach (var item in items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    result.StockOutNotifiesUpdated +=
                        await _soItemExtendSync.SyncStockOutNotifyPlanQtyAsync(item.Id, cancellationToken);
                }
            }

            var recalcOptions = facet is SalesOrderRefreshFacet.Pn or SalesOrderRefreshFacet.Brand
                ? null
                : SellOrderItemRecalculateOptions.StatusOnly;

            if (recalcOptions != null)
            {
                foreach (var item in items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var before = await BuildRefreshSnapshotAsync(item.Id);
                    await _soItemExtendSync.RecalculateAsync(item.Id, recalcOptions, cancellationToken);
                    var after = await BuildRefreshSnapshotAsync(item.Id);
                    var fields = BuildFieldChanges(before, after);
                    if (fields.Count == 0) continue;

                    result.Changes.Add(new SalesOrderItemExtendChangeDto
                    {
                        SellOrderItemId = item.Id,
                        SellOrderItemCode = item.SellOrderItemCode,
                        Fields = fields
                    });
                    result.ChangedFieldsCount += fields.Count;
                    if (fields.Any(f => string.Equals(f.Field, "stockOutNotifyProgressStatus", StringComparison.Ordinal)))
                        result.SyncedStockOutNotifyStatusCount += 1;
                }

                if (await _mainStatusSync.TrySyncOrderMainStatusAsync(orderId, cancellationToken))
                    result.ChangedFieldsCount += 1;
            }

            result.ChangedItems = result.Changes.Count;
            await _unitOfWork.SaveChangesAsync();

            await AppendFacetRefreshLogAsync(order, facet, result, priceSync, identitySync, actingUserId, cancellationToken);

            _logger.LogInformation(
                "SO分面刷新完成: Facet={Facet} SalesOrderId={SalesOrderId} Code={Code} TotalItems={TotalItems} ChangedItems={ChangedItems} ChangedFields={ChangedFields} Packing={Packing} StockItem={StockItem} StockOutExt={StockOutExt} StockOutHead={StockOutHead} Receivable={Receivable}",
                result.Facet,
                orderId,
                order.SellOrderCode,
                result.TotalItems,
                result.ChangedItems,
                result.ChangedFieldsCount,
                result.PackingItemExtendsUpdated,
                result.StockItemsUpdated,
                result.StockOutItemExtendsUpdated,
                result.StockOutHeadersUpdated,
                result.ReceivablesUpdated);
            return result;
        }

        public async Task<SalesOrderRefreshCompletedPreview> PreviewRefreshDownstreamAsync(
            string salesOrderId,
            SalesOrderRefreshFacet facet,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(salesOrderId))
                throw new ArgumentException("销售订单ID不能为空", nameof(salesOrderId));

            if (facet == SalesOrderRefreshFacet.Status)
            {
                return new SalesOrderRefreshCompletedPreview
                {
                    Facet = facet.ToApiValue(),
                    CanProceed = true,
                    AllowCompletedParam = true
                };
            }

            if (facet == SalesOrderRefreshFacet.Customer)
            {
                if (_customerDownstreamSyncService == null)
                    throw new InvalidOperationException("客户同步服务未配置");
                var customerPreview = await _customerDownstreamSyncService.PreviewAsync(
                    salesOrderId.Trim(),
                    cancellationToken: cancellationToken);
                return new SalesOrderRefreshCompletedPreview
                {
                    Facet = facet.ToApiValue(),
                    CanProceed = customerPreview.CanSync,
                    BlockReason = customerPreview.BlockReason,
                    AllowCompletedParam = customerPreview.AllowCompletedParam,
                    CompletedDocuments = customerPreview.CompletedDocuments.ToList()
                };
            }

            if (_refreshCompletedGate == null)
            {
                return new SalesOrderRefreshCompletedPreview
                {
                    Facet = facet.ToApiValue(),
                    CanProceed = true,
                    AllowCompletedParam = true
                };
            }

            return await _refreshCompletedGate.PreviewAsync(salesOrderId.Trim(), facet, cancellationToken);
        }

        private async Task<SalesOrderItemExtendRefreshResult> MapCustomerRefreshResultAsync(
            string salesOrderId,
            string? actingUserId,
            bool confirmCompleted,
            CancellationToken cancellationToken)
        {
            if (_customerDownstreamSyncService == null)
                throw new InvalidOperationException("客户同步服务未配置");

            var order = await _soRepo.GetByIdAsync(salesOrderId.Trim())
                ?? throw new InvalidOperationException($"销售订单 {salesOrderId} 不存在");
            var apply = await _customerDownstreamSyncService.ApplyAsync(
                order,
                actingUserId,
                saveChanges: true,
                cancellationToken: cancellationToken,
                confirmCompleted: confirmCompleted);
            return new SalesOrderItemExtendRefreshResult
            {
                Facet = SalesOrderRefreshFacet.Customer.ToApiValue(),
                SalesOrderId = apply.Preview.SalesOrderId,
                TotalItems = 1,
                ChangedItems = apply.Applied ? 1 : 0,
                ChangedFieldsCount = apply.Applied ? 1 : 0,
                OldCustomerName = apply.Preview.OldCustomerName,
                NewCustomerName = apply.Preview.CustomerName,
                RefreshedAt = DateTime.UtcNow
            };
        }

        private static void ApplyPriceSyncToResult(
            SalesOrderItemExtendRefreshResult result,
            SalesOrderSalesPriceDownstreamSyncResult priceSync)
        {
            result.PackingItemExtendsUpdated = priceSync.PackingItemExtendsUpdated;
            result.StockItemsUpdated = priceSync.StockItemsUpdated;
            result.StockOutItemExtendsUpdated = priceSync.StockOutItemExtendsUpdated;
            result.StockOutHeadersUpdated = priceSync.StockOutHeadersUpdated;
            result.ReceivablesUpdated = priceSync.ReceivablesUpdated;
            result.SalesPriceLineChanges = priceSync.LineChanges;
            result.ReceivableWarnings = priceSync.ReceivableWarnings;
        }

        private async Task AppendFacetRefreshLogAsync(
            SellOrder order,
            SalesOrderRefreshFacet facet,
            SalesOrderItemExtendRefreshResult result,
            SalesOrderSalesPriceDownstreamSyncResult? priceSync,
            SalesOrderIdentityDownstreamSyncResult? identitySync,
            string? actingUserId,
            CancellationToken cancellationToken)
        {
            if (facet == SalesOrderRefreshFacet.Price)
            {
                if (priceSync is { HasUpdates: true })
                    await AppendSalesPriceRefreshLogAsync(order, priceSync, actingUserId, cancellationToken);
                return;
            }

            var hasWork = result.ChangedItems > 0
                || result.ChangedFieldsCount > 0
                || result.StockOutNotifiesUpdated > 0
                || result.PackingItemsUpdated > 0
                || result.PackingItemExtendsUpdated > 0
                || result.ReceivablesUpdated > 0
                || (identitySync?.HasUpdates ?? false);
            if (!hasWork)
                return;

            var actionType = facet switch
            {
                SalesOrderRefreshFacet.Pn => OperationLogActionTypes.SellOrderRefreshPn,
                SalesOrderRefreshFacet.Brand => OperationLogActionTypes.SellOrderRefreshBrand,
                SalesOrderRefreshFacet.Qty => OperationLogActionTypes.SellOrderRefreshQty,
                _ => OperationLogActionTypes.SellOrderRefreshStatus
            };

            string? operatorName = null;
            var actor = string.IsNullOrWhiteSpace(actingUserId) ? null : actingUserId.Trim();
            if (!string.IsNullOrEmpty(actor))
            {
                var user = await _userService.GetByIdAsync(actor);
                operatorName = string.IsNullOrWhiteSpace(user?.RealName) ? user?.UserName : user!.RealName;
            }

            var desc = facet switch
            {
                SalesOrderRefreshFacet.Pn =>
                    $"覆盖下游物料型号快照。出库通知 {result.StockOutNotifiesUpdated}、装箱 {result.PackingItemsUpdated}、装箱扩展 {result.PackingItemExtendsUpdated}、应收 {result.ReceivablesUpdated}。",
                SalesOrderRefreshFacet.Brand =>
                    $"覆盖下游品牌快照。出库通知 {result.StockOutNotifiesUpdated}、装箱 {result.PackingItemsUpdated}、装箱扩展 {result.PackingItemExtendsUpdated}、应收 {result.ReceivablesUpdated}。",
                SalesOrderRefreshFacet.Qty =>
                    $"按销售行数量对齐出库通知计划量（仅收缩超量单条未出库通知），并重算进度。出库通知 {result.StockOutNotifiesUpdated} 条。不改已出库通知与实出数量。",
                _ =>
                    $"重算销售明细派生状态与进度。变更明细 {result.ChangedItems}、字段 {result.ChangedFieldsCount}；出库通知状态 {result.SyncedStockOutNotifyStatusCount}。"
            };

            var extraInfo = JsonSerializer.Serialize(new
            {
                facet = result.Facet,
                stockOutNotifies = result.StockOutNotifiesUpdated,
                packingItems = result.PackingItemsUpdated,
                packingItemExtends = result.PackingItemExtendsUpdated,
                receivables = result.ReceivablesUpdated,
                identityChanges = result.IdentityChanges,
                changedItems = result.ChangedItems,
                changedFields = result.ChangedFieldsCount
            });

            await _logOperationAppend.AppendAsync(
                BusinessLogTypes.SalesOrder,
                order.Id,
                order.SellOrderCode,
                actionType,
                actor,
                operatorName,
                desc,
                null,
                extraInfo,
                cancellationToken);
        }

        private async Task AppendSalesPriceRefreshLogAsync(
            SellOrder order,
            SalesOrderSalesPriceDownstreamSyncResult priceSync,
            string? actingUserId,
            CancellationToken cancellationToken)
        {
            string? operatorName = null;
            var actor = string.IsNullOrWhiteSpace(actingUserId) ? null : actingUserId.Trim();
            if (!string.IsNullOrEmpty(actor))
            {
                var user = await _userService.GetByIdAsync(actor);
                operatorName = string.IsNullOrWhiteSpace(user?.RealName) ? user?.UserName : user!.RealName;
            }

            var lineDesc = priceSync.LineChanges.Count == 0
                ? "无单价变化行"
                : string.Join("；", priceSync.LineChanges.Select(c =>
                    $"{c.SellOrderItemCode ?? c.SellOrderItemId}: {c.OldPrice}→{c.NewPrice}"));
            var desc =
                $"覆盖下游销售价快照。明细 {lineDesc}。装箱 {priceSync.PackingItemExtendsUpdated}、库存 {priceSync.StockItemsUpdated}、出库扩展 {priceSync.StockOutItemExtendsUpdated}、出库单头 {priceSync.StockOutHeadersUpdated}、应收 {priceSync.ReceivablesUpdated}。";
            if (priceSync.ReceivableWarnings.Count > 0)
                desc += $" 超额警告 {priceSync.ReceivableWarnings.Count} 条。";

            var extraInfo = JsonSerializer.Serialize(new
            {
                lines = priceSync.LineChanges,
                packingItemExtends = priceSync.PackingItemExtendsUpdated,
                stockItems = priceSync.StockItemsUpdated,
                stockOutItemExtends = priceSync.StockOutItemExtendsUpdated,
                stockOutHeaders = priceSync.StockOutHeadersUpdated,
                receivables = priceSync.ReceivablesUpdated,
                warnings = priceSync.ReceivableWarnings
            });

            await _logOperationAppend.AppendAsync(
                BusinessLogTypes.SalesOrder,
                order.Id,
                order.SellOrderCode,
                OperationLogActionTypes.SellOrderRefreshSalesPrice,
                actor,
                operatorName,
                desc,
                null,
                extraInfo,
                cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<string, bool>> GetStockOutApplyPurchaseGateBySellLineIdsAsync(
            IEnumerable<string> sellOrderItemIds)
        {
            var details = await GetStockOutApplyPurchaseGateDetailsBySellLineIdsAsync(sellOrderItemIds);
            return details.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.Ok,
                StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<string, StockOutApplyPurchaseGateDetailDto>>
            GetStockOutApplyPurchaseGateDetailsBySellLineIdsAsync(IEnumerable<string> sellOrderItemIds)
        {
            var idSet = sellOrderItemIds
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (idSet.Count == 0)
                return new Dictionary<string, StockOutApplyPurchaseGateDetailDto>(StringComparer.OrdinalIgnoreCase);

            var allPoItems = (await _poItemRepo.GetAllAsync()).ToList()
                .Where(i => !string.IsNullOrWhiteSpace(i.SellOrderItemId) &&
                            idSet.Contains(i.SellOrderItemId.Trim()))
                .ToList();
            var poIds = allPoItems
                .Select(i => i.PurchaseOrderId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var poById = (await _poRepo.GetAllAsync()).ToList()
                .Where(p => !string.IsNullOrWhiteSpace(p.Id) && poIds.Contains(p.Id.Trim()))
                .ToDictionary(p => p.Id.Trim(), p => p, StringComparer.OrdinalIgnoreCase);

            var min = PurchaseOrderMainStatusCodes.VendorConfirmedOrBeyond;
            var bySellLine = allPoItems
                .GroupBy(i => i.SellOrderItemId!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var result = new Dictionary<string, StockOutApplyPurchaseGateDetailDto>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in idSet)
            {
                var detail = new StockOutApplyPurchaseGateDetailDto();
                if (!bySellLine.TryGetValue(id, out var lines) || lines.Count == 0)
                {
                    detail.HasPoItems = false;
                    detail.Ok = false;
                    result[id] = detail;
                    continue;
                }

                detail.HasPoItems = true;
                var distinctPoIds = lines
                    .Select(l => l.PurchaseOrderId)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var blocking = new List<StockOutApplyPurchaseGateBlockingPoDto>();
                foreach (var pid in distinctPoIds)
                {
                    if (!poById.TryGetValue(pid, out var po))
                    {
                        blocking.Add(new StockOutApplyPurchaseGateBlockingPoDto
                        {
                            PurchaseOrderId = pid,
                            Missing = true
                        });
                        continue;
                    }

                    if (po.Status < min)
                    {
                        blocking.Add(new StockOutApplyPurchaseGateBlockingPoDto
                        {
                            PurchaseOrderId = pid,
                            OrderCode = po.PurchaseOrderCode,
                            Status = po.Status,
                            Missing = false
                        });
                    }
                }

                detail.BlockingPurchaseOrders = blocking;
                detail.Ok = blocking.Count == 0;
                result[id] = detail;
            }

            return result;
        }

        private async Task<SoItemRefreshSnapshot?> BuildRefreshSnapshotAsync(string sellOrderItemId)
        {
            var item = await _soItemRepo.GetByIdAsync(sellOrderItemId);
            if (item == null) return null;
            var ext = await _soItemExtendRepo.GetByIdAsync(sellOrderItemId);

            var usdUnit = item.ConvertPrice;
            var usdLine = Math.Round(item.Qty * usdUnit, 2, MidpointRounding.AwayFromZero);

            return new SoItemRefreshSnapshot
            {
                PurchaseProgressStatus = ext?.PurchaseProgressStatus ?? 0,
                StockInProgressStatus = ext?.StockInProgressStatus ?? 0,
                StockOutProgressStatus = ext?.StockOutProgressStatus ?? 0,
                ReceiptProgressStatus = ext?.ReceiptProgressStatus ?? 0,
                InvoiceProgressStatus = ext?.InvoiceProgressStatus ?? 0,
                UsdUnitPrice = usdUnit,
                UsdLineTotal = usdLine,
                SalesProfitExpected = ext?.SalesProfitExpected ?? 0m,
                ProfitOutBizUsd = ext?.ProfitOutBizUsd ?? 0m,
                ProfitOutRateBiz = ext?.ProfitOutRateBiz ?? 0m,
                QtyAlreadyPurchased = ext?.QtyAlreadyPurchased ?? 0m,
                QtyNotPurchase = ext?.QtyNotPurchase ?? 0m,
                QtyStockOutNotify = ext?.QtyStockOutNotify ?? 0m,
                QtyStockOutNotifyNot = ext?.QtyStockOutNotifyNot ?? 0m,
                QtyStockOutActual = ext?.QtyStockOutActual ?? 0m,
                InvoiceAmount = ext?.InvoiceAmount ?? 0m,
                InvoiceAmountNot = ext?.InvoiceAmountNot ?? 0m,
                InvoiceAmountFinish = ext?.InvoiceAmountFinish ?? 0m,
                PurchasedStockAvailableQty = ext?.PurchasedStock_AvailableQty ?? 0,
                ReceiptAmount = ext?.ReceiptAmount ?? 0m,
                ReceiptAmountNot = ext?.ReceiptAmountNot ?? 0m,
                ReceiptAmountFinish = ext?.ReceiptAmountFinish ?? 0m,
                PaymentAmount = ext?.PaymentAmount ?? 0m,
                PaymentAmountDone = ext?.PaymentAmountDone ?? 0m,
                PaymentAmountToBe = ext?.PaymentAmountToBe ?? 0m,
                PurchaseInvoiceAmount = ext?.PurchaseInvoiceAmount ?? 0m,
                PurchaseInvoiceDone = ext?.PurchaseInvoiceDone ?? 0m,
                PoCostUsdConfirmed = ext?.PoCostUsdConfirmed ?? 0m,
                ProfitOutFinUsd = ext?.ProfitOutFinUsd ?? 0m,
                ProfitOutRateFin = ext?.ProfitOutRateFin ?? 0m
            };
        }

        private static List<SalesOrderItemExtendFieldChangeDto> BuildFieldChanges(SoItemRefreshSnapshot? before, SoItemRefreshSnapshot? after)
        {
            before ??= new SoItemRefreshSnapshot();
            after ??= new SoItemRefreshSnapshot();
            var changes = new List<SalesOrderItemExtendFieldChangeDto>();
            AddShortField(changes, "purchaseProgressStatus", "采购状态", before.PurchaseProgressStatus, after.PurchaseProgressStatus);
            AddShortField(changes, "stockInProgressStatus", "入库状态", before.StockInProgressStatus, after.StockInProgressStatus);
            AddShortField(changes, "stockOutProgressStatus", "出库状态", before.StockOutProgressStatus, after.StockOutProgressStatus);
            AddShortField(changes, "receiptProgressStatus", "收款状态", before.ReceiptProgressStatus, after.ReceiptProgressStatus);
            AddShortField(changes, "invoiceProgressStatus", "开票状态", before.InvoiceProgressStatus, after.InvoiceProgressStatus);

            AddDecimalField(changes, "usdUnitPrice", "折算美金单价", before.UsdUnitPrice, after.UsdUnitPrice, 6);
            AddDecimalField(changes, "usdLineTotal", "折算美金总额", before.UsdLineTotal, after.UsdLineTotal, 2);
            AddDecimalField(changes, "salesProfitExpected", "预计销售利润", before.SalesProfitExpected, after.SalesProfitExpected, 2);
            AddDecimalField(changes, "profitOutBizUsd", "出库利润", before.ProfitOutBizUsd, after.ProfitOutBizUsd, 2);
            AddDecimalField(changes, "profitOutRateBiz", "利润率", before.ProfitOutRateBiz, after.ProfitOutRateBiz, 6);

            AddDecimalField(changes, "qtyAlreadyPurchased", "已采购数量", before.QtyAlreadyPurchased, after.QtyAlreadyPurchased, 4);
            AddDecimalField(changes, "qtyNotPurchase", "未采购数量", before.QtyNotPurchase, after.QtyNotPurchase, 4);
            AddDecimalField(changes, "qtyStockOutNotify", "已通知出库数量", before.QtyStockOutNotify, after.QtyStockOutNotify, 4);
            AddDecimalField(changes, "qtyStockOutNotifyNot", "待通知出库数量", before.QtyStockOutNotifyNot, after.QtyStockOutNotifyNot, 4);
            AddShortField(
                changes,
                "stockOutNotifyProgressStatus",
                "出库通知状态",
                ComputeStockOutNotifyProgressStatus(before.QtyStockOutNotify, before.QtyStockOutNotifyNot),
                ComputeStockOutNotifyProgressStatus(after.QtyStockOutNotify, after.QtyStockOutNotifyNot));
            AddDecimalField(changes, "qtyStockOutActual", "已实际出库数量", before.QtyStockOutActual, after.QtyStockOutActual, 4);
            AddDecimalField(changes, "invoiceAmount", "销项开票总额", before.InvoiceAmount, after.InvoiceAmount, 2);
            AddDecimalField(changes, "invoiceAmountNot", "待开票金额", before.InvoiceAmountNot, after.InvoiceAmountNot, 2);
            AddDecimalField(changes, "invoiceAmountFinish", "已开票金额", before.InvoiceAmountFinish, after.InvoiceAmountFinish, 2);
            AddIntField(changes, "purchasedStockAvailableQty", "采购备货可用量", before.PurchasedStockAvailableQty, after.PurchasedStockAvailableQty);
            AddDecimalField(changes, "receiptAmount", "应收金额", before.ReceiptAmount, after.ReceiptAmount, 2);
            AddDecimalField(changes, "receiptAmountNot", "待收金额", before.ReceiptAmountNot, after.ReceiptAmountNot, 2);
            AddDecimalField(changes, "receiptAmountFinish", "已收金额", before.ReceiptAmountFinish, after.ReceiptAmountFinish, 2);
            AddDecimalField(changes, "paymentAmount", "应付金额", before.PaymentAmount, after.PaymentAmount, 2);
            AddDecimalField(changes, "paymentAmountDone", "已付金额", before.PaymentAmountDone, after.PaymentAmountDone, 2);
            AddDecimalField(changes, "paymentAmountToBe", "待付金额", before.PaymentAmountToBe, after.PaymentAmountToBe, 2);
            AddDecimalField(changes, "purchaseInvoiceAmount", "进项发票总额", before.PurchaseInvoiceAmount, after.PurchaseInvoiceAmount, 2);
            AddDecimalField(changes, "purchaseInvoiceDone", "已开进项金额", before.PurchaseInvoiceDone, after.PurchaseInvoiceDone, 2);
            AddDecimalField(changes, "poCostUsdConfirmed", "已确认采购成本USD", before.PoCostUsdConfirmed, after.PoCostUsdConfirmed, 2);
            AddDecimalField(changes, "profitOutFinUsd", "出库利润(财务USD)", before.ProfitOutFinUsd, after.ProfitOutFinUsd, 2);
            AddDecimalField(changes, "profitOutRateFin", "出库利润率(财务)", before.ProfitOutRateFin, after.ProfitOutRateFin, 6);
            return changes;
        }

        private static short ComputeStockOutNotifyProgressStatus(decimal qtyNotify, decimal qtyNotifyNot)
        {
            if (qtyNotify <= 0m) return 0;
            if (qtyNotifyNot <= 0m) return 2;
            return 1;
        }

        private static void AddShortField(List<SalesOrderItemExtendFieldChangeDto> changes, string field, string label, short before, short after)
        {
            if (before == after) return;
            changes.Add(new SalesOrderItemExtendFieldChangeDto
            {
                Field = field,
                Label = label,
                Before = before.ToString(),
                After = after.ToString()
            });
        }

        private static void AddIntField(List<SalesOrderItemExtendFieldChangeDto> changes, string field, string label, int before, int after)
        {
            if (before == after) return;
            changes.Add(new SalesOrderItemExtendFieldChangeDto
            {
                Field = field,
                Label = label,
                Before = before.ToString(),
                After = after.ToString()
            });
        }

        private static void AddDecimalField(List<SalesOrderItemExtendFieldChangeDto> changes, string field, string label, decimal before, decimal after, int digits)
        {
            var b = decimal.Round(before, digits, MidpointRounding.AwayFromZero);
            var a = decimal.Round(after, digits, MidpointRounding.AwayFromZero);
            if (b == a) return;
            changes.Add(new SalesOrderItemExtendFieldChangeDto
            {
                Field = field,
                Label = label,
                Before = b.ToString($"F{digits}"),
                After = a.ToString($"F{digits}")
            });
        }

        private static string SqlQ(string? s) => (s ?? "").Replace("'", "''", StringComparison.Ordinal);

        public async Task<IReadOnlyList<SalesOrderFieldChangeLogDto>> GetFieldChangeLogsAsync(string sellOrderId)
        {
            if (string.IsNullOrWhiteSpace(sellOrderId))
                return Array.Empty<SalesOrderFieldChangeLogDto>();
            var safe = SqlQ(sellOrderId.Trim());
            var headerBiz = BusinessLogTypes.SalesOrder;
            var itemBiz = BusinessLogTypes.SellOrderItem;
            var sql = $@"
SELECT c.""Id"",
       '{safe}' AS ""SellOrderId"",
       so.sell_order_code AS ""SellOrderCode"",
       c.""FieldName"",
       c.""FieldLabel"",
       c.""OldValue"",
       c.""NewValue"",
       c.""ChangedByUserId"",
       c.""ChangedByUserName"",
       c.""ChangedAt"",
       CASE
         WHEN c.""BizType"" = '{headerBiz}' THEN '主表'
         ELSE COALESCE(NULLIF(TRIM(c.""RecordCode""), ''), '明细')
       END AS ""ObjectLabel""
FROM log_change_fldval c
LEFT JOIN sellorder so ON so.""SellOrderId"" = '{safe}'
WHERE (
    c.""BizType"" = '{headerBiz}' AND c.""RecordId"" = '{safe}'
) OR (
    c.""BizType"" = '{itemBiz}' AND c.""RecordId"" IN (
        SELECT i.""SellOrderItemId"" FROM sellorderitem i
        WHERE i.sell_order_id = '{safe}'
    )
)
ORDER BY c.""ChangedAt"" DESC";
            var rows = await _unitOfWork.QueryAsync<SalesOrderFieldChangeLogDto>(sql);
            return rows.ToList();
        }

        public async Task<IReadOnlyList<SalesOrderDeletedItemLogDto>> GetDeletedOrderItemsAsync(string sellOrderId)
        {
            if (string.IsNullOrWhiteSpace(sellOrderId))
                return Array.Empty<SalesOrderDeletedItemLogDto>();
            var safe = SqlQ(sellOrderId.Trim());
            var itemBiz = BusinessLogTypes.SellOrderItem;
            var sql = $@"
SELECT i.""SellOrderItemId"",
       i.sell_order_item_code AS ""SellOrderItemCode"",
       i.pn AS ""PN"",
       i.brand AS ""Brand"",
       i.qty AS ""Qty"",
       i.price AS ""Price"",
       i.currency AS ""Currency"",
       i.comment AS ""Comment"",
       i.""CreateTime"",
       i.""ModifyTime"" AS ""DeletedAt"",
       COALESCE(
         NULLIF(TRIM(i.deleted_by_user_name), ''),
         NULLIF(TRIM(del_op.""OperatorUserName""), ''),
         NULLIF(TRIM(chg_near.""ChangedByUserName""), ''),
         NULLIF(TRIM(u.""UserName""), '')
       ) AS ""DeletedByUserName"",
       COALESCE(NULLIF(TRIM(i.deleted_by_user_id), ''), del_op.""OperatorUserId"") AS ""DeletedByUserId""
FROM sellorderitem i
INNER JOIN sellorder so ON so.""SellOrderId"" = i.sell_order_id
LEFT JOIN ""user"" u ON u.""UserId"" = so.modify_by_user_id
    AND so.""ModifyTime"" IS NOT NULL
    AND i.""ModifyTime"" IS NOT NULL
    AND ABS(EXTRACT(EPOCH FROM (i.""ModifyTime"" - so.""ModifyTime""))) <= 120
LEFT JOIN LATERAL (
    SELECT o.""OperatorUserId"", o.""OperatorUserName""
    FROM log_operation o
    WHERE o.""BizType"" = '{itemBiz}'
      AND o.""RecordId"" = i.""SellOrderItemId""
      AND o.""ActionType"" IN ('{OperationLogActionTypes.SellOrderItemDelete}', '{OperationLogActionTypes.SellOrderItemDeleteWithOrder}')
    ORDER BY o.""OperationTime"" DESC
    LIMIT 1
) del_op ON true
LEFT JOIN LATERAL (
    SELECT c.""ChangedByUserName""
    FROM log_change_fldval c
    WHERE c.""BizType"" = '{BusinessLogTypes.SalesOrder}'
      AND c.""RecordId"" = so.""SellOrderId""
      AND c.""ChangedByUserName"" IS NOT NULL
      AND TRIM(c.""ChangedByUserName"") <> ''
      AND c.""ChangedAt"" BETWEEN i.""ModifyTime"" - INTERVAL '30 minutes' AND i.""ModifyTime"" + INTERVAL '30 minutes'
    ORDER BY ABS(EXTRACT(EPOCH FROM (c.""ChangedAt"" - i.""ModifyTime"")))
    LIMIT 1
) chg_near ON true
WHERE i.sell_order_id = '{safe}' AND i.is_deleted = true
ORDER BY i.""ModifyTime"" DESC NULLS LAST, i.""CreateTime"" DESC";
            var rows = await _unitOfWork.QueryAsync<SalesOrderDeletedItemLogDto>(sql);
            return rows.ToList();
        }

        private sealed record SalesOrderHeaderSnapshot(
            string? CustomerName,
            string? SalesUserId,
            string? SalesUserName,
            string? Assistor,
            short Type,
            short Currency,
            DateTime? DeliveryDate,
            string? DeliveryAddress,
            string? ProductKind,
            string? CustomerContactName,
            string? InvoiceInfo,
            string? PaymentTermsText,
            string? Comment,
            decimal Total,
            decimal ConvertTotal);

        private static SalesOrderHeaderSnapshot CaptureSalesOrderHeaderSnapshot(SellOrder order) =>
            new(
                order.CustomerName,
                order.SalesUserId,
                order.SalesUserName,
                order.Assistor,
                order.Type,
                order.Currency,
                order.DeliveryDate,
                order.DeliveryAddress,
                order.ProductKind,
                order.CustomerContactName,
                order.InvoiceInfo,
                order.PaymentTermsText,
                order.Comment,
                order.Total,
                order.ConvertTotal);

        private async Task LogSalesOrderHeaderChangesAsync(
            SellOrder order,
            SalesOrderHeaderSnapshot before,
            string? actingUserId)
        {
            var after = CaptureSalesOrderHeaderSnapshot(order);
            await CompareAndLogHeaderFieldAsync(order, before.CustomerName, after.CustomerName, "customerName", "客户名称", actingUserId);
            await CompareAndLogHeaderFieldAsync(order, before.SalesUserName, after.SalesUserName, "salesUserName", "业务员", actingUserId);
            await CompareAndLogSoAssistorFieldAsync(order, before.Assistor, after.Assistor, actingUserId);
            await CompareAndLogHeaderFieldAsync(order, FormatOrderType(before.Type), FormatOrderType(after.Type), "type", "订单类型", actingUserId);
            await CompareAndLogHeaderFieldAsync(order, FormatCurrency(before.Currency), FormatCurrency(after.Currency), "currency", "币别", actingUserId);
            await CompareAndLogHeaderFieldAsync(order, FormatDate(before.DeliveryDate), FormatDate(after.DeliveryDate), "deliveryDate", "交期", actingUserId);
            await CompareAndLogHeaderFieldAsync(order, before.DeliveryAddress, after.DeliveryAddress, "deliveryAddress", "交货地址", actingUserId);
            await CompareAndLogHeaderFieldAsync(order, before.ProductKind, after.ProductKind, "productKind", "产品种类", actingUserId);
            await CompareAndLogHeaderFieldAsync(order, before.CustomerContactName, after.CustomerContactName, "customerContactName", "客户联系人", actingUserId);
            await CompareAndLogHeaderFieldAsync(order, before.InvoiceInfo, after.InvoiceInfo, "invoiceInfo", "开票信息", actingUserId);
            await CompareAndLogHeaderFieldAsync(order, before.PaymentTermsText, after.PaymentTermsText, "paymentTermsText", "付款条款", actingUserId);
            await CompareAndLogHeaderFieldAsync(order, before.Comment, after.Comment, "comment", "备注", actingUserId);
            await CompareAndLogHeaderFieldAsync(order, FormatDecimal2(before.Total), FormatDecimal2(after.Total), "total", "订单总额", actingUserId);
            await CompareAndLogHeaderFieldAsync(order, FormatDecimal2(before.ConvertTotal), FormatDecimal2(after.ConvertTotal), "convertTotal", "折算总额(USD)", actingUserId);
        }

        private async Task CompareAndLogSoAssistorFieldAsync(
            SellOrder order,
            string? oldId,
            string? newId,
            string? actingUserId)
        {
            var o = await ResolveUserDisplayNameAsync(oldId);
            var n = await ResolveUserDisplayNameAsync(newId);
            if (string.Equals(o, n, StringComparison.Ordinal))
                return;
            await CompareAndLogHeaderFieldAsync(order, o, n, "assistor", "销售助理", actingUserId);
        }

        private async Task<string?> ResolveUserDisplayNameAsync(string? userId)
        {
            var id = NormalizeActingUserId(userId);
            if (string.IsNullOrEmpty(id))
                return string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
            var user = await _userService.GetByIdAsync(id);
            return string.IsNullOrWhiteSpace(user?.UserName) ? id : user!.UserName!.Trim();
        }

        private async Task CompareAndLogHeaderFieldAsync(
            SellOrder order,
            string? oldVal,
            string? newVal,
            string fieldName,
            string fieldLabel,
            string? actingUserId)
        {
            var o = string.IsNullOrWhiteSpace(oldVal) ? null : oldVal.Trim();
            var n = string.IsNullOrWhiteSpace(newVal) ? null : newVal.Trim();
            if (string.Equals(o, n, StringComparison.Ordinal))
                return;
            await AddSalesOrderFieldChangeLogAsync(order, fieldName, fieldLabel, o, n, actingUserId);
        }

        private async Task AddSalesOrderFieldChangeLogAsync(
            SellOrder order,
            string fieldName,
            string fieldLabel,
            string? oldValue,
            string? newValue,
            string? actingUserId)
        {
            var (userId, userName) = await ResolveActorAsync(actingUserId);
            var recordCodeSql = string.IsNullOrWhiteSpace(order.SellOrderCode) ? "NULL" : $"'{SqlQ(order.SellOrderCode)}'";
            var safeRecordId = SqlQ(order.Id);
            var safeField = SqlQ(fieldName);
            var safeLabel = SqlQ(fieldLabel);
            var oldSql = oldValue == null ? "NULL" : $"'{SqlQ(oldValue)}'";
            var newSql = newValue == null ? "NULL" : $"'{SqlQ(newValue)}'";
            var userIdSql = string.IsNullOrWhiteSpace(userId) ? "NULL" : $"'{SqlQ(userId)}'";
            var sql = $@"
INSERT INTO log_change_fldval (""Id"", ""BizType"", ""RecordId"", ""RecordCode"", ""FieldName"", ""FieldLabel"", ""OldValue"", ""NewValue"", ""ChangedAt"", ""ChangedByUserId"", ""ChangedByUserName"", ""ExtraInfo"", ""SysRemark"")
VALUES (gen_random_uuid()::text, '{BusinessLogTypes.SalesOrder}', '{safeRecordId}', {recordCodeSql}, '{safeField}', '{safeLabel}', {oldSql}, {newSql}, NOW(), {userIdSql}, '{SqlQ(userName)}', NULL, NULL)";
            await _unitOfWork.ExecuteAsync(sql);
        }

        private async Task<(string? UserId, string UserName)> ResolveActorAsync(string? actingUserId)
        {
            var id = NormalizeActingUserId(actingUserId);
            if (string.IsNullOrEmpty(id))
                return (null, "系统");
            var user = await _userService.GetByIdAsync(id);
            return (id, string.IsNullOrWhiteSpace(user?.UserName) ? id : user!.UserName!.Trim());
        }

        private static SellOrderItemFieldSnapshot CaptureSellOrderItemFieldSnapshot(SellOrderItem item) =>
            new(
                item.QuoteId,
                item.ProductId,
                item.PN,
                item.Brand,
                item.CustomerSo,
                item.CustomerPn,
                item.CustomerBrand,
                item.Qty,
                item.Price,
                item.Currency,
                item.DateCode,
                item.DeliveryDate,
                item.Comment);

        private async Task LogSellOrderItemFieldChangesAsync(
            SellOrderItem item,
            SellOrderItemFieldSnapshot before,
            string? actingUserId)
        {
            var after = CaptureSellOrderItemFieldSnapshot(item);
            await CompareAndLogSoItemFieldAsync(item, before.Qty, after.Qty, "qty", "数量", FormatDecimal4, actingUserId);
            await CompareAndLogSoItemFieldAsync(item, before.Price, after.Price, "price", "单价", FormatDecimal4, actingUserId);
            await CompareAndLogSoItemFieldAsync(item, FormatCurrency(before.Currency), FormatCurrency(after.Currency), "currency", "币别", actingUserId);
            await CompareAndLogSoItemFieldAsync(item, FormatDate(before.DeliveryDate), FormatDate(after.DeliveryDate), "deliveryDate", "交期", actingUserId);
            await CompareAndLogSoItemFieldAsync(item, before.DateCode, after.DateCode, "dateCode", "DC", actingUserId);
            await CompareAndLogSoItemFieldAsync(item, before.PN, after.PN, "pn", "物料型号", actingUserId);
            await CompareAndLogSoItemFieldAsync(item, before.Brand, after.Brand, "brand", "品牌", actingUserId);
            await CompareAndLogSoItemFieldAsync(item, before.Comment, after.Comment, "comment", "备注", actingUserId);
            await CompareAndLogSoItemFieldAsync(item, before.QuoteId, after.QuoteId, "quoteId", "关联报价", actingUserId);
            await CompareAndLogSoItemFieldAsync(item, before.CustomerSo, after.CustomerSo, "customerSo", "客户订单号", actingUserId);
            await CompareAndLogSoItemFieldAsync(item, before.CustomerPn, after.CustomerPn, "customerPn", "客户料号", actingUserId);
            await CompareAndLogSoItemFieldAsync(item, before.CustomerBrand, after.CustomerBrand, "customerBrand", "客户品牌", actingUserId);
            await CompareAndLogSoItemFieldAsync(item, before.ProductId, after.ProductId, "productId", "产品ID", actingUserId);
        }

        private async Task LogSellOrderItemAddedAsync(SellOrderItem item, string? actingUserId)
        {
            var summary =
                $"{item.SellOrderItemCode} · {item.PN} · 数量 {FormatDecimal4(item.Qty)} · 单价 {FormatDecimal4(item.Price)} {FormatCurrency(item.Currency)}";
            await AddSellOrderItemFieldChangeLogAsync(item, "lineAdded", "新增明细", null, summary, actingUserId);
        }

        private async Task CompareAndLogSoItemFieldAsync(
            SellOrderItem item,
            string? oldVal,
            string? newVal,
            string fieldName,
            string fieldLabel,
            string? actingUserId)
        {
            var o = string.IsNullOrWhiteSpace(oldVal) ? null : oldVal.Trim();
            var n = string.IsNullOrWhiteSpace(newVal) ? null : newVal.Trim();
            if (string.Equals(o, n, StringComparison.Ordinal))
                return;
            await AddSellOrderItemFieldChangeLogAsync(item, fieldName, fieldLabel, o, n, actingUserId);
        }

        private async Task CompareAndLogSoItemFieldAsync(
            SellOrderItem item,
            decimal oldVal,
            decimal newVal,
            string fieldName,
            string fieldLabel,
            Func<decimal, string> format,
            string? actingUserId)
        {
            if (oldVal == newVal)
                return;
            await AddSellOrderItemFieldChangeLogAsync(item, fieldName, fieldLabel, format(oldVal), format(newVal), actingUserId);
        }

        private async Task AddSellOrderItemFieldChangeLogAsync(
            SellOrderItem item,
            string fieldName,
            string fieldLabel,
            string? oldValue,
            string? newValue,
            string? actingUserId)
        {
            var (userId, userName) = await ResolveActorAsync(actingUserId);
            var recordCode = string.IsNullOrWhiteSpace(item.SellOrderItemCode) ? null : item.SellOrderItemCode.Trim();
            var recordCodeSql = recordCode == null ? "NULL" : $"'{SqlQ(recordCode)}'";
            var sql = $@"
INSERT INTO log_change_fldval (""Id"", ""BizType"", ""RecordId"", ""RecordCode"", ""FieldName"", ""FieldLabel"", ""OldValue"", ""NewValue"", ""ChangedAt"", ""ChangedByUserId"", ""ChangedByUserName"", ""ExtraInfo"", ""SysRemark"")
VALUES (gen_random_uuid()::text, '{BusinessLogTypes.SellOrderItem}', '{SqlQ(item.Id)}', {recordCodeSql}, '{SqlQ(fieldName)}', '{SqlQ(fieldLabel)}', {(oldValue == null ? "NULL" : $"'{SqlQ(oldValue)}'")}, {(newValue == null ? "NULL" : $"'{SqlQ(newValue)}'")}, NOW(), {(userId == null ? "NULL" : $"'{SqlQ(userId)}'")}, '{SqlQ(userName)}', NULL, NULL)";
            await _unitOfWork.ExecuteAsync(sql);
        }

        private static string FormatDecimal2(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        private static string FormatDecimal4(decimal value) =>
            Math.Round(value, 4, MidpointRounding.AwayFromZero).ToString("F4", System.Globalization.CultureInfo.InvariantCulture);

        private sealed record SellOrderItemFieldChangeAudit(SellOrderItem Item, SellOrderItemFieldSnapshot Before);

        private sealed record SellOrderItemFieldSnapshot(
            string? QuoteId,
            string? ProductId,
            string? PN,
            string? Brand,
            string? CustomerSo,
            string? CustomerPn,
            string? CustomerBrand,
            decimal Qty,
            decimal Price,
            short Currency,
            string? DateCode,
            DateTime? DeliveryDate,
            string? Comment);

        private sealed record SellOrderItemSyncResult(
            List<SellOrderItem> Inserted,
            List<SellOrderItem> Updated,
            List<SellOrderItem> Deleted,
            List<SellOrderItemFieldChangeAudit> ItemUpdateAudits,
            decimal Total,
            decimal ConvertTotal);

        private async Task<SellOrderItemSyncResult> SyncSellOrderItemsOnUpdateAsync(
            SellOrder order,
            string sellOrderId,
            List<CreateSalesOrderItemRequest> requestItems,
            string? actingUserId)
        {
            var existingActive = (await _soItemRepo.FindAsync(i => i.SellOrderId == sellOrderId))
                .Where(i => !i.IsDeleted)
                .ToDictionary(i => i.Id, StringComparer.OrdinalIgnoreCase);

            var fx = await _financeExchangeRateService.GetCurrentAsync();
            var inserted = new List<SellOrderItem>();
            var updated = new List<SellOrderItem>();
            var keptIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            decimal total = 0m;
            decimal convertTotalUsd = 0m;

            var newItemRequests = new List<CreateSalesOrderItemRequest>();
            var itemUpdateAudits = new List<SellOrderItemFieldChangeAudit>();

            foreach (var itemReq in requestItems)
            {
                var reqId = itemReq.Id?.Trim();
                if (!string.IsNullOrEmpty(reqId))
                {
                    if (!existingActive.TryGetValue(reqId, out var existing))
                        throw new InvalidOperationException($"销售订单明细 {reqId} 不存在或已删除");

                    keptIds.Add(reqId);
                    var before = CaptureSellOrderItemFieldSnapshot(existing);
                    ApplySellOrderItemFromRequest(existing, itemReq, fx);
                    existing.ModifyTime = DateTime.UtcNow;
                    await _soItemRepo.UpdateAsync(existing);
                    updated.Add(existing);
                    itemUpdateAudits.Add(new SellOrderItemFieldChangeAudit(existing, before));
                    total += existing.Qty * existing.Price;
                    convertTotalUsd += ExchangeRateToUsdConverter.LineAmountUsd(existing.Qty, existing.ConvertPrice);
                }
                else
                {
                    newItemRequests.Add(itemReq);
                }
            }

            if (newItemRequests.Count > 0)
            {
                var firstSeq = await _soLineSeq.ReserveNextSequenceBlockAsync(sellOrderId, newItemRequests.Count);
                var lineIndex = 0;
                foreach (var item in newItemRequests)
                {
                    var seq = firstSeq + lineIndex++;
                    var soItem = new SellOrderItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        SellOrderId = sellOrderId,
                        SellOrderItemCode = OrderLineItemCodes.Sell(order.SellOrderCode, seq),
                        CreateTime = DateTime.UtcNow
                    };
                    ApplySellOrderItemFromRequest(soItem, item, fx);
                    await _soItemRepo.AddAsync(soItem);
                    inserted.Add(soItem);
                    await AddSellOrderItemExtendAsync(soItem, fx);
                    total += soItem.Qty * soItem.Price;
                    convertTotalUsd += ExchangeRateToUsdConverter.LineAmountUsd(soItem.Qty, soItem.ConvertPrice);
                }
            }

            var deleted = new List<SellOrderItem>();
            var (deleteActorId, deleteActorName) = await ResolveActorAsync(actingUserId);
            foreach (var existing in existingActive.Values)
            {
                if (keptIds.Contains(existing.Id))
                    continue;

                existing.IsDeleted = true;
                existing.ModifyTime = DateTime.UtcNow;
                existing.DeletedByUserId = deleteActorId;
                existing.DeletedByUserName = deleteActorName;
                await _soItemRepo.UpdateAsync(existing);
                await _soItemExtendRepo.DeleteAsync(existing.Id);
                deleted.Add(existing);
            }

            return new SellOrderItemSyncResult(inserted, updated, deleted, itemUpdateAudits, total, convertTotalUsd);
        }

        private static void ApplySellOrderItemFromRequest(
            SellOrderItem target,
            CreateSalesOrderItemRequest item,
            FinanceExchangeRateDto fx)
        {
            target.QuoteId = item.QuoteId;
            target.ProductId = item.ProductId;
            target.PN = item.PN;
            target.Brand = item.Brand;
            target.CustomerSo = item.CustomerSo;
            target.CustomerPn = string.IsNullOrWhiteSpace(item.CustomerPn) ? null : item.CustomerPn.Trim();
            target.CustomerBrand = string.IsNullOrWhiteSpace(item.CustomerBrand) ? null : item.CustomerBrand.Trim();
            target.Qty = item.Qty;
            target.Price = item.Price;
            target.Currency = item.Currency;
            target.DateCode = item.DateCode;
            target.DeliveryDate = PostgreSqlDateTime.ToUtc(item.DeliveryDate);
            target.Comment = item.Comment;
            target.ConvertPrice = ExchangeRateToUsdConverter.UnitLocalToUsd(
                target.Price, target.Currency, fx.UsdToCny, fx.UsdToHkd, fx.UsdToEur);
        }

        private async Task AppendSellOrderItemDeleteOperationLogsAsync(
            SellOrder order,
            IReadOnlyList<SellOrderItem> deletedItems,
            string? actingUserId,
            string actionType,
            string descriptionPrefix)
        {
            var (actorId, actorName) = await ResolveActorAsync(actingUserId);
            foreach (var d in deletedItems)
            {
                await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
                {
                    BizType = BusinessLogTypes.SellOrderItem,
                    RecordId = d.Id,
                    RecordCode = d.SellOrderItemCode,
                    ActionTypeOverride = actionType,
                    OperatorUserId = actorId,
                    OperatorUserName = actorName,
                    OperationDescOverride = $"{descriptionPrefix} {d.SellOrderItemCode}"
                });
            }
        }

        private async Task AppendSellOrderWholeDeleteOperationLogsAsync(
            SellOrder order,
            IReadOnlyList<SellOrderItem> deletedItems,
            string? actingUserId)
        {
            var (actorId, actorName) = await ResolveActorAsync(actingUserId);
            await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
            {
                BizType = BusinessLogTypes.SalesOrder,
                RecordId = order.Id,
                RecordCode = order.SellOrderCode,
                ActionTypeOverride = OperationLogActionTypes.SellOrderDelete,
                OperatorUserId = actorId,
                OperatorUserName = actorName,
                OperationDescOverride =
                    $"整单删除销售订单 {order.SellOrderCode}，共 {deletedItems.Count} 条明细"
            });

            if (deletedItems.Count > 0)
            {
                await AppendSellOrderItemDeleteOperationLogsAsync(
                    order,
                    deletedItems,
                    actingUserId,
                    OperationLogActionTypes.SellOrderItemDeleteWithOrder,
                    $"整单删除销售订单 {order.SellOrderCode} 时删除明细行");
            }
        }

        private async Task SoftDeleteSellOrderExtendAsync(string sellOrderId)
        {
            if (string.IsNullOrWhiteSpace(sellOrderId))
                return;
            var safeId = sellOrderId.Trim().Replace("'", "''", StringComparison.Ordinal);
            await _unitOfWork.ExecuteAsync(
                $@"UPDATE sellorderextend SET is_deleted = true, ""ModifyTime"" = NOW() WHERE ""SellOrderId"" = '{safeId}' AND is_deleted = false");
        }

        private static string FormatSellOrderStatus(short status) => status switch
        {
            1 => "新建",
            2 => "待审核",
            10 => "审核通过",
            20 => "进行中",
            100 => "完成",
            -1 => "审核失败",
            -2 => "取消",
            _ => status.ToString()
        };

        private static string FormatOrderType(short type) => type switch
        {
            1 => "客单采购",
            2 => "备货采购",
            3 => "样品采购",
            _ => type.ToString()
        };

        private static string FormatCurrency(short currency) => currency switch
        {
            1 => "RMB",
            2 => "USD",
            3 => "EUR",
            4 => "HKD",
            _ => currency.ToString()
        };

        private static string? FormatDate(DateTime? dt) =>
            dt.HasValue ? dt.Value.ToString("yyyy-MM-dd") : null;

        private sealed class SoItemRefreshSnapshot
        {
            public short PurchaseProgressStatus { get; set; }
            public short StockInProgressStatus { get; set; }
            public short StockOutProgressStatus { get; set; }
            public short ReceiptProgressStatus { get; set; }
            public short InvoiceProgressStatus { get; set; }
            public decimal UsdUnitPrice { get; set; }
            public decimal UsdLineTotal { get; set; }
            public decimal SalesProfitExpected { get; set; }
            public decimal ProfitOutBizUsd { get; set; }
            public decimal ProfitOutRateBiz { get; set; }
            public decimal QtyAlreadyPurchased { get; set; }
            public decimal QtyNotPurchase { get; set; }
            public decimal QtyStockOutNotify { get; set; }
            public decimal QtyStockOutNotifyNot { get; set; }
            public decimal QtyStockOutActual { get; set; }
            public decimal InvoiceAmount { get; set; }
            public decimal InvoiceAmountNot { get; set; }
            public decimal InvoiceAmountFinish { get; set; }
            public int PurchasedStockAvailableQty { get; set; }
            public decimal ReceiptAmount { get; set; }
            public decimal ReceiptAmountNot { get; set; }
            public decimal ReceiptAmountFinish { get; set; }
            public decimal PaymentAmount { get; set; }
            public decimal PaymentAmountDone { get; set; }
            public decimal PaymentAmountToBe { get; set; }
            public decimal PurchaseInvoiceAmount { get; set; }
            public decimal PurchaseInvoiceDone { get; set; }
            public decimal PoCostUsdConfirmed { get; set; }
            public decimal ProfitOutFinUsd { get; set; }
            public decimal ProfitOutRateFin { get; set; }
        }
    }
}
