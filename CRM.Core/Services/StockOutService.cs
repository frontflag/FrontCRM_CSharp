using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customs;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Models.System;
using CRM.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services
{
    /// <summary>
    /// 出库服务实现
    /// </summary>
    public class StockOutService : IStockOutService
    {
        private readonly IRepository<StockOut> _stockOutRepository;
        private readonly IRepository<StockOutItem> _stockOutItemRepository;
        private readonly IRepository<StockOutItemExtend> _stockOutItemExtendRepository;
        private readonly IRepository<StockOutRequest> _stockOutRequestRepository;
        private readonly IRepository<CustomsPendlist> _customsPendlistRepository;
        private readonly IRepository<Packing> _packingRepository;
        private readonly IRepository<PackingItem> _packingItemRepository;
        private readonly IRepository<PickingTask> _pickingTaskRepository;
        private readonly IRepository<PickingTaskItem> _pickingTaskItemRepository;
        private readonly IRepository<StockInfo> _stockRepository;
        private readonly IRepository<StockItem> _stockItemRepository;
        private readonly IRepository<InventoryLedger> _ledgerRepository;
        private readonly IRepository<StockInItem> _stockInItemRepository;
        private readonly IRepository<StockIn> _stockInRepository;
        private readonly IRepository<SellOrder> _sellOrderRepository;
        private readonly IRepository<SellOrderItem> _sellOrderItemRepository;
        private readonly IRepository<SellOrderItemExtend> _sellOrderItemExtendRepository;
        private readonly IRepository<CustomerInfo> _customerRepository;
        private readonly IRepository<PurchaseOrderItem> _purchaseOrderItemRepository;
        private readonly IRepository<PurchaseOrder> _purchaseOrderRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<WarehouseInfo> _warehouseRepository;
        private readonly IInventoryCenterService _inventoryCenterService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;
        private readonly ISellOrderItemExtendSyncService _sellOrderItemExtendSync;
        private readonly ISellOrderItemPurchasedStockAvailableSyncService _purchasedStockAvailableSync;
        private readonly IForceDeleteGuardService _forceDeleteGuard;
        private readonly ILogOperationAppendService _logOperationAppend;
        private readonly ILogger<StockOutService> _logger;
        private readonly IStockOutListQuery _stockOutListQuery;
        private readonly IStockOutRequestListQuery _stockOutRequestListQuery;
        private readonly IStockOutItemListQuery _stockOutItemListQuery;
        private readonly ICustomsV2FlowService _customsV2FlowService;
        private readonly IFinanceReceivableService _financeReceivableService;

        public StockOutService(
            IRepository<StockOut> stockOutRepository,
            IRepository<StockOutItem> stockOutItemRepository,
            IRepository<StockOutItemExtend> stockOutItemExtendRepository,
            IRepository<StockOutRequest> stockOutRequestRepository,
            IRepository<CustomsPendlist> customsPendlistRepository,
            IRepository<Packing> packingRepository,
            IRepository<PackingItem> packingItemRepository,
            IRepository<PickingTask> pickingTaskRepository,
            IRepository<PickingTaskItem> pickingTaskItemRepository,
            IRepository<StockInfo> stockRepository,
            IRepository<StockItem> stockItemRepository,
            IRepository<InventoryLedger> ledgerRepository,
            IRepository<StockInItem> stockInItemRepository,
            IRepository<StockIn> stockInRepository,
            IRepository<SellOrder> sellOrderRepository,
            IRepository<SellOrderItem> sellOrderItemRepository,
            IRepository<SellOrderItemExtend> sellOrderItemExtendRepository,
            IRepository<CustomerInfo> customerRepository,
            IRepository<PurchaseOrderItem> purchaseOrderItemRepository,
            IRepository<PurchaseOrder> purchaseOrderRepository,
            IRepository<User> userRepository,
            IRepository<WarehouseInfo> warehouseRepository,
            IInventoryCenterService inventoryCenterService,
            ISerialNumberService serialNumberService,
            ISellOrderItemExtendSyncService sellOrderItemExtendSync,
            ISellOrderItemPurchasedStockAvailableSyncService purchasedStockAvailableSync,
            IUnitOfWork unitOfWork,
            IForceDeleteGuardService forceDeleteGuard,
            ILogOperationAppendService logOperationAppend,
            ILogger<StockOutService> logger,
            IStockOutListQuery stockOutListQuery,
            IStockOutRequestListQuery stockOutRequestListQuery,
            IStockOutItemListQuery stockOutItemListQuery,
            ICustomsV2FlowService customsV2FlowService,
            IFinanceReceivableService financeReceivableService)
        {
            _stockOutRepository = stockOutRepository;
            _stockOutItemRepository = stockOutItemRepository;
            _stockOutItemExtendRepository = stockOutItemExtendRepository;
            _stockOutRequestRepository = stockOutRequestRepository;
            _customsPendlistRepository = customsPendlistRepository;
            _packingRepository = packingRepository;
            _packingItemRepository = packingItemRepository;
            _pickingTaskRepository = pickingTaskRepository;
            _pickingTaskItemRepository = pickingTaskItemRepository;
            _stockRepository = stockRepository;
            _stockItemRepository = stockItemRepository;
            _ledgerRepository = ledgerRepository;
            _stockInItemRepository = stockInItemRepository;
            _stockInRepository = stockInRepository;
            _sellOrderRepository = sellOrderRepository;
            _sellOrderItemRepository = sellOrderItemRepository;
            _sellOrderItemExtendRepository = sellOrderItemExtendRepository;
            _customerRepository = customerRepository;
            _purchaseOrderItemRepository = purchaseOrderItemRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
            _userRepository = userRepository;
            _warehouseRepository = warehouseRepository;
            _inventoryCenterService = inventoryCenterService;
            _serialNumberService = serialNumberService;
            _sellOrderItemExtendSync = sellOrderItemExtendSync;
            _purchasedStockAvailableSync = purchasedStockAvailableSync;
            _unitOfWork = unitOfWork;
            _forceDeleteGuard = forceDeleteGuard;
            _logOperationAppend = logOperationAppend;
            _logger = logger;
            _stockOutListQuery = stockOutListQuery;
            _stockOutRequestListQuery = stockOutRequestListQuery;
            _stockOutItemListQuery = stockOutItemListQuery;
            _customsV2FlowService = customsV2FlowService;
            _financeReceivableService = financeReceivableService;
        }

        public async Task<StockOutRequest> CreateStockOutRequestAsync(CreateStockOutRequestRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(request.SalesOrderId))
                throw new ArgumentException("销售订单ID不能为空", nameof(request.SalesOrderId));
            if (string.IsNullOrWhiteSpace(request.SalesOrderItemId))
                throw new ArgumentException("销售订单明细不能为空", nameof(request.SalesOrderItemId));

            var so = await _sellOrderRepository.GetByIdAsync(request.SalesOrderId);
            if (so == null)
                throw new InvalidOperationException("销售订单不存在");
            if (so.Status < SellOrderMainStatus.Approved)
                throw new InvalidOperationException("销售订单未审核，不能申请出库");
            if (so.Status == SellOrderMainStatus.Completed)
                throw new InvalidOperationException("销售订单已完成，不能申请出库");

            var soItem = await _sellOrderItemRepository.GetByIdAsync(request.SalesOrderItemId.Trim());
            if (soItem == null)
                throw new InvalidOperationException("销售订单明细不存在");
            if (!string.Equals(soItem.SellOrderId, request.SalesOrderId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("销售订单明细不属于该订单");
            if (soItem.Status != 0)
                throw new InvalidOperationException("该销售订单明细已取消，不能申请出库");

            var lineId = request.SalesOrderItemId.Trim();
            await EnsureSellLineMeetsStockOutPurchaseGateAsync(lineId);

            var qtyInt = InventoryQuantity.RoundFromDecimal(request.Quantity);
            if (qtyInt <= 0)
                throw new ArgumentException("出库通知数量必须大于 0", nameof(request.Quantity));
            // 勿用 string.Equals(..., OrdinalIgnoreCase)：EF Core 无法翻译为 SQL（Npgsql）
            var existingReqs = (await _stockOutRequestRepository.FindAsync(r => r.SalesOrderItemId == lineId))
                .ToList();
            var alreadyNotified = existingReqs.Where(r => StockOutRequestStatusCode.IsActiveForQuantitySum(r.Status)).Sum(r => r.Quantity);
            var remainingByLine = soItem.Qty - alreadyNotified;
            if (remainingByLine <= 0m)
                throw new InvalidOperationException("该销售明细可出库通知数量已用尽，无法继续申请");
            if (request.Quantity > remainingByLine)
                throw new ArgumentException(
                    $"出库通知数量不能超过剩余可申请数量（{remainingByLine.ToString(CultureInfo.InvariantCulture)}，已占用 {alreadyNotified.ToString(CultureInfo.InvariantCulture)}）",
                    nameof(request.Quantity));

            var stockDto = await _inventoryCenterService.GetAvailableQtyForSellOrderItemAsync(lineId);
            var lineAvail = stockDto.AvailableQty;
            if (lineAvail < 0)
                lineAvail = 0;
            var extForCap = await _sellOrderItemExtendRepository.GetByIdAsync(lineId);
            var stockingAvail = extForCap?.PurchasedStock_AvailableQty ?? 0;
            var maxShippable = lineAvail + stockingAvail;
            if (qtyInt > maxShippable)
                throw new InvalidOperationException(
                    $"在库可用数量不足（客单在库 {lineAvail.ToString(CultureInfo.InvariantCulture)} + 备货在库 {stockingAvail.ToString(CultureInfo.InvariantCulture)} = {maxShippable.ToString(CultureInfo.InvariantCulture)}，本次申请 {qtyInt.ToString(CultureInfo.InvariantCulture)}）");

            var requestCode = string.IsNullOrWhiteSpace(request.RequestCode)
                ? await _serialNumberService.GenerateNextAsync(ModuleCodes.StockOutRequest)
                : request.RequestCode.Trim();

            var customers = (await _customerRepository.FindAsync(c => !c.IsDeleted)).ToList();
            var customerIdByDisplayName = CustomerIdResolveHelper.BuildDisplayNameIndex(customers);
            var resolvedCustomerId = CustomerIdResolveHelper.ResolveForStockOutNotify(
                request.CustomerId,
                so,
                customerIdByDisplayName);

            var regionType = RegionTypeCode.Normalize(request.RegionType);
            var stockOutType = StockOutTypeCode.NormalizeForNotify(request.StockOutType);

            LogisticsShipmentMethodCode.EnsureRequired(request.ShipmentMethod);
            var shipmentMethod = LogisticsShipmentMethodCode.Normalize(request.ShipmentMethod)!;
            var expressCompany = LogisticsShipmentMethodCode.NormalizeExpressCompany(request.ExpressCompany);

            var stockOutRequest = new StockOutRequest
            {
                Id = Guid.NewGuid().ToString(),
                RequestCode = requestCode,
                SalesOrderId = request.SalesOrderId,
                SalesOrderItemId = request.SalesOrderItemId.Trim(),
                MaterialCode = string.IsNullOrWhiteSpace(request.MaterialCode) ? (soItem.PN?.Trim() ?? string.Empty) : request.MaterialCode.Trim(),
                MaterialName = string.IsNullOrWhiteSpace(request.MaterialName) ? soItem.Brand?.Trim() : request.MaterialName.Trim(),
                Quantity = qtyInt,
                CustomerId = resolvedCustomerId ?? string.Empty,
                RequestUserId = request.RequestUserId,
                RequestDate = PostgreSqlDateTime.ToUtc(request.RequestDate),
                Status = StockOutRequestStatusCode.PendingPacking,
                Remark = request.Remark,
                ShipmentMethod = shipmentMethod,
                ExpressCompany = expressCompany,
                RegionType = regionType,
                StockOutType = stockOutType,
                CustomsStatus = StockOutNotifyCustomsStatusCode.Unknown,
                CreateTime = DateTime.UtcNow,
                CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId)
            };

            CustomsPendlist? pendlistToAdd = null;
            if (regionType == RegionTypeCode.Domestic && stockOutType == StockOutTypeCode.Sales)
            {
                var applySnapshot = await BuildApplyInventorySnapshotAsync(
                    lineId,
                    soItem.PN,
                    soItem.Brand,
                    qtyInt,
                    regionType);
                var useCustoms = request.UseOverseasWarehouseAndCustoms == true;
                ValidateCustomsChoiceForCreate(applySnapshot.CustomsOption, useCustoms);
                if (useCustoms)
                {
                    pendlistToAdd = await TryBuildCustomsPendlistAsync(
                        lineId,
                        soItem.PN,
                        soItem.Brand,
                        qtyInt,
                        stockOutRequest.Id,
                        actingUserId);
                    stockOutRequest.Status = StockOutRequestStatusCode.PendingCustoms;
                    stockOutRequest.CustomsStatus = StockOutNotifyCustomsStatusCode.PendingCustoms;
                }
            }

            await _stockOutRequestRepository.AddAsync(stockOutRequest);
            var saveAfterReq = await _unitOfWork.SaveChangesAsync();
            if (pendlistToAdd != null)
            {
                await _customsPendlistRepository.AddAsync(pendlistToAdd);
                await _unitOfWork.SaveChangesAsync();
            }
            _logger.LogInformation(
                "[SellLineStockOutSync] CreateStockOutRequest saved StockOutRequestId={RequestId} SellOrderItemId={SellOrderItemId} SaveChanges={Rows}",
                stockOutRequest.Id, lineId, saveAfterReq);
            await _sellOrderItemExtendSync.RecalculateAsync(lineId);
            var saveAfterExtend = await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation(
                "[SellLineStockOutSync] CreateStockOutRequest after Recalculate SellOrderItemId={SellOrderItemId} SaveChanges={Rows}",
                lineId, saveAfterExtend);
            return stockOutRequest;
        }

        /// <inheritdoc />
        public async Task<StockOutApplyContextDto> GetApplyContextAsync(
            string salesOrderId,
            string salesOrderItemId,
            decimal? requestedQty = null)
        {
            if (string.IsNullOrWhiteSpace(salesOrderId))
                throw new ArgumentException("销售订单ID不能为空", nameof(salesOrderId));
            if (string.IsNullOrWhiteSpace(salesOrderItemId))
                throw new ArgumentException("销售订单明细不能为空", nameof(salesOrderItemId));

            var so = await _sellOrderRepository.GetByIdAsync(salesOrderId.Trim());
            if (so == null)
                throw new InvalidOperationException("销售订单不存在");
            if (so.Status < SellOrderMainStatus.Approved)
                throw new InvalidOperationException("销售订单未审核，不能申请出库");
            if (so.Status == SellOrderMainStatus.Completed)
                throw new InvalidOperationException("销售订单已完成，不能申请出库");

            var soItem = await _sellOrderItemRepository.GetByIdAsync(salesOrderItemId.Trim());
            if (soItem == null)
                throw new InvalidOperationException("销售订单明细不存在");
            if (!string.Equals(soItem.SellOrderId, salesOrderId.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("销售订单明细不属于该订单");
            if (soItem.Status != 0)
                throw new InvalidOperationException("该销售订单明细已取消，不能申请出库");

            var lineId = salesOrderItemId.Trim();
            await EnsureSellLineMeetsStockOutPurchaseGateAsync(lineId);
            var existingReqs = (await _stockOutRequestRepository.FindAsync(r => r.SalesOrderItemId == lineId))
                .ToList();
            var alreadyNotified = existingReqs.Where(r => StockOutRequestStatusCode.IsActiveForQuantitySum(r.Status)).Sum(r => r.Quantity);
            var remainingNotify = soItem.Qty - alreadyNotified;
            if (remainingNotify < 0m)
                remainingNotify = 0m;

            var stockDto = await _inventoryCenterService.GetAvailableQtyForSellOrderItemAsync(lineId);
            var lineAvail = stockDto.AvailableQty;
            if (lineAvail < 0)
                lineAvail = 0;
            var ext = await _sellOrderItemExtendRepository.GetByIdAsync(lineId);
            var purchasedStock = ext?.PurchasedStock_AvailableQty ?? 0;
            var combined = (decimal)lineAvail + purchasedStock;
            var suggested = remainingNotify <= combined ? remainingNotify : combined;
            if (suggested < 0m)
                suggested = 0m;

            var evalQty = requestedQty.HasValue && requestedQty.Value > 0m
                ? InventoryQuantity.RoundFromDecimal(requestedQty.Value)
                : InventoryQuantity.RoundFromDecimal(suggested);
            if (evalQty < 0)
                evalQty = 0;

            var regionType = soItem.Currency == (short)CurrencyCode.RMB
                ? RegionTypeCode.Domestic
                : RegionTypeCode.Overseas;
            var snapshot = await BuildApplyInventorySnapshotAsync(
                lineId,
                soItem.PN,
                soItem.Brand,
                evalQty,
                regionType);

            return new StockOutApplyContextDto
            {
                salesOrderItemId = lineId,
                salesOrderQty = soItem.Qty,
                alreadyNotifiedQty = alreadyNotified,
                remainingNotifyQty = remainingNotify,
                availableStockQty = lineAvail,
                purchasedStockAvailableQty = purchasedStock,
                suggestedMaxQty = suggested,
                customerOrderInventoryByRegion = snapshot.CustomerOrderInventoryByRegion,
                stockingAvailabilityByRegion = snapshot.StockingAvailabilityByRegion,
                evaluatedRequestedQty = evalQty,
                customsOption = snapshot.CustomsOption
            };
        }

        /// <summary>
        /// 销售明细须已有关联采购行，且每条关联采购单主表状态 ≥ 供应商确认（30）；
        /// 与列表「申请出库」可点逻辑一致：<see cref="SellOrderItemExtend.PurchasedStock_AvailableQty"/> &gt; 0 时跳过本门槛（备货可用放宽）。
        /// </summary>
        private async Task EnsureSellLineMeetsStockOutPurchaseGateAsync(string sellOrderItemLineId)
        {
            var lineId = sellOrderItemLineId.Trim();

            var ext = await _sellOrderItemExtendRepository.GetByIdAsync(lineId);
            if (ext != null && ext.PurchasedStock_AvailableQty > 0)
                return;

            var min = PurchaseOrderMainStatusCodes.VendorConfirmedOrBeyond;
            var poItems = (await _purchaseOrderItemRepository.FindAsync(i => i.SellOrderItemId == lineId))
                .ToList();
            if (poItems.Count == 0)
                throw new InvalidOperationException("该销售明细尚未生成采购订单明细，不能申请出库");

            var poIds = poItems
                .Select(i => i.PurchaseOrderId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var pid in poIds)
            {
                var po = await _purchaseOrderRepository.GetByIdAsync(pid);
                if (po == null || po.Status < min)
                    throw new InvalidOperationException("关联采购订单尚未供应商确认，不能申请出库");
            }
        }

        private sealed class StockOutApplyInventorySnapshot
        {
            public List<StockOutApplyRegionInventoryDto> CustomerOrderInventoryByRegion { get; init; } = new();
            public List<StockOutApplyStockingRegionAvailabilityDto> StockingAvailabilityByRegion { get; init; } = new();
            public StockOutApplyCustomsOptionDto CustomsOption { get; init; } = new();
        }

        private async Task<StockOutApplyInventorySnapshot> BuildApplyInventorySnapshotAsync(
            string sellOrderItemId,
            string? purchasePn,
            string? purchaseBrand,
            int requestedQty,
            short deliveryRegionType)
        {
            var lineId = sellOrderItemId.Trim();
            var customerLayers = (await _stockItemRepository.FindAsync(si =>
                si.SellOrderItemId == lineId && si.QtyRepertoryAvailable > 0)).ToList();

            var customerByRegion = new[]
            {
                RegionTypeCode.Domestic,
                RegionTypeCode.Overseas
            }.Select(region =>
            {
                var qty = customerLayers
                    .Where(si => RegionTypeCode.Normalize(si.RegionType) == region)
                    .Sum(si => si.QtyRepertoryAvailable);
                return new StockOutApplyRegionInventoryDto
                {
                    regionType = region,
                    hasInventory = qty > 0,
                    availableQty = qty
                };
            }).ToList();

            var stockingLayers = await LoadEligibleStockingLayersAsync(purchasePn, purchaseBrand);
            var stockingByRegion = new[]
            {
                RegionTypeCode.Domestic,
                RegionTypeCode.Overseas
            }.Select(region =>
            {
                var sum = stockingLayers
                    .Where(si => RegionTypeCode.Normalize(si.RegionType) == region)
                    .Sum(si => si.QtyRepertoryAvailable);
                return new StockOutApplyStockingRegionAvailabilityDto
                {
                    regionType = region,
                    isAvailable = requestedQty > 0 && sum >= requestedQty
                };
            }).ToList();

            var customsOption = ResolveCustomsOption(
                deliveryRegionType,
                customerByRegion,
                stockingByRegion);

            return new StockOutApplyInventorySnapshot
            {
                CustomerOrderInventoryByRegion = customerByRegion,
                StockingAvailabilityByRegion = stockingByRegion,
                CustomsOption = customsOption
            };
        }

        private static StockOutApplyCustomsOptionDto ResolveCustomsOption(
            short deliveryRegionType,
            IReadOnlyList<StockOutApplyRegionInventoryDto> customerByRegion,
            IReadOnlyList<StockOutApplyStockingRegionAvailabilityDto> stockingByRegion)
        {
            if (RegionTypeCode.Normalize(deliveryRegionType) != RegionTypeCode.Domestic)
                return new StockOutApplyCustomsOptionDto();

            var custDomestic = customerByRegion.FirstOrDefault(x => x.regionType == RegionTypeCode.Domestic);
            var custOverseas = customerByRegion.FirstOrDefault(x => x.regionType == RegionTypeCode.Overseas);
            var stockingOverseas = stockingByRegion.FirstOrDefault(x => x.regionType == RegionTypeCode.Overseas);

            var hasCustDomestic = custDomestic?.hasInventory == true;
            var hasCustOverseas = custOverseas?.hasInventory == true;
            var stockingOverseasAvail = stockingOverseas?.isAvailable == true;

            if (hasCustOverseas && !hasCustDomestic)
            {
                return new StockOutApplyCustomsOptionDto
                {
                    visible = true,
                    defaultChecked = true,
                    locked = true
                };
            }

            if (!hasCustOverseas && !hasCustDomestic && stockingOverseasAvail)
            {
                return new StockOutApplyCustomsOptionDto
                {
                    visible = true,
                    defaultChecked = false,
                    locked = false
                };
            }

            if (hasCustOverseas && hasCustDomestic)
            {
                return new StockOutApplyCustomsOptionDto
                {
                    visible = true,
                    defaultChecked = false,
                    locked = false
                };
            }

            return new StockOutApplyCustomsOptionDto();
        }

        private static void ValidateCustomsChoiceForCreate(StockOutApplyCustomsOptionDto option, bool useCustoms)
        {
            if (!option.visible)
            {
                if (useCustoms)
                    throw new InvalidOperationException("当前库存状况不支持使用海外仓库并报关。");
                return;
            }

            if (option.locked && !useCustoms)
                throw new InvalidOperationException("客单库存仅在海外仓，须使用海外仓库并报关。");
        }

        private async Task<List<StockItem>> LoadEligibleStockingLayersAsync(string? purchasePn, string? purchaseBrand)
        {
            var pnKey = NormInventoryKey(purchasePn);
            var brKey = NormInventoryKey(purchaseBrand);
            if (string.IsNullOrEmpty(pnKey) || string.IsNullOrEmpty(brKey))
                return new List<StockItem>();

            var layers = (await _stockItemRepository.FindAsync(si =>
                si.StockType == StockInventoryTypeCodes.Stocking
                && si.QtyRepertoryAvailable > 0
                && (si.TransferType == null
                    || si.TransferType != StockItemTransferTypeCodes.ManualTransferSource))).ToList();

            return layers
                .Where(si =>
                    string.Equals(NormInventoryKey(si.PurchasePn), pnKey, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(NormInventoryKey(si.PurchaseBrand), brKey, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static string NormInventoryKey(string? v) => (v ?? string.Empty).Trim();

        /// <summary>
        /// 境内销售出库勾选报关：优先扣减客单境外 <c>stock_item</c>，不足时允许同 PN+品牌境外备货库存。
        /// </summary>
        private async Task<CustomsPendlist> TryBuildCustomsPendlistAsync(
            string sellOrderItemId,
            string? purchasePn,
            string? purchaseBrand,
            int requestedQty,
            string salesStockOutNotifyId,
            string? actingUserId)
        {
            var lineId = sellOrderItemId.Trim();
            var customerOverseasLayers = (await _stockItemRepository.FindAsync(si =>
                si.SellOrderItemId == lineId
                && si.QtyRepertoryAvailable > 0
                && si.RegionType == RegionTypeCode.Overseas)).ToList();
            var customerOverseasAvail = customerOverseasLayers.Sum(si => si.QtyRepertoryAvailable);

            List<StockItem> sourceLayers;
            if (customerOverseasAvail >= requestedQty)
            {
                sourceLayers = customerOverseasLayers;
            }
            else
            {
                var stockingLayers = (await LoadEligibleStockingLayersAsync(purchasePn, purchaseBrand))
                    .Where(si => RegionTypeCode.Normalize(si.RegionType) == RegionTypeCode.Overseas)
                    .ToList();
                var stockingAvail = stockingLayers.Sum(si => si.QtyRepertoryAvailable);
                if (customerOverseasAvail + stockingAvail < requestedQty)
                {
                    throw new InvalidOperationException(
                        $"境外可用库存不足（客单 {customerOverseasAvail} + 备货 {stockingAvail}，需要 {requestedQty}），无法使用海外仓库并报关。");
                }

                sourceLayers = customerOverseasLayers.Concat(stockingLayers).ToList();
            }

            var topWarehouseId = sourceLayers
                .GroupBy(si => si.WarehouseId?.Trim() ?? string.Empty)
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .OrderByDescending(g => g.Sum(x => x.QtyRepertoryAvailable))
                .Select(g => g.Key)
                .FirstOrDefault() ?? string.Empty;

            return new CustomsPendlist
            {
                Id = Guid.NewGuid().ToString(),
                SalesStockOutNotifyId = salesStockOutNotifyId,
                SellOrderItemId = lineId,
                Qty = requestedQty,
                Status = CustomsPendlistStatusCode.Open,
                OverseasWarehouseId = topWarehouseId,
                CreateTime = DateTime.UtcNow,
                CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId),
                IsDeleted = false
            };
        }

        public async Task<IEnumerable<StockOutRequestListItemDto>> GetStockOutRequestListAsync()
        {
            var reqs = (await _stockOutRequestRepository.GetAllAsync())
                .OrderByDescending(x => x.CreateTime)
                .ThenByDescending(x => x.RequestDate)
                .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return await ProjectStockOutRequestListDtosAsync(reqs);
        }

        /// <inheritdoc />
        public async Task<PagedResult<StockOutRequestListItemDto>> GetStockOutRequestListPagedAsync(
            StockOutRequestListQueryRequest? filter,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var paged = await _stockOutRequestListQuery.GetPagedStockOutRequestIdsAsync(
                filter,
                page,
                pageSize,
                cancellationToken);
            if (paged.TotalCount == 0)
            {
                return new PagedResult<StockOutRequestListItemDto>
                {
                    Items = Array.Empty<StockOutRequestListItemDto>(),
                    TotalCount = 0,
                    PageIndex = paged.PageIndex,
                    PageSize = paged.PageSize
                };
            }

            var idOrder = paged.Items.ToList();
            var idSet = idOrder.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var loaded = (await _stockOutRequestRepository.FindAsync(x => idSet.Contains(x.Id))).ToList();
            var byId = loaded.ToDictionary(x => x.Id.Trim(), x => x, StringComparer.OrdinalIgnoreCase);
            var ordered = new List<StockOutRequest>();
            foreach (var id in idOrder)
            {
                if (byId.TryGetValue(id.Trim(), out var ent))
                    ordered.Add(ent);
            }

            var dtos = await ProjectStockOutRequestListDtosAsync(ordered);
            return new PagedResult<StockOutRequestListItemDto>
            {
                Items = dtos,
                TotalCount = paged.TotalCount,
                PageIndex = paged.PageIndex,
                PageSize = paged.PageSize
            };
        }

        private async Task<List<StockOutRequestListItemDto>> ProjectStockOutRequestListDtosAsync(
            IReadOnlyList<StockOutRequest> reqs)
        {
            var soMap = (await _sellOrderRepository.GetAllAsync())
                .ToDictionary(x => x.Id, x => x);
            var reqLineIds = reqs
                .Select(r => r.SalesOrderItemId?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();
            var soItemCurrencyMap = reqLineIds.Count == 0
                ? new Dictionary<string, short>(StringComparer.OrdinalIgnoreCase)
                : (await _sellOrderItemRepository.FindAsync(si => reqLineIds.Contains(si.Id)))
                    .ToDictionary(si => si.Id.Trim(), si => si.Currency, StringComparer.OrdinalIgnoreCase);
            var users = (await _userRepository.GetAllAsync()).ToList();
            var userLoginById = users
                .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var first = g.First();
                        return EntityLookupService.FormatUserLoginName(first) ?? first.UserName ?? "";
                    },
                    StringComparer.OrdinalIgnoreCase);
            var userLoginByLoginKey = users
                .Where(x => !string.IsNullOrWhiteSpace(x.UserName))
                .GroupBy(x => x.UserName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var first = g.First();
                        return EntityLookupService.FormatUserLoginName(first) ?? first.UserName ?? "";
                    },
                    StringComparer.OrdinalIgnoreCase);

            var customers = (await _customerRepository.FindAsync(c => !c.IsDeleted)).ToList();
            var customerIdByDisplayName = CustomerIdResolveHelper.BuildDisplayNameIndex(customers);

            var notifyIds = reqs.Select(r => r.Id).ToList();
            var packingLinkByNotifyId = await ResolvePackingLinkByNotifyIdsAsync(notifyIds);
            var salesNotifyLinkByCustomsNotifyId = await ResolveSalesNotifyLinkByCustomsNotifyIdsAsync(reqs);

            return reqs
                .Select(x =>
                {
                    soMap.TryGetValue(x.SalesOrderId, out var so);
                    var materialModel = string.IsNullOrWhiteSpace(x.MaterialCode) ? null : x.MaterialCode.Trim();
                    var brand = string.IsNullOrWhiteSpace(x.MaterialName) ? null : x.MaterialName.Trim();

                    string? requestUserName = null;
                    if (!string.IsNullOrWhiteSpace(x.RequestUserId))
                    {
                        if (!userLoginById.TryGetValue(x.RequestUserId, out requestUserName))
                            userLoginByLoginKey.TryGetValue(x.RequestUserId, out requestUserName);
                    }

                    packingLinkByNotifyId.TryGetValue(x.Id.Trim(), out var packingLink);
                    salesNotifyLinkByCustomsNotifyId.TryGetValue(x.Id.Trim(), out var salesNotifyLink);

                    return new StockOutRequestListItemDto
                    {
                        Id = x.Id,
                        RequestCode = x.RequestCode,
                        SalesOrderId = x.SalesOrderId,
                        SalesOrderItemId = x.SalesOrderItemId,
                        SalesOrderCode = so?.SellOrderCode,
                        MaterialModel = materialModel,
                        Brand = brand,
                        OutQuantity = x.Quantity,
                        ExpectedStockOutDate = x.RequestDate == default ? null : x.RequestDate,
                        SalesUserName = ResolveSellOrderSalesLogin(so, userLoginById),
                        CustomerId = CustomerIdResolveHelper.ResolveForStockOutNotify(
                                x.CustomerId,
                                so,
                                customerIdByDisplayName)
                            ?? string.Empty,
                        CustomerName = so?.CustomerName,
                        RequestUserId = x.RequestUserId,
                        RequestUserName = requestUserName,
                        RequestDate = x.RequestDate,
                        Status = x.Status,
                        CustomsStatus = x.CustomsStatus,
                        Remark = x.Remark,
                        ShipmentMethod = string.IsNullOrWhiteSpace(x.ShipmentMethod) ? null : x.ShipmentMethod.Trim(),
                        ExpressCompany = string.IsNullOrWhiteSpace(x.ExpressCompany) ? null : x.ExpressCompany.Trim(),
                        PackingId = packingLink.PackingId,
                        PackingCode = packingLink.PackingCode,
                        RegionType = x.RegionType,
                        StockOutType = x.StockOutType,
                        SalesStockOutNotifyId = salesNotifyLink.SalesId,
                        SalesStockOutNotifyCode = salesNotifyLink.SalesCode,
                        Currency = soItemCurrencyMap.TryGetValue(x.SalesOrderItemId.Trim(), out var cur)
                            ? cur
                            : (short)0,
                        CreateTime = x.CreateTime
                    };
                })
                .ToList();
        }

        /// <summary>报关出库通知 → 原销售出库通知单号（经 customs_pendlist）。</summary>
        private async Task<IReadOnlyDictionary<string, (string? SalesId, string? SalesCode)>> ResolveSalesNotifyLinkByCustomsNotifyIdsAsync(
            IReadOnlyList<StockOutRequest> reqs)
        {
            var result = new Dictionary<string, (string? SalesId, string? SalesCode)>(StringComparer.OrdinalIgnoreCase);
            if (reqs == null || reqs.Count == 0)
                return result;

            var customsReqs = reqs
                .Where(r => StockOutTypeCode.NormalizeForNotify(r.StockOutType) == StockOutTypeCode.Customs)
                .ToList();
            if (customsReqs.Count == 0)
                return result;

            var pendlistIds = customsReqs
                .Select(r => r.CustomsPendlistId?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();

            var pendlists = pendlistIds.Count == 0
                ? new List<CustomsPendlist>()
                : (await _customsPendlistRepository.FindAsync(p =>
                    pendlistIds.Contains(p.Id) && !p.IsDeleted)).ToList();

            var pendlistById = pendlists.ToDictionary(p => p.Id.Trim(), p => p, StringComparer.OrdinalIgnoreCase);

            var salesIds = pendlists
                .Select(p => p.SalesStockOutNotifyId?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();

            var salesSorById = salesIds.Count == 0
                ? new Dictionary<string, StockOutRequest>(StringComparer.OrdinalIgnoreCase)
                : (await _stockOutRequestRepository.FindAsync(r => salesIds.Contains(r.Id)))
                    .ToDictionary(r => r.Id.Trim(), r => r, StringComparer.OrdinalIgnoreCase);

            foreach (var notify in customsReqs)
            {
                var notifyId = notify.Id.Trim();
                if (string.IsNullOrWhiteSpace(notify.CustomsPendlistId))
                    continue;
                if (!pendlistById.TryGetValue(notify.CustomsPendlistId.Trim(), out var pendlist))
                    continue;

                var salesId = pendlist.SalesStockOutNotifyId?.Trim();
                if (string.IsNullOrEmpty(salesId))
                    continue;

                salesSorById.TryGetValue(salesId, out var salesSor);
                result[notifyId] = (salesId, salesSor?.RequestCode);
            }

            return result;
        }

        /// <summary>报关出库单 SourceId（报关出库通知 Id）→ 原销售出库通知。</summary>
        private async Task<IReadOnlyDictionary<string, (string? SalesId, string? SalesCode)>> ResolveSalesNotifyLinkByCustomsStockOutNotifyIdsAsync(
            IEnumerable<string> customsNotifyIds)
        {
            var result = new Dictionary<string, (string? SalesId, string? SalesCode)>(StringComparer.OrdinalIgnoreCase);
            var ids = customsNotifyIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ids.Count == 0)
                return result;

            var pendlists = (await _customsPendlistRepository.FindAsync(p =>
                    !p.IsDeleted
                    && p.CustomsStockOutNotifyId != null
                    && ids.Contains(p.CustomsStockOutNotifyId)))
                .ToList();

            var salesIds = pendlists
                .Select(p => p.SalesStockOutNotifyId?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();

            var salesSorById = salesIds.Count == 0
                ? new Dictionary<string, StockOutRequest>(StringComparer.OrdinalIgnoreCase)
                : (await _stockOutRequestRepository.FindAsync(r => salesIds.Contains(r.Id)))
                    .ToDictionary(r => r.Id.Trim(), r => r, StringComparer.OrdinalIgnoreCase);

            foreach (var pendlist in pendlists)
            {
                var customsNotifyId = pendlist.CustomsStockOutNotifyId?.Trim();
                if (string.IsNullOrEmpty(customsNotifyId))
                    continue;
                var salesId = pendlist.SalesStockOutNotifyId?.Trim();
                if (string.IsNullOrEmpty(salesId))
                    continue;
                salesSorById.TryGetValue(salesId, out var salesSor);
                result[customsNotifyId] = (salesId, salesSor?.RequestCode);
            }

            return result;
        }

        /// <summary>出库通知列表：按 <c>packing_item.stockout_notify_id</c> 解析关联装箱单。</summary>
        private async Task<IReadOnlyDictionary<string, (string? PackingId, string? PackingCode)>> ResolvePackingLinkByNotifyIdsAsync(
            IReadOnlyList<string> notifyIds)
        {
            var result = new Dictionary<string, (string? PackingId, string? PackingCode)>(StringComparer.OrdinalIgnoreCase);
            if (notifyIds == null || notifyIds.Count == 0)
                return result;

            var idSet = notifyIds
                .Select(x => x?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Cast<string>()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (idSet.Count == 0)
                return result;

            var packingItems = (await _packingItemRepository.FindAsync(pi =>
                    !pi.IsDeleted && pi.StockOutNotifyId != null && idSet.Contains(pi.StockOutNotifyId)))
                .ToList();
            if (packingItems.Count == 0)
                return result;

            var packingIdSet = packingItems
                .Select(pi => pi.PackingId?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();
            var packingById = packingIdSet.Count == 0
                ? new Dictionary<string, Packing>(StringComparer.OrdinalIgnoreCase)
                : (await _packingRepository.FindAsync(p => packingIdSet.Contains(p.Id) && !p.IsDeleted))
                    .ToDictionary(p => p.Id.Trim(), p => p, StringComparer.OrdinalIgnoreCase);

            foreach (var grp in packingItems.GroupBy(pi => pi.StockOutNotifyId!.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                var links = grp
                    .Select(pi =>
                    {
                        var pid = pi.PackingId?.Trim();
                        if (string.IsNullOrEmpty(pid) || !packingById.TryGetValue(pid, out var pk))
                            return (Id: (string?)null, Code: (string?)null);
                        var code = string.IsNullOrWhiteSpace(pk.Code) ? null : pk.Code.Trim();
                        return (Id: pid, Code: code);
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Code))
                    .GroupBy(x => x.Id!, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (links.Count == 0)
                    continue;

                result[grp.Key] = (
                    links[0].Id,
                    links.Count == 1 ? links[0].Code : string.Join("、", links.Select(x => x.Code)));
            }

            return result;
        }

        private static string? ResolveSellOrderSalesLogin(
            SellOrder? so,
            IReadOnlyDictionary<string, string> userLoginById)
        {
            if (so == null) return null;
            var sid = so.SalesUserId?.Trim();
            if (!string.IsNullOrWhiteSpace(sid) &&
                userLoginById.TryGetValue(sid, out var login) &&
                !string.IsNullOrWhiteSpace(login))
                return login;
            return string.IsNullOrWhiteSpace(so.SalesUserName) ? null : so.SalesUserName.Trim();
        }

        private static void ApplyOutboundTakeToStockAndOptionalLayer(
            StockInfo stock,
            StockItem? layer,
            int takeQty,
            HashSet<StockInfo> changedStocks,
            HashSet<StockItem> changedLayers)
        {
            InventoryStockOutboundMutation.ApplyTake(stock, layer, takeQty);
            changedStocks.Add(stock);
            if (layer != null)
                changedLayers.Add(layer);
        }

        /// <summary>
        /// 执行出库：按拣货明细生成出库单；库存扣减在「生成拣货单」时完成，装箱单批量出库此处不再扣 <c>stockitem</c>。
        /// </summary>
        public async Task<StockOut> ExecuteStockOutAsync(ExecuteStockOutRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(request.WarehouseId))
                throw new ArgumentException("仓库ID不能为空", nameof(request.WarehouseId));

            if (string.IsNullOrWhiteSpace(request.StockOutRequestId))
                throw new InvalidOperationException("执行出库前必须关联出库申请并完成拣货任务");

            var requestId = request.StockOutRequestId.Trim();
            var stockOutRequest = await _stockOutRequestRepository.GetByIdAsync(requestId)
                ?? throw new InvalidOperationException("出库申请不存在");
            var isPackingBatch = request.PackingListBatchStockOut;
            if (!request.SkipStockOutNotifyStatusChecks)
            {
                if (stockOutRequest.Status == StockOutRequestStatusCode.StockedOut)
                    throw new InvalidOperationException("该出库通知已执行出库，请勿重复操作");
                if (stockOutRequest.Status == StockOutRequestStatusCode.Cancelled)
                    throw new InvalidOperationException("该出库通知已取消，不能执行出库");
                if (stockOutRequest.Status == StockOutRequestStatusCode.PendingCustoms)
                    throw new InvalidOperationException("该出库通知待报关，请先完成报关流程后再执行出库");
                if (stockOutRequest.Status == StockOutRequestStatusCode.PendingPacking)
                    throw new InvalidOperationException("请先完成装箱后再执行出库");
            }

            var executePackingId = request.PackingId?.Trim();
            var packingIds = (await _packingItemRepository.FindAsync(pi =>
                    !pi.IsDeleted && pi.StockOutNotifyId != null && pi.StockOutNotifyId == requestId))
                .Select(pi => pi.PackingId?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            List<PickingTask> pickingTasks;
            if (isPackingBatch && !string.IsNullOrEmpty(executePackingId))
            {
                pickingTasks = (await _pickingTaskRepository.FindAsync(t =>
                    !t.IsDeleted && t.PackingId != null && t.PackingId == executePackingId)).ToList();
            }
            else
            {
                pickingTasks = packingIds.Count == 0
                    ? new List<PickingTask>()
                    : (await _pickingTaskRepository.FindAsync(t =>
                        t.PackingId != null && packingIds.Contains(t.PackingId))).ToList();
            }

            if (!isPackingBatch)
            {
                if (!pickingTasks.Any())
                    throw new InvalidOperationException("执行出库前请先生成拣货任务");
                if (!pickingTasks.Any(x => x.Status == 100))
                    throw new InvalidOperationException("执行出库前请先完成拣货任务");
            }

            var sellLineId = stockOutRequest.SalesOrderItemId.Trim();

            var isCustomsOut = StockOutTypeCode.NormalizeForNotify(stockOutRequest.StockOutType) == StockOutTypeCode.Customs;
            if (isCustomsOut)
                await _customsV2FlowService.EnsureCustomsOutReadyAsync(requestId);

            var stockOutCode = string.IsNullOrWhiteSpace(request.StockOutCode)
                ? await _serialNumberService.GenerateNextAsync(ModuleCodes.StockOut)
                : request.StockOutCode.Trim();

            _logger.LogInformation(
                "[SellLineStockOutSync] ExecuteStockOut begin StockOutRequestId={RequestId} SellOrderItemId={SellOrderItemId} WarehouseId={WarehouseId} PlannedStockOutCode={StockOutCode} PackingListBatch={IsPackingBatch}",
                requestId, sellLineId, request.WarehouseId, stockOutCode, isPackingBatch);

            if (request.Items == null || request.Items.Count == 0)
                throw new ArgumentException("出库明细不能为空", nameof(request.Items));

            if (!isPackingBatch)
            {
                var requestQtySum = request.Items.Sum(x => InventoryQuantity.RoundFromDecimal(x.Quantity));
                if (requestQtySum != stockOutRequest.Quantity)
                    throw new InvalidOperationException(
                        $"出库明细数量合计（{requestQtySum}）须与出库通知数量（{stockOutRequest.Quantity}）一致。");
            }

            PickingTask? completedTask;
            List<PickingTaskItem> pickItems;
            if (isPackingBatch)
            {
                completedTask = pickingTasks
                    .OrderByDescending(x => x.Status == 100 ? 1 : 0)
                    .ThenByDescending(x => x.ModifyTime ?? DateTime.MinValue)
                    .ThenByDescending(x => x.CreateTime)
                    .FirstOrDefault();
                pickItems = completedTask == null
                    ? new List<PickingTaskItem>()
                    : (await _pickingTaskItemRepository.GetAllAsync())
                        .Where(x => string.Equals(x.PickingTaskId, completedTask.Id, StringComparison.OrdinalIgnoreCase))
                        .Where(x => !string.IsNullOrWhiteSpace(x.StockItemId) && x.PlanQty > 0)
                        .OrderBy(x => x.CreateTime)
                        .ToList();
            }
            else
            {
                completedTask = pickingTasks
                    .Where(x => x.Status == 100)
                    .OrderByDescending(x => x.ModifyTime ?? DateTime.MinValue)
                    .ThenByDescending(x => x.CreateTime)
                    .FirstOrDefault();
                if (completedTask == null)
                    throw new InvalidOperationException("执行出库前请先完成拣货任务");

                pickItems = (await _pickingTaskItemRepository.GetAllAsync())
                    .Where(x => string.Equals(x.PickingTaskId, completedTask.Id, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.CreateTime)
                    .ToList();
                if (pickItems.Count == 0)
                    throw new InvalidOperationException("拣货任务无明细，无法执行出库");
                if (pickItems.Any(x => string.IsNullOrWhiteSpace(x.StockItemId)))
                    throw new InvalidOperationException(
                        "拣货明细缺少在库明细绑定（stock_item_id），请按新流程保存拣货并完成拣货后再执行出库");

                if (string.IsNullOrEmpty(executePackingId) && packingIds.Count > 0)
                    executePackingId = packingIds[0];
                if (string.IsNullOrEmpty(executePackingId))
                    executePackingId = completedTask.PackingId?.Trim();

                pickItems = await ScopePickItemsToStockOutRequestAsync(
                    pickItems, requestId, executePackingId, sellLineId);
                if (pickItems.Count == 0)
                    throw new InvalidOperationException("当前出库通知无匹配的拣货明细，无法执行出库");
            }

            var stockOutId = Guid.NewGuid().ToString();
            var totalQty = 0;
            decimal totalAmount = 0m;
            short stockOutHeaderRegionType = RegionTypeCode.Domestic;
            var stockOutHeaderRegionCaptured = false;

            // 预加载所有库存，避免多次访问数据库
            var allStocks = (await _stockRepository.GetAllAsync()).ToList();
            var allStockItems = (await _stockItemRepository.GetAllAsync()).ToList();
            var changedStocks = new HashSet<StockInfo>();
            var changedLayers = new HashSet<StockItem>();
            var stocksById = allStocks.ToDictionary(s => s.Id, s => s, StringComparer.OrdinalIgnoreCase);
            var stockItemsById = allStockItems
                .GroupBy(s => s.Id?.Trim() ?? "", StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Key.Length > 0)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var wh = request.WarehouseId.Trim();

            var defaultPackingId = request.PackingId?.Trim();
            if (string.IsNullOrEmpty(defaultPackingId))
                defaultPackingId = completedTask?.PackingId?.Trim();
            if (string.IsNullOrEmpty(defaultPackingId) && packingIds.Count > 0)
                defaultPackingId = packingIds[0];

            var pickPackingItemIds = pickItems
                .Select(p => p.PackingItemId?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();
            var packingItemById = pickPackingItemIds.Count == 0
                ? new Dictionary<string, PackingItem>(StringComparer.OrdinalIgnoreCase)
                : (await _packingItemRepository.FindAsync(pi => pickPackingItemIds.Contains(pi.Id)))
                    .Where(pi => !pi.IsDeleted)
                    .GroupBy(pi => pi.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            IReadOnlyDictionary<string, CustomsDeclarationItem> decItemByPackingItemId =
                new Dictionary<string, CustomsDeclarationItem>(StringComparer.OrdinalIgnoreCase);
            if (isCustomsOut)
            {
                decItemByPackingItemId =
                    await _customsV2FlowService.GetDeclarationItemsMapForCustomsStockOutAsync(requestId);
            }

            foreach (var pickItem in pickItems)
            {
                var takeQty = pickItem.PlanQty;
                if (takeQty <= 0)
                    continue;

                var layerId = pickItem.StockItemId!.Trim();
                if (!stockItemsById.TryGetValue(layerId, out var layer))
                    throw new InvalidOperationException($"拣货引用的在库明细不存在：{layerId}");

                var stockIdPick = pickItem.StockId?.Trim() ?? "";
                if (string.IsNullOrEmpty(stockIdPick) || !stocksById.TryGetValue(stockIdPick, out var stock))
                    throw new InvalidOperationException($"拣货引用的汇总库存不存在：{stockIdPick}");

                if (!string.Equals(layer.StockAggregateId?.Trim(), stock.Id, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("拣货行 stock_id 与在库明细汇总桶不一致");

                if (!string.Equals(layer.WarehouseId?.Trim(), wh, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("拣货明细仓库与执行出库所选仓库不一致");

                // 装箱单列表批量出库：库存已在「生成拣货单」时扣减，此处只落出库单与明细。
                var skipStockMutation = isPackingBatch
                    || InventoryStockOutboundMutation.IsInventoryAlreadyAppliedAtPick(pickItem, layer, takeQty);

                if (!skipStockMutation)
                {
                    if (layer.QtyRepertoryAvailable < takeQty)
                        throw new InvalidOperationException(
                            $"在库明细 {layerId} 可用量不足（当前 {layer.QtyRepertoryAvailable}，需出 {takeQty}），可能库存已变更，请重新拣货。");

                    ApplyOutboundTakeToStockAndOptionalLayer(stock, layer, takeQty, changedStocks, changedLayers);
                }

                if (!stockOutHeaderRegionCaptured)
                {
                    stockOutHeaderRegionType = RegionTypeCode.Normalize(stock.RegionType);
                    stockOutHeaderRegionCaptured = true;
                }

                var linePackingId = defaultPackingId;
                var pickPiId = pickItem.PackingItemId?.Trim();
                if (!string.IsNullOrEmpty(pickPiId)
                    && packingItemById.TryGetValue(pickPiId, out var pickPackingItem)
                    && !string.IsNullOrWhiteSpace(pickPackingItem.PackingId))
                {
                    linePackingId = pickPackingItem.PackingId.Trim();
                }

                var outLine = new StockOutItem
                {
                    Id = Guid.NewGuid().ToString(),
                    StockOutId = stockOutId,
                    MaterialId = layer.MaterialId,
                    PurchasePn = string.IsNullOrWhiteSpace(layer.PurchasePn) ? null : layer.PurchasePn.Trim(),
                    PurchaseBrand = string.IsNullOrWhiteSpace(layer.PurchaseBrand) ? null : layer.PurchaseBrand.Trim(),
                    Quantity = takeQty,
                    OrderQty = stockOutRequest.Quantity,
                    PlanQty = takeQty,
                    PickQty = takeQty,
                    ActualQty = takeQty,
                    Price = 0m,
                    Amount = 0m,
                    StockId = stock.Id,
                    StockItemId = layer.Id,
                    PickingTaskItemId = pickItem.Id,
                    PackingId = linePackingId,
                    WarehouseId = stock.WarehouseId,
                    LocationId = layer.LocationId,
                    BatchNo = layer.BatchNo,
                    CreateTime = DateTime.UtcNow
                };
                await _stockOutItemRepository.AddAsync(outLine);
                var ext = BuildStockOutItemExtend(outLine, layer, stock, takeQty);
                if (isCustomsOut)
                {
                    _customsV2FlowService.ApplyCustomsStockOutExtend(
                        ext, layer, pickItem.PackingItemId, decItemByPackingItemId);
                }

                await _stockOutItemExtendRepository.AddAsync(ext);

                totalQty += takeQty;
            }

            // 批量出库不做 FIFO 扣库存（无拣货明细时应先补拣货/重新生成拣货单）。
            if (!isPackingBatch && pickItems.Count == 0)
            {
                var needQty = stockOutRequest.Quantity;
                var fifoLayers = allStockItems
                    .Where(si =>
                        string.Equals(si.SellOrderItemId?.Trim(), sellLineId, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(si.WarehouseId?.Trim(), wh, StringComparison.OrdinalIgnoreCase)
                        && si.QtyRepertoryAvailable > 0)
                    .OrderBy(si => si.ProductionDate ?? si.CreateTime)
                    .ThenBy(si => si.CreateTime)
                    .ToList();

                foreach (var layer in fifoLayers)
                {
                    if (needQty <= 0)
                        break;

                    var takeQty = Math.Min(needQty, layer.QtyRepertoryAvailable);
                    if (takeQty <= 0)
                        continue;

                    var stockIdPick = layer.StockAggregateId?.Trim() ?? "";
                    if (string.IsNullOrEmpty(stockIdPick) || !stocksById.TryGetValue(stockIdPick, out var stock))
                        continue;

                    if (!stockOutHeaderRegionCaptured)
                    {
                        stockOutHeaderRegionType = RegionTypeCode.Normalize(stock.RegionType);
                        stockOutHeaderRegionCaptured = true;
                    }

                    ApplyOutboundTakeToStockAndOptionalLayer(stock, layer, takeQty, changedStocks, changedLayers);

                    var outLine = new StockOutItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        StockOutId = stockOutId,
                        MaterialId = layer.MaterialId,
                        PurchasePn = string.IsNullOrWhiteSpace(layer.PurchasePn) ? null : layer.PurchasePn.Trim(),
                        PurchaseBrand = string.IsNullOrWhiteSpace(layer.PurchaseBrand) ? null : layer.PurchaseBrand.Trim(),
                        Quantity = takeQty,
                        OrderQty = stockOutRequest.Quantity,
                        PlanQty = takeQty,
                        PickQty = takeQty,
                        ActualQty = takeQty,
                        Price = 0m,
                        Amount = 0m,
                        StockId = stock.Id,
                        StockItemId = layer.Id,
                        PickingTaskItemId = null,
                        PackingId = defaultPackingId,
                        WarehouseId = stock.WarehouseId,
                        LocationId = layer.LocationId,
                        BatchNo = layer.BatchNo,
                        CreateTime = DateTime.UtcNow
                    };
                    await _stockOutItemRepository.AddAsync(outLine);
                    await _stockOutItemExtendRepository.AddAsync(
                        BuildStockOutItemExtend(outLine, layer, stock, takeQty));

                    totalQty += takeQty;
                    needQty -= takeQty;
                }
            }

            if (!isPackingBatch && totalQty != stockOutRequest.Quantity)
                throw new InvalidOperationException("实际扣减数量与出库通知数量不一致，已中止。");

            // 持久化库存变更
            foreach (var stock in changedStocks)
            {
                await _stockRepository.UpdateAsync(stock);
            }

            foreach (var layer in changedLayers)
            {
                await _stockItemRepository.UpdateAsync(layer);
            }

            // SourceCode 字段最大 32 字符，不能写入 36 位 GUID；完整出库通知 ID 放在 SourceId
            var requestCode = stockOutRequest.RequestCode?.Trim() ?? string.Empty;
            if (requestCode.Length > 32)
                requestCode = requestCode.Substring(0, 32);

            var stockOutHeaderType = await ResolveStockOutTypeFromPackingAsync(defaultPackingId);

            var stockOut = new StockOut
            {
                Id = stockOutId,
                StockOutCode = stockOutCode,
                StockOutType = stockOutHeaderType,
                RegionType = stockOutHeaderRegionType,
                SourceCode = string.IsNullOrEmpty(requestCode) ? null : requestCode,
                SourceId = requestId,
                PickingTaskId = completedTask?.Id,
                SellOrderItemId = sellLineId,
                CustomerId = string.IsNullOrWhiteSpace(stockOutRequest.CustomerId) ? null : stockOutRequest.CustomerId.Trim(),
                WarehouseId = request.WarehouseId,
                StockOutDate = request.StockOutDate is { } stockOutDate && stockOutDate != default
                    ? PostgreSqlDateTime.ToUtc(stockOutDate)
                    : null,
                ExpectedStockOutDate = request.ExpectedStockOutDate.HasValue
                    ? PostgreSqlDateTime.ToUtc(request.ExpectedStockOutDate.Value)
                    : null,
                TotalQuantity = totalQty,
                TotalAmount = totalAmount,
                Remark = request.Remark,
                ShipmentMethod = string.IsNullOrWhiteSpace(stockOutRequest.ShipmentMethod)
                    ? null
                    : stockOutRequest.ShipmentMethod.Trim(),
                Status = 2,
                ConfirmedBy = request.OperatorId,
                ConfirmedTime = DateTime.UtcNow,
                CreateTime = DateTime.UtcNow,
                CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId ?? request.OperatorId)
            };

            await _stockOutRepository.AddAsync(stockOut);
            var saveStockOut = await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation(
                "[SellLineStockOutSync] ExecuteStockOut persisted header StockOutId={StockOutId} StockOutCode={StockOutCode} SellOrderItemId={SellOrderItemId} Status={Status} TotalQuantity={TotalQty} SaveChanges={Rows}",
                stockOut.Id, stockOut.StockOutCode, stockOut.SellOrderItemId, stockOut.Status, stockOut.TotalQuantity, saveStockOut);

            await _inventoryCenterService.RecordStockOutAsync(stockOut.Id);
            _logger.LogInformation("[SellLineStockOutSync] ExecuteStockOut RecordStockOutAsync done StockOutId={StockOutId}", stockOut.Id);

            stockOutRequest.Status = StockOutRequestStatusCode.StockedOut;
            stockOutRequest.ModifyTime = DateTime.UtcNow;
            stockOutRequest.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId ?? request.OperatorId);
            await _stockOutRequestRepository.UpdateAsync(stockOutRequest);
            var saveRequest = await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation(
                "[SellLineStockOutSync] ExecuteStockOut request marked executed StockOutRequestId={RequestId} SaveChanges={Rows}",
                requestId, saveRequest);

            _logger.LogInformation(
                "[SellLineStockOutSync] ExecuteStockOut calling RecalculateAsync SellOrderItemId={SellOrderItemId}",
                sellLineId);
            await _sellOrderItemExtendSync.RecalculateAsync(sellLineId);
            try
            {
                await _purchasedStockAvailableSync.TryRecalculateFromChangedStockInfosAsync(changedStocks);
                if (changedLayers.Count > 0)
                    await _purchasedStockAvailableSync.TryRecalculateFromChangedStockItemsAsync(changedLayers);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[PurchasedStockAvail] TryRecalculateFromChangedStockInfos failed after ExecuteStockOut SellOrderItemId={SellOrderItemId}",
                    sellLineId);
            }

            var saveExtend = await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation(
                "[SellLineStockOutSync] ExecuteStockOut after Recalculate SellOrderItemId={SellOrderItemId} SaveChanges={Rows}",
                sellLineId, saveExtend);

            if (isCustomsOut)
                await _customsV2FlowService.OnCustomsStockOutCompletedAsync(requestId, actingUserId);

            if (stockOutHeaderType == StockOutTypeCode.Sales)
                await _financeReceivableService.TryEnsureFromStockOutAsync(stockOut.Id, actingUserId);

            return stockOut;
        }

        public async Task<StockOut?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return await _stockOutRepository.GetByIdAsync(id);
        }

        /// <inheritdoc />
        public async Task<StockOutDetailViewDto?> GetDetailViewAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;
            var x = await _stockOutRepository.GetByIdAsync(id.Trim());
            if (x == null)
                return null;

            SellOrderItem? line = null;
            if (!string.IsNullOrWhiteSpace(x.SellOrderItemId))
                line = await _sellOrderItemRepository.GetByIdAsync(x.SellOrderItemId.Trim());

            SellOrder? so = null;
            if (line != null && !string.IsNullOrWhiteSpace(line.SellOrderId))
                so = await _sellOrderRepository.GetByIdAsync(line.SellOrderId.Trim());

            string? customerName = null;
            if (!string.IsNullOrWhiteSpace(x.CustomerId))
            {
                var cust = await _customerRepository.GetByIdAsync(x.CustomerId.Trim());
                if (cust != null)
                    customerName = string.IsNullOrWhiteSpace(cust.OfficialName) ? cust.CustomerName : cust.OfficialName;
            }

            if (customerName == null && so != null)
                customerName = so.CustomerName;

            string? salesUserName = so?.SalesUserName;
            if (so != null && !string.IsNullOrWhiteSpace(so.SalesUserId))
            {
                var su = await _userRepository.GetByIdAsync(so.SalesUserId.Trim());
                if (su != null)
                    salesUserName = EntityLookupService.FormatUserLoginName(su) ?? salesUserName;
            }

            var sellOrderItemCode = string.IsNullOrWhiteSpace(line?.SellOrderItemCode)
                ? null
                : line!.SellOrderItemCode;

            string? createUserName = null;
            if (!string.IsNullOrWhiteSpace(x.CreateByUserId))
            {
                var u = await _userRepository.GetByIdAsync(x.CreateByUserId.Trim());
                if (u != null)
                    createUserName = EntityLookupService.FormatUserLoginName(u) ?? u.UserName;
            }

            string? warehouseCode = null;
            if (!string.IsNullOrWhiteSpace(x.WarehouseId))
            {
                var wh = await _warehouseRepository.GetByIdAsync(x.WarehouseId.Trim());
                if (wh != null && !string.IsNullOrWhiteSpace(wh.WarehouseCode))
                    warehouseCode = wh.WarehouseCode.Trim();
            }

            string? salesStockOutNotifyId = null;
            string? salesStockOutNotifyCode = null;
            var sourceId = x.SourceId?.Trim();
            if (StockOutTypeCode.NormalizeForNotify(x.StockOutType) == StockOutTypeCode.Customs
                && !string.IsNullOrEmpty(sourceId))
            {
                var salesLinkMap = await ResolveSalesNotifyLinkByCustomsStockOutNotifyIdsAsync(new[] { sourceId });
                if (salesLinkMap.TryGetValue(sourceId, out var salesLink))
                {
                    salesStockOutNotifyId = salesLink.SalesId;
                    salesStockOutNotifyCode = salesLink.SalesCode;
                }
            }

            var listRow = new StockOutListItemDto
            {
                Id = x.Id,
                StockOutCode = x.StockOutCode,
                StockOutType = x.StockOutType,
                SourceCode = x.SourceCode,
                SourceId = x.SourceId,
                SalesStockOutNotifyId = salesStockOutNotifyId,
                SalesStockOutNotifyCode = salesStockOutNotifyCode,
                StockOutDate = x.StockOutDate,
                ExpectedStockOutDate = x.ExpectedStockOutDate,
                TotalQuantity = x.TotalQuantity,
                TotalAmount = x.TotalAmount,
                Status = x.Status,
                Remark = x.Remark,
                CreateTime = x.CreateTime,
                CreateByUserId = x.CreateByUserId,
                CreateUserName = createUserName,
                CustomerName = customerName,
                SalesUserName = salesUserName,
                SellOrderItemCode = sellOrderItemCode,
                ShipmentMethod = string.IsNullOrWhiteSpace(x.ShipmentMethod) ? null : x.ShipmentMethod.Trim(),
                CourierTrackingNo = string.IsNullOrWhiteSpace(x.CourierTrackingNo) ? null : x.CourierTrackingNo.Trim()
            };

            return new StockOutDetailViewDto
            {
                Id = listRow.Id,
                StockOutCode = listRow.StockOutCode,
                StockOutType = listRow.StockOutType,
                SourceCode = listRow.SourceCode,
                SourceId = listRow.SourceId,
                SalesStockOutNotifyId = listRow.SalesStockOutNotifyId,
                SalesStockOutNotifyCode = listRow.SalesStockOutNotifyCode,
                StockOutDate = listRow.StockOutDate,
                TotalQuantity = listRow.TotalQuantity,
                TotalAmount = listRow.TotalAmount,
                Status = listRow.Status,
                Remark = listRow.Remark,
                CreateTime = listRow.CreateTime,
                CreateByUserId = listRow.CreateByUserId,
                CreateUserName = listRow.CreateUserName,
                CustomerName = listRow.CustomerName,
                SalesUserName = listRow.SalesUserName,
                SellOrderItemCode = listRow.SellOrderItemCode,
                ShipmentMethod = listRow.ShipmentMethod,
                CourierTrackingNo = listRow.CourierTrackingNo,
                WarehouseId = x.WarehouseId,
                WarehouseCode = warehouseCode,
                SellOrderItemId = string.IsNullOrWhiteSpace(x.SellOrderItemId) ? null : x.SellOrderItemId.Trim()
            };
        }

        public async Task<IEnumerable<StockOutListItemDto>> GetStockOutListAsync()
        {
            var outs = (await _stockOutRepository.GetAllAsync())
                .Where(x => x.StockOutType != StockOutTypeCode.Transfer)
                .OrderByDescending(x => x.CreateTime)
                .ThenByDescending(x => x.Id)
                .ToList();
            return await ProjectStockOutListDtosForOutsAsync(outs);
        }

        /// <inheritdoc />
        public async Task<PagedResult<StockOutListItemDto>> GetStockOutListPagedAsync(
            StockOutListQueryRequest? filter,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var paged = await _stockOutListQuery.GetPagedStockOutIdsAsync(
                filter,
                page,
                pageSize,
                cancellationToken);
            if (paged.TotalCount == 0)
            {
                return new PagedResult<StockOutListItemDto>
                {
                    Items = Array.Empty<StockOutListItemDto>(),
                    TotalCount = 0,
                    PageIndex = paged.PageIndex,
                    PageSize = paged.PageSize
                };
            }

            var idOrder = paged.Items.ToList();
            var idSet = idOrder.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var loaded = (await _stockOutRepository.FindAsync(x => idSet.Contains(x.Id))).ToList();
            var byId = loaded.ToDictionary(x => x.Id.Trim(), x => x, StringComparer.OrdinalIgnoreCase);
            var ordered = new List<StockOut>();
            foreach (var id in idOrder)
            {
                if (byId.TryGetValue(id.Trim(), out var ent))
                    ordered.Add(ent);
            }

            var dtos = await ProjectStockOutListDtosForOutsAsync(ordered);
            return new PagedResult<StockOutListItemDto>
            {
                Items = dtos,
                TotalCount = paged.TotalCount,
                PageIndex = paged.PageIndex,
                PageSize = paged.PageSize
            };
        }

        private async Task<List<StockOutListItemDto>> ProjectStockOutListDtosForOutsAsync(IReadOnlyList<StockOut> outs)
        {
            var lineIdSet = outs
                .Select(x => x.SellOrderItemId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var itemById = (await _sellOrderItemRepository.GetAllAsync())
                .Where(x => lineIdSet.Contains(x.Id))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var orderIdSet = itemById.Values
                .Select(x => x.SellOrderId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var orderById = (await _sellOrderRepository.GetAllAsync())
                .Where(x => orderIdSet.Contains(x.Id))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var custIdSet = outs
                .Select(x => x.CustomerId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var o in orderById.Values)
            {
                if (!string.IsNullOrWhiteSpace(o.CustomerId))
                    custIdSet.Add(o.CustomerId.Trim());
            }

            var customerById = (await _customerRepository.GetAllAsync())
                .Where(c => custIdSet.Contains(c.Id))
                .GroupBy(c => c.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var users = (await _userRepository.GetAllAsync()).ToList();
            var userLoginById = users
                .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var first = g.First();
                        return EntityLookupService.FormatUserLoginName(first) ?? first.UserName ?? "";
                    },
                    StringComparer.OrdinalIgnoreCase);
            var userLoginByLoginKey = users
                .Where(x => !string.IsNullOrWhiteSpace(x.UserName))
                .GroupBy(x => x.UserName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var first = g.First();
                        return EntityLookupService.FormatUserLoginName(first) ?? first.UserName ?? "";
                    },
                    StringComparer.OrdinalIgnoreCase);

            var stockOutIds = outs.Select(x => x.Id).ToList();
            var packingSummaryById = await ResolvePackingSummaryByStockOutIdAsync(stockOutIds);
            var freightForwarderByStockOutId = await ResolveFreightForwarderOrderNoByStockOutIdAsync(stockOutIds);

            var notifyIdSet = outs
                .Select(x => x.SourceId?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Cast<string>()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var notifyById = notifyIdSet.Count == 0
                ? new Dictionary<string, StockOutRequest>(StringComparer.OrdinalIgnoreCase)
                : (await _stockOutRequestRepository.FindAsync(r => notifyIdSet.Contains(r.Id)))
                    .ToDictionary(r => r.Id.Trim(), r => r, StringComparer.OrdinalIgnoreCase);

            var customsSourceIds = outs
                .Where(x => StockOutTypeCode.NormalizeForNotify(x.StockOutType) == StockOutTypeCode.Customs)
                .Select(x => x.SourceId?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var salesNotifyByCustomsSourceId = await ResolveSalesNotifyLinkByCustomsStockOutNotifyIdsAsync(customsSourceIds);

            return outs
                .Select(x =>
                {
                    SellOrderItem? line = null;
                    if (!string.IsNullOrWhiteSpace(x.SellOrderItemId))
                        itemById.TryGetValue(x.SellOrderItemId.Trim(), out line);

                    SellOrder? so = null;
                    if (line != null && !string.IsNullOrWhiteSpace(line.SellOrderId))
                        orderById.TryGetValue(line.SellOrderId.Trim(), out so);

                    CustomerInfo? cust = null;
                    if (!string.IsNullOrWhiteSpace(x.CustomerId)
                        && customerById.TryGetValue(x.CustomerId.Trim(), out var custDirect))
                    {
                        cust = custDirect;
                    }
                    else if (so != null && !string.IsNullOrWhiteSpace(so.CustomerId)
                             && customerById.TryGetValue(so.CustomerId.Trim(), out var custFromOrder))
                    {
                        cust = custFromOrder;
                    }

                    string? customerName = null;
                    string? customerEnglishName = null;
                    string? customerCode = null;
                    if (cust != null)
                    {
                        customerName = string.IsNullOrWhiteSpace(cust.OfficialName) ? cust.CustomerName : cust.OfficialName;
                        customerEnglishName = string.IsNullOrWhiteSpace(cust.EnglishOfficialName)
                            ? null
                            : cust.EnglishOfficialName.Trim();
                        customerCode = string.IsNullOrWhiteSpace(cust.CustomerCode) ? null : cust.CustomerCode.Trim();
                    }
                    else if (so != null)
                    {
                        customerName = so.CustomerName;
                    }

                    var salesUserName = ResolveSellOrderSalesLogin(so, userLoginById);
                    var sellOrderItemCode = string.IsNullOrWhiteSpace(line?.SellOrderItemCode)
                        ? null
                        : line!.SellOrderItemCode;

                    string? createUserName = null;
                    if (!string.IsNullOrWhiteSpace(x.CreateByUserId))
                    {
                        if (!userLoginById.TryGetValue(x.CreateByUserId, out createUserName))
                            userLoginByLoginKey.TryGetValue(x.CreateByUserId, out createUserName);
                    }

                    var shipmentMethod = string.IsNullOrWhiteSpace(x.ShipmentMethod) ? null : x.ShipmentMethod.Trim();
                    string? expressCompany = null;
                    var sourceId = x.SourceId?.Trim();
                    if (!string.IsNullOrEmpty(sourceId) && notifyById.TryGetValue(sourceId, out var notify))
                    {
                        if (string.IsNullOrWhiteSpace(shipmentMethod))
                            shipmentMethod = string.IsNullOrWhiteSpace(notify.ShipmentMethod)
                                ? null
                                : notify.ShipmentMethod.Trim();
                        expressCompany = string.IsNullOrWhiteSpace(notify.ExpressCompany)
                            ? null
                            : notify.ExpressCompany.Trim();
                    }

                    string? salesStockOutNotifyId = null;
                    string? salesStockOutNotifyCode = null;
                    if (StockOutTypeCode.NormalizeForNotify(x.StockOutType) == StockOutTypeCode.Customs
                        && !string.IsNullOrEmpty(sourceId)
                        && salesNotifyByCustomsSourceId.TryGetValue(sourceId, out var salesLink))
                    {
                        salesStockOutNotifyId = salesLink.SalesId;
                        salesStockOutNotifyCode = salesLink.SalesCode;
                    }

                    return new StockOutListItemDto
                    {
                        Id = x.Id,
                        StockOutCode = x.StockOutCode,
                        StockOutType = x.StockOutType,
                        SourceCode = x.SourceCode,
                        SourceId = x.SourceId,
                        SalesStockOutNotifyId = salesStockOutNotifyId,
                        SalesStockOutNotifyCode = salesStockOutNotifyCode,
                        StockOutDate = x.StockOutDate,
                        ExpectedStockOutDate = x.ExpectedStockOutDate,
                        PackingCount = packingSummaryById.TryGetValue(x.Id, out var packingSummary)
                            ? packingSummary.Count
                            : 0,
                        PackingCodes = packingSummaryById.TryGetValue(x.Id, out var packingSummaryForCodes)
                            ? packingSummaryForCodes.Codes
                            : null,
                        TotalQuantity = x.TotalQuantity,
                        TotalAmount = x.TotalAmount,
                        Status = x.Status,
                        Remark = x.Remark,
                        CreateTime = x.CreateTime,
                        CreateByUserId = x.CreateByUserId,
                        CreateUserName = createUserName,
                        CustomerName = customerName,
                        CustomerEnglishName = customerEnglishName,
                        CustomerCode = customerCode,
                        SalesUserName = salesUserName,
                        SellOrderItemCode = sellOrderItemCode,
                        ShipmentMethod = shipmentMethod,
                        ExpressCompany = expressCompany,
                        CourierTrackingNo = string.IsNullOrWhiteSpace(x.CourierTrackingNo) ? null : x.CourierTrackingNo.Trim(),
                        FreightForwarderOrderNo = freightForwarderByStockOutId.TryGetValue(x.Id, out var ff) && !string.IsNullOrWhiteSpace(ff)
                            ? ff.Trim()
                            : null
                    };
                })
                .ToList();
        }

        private sealed class StockOutPackingSummary
        {
            public int Count { get; init; }
            public string? Codes { get; init; }
        }

        private async Task<IReadOnlyDictionary<string, string>> ResolveFreightForwarderOrderNoByStockOutIdAsync(
            IReadOnlyList<string> stockOutIds)
        {
            if (stockOutIds.Count == 0)
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var items = (await _stockOutItemRepository.FindAsync(x => stockOutIds.Contains(x.StockOutId)))
                .Where(x => !x.IsDeleted)
                .ToList();
            if (items.Count == 0)
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var itemIds = items.Select(x => x.Id.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var extends = (await _stockOutItemExtendRepository.FindAsync(e => itemIds.Contains(e.Id)))
                .Where(e => !string.IsNullOrWhiteSpace(e.PurchaseOrderItemId))
                .ToList();
            if (extends.Count == 0)
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var poiIds = extends
                .Select(e => e.PurchaseOrderItemId!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var pois = (await _purchaseOrderItemRepository.FindAsync(p => poiIds.Contains(p.Id))).ToList();
            var poIds = pois
                .Select(p => p.PurchaseOrderId?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();
            var pos = poIds.Count == 0
                ? new List<PurchaseOrder>()
                : (await _purchaseOrderRepository.FindAsync(p => poIds.Contains(p.Id))).ToList();
            var poiById = pois.GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var poById = pos.GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var ffByOutItemId = extends.ToDictionary(
                e => e.Id.Trim(),
                e => FreightForwarderOrderNoLookup.FromPurchaseOrderItemId(e.PurchaseOrderItemId, poiById, poById) ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);

            var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in items)
            {
                var outId = line.StockOutId?.Trim() ?? string.Empty;
                if (outId.Length == 0) continue;
                if (!ffByOutItemId.TryGetValue(line.Id.Trim(), out var ff) || string.IsNullOrWhiteSpace(ff))
                    continue;
                if (!result.TryGetValue(outId, out var set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    result[outId] = set;
                }
                set.Add(ff.Trim());
            }

            return result.ToDictionary(
                kv => kv.Key,
                kv => FreightForwarderOrderNoDisplay.JoinDistinct(kv.Value),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>按出库单统计关联装箱单（<c>stock_out_item.packing_id</c> 或拣货任务关联）。</summary>
        private async Task<IReadOnlyDictionary<string, StockOutPackingSummary>> ResolvePackingSummaryByStockOutIdAsync(
            IReadOnlyList<string> stockOutIds)
        {
            if (stockOutIds.Count == 0)
                return new Dictionary<string, StockOutPackingSummary>(StringComparer.OrdinalIgnoreCase);

            var idSet = stockOutIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var items = (await _stockOutItemRepository.GetAllAsync())
                .Where(i => !i.IsDeleted && idSet.Contains(i.StockOutId ?? string.Empty))
                .ToList();

            var pickingItemIds = items
                .Select(i => i.PickingTaskItemId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var packingByPickingItem = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (pickingItemIds.Count > 0)
            {
                var pickItems = (await _pickingTaskItemRepository.GetAllAsync())
                    .Where(pti => pickingItemIds.Contains(pti.Id) && !pti.IsDeleted)
                    .Select(pti => new { pti.Id, pti.PickingTaskId })
                    .ToList();
                var taskIds = pickItems
                    .Select(x => x.PickingTaskId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var taskPackingById = (await _pickingTaskRepository.GetAllAsync())
                    .Where(pt => taskIds.Contains(pt.Id) && !pt.IsDeleted && !string.IsNullOrWhiteSpace(pt.PackingId))
                    .ToDictionary(pt => pt.Id, pt => pt.PackingId!.Trim(), StringComparer.OrdinalIgnoreCase);
                foreach (var pi in pickItems)
                {
                    if (taskPackingById.TryGetValue(pi.PickingTaskId, out var packingId))
                        packingByPickingItem[pi.Id] = packingId;
                }
            }

            var packingIdsByStockOut = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var grp in items.GroupBy(i => i.StockOutId?.Trim() ?? string.Empty))
            {
                if (string.IsNullOrEmpty(grp.Key) || !idSet.Contains(grp.Key))
                    continue;

                var packingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in grp)
                {
                    if (!string.IsNullOrWhiteSpace(item.PackingId))
                    {
                        packingIds.Add(item.PackingId.Trim());
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(item.PickingTaskItemId)
                        && packingByPickingItem.TryGetValue(item.PickingTaskItemId.Trim(), out var viaPick))
                    {
                        packingIds.Add(viaPick);
                    }
                }

                packingIdsByStockOut[grp.Key] = packingIds;
            }

            var allPackingIds = packingIdsByStockOut.Values
                .SelectMany(x => x)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var codeByPackingId = allPackingIds.Count == 0
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : (await _packingRepository.FindAsync(p => allPackingIds.Contains(p.Id) && !p.IsDeleted))
                    .GroupBy(p => p.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().Code?.Trim() ?? string.Empty,
                        StringComparer.OrdinalIgnoreCase);

            var result = stockOutIds.ToDictionary(
                id => id,
                _ => new StockOutPackingSummary { Count = 0, Codes = null },
                StringComparer.OrdinalIgnoreCase);
            foreach (var (stockOutId, packingIds) in packingIdsByStockOut)
            {
                var codes = packingIds
                    .Select(pid => codeByPackingId.TryGetValue(pid, out var code) ? code : string.Empty)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                result[stockOutId] = new StockOutPackingSummary
                {
                    Count = packingIds.Count,
                    Codes = codes.Count > 0 ? string.Join(",", codes) : null
                };
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StockOutItemListRowDto>> GetStockOutItemListAsync(StockOutItemListQuery? query)
        {
            query ??= new StockOutItemListQuery();
            var items = (await _stockOutItemRepository.GetAllAsync()).ToList();
            var outById = (await _stockOutRepository.GetAllAsync())
                .ToDictionary(x => x.Id.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

            var lineIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var custIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var x in items)
            {
                if (!outById.TryGetValue(x.StockOutId?.Trim() ?? string.Empty, out var hdr))
                    continue;
                if (!string.IsNullOrWhiteSpace(hdr.SellOrderItemId))
                    lineIdSet.Add(hdr.SellOrderItemId.Trim());
                if (!string.IsNullOrWhiteSpace(hdr.CustomerId))
                    custIdSet.Add(hdr.CustomerId.Trim());
            }

            var itemById = (await _sellOrderItemRepository.GetAllAsync())
                .Where(x => lineIdSet.Contains(x.Id))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var orderIdSet = itemById.Values
                .Select(x => x.SellOrderId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var orderById = (await _sellOrderRepository.GetAllAsync())
                .Where(x => orderIdSet.Contains(x.Id))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var userLoginByIdForSo = (await _userRepository.GetAllAsync())
                .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var first = g.First();
                        return EntityLookupService.FormatUserLoginName(first) ?? first.UserName ?? "";
                    },
                    StringComparer.OrdinalIgnoreCase);

            foreach (var o in orderById.Values)
            {
                if (!string.IsNullOrWhiteSpace(o.CustomerId))
                    custIdSet.Add(o.CustomerId.Trim());
            }

            var customerById = (await _customerRepository.GetAllAsync())
                .Where(c => custIdSet.Contains(c.Id))
                .GroupBy(c => c.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var extendByOutItemId = (await _stockOutItemExtendRepository.GetAllAsync())
                .GroupBy(e => e.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var stockInItemIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in items)
            {
                if (!extendByOutItemId.TryGetValue(line.Id.Trim(), out var ext))
                    continue;
                if (!string.IsNullOrWhiteSpace(ext.StockInItemId))
                    stockInItemIdSet.Add(ext.StockInItemId.Trim());
            }

            var stockInItemById = (await _stockInItemRepository.GetAllAsync())
                .Where(x => stockInItemIdSet.Contains(x.Id.Trim()))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var stockInIdSet = stockInItemById.Values
                .Select(x => x.StockInId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var stockInById = (await _stockInRepository.GetAllAsync())
                .Where(x => stockInIdSet.Contains(x.Id.Trim()))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var codeNeedle = query.StockOutCode?.Trim();
            var custNeedle = query.CustomerName?.Trim();
            var salesNeedle = query.SalesUserName?.Trim();
            var pnNeedle = query.PurchasePn?.Trim();
            var soLineCodeNeedle = query.SellOrderItemCode?.Trim();
            var stockInCodeNeedle = query.StockInCode?.Trim();
            var packingCodeNeedle = query.PackingCode?.Trim();
            var statusFilter = query.Status;

            var packingDisplayByLineId = await ResolvePackingDisplayByStockOutItemIdAsync(items);

            var result = new List<StockOutItemListRowDto>();
            foreach (var line in items)
            {
                if (!outById.TryGetValue(line.StockOutId?.Trim() ?? string.Empty, out var hdr))
                    continue;

                if (statusFilter.HasValue && hdr.Status != statusFilter.Value)
                    continue;
                if (!TextContainsOptional(hdr.StockOutCode, codeNeedle))
                    continue;
                if (!StockOutDateInRange(hdr.StockOutDate, query.StockOutDateFrom, query.StockOutDateTo))
                    continue;

                SellOrderItem? soLine = null;
                if (!string.IsNullOrWhiteSpace(hdr.SellOrderItemId))
                    itemById.TryGetValue(hdr.SellOrderItemId.Trim(), out soLine);

                SellOrder? so = null;
                if (soLine != null && !string.IsNullOrWhiteSpace(soLine.SellOrderId))
                    orderById.TryGetValue(soLine.SellOrderId.Trim(), out so);

                string? customerName = null;
                if (!string.IsNullOrWhiteSpace(hdr.CustomerId)
                    && customerById.TryGetValue(hdr.CustomerId.Trim(), out var cust))
                {
                    customerName = string.IsNullOrWhiteSpace(cust.OfficialName) ? cust.CustomerName : cust.OfficialName;
                }
                else if (so != null)
                    customerName = so.CustomerName;

                if (!TextContainsOptional(customerName, custNeedle))
                    continue;

                var salesUserName = ResolveSellOrderSalesLogin(so, userLoginByIdForSo);
                if (!TextContainsOptional(salesUserName, salesNeedle))
                    continue;

                var sellOrderItemCode = string.IsNullOrWhiteSpace(soLine?.SellOrderItemCode)
                    ? null
                    : soLine!.SellOrderItemCode.Trim();
                if (!TextContainsOptional(sellOrderItemCode, soLineCodeNeedle))
                    continue;

                var pn = string.IsNullOrWhiteSpace(line.PurchasePn) ? null : line.PurchasePn.Trim();
                if (!TextContainsOptional(pn, pnNeedle))
                    continue;

                string? headerStockInCode = null;
                if (extendByOutItemId.TryGetValue(line.Id.Trim(), out var extRow)
                    && !string.IsNullOrWhiteSpace(extRow.StockInItemId)
                    && stockInItemById.TryGetValue(extRow.StockInItemId.Trim(), out var sinIt)
                    && !string.IsNullOrWhiteSpace(sinIt.StockInId)
                    && stockInById.TryGetValue(sinIt.StockInId.Trim(), out var sinHdr))
                {
                    headerStockInCode = string.IsNullOrWhiteSpace(sinHdr.StockInCode) ? null : sinHdr.StockInCode.Trim();
                }

                if (!TextContainsOptional(headerStockInCode, stockInCodeNeedle))
                    continue;

                packingDisplayByLineId.TryGetValue(line.Id.Trim(), out var packingDisplay);
                if (!TextContainsOptional(packingDisplay.Code, packingCodeNeedle))
                    continue;

                var outQty = line.ActualQty > 0 ? line.ActualQty : line.Quantity;

                result.Add(new StockOutItemListRowDto
                {
                    StockOutItemId = line.Id,
                    StockOutId = hdr.Id,
                    Status = hdr.Status,
                    StockOutCode = hdr.StockOutCode,
                    StockOutDate = hdr.StockOutDate,
                    CustomerName = customerName,
                    SalesUserName = salesUserName,
                    PurchasePn = pn,
                    PurchaseBrand = string.IsNullOrWhiteSpace(line.PurchaseBrand) ? null : line.PurchaseBrand.Trim(),
                    OutQuantity = outQty,
                    ShipmentMethod = string.IsNullOrWhiteSpace(hdr.ShipmentMethod) ? null : hdr.ShipmentMethod.Trim(),
                    CourierTrackingNo = string.IsNullOrWhiteSpace(hdr.CourierTrackingNo) ? null : hdr.CourierTrackingNo.Trim(),
                    SellOrderItemCode = sellOrderItemCode,
                    StockInCode = headerStockInCode,
                    PackingId = packingDisplay.Id,
                    PackingCode = packingDisplay.Code
                });
            }

            return result
                .OrderByDescending(x => x.StockOutDate ?? DateTime.MinValue)
                .ThenBy(x => x.StockOutCode, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.StockOutItemId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <inheritdoc />
        public async Task<PagedResult<StockOutItemListRowDto>> GetStockOutItemListPagedAsync(
            StockOutItemListQuery? query,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            query ??= new StockOutItemListQuery();
            var paged = await _stockOutItemListQuery.GetPagedStockOutItemIdsAsync(query, page, pageSize, cancellationToken);
            if (paged.TotalCount == 0)
            {
                return new PagedResult<StockOutItemListRowDto>
                {
                    Items = Array.Empty<StockOutItemListRowDto>(),
                    TotalCount = 0,
                    PageIndex = paged.PageIndex,
                    PageSize = paged.PageSize
                };
            }

            var idOrder = paged.Items.ToList();
            var idSet = idOrder.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var loaded = (await _stockOutItemRepository.FindAsync(x => idSet.Contains(x.Id))).ToList();
            var byId = loaded.ToDictionary(x => x.Id.Trim(), x => x, StringComparer.OrdinalIgnoreCase);
            var ordered = new List<StockOutItem>();
            foreach (var id in idOrder)
            {
                if (byId.TryGetValue(id.Trim(), out var ent))
                    ordered.Add(ent);
            }

            var rows = await BuildStockOutItemListRowsForItemsAsync(ordered);
            return new PagedResult<StockOutItemListRowDto>
            {
                Items = rows,
                TotalCount = paged.TotalCount,
                PageIndex = paged.PageIndex,
                PageSize = paged.PageSize
            };
        }

        private async Task<List<StockOutItemListRowDto>> BuildStockOutItemListRowsForItemsAsync(
            IReadOnlyList<StockOutItem> linesOrdered)
        {
            if (linesOrdered.Count == 0)
                return new List<StockOutItemListRowDto>();

            var outIds = linesOrdered
                .Select(x => x.StockOutId?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var outs = (await _stockOutRepository.FindAsync(x => outIds.Contains(x.Id))).ToList();
            var outById = outs.ToDictionary(x => x.Id.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

            var lineIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var custIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var x in linesOrdered)
            {
                if (!outById.TryGetValue(x.StockOutId?.Trim() ?? string.Empty, out var hdr))
                    continue;
                if (!string.IsNullOrWhiteSpace(hdr.SellOrderItemId))
                    lineIdSet.Add(hdr.SellOrderItemId.Trim());
                if (!string.IsNullOrWhiteSpace(hdr.CustomerId))
                    custIdSet.Add(hdr.CustomerId.Trim());
            }

            var itemById = (await _sellOrderItemRepository.GetAllAsync())
                .Where(x => lineIdSet.Contains(x.Id))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var orderIdSet = itemById.Values
                .Select(x => x.SellOrderId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var orderById = (await _sellOrderRepository.GetAllAsync())
                .Where(x => orderIdSet.Contains(x.Id))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var userLoginByIdForSo = (await _userRepository.GetAllAsync())
                .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var first = g.First();
                        return EntityLookupService.FormatUserLoginName(first) ?? first.UserName ?? "";
                    },
                    StringComparer.OrdinalIgnoreCase);

            foreach (var o in orderById.Values)
            {
                if (!string.IsNullOrWhiteSpace(o.CustomerId))
                    custIdSet.Add(o.CustomerId.Trim());
            }

            var customerById = (await _customerRepository.GetAllAsync())
                .Where(c => custIdSet.Contains(c.Id))
                .GroupBy(c => c.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var lineIdSetForExt = linesOrdered.Select(x => x.Id.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var extendByOutItemId = (await _stockOutItemExtendRepository.GetAllAsync())
                .Where(e => lineIdSetForExt.Contains(e.Id.Trim()))
                .GroupBy(e => e.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var poiIdsForFf = extendByOutItemId.Values
                .Select(e => e.PurchaseOrderItemId?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();
            var poisForFf = poiIdsForFf.Count == 0
                ? new List<PurchaseOrderItem>()
                : (await _purchaseOrderItemRepository.FindAsync(p => poiIdsForFf.Contains(p.Id))).ToList();
            var poIdsForFf = poisForFf
                .Select(p => p.PurchaseOrderId?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();
            var posForFf = poIdsForFf.Count == 0
                ? new List<PurchaseOrder>()
                : (await _purchaseOrderRepository.FindAsync(p => poIdsForFf.Contains(p.Id))).ToList();
            var poiByIdForFf = poisForFf.GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var poByIdForFf = posForFf.GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var stockInItemIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in linesOrdered)
            {
                if (!extendByOutItemId.TryGetValue(line.Id.Trim(), out var ext))
                    continue;
                if (!string.IsNullOrWhiteSpace(ext.StockInItemId))
                    stockInItemIdSet.Add(ext.StockInItemId.Trim());
            }

            var stockInItemById = (await _stockInItemRepository.GetAllAsync())
                .Where(x => stockInItemIdSet.Contains(x.Id.Trim()))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var stockInIdSet = stockInItemById.Values
                .Select(x => x.StockInId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var stockInById = (await _stockInRepository.GetAllAsync())
                .Where(x => stockInIdSet.Contains(x.Id.Trim()))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var packingDisplayByLineId = await ResolvePackingDisplayByStockOutItemIdAsync(linesOrdered);

            var result = new List<StockOutItemListRowDto>();
            foreach (var line in linesOrdered)
            {
                if (!outById.TryGetValue(line.StockOutId?.Trim() ?? string.Empty, out var hdr))
                    continue;

                SellOrderItem? soLine = null;
                if (!string.IsNullOrWhiteSpace(hdr.SellOrderItemId))
                    itemById.TryGetValue(hdr.SellOrderItemId.Trim(), out soLine);

                SellOrder? so = null;
                if (soLine != null && !string.IsNullOrWhiteSpace(soLine.SellOrderId))
                    orderById.TryGetValue(soLine.SellOrderId.Trim(), out so);

                string? customerName = null;
                if (!string.IsNullOrWhiteSpace(hdr.CustomerId)
                    && customerById.TryGetValue(hdr.CustomerId.Trim(), out var cust))
                {
                    customerName = string.IsNullOrWhiteSpace(cust.OfficialName) ? cust.CustomerName : cust.OfficialName;
                }
                else if (so != null)
                {
                    customerName = so.CustomerName;
                }

                var salesUserName = ResolveSellOrderSalesLogin(so, userLoginByIdForSo);
                var sellOrderItemCode = string.IsNullOrWhiteSpace(soLine?.SellOrderItemCode)
                    ? null
                    : soLine!.SellOrderItemCode.Trim();
                var pn = string.IsNullOrWhiteSpace(line.PurchasePn) ? null : line.PurchasePn.Trim();

                string? headerStockInCode = null;
                if (extendByOutItemId.TryGetValue(line.Id.Trim(), out var extRow)
                    && !string.IsNullOrWhiteSpace(extRow.StockInItemId)
                    && stockInItemById.TryGetValue(extRow.StockInItemId.Trim(), out var sinIt)
                    && !string.IsNullOrWhiteSpace(sinIt.StockInId)
                    && stockInById.TryGetValue(sinIt.StockInId.Trim(), out var sinHdr))
                {
                    headerStockInCode = string.IsNullOrWhiteSpace(sinHdr.StockInCode) ? null : sinHdr.StockInCode.Trim();
                }

                var outQty = line.ActualQty > 0 ? line.ActualQty : line.Quantity;
                packingDisplayByLineId.TryGetValue(line.Id.Trim(), out var packingDisplay);

                string? freightForwarderOrderNo = null;
                if (extendByOutItemId.TryGetValue(line.Id.Trim(), out var extFf))
                    freightForwarderOrderNo = FreightForwarderOrderNoLookup.FromPurchaseOrderItemId(
                        extFf.PurchaseOrderItemId, poiByIdForFf, poByIdForFf);

                result.Add(new StockOutItemListRowDto
                {
                    StockOutItemId = line.Id,
                    StockOutId = hdr.Id,
                    Status = hdr.Status,
                    StockOutCode = hdr.StockOutCode,
                    StockOutDate = hdr.StockOutDate,
                    CustomerName = customerName,
                    SalesUserName = salesUserName,
                    PurchasePn = pn,
                    PurchaseBrand = string.IsNullOrWhiteSpace(line.PurchaseBrand) ? null : line.PurchaseBrand.Trim(),
                    OutQuantity = outQty,
                    ShipmentMethod = string.IsNullOrWhiteSpace(hdr.ShipmentMethod) ? null : hdr.ShipmentMethod.Trim(),
                    CourierTrackingNo = string.IsNullOrWhiteSpace(hdr.CourierTrackingNo) ? null : hdr.CourierTrackingNo.Trim(),
                    SellOrderItemCode = sellOrderItemCode,
                    StockInCode = headerStockInCode,
                    PackingId = packingDisplay.Id,
                    PackingCode = packingDisplay.Code,
                    FreightForwarderOrderNo = freightForwarderOrderNo
                });
            }

            return result;
        }

        /// <summary>按出库明细行解析关联装箱单（<c>packing_id</c> 或拣货任务）。</summary>
        private async Task<IReadOnlyDictionary<string, (string? Id, string? Code)>> ResolvePackingDisplayByStockOutItemIdAsync(
            IReadOnlyList<StockOutItem> lines)
        {
            if (lines.Count == 0)
                return new Dictionary<string, (string? Id, string? Code)>(StringComparer.OrdinalIgnoreCase);

            var pickingItemIds = lines
                .Where(l => string.IsNullOrWhiteSpace(l.PackingId) && !string.IsNullOrWhiteSpace(l.PickingTaskItemId))
                .Select(l => l.PickingTaskItemId!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var packingByPickingItem = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (pickingItemIds.Count > 0)
            {
                var pickItems = (await _pickingTaskItemRepository.GetAllAsync())
                    .Where(pti => pickingItemIds.Contains(pti.Id) && !pti.IsDeleted)
                    .Select(pti => new { pti.Id, pti.PickingTaskId })
                    .ToList();
                var taskIds = pickItems
                    .Select(x => x.PickingTaskId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var taskPackingById = (await _pickingTaskRepository.GetAllAsync())
                    .Where(pt => taskIds.Contains(pt.Id) && !pt.IsDeleted && !string.IsNullOrWhiteSpace(pt.PackingId))
                    .ToDictionary(pt => pt.Id, pt => pt.PackingId!.Trim(), StringComparer.OrdinalIgnoreCase);
                foreach (var pi in pickItems)
                {
                    if (taskPackingById.TryGetValue(pi.PickingTaskId, out var packingId))
                        packingByPickingItem[pi.Id] = packingId;
                }
            }

            var packingIdByLineId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in lines)
            {
                var lineId = line.Id.Trim();
                if (!string.IsNullOrWhiteSpace(line.PackingId))
                    packingIdByLineId[lineId] = line.PackingId.Trim();
                else if (!string.IsNullOrWhiteSpace(line.PickingTaskItemId)
                         && packingByPickingItem.TryGetValue(line.PickingTaskItemId.Trim(), out var viaPick))
                {
                    packingIdByLineId[lineId] = viaPick;
                }
            }

            var allPackingIds = packingIdByLineId.Values
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var codeByPackingId = allPackingIds.Count == 0
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : (await _packingRepository.FindAsync(p => allPackingIds.Contains(p.Id) && !p.IsDeleted))
                    .GroupBy(p => p.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().Code?.Trim() ?? string.Empty,
                        StringComparer.OrdinalIgnoreCase);

            var result = new Dictionary<string, (string? Id, string? Code)>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in lines)
            {
                var lineId = line.Id.Trim();
                if (!packingIdByLineId.TryGetValue(lineId, out var packingId))
                {
                    result[lineId] = (null, null);
                    continue;
                }

                string? code = null;
                if (codeByPackingId.TryGetValue(packingId, out var c) && !string.IsNullOrWhiteSpace(c))
                    code = c;
                result[lineId] = (packingId, code);
            }

            return result;
        }

        private static bool TextContainsOptional(string? haystack, string? needleTrimmedOrNull)
        {
            if (string.IsNullOrEmpty(needleTrimmedOrNull))
                return true;
            if (string.IsNullOrEmpty(haystack))
                return false;
            return haystack.Contains(needleTrimmedOrNull, StringComparison.OrdinalIgnoreCase);
        }

        private static DateTime DateStartUtc(DateTime d)
        {
            var utc = d.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : d.ToUniversalTime();
            return utc.Date;
        }

        private static bool StockOutDateInRange(DateTime? stockOutDate, DateTime? from, DateTime? to)
        {
            if (!from.HasValue && !to.HasValue)
                return true;
            if (!stockOutDate.HasValue)
                return false;
            var t = DateStartUtc(stockOutDate.Value);
            if (from.HasValue && t < DateStartUtc(from.Value))
                return false;
            if (to.HasValue && t >= DateStartUtc(to.Value).AddDays(1))
                return false;
            return true;
        }

        /// <inheritdoc />
        public async Task MarkFinishedAsync(string id, MarkStockOutFinishedRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.StockOutDate == default)
                throw new ArgumentException("请填写实际出库日期", nameof(request));
            if (string.IsNullOrWhiteSpace(request.CourierTrackingNo))
                throw new ArgumentException("请填写快递单号", nameof(request));

            var stockOut = await _stockOutRepository.GetByIdAsync(id.Trim())
                ?? throw new InvalidOperationException($"出库单 {id} 不存在");

            if (stockOut.Status == 4)
                throw new InvalidOperationException("该出库单已是完成状态");

            stockOut.StockOutDate = PostgreSqlDateTime.ToUtc(request.StockOutDate);
            stockOut.CourierTrackingNo = request.CourierTrackingNo.Trim();
            stockOut.Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim();
            stockOut.ModifyTime = DateTime.UtcNow;
            stockOut.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
            await _stockOutRepository.UpdateAsync(stockOut);
            await _unitOfWork.SaveChangesAsync();

            await UpdateStatusAsync(id.Trim(), 4, actingUserId);
        }

        /// <inheritdoc />
        public async Task UpdateHeaderAsync(string id, UpdateStockOutHeaderRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var stockOut = await _stockOutRepository.GetByIdAsync(id.Trim());
            if (stockOut == null)
                throw new InvalidOperationException($"出库单 {id} 不存在");

            stockOut.StockOutDate = PostgreSqlDateTime.ToUtc(request.StockOutDate);
            stockOut.ShipmentMethod = string.IsNullOrWhiteSpace(request.ShipmentMethod)
                ? null
                : request.ShipmentMethod.Trim();
            stockOut.CourierTrackingNo = string.IsNullOrWhiteSpace(request.CourierTrackingNo)
                ? null
                : request.CourierTrackingNo.Trim();
            stockOut.ModifyTime = DateTime.UtcNow;
            stockOut.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);

            await _stockOutRepository.UpdateAsync(stockOut);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task ForceDeleteWithInventoryRollbackAsync(string id, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var stockOut = await _stockOutRepository.GetByIdAsync(id.Trim())
                ?? throw new InvalidOperationException($"出库单 {id} 不存在");

            var lineItems = (await _stockOutItemRepository.FindAsync(x => x.StockOutId == stockOut.Id)).ToList();
            var itemIds = lineItems
                .Select(x => x.Id)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var exts = (await _stockOutItemExtendRepository.GetAllAsync())
                .Where(x => itemIds.Contains(x.Id))
                .ToList();
            var originalOutLedgers = (await _ledgerRepository.FindAsync(x =>
                    x.BizType == "STOCK_OUT"
                    && x.BizId == stockOut.Id
                    && x.BizLineId != null))
                .ToList();
            var outLedgerByLineId = originalOutLedgers
                .Where(x => !string.IsNullOrWhiteSpace(x.BizLineId))
                .GroupBy(x => x.BizLineId!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var changedStocks = new HashSet<StockInfo>();
            var changedLayers = new HashSet<StockItem>();
            var reverseLedgerRows = new List<InventoryLedger>();
            var stocksById = new Dictionary<string, StockInfo>(StringComparer.OrdinalIgnoreCase);
            var layersById = new Dictionary<string, StockItem>(StringComparer.OrdinalIgnoreCase);
            if (IsOutboundDoneStatus(stockOut.Status))
            {
                stocksById = (await _stockRepository.GetAllAsync())
                    .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
                layersById = (await _stockItemRepository.GetAllAsync())
                    .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                foreach (var line in lineItems)
                {
                    var rollbackQty = line.ActualQty > 0 ? line.ActualQty : line.Quantity;
                    if (rollbackQty <= 0)
                        continue;

                    StockInfo? ledgerStock = null;
                    if (!string.IsNullOrWhiteSpace(line.StockId)
                        && stocksById.TryGetValue(line.StockId.Trim(), out var stock))
                    {
                        stock.QtyStockOut = Math.Max(0, stock.QtyStockOut - rollbackQty);
                        stock.QtyRepertory = stock.Qty - stock.QtyStockOut;
                        stock.QtyRepertoryAvailable = stock.QtyRepertory - stock.QtyOccupy - stock.QtySales;
                        stock.ModifyTime = DateTime.UtcNow;
                        changedStocks.Add(stock);
                        ledgerStock = stock;
                    }

                    if (!string.IsNullOrWhiteSpace(line.StockItemId)
                        && layersById.TryGetValue(line.StockItemId.Trim(), out var layer))
                    {
                        layer.QtyStockOut = Math.Max(0, layer.QtyStockOut - rollbackQty);
                        layer.QtyRepertory = layer.QtyInbound - layer.QtyStockOut;
                        layer.QtyRepertoryAvailable = layer.QtyRepertory - layer.QtyOccupy - layer.QtySales;
                        layer.SyncStockOutStatusFromQuantities();
                        layer.ModifyTime = DateTime.UtcNow;
                        changedLayers.Add(layer);

                        if (ledgerStock == null
                            && !string.IsNullOrWhiteSpace(layer.StockAggregateId)
                            && stocksById.TryGetValue(layer.StockAggregateId.Trim(), out var stockFromLayer))
                        {
                            ledgerStock = stockFromLayer;
                        }
                    }

                    reverseLedgerRows.Add(new InventoryLedger
                    {
                        Id = Guid.NewGuid().ToString(),
                        BizType = "STOCK_OUT_REVERSE",
                        BizId = stockOut.Id,
                        BizLineId = line.Id,
                        MaterialId = line.MaterialId,
                        WarehouseId = string.IsNullOrWhiteSpace(line.WarehouseId) ? stockOut.WarehouseId : line.WarehouseId!,
                        LocationId = line.LocationId,
                        BatchNo = line.BatchNo,
                        QtyIn = rollbackQty,
                        QtyOut = 0,
                        UnitCost = outLedgerByLineId.TryGetValue(line.Id.Trim(), out var outLedger)
                            ? outLedger.UnitCost
                            : 0m,
                        Amount = outLedgerByLineId.TryGetValue(line.Id.Trim(), out outLedger)
                            ? Math.Round(Math.Abs(outLedger.UnitCost) * rollbackQty, 2, MidpointRounding.AwayFromZero)
                            : 0m,
                        Currency = outLedgerByLineId.TryGetValue(line.Id.Trim(), out outLedger)
                            ? (outLedger.Currency > 0 ? outLedger.Currency : (short)CurrencyCode.RMB)
                            : (short)CurrencyCode.RMB,
                        PurchaseOrderItemCode = ledgerStock?.PurchaseOrderItemCode,
                        PurchaseOrderItemId = ledgerStock?.PurchaseOrderItemId,
                        SellOrderItemCode = ledgerStock?.SellOrderItemCode,
                        SellOrderItemId = ledgerStock?.SellOrderItemId,
                        Remark = $"强制删除出库单反向冲销 {stockOut.StockOutCode}",
                        CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId),
                        CreateTime = DateTime.UtcNow
                    });
                }
            }

            foreach (var stock in changedStocks)
                await _stockRepository.UpdateAsync(stock);
            foreach (var layer in changedLayers)
                await _stockItemRepository.UpdateAsync(layer);
            foreach (var ledger in reverseLedgerRows)
                await _ledgerRepository.AddAsync(ledger);

            foreach (var ext in exts)
                await _stockOutItemExtendRepository.DeleteAsync(ext.Id);
            foreach (var item in lineItems)
                await _stockOutItemRepository.DeleteAsync(item.Id);
            await _stockOutRepository.DeleteAsync(stockOut.Id);

            if (!string.IsNullOrWhiteSpace(stockOut.SourceId))
            {
                var sourceId = stockOut.SourceId.Trim();
                var req = await _stockOutRequestRepository.GetByIdAsync(sourceId);
                if (req != null && req.Status == StockOutRequestStatusCode.StockedOut)
                {
                    var otherDone = (await _stockOutRepository.FindAsync(x => x.SourceId == sourceId))
                        .Any(x => !string.Equals(x.Id, stockOut.Id, StringComparison.OrdinalIgnoreCase)
                                  && IsOutboundDoneStatus(x.Status));
                    if (!otherDone)
                    {
                        req.Status = StockOutRequestStatusCode.Packed;
                        req.ModifyTime = DateTime.UtcNow;
                        req.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
                        await _stockOutRequestRepository.UpdateAsync(req);
                    }
                }
            }

            var lineId = !string.IsNullOrWhiteSpace(stockOut.SellOrderItemId)
                ? stockOut.SellOrderItemId.Trim()
                : null;
            if (!string.IsNullOrWhiteSpace(lineId))
            {
                await _sellOrderItemExtendSync.RecalculateAsync(lineId);
            }
            if (changedStocks.Count > 0)
            {
                await _purchasedStockAvailableSync.TryRecalculateFromChangedStockInfosAsync(changedStocks);
            }
            if (changedLayers.Count > 0)
            {
                await _purchasedStockAvailableSync.TryRecalculateFromChangedStockItemsAsync(changedLayers);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task ForceDeleteStockOutRequestAsync(string id, string confirmBillCode, string actingUserId, string? actingUserName)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));
            if (string.IsNullOrWhiteSpace(confirmBillCode))
                throw new ArgumentException("请填写 confirmBillCode", nameof(confirmBillCode));
            if (string.IsNullOrWhiteSpace(actingUserId))
                throw new ArgumentException("操作人不能为空", nameof(actingUserId));

            var entity = await _stockOutRequestRepository.GetByIdAsync(id.Trim())
                ?? throw new InvalidOperationException("出库通知不存在");
            if (!string.Equals(confirmBillCode.Trim(), entity.RequestCode?.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("确认单号不匹配，已拒绝删除");

            var guard = await _forceDeleteGuard.CanForceDeleteStockOutRequestAsync(entity.Id);
            if (!guard.CanDelete)
                throw new ArgumentException(guard.Message);

            await _stockOutRequestRepository.DeleteAsync(entity.Id);
            await _unitOfWork.SaveChangesAsync();

            var recordCode = string.IsNullOrWhiteSpace(entity.RequestCode) ? null : entity.RequestCode.Trim();
            await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
            {
                BizType = BusinessLogTypes.StockOut,
                RecordId = entity.Id,
                RecordCode = recordCode,
                EntityDisplayName = DeleteLogEntityNames.StockOutRequest,
                IsForceDelete = true,
                ForceConfirmBillCode = confirmBillCode.Trim(),
                OperatorUserId = actingUserId.Trim(),
                OperatorUserName = actingUserName?.Trim(),
                OperationDescOverride = $"强制删除出库通知 RequestId={entity.Id}，确认单号={recordCode}"
            });
        }

        /// <inheritdoc />
        public async Task ForceDeleteStockOutAsync(string id, string confirmBillCode, string actingUserId, string? actingUserName)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));
            if (string.IsNullOrWhiteSpace(confirmBillCode))
                throw new ArgumentException("请填写 confirmBillCode", nameof(confirmBillCode));
            if (string.IsNullOrWhiteSpace(actingUserId))
                throw new ArgumentException("操作人不能为空", nameof(actingUserId));

            var entity = await _stockOutRepository.GetByIdAsync(id.Trim())
                ?? throw new InvalidOperationException("出库单不存在");
            if (string.IsNullOrWhiteSpace(entity.StockOutCode)
                || !string.Equals(confirmBillCode.Trim(), entity.StockOutCode.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("确认单号不匹配，已拒绝删除");

            var guard = await _forceDeleteGuard.CanForceDeleteStockOutAsync(entity.Id);
            if (!guard.CanDelete)
                throw new ArgumentException(guard.Message);

            await ForceDeleteWithInventoryRollbackAsync(entity.Id, actingUserId.Trim());

            var recordCode = string.IsNullOrWhiteSpace(entity.StockOutCode) ? null : entity.StockOutCode.Trim();
            await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
            {
                BizType = BusinessLogTypes.StockOut,
                RecordId = entity.Id,
                RecordCode = recordCode,
                EntityDisplayName = DeleteLogEntityNames.StockOut,
                IsForceDelete = true,
                ForceConfirmBillCode = confirmBillCode.Trim(),
                OperatorUserId = actingUserId.Trim(),
                OperatorUserName = actingUserName?.Trim(),
                OperationDescOverride = $"强制删除出库单 StockOutId={entity.Id}，确认单号={recordCode}"
            });
        }

        private static bool IsOutboundDoneStatus(short status) => status == 2 || status == 4;

        public async Task UpdateStatusAsync(string id, short status, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var stockOut = await _stockOutRepository.GetByIdAsync(id);
            if (stockOut == null)
                throw new InvalidOperationException($"出库单 {id} 不存在");

            if (stockOut.StockOutType == StockOutTypeCode.Transfer)
            {
                if (stockOut.Status == status)
                    return;
                throw new InvalidOperationException("调拨类虚拟出库单不可通过此入口变更状态。");
            }

            var previousStatus = stockOut.Status;
            stockOut.Status = status;
            stockOut.ModifyTime = DateTime.UtcNow;
            stockOut.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);

            _logger.LogInformation(
                "[SellLineStockOutSync] UpdateStatus begin StockOutId={StockOutId} StockOutCode={StockOutCode} Type={StockOutType} PrevStatus={Prev} NewStatus={New} SellOrderItemId={SellOrderItemId} SourceId={SourceId}",
                stockOut.Id,
                stockOut.StockOutCode,
                stockOut.StockOutType,
                previousStatus,
                status,
                stockOut.SellOrderItemId ?? "(null)",
                stockOut.SourceId ?? "(null)");

            await _stockOutRepository.UpdateAsync(stockOut);
            var saveHeader = await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation(
                "[SellLineStockOutSync] UpdateStatus header saved StockOutId={StockOutId} SaveChanges={Rows}",
                stockOut.Id, saveHeader);

            // 销售出库：进入或离开「已出库/已完成」时须刷新销售明细扩展（汇总仅含 2、4）；扩展变更需 SaveChanges 才落库（与入库链一致）
            const short stockOutCompleted = 2;
            const short stockOutFinished = 4;
            static bool IsOutboundDone(short s) => s == stockOutCompleted || s == stockOutFinished;

            if (stockOut.StockOutType != StockOutTypeCode.Sales)
            {
                _logger.LogInformation(
                    "[SellLineStockOutSync] UpdateStatus skip extend chain (not sales stock-out) StockOutId={StockOutId} StockOutType={StockOutType}",
                    stockOut.Id,
                    stockOut.StockOutType);
                return;
            }

            if (IsOutboundDone(status) && !IsOutboundDone(previousStatus))
                await _financeReceivableService.TryEnsureFromStockOutAsync(stockOut.Id, actingUserId);
            else if (!IsOutboundDone(status) && IsOutboundDone(previousStatus))
                await _financeReceivableService.TrySoftDeleteForStockOutAsync(stockOut.Id, actingUserId);

            StockOutRequest? sorForLine = null;
            if (!string.IsNullOrWhiteSpace(stockOut.SourceId))
                sorForLine = await _stockOutRequestRepository.GetByIdAsync(stockOut.SourceId.Trim());

            if (IsOutboundDone(status) && !IsOutboundDone(previousStatus))
            {
                if (sorForLine != null
                    && sorForLine.Status != StockOutRequestStatusCode.StockedOut
                    && sorForLine.Status != StockOutRequestStatusCode.Cancelled)
                {
                    sorForLine.Status = StockOutRequestStatusCode.StockedOut;
                    sorForLine.ModifyTime = DateTime.UtcNow;
                    sorForLine.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
                    await _stockOutRequestRepository.UpdateAsync(sorForLine);
                    var saveSor = await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation(
                        "[SellLineStockOutSync] UpdateStatus stockoutrequest marked fulfilled StockOutRequestId={RequestId} SaveChanges={Rows}",
                        sorForLine.Id,
                        saveSor);
                }
            }

            var soLineId = !string.IsNullOrWhiteSpace(stockOut.SellOrderItemId)
                ? stockOut.SellOrderItemId.Trim()
                : sorForLine?.SalesOrderItemId?.Trim();
            if (string.IsNullOrWhiteSpace(soLineId))
            {
                _logger.LogWarning(
                    "[SellLineStockOutSync] UpdateStatus cannot resolve SellOrderItemId (header null and no request line) StockOutId={StockOutId} SourceId={SourceId}",
                    stockOut.Id,
                    stockOut.SourceId ?? "(null)");
                return;
            }

            var extendRefresh = IsOutboundDone(status) || IsOutboundDone(previousStatus);
            if (!extendRefresh)
            {
                _logger.LogInformation(
                    "[SellLineStockOutSync] UpdateStatus skip Recalculate (neither prev nor new status is outbound-done 2|4) StockOutId={StockOutId} Prev={Prev} New={New}",
                    stockOut.Id,
                    previousStatus,
                    status);
                return;
            }

            _logger.LogInformation(
                "[SellLineStockOutSync] UpdateStatus calling RecalculateAsync SellOrderItemId={SellOrderItemId}",
                soLineId);
            await _sellOrderItemExtendSync.RecalculateAsync(soLineId);
            var saveExtend = await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation(
                "[SellLineStockOutSync] UpdateStatus after Recalculate SellOrderItemId={SellOrderItemId} SaveChanges={Rows}",
                soLineId,
                saveExtend);
        }

        /// <summary>每条出库明细对应一条扩展行（主键与 <see cref="StockOutItem.Id"/> 相同）。</summary>
        /// <param name="lineQty">本条出库数量（与明细 <c>ActualQty</c>/<c>Quantity</c> 一致，本笔为 takeQty）。</param>
        /// <param name="aggregatePricingLayer">仅汇总层出库时：用于价快照的在库明细（采/销折 USD 与扩展行利润计算同源）。</param>
        private static StockOutItemExtend BuildStockOutItemExtend(
            StockOutItem outLine,
            StockItem? layer,
            StockInfo stock,
            int lineQty,
            StockItem? aggregatePricingLayer = null)
        {
            var ext = new StockOutItemExtend
            {
                Id = outLine.Id,
                StockItemId = string.IsNullOrWhiteSpace(outLine.StockItemId) ? null : outLine.StockItemId.Trim(),
                CreateTime = DateTime.UtcNow,
                QtyStockOut = lineQty,
            };
            if (layer != null)
            {
                FillStockOutItemExtendPricingFromLayer(ext, layer, lineQty);
                return ext;
            }

            ext.StockType = stock.StockType;
            ext.SellOrderItemId = string.IsNullOrWhiteSpace(stock.SellOrderItemId) ? null : stock.SellOrderItemId.Trim();
            ext.SellOrderItemCode = string.IsNullOrWhiteSpace(stock.SellOrderItemCode) ? null : stock.SellOrderItemCode.Trim();
            ext.PurchaseOrderItemId = string.IsNullOrWhiteSpace(stock.PurchaseOrderItemId) ? null : stock.PurchaseOrderItemId.Trim();
            ext.PurchaseOrderItemCode = string.IsNullOrWhiteSpace(stock.PurchaseOrderItemCode) ? null : stock.PurchaseOrderItemCode.Trim();
            if (aggregatePricingLayer != null)
                FillStockOutItemExtendPricingFromLayer(ext, aggregatePricingLayer, lineQty);
            else
            {
                ext.PurchasePrice = 0m;
                ext.PurchaseCurrency = (short)CurrencyCode.RMB;
                ext.PurchasePriceUsd = 0m;
                ext.SalesPrice = null;
                ext.SalesCurrency = null;
                ext.SalesPriceUsd = null;
                ext.ProfitOutBizUsd = 0m;
            }

            return ext;
        }

        /// <summary>
        /// 扩展行利润与 <see cref="StockItem.ComputeProfitOutBizUsd"/> 公式一致（数量用本条出库量；<see cref="StockItem.ProfitOutBizUsd"/> 为入库 × <c>QtyInbound</c> 快照），
        /// 数量参数为<strong>本条出库明细的出库数量</strong> <paramref name="lineQty"/>（非层上累计 <c>QtyStockOut</c>）。
        /// </summary>
        private static void FillStockOutItemExtendPricingFromLayer(StockOutItemExtend ext, StockItem layer, int lineQty)
        {
            ext.StockType = layer.StockType;
            ext.StockInItemId = string.IsNullOrWhiteSpace(layer.StockInItemId) ? null : layer.StockInItemId.Trim();
            ext.StockInItemCode = string.IsNullOrWhiteSpace(layer.StockInItemCode) ? null : layer.StockInItemCode.Trim();
            ext.SellOrderItemId = string.IsNullOrWhiteSpace(layer.SellOrderItemId) ? null : layer.SellOrderItemId.Trim();
            ext.SellOrderItemCode = string.IsNullOrWhiteSpace(layer.SellOrderItemCode) ? null : layer.SellOrderItemCode.Trim();
            ext.PurchaseOrderItemId = string.IsNullOrWhiteSpace(layer.PurchaseOrderItemId) ? null : layer.PurchaseOrderItemId.Trim();
            ext.PurchaseOrderItemCode = string.IsNullOrWhiteSpace(layer.PurchaseOrderItemCode) ? null : layer.PurchaseOrderItemCode.Trim();
            ext.PurchasePrice = layer.PurchasePrice;
            ext.PurchaseCurrency = layer.PurchaseCurrency;
            ext.PurchasePriceUsd = layer.PurchasePriceUsd;
            ext.SalesPrice = layer.SalesPrice;
            ext.SalesCurrency = layer.SalesCurrency;
            ext.SalesPriceUsd = layer.SalesPriceUsd;
            ext.ProfitOutBizUsd = StockItem.ComputeProfitOutBizUsd(
                layer.SellOrderItemId,
                layer.SalesPriceUsd,
                layer.PurchasePriceUsd,
                lineQty);
        }

        /// <summary>
        /// 装箱单出库：仅保留与当前出库通知绑定的装箱明细行上的拣货行（避免整单拣货合计与单条通知数量校验冲突）。
        /// 无装箱范围或未匹配到装箱行时返回原列表。
        /// </summary>
        private async Task<List<PickingTaskItem>> ScopePickItemsToStockOutRequestAsync(
            List<PickingTaskItem> pickItems,
            string stockOutRequestId,
            string? packingId,
            string sellOrderItemId)
        {
            if (string.IsNullOrWhiteSpace(packingId))
                return pickItems;

            var packingIdTrim = packingId.Trim();
            var reqId = stockOutRequestId.Trim();
            var sellLine = sellOrderItemId.Trim();
            var packingItems = (await _packingItemRepository.FindAsync(pi =>
                !pi.IsDeleted && pi.PackingId == packingIdTrim)).ToList();

            var linkedPackingItemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pi in packingItems)
            {
                var piId = pi.Id.Trim();
                var notifyId = pi.StockOutNotifyId?.Trim();
                if (!string.IsNullOrEmpty(notifyId)
                    && string.Equals(notifyId, reqId, StringComparison.OrdinalIgnoreCase))
                {
                    linkedPackingItemIds.Add(piId);
                    continue;
                }

                if (string.IsNullOrEmpty(notifyId)
                    && !string.IsNullOrEmpty(pi.SellOrderItemId?.Trim())
                    && string.Equals(pi.SellOrderItemId.Trim(), sellLine, StringComparison.OrdinalIgnoreCase))
                {
                    linkedPackingItemIds.Add(piId);
                }
            }

            if (linkedPackingItemIds.Count == 0)
                return pickItems;

            return pickItems
                .Where(x => !string.IsNullOrWhiteSpace(x.PackingItemId)
                    && linkedPackingItemIds.Contains(x.PackingItemId!.Trim()))
                .ToList();
        }

        /// <summary>汇总层出库无拣货 <c>stockitem</c> 时，找同桶、同销售行、同物料的一条在库明细用于价快照（FIFO 序第一条）。</summary>
        private static StockItem? FindPricingStockItemForAggregateOut(
            StockInfo aggregate,
            string sellOrderItemId,
            string materialId,
            string warehouseId,
            List<StockItem> allStockItems)
        {
            var aggId = aggregate.Id.Trim();
            var line = sellOrderItemId.Trim();
            var mat = materialId.Trim();
            var wh = warehouseId.Trim();
            return allStockItems
                .Where(si =>
                    string.Equals(si.StockAggregateId?.Trim(), aggId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(si.WarehouseId?.Trim(), wh, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(si.SellOrderItemId?.Trim(), line, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(si.MaterialId?.Trim(), mat, StringComparison.OrdinalIgnoreCase))
                .OrderBy(si => si.ProductionDate ?? si.CreateTime)
                .ThenBy(si => si.CreateTime)
                .FirstOrDefault();
        }

        /// <summary>出库单头 <see cref="StockOut.StockOutType"/> 取自关联装箱单 <see cref="Packing.StockOutType"/>。</summary>
        private async Task<short> ResolveStockOutTypeFromPackingAsync(string? packingId)
        {
            var pid = packingId?.Trim();
            if (string.IsNullOrEmpty(pid))
                return StockOutTypeCode.Sales;

            var packing = await _packingRepository.GetByIdAsync(pid);
            if (packing == null || packing.IsDeleted)
                return StockOutTypeCode.Sales;

            var t = packing.StockOutType;
            return t is StockOutTypeCode.Sales
                or StockOutTypeCode.Customs
                or StockOutTypeCode.Return
                or StockOutTypeCode.Scrap
                ? t
                : StockOutTypeCode.Sales;
        }
    }
}
