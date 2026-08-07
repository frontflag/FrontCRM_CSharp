using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Customs;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;

namespace CRM.Core.Services;

public class CustomsPendlistFlowService : ICustomsPendlistFlowService
{
    private readonly IRepository<CustomsPendlist> _pendlistRepo;
    private readonly IRepository<StockOutRequest> _stockOutRequestRepo;
    private readonly IRepository<SellOrder> _sellOrderRepo;
    private readonly IRepository<SellOrderItem> _sellOrderItemRepo;
    private readonly IRepository<CustomerInfo> _customerRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<PackingItem> _packingItemRepo;
    private readonly IRepository<Packing> _packingRepo;
    private readonly IRepository<PickingTask> _pickingTaskRepo;
    private readonly IRepository<StockOutItem> _stockOutItemRepo;
    private readonly IRepository<StockOut> _stockOutRepo;
    private readonly IRepository<CustomsDeclarationItem> _declarationItemRepo;
    private readonly IRepository<CustomsDeclaration> _declarationRepo;
    private readonly IRepository<StockInNotify> _arrivalRepo;
    private readonly IRepository<QCInfo> _qcRepo;
    private readonly IRepository<StockIn> _stockInRepo;

    public CustomsPendlistFlowService(
        IRepository<CustomsPendlist> pendlistRepo,
        IRepository<StockOutRequest> stockOutRequestRepo,
        IRepository<SellOrder> sellOrderRepo,
        IRepository<SellOrderItem> sellOrderItemRepo,
        IRepository<CustomerInfo> customerRepo,
        IRepository<User> userRepo,
        IRepository<PackingItem> packingItemRepo,
        IRepository<Packing> packingRepo,
        IRepository<PickingTask> pickingTaskRepo,
        IRepository<StockOutItem> stockOutItemRepo,
        IRepository<StockOut> stockOutRepo,
        IRepository<CustomsDeclarationItem> declarationItemRepo,
        IRepository<CustomsDeclaration> declarationRepo,
        IRepository<StockInNotify> arrivalRepo,
        IRepository<QCInfo> qcRepo,
        IRepository<StockIn> stockInRepo)
    {
        _pendlistRepo = pendlistRepo;
        _stockOutRequestRepo = stockOutRequestRepo;
        _sellOrderRepo = sellOrderRepo;
        _sellOrderItemRepo = sellOrderItemRepo;
        _customerRepo = customerRepo;
        _userRepo = userRepo;
        _packingItemRepo = packingItemRepo;
        _packingRepo = packingRepo;
        _pickingTaskRepo = pickingTaskRepo;
        _stockOutItemRepo = stockOutItemRepo;
        _stockOutRepo = stockOutRepo;
        _declarationItemRepo = declarationItemRepo;
        _declarationRepo = declarationRepo;
        _arrivalRepo = arrivalRepo;
        _qcRepo = qcRepo;
        _stockInRepo = stockInRepo;
    }

    public async Task<CustomsPendlistFlowAggregatesDto> GetFlowAggregatesAsync(
        string pendlistId,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var id = pendlistId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("待报关记录ID不能为空", nameof(pendlistId));

        var pendlist = (await _pendlistRepo.FindIgnoreFiltersAsync(p => p.Id == id)).FirstOrDefault()
                       ?? throw new InvalidOperationException("待报关记录不存在");

        var line = await _sellOrderItemRepo.GetByIdAsync(pendlist.SellOrderItemId.Trim());
        SellOrder? so = null;
        CustomerInfo? customer = null;
        User? soSales = null;
        if (line != null && !string.IsNullOrWhiteSpace(line.SellOrderId))
        {
            so = await _sellOrderRepo.GetByIdAsync(line.SellOrderId.Trim());
            if (so != null && !string.IsNullOrWhiteSpace(so.CustomerId))
                customer = await _customerRepo.GetByIdAsync(so.CustomerId.Trim());
            if (so != null && !string.IsNullOrWhiteSpace(so.SalesUserId))
                soSales = await _userRepo.GetByIdAsync(so.SalesUserId.Trim());
        }

        var salesSor = (await _stockOutRequestRepo.FindIgnoreFiltersAsync(r =>
                r.Id == pendlist.SalesStockOutNotifyId.Trim()))
            .FirstOrDefault();

        StockOutRequest? customsSor = null;
        if (!string.IsNullOrWhiteSpace(pendlist.CustomsStockOutNotifyId))
        {
            customsSor = (await _stockOutRequestRepo.FindIgnoreFiltersAsync(r =>
                    r.Id == pendlist.CustomsStockOutNotifyId.Trim()))
                .FirstOrDefault();
        }

        if (customsSor == null)
        {
            customsSor = (await _stockOutRequestRepo.FindAsync(r =>
                    r.CustomsPendlistId == id
                    && r.StockOutType == StockOutTypeCode.Customs))
                .FirstOrDefault();
        }

        var packingItems = (await _packingItemRepo.FindAsync(pi =>
                pi.CustomsPendlistId == id && !pi.IsDeleted))
            .ToList();
        if (packingItems.Count == 0 && customsSor != null)
        {
            packingItems = (await _packingItemRepo.FindAsync(pi =>
                    pi.StockOutNotifyId == customsSor.Id && !pi.IsDeleted))
                .ToList();
        }

        var packingIds = packingItems
            .Select(pi => pi.PackingId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        var packings = packingIds.Count == 0
            ? new List<Packing>()
            : (await _packingRepo.FindAsync(p => packingIds.Contains(p.Id) && !p.IsDeleted)).ToList();

        var pickings = packingIds.Count == 0
            ? new List<PickingTask>()
            : (await _pickingTaskRepo.FindAsync(t =>
                    t.PackingId != null && packingIds.Contains(t.PackingId) && !t.IsDeleted))
                .Where(t => t.Status != -1)
                .ToList();

        var stockOutItems = packingIds.Count == 0
            ? new List<StockOutItem>()
            : (await _stockOutItemRepo.FindAsync(i =>
                    i.PackingId != null && packingIds.Contains(i.PackingId) && !i.IsDeleted))
                .ToList();
        var stockOutIds = stockOutItems
            .Select(i => i.StockOutId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        var stockOuts = stockOutIds.Count == 0
            ? new List<StockOut>()
            : (await _stockOutRepo.FindAsync(s => stockOutIds.Contains(s.Id) && !s.IsDeleted)).ToList();

        var declarationItems = (await _declarationItemRepo.FindAsync(i =>
                i.CustomsPendlistId == id && !i.IsDeleted))
            .ToList();
        var declarationIds = declarationItems
            .Select(i => i.DeclarationId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        foreach (var p in packings)
        {
            if (!string.IsNullOrWhiteSpace(p.CustomsDeclarationId)
                && declarationIds.All(x => !string.Equals(x, p.CustomsDeclarationId.Trim(), StringComparison.OrdinalIgnoreCase)))
                declarationIds.Add(p.CustomsDeclarationId.Trim());
        }

        var declarations = declarationIds.Count == 0
            ? new List<CustomsDeclaration>()
            : (await _declarationRepo.FindAsync(d => declarationIds.Contains(d.Id) && !d.IsDeleted)).ToList();

        var cdiIds = declarationItems.Select(i => i.Id.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var arrivals = cdiIds.Count == 0
            ? new List<StockInNotify>()
            : (await _arrivalRepo.FindAsync(a =>
                    a.CustomsDeclarationItemId != null
                    && cdiIds.Contains(a.CustomsDeclarationItemId)
                    && a.StockInType == StockInTypeCode.Customs
                    && !a.IsDeleted))
                .ToList();

        var arrivalIds = arrivals.Select(a => a.Id.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var qcs = arrivalIds.Count == 0
            ? new List<QCInfo>()
            : (await _qcRepo.FindAsync(q =>
                    arrivalIds.Contains(q.StockInNotifyId) && !q.IsDeleted))
                .ToList();

        var qcIds = qcs.Select(q => q.Id.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
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

        User? pendlistCreator = null;
        if (!string.IsNullOrWhiteSpace(pendlist.CreateByUserId))
            pendlistCreator = await _userRepo.GetByIdAsync(pendlist.CreateByUserId.Trim());

        var custId = so?.CustomerId;
        var custName = so?.CustomerName ?? customer?.CustomerName;
        var custCode = customer?.CustomerCode;

        var dto = new CustomsPendlistFlowAggregatesDto
        {
            PendlistId = pendlist.Id,
            SellOrderItem = line == null
                ? null
                : new CustomsPendlistFlowDocDto
                {
                    Id = line.Id,
                    DocCode = line.SellOrderItemCode,
                    Status = line.Status,
                    CreateTime = line.CreateTime,
                    CustomerId = custId,
                    CustomerName = custName,
                    CustomerCode = custCode,
                    PersonName = soSales?.UserName,
                    UnitPrice = line.Price,
                    Currency = line.Currency,
                    Qty = line.Qty,
                    SalesOrderId = line.SellOrderId
                },
            SalesStockOutNotify = salesSor == null
                ? new CustomsPendlistFlowDocDto
                {
                    Id = pendlist.SalesStockOutNotifyId,
                    DocCode = null,
                    IsDeleted = true,
                    CustomerId = custId,
                    CustomerName = custName,
                    CustomerCode = custCode
                }
                : new CustomsPendlistFlowDocDto
                {
                    Id = salesSor.Id,
                    DocCode = salesSor.RequestCode,
                    Status = salesSor.Status,
                    CreateTime = salesSor.CreateTime,
                    CustomerId = custId,
                    CustomerName = custName,
                    CustomerCode = custCode,
                    Qty = salesSor.Quantity,
                    IsDeleted = salesSor.IsDeleted,
                    SalesOrderId = salesSor.SalesOrderId
                },
            Pendlist = new CustomsPendlistFlowDocDto
            {
                Id = pendlist.Id,
                DocCode = salesSor?.RequestCode,
                Status = pendlist.Status,
                CreateTime = pendlist.CreateTime,
                CustomerId = custId,
                CustomerName = custName,
                CustomerCode = custCode,
                PersonName = pendlistCreator?.UserName,
                Qty = pendlist.Qty,
                IsDeleted = pendlist.IsDeleted,
                SalesOrderId = so?.Id ?? line?.SellOrderId
            }
        };

        if (customsSor != null)
        {
            dto.CustomsStockOutNotifies.Add(new CustomsPendlistFlowDocDto
            {
                Id = customsSor.Id,
                DocCode = customsSor.RequestCode,
                Status = customsSor.Status,
                CreateTime = customsSor.CreateTime,
                CustomerId = custId,
                CustomerName = custName,
                CustomerCode = custCode,
                Qty = customsSor.Quantity,
                IsDeleted = customsSor.IsDeleted,
                PendlistId = pendlist.Id,
                SalesOrderId = customsSor.SalesOrderId
            });
        }

        var packingUserIds = packings.Select(p => p.SalesId?.Trim()).Where(x => !string.IsNullOrEmpty(x)).Distinct().Cast<string>().ToList();
        var packingUsers = packingUserIds.Count == 0
            ? new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase)
            : (await _userRepo.FindAsync(u => packingUserIds.Contains(u.Id)))
                .ToDictionary(u => u.Id.Trim(), u => u, StringComparer.OrdinalIgnoreCase);

        foreach (var p in packings.OrderBy(x => x.CreateTime))
        {
            packingUsers.TryGetValue(p.SalesId?.Trim() ?? string.Empty, out var salesUser);
            var lineQty = packingItems.Where(i => i.PackingId == p.Id).Sum(i => (decimal)i.Qty);
            dto.Packings.Add(new CustomsPendlistFlowDocDto
            {
                Id = p.Id,
                DocCode = p.Code,
                Status = p.Status,
                CreateTime = p.CreateTime,
                CustomerId = p.CustomerId ?? custId,
                CustomerName = custName,
                CustomerCode = custCode,
                PersonName = salesUser?.UserName,
                Qty = lineQty > 0 ? lineQty : null
            });
        }

        foreach (var t in pickings.OrderBy(x => x.CreateTime))
        {
            dto.Pickings.Add(new CustomsPendlistFlowDocDto
            {
                Id = t.Id,
                DocCode = t.TaskCode,
                Status = t.Status,
                CreateTime = t.CreateTime,
                CustomerId = custId,
                CustomerName = custName,
                CustomerCode = custCode
            });
        }

        foreach (var s in stockOuts.OrderBy(x => x.CreateTime))
        {
            var qty = stockOutItems.Where(i => i.StockOutId == s.Id).Sum(i => i.Quantity);
            dto.StockOuts.Add(new CustomsPendlistFlowDocDto
            {
                Id = s.Id,
                DocCode = s.StockOutCode,
                Status = s.Status,
                CreateTime = s.CreateTime,
                CustomerId = custId,
                CustomerName = custName,
                CustomerCode = custCode,
                Qty = qty
            });
        }

        foreach (var d in declarations.OrderBy(x => x.CreateTime))
        {
            var qty = declarationItems.Where(i => i.DeclarationId == d.Id).Sum(i => (decimal)i.DeclareQty);
            dto.Declarations.Add(new CustomsPendlistFlowDocDto
            {
                Id = d.Id,
                DocCode = d.DeclarationCode,
                Status = d.InternalStatus,
                CreateTime = d.CreateTime,
                CustomerId = custId,
                CustomerName = custName,
                CustomerCode = custCode,
                Qty = qty > 0 ? qty : null
            });
        }

        foreach (var a in arrivals.OrderBy(x => x.CreateTime))
        {
            dto.Arrivals.Add(new CustomsPendlistFlowDocDto
            {
                Id = a.Id,
                DocCode = a.NoticeCode,
                Status = a.Status,
                CreateTime = a.CreateTime,
                CustomerId = custId,
                CustomerName = custName,
                CustomerCode = custCode,
                PersonName = a.PurchaseUserName,
                Qty = a.ExpectQty,
                UnitPrice = a.Cost
            });
        }

        foreach (var q in qcs.OrderBy(x => x.CreateTime))
        {
            dto.Qcs.Add(new CustomsPendlistFlowDocDto
            {
                Id = q.Id,
                DocCode = q.QcCode,
                Status = q.Status,
                CreateTime = q.CreateTime,
                CustomerId = custId,
                CustomerName = custName,
                CustomerCode = custCode
            });
        }

        foreach (var s in stockIns.OrderBy(x => x.CreateTime))
        {
            dto.StockIns.Add(new CustomsPendlistFlowDocDto
            {
                Id = s.Id,
                DocCode = s.StockInCode,
                Status = s.Status,
                CreateTime = s.CreateTime,
                CustomerId = custId,
                CustomerName = custName,
                CustomerCode = custCode,
                Qty = s.TotalQuantity
            });
        }

        return dto;
    }
}
