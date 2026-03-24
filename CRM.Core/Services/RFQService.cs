using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.RFQ;
using CRM.Core.Utilities;


namespace CRM.Core.Services
{
    /// <summary>需求(RFQ)服务实现</summary>
    public class RFQService : IRFQService
    {
        /// <summary>明细字符串列在库中为 NOT NULL；请求 JSON 可能带 null，需归一为非 null。</summary>
        private static string NormalizeLineString(string? value) => (value ?? string.Empty).Trim();
        private readonly IRepository<RFQ> _rfqRepo;
        private readonly IRepository<RFQItem> _itemRepo;
        private readonly IRepository<CustomerInfo> _customerRepo;
        private readonly IEntityLookupService _entityLookup;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IUserService _userService;

        public RFQService(
            IRepository<RFQ> rfqRepo,
            IRepository<RFQItem> itemRepo,
            IRepository<CustomerInfo> customerRepo,
            IEntityLookupService entityLookup,
            IUnitOfWork unitOfWork,
            ISerialNumberService serialNumberService,
            IDataPermissionService dataPermissionService,
            IUserService userService)
        {
            _rfqRepo = rfqRepo;
            _itemRepo = itemRepo;
            _customerRepo = customerRepo;
            _entityLookup = entityLookup;
            _unitOfWork = unitOfWork;
            _serialNumberService = serialNumberService;
            _dataPermissionService = dataPermissionService;
            _userService = userService;
        }

        // ─── Create ──────────────────────────────────────────────────────────────
        public async Task<RFQ> CreateAsync(CreateRFQRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CustomerId))
                throw new ArgumentException("请选择客户");
            if (request.Items == null || request.Items.Count == 0)
                throw new ArgumentException("请至少添加一条需求明细");
            foreach (var line in request.Items)
            {
                if (string.IsNullOrWhiteSpace(line.Mpn))
                    throw new ArgumentException("需求明细中的物料型号(MPN)不能为空");
                if (string.IsNullOrWhiteSpace(NormalizeLineString(line.Brand)))
                    throw new ArgumentException("需求明细中的品牌不能为空");
            }

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
                RfqDate = request.RfqDate == default ? DateTime.UtcNow : PostgreSqlDateTime.ToUtc(request.RfqDate),
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
                        CustomerMpn = string.IsNullOrWhiteSpace(itemReq.CustomerMpn) ? null : itemReq.CustomerMpn.Trim(),
                        Mpn = NormalizeLineString(itemReq.Mpn),
                        CustomerBrand = NormalizeLineString(itemReq.CustomerBrand),
                        Brand = NormalizeLineString(itemReq.Brand),
                        TargetPrice = itemReq.TargetPrice,
                        PriceCurrency = itemReq.PriceCurrency,
                        Quantity = itemReq.Quantity,
                        ProductionDate = itemReq.ProductionDate,
                        ExpiryDate = PostgreSqlDateTime.ToUtc(itemReq.ExpiryDate),
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

            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
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

            // 详情接口补充展示字段（列表接口单独组装；实体表不存客户名/业务员名）
            if (!string.IsNullOrWhiteSpace(rfq.CustomerId))
            {
                var customer = await _entityLookup.GetCustomerByIdAsync(rfq.CustomerId);
                if (customer != null)
                    rfq.CustomerName = customer.OfficialName ?? customer.NickName;
            }

            if (!string.IsNullOrWhiteSpace(rfq.SalesUserId))
                rfq.SalesUserName = await _entityLookup.GetUserDisplayNameAsync(rfq.SalesUserId);

            if (!string.IsNullOrWhiteSpace(rfq.ContactId))
            {
                var contact = await _entityLookup.GetCustomerContactByIdAsync(rfq.ContactId);
                if (contact != null)
                    rfq.ContactPersonName = contact.Name;
            }

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

            var items = query
                .OrderByDescending(r => r.CreateTime)
                .ToList();

            // 批量获取客户名称
            var customerIds = items.Where(r => r.CustomerId != null).Select(r => r.CustomerId!).Distinct().ToList();
            var customers = new Dictionary<string, string>();
            if (customerIds.Count > 0 && _customerRepo != null)
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

            // 数据权限过滤（在分页前）
            if (!string.IsNullOrWhiteSpace(request.CurrentUserId))
            {
                listItems = (await _dataPermissionService.FilterRFQsAsync(request.CurrentUserId, listItems)).ToList();
            }

            var totalCount = listItems.Count;
            var pagedItems = listItems
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PagedResult<RFQListItem>
            {
                Items = pagedItems,
                TotalCount = totalCount,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
            };
        }

        public async Task<PagedResult<RFQItemListItem>> GetPagedItemsAsync(RFQItemQueryRequest request)
        {
            var allRfqs = (await _rfqRepo.GetAllAsync()).ToDictionary(r => r.Id);
            HashSet<string>? allowedRfqIds = null;
            if (!string.IsNullOrWhiteSpace(request.CurrentUserId))
            {
                var minimal = allRfqs.Values.Select(r => new RFQListItem { Id = r.Id }).ToList();
                var allowed = await _dataPermissionService.FilterRFQsAsync(request.CurrentUserId, minimal);
                allowedRfqIds = allowed.Select(x => x.Id).ToHashSet();
            }

            var customers = new Dictionary<string, string>();
            if (_customerRepo != null)
            {
                var allCustomers = await _customerRepo.GetAllAsync();
                customers = allCustomers.ToDictionary(c => c.Id, c => c.OfficialName ?? c.NickName ?? "");
            }

            var users = (await _userService.GetAllAsync()).ToDictionary(u => u.Id, u => u);

            var allItems = (await _itemRepo.GetAllAsync()).ToList();
            if (allowedRfqIds != null)
                allItems = allItems.Where(i => allowedRfqIds.Contains(i.RfqId)).ToList();

            var rows = new List<RFQItemListItem>();
            foreach (var item in allItems)
            {
                if (!allRfqs.TryGetValue(item.RfqId, out var rfq))
                    continue;

                users.TryGetValue(rfq.SalesUserId ?? "", out var salesUser);
                var customerName = rfq.CustomerId != null && customers.TryGetValue(rfq.CustomerId, out var cn)
                    ? cn
                    : null;

                rows.Add(new RFQItemListItem
                {
                    Id = item.Id,
                    RfqId = item.RfqId,
                    RfqCode = rfq.RfqCode,
                    RfqCreateTime = rfq.CreateTime,
                    LineNo = item.LineNo,
                    Mpn = item.Mpn,
                    CustomerMpn = item.CustomerMpn,
                    Brand = item.Brand,
                    Quantity = item.Quantity,
                    Status = item.Status,
                    CustomerId = rfq.CustomerId,
                    CustomerName = customerName,
                    SalesUserId = rfq.SalesUserId,
                    SalesUserName = EntityLookupService.FormatUserDisplayName(salesUser),
                });
            }

            if (request.StartDate.HasValue)
            {
                var start = request.StartDate.Value.Date;
                rows = rows.Where(r => r.RfqCreateTime >= start).ToList();
            }

            if (request.EndDate.HasValue)
            {
                var endExclusive = request.EndDate.Value.Date.AddDays(1);
                rows = rows.Where(r => r.RfqCreateTime < endExclusive).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.CustomerKeyword))
            {
                var kw = request.CustomerKeyword.Trim().ToLowerInvariant();
                rows = rows.Where(r =>
                    (r.CustomerName != null && r.CustomerName.ToLowerInvariant().Contains(kw)) ||
                    (r.CustomerId != null && r.CustomerId.ToLowerInvariant().Contains(kw))).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.MaterialModel))
            {
                var kw = request.MaterialModel.Trim().ToLowerInvariant();
                rows = rows.Where(r =>
                    r.Mpn.ToLowerInvariant().Contains(kw) ||
                    (r.CustomerMpn != null && r.CustomerMpn.ToLowerInvariant().Contains(kw))).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.SalesUserKeyword))
            {
                var kw = request.SalesUserKeyword.Trim().ToLowerInvariant();
                rows = rows.Where(r =>
                    (r.SalesUserName != null && r.SalesUserName.ToLowerInvariant().Contains(kw)) ||
                    (r.SalesUserId != null && r.SalesUserId.ToLowerInvariant().Contains(kw)) ||
                    (users.TryGetValue(r.SalesUserId ?? "", out var u) &&
                     (u.UserName.ToLowerInvariant().Contains(kw) ||
                      (u.RealName != null && u.RealName.ToLowerInvariant().Contains(kw))))).ToList();
            }

            rows = rows.OrderByDescending(r => r.RfqCreateTime).ThenBy(r => r.LineNo).ToList();

            var totalCount = rows.Count;
            var pagedItems = rows
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PagedResult<RFQItemListItem>
            {
                Items = pagedItems,
                TotalCount = totalCount,
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
            if (request.RfqDate.HasValue) rfq.RfqDate = PostgreSqlDateTime.ToUtc(request.RfqDate.Value);
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
                        CustomerMpn = string.IsNullOrWhiteSpace(itemReq.CustomerMpn) ? null : itemReq.CustomerMpn.Trim(),
                        Mpn = NormalizeLineString(itemReq.Mpn),
                        CustomerBrand = NormalizeLineString(itemReq.CustomerBrand),
                        Brand = NormalizeLineString(itemReq.Brand),
                        TargetPrice = itemReq.TargetPrice,
                        PriceCurrency = itemReq.PriceCurrency,
                        Quantity = itemReq.Quantity,
                        ProductionDate = itemReq.ProductionDate,
                        ExpiryDate = PostgreSqlDateTime.ToUtc(itemReq.ExpiryDate),
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
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
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
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
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
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }
    }
}
