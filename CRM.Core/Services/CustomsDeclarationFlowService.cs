using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Customs;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;

namespace CRM.Core.Services;

public class CustomsDeclarationFlowService : ICustomsDeclarationFlowService
{
    private readonly IRepository<CustomsDeclaration> _declarationRepo;
    private readonly IRepository<CustomsDeclarationItem> _declarationItemRepo;
    private readonly IRepository<CustomsBroker> _brokerRepo;
    private readonly IRepository<CustomsPendlist> _pendlistRepo;
    private readonly IRepository<StockOutRequest> _stockOutRequestRepo;
    private readonly IRepository<SellOrder> _sellOrderRepo;
    private readonly IRepository<SellOrderItem> _sellOrderItemRepo;
    private readonly IRepository<CustomerInfo> _customerRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<PackingItem> _packingItemRepo;
    private readonly IRepository<Packing> _packingRepo;
    private readonly IRepository<StockOutItem> _stockOutItemRepo;
    private readonly IRepository<StockOut> _stockOutRepo;
    private readonly IRepository<StockInNotify> _arrivalRepo;
    private readonly IRepository<QCInfo> _qcRepo;
    private readonly IRepository<StockIn> _stockInRepo;

    public CustomsDeclarationFlowService(
        IRepository<CustomsDeclaration> declarationRepo,
        IRepository<CustomsDeclarationItem> declarationItemRepo,
        IRepository<CustomsBroker> brokerRepo,
        IRepository<CustomsPendlist> pendlistRepo,
        IRepository<StockOutRequest> stockOutRequestRepo,
        IRepository<SellOrder> sellOrderRepo,
        IRepository<SellOrderItem> sellOrderItemRepo,
        IRepository<CustomerInfo> customerRepo,
        IRepository<User> userRepo,
        IRepository<PackingItem> packingItemRepo,
        IRepository<Packing> packingRepo,
        IRepository<StockOutItem> stockOutItemRepo,
        IRepository<StockOut> stockOutRepo,
        IRepository<StockInNotify> arrivalRepo,
        IRepository<QCInfo> qcRepo,
        IRepository<StockIn> stockInRepo)
    {
        _declarationRepo = declarationRepo;
        _declarationItemRepo = declarationItemRepo;
        _brokerRepo = brokerRepo;
        _pendlistRepo = pendlistRepo;
        _stockOutRequestRepo = stockOutRequestRepo;
        _sellOrderRepo = sellOrderRepo;
        _sellOrderItemRepo = sellOrderItemRepo;
        _customerRepo = customerRepo;
        _userRepo = userRepo;
        _packingItemRepo = packingItemRepo;
        _packingRepo = packingRepo;
        _stockOutItemRepo = stockOutItemRepo;
        _stockOutRepo = stockOutRepo;
        _arrivalRepo = arrivalRepo;
        _qcRepo = qcRepo;
        _stockInRepo = stockInRepo;
    }

    public async Task<CustomsDeclarationFlowAggregatesDto> GetFlowAggregatesAsync(
        string declarationId,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var id = declarationId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("报关单ID不能为空", nameof(declarationId));

        var dec = (await _declarationRepo.FindAsync(d => d.Id == id && !d.IsDeleted)).FirstOrDefault()
                  ?? throw new InvalidOperationException("报关单不存在");

        var items = (await _declarationItemRepo.FindAsync(i => i.DeclarationId == id && !i.IsDeleted))
            .OrderBy(i => i.LineNo)
            .ThenBy(i => i.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        CustomsBroker? broker = null;
        if (!string.IsNullOrWhiteSpace(dec.CustomsBrokerId))
            broker = await _brokerRepo.GetByIdAsync(dec.CustomsBrokerId.Trim());

        User? decCreator = null;
        if (!string.IsNullOrWhiteSpace(dec.CreateByUserId))
            decCreator = await _userRepo.GetByIdAsync(dec.CreateByUserId.Trim());

        Packing? packing = null;
        if (!string.IsNullOrWhiteSpace(dec.PackingId))
            packing = await _packingRepo.GetByIdAsync(dec.PackingId.Trim());

        var packingItems = packing == null
            ? new List<PackingItem>()
            : (await _packingItemRepo.FindAsync(pi => pi.PackingId == packing.Id && !pi.IsDeleted)).ToList();

        var sellLineIds = DistinctIds(items.Select(i => i.SellOrderItemId));
        var sellLines = sellLineIds.Count == 0
            ? new List<SellOrderItem>()
            : (await _sellOrderItemRepo.FindIgnoreFiltersAsync(l => sellLineIds.Contains(l.Id))).ToList();
        var sellById = sellLines.ToDictionary(x => x.Id.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

        var sellOrderIds = DistinctIds(sellLines.Select(l => l.SellOrderId));
        var sellOrders = sellOrderIds.Count == 0
            ? new List<SellOrder>()
            : (await _sellOrderRepo.FindIgnoreFiltersAsync(o => sellOrderIds.Contains(o.Id))).ToList();
        var soById = sellOrders.ToDictionary(x => x.Id.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

        var salesSorIds = DistinctIds(items.Select(i => i.StockOutRequestId));
        var salesSors = salesSorIds.Count == 0
            ? new List<StockOutRequest>()
            : (await _stockOutRequestRepo.FindIgnoreFiltersAsync(r => salesSorIds.Contains(r.Id))).ToList();
        var salesSorById = salesSors.ToDictionary(x => x.Id.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

        var pendlistIds = DistinctIds(items.Select(i => i.CustomsPendlistId));
        var pendlists = pendlistIds.Count == 0
            ? new List<CustomsPendlist>()
            : (await _pendlistRepo.FindIgnoreFiltersAsync(p => pendlistIds.Contains(p.Id))).ToList();
        var pendlistById = pendlists.ToDictionary(x => x.Id.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

        var customsSorIds = DistinctIds(items.Select(i => i.CustomsStockOutNotifyId));
        var customsSors = customsSorIds.Count == 0
            ? new List<StockOutRequest>()
            : (await _stockOutRequestRepo.FindIgnoreFiltersAsync(r => customsSorIds.Contains(r.Id))).ToList();
        var customsSorById = customsSors.ToDictionary(x => x.Id.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

        var customerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var i in items)
            AddId(customerIds, i.CustomerId);
        foreach (var so in sellOrders)
            AddId(customerIds, so.CustomerId);
        AddId(customerIds, packing?.CustomerId);
        var customers = customerIds.Count == 0
            ? new List<CustomerInfo>()
            : (await _customerRepo.FindAsync(c => customerIds.Contains(c.Id))).ToList();
        var customerById = customers.ToDictionary(x => x.Id.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

        var userIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var i in items)
            AddId(userIds, i.SalesUserId);
        foreach (var so in sellOrders)
            AddId(userIds, so.SalesUserId);
        AddId(userIds, packing?.SalesId);
        foreach (var p in pendlists)
            AddId(userIds, p.CreateByUserId);
        var users = userIds.Count == 0
            ? new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase)
            : (await _userRepo.FindAsync(u => userIds.Contains(u.Id)))
                .ToDictionary(u => u.Id.Trim(), u => u, StringComparer.OrdinalIgnoreCase);

        var packingId = packing?.Id;
        var stockOutItems = packingId == null
            ? new List<StockOutItem>()
            : (await _stockOutItemRepo.FindAsync(i =>
                    i.PackingId != null && i.PackingId == packingId && !i.IsDeleted)).ToList();
        var stockOutIds = DistinctIds(stockOutItems.Select(i => i.StockOutId));
        var stockOuts = stockOutIds.Count == 0
            ? new List<StockOut>()
            : (await _stockOutRepo.FindAsync(s => stockOutIds.Contains(s.Id) && !s.IsDeleted)).ToList();

        var cdiIds = items.Select(i => i.Id.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var arrivals = cdiIds.Count == 0
            ? new List<StockInNotify>()
            : (await _arrivalRepo.FindAsync(a =>
                    a.CustomsDeclarationItemId != null
                    && cdiIds.Contains(a.CustomsDeclarationItemId)
                    && a.StockInType == StockInTypeCode.Customs
                    && !a.IsDeleted)).ToList();

        var arrivalIds = DistinctIds(arrivals.Select(a => a.Id));
        var qcs = arrivalIds.Count == 0
            ? new List<QCInfo>()
            : (await _qcRepo.FindAsync(q => arrivalIds.Contains(q.StockInNotifyId) && !q.IsDeleted)).ToList();

        var qcIds = DistinctIds(qcs.Select(q => q.Id));
        var stockIns = new List<StockIn>();
        if (arrivalIds.Count > 0 || qcIds.Count > 0)
        {
            stockIns = (await _stockInRepo.FindAsync(s =>
                    s.StockInType == StockInTypeCode.Customs
                    && !s.IsDeleted
                    && ((s.SourceId != null && arrivalIds.Contains(s.SourceId))
                        || (s.QcId != null && qcIds.Contains(s.QcId)))))
                .ToList();
        }

        var dto = new CustomsDeclarationFlowAggregatesDto
        {
            DeclarationId = dec.Id,
            Declaration = new CustomsDeclarationFlowDocDto
            {
                Id = dec.Id,
                DocCode = dec.DeclarationCode,
                Status = dec.InternalStatus,
                CreateTime = dec.CreateTime,
                PersonName = decCreator?.UserName,
                Qty = items.Count == 0 ? null : items.Sum(i => (decimal)i.DeclareQty),
                IsDeleted = dec.IsDeleted,
                BrokerName = string.IsNullOrWhiteSpace(broker?.Cname) ? null : broker!.Cname.Trim(),
                CustomsDeclarationId = dec.Id,
                CustomsDeclarationCode = dec.DeclarationCode
            }
        };

        var seenSell = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var lineId = item.SellOrderItemId?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(lineId) || !seenSell.Add(lineId))
                continue;
            sellById.TryGetValue(lineId, out var line);
            var so = ResolveSellOrder(line, soById);
            var party = ResolveParty(item, so, customerById, users);
            dto.SellOrderItems.Add(new CustomsDeclarationFlowDocDto
            {
                Id = line?.Id ?? lineId,
                DocCode = line?.SellOrderItemCode ?? item.SellOrderItemCode,
                Status = line?.Status,
                CreateTime = line?.CreateTime ?? item.CreateTime,
                CustomerId = party.CustomerId,
                CustomerName = party.CustomerName,
                CustomerCode = party.CustomerCode,
                PersonName = party.PersonName,
                UnitPrice = line?.Price,
                Currency = line?.Currency,
                Qty = line?.Qty,
                IsDeleted = line == null || line.IsDeleted,
                SalesOrderId = so?.Id ?? line?.SellOrderId
            });
        }
        dto.SellOrderItems = OrderByCreate(dto.SellOrderItems);

        var seenSalesSor = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var sorId = item.StockOutRequestId?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(sorId) || !seenSalesSor.Add(sorId))
                continue;
            salesSorById.TryGetValue(sorId, out var sor);
            var so = ResolveSellOrderFromItem(item, sellById, soById);
            var party = ResolveParty(item, so, customerById, users);
            dto.SalesStockOutNotifies.Add(sor == null
                ? new CustomsDeclarationFlowDocDto
                {
                    Id = sorId,
                    IsDeleted = true,
                    CustomerId = party.CustomerId,
                    CustomerName = party.CustomerName,
                    CustomerCode = party.CustomerCode,
                    SalesOrderId = so?.Id
                }
                : new CustomsDeclarationFlowDocDto
                {
                    Id = sor.Id,
                    DocCode = sor.RequestCode,
                    Status = sor.Status,
                    CreateTime = sor.CreateTime,
                    CustomerId = party.CustomerId,
                    CustomerName = party.CustomerName,
                    CustomerCode = party.CustomerCode,
                    PersonName = party.PersonName,
                    Qty = sor.Quantity,
                    IsDeleted = sor.IsDeleted,
                    SalesOrderId = sor.SalesOrderId ?? so?.Id
                });
        }
        dto.SalesStockOutNotifies = OrderByCreate(dto.SalesStockOutNotifies);

        var seenPend = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var pid = item.CustomsPendlistId?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(pid) || !seenPend.Add(pid))
                continue;
            pendlistById.TryGetValue(pid, out var pend);
            var so = ResolveSellOrderFromItem(item, sellById, soById);
            var party = ResolveParty(item, so, customerById, users);
            users.TryGetValue(pend?.CreateByUserId?.Trim() ?? string.Empty, out var pendCreator);
            salesSorById.TryGetValue(item.StockOutRequestId?.Trim() ?? string.Empty, out var salesSor);
            dto.Pendlists.Add(new CustomsDeclarationFlowDocDto
            {
                Id = pend?.Id ?? pid,
                DocCode = salesSor?.RequestCode,
                Status = pend?.Status,
                CreateTime = pend?.CreateTime,
                CustomerId = party.CustomerId,
                CustomerName = party.CustomerName,
                CustomerCode = party.CustomerCode,
                PersonName = pendCreator?.UserName,
                Qty = pend?.Qty,
                IsDeleted = pend == null || pend.IsDeleted,
                SalesOrderId = so?.Id
            });
        }
        dto.Pendlists = OrderByCreate(dto.Pendlists);

        var seenCustomsSor = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var cid = item.CustomsStockOutNotifyId?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(cid) || !seenCustomsSor.Add(cid))
                continue;
            customsSorById.TryGetValue(cid, out var sor);
            var so = ResolveSellOrderFromItem(item, sellById, soById);
            var party = ResolveParty(item, so, customerById, users);
            dto.CustomsStockOutNotifies.Add(sor == null
                ? new CustomsDeclarationFlowDocDto
                {
                    Id = cid,
                    IsDeleted = true,
                    CustomerId = party.CustomerId,
                    CustomerName = party.CustomerName,
                    CustomerCode = party.CustomerCode,
                    StockOutType = StockOutTypeCode.Customs,
                    CustomsDeclarationId = dec.Id,
                    CustomsDeclarationCode = dec.DeclarationCode
                }
                : new CustomsDeclarationFlowDocDto
                {
                    Id = sor.Id,
                    DocCode = sor.RequestCode,
                    Status = sor.Status,
                    CreateTime = sor.CreateTime,
                    CustomerId = party.CustomerId,
                    CustomerName = party.CustomerName,
                    CustomerCode = party.CustomerCode,
                    Qty = sor.Quantity,
                    IsDeleted = sor.IsDeleted,
                    SalesOrderId = sor.SalesOrderId ?? so?.Id,
                    StockOutType = StockOutTypeCode.NormalizeForNotify(sor.StockOutType),
                    CustomsDeclarationId = dec.Id,
                    CustomsDeclarationCode = dec.DeclarationCode
                });
        }
        dto.CustomsStockOutNotifies = OrderByCreate(dto.CustomsStockOutNotifies);

        if (packing != null && !packing.IsDeleted)
        {
            users.TryGetValue(packing.SalesId?.Trim() ?? string.Empty, out var packingSales);
            CustomerInfo? packingCust = null;
            if (!string.IsNullOrWhiteSpace(packing.CustomerId))
                customerById.TryGetValue(packing.CustomerId.Trim(), out packingCust);
            var lineQty = packingItems.Sum(i => (decimal)i.Qty);
            dto.Packing = new CustomsDeclarationFlowDocDto
            {
                Id = packing.Id,
                DocCode = packing.Code,
                Status = packing.Status,
                CreateTime = packing.CreateTime,
                CustomerId = packing.CustomerId,
                CustomerName = packingCust?.CustomerName,
                CustomerCode = packingCust?.CustomerCode,
                PersonName = packingSales?.UserName,
                Qty = lineQty > 0 ? lineQty : null,
                StockOutType = StockOutTypeCode.NormalizeForNotify(packing.StockOutType),
                CustomsDeclarationId = dec.Id,
                CustomsDeclarationCode = dec.DeclarationCode
            };
        }

        foreach (var s in stockOuts.OrderBy(x => x.CreateTime))
        {
            var qty = stockOutItems.Where(i => i.StockOutId == s.Id).Sum(i => i.Quantity);
            CustomerInfo? soCust = null;
            if (!string.IsNullOrWhiteSpace(s.CustomerId))
                customerById.TryGetValue(s.CustomerId.Trim(), out soCust);
            dto.StockOuts.Add(new CustomsDeclarationFlowDocDto
            {
                Id = s.Id,
                DocCode = s.StockOutCode,
                Status = s.Status,
                CreateTime = s.CreateTime,
                CustomerId = s.CustomerId,
                CustomerName = soCust?.CustomerName,
                CustomerCode = soCust?.CustomerCode,
                Qty = qty,
                StockOutType = StockOutTypeCode.NormalizeForNotify(s.StockOutType),
                CustomsDeclarationId = dec.Id,
                CustomsDeclarationCode = dec.DeclarationCode
            });
        }

        foreach (var a in arrivals.OrderBy(x => x.CreateTime))
        {
            var item = items.FirstOrDefault(i =>
                string.Equals(i.Id, a.CustomsDeclarationItemId, StringComparison.OrdinalIgnoreCase));
            var so = item == null ? null : ResolveSellOrderFromItem(item, sellById, soById);
            var party = item == null
                ? default
                : ResolveParty(item, so, customerById, users);
            dto.Arrivals.Add(new CustomsDeclarationFlowDocDto
            {
                Id = a.Id,
                DocCode = a.NoticeCode,
                Status = a.Status,
                CreateTime = a.CreateTime,
                CustomerId = party.CustomerId,
                CustomerName = party.CustomerName,
                CustomerCode = party.CustomerCode,
                PersonName = a.PurchaseUserName,
                Qty = a.ExpectQty,
                UnitPrice = a.Cost
            });
        }

        foreach (var q in qcs.OrderBy(x => x.CreateTime))
        {
            dto.Qcs.Add(new CustomsDeclarationFlowDocDto
            {
                Id = q.Id,
                DocCode = q.QcCode,
                Status = q.Status,
                CreateTime = q.CreateTime
            });
        }

        foreach (var s in stockIns.OrderBy(x => x.CreateTime))
        {
            dto.StockIns.Add(new CustomsDeclarationFlowDocDto
            {
                Id = s.Id,
                DocCode = s.StockInCode,
                Status = s.Status,
                CreateTime = s.CreateTime,
                Qty = s.TotalQuantity,
                StockInType = StockInTypeCode.Normalize(s.StockInType),
                CustomsDeclarationId = dec.Id,
                CustomsDeclarationCode = dec.DeclarationCode
            });
        }

        return dto;
    }

    private static SellOrder? ResolveSellOrder(SellOrderItem? line, IReadOnlyDictionary<string, SellOrder> soById)
    {
        if (line == null || string.IsNullOrWhiteSpace(line.SellOrderId))
            return null;
        soById.TryGetValue(line.SellOrderId.Trim(), out var so);
        return so;
    }

    private static SellOrder? ResolveSellOrderFromItem(
        CustomsDeclarationItem item,
        IReadOnlyDictionary<string, SellOrderItem> sellById,
        IReadOnlyDictionary<string, SellOrder> soById)
    {
        var lineId = item.SellOrderItemId?.Trim() ?? string.Empty;
        sellById.TryGetValue(lineId, out var line);
        return ResolveSellOrder(line, soById);
    }

    private readonly struct PartySnap
    {
        public string? CustomerId { get; init; }
        public string? CustomerName { get; init; }
        public string? CustomerCode { get; init; }
        public string? PersonName { get; init; }
    }

    private static PartySnap ResolveParty(
        CustomsDeclarationItem item,
        SellOrder? so,
        IReadOnlyDictionary<string, CustomerInfo> customerById,
        IReadOnlyDictionary<string, User> users)
    {
        var custId = FirstId(item.CustomerId, so?.CustomerId);
        CustomerInfo? customer = null;
        if (!string.IsNullOrEmpty(custId))
            customerById.TryGetValue(custId, out customer);
        var salesId = FirstId(item.SalesUserId, so?.SalesUserId);
        users.TryGetValue(salesId ?? string.Empty, out var sales);
        return new PartySnap
        {
            CustomerId = custId,
            CustomerName = so?.CustomerName ?? customer?.CustomerName,
            CustomerCode = customer?.CustomerCode,
            PersonName = sales?.UserName
        };
    }

    private static List<string> DistinctIds(IEnumerable<string?> values)
    {
        return values
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
    }

    private static void AddId(HashSet<string> set, string? value)
    {
        var v = value?.Trim();
        if (!string.IsNullOrEmpty(v))
            set.Add(v);
    }

    private static string? FirstId(params string?[] values)
    {
        foreach (var v in values)
        {
            var s = v?.Trim();
            if (!string.IsNullOrEmpty(s))
                return s;
        }
        return null;
    }

    private static List<CustomsDeclarationFlowDocDto> OrderByCreate(List<CustomsDeclarationFlowDocDto> list)
    {
        return list
            .OrderBy(x => x.CreateTime ?? DateTime.MinValue)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
