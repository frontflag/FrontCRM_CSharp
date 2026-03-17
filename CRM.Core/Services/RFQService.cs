using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using CRM.Core.Models.RFQ;


namespace CRM.Core.Services
{
    /// <summary>需求(RFQ)服务实现</summary>
    public class RFQService : IRFQService
    {
        private readonly IRepository<RFQ> _rfqRepo;
        private readonly IRepository<RFQItem> _itemRepo;
        private readonly IRepository<CustomerInfo> _customerRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;

        public RFQService(
            IRepository<RFQ> rfqRepo,
            IRepository<RFQItem> itemRepo,
            IRepository<CustomerInfo> customerRepo,
            IUnitOfWork unitOfWork,
            ISerialNumberService serialNumberService)
        {
            _rfqRepo = rfqRepo;
            _itemRepo = itemRepo;
            _customerRepo = customerRepo;
            _unitOfWork = unitOfWork;
            _serialNumberService = serialNumberService;
        }

        // ─── Create ──────────────────────────────────────────────────────────────
        public async Task<RFQ> CreateAsync(CreateRFQRequest request)
        {
            // 自动生成需求单号 (格式: RF + 年月日 + 4位序号)
            var rfqCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.RFQ);

            var rfq = new RFQ
            {
                Id = Guid.NewGuid().ToString(),
                RfqCode = rfqCode,
                CustomerId = request.CustomerId,
                ContactId = request.ContactId,
                ContactEmail = request.ContactEmail,
                SalesUserId = request.SalesUserId,
                RfqType = request.RfqType,
                QuoteMethod = request.QuoteMethod,
                AssignMethod = request.AssignMethod,
                Industry = request.Industry,
                Product = request.Product,
                TargetType = request.TargetType,
                Importance = request.Importance,
                IsLastInquiry = request.IsLastInquiry,
                ProjectBackground = request.ProjectBackground,
                Competitor = request.Competitor,
                Remark = request.Remark,
                RfqDate = request.RfqDate == default ? DateTime.UtcNow : request.RfqDate,
                Status = 0,
                ItemCount = request.Items?.Count ?? 0,
                CreateTime = DateTime.UtcNow
            };

            await _rfqRepo.AddAsync(rfq);

            // 保存明细
            if (request.Items != null && request.Items.Count > 0)
            {
                for (int i = 0; i < request.Items.Count; i++)
                {
                    var itemReq = request.Items[i];
                    var item = new RFQItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        RfqId = rfq.Id,
                        LineNo = itemReq.LineNo > 0 ? itemReq.LineNo : i + 1,
                        CustomerMpn = itemReq.CustomerMpn,
                        Mpn = itemReq.Mpn,
                        CustomerBrand = itemReq.CustomerBrand,
                        Brand = itemReq.Brand,
                        TargetPrice = itemReq.TargetPrice,
                        PriceCurrency = itemReq.PriceCurrency,
                        Quantity = itemReq.Quantity,
                        ProductionDate = itemReq.ProductionDate,
                        ExpiryDate = itemReq.ExpiryDate,
                        MinPackageQty = itemReq.MinPackageQty,
                        Moq = itemReq.Moq,
                        Alternatives = itemReq.Alternatives,
                        Remark = itemReq.Remark,
                        Status = 0,
                        CreateTime = DateTime.UtcNow
                    };
                    await _itemRepo.AddAsync(item);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return rfq;
        }

        // ─── Read ────────────────────────────────────────────────────────────────
        public async Task<RFQ?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            var rfq = await _rfqRepo.GetByIdAsync(id);
            if (rfq == null) return null;
            // 加载明细
            var items = await _itemRepo.FindAsync(i => i.RfqId == id);
            rfq.Items = items.OrderBy(i => i.LineNo).ToList();
            return rfq;
        }

        public async Task<PagedResult<RFQListItem>> GetPagedAsync(RFQQueryRequest request)
        {
            var all = await _rfqRepo.GetAllAsync();
            var query = all.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var kw = request.Keyword.ToLower();
                query = query.Where(r =>
                    r.RfqCode.ToLower().Contains(kw) ||
                    (r.Industry != null && r.Industry.ToLower().Contains(kw)) ||
                    (r.Product != null && r.Product.ToLower().Contains(kw)));
            }
            if (request.Status.HasValue)
                query = query.Where(r => r.Status == request.Status.Value);
            if (!string.IsNullOrWhiteSpace(request.CustomerId))
                query = query.Where(r => r.CustomerId == request.CustomerId);
            if (request.StartDate.HasValue)
                query = query.Where(r => r.RfqDate >= request.StartDate.Value);
            if (request.EndDate.HasValue)
                query = query.Where(r => r.RfqDate <= request.EndDate.Value);

            var total = query.Count();
            var items = query
                .OrderByDescending(r => r.CreateTime)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // 批量获取客户名称
            var customerIds = items.Where(r => r.CustomerId != null).Select(r => r.CustomerId!).Distinct().ToList();
            var customers = new Dictionary<string, string>();
            if (customerIds.Count > 0)
            {
                var allCustomers = await _customerRepo.GetAllAsync();
                customers = allCustomers
                    .Where(c => customerIds.Contains(c.Id))
                    .ToDictionary(c => c.Id, c => c.OfficialName ?? c.NickName ?? "");
            }

            var listItems = items.Select(r => new RFQListItem
            {
                Id = r.Id,
                RfqCode = r.RfqCode,
                CustomerId = r.CustomerId,
                CustomerName = r.CustomerId != null && customers.ContainsKey(r.CustomerId) ? customers[r.CustomerId] : null,
                Status = r.Status,
                RfqType = r.RfqType,
                Industry = r.Industry,
                Product = r.Product,
                Importance = r.Importance,
                ItemCount = r.ItemCount,
                RfqDate = r.RfqDate,
                CreateTime = r.CreateTime
            }).ToList();

            return new PagedResult<RFQListItem>
            {
                Items = listItems,
                TotalCount = total,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
            };
        }

        // ─── Update ──────────────────────────────────────────────────────────────
        public async Task<RFQ> UpdateAsync(string id, UpdateRFQRequest request)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID不能为空");
            var rfq = await _rfqRepo.GetByIdAsync(id);
            if (rfq == null) throw new InvalidOperationException($"需求 {id} 不存在");

            if (request.CustomerId != null) rfq.CustomerId = request.CustomerId;
            if (request.ContactId != null) rfq.ContactId = request.ContactId;
            if (request.ContactEmail != null) rfq.ContactEmail = request.ContactEmail;
            if (request.SalesUserId != null) rfq.SalesUserId = request.SalesUserId;
            if (request.RfqType.HasValue) rfq.RfqType = request.RfqType.Value;
            if (request.QuoteMethod.HasValue) rfq.QuoteMethod = request.QuoteMethod.Value;
            if (request.AssignMethod.HasValue) rfq.AssignMethod = request.AssignMethod.Value;
            if (request.Industry != null) rfq.Industry = request.Industry;
            if (request.Product != null) rfq.Product = request.Product;
            if (request.TargetType.HasValue) rfq.TargetType = request.TargetType.Value;
            if (request.Importance.HasValue) rfq.Importance = request.Importance.Value;
            if (request.IsLastInquiry.HasValue) rfq.IsLastInquiry = request.IsLastInquiry.Value;
            if (request.ProjectBackground != null) rfq.ProjectBackground = request.ProjectBackground;
            if (request.Competitor != null) rfq.Competitor = request.Competitor;
            if (request.Remark != null) rfq.Remark = request.Remark;
            if (request.RfqDate.HasValue) rfq.RfqDate = request.RfqDate.Value;
            rfq.ModifyTime = DateTime.UtcNow;

            // 更新明细：删除旧的，重新插入
            if (request.Items != null)
            {
                var oldItems = await _itemRepo.FindAsync(i => i.RfqId == id);
                foreach (var old in oldItems)
                    await _itemRepo.DeleteAsync(old.Id);

                for (int i = 0; i < request.Items.Count; i++)
                {
                    var itemReq = request.Items[i];
                    var item = new RFQItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        RfqId = rfq.Id,
                        LineNo = itemReq.LineNo > 0 ? itemReq.LineNo : i + 1,
                        CustomerMpn = itemReq.CustomerMpn,
                        Mpn = itemReq.Mpn,
                        CustomerBrand = itemReq.CustomerBrand,
                        Brand = itemReq.Brand,
                        TargetPrice = itemReq.TargetPrice,
                        PriceCurrency = itemReq.PriceCurrency,
                        Quantity = itemReq.Quantity,
                        ProductionDate = itemReq.ProductionDate,
                        ExpiryDate = itemReq.ExpiryDate,
                        MinPackageQty = itemReq.MinPackageQty,
                        Moq = itemReq.Moq,
                        Alternatives = itemReq.Alternatives,
                        Remark = itemReq.Remark,
                        Status = 0,
                        CreateTime = DateTime.UtcNow
                    };
                    await _itemRepo.AddAsync(item);
                }
                rfq.ItemCount = request.Items.Count;
            }

            await _rfqRepo.UpdateAsync(rfq);
            await _unitOfWork.SaveChangesAsync();
            return rfq;
        }

        // ─── Delete ──────────────────────────────────────────────────────────────
        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID不能为空");
            var rfq = await _rfqRepo.GetByIdAsync(id);
            if (rfq == null) throw new InvalidOperationException($"需求 {id} 不存在");

            // 级联删除明细
            var items = await _itemRepo.FindAsync(i => i.RfqId == id);
            foreach (var item in items)
                await _itemRepo.DeleteAsync(item.Id);

            await _rfqRepo.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
        }

        // ─── Status ──────────────────────────────────────────────────────────────
        public async Task UpdateStatusAsync(string id, short status)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID不能为空");
            var rfq = await _rfqRepo.GetByIdAsync(id);
            if (rfq == null) throw new InvalidOperationException($"需求 {id} 不存在");
            rfq.Status = status;
            rfq.ModifyTime = DateTime.UtcNow;
            await _rfqRepo.UpdateAsync(rfq);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
