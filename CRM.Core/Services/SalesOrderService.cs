using CRM.Core.Constants;
using System.Collections.Generic;
using CRM.Core.Interfaces;
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
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;
        private readonly IFinanceExchangeRateService _financeExchangeRateService;
        private readonly IOrderJourneyLogService _orderJourneyLog;
        private readonly IRepository<QuoteItem> _quoteItemRepo;
        private readonly ISellOrderItemExtendSyncService _soItemExtendSync;
        private readonly ISellOrderItemPurchasedStockAvailableSyncService _purchasedStockAvailableSync;
        private readonly ISellOrderExtendLineSeqService _soLineSeq;
        private readonly IUserService _userService;
        private readonly ISalesOrderListQuery _salesOrderListQuery;
        private readonly ISalesOrderItemLineListQuery _salesOrderItemLineListQuery;
        private readonly ILogger<SalesOrderService> _logger;

        public SalesOrderService(
            IRepository<SellOrder> soRepo,
            IRepository<SellOrderItem> soItemRepo,
            IRepository<SellOrderItemExtend> soItemExtendRepo,
            IRepository<PurchaseOrder> poRepo,
            IRepository<PurchaseOrderItem> poItemRepo,
            IRepository<PurchaseRequisition> prRepo,
            IRepository<QuoteItem> quoteItemRepo,
            IDataPermissionService dataPermissionService,
            ISerialNumberService serialNumberService,
            IFinanceExchangeRateService financeExchangeRateService,
            IOrderJourneyLogService orderJourneyLog,
            ISellOrderItemExtendSyncService soItemExtendSync,
            ISellOrderItemPurchasedStockAvailableSyncService purchasedStockAvailableSync,
            ISellOrderExtendLineSeqService soLineSeq,
            IUserService userService,
            ISalesOrderListQuery salesOrderListQuery,
            ISalesOrderItemLineListQuery salesOrderItemLineListQuery,
            IUnitOfWork unitOfWork,
            ILogger<SalesOrderService> logger)
        {
            _soRepo = soRepo;
            _soItemRepo = soItemRepo;
            _soItemExtendRepo = soItemExtendRepo;
            _poRepo = poRepo;
            _poItemRepo = poItemRepo;
            _prRepo = prRepo;
            _quoteItemRepo = quoteItemRepo;
            _dataPermissionService = dataPermissionService;
            _serialNumberService = serialNumberService;
            _financeExchangeRateService = financeExchangeRateService;
            _orderJourneyLog = orderJourneyLog;
            _soItemExtendSync = soItemExtendSync;
            _purchasedStockAvailableSync = purchasedStockAvailableSync;
            _soLineSeq = soLineSeq;
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _salesOrderListQuery = salesOrderListQuery ?? throw new ArgumentNullException(nameof(salesOrderListQuery));
            _salesOrderItemLineListQuery = salesOrderItemLineListQuery ?? throw new ArgumentNullException(nameof(salesOrderItemLineListQuery));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger;
        }

        private static string? NormalizeActingUserId(string? actingUserId) =>
            string.IsNullOrWhiteSpace(actingUserId) ? null : actingUserId.Trim();

        public async Task<SellOrder> CreateAsync(CreateSalesOrderRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(request.CustomerId))
                throw new ArgumentException("客户ID不能为空", nameof(request.CustomerId));

            var sellOrderCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.SalesOrder);

            var order = new SellOrder
            {
                Id = Guid.NewGuid().ToString(),
                SellOrderCode = sellOrderCode,
                CustomerId = request.CustomerId,
                CustomerName = request.CustomerName,
                SalesUserId = request.SalesUserId,
                SalesUserName = request.SalesUserName,
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
            }
            order.Total = total;
            order.ConvertTotal = total;
            await _soRepo.UpdateAsync(order);

            await _unitOfWork.SaveChangesAsync();

            foreach (var line in createdLines)
                await _soItemExtendSync.RecalculateAsync(line.Id);

            await TryRefreshPurchasedStockAvailableForSellLinesAsync(createdLines);

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
            order.Items = items.ToList();
            return order;
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
                await HydrateSellOrderListSalesLoginAsync(list);

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
            if (HeaderRemarksTouched(request))
                PatchSalesOrderHeaderRemarksFromRequest(order, request);

            if (request.CustomerName != null) order.CustomerName = request.CustomerName;
            if (request.SalesUserId != null) order.SalesUserId = request.SalesUserId;
            if (request.SalesUserName != null) order.SalesUserName = request.SalesUserName;
            if (request.Type.HasValue) order.Type = request.Type.Value;
            if (request.Currency.HasValue) order.Currency = request.Currency.Value;
            if (request.DeliveryDate.HasValue) order.DeliveryDate = PostgreSqlDateTime.ToUtc(request.DeliveryDate.Value);
            if (request.DeliveryAddress != null) order.DeliveryAddress = request.DeliveryAddress;

            var replacedItemCount = 0;
            List<SellOrderItem>? newLines = null;
            if (request.Items != null && request.Items.Count > 0)
            {
                var existingItems = await _soItemRepo.GetAllAsync();
                var toDelete = existingItems.Where(i => i.SellOrderId == id).ToList();
                foreach (var d in toDelete)
                {
                    await _soItemExtendRepo.DeleteAsync(d.Id);
                    await _soItemRepo.DeleteAsync(d.Id);
                }

                var fx = await _financeExchangeRateService.GetCurrentAsync();
                var firstSeq = await _soLineSeq.ReserveNextSequenceBlockAsync(id, request.Items.Count);
                var lineIndex = 0;
                decimal total = 0m;
                newLines = new List<SellOrderItem>();
                foreach (var item in request.Items)
                {
                    var seq = firstSeq + lineIndex++;
                    var soItem = new SellOrderItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        SellOrderId = id,
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
                    newLines.Add(soItem);
                    await AddSellOrderItemExtendAsync(soItem, fx);
                    total += item.Qty * item.Price;
                }
                order.Total = total;
                order.ConvertTotal = total;
                order.ItemRows = request.Items.Count;
                replacedItemCount = request.Items.Count;
            }

            order.ModifyTime = DateTime.UtcNow;
            order.ModifyByUserId = NormalizeActingUserId(actingUserId);
            await _soRepo.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            if (newLines != null)
            {
                foreach (var line in newLines)
                    await _soItemExtendSync.RecalculateAsync(line.Id);
                await TryRefreshPurchasedStockAvailableForSellLinesAsync(newLines);
            }

            if (replacedItemCount > 0 && newLines != null)
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
                foreach (var line in newLines)
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

        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID不能为空");
            var order = await _soRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"销售订单 {id} 不存在");
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
            var items = await _soItemRepo.GetAllAsync();
            foreach (var item in items.Where(i => i.SellOrderId == id))
            {
                await _soItemExtendRepo.DeleteAsync(item.Id);
                await _soItemRepo.DeleteAsync(item.Id);
            }
            await _soRepo.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
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
            order.Status = status;
            if (status == SellOrderMainStatus.AuditFailed && !string.IsNullOrWhiteSpace(auditRemark))
                order.AuditRemark = auditRemark.Trim();
            else if (status == SellOrderMainStatus.Approved)
                order.AuditRemark = null;

            order.ModifyTime = DateTime.UtcNow;
            order.ModifyByUserId = NormalizeActingUserId(actingUserId);
            await _soRepo.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

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
        }

        public async Task RequestStockOutAsync(string id, string requestedBy)
        {
            // 申请出库后进入「进行中」（完成=Completed 由业务手动或后续流程标记）
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
            await EnrichSellOrderItemLineListAsync(list);

            return new PagedResult<SellOrderItemLineDto>
            {
                Items = list,
                TotalCount = pageResult.TotalCount,
                PageIndex = pageResult.PageIndex,
                PageSize = pageResult.PageSize
            };
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
                    row.ProfitOutRateBiz = ext.ProfitOutRateBiz;
                    row.PurchasedStockAvailableQty = ext.PurchasedStock_AvailableQty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[SellLineStockOutSync] GetSellOrderItemLinesPagedAsync merge sellorderitemextend failed; progress columns left at default 0. LineIdCount={Count}",
                    list.Count);
            }

            var gate = await GetStockOutApplyPurchaseGateBySellLineIdsAsync(list.Select(x => x.SellOrderItemId));
            foreach (var row in list)
            {
                var key = row.SellOrderItemId?.Trim() ?? string.Empty;
                row.StockOutApplyPurchaseGateOk = !string.IsNullOrEmpty(key) &&
                                                  gate.TryGetValue(key, out var g) && g;
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
        }

        public async Task<SalesOrderItemExtendRefreshResult> RefreshItemExtendsAsync(string salesOrderId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(salesOrderId))
                throw new ArgumentException("销售订单ID不能为空", nameof(salesOrderId));

            var orderId = salesOrderId.Trim();
            var order = await _soRepo.GetByIdAsync(orderId)
                ?? throw new InvalidOperationException($"销售订单 {orderId} 不存在");

            var items = (await _soItemRepo.FindAsync(x => x.SellOrderId == orderId)).ToList();
            var result = new SalesOrderItemExtendRefreshResult
            {
                SalesOrderId = orderId,
                TotalItems = items.Count,
                RefreshedAt = DateTime.UtcNow
            };

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var before = await BuildRefreshSnapshotAsync(item.Id);
                await _soItemExtendSync.RecalculateAsync(item.Id, cancellationToken);
                await _purchasedStockAvailableSync.RecalculateByPurchasePnAndBrandAsync(item.PN, item.Brand, cancellationToken);
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

            // 销售订单明细列表「主状态」列 = 主表 sellorder.status；与明细扩展刷新对齐（可上调/下调，避免头表与执行事实长期脱节）
            var lineIds = items.Select(i => i.Id).ToList();
            var extendById = new Dictionary<string, SellOrderItemExtend>(StringComparer.OrdinalIgnoreCase);
            if (lineIds.Count > 0)
            {
                foreach (var ext in await _soItemExtendRepo.FindAsync(e => lineIds.Contains(e.Id)))
                    extendById[ext.Id] = ext;
            }

            var beforeOrderStatus = order.Status;
            var targetOrderStatus = ComputeSellOrderMainStatusAfterRefresh(order, items, extendById);
            if (targetOrderStatus != beforeOrderStatus)
            {
                order.Status = targetOrderStatus;
                order.ModifyTime = DateTime.UtcNow;
                await _soRepo.UpdateAsync(order);
                result.ChangedFieldsCount += 1;
            }

            result.ChangedItems = result.Changes.Count;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "SO明细扩展刷新完成: SalesOrderId={SalesOrderId} Code={Code} TotalItems={TotalItems} ChangedItems={ChangedItems} ChangedFields={ChangedFields}",
                orderId, order.SellOrderCode, result.TotalItems, result.ChangedItems, result.ChangedFieldsCount);
            return result;
        }

        /// <summary>
        /// 按未取消明细的扩展进度对齐 <see cref="SellOrder.Status"/>（与列表 <c>orderStatus</c> 同源）。
        /// 新建/待审核/取消/审核失败不自动改写；已审核起可根据执行链上调或纠正「完成」。
        /// </summary>
        private static SellOrderMainStatus ComputeSellOrderMainStatusAfterRefresh(
            SellOrder order,
            IReadOnlyList<SellOrderItem> items,
            IReadOnlyDictionary<string, SellOrderItemExtend> extendByItemId)
        {
            // sellorderitem.status：1=已取消（见实体注释）
            const short cancelledLine = 1;
            if (order.Status is SellOrderMainStatus.Cancelled or SellOrderMainStatus.AuditFailed
                or SellOrderMainStatus.New or SellOrderMainStatus.PendingAudit)
                return order.Status;

            var hasActiveLine = false;
            foreach (var it in items)
            {
                if (it.Status != cancelledLine)
                {
                    hasActiveLine = true;
                    break;
                }
            }

            if (!hasActiveLine)
                return order.Status;

            bool AnyActiveExecutionStarted()
            {
                foreach (var it in items)
                {
                    if (it.Status == cancelledLine)
                        continue;
                    if (!extendByItemId.TryGetValue(it.Id, out var e))
                        continue;
                    if (e.PurchaseProgressStatus > 0 || e.StockInProgressStatus > 0 || e.StockOutProgressStatus > 0
                        || e.ReceiptProgressStatus > 0 || e.InvoiceProgressStatus > 0
                        || e.QtyStockOutNotify > 0m)
                        return true;
                }

                return false;
            }

            bool AllActiveStockOutComplete()
            {
                foreach (var it in items)
                {
                    if (it.Status == cancelledLine)
                        continue;
                    if (!extendByItemId.TryGetValue(it.Id, out var e) || e.StockOutProgressStatus < 2)
                        return false;
                }

                return true;
            }

            var next = order.Status;

            if (next == SellOrderMainStatus.Approved && AnyActiveExecutionStarted())
                next = SellOrderMainStatus.InProgress;

            if (next == SellOrderMainStatus.InProgress && AllActiveStockOutComplete())
                next = SellOrderMainStatus.Completed;

            if (next == SellOrderMainStatus.Completed && !AllActiveStockOutComplete())
                next = SellOrderMainStatus.InProgress;

            return next;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<string, bool>> GetStockOutApplyPurchaseGateBySellLineIdsAsync(
            IEnumerable<string> sellOrderItemIds)
        {
            var idSet = sellOrderItemIds
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (idSet.Count == 0)
                return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

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

            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in idSet)
            {
                if (!bySellLine.TryGetValue(id, out var lines) || lines.Count == 0)
                {
                    result[id] = false;
                    continue;
                }

                var distinctPoIds = lines
                    .Select(l => l.PurchaseOrderId)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var ok = true;
                foreach (var pid in distinctPoIds)
                {
                    if (!poById.TryGetValue(pid, out var po))
                    {
                        ok = false;
                        break;
                    }

                    if (po.Status < min)
                    {
                        ok = false;
                        break;
                    }
                }

                result[id] = ok;
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
