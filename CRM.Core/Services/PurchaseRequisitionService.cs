using CRM.Core.Interfaces;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Models.Quote;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    /// <summary>采购申请服务实现（MVP：弹窗行选项与创建）</summary>
    public class PurchaseRequisitionService : IPurchaseRequisitionService
    {
        private readonly IRepository<PurchaseRequisition> _prRepo;
        private readonly IRepository<SellOrder> _soRepo;
        private readonly IRepository<SellOrderItem> _soItemRepo;
        private readonly IRepository<PurchaseOrderItem> _poItemRepo;
        private readonly IRepository<QuoteItem> _quoteItemRepo;
        private readonly IUnitOfWork? _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;

        public PurchaseRequisitionService(
            IRepository<PurchaseRequisition> prRepo,
            IRepository<SellOrder> soRepo,
            IRepository<SellOrderItem> soItemRepo,
            IRepository<PurchaseOrderItem> poItemRepo,
            IRepository<QuoteItem> quoteItemRepo,
            ISerialNumberService serialNumberService,
            IUnitOfWork? unitOfWork = null)
        {
            _prRepo = prRepo;
            _soRepo = soRepo;
            _soItemRepo = soItemRepo;
            _poItemRepo = poItemRepo;
            _quoteItemRepo = quoteItemRepo;
            _serialNumberService = serialNumberService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 「可申请采购」明细行（与创建校验一致）：
        /// purchasedQty = 同一销售明细下采购订单明细数量之和；
        /// openPR = 采购申请状态 0/1（新建、部分完成）的数量之和；3=已取消不计；2=全部完成视为已闭环，不再占用本条额度（由已下采购量体现）；
        /// remainingQty = 销售明细数量 − purchasedQty − openPR；≤0 不返回。同一明细可多次申请，但总占用受 remaining 约束。
        /// </summary>
        public async Task<IEnumerable<SellOrderLineOptionDto>> GetSellOrderLineOptionsAsync(string sellOrderId)
        {
            if (string.IsNullOrWhiteSpace(sellOrderId))
                return Enumerable.Empty<SellOrderLineOptionDto>();

            var soItems = (await _soItemRepo.FindAsync(i => i.SellOrderId == sellOrderId)).ToList();
            if (!soItems.Any()) return Enumerable.Empty<SellOrderLineOptionDto>();

            var soItemIds = soItems.Select(i => i.Id).ToList();

            var poItems = (await _poItemRepo.FindAsync(i => soItemIds.Contains(i.SellOrderItemId))).ToList();
            var purchasedQtyBySellItemId = poItems
                .GroupBy(i => i.SellOrderItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var prRows = (await _prRepo.FindAsync(r => soItemIds.Contains(r.SellOrderItemId))).ToList();
            var openPrQtyBySellItemId = prRows
                .Where(r => r.Status == 0 || r.Status == 1)
                .GroupBy(r => r.SellOrderItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var result = new List<SellOrderLineOptionDto>();
            foreach (var item in soItems)
            {
                var purchasedQty = purchasedQtyBySellItemId.TryGetValue(item.Id, out var pv) ? pv : 0m;
                var openPr = openPrQtyBySellItemId.TryGetValue(item.Id, out var ov) ? ov : 0m;
                var remainingQty = item.Qty - purchasedQty - openPr;
                if (remainingQty <= 0m) continue;

                result.Add(new SellOrderLineOptionDto
                {
                    sellOrderItemId = item.Id,
                    pn = item.PN,
                    brand = item.Brand,
                    salesOrderQty = item.Qty,
                    purchasedQty = purchasedQty,
                    openPurchaseRequisitionQty = openPr,
                    remainingQty = remainingQty
                });
            }

            return result;
        }

        public async Task<PurchaseRequisition> CreateAsync(CreatePurchaseRequisitionRequest request, string? actingUserId = null)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.SellOrderItemId)) throw new ArgumentException("SellOrderItemId不能为空");
            if (request.Qty <= 0) throw new ArgumentException("Qty必须大于0");

            var soItem = await _soItemRepo.GetByIdAsync(request.SellOrderItemId);
            if (soItem == null) throw new InvalidOperationException($"销售订单明细不存在: {request.SellOrderItemId}");

            var remainingOptions = await GetSellOrderLineOptionsAsync(soItem.SellOrderId);
            var line = remainingOptions.FirstOrDefault(x =>
                string.Equals(x.sellOrderItemId, soItem.Id, StringComparison.OrdinalIgnoreCase));
            if (line == null)
                throw new InvalidOperationException("该销售明细当前不可创建采购申请（剩余可采数量为 0）");

            if (request.Qty > line.remainingQty)
                throw new InvalidOperationException("申请采购数量不能大于剩余可采数量");

            var so = await _soRepo.GetByIdAsync(soItem.SellOrderId);

            // 从报价主表 QuoteId 关联的报价明细中取一行（不按销单行 PN/品牌/单价等再匹配，见 QuoteItemForPrResolver）
            QuoteItem? matchedQuoteItem = null;
            if (!string.IsNullOrWhiteSpace(soItem.QuoteId))
            {
                var quoteItems = (await _quoteItemRepo.FindAsync(qi => qi.QuoteId == soItem.QuoteId)).ToList();
                matchedQuoteItem = QuoteItemForPrResolver.PickSingleLine(quoteItems);
            }

            var billCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.PurchaseRequisition);

            var pr = new PurchaseRequisition
            {
                Id = Guid.NewGuid().ToString(),
                BillCode = billCode,
                SellOrderItemId = soItem.Id,
                SellOrderId = soItem.SellOrderId,
                Qty = request.Qty,
                ExpectedPurchaseTime = PostgreSqlDateTime.ToUtc(request.ExpectedPurchaseTime),
                Status = 0,
                Type = request.Type,
                PurchaseUserId = request.PurchaseUserId,
                SalesUserId = so?.SalesUserId,
                QuoteVendorId = matchedQuoteItem?.VendorId,
                QuoteCost = matchedQuoteItem != null ? matchedQuoteItem.UnitPrice : 0m,
                PN = soItem.PN,
                Brand = soItem.Brand,
                Remark = request.Remark,
                CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId)
            };

            await _prRepo.AddAsync(pr);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            return pr;
        }

        public async Task<IEnumerable<PurchaseRequisition>> AutoGenerateAsync(string sellOrderId, string purchaseUserId, string? actingUserId = null)
        {
            var options = (await GetSellOrderLineOptionsAsync(sellOrderId)).ToList();
            if (!options.Any()) return Enumerable.Empty<PurchaseRequisition>();

            var created = new List<PurchaseRequisition>();
            foreach (var opt in options)
            {
                var pr = await CreateAsync(new CreatePurchaseRequisitionRequest
                {
                    SellOrderItemId = opt.sellOrderItemId,
                    Qty = opt.remainingQty,
                    ExpectedPurchaseTime = DateTime.UtcNow,
                    Type = 0,
                    PurchaseUserId = purchaseUserId,
                    Remark = "自动生成的采购申请"
                }, actingUserId);
                created.Add(pr);
            }

            return created;
        }

        public async Task RecalculateAsync(string id)
        {
            // MVP：当前不做重算
            await Task.CompletedTask;
        }
    }
}

#if false
using CRM.Core.Interfaces;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    /// <summary>采购申请服务实现（MVP：支持弹窗行选项与创建）</summary>
    public class PurchaseRequisitionService : IPurchaseRequisitionService
    {
        private readonly IRepository<PurchaseRequisition> _prRepo;
        private readonly IRepository<SellOrder> _soRepo;
        private readonly IRepository<SellOrderItem> _soItemRepo;
        private readonly IRepository<PurchaseOrderItem> _poItemRepo;
        private readonly IUnitOfWork? _unitOfWork;

        public PurchaseRequisitionService(
            IRepository<PurchaseRequisition> prRepo,
            IRepository<SellOrder> soRepo,
            IRepository<SellOrderItem> soItemRepo,
            IRepository<PurchaseOrderItem> poItemRepo,
            IUnitOfWork? unitOfWork = null)
        {
            _prRepo = prRepo;
            _soRepo = soRepo;
            _soItemRepo = soItemRepo;
            _poItemRepo = poItemRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<SellOrderLineOptionDto>> GetSellOrderLineOptionsAsync(string sellOrderId)
        {
            if (string.IsNullOrWhiteSpace(sellOrderId))
                return Enumerable.Empty<SellOrderLineOptionDto>();

            var soItems = (await _soItemRepo.FindAsync(i => i.SellOrderId == sellOrderId)).ToList();
            if (!soItems.Any()) return Enumerable.Empty<SellOrderLineOptionDto>();

            var soItemIds = soItems.Select(i => i.Id).ToList();

            // 已采购数量（采购订单明细）
            var poItems = (await _poItemRepo.FindAsync(i => soItemIds.Contains(i.SellOrderItemId))).ToList();
            var purchasedQtyBySellItemId = poItems
                .GroupBy(i => i.SellOrderItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            // 已存在采购申请（非取消：status != 3） -> 该销售明细不再展示
            var existingReqs = (await _prRepo.FindAsync(r => soItemIds.Contains(r.SellOrderItemId) && r.Status != 3)).ToList();
            var hasActiveReqBySellItemId = existingReqs
                .GroupBy(r => r.SellOrderItemId)
                .ToDictionary(g => g.Key, g => g.Any());

            var result = new List<SellOrderLineOptionDto>();
            foreach (var item in soItems)
            {
                if (hasActiveReqBySellItemId.TryGetValue(item.Id, out var hasActive) && hasActive)
                    continue;

                var purchasedQty = purchasedQtyBySellItemId.TryGetValue(item.Id, out var v) ? v : 0m;
                var remainingQty = item.Qty - purchasedQty;
                if (remainingQty <= 0m) continue;

                result.Add(new SellOrderLineOptionDto
                {
                    sellOrderItemId = item.Id,
                    pn = item.PN,
                    brand = item.Brand,
                    salesOrderQty = item.Qty,
                    remainingQty = remainingQty
                });
            }

            return result;
        }

        public async Task<PurchaseRequisition> CreateAsync(CreatePurchaseRequisitionRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.SellOrderItemId)) throw new ArgumentException("SellOrderItemId不能为空");
            if (request.Qty <= 0) throw new ArgumentException("Qty必须大于0");

            var soItem = await _soItemRepo.GetByIdAsync(request.SellOrderItemId);
            if (soItem == null) throw new InvalidOperationException($"销售订单明细不存在: {request.SellOrderItemId}");

            // 校验剩余数量与可创建性
            var remainingOptions = await GetSellOrderLineOptionsAsync(soItem.SellOrderId);
            var line = remainingOptions.FirstOrDefault(x => x.sellOrderItemId == soItem.Id);
            if (line == null)
                throw new InvalidOperationException("该销售明细当前不可创建采购申请（可能已创建或剩余数量为0）");

            if (request.Qty > line.remainingQty)
                throw new InvalidOperationException("申请采购数量不能大于剩余数量");

            // 防重：非取消状态下同一明细只能存在一条采购申请
            var existingReq = (await _prRepo.FindAsync(r =>
                    r.SellOrderItemId == request.SellOrderItemId && r.Status != 3))
                .FirstOrDefault();
            if (existingReq != null)
                throw new InvalidOperationException("该销售明细已创建采购申请");

            var so = await _soRepo.GetByIdAsync(soItem.SellOrderId);

            var pr = new PurchaseRequisition
            {
                Id = Guid.NewGuid().ToString(),
                BillCode = GenerateBillCode(),
                SellOrderItemId = soItem.Id,
                SellOrderId = soItem.SellOrderId,
                Qty = request.Qty,
                ExpectedPurchaseTime = PostgreSqlDateTime.ToUtc(request.ExpectedPurchaseTime),
                Status = 0,
                Type = request.Type,
                PurchaseUserId = request.PurchaseUserId,
                SalesUserId = so?.SalesUserId,
                QuoteVendorId = null,
                QuoteCost = 0m,
                PN = soItem.PN,
                Brand = soItem.Brand,
                Remark = request.Remark
            };

            await _prRepo.AddAsync(pr);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            return pr;
        }

        public async Task<IEnumerable<PurchaseRequisition>> AutoGenerateAsync(string sellOrderId, string purchaseUserId)
        {
            // 简化实现：为所有可申请明细创建一条“剩余数量”采购申请
            var options = (await GetSellOrderLineOptionsAsync(sellOrderId)).ToList();
            if (!options.Any()) return Enumerable.Empty<PurchaseRequisition>();

            var created = new List<PurchaseRequisition>();
            foreach (var opt in options)
            {
                var pr = await CreateAsync(new CreatePurchaseRequisitionRequest
                {
                    SellOrderItemId = opt.sellOrderItemId,
                    Qty = opt.remainingQty,
                    ExpectedPurchaseTime = DateTime.UtcNow,
                    Type = 0,
                    PurchaseUserId = purchaseUserId,
                    Remark = "自动生成的采购申请"
                });
                created.Add(pr);
            }

            return created;
        }

        public async Task RecalculateAsync(string id)
        {
            // MVP：当前不做重算
            await Task.CompletedTask;
        }

        private static string GenerateBillCode()
        {
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var rand = new Random().Next(1000, 9999);
            return $"PR{date}{rand}";
        }
    }
}

using CRM.Core.Interfaces;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    /// <summary>采购申请服务实现（MVP：支持弹窗行选项与创建）</summary>
    public class PurchaseRequisitionService : IPurchaseRequisitionService
    {
        private readonly IRepository<PurchaseRequisition> _prRepo;
        private readonly IRepository<SellOrder> _soRepo;
        private readonly IRepository<SellOrderItem> _soItemRepo;
        private readonly IRepository<PurchaseOrderItem> _poItemRepo;
        private readonly IUnitOfWork? _unitOfWork;

        public PurchaseRequisitionService(
            IRepository<PurchaseRequisition> prRepo,
            IRepository<SellOrder> soRepo,
            IRepository<SellOrderItem> soItemRepo,
            IRepository<PurchaseOrderItem> poItemRepo,
            IUnitOfWork? unitOfWork = null)
        {
            _prRepo = prRepo;
            _soRepo = soRepo;
            _soItemRepo = soItemRepo;
            _poItemRepo = poItemRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<SellOrderLineOptionDto>> GetSellOrderLineOptionsAsync(string sellOrderId)
        {
            if (string.IsNullOrWhiteSpace(sellOrderId))
                return Enumerable.Empty<SellOrderLineOptionDto>();

            var soItems = (await _soItemRepo.FindAsync(i => i.SellOrderId == sellOrderId)).ToList();
            if (!soItems.Any()) return Enumerable.Empty<SellOrderLineOptionDto>();

            var soItemIds = soItems.Select(i => i.Id).ToList();

            // 已采购数量（采购订单明细）
            var poItems = (await _poItemRepo.FindAsync(i => soItemIds.Contains(i.SellOrderItemId))).ToList();
            var purchasedQtyBySellItemId = poItems
                .GroupBy(i => i.SellOrderItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            // 已存在采购申请（非取消：status != 3） -> 该销售明细不再展示
            var existingReqs = (await _prRepo.FindAsync(r => soItemIds.Contains(r.SellOrderItemId) && r.Status != 3)).ToList();
            var hasActiveReqBySellItemId = existingReqs
                .GroupBy(r => r.SellOrderItemId)
                .ToDictionary(g => g.Key, g => g.Any());

            var result = new List<SellOrderLineOptionDto>();
            foreach (var item in soItems)
            {
                if (hasActiveReqBySellItemId.TryGetValue(item.Id, out var hasActive) && hasActive)
                    continue;

                var purchasedQty = purchasedQtyBySellItemId.TryGetValue(item.Id, out var v) ? v : 0m;
                var remainingQty = item.Qty - purchasedQty;
                if (remainingQty <= 0m) continue;

                result.Add(new SellOrderLineOptionDto
                {
                    sellOrderItemId = item.Id,
                    pn = item.PN,
                    brand = item.Brand,
                    salesOrderQty = item.Qty,
                    remainingQty = remainingQty
                });
            }

            return result;
        }

        public async Task<PurchaseRequisition> CreateAsync(CreatePurchaseRequisitionRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.SellOrderItemId)) throw new ArgumentException("SellOrderItemId不能为空");
            if (request.Qty <= 0) throw new ArgumentException("Qty必须大于0");

            var soItem = await _soItemRepo.GetByIdAsync(request.SellOrderItemId);
            if (soItem == null) throw new InvalidOperationException($"销售订单明细不存在: {request.SellOrderItemId}");

            var remainingOptions = await GetSellOrderLineOptionsAsync(soItem.SellOrderId);
            var line = remainingOptions.FirstOrDefault(x => x.sellOrderItemId == soItem.Id);
            if (line == null)
                throw new InvalidOperationException("该销售明细当前不可创建采购申请（可能已创建或剩余数量为0）");

            if (request.Qty > line.remainingQty)
                throw new InvalidOperationException("申请采购数量不能大于剩余数量");

            // 防重
            var existingReq = (await _prRepo.FindAsync(r =>
                    r.SellOrderItemId == request.SellOrderItemId && r.Status != 3))
                .FirstOrDefault();
            if (existingReq != null)
                throw new InvalidOperationException("该销售明细已创建采购申请");

            var so = await _soRepo.GetByIdAsync(soItem.SellOrderId);

            var pr = new PurchaseRequisition
            {
                Id = Guid.NewGuid().ToString(),
                BillCode = GenerateBillCode(),
                SellOrderItemId = soItem.Id,
                SellOrderId = soItem.SellOrderId,
                Qty = request.Qty,
                ExpectedPurchaseTime = PostgreSqlDateTime.ToUtc(request.ExpectedPurchaseTime),
                Status = 0,
                Type = request.Type,
                PurchaseUserId = request.PurchaseUserId,
                SalesUserId = so?.SalesUserId,
                QuoteVendorId = null,
                QuoteCost = 0m,
                PN = soItem.PN,
                Brand = soItem.Brand,
                Remark = request.Remark
            };

            await _prRepo.AddAsync(pr);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            return pr;
        }

        public async Task<IEnumerable<PurchaseRequisition>> AutoGenerateAsync(string sellOrderId, string purchaseUserId)
        {
            // 简化实现：为所有可申请明细创建一条“剩余数量”采购申请
            var options = (await GetSellOrderLineOptionsAsync(sellOrderId)).ToList();
            if (!options.Any()) return Enumerable.Empty<PurchaseRequisition>();

            var created = new List<PurchaseRequisition>();
            foreach (var opt in options)
            {
                var pr = await CreateAsync(new CreatePurchaseRequisitionRequest
                {
                    SellOrderItemId = opt.sellOrderItemId,
                    Qty = opt.remainingQty,
                    ExpectedPurchaseTime = DateTime.UtcNow,
                    Type = 0,
                    PurchaseUserId = purchaseUserId,
                    Remark = "自动生成的采购申请"
                });
                created.Add(pr);
            }

            return created;
        }

        public async Task RecalculateAsync(string id)
        {
            // MVP：当前不做重算
            await Task.CompletedTask;
        }

        private static string GenerateBillCode()
        {
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var rand = new Random().Next(1000, 9999);
            return $"PR{date}{rand}";
        }
    }
}

using CRM.Core.Interfaces;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    /// <summary>采购申请服务实现</summary>
    public class PurchaseRequisitionService : IPurchaseRequisitionService
    {
        private readonly IRepository<PurchaseRequisition> _prRepo;
        private readonly IRepository<SellOrder> _soRepo;
        private readonly IRepository<SellOrderItem> _soItemRepo;
        private readonly IRepository<CRM.Core.Models.Purchase.PurchaseOrderItem> _poItemRepo;
        private readonly IUnitOfWork? _unitOfWork;

        public PurchaseRequisitionService(
            IRepository<PurchaseRequisition> prRepo,
            IRepository<SellOrder> soRepo,
            IRepository<SellOrderItem> soItemRepo,
            IRepository<CRM.Core.Models.Purchase.PurchaseOrderItem> poItemRepo,
            IUnitOfWork? unitOfWork = null)
        {
            _prRepo = prRepo;
            _soRepo = soRepo;
            _soItemRepo = soItemRepo;
            _poItemRepo = poItemRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<SellOrderLineOptionDto>> GetSellOrderLineOptionsAsync(string sellOrderId)
        {
            if (string.IsNullOrWhiteSpace(sellOrderId))
                return Enumerable.Empty<SellOrderLineOptionDto>();

            // 取出销售订单明细
            var soItems = (await _soItemRepo.FindAsync(i => i.SellOrderId == sellOrderId)).ToList();
            if (!soItems.Any()) return Enumerable.Empty<SellOrderLineOptionDto>();

            var soItemIds = soItems.Select(i => i.Id).ToList();

            // 已采购数量（来自采购订单明细）
            var poItems = (await _poItemRepo.FindAsync(i => soItemIds.Contains(i.SellOrderItemId))).ToList();
            var purchasedQtyBySellItemId = poItems
                .GroupBy(i => i.SellOrderItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            // 已存在采购申请（非取消状态：status != 3） -> 该明细不再展示
            var existingReqs = (await _prRepo.FindAsync(r => soItemIds.Contains(r.SellOrderItemId) && r.Status != 3)).ToList();
            var hasActiveReqBySellItemId = existingReqs
                .GroupBy(r => r.SellOrderItemId)
                .ToDictionary(g => g.Key, g => g.Any());

            var result = new List<SellOrderLineOptionDto>();
            foreach (var item in soItems)
            {
                if (hasActiveReqBySellItemId.TryGetValue(item.Id, out var hasActive) && hasActive)
                    continue;

                var purchasedQty = purchasedQtyBySellItemId.TryGetValue(item.Id, out var v) ? v : 0m;
                var remainingQty = item.Qty - purchasedQty;
                if (remainingQty <= 0m) continue;

                result.Add(new SellOrderLineOptionDto
                {
                    sellOrderItemId = item.Id,
                    pn = item.PN,
                    brand = item.Brand,
                    salesOrderQty = item.Qty,
                    remainingQty = remainingQty
                });
            }

            return result;
        }

        public async Task<PurchaseRequisition> CreateAsync(CreatePurchaseRequisitionRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.SellOrderItemId)) throw new ArgumentException("SellOrderItemId不能为空");
            if (request.Qty <= 0) throw new ArgumentException("Qty必须大于0");

            var soItem = await _soItemRepo.GetByIdAsync(request.SellOrderItemId);
            if (soItem == null) throw new InvalidOperationException($"销售订单明细不存在: {request.SellOrderItemId}");

            var remainingOptions = await GetSellOrderLineOptionsAsync(soItem.SellOrderId);
            var line = remainingOptions.FirstOrDefault(x => x.sellOrderItemId == soItem.Id);
            if (line == null)
            {
                // 可能是已经创建过采购申请，或剩余数量为0
                throw new InvalidOperationException("该销售明细当前不可创建采购申请（可能已创建或剩余数量为0）");
            }
            if (request.Qty > line.remainingQty)
                throw new InvalidOperationException("申请采购数量不能大于剩余数量");

            var existingReq = (await _prRepo.FindAsync(r => r.SellOrderItemId == request.SellOrderItemId && r.Status != 3)).FirstOrDefault();
            if (existingReq != null)
                throw new InvalidOperationException("该销售明细已创建采购申请");

            var so = await _soRepo.GetByIdAsync(soItem.SellOrderId);

            var pr = new PurchaseRequisition
            {
                Id = Guid.NewGuid().ToString(),
                BillCode = GenerateBillCode(),
                SellOrderItemId = soItem.Id,
                SellOrderId = soItem.SellOrderId,
                Qty = request.Qty,
                ExpectedPurchaseTime = PostgreSqlDateTime.ToUtc(request.ExpectedPurchaseTime),
                Status = 0,
                Type = request.Type,
                PurchaseUserId = request.PurchaseUserId,
                SalesUserId = so?.SalesUserId,
                QuoteVendorId = null,
                QuoteCost = 0m,
                PN = soItem.PN,
                Brand = soItem.Brand,
                Remark = request.Remark
            };

            await _prRepo.AddAsync(pr);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            return pr;
        }

        public async Task<IEnumerable<PurchaseRequisition>> AutoGenerateAsync(string sellOrderId, string purchaseUserId)
        {
            // 简化实现：为所有可申请明细行创建一条“剩余数量”采购申请
            var options = (await GetSellOrderLineOptionsAsync(sellOrderId)).ToList();
            if (!options.Any()) return Enumerable.Empty<PurchaseRequisition>();

            var created = new List<PurchaseRequisition>();
            foreach (var opt in options)
            {
                var pr = await CreateAsync(new CreatePurchaseRequisitionRequest
                {
                    SellOrderItemId = opt.sellOrderItemId,
                    Qty = opt.remainingQty,
                    ExpectedPurchaseTime = DateTime.UtcNow,
                    Type = 0,
                    PurchaseUserId = purchaseUserId,
                    Remark = "自动生成的采购申请"
                });
                created.Add(pr);
            }

            return created;
        }

        public async Task RecalculateAsync(string id)
        {
            // 占位：当前简化实现不进行“状态/数量关系”的重算
            await Task.CompletedTask;
        }

        private static string GenerateBillCode()
        {
            // 简化编码：PR + yyyyMMdd + 4位随机数
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var rand = new Random().Next(1000, 9999);
            return $"PR{date}{rand}";
        }
    }
}

using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Models.Vendor;

namespace CRM.Core.Services
{
    public class PurchaseRequisitionService : IPurchaseRequisitionService
    {
        private readonly IRepository<PurchaseRequisition> _prRepo;
        private readonly IRepository<SellOrder> _soRepo;
        private readonly IRepository<SellOrderItem> _soItemRepo;
        private readonly IRepository<PurchaseOrderItem> _poItemRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<VendorInfo> _vendorRepo;
        private readonly ISerialNumberService _serialNumberService;
        private readonly IUnitOfWork? _unitOfWork;

        public PurchaseRequisitionService(
            IRepository<PurchaseRequisition> prRepo,
            IRepository<SellOrder> soRepo,
            IRepository<SellOrderItem> soItemRepo,
            IRepository<PurchaseOrderItem> poItemRepo,
            IRepository<User> userRepo,
            IRepository<VendorInfo> vendorRepo,
            ISerialNumberService serialNumberService,
            IUnitOfWork? unitOfWork = null)
        {
            _prRepo = prRepo;
            _soRepo = soRepo;
            _soItemRepo = soItemRepo;
            _poItemRepo = poItemRepo;
            _userRepo = userRepo;
            _vendorRepo = vendorRepo;
            _serialNumberService = serialNumberService;
            _unitOfWork = unitOfWork;
        }

        public async Task<PurchaseRequisitionListItemDto> CreateAsync(
            CreatePurchaseRequisitionRequest request,
            string? auditUserId = null,
            string? auditUserName = null)
        {
            if (string.IsNullOrWhiteSpace(request.SellOrderItemId))
                throw new ArgumentException("销售订单明细不能为空", nameof(request.SellOrderItemId));
            if (!request.ExpectedPurchaseTime.HasValue)
                throw new ArgumentException("预计采购日期不能为空", nameof(request.ExpectedPurchaseTime));

            var line = await _soItemRepo.GetByIdAsync(request.SellOrderItemId.Trim())
                ?? throw new InvalidOperationException("销售订单明细不存在");

            var so = await _soRepo.GetByIdAsync(line.SellOrderId)
                ?? throw new InvalidOperationException("销售订单不存在");

            if (so.Status < SellOrderMainStatus.Approved || so.Status == SellOrderMainStatus.AuditFailed || so.Status == SellOrderMainStatus.Cancelled)
                throw new InvalidOperationException("仅已审批且未取消的销售订单可创建采购申请");

            var allPr = await _prRepo.GetAllAsync();
            if (allPr.Any(p => p.SellOrderItemId == line.Id))
                throw new InvalidOperationException("该销售明细已存在采购申请");

            var qty = request.Qty ?? line.Qty;
            if (qty <= 0)
                throw new ArgumentException("申请数量必须大于 0", nameof(request.Qty));
            if (qty > line.Qty)
                throw new ArgumentException("申请数量不能大于销售订单明细数量", nameof(request.Qty));

            var billCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.PurchaseRequisition);
            var expected = NormalizeUtc(request.ExpectedPurchaseTime.Value);

            var entity = new PurchaseRequisition
            {
                Id = Guid.NewGuid().ToString(),
                BillCode = billCode,
                SellOrderItemId = line.Id,
                SellOrderId = line.SellOrderId,
                Qty = qty,
                OriginalQty = qty,
                ExpectedPurchaseTime = expected,
                Status = 0,
                Type = request.Type,
                PurchaseUserId = request.PurchaseUserId,
                SalesUserId = so.SalesUserId,
                ProductId = line.ProductId,
                PN = line.PN,
                Brand = line.Brand,
                Remark = request.Remark,
                BusComplianceStatus = string.IsNullOrWhiteSpace(request.BusComplianceStatus)
                    ? "无需处理"
                    : request.BusComplianceStatus!.Trim(),
                CountryOfOrigin = request.CountryOfOrigin?.Trim(),
                QuoteBaseOrigin = request.QuoteBaseOrigin?.Trim(),
                QuotePackageOrigin = request.QuotePackageOrigin?.Trim(),
                QuoteTotal = request.QuoteTotal ?? 0m,
                QuoteVendorId = string.IsNullOrWhiteSpace(request.QuoteVendorId) ? null : request.QuoteVendorId.Trim(),
                CreateUserIdStr = auditUserId,
                CreateUserName = auditUserName
            };

            ApplyCalc(entity, await LoadPoItemsForLineAsync(line.Id));
            await _prRepo.AddAsync(entity);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();

            var dicts = await BuildLookupDictsAsync();
            return MapToDto(entity, so, line, dicts.users, dicts.vendors);
        }

        public async Task<PurchaseRequisition?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return await _prRepo.GetByIdAsync(id);
        }

        public async Task<PurchaseRequisitionListItemDto?> GetDetailAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            var pr = await _prRepo.GetByIdAsync(id);
            if (pr == null) return null;
            ApplyCalc(pr, await LoadPoItemsForLineAsync(pr.SellOrderItemId));
            await _prRepo.UpdateAsync(pr);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            var so = await _soRepo.GetByIdAsync(pr.SellOrderId);
            var line = await _soItemRepo.GetByIdAsync(pr.SellOrderItemId);
            var dicts = await BuildLookupDictsAsync();
            return MapToDto(pr, so, line, dicts.users, dicts.vendors);
        }

        public async Task<PagedResult<PurchaseRequisitionListItemDto>> GetPagedAsync(PurchaseRequisitionQueryRequest request)
        {
            var all = (await _prRepo.GetAllAsync()).ToList();
            var soAll = await _soRepo.GetAllAsync();
            var soCodeById = soAll.ToDictionary(s => s.Id, s => s.SellOrderCode);
            var soById = soAll.ToDictionary(s => s.Id);

            IEnumerable<PurchaseRequisition> query = all;
            if (!string.IsNullOrWhiteSpace(request.SellOrderId))
                query = query.Where(p => p.SellOrderId == request.SellOrderId);

            if (request.Status.HasValue)
                query = query.Where(p => p.Status == request.Status.Value);

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var kw = request.Keyword.Trim();
                query = query.Where(p =>
                    p.BillCode.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    (soCodeById.TryGetValue(p.SellOrderId, out var code) &&
                     !string.IsNullOrEmpty(code) &&
                     code.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(p.PN) && p.PN.Contains(kw, StringComparison.OrdinalIgnoreCase)));
            }

            var list = query.OrderByDescending(p => p.CreateTime).ToList();
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 20 : request.PageSize;
            var total = list.Count;
            var slice = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var dicts = await BuildLookupDictsAsync();
            var items = slice.Select(p =>
            {
                soById.TryGetValue(p.SellOrderId, out var so);
                return MapToDto(p, so, null, dicts.users, dicts.vendors);
            }).ToList();

            return new PagedResult<PurchaseRequisitionListItemDto>
            {
                Items = items,
                TotalCount = total,
                PageIndex = page,
                PageSize = pageSize
            };
        }

        public async Task<PurchaseRequisition> UpdateAsync(string id, UpdatePurchaseRequisitionRequest request)
        {
            var pr = await _prRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException("采购申请不存在");

            var line = await _soItemRepo.GetByIdAsync(pr.SellOrderItemId)
                ?? throw new InvalidOperationException("销售订单明细不存在");

            if (request.Qty.HasValue)
            {
                if (request.Qty.Value <= 0)
                    throw new ArgumentException("申请数量必须大于 0", nameof(request.Qty));
                if (request.Qty.Value > line.Qty)
                    throw new ArgumentException("申请数量不能大于销售订单明细数量", nameof(request.Qty));
                pr.Qty = request.Qty.Value;
            }

            if (request.ExpectedPurchaseTime.HasValue)
                pr.ExpectedPurchaseTime = NormalizeUtc(request.ExpectedPurchaseTime.Value);
            if (request.Type.HasValue) pr.Type = request.Type.Value;
            if (request.PurchaseUserId != null) pr.PurchaseUserId = request.PurchaseUserId;
            if (request.SalesUserId != null) pr.SalesUserId = request.SalesUserId;
            if (request.QuoteVendorId != null) pr.QuoteVendorId = string.IsNullOrWhiteSpace(request.QuoteVendorId) ? null : request.QuoteVendorId;
            if (request.QuoteCost.HasValue) pr.QuoteCost = request.QuoteCost.Value;
            if (request.QuoteVendorCurrency.HasValue) pr.QuoteVendorCurrency = request.QuoteVendorCurrency.Value;
            if (request.SalesCancelQty.HasValue)
            {
                if (request.SalesCancelQty.Value < 0 || request.SalesCancelQty.Value > pr.Qty)
                    throw new ArgumentException("销售取消数量必须在 0 与申请数量之间");
                pr.SalesCancelQty = request.SalesCancelQty.Value;
            }

            if (request.StockClearedQty.HasValue)
            {
                if (pr.Type == 1)
                    throw new InvalidOperationException("公开备货类型不可维护清库存数量");
                if (request.StockClearedQty.Value < 0 || request.StockClearedQty.Value > pr.Qty)
                    throw new ArgumentException("清库存数量必须在 0 与申请数量之间");
                pr.StockClearedQty = request.StockClearedQty.Value;
            }

            if (request.Remark != null) pr.Remark = request.Remark;
            if (request.BusComplianceStatus != null)
                pr.BusComplianceStatus = string.IsNullOrWhiteSpace(request.BusComplianceStatus) ? "无需处理" : request.BusComplianceStatus.Trim();
            if (request.CountryOfOrigin != null) pr.CountryOfOrigin = request.CountryOfOrigin.Trim();
            if (request.QuoteBaseOrigin != null) pr.QuoteBaseOrigin = request.QuoteBaseOrigin.Trim();
            if (request.QuotePackageOrigin != null) pr.QuotePackageOrigin = request.QuotePackageOrigin.Trim();
            if (request.QuoteTotal.HasValue) pr.QuoteTotal = request.QuoteTotal.Value;

            ApplyCalc(pr, await LoadPoItemsForLineAsync(pr.SellOrderItemId));
            await _prRepo.UpdateAsync(pr);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            return pr;
        }

        public async Task DeleteAsync(string id)
        {
            var pr = await _prRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException("采购申请不存在");

            var poItems = await LoadPoItemsForLineAsync(pr.SellOrderItemId);
            var activePoQty = poItems.Where(i => i.Status == 0).Sum(i => i.Qty);
            if (activePoQty > 0 || pr.StockClearedQty > 0)
                throw new InvalidOperationException("存在有效采购订单数量或已清库存，不能删除");

            await _prRepo.DeleteAsync(id);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<PurchaseRequisitionListItemDto>> AutoGenerateForSellOrderAsync(string sellOrderId)
        {
            var so = await _soRepo.GetByIdAsync(sellOrderId)
                ?? throw new InvalidOperationException($"销售订单 {sellOrderId} 不存在");

            if (so.Status < SellOrderMainStatus.Approved || so.Status == SellOrderMainStatus.AuditFailed || so.Status == SellOrderMainStatus.Cancelled)
                throw new InvalidOperationException("仅已审批且未取消的销售订单可自动生成采购申请");

            var soItems = await _soItemRepo.GetAllAsync();
            var lines = soItems.Where(i => i.SellOrderId == sellOrderId && i.Status == 0).ToList();
            var allPr = await _prRepo.GetAllAsync();
            var existingLineIds = allPr.Select(p => p.SellOrderItemId).ToHashSet();

            var result = new List<PurchaseRequisitionListItemDto>();
            foreach (var line in lines)
            {
                if (existingLineIds.Contains(line.Id)) continue;
                if (line.Qty <= 0) continue;

                var dto = await CreateAsync(new CreatePurchaseRequisitionRequest
                {
                    SellOrderItemId = line.Id,
                    Qty = line.Qty,
                    ExpectedPurchaseTime = DateTime.UtcNow,
                    Type = 0,
                    Remark = "自动生成"
                });
                result.Add(dto);
                existingLineIds.Add(line.Id);
            }

            return result;
        }

        public async Task RecalculateAndSaveAsync(string id)
        {
            var pr = await _prRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException("采购申请不存在");
            ApplyCalc(pr, await LoadPoItemsForLineAsync(pr.SellOrderItemId));
            await _prRepo.UpdateAsync(pr);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<PurchaseRequisitionLineOptionDto>> GetLineOptionsForSellOrderAsync(string sellOrderId)
        {
            var so = await _soRepo.GetByIdAsync(sellOrderId)
                ?? throw new InvalidOperationException("销售订单不存在");

            if (so.Status < SellOrderMainStatus.Approved || so.Status == SellOrderMainStatus.AuditFailed || so.Status == SellOrderMainStatus.Cancelled)
                throw new InvalidOperationException("仅已审批且未取消的销售订单可创建采购申请");

            var allPr = await _prRepo.GetAllAsync();
            var usedLineIds = allPr.Select(p => p.SellOrderItemId).ToHashSet();

            var soItems = await _soItemRepo.GetAllAsync();
            var lines = soItems
                .Where(i => i.SellOrderId == sellOrderId && i.Status == 0 && !usedLineIds.Contains(i.Id))
                .ToList();

            return lines.Select(l => new PurchaseRequisitionLineOptionDto
            {
                SellOrderItemId = l.Id,
                PN = l.PN,
                Brand = l.Brand,
                SalesOrderQty = l.Qty,
                RemainingQty = l.Qty
            }).ToList();
        }

        private async Task<(Dictionary<string, string> users, Dictionary<string, string?> vendors)> BuildLookupDictsAsync()
        {
            var users = (await _userRepo.GetAllAsync())
                .ToDictionary(u => u.Id, u => u.UserName, StringComparer.Ordinal);
            var vendors = (await _vendorRepo.GetAllAsync())
                .ToDictionary(v => v.Id, v => (string?)(v.OfficialName ?? v.NickName ?? v.Code), StringComparer.Ordinal);
            return (users, vendors);
        }

        private async Task<List<PurchaseOrderItem>> LoadPoItemsForLineAsync(string sellOrderItemId)
        {
            var all = await _poItemRepo.GetAllAsync();
            return all.Where(i => i.SellOrderItemId == sellOrderItemId).ToList();
        }

        private static PurchaseRequisitionListItemDto MapToDto(
            PurchaseRequisition p,
            SellOrder? so,
            SellOrderItem? line,
            IReadOnlyDictionary<string, string> userNames,
            IReadOnlyDictionary<string, string?> vendorNames)
        {
            string? salesUserName = null;
            if (so != null && p.SalesUserId == so.SalesUserId)
                salesUserName = so.SalesUserName;
            if (string.IsNullOrEmpty(salesUserName) && !string.IsNullOrEmpty(p.SalesUserId))
                userNames.TryGetValue(p.SalesUserId, out salesUserName);

            string? purchaseUserName = null;
            if (!string.IsNullOrEmpty(p.PurchaseUserId))
                userNames.TryGetValue(p.PurchaseUserId, out purchaseUserName);

            string? vendorName = null;
            if (!string.IsNullOrEmpty(p.QuoteVendorId))
                vendorNames.TryGetValue(p.QuoteVendorId, out vendorName);

            return new PurchaseRequisitionListItemDto
            {
                Id = p.Id,
                BillCode = p.BillCode,
                SellOrderId = p.SellOrderId,
                SellOrderCode = so?.SellOrderCode,
                SellOrderItemId = p.SellOrderItemId,
                PN = p.PN,
                Brand = p.Brand,
                Qty = p.Qty,
                OriginalQty = p.OriginalQty,
                ExpectedPurchaseTime = p.ExpectedPurchaseTime,
                Status = p.Status,
                Type = p.Type,
                PurchaseUserId = p.PurchaseUserId,
                SalesUserId = p.SalesUserId,
                PurchaseUserName = purchaseUserName,
                SalesUserName = salesUserName,
                QuoteVendorId = p.QuoteVendorId,
                IntendedVendorName = vendorName,
                BusComplianceStatus = p.BusComplianceStatus,
                CountryOfOrigin = p.CountryOfOrigin,
                QuoteBaseOrigin = p.QuoteBaseOrigin,
                QuotePackageOrigin = p.QuotePackageOrigin,
                QuoteTotal = p.QuoteTotal,
                QtyPurchaseOrder = p.QtyPurchaseOrder,
                QtyPurchaseOrderNot = p.QtyPurchaseOrderNot,
                SalesCancelQty = p.SalesCancelQty,
                AbnormalCancelQty = p.AbnormalCancelQty,
                EffectivePurchaseQty = p.EffectivePurchaseQty,
                StockClearedQty = p.StockClearedQty,
                Remark = p.Remark,
                CreateTime = p.CreateTime,
                CreateUserName = p.CreateUserName,
                SellOrderLineQty = line?.Qty,
                RemainingQty = line != null ? line.Qty - p.Qty : null
            };
        }

        private static void ApplyCalc(PurchaseRequisition pr, IReadOnlyList<PurchaseOrderItem> poItems)
        {
            var purchaseOrderQty = poItems.Where(i => i.Status == 0).Sum(i => i.Qty);
            var abnormalCancelQty = poItems.Where(i => i.Status == 1).Sum(i => i.Qty);

            pr.AbnormalCancelQty = abnormalCancelQty;
            pr.QtyPurchaseOrder = purchaseOrderQty + pr.StockClearedQty;
            pr.EffectivePurchaseQty = pr.Qty - pr.SalesCancelQty - pr.AbnormalCancelQty;
            pr.QtyPurchaseOrderNot = pr.EffectivePurchaseQty - (pr.QtyPurchaseOrder - pr.AbnormalCancelQty);
            if (pr.QtyPurchaseOrderNot < 0)
                pr.QtyPurchaseOrderNot = 0;

            pr.Status = CalcStatus(pr.EffectivePurchaseQty, pr.QtyPurchaseOrder, pr.QtyPurchaseOrderNot);
        }

        private static short CalcStatus(decimal effectivePurchaseQty, decimal qtyPurchaseOrder, decimal qtyPurchaseOrderNot)
        {
            if (qtyPurchaseOrderNot == 0 && effectivePurchaseQty > 0)
                return 2;
            if (qtyPurchaseOrder > 0 && qtyPurchaseOrderNot > 0)
                return 1;
            if (effectivePurchaseQty == 0)
                return 3;
            return 0;
        }

        private static DateTime NormalizeUtc(DateTime dt)
        {
            return dt.Kind switch
            {
                DateTimeKind.Utc => dt,
                DateTimeKind.Local => dt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
            };
        }
    }
}
#endif
