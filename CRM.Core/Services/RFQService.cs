using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Rbac;
using CRM.Core.Models.Quote;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.System;
using CRM.Core.Utilities;
using Microsoft.Extensions.Logging;

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
        private readonly IRepository<SysParam> _sysParamRepo;
        private readonly IRepository<RbacRole> _rbacRoleRepo;
        private readonly IRepository<RbacUserRole> _rbacUserRoleRepo;
        private readonly IRepository<RbacDepartment> _rbacDepartmentRepo;
        private readonly IRepository<RbacUserDepartment> _rbacUserDepartmentRepo;
        private readonly IRepository<Quote> _quoteRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRbacService _rbacService;
        private readonly ILogger<RFQService> _logger;

        public RFQService(
            IRepository<RFQ> rfqRepo,
            IRepository<RFQItem> itemRepo,
            IRepository<CustomerInfo> customerRepo,
            IEntityLookupService entityLookup,
            IUnitOfWork unitOfWork,
            ISerialNumberService serialNumberService,
            IDataPermissionService dataPermissionService,
            IUserService userService,
            IRepository<SysParam> sysParamRepo,
            IRepository<RbacRole> rbacRoleRepo,
            IRepository<RbacUserRole> rbacUserRoleRepo,
            IRepository<RbacDepartment> rbacDepartmentRepo,
            IRepository<RbacUserDepartment> rbacUserDepartmentRepo,
            IRepository<Quote> quoteRepo,
            IRepository<User> userRepo,
            IRbacService rbacService,
            ILogger<RFQService> logger)
        {
            _rfqRepo = rfqRepo;
            _itemRepo = itemRepo;
            _customerRepo = customerRepo;
            _entityLookup = entityLookup;
            _unitOfWork = unitOfWork;
            _serialNumberService = serialNumberService;
            _dataPermissionService = dataPermissionService;
            _userService = userService;
            _sysParamRepo = sysParamRepo;
            _rbacRoleRepo = rbacRoleRepo;
            _rbacUserRoleRepo = rbacUserRoleRepo;
            _rbacDepartmentRepo = rbacDepartmentRepo;
            _rbacUserDepartmentRepo = rbacUserDepartmentRepo;
            _quoteRepo = quoteRepo;
            _userRepo = userRepo;
            _rbacService = rbacService;
            _logger = logger;
        }

        // ─── Create ──────────────────────────────────────────────────────────────
        public async Task<RFQ> CreateAsync(CreateRFQRequest request, string? actingUserId = null)
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

            _logger.LogInformation(
                "【需求-采购员轮询】开始新建需求：RfqCode={RfqCode} CustomerId={CustomerId} 明细行数={ItemCount} SalesUserId={SalesUserId}",
                rfqCode,
                request.CustomerId,
                request.Items?.Count ?? 0,
                request.SalesUserId ?? "(null)");

            // 每个需求取轮询队列中连续 2 名采购员，写入该需求下全部明细；游标全局 +2
            var (purchaser1, purchaser2) = await TakeNextRoundRobinPurchaserPairAsync();

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
                AssignMethod = purchaser1 != null ? (short)2 : request.AssignMethod,
                Industry = request.Industry,
                Product = request.Product,
                TargetType = request.TargetType,
                Importance = request.Importance,
                IsLastInquiry = request.IsLastInquiry,
                ProjectBackground = request.ProjectBackground,
                Competitor = request.Competitor,
                Remark = request.Remark,
                Status = purchaser1 != null ? (short)1 : (short)0,
                ItemCount = request.Items?.Count ?? 0,
                CreateTime = DateTime.UtcNow,
                CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId)
            };

            await _rfqRepo.AddAsync(rfq);

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
                        AssignedPurchaserUserId1 = purchaser1,
                        AssignedPurchaserUserId2 = purchaser2,
                        CreateTime = DateTime.UtcNow
                    };
                    await _itemRepo.AddAsync(item);
                }
            }

            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "【需求-采购员轮询】新建需求已保存：RfqId={RfqId} RfqCode={RfqCode} Status={Status}(0待分配/1已分配) AssignMethod={AssignMethod} " +
                "AssignedPurchaserUserId1={P1} AssignedPurchaserUserId2={P2} 明细行数={ItemCount}",
                rfq.Id,
                rfq.RfqCode,
                rfq.Status,
                rfq.AssignMethod,
                purchaser1 ?? "(null)",
                purchaser2 ?? "(null)",
                request.Items?.Count ?? 0);

            return rfq;
        }

        // ─── Read ────────────────────────────────────────────────────────────────
        public async Task<RFQ?> GetByIdAsync(string id, string? viewerUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            var rfq = await _rfqRepo.GetByIdAsync(id);
            if (rfq == null) return null;
            // 加载明细
            var items = await _itemRepo.FindAsync(i => i.RfqId == id);
            rfq.Items = items.OrderBy(i => i.LineNo).ToList();

            var canViewCustomer = string.IsNullOrWhiteSpace(viewerUserId)
                || await UserCanViewCustomerInRfqContextAsync(viewerUserId);

            // 详情接口补充展示字段（列表接口单独组装；实体表不存客户名/业务员名）
            if (canViewCustomer && !string.IsNullOrWhiteSpace(rfq.CustomerId))
            {
                var customer = await _entityLookup.GetCustomerByIdAsync(rfq.CustomerId);
                if (customer != null)
                    rfq.CustomerName = customer.OfficialName ?? customer.NickName;
            }

            if (!string.IsNullOrWhiteSpace(rfq.SalesUserId))
                rfq.SalesUserName = await _entityLookup.GetUserDisplayNameAsync(rfq.SalesUserId);

            if (canViewCustomer && !string.IsNullOrWhiteSpace(rfq.ContactId))
            {
                var contact = await _entityLookup.GetCustomerContactByIdAsync(rfq.ContactId);
                if (contact != null)
                    rfq.ContactPersonName = contact.Name;
            }

            foreach (var it in rfq.Items)
            {
                it.AssignedPurchaserName1 = await _entityLookup.GetUserDisplayNameAsync(it.AssignedPurchaserUserId1);
                it.AssignedPurchaserName2 = await _entityLookup.GetUserDisplayNameAsync(it.AssignedPurchaserUserId2);
            }

            if (!string.IsNullOrWhiteSpace(viewerUserId) && !canViewCustomer)
                MaskRfqCustomerFieldsForViewer(rfq);

            if (!string.IsNullOrWhiteSpace(viewerUserId))
            {
                var s = await _rbacService.GetUserPermissionSummaryAsync(viewerUserId.Trim());
                if (SaleSensitiveFieldMask521.ShouldMask(s))
                {
                    rfq.SalesUserId = null;
                    rfq.SalesUserName = null;
                }
            }

            return rfq;
        }

        /// <summary>具备 customer.info.read（客户敏感信息字段）或为系统管理员时，可在需求场景查看客户名/联系人等；与 PURCHASER 不授 info.read、仅 customer.read 的口径一致。</summary>
        private async Task<bool> UserCanViewCustomerInRfqContextAsync(string userId)
        {
            var uid = userId.Trim();
            if (string.IsNullOrEmpty(uid)) return false;
            var s = await _rbacService.GetUserPermissionSummaryAsync(uid);
            if (SaleSensitiveFieldMask521.ShouldMask(s)) return false;
            if (s.IsSysAdmin) return true;
            return s.PermissionCodes.Any(c => string.Equals(c, "customer.info.read", StringComparison.OrdinalIgnoreCase));
        }

        private static void MaskRfqCustomerFieldsForViewer(RFQ rfq)
        {
            rfq.CustomerId = null;
            rfq.CustomerName = null;
            rfq.ContactId = null;
            rfq.ContactPersonName = null;
            rfq.ContactEmail = null;
            if (rfq.Items == null) return;
            foreach (var it in rfq.Items)
            {
                it.CustomerMpn = null;
                it.CustomerBrand = string.Empty;
            }
        }

        private static void MaskRfqListItemCustomerFields(RFQListItem item)
        {
            item.CustomerId = null;
            item.CustomerName = null;
        }

        private static void MaskRfqItemListRowCustomerFields(RFQItemListItem row)
        {
            row.CustomerId = null;
            row.CustomerName = null;
            row.CustomerMpn = null;
            row.CustomerBrand = null;
        }

        public async Task<PagedResult<RFQListItem>> GetPagedAsync(RFQQueryRequest request)
        {
            var canViewCustomerInList = string.IsNullOrWhiteSpace(request.CurrentUserId)
                || await UserCanViewCustomerInRfqContextAsync(request.CurrentUserId!);
            var effectiveCustomerIdFilter = canViewCustomerInList ? request.CustomerId : null;

            var all = await _rfqRepo.GetAllAsync();
            var query = all.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var kw = request.Keyword.ToLower();
                query = query.Where(r =>
                    r.RfqCode.ToLower().Contains(kw) ||
                    (r.Industry != null && r.Industry.ToLower().Contains(kw)) ||
                    (r.Product != null && r.Product.ToLower().Contains(kw)) ||
                    (r.Remark != null && r.Remark.ToLower().Contains(kw)));
            }
            if (request.Status.HasValue)
                query = query.Where(r => r.Status == request.Status.Value);
            if (!string.IsNullOrWhiteSpace(effectiveCustomerIdFilter))
                query = query.Where(r => r.CustomerId == effectiveCustomerIdFilter);
            if (request.StartDate.HasValue)
                query = query.Where(r => r.CreateTime >= request.StartDate.Value);
            if (request.EndDate.HasValue)
                query = query.Where(r => r.CreateTime <= request.EndDate.Value);

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

            var users = (await _userService.GetAllAsync())
                .ToDictionary(u => u.Id, u => u, StringComparer.OrdinalIgnoreCase);

            var listItems = items.Select(r =>
            {
                users.TryGetValue(r.SalesUserId ?? string.Empty, out var salesUser);
                users.TryGetValue(r.CreateByUserId ?? string.Empty, out var createUser);
                return new RFQListItem
                {
                    Id = r.Id,
                    RfqCode = r.RfqCode,
                    CustomerId = r.CustomerId,
                    CustomerName = r.CustomerId != null && customers.ContainsKey(r.CustomerId) ? customers[r.CustomerId] : null,
                    Status = r.Status,
                    RfqType = r.RfqType,
                    TargetType = r.TargetType,
                    Industry = r.Industry,
                    Product = r.Product,
                    Importance = r.Importance,
                    ItemCount = r.ItemCount,
                    Remark = r.Remark,
                    CreateTime = r.CreateTime,
                    SalesUserId = r.SalesUserId,
                    SalesUserName = EntityLookupService.FormatUserDisplayName(salesUser),
                    CreateByUserId = r.CreateByUserId,
                    CreateUserName = EntityLookupService.FormatUserDisplayName(createUser)
                };
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

            if (!canViewCustomerInList)
            {
                foreach (var it in pagedItems)
                    MaskRfqListItemCustomerFields(it);
            }

            if (!string.IsNullOrWhiteSpace(request.CurrentUserId))
            {
                var s = await _rbacService.GetUserPermissionSummaryAsync(request.CurrentUserId.Trim());
                if (SaleSensitiveFieldMask521.ShouldMask(s))
                {
                    foreach (var it in pagedItems)
                    {
                        it.SalesUserId = null;
                        it.SalesUserName = null;
                    }
                }
            }

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
            var canViewCustomerInList = string.IsNullOrWhiteSpace(request.CurrentUserId)
                || await UserCanViewCustomerInRfqContextAsync(request.CurrentUserId!);
            var customerKeywordForFilter = canViewCustomerInList ? request.CustomerKeyword : null;

            var allRfqs = (await _rfqRepo.GetAllAsync()).ToDictionary(r => r.Id);

            System.Func<RFQ, RFQItem, bool>? linePredicate = null;
            if (!string.IsNullOrWhiteSpace(request.CurrentUserId))
                linePredicate = await _dataPermissionService.GetRfqItemLineVisibilityPredicateAsync(request.CurrentUserId!);

            var customers = new Dictionary<string, string>();
            if (_customerRepo != null)
            {
                var allCustomers = await _customerRepo.GetAllAsync();
                customers = allCustomers.ToDictionary(c => c.Id, c => c.OfficialName ?? c.NickName ?? "");
            }

            var users = (await _userService.GetAllAsync()).ToDictionary(u => u.Id, u => u);

            var allItems = (await _itemRepo.GetAllAsync()).ToList();

            // 存在报价头 rfq_item_id 指向该明细时，库内 status 可能未回写；列表展示与「报价条数」一致（仅按 RFQItemId，不按 RFQId+Mpn，避免同 PN 多行歧义）。
            var allQuotes = (await _quoteRepo.GetAllAsync()).ToList();
            var itemIdsWithQuoteHeader = new HashSet<string>(
                allQuotes
                    .Where(q => !string.IsNullOrWhiteSpace(q.RFQItemId))
                    .Select(q => q.RFQItemId!.Trim()),
                StringComparer.OrdinalIgnoreCase);

            var rows = new List<RFQItemListItem>();
            foreach (var item in allItems)
            {
                if (!allRfqs.TryGetValue(item.RfqId, out var rfq))
                    continue;
                if (linePredicate != null && !linePredicate(rfq, item))
                    continue;

                users.TryGetValue(rfq.SalesUserId ?? "", out var salesUser);
                users.TryGetValue(item.AssignedPurchaserUserId1 ?? "", out var pu1);
                users.TryGetValue(item.AssignedPurchaserUserId2 ?? "", out var pu2);
                var customerName = rfq.CustomerId != null && customers.TryGetValue(rfq.CustomerId, out var cn)
                    ? cn
                    : null;

                var lineStatus = item.Status;
                if (lineStatus == 0 && itemIdsWithQuoteHeader.Contains((item.Id ?? string.Empty).Trim()))
                    lineStatus = 1;

                rows.Add(new RFQItemListItem
                {
                    Id = item.Id ?? string.Empty,
                    RfqId = item.RfqId,
                    RfqCode = rfq.RfqCode,
                    RfqCreateTime = rfq.CreateTime,
                    LineNo = item.LineNo,
                    Mpn = item.Mpn,
                    CustomerMpn = item.CustomerMpn,
                    CustomerBrand = string.IsNullOrWhiteSpace(item.CustomerBrand) ? null : item.CustomerBrand.Trim(),
                    Brand = item.Brand,
                    Quantity = item.Quantity,
                    Status = lineStatus,
                    CustomerId = rfq.CustomerId,
                    CustomerName = customerName,
                    SalesUserId = rfq.SalesUserId,
                    SalesUserName = EntityLookupService.FormatUserDisplayName(salesUser),
                    AssignedPurchaserUserId1 = item.AssignedPurchaserUserId1,
                    AssignedPurchaserUserId2 = item.AssignedPurchaserUserId2,
                    AssignedPurchaserName1 = EntityLookupService.FormatUserDisplayName(pu1),
                    AssignedPurchaserName2 = EntityLookupService.FormatUserDisplayName(pu2),
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

            if (!string.IsNullOrWhiteSpace(customerKeywordForFilter))
            {
                var kw = customerKeywordForFilter.Trim().ToLowerInvariant();
                rows = rows.Where(r =>
                    (r.CustomerName != null && r.CustomerName.ToLowerInvariant().Contains(kw)) ||
                    (r.CustomerId != null && r.CustomerId.ToLowerInvariant().Contains(kw))).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.MaterialModel))
            {
                var kw = request.MaterialModel.Trim().ToLowerInvariant();
                rows = rows.Where(r =>
                    r.Mpn.ToLowerInvariant().Contains(kw) ||
                    (canViewCustomerInList && r.CustomerMpn != null && r.CustomerMpn.ToLowerInvariant().Contains(kw))).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.SalesUserId))
            {
                var sid = request.SalesUserId.Trim();
                rows = rows.Where(r =>
                    r.SalesUserId != null &&
                    string.Equals(r.SalesUserId, sid, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else if (!string.IsNullOrWhiteSpace(request.SalesUserKeyword))
            {
                var kw = request.SalesUserKeyword.Trim().ToLowerInvariant();
                rows = rows.Where(r =>
                    (r.SalesUserName != null && r.SalesUserName.ToLowerInvariant().Contains(kw)) ||
                    (r.SalesUserId != null && r.SalesUserId.ToLowerInvariant().Contains(kw)) ||
                    (users.TryGetValue(r.SalesUserId ?? "", out var u) &&
                     (u.UserName.ToLowerInvariant().Contains(kw) ||
                      (u.RealName != null && u.RealName.ToLowerInvariant().Contains(kw))))).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.PurchaserUserId))
            {
                var pid = request.PurchaserUserId.Trim();
                rows = rows.Where(r =>
                    string.Equals(r.AssignedPurchaserUserId1, pid, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(r.AssignedPurchaserUserId2, pid, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (request.HasQuotesOnly == true)
            {
                var rfqItemIdsWithQuote = new HashSet<string>(
                    allQuotes
                        .Where(q => !string.IsNullOrWhiteSpace(q.RFQItemId))
                        .Select(q => q.RFQItemId!.Trim()),
                    StringComparer.OrdinalIgnoreCase);
                rows = rows.Where(r => rfqItemIdsWithQuote.Contains(r.Id.Trim())).ToList();
            }

            rows = rows.OrderByDescending(r => r.RfqCreateTime).ThenBy(r => r.LineNo).ToList();

            var totalCount = rows.Count;
            var pagedItems = rows
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            if (!canViewCustomerInList)
            {
                foreach (var r in pagedItems)
                    MaskRfqItemListRowCustomerFields(r);
            }

            if (!string.IsNullOrWhiteSpace(request.CurrentUserId))
            {
                var s = await _rbacService.GetUserPermissionSummaryAsync(request.CurrentUserId.Trim());
                if (SaleSensitiveFieldMask521.ShouldMask(s))
                {
                    foreach (var r in pagedItems)
                    {
                        r.SalesUserId = null;
                        r.SalesUserName = null;
                    }
                }
            }

            return new PagedResult<RFQItemListItem>
            {
                Items = pagedItems,
                TotalCount = totalCount,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
            };
        }

        // ─── Update ──────────────────────────────────────────────────────────────
        public async Task<RFQ> UpdateAsync(string id, UpdateRFQRequest request, string? actingUserId = null)
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
            rfq.ModifyTime = DateTime.UtcNow;
            rfq.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);

            // 更新明细：删除旧的，重新插入（全单共用新一轮询的一对采购员；无明细时不消耗游标）
            if (request.Items != null)
            {
                var oldItems = await _itemRepo.FindAsync(i => i.RfqId == id);
                foreach (var old in oldItems)
                    await _itemRepo.DeleteAsync(old.Id);

                string? purchaser1 = null;
                string? purchaser2 = null;
                if (request.Items.Count > 0)
                {
                    _logger.LogInformation(
                        "【需求-采购员轮询】更新需求并重发明细，重新取轮询对：RfqId={RfqId} RfqCode={RfqCode} 新明细行数={ItemCount}",
                        rfq.Id,
                        rfq.RfqCode,
                        request.Items.Count);
                    (purchaser1, purchaser2) = await TakeNextRoundRobinPurchaserPairAsync();
                }

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
                        AssignedPurchaserUserId1 = purchaser1,
                        AssignedPurchaserUserId2 = purchaser2,
                        CreateTime = DateTime.UtcNow
                    };
                    await _itemRepo.AddAsync(item);
                }

                if (purchaser1 != null)
                {
                    rfq.AssignMethod = 2;
                    if (rfq.Status == 0)
                        rfq.Status = 1;
                }

                rfq.ItemCount = request.Items.Count;

                if (request.Items.Count > 0)
                {
                    _logger.LogInformation(
                        "【需求-采购员轮询】更新需求明细已写入：RfqId={RfqId} Status={Status} AssignMethod={AssignMethod} " +
                        "AssignedPurchaserUserId1={P1} AssignedPurchaserUserId2={P2}",
                        rfq.Id,
                        rfq.Status,
                        rfq.AssignMethod,
                        purchaser1 ?? "(null)",
                        purchaser2 ?? "(null)");
                }
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
        public async Task UpdateStatusAsync(string id, short status, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID不能为空");
            var rfq = await _rfqRepo.GetByIdAsync(id);
            if (rfq == null) throw new InvalidOperationException($"需求 {id} 不存在");
            rfq.Status = status;
            rfq.ModifyTime = DateTime.UtcNow;
            rfq.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
            await _rfqRepo.UpdateAsync(rfq);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<RFQ> AssignPurchaserAsync(string rfqId, AssignPurchaserRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(rfqId)) throw new ArgumentException("ID不能为空");
            if (request == null || string.IsNullOrWhiteSpace(request.PurchaserId))
                throw new ArgumentException("请选择采购员");

            var rfq = await _rfqRepo.GetByIdAsync(rfqId);
            if (rfq == null) throw new InvalidOperationException($"需求 {rfqId} 不存在");

            if (rfq.Status == 7 || rfq.Status == 8)
                throw new ArgumentException("需求已关闭或已取消，无法分配采购员");

            var raw = request.PurchaserId.Trim();
            var purchaser = await _userService.GetByIdAsync(raw)
                ?? await _userService.GetByUserNameAsync(raw);
            if (purchaser == null || !purchaser.IsActive)
                throw new ArgumentException("采购员不存在或已停用");

            var items = await _itemRepo.FindAsync(i => i.RfqId == rfqId);
            foreach (var item in items)
            {
                item.AssignedPurchaserUserId1 = purchaser.Id;
                item.AssignedPurchaserUserId2 = null;
                item.ModifyTime = DateTime.UtcNow;
                await _itemRepo.UpdateAsync(item);
            }

            rfq.AssignMethod = 4;
            if (rfq.Status == 0)
                rfq.Status = 1;
            rfq.ModifyTime = DateTime.UtcNow;
            rfq.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
            await _rfqRepo.UpdateAsync(rfq);

            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();

            return await GetByIdAsync(rfqId) ?? rfq;
        }

        /// <summary>
        /// 从全局轮询池取连续 2 名采购员（同一需求下所有明细相同）；游标 +2。
        /// 池仅 1 人时两人相同；池为空时返回 (null,null)。
        /// </summary>
        private async Task<(string? UserId1, string? UserId2)> TakeNextRoundRobinPurchaserPairAsync()
        {
            //取采购员池
            var pool = await GetPurchaserPoolOrderedAsync();
            var n = pool.Count;
            if (n == 0)
            {
                _logger.LogWarning(
                    "【需求-采购员轮询】采购员池为空，跳过分配。请检查系统参数 {ParamCode}、角色 RoleCode、rbac_user_role 与用户 IsActive。",
                    SysParamCodes.RfqRoundRobinPurchaserRoleCodes);
                return (null, null);
            }

            //取游标
            var cursor = await GetRoundRobinCursorAsync();
            var idx1 = cursor % n;
            var idx2 = (cursor + 1) % n;
            var a1 = pool[idx1];
            var a2 = pool[idx2];
            var newCursor = cursor + 2;
            await SaveRoundRobinCursorAsync(newCursor);

            _logger.LogInformation(
                "【需求-采购员轮询】本笔取值：池人数={PoolCount} CursorBefore={CursorBefore} 取下标[{Idx1},{Idx2}] " +
                "UserId1={UserId1} UserId2={UserId2} CursorAfter={CursorAfter}",
                n,
                cursor,
                idx1,
                idx2,
                a1,
                a2,
                newCursor);

            return (a1, a2);
        }

        private async Task<List<string>> GetPurchaserPoolOrderedAsync()
        {
            //取可以报价角色
            var paramRows = await _sysParamRepo.FindAsync(p =>
                p.ParamCode == SysParamCodes.RfqRoundRobinPurchaserRoleCodes && p.Status == 1);
            var paramRow = paramRows.FirstOrDefault();
            var raw = paramRow?.ValueString?.Trim() ?? "";

            if (paramRow == null)
            {
                _logger.LogInformation(
                    "【需求-采购员轮询】系统参数未找到或未启用(Status=1)：{ParamCode}，将使用默认角色编码列表。",
                    SysParamCodes.RfqRoundRobinPurchaserRoleCodes);
            }
            else
            {
                _logger.LogInformation(
                    "【需求-采购员轮询】系统参数 {ParamCode}：Status={Status} ValueString=\"{ValueString}\"",
                    SysParamCodes.RfqRoundRobinPurchaserRoleCodes,
                    paramRow.Status,
                    string.IsNullOrEmpty(raw) ? "(空，将用默认角色码)" : raw);
            }

            var codes = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => s.Length > 0)
                .ToList();
            if (codes.Count == 0)
                codes = new List<string> { "purchase_buyer", "purchaser", "purchase_staff" };

            _logger.LogInformation(
                "【需求-采购员轮询】用于匹配 rbac_role.RoleCode 的编码列表：{Codes}",
                string.Join(", ", codes));

            var allRoles = (await _rbacRoleRepo.GetAllAsync()).ToList();
            var roles = allRoles
                .Where(r => codes.Any(c => string.Equals(c, r.RoleCode, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            var roleIds = roles.Select(r => r.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (roles.Count == 0)
            {
                var sampleCodes = allRoles.Take(15).Select(r => r.RoleCode).ToList();
                _logger.LogWarning(
                    "【需求-采购员轮询】未匹配到任何角色。库中 rbac_role 总数={TotalRoleCount} 前若干 RoleCode 示例：{Sample}",
                    allRoles.Count,
                    sampleCodes.Count == 0 ? "(无角色表数据)" : string.Join(", ", sampleCodes));
                return new List<string>();
            }

            _logger.LogInformation(
                "【需求-采购员轮询】匹配到的角色：{RoleSummary}",
                string.Join("; ", roles.Select(r => $"{r.RoleCode}(Id={r.Id})")));

            var userRoleRows = (await _rbacUserRoleRepo.GetAllAsync())
                .Where(ur => roleIds.Contains(ur.RoleId))
                .ToList();
            var candIds = userRoleRows.Select(ur => ur.UserId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            _logger.LogInformation(
                "【需求-采购员轮询】rbac_user_role 中关联上述角色的用户数（去重 UserId）={CandCount}",
                candIds.Count);

            var allUsers = (await _userRepo.GetAllAsync()).ToList();
            var activeById = allUsers.Where(u => u.IsActive).ToDictionary(u => u.Id, StringComparer.OrdinalIgnoreCase);

            var allDepts = (await _rbacDepartmentRepo.GetAllAsync()).Where(d => d.Status == 1).ToList();
            var allUserDept = (await _rbacUserDepartmentRepo.GetAllAsync()).ToList();

            var purchaseDeptIds = allDepts
                .Where(PurchasingDepartmentRules.IsPurchaseDepartmentForRfqBuyer)
                .Select(d => d.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var opsDeptIds = allDepts
                .Where(PurchasingDepartmentRules.IsPurchasingOperationsDepartment)
                .Select(d => d.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var primaryPurchaseUserIds = allUserDept
                .Where(ud => ud.IsPrimary && purchaseDeptIds.Contains(ud.DepartmentId))
                .Select(ud => ud.UserId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var poolSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pool = new List<string>();

            void TryAddPool(string userId)
            {
                if (string.IsNullOrWhiteSpace(userId) || poolSet.Contains(userId)) return;
                if (!activeById.TryGetValue(userId, out _)) return;
                poolSet.Add(userId);
                pool.Add(userId);
            }

            foreach (var uid in candIds.OrderBy(x => x, StringComparer.Ordinal))
                TryAddPool(uid);

            var fromRoleCount = pool.Count;
            foreach (var uid in primaryPurchaseUserIds.OrderBy(x => x, StringComparer.Ordinal))
                TryAddPool(uid);

            var fromDeptOnly = pool.Count - fromRoleCount;
            if (fromDeptOnly > 0)
            {
                _logger.LogInformation(
                    "【需求-采购员轮询】除角色池外，合并主部门在采购相关部门的在职用户 {DeptOnlyCount} 人（仅绑 DEPT_EMPLOYEE 等、未绑 purchase_buyer 也会入池）。",
                    fromDeptOnly);
            }

            var inactiveOrMissing = candIds.Count - allUsers.Count(u => candIds.Contains(u.Id) && u.IsActive);
            if (inactiveOrMissing > 0)
            {
                _logger.LogInformation(
                    "【需求-采购员轮询】角色候选 UserId 中因用户不存在或 IsActive=false 被过滤约 {Filtered} 个（候选 {Cand} 人）。",
                    inactiveOrMissing,
                    candIds.Count);
            }

            pool.Sort(StringComparer.Ordinal);

            if (opsDeptIds.Count > 0 && pool.Count > 0)
            {
                var before = pool.Count;
                pool = pool
                    .Where(uid =>
                    {
                        var p = allUserDept.FirstOrDefault(ud =>
                            string.Equals(ud.UserId, uid, StringComparison.OrdinalIgnoreCase) && ud.IsPrimary);
                        return p == null || !opsDeptIds.Contains(p.DepartmentId);
                    })
                    .ToList();
                if (before != pool.Count)
                {
                    _logger.LogInformation(
                        "【需求-采购员轮询】已排除主部门在采购运营部的用户 {Removed} 人（不参与询价分配）。",
                        before - pool.Count);
                }
            }

            _logger.LogInformation(
                "【需求-采购员轮询】最终轮询池（按 User.Id 排序，共 {PoolCount} 人）：{PoolIds}",
                pool.Count,
                pool.Count == 0 ? "(空)" : string.Join(", ", pool));

            return pool;
        }

        /// <summary>
        /// 获取采购员轮询游标
        /// </summary>
        /// <returns></returns>
        private async Task<int> GetRoundRobinCursorAsync()
        {
            var rows = await _sysParamRepo.FindAsync(p => p.ParamCode == SysParamCodes.RfqPurchaserRoundRobinCursor);
            var row = rows.FirstOrDefault();
            if (row == null)
            {
                _logger.LogInformation(
                    "【需求-采购员轮询】游标参数不存在 {ParamCode}，按 Cursor=0 处理。",
                    SysParamCodes.RfqPurchaserRoundRobinCursor);
                return 0;
            }

            var v = int.TryParse(row.ValueString?.Trim(), out var parsed) && parsed >= 0 ? parsed : 0;
            if (row.ValueString?.Trim() is { } s && !int.TryParse(s, out _))
                _logger.LogWarning(
                    "【需求-采购员轮询】游标参数 ValueString 非有效非负整数，已按 0 处理：{ParamCode}=\"{Raw}\"",
                    SysParamCodes.RfqPurchaserRoundRobinCursor,
                    s);

            return v;
        }

        private async Task SaveRoundRobinCursorAsync(int cursor)
        {
            var rows = await _sysParamRepo.FindAsync(p => p.ParamCode == SysParamCodes.RfqPurchaserRoundRobinCursor);
            var row = rows.FirstOrDefault();
            if (row == null)
            {
                var groupFrom = (await _sysParamRepo.FindAsync(p => p.ParamCode == SysParamCodes.RfqRoundRobinPurchaserRoleCodes))
                    .FirstOrDefault();
                row = new SysParam
                {
                    Id = "00000000-0000-4000-8000-000000000013",
                    ParamCode = SysParamCodes.RfqPurchaserRoundRobinCursor,
                    ParamName = "需求采购员轮询游标",
                    GroupId = groupFrom?.GroupId,
                    DataType = ParamDataType.String,
                    ValueString = cursor.ToString(),
                    Status = 1,
                    IsSystem = true,
                    IsEditable = true,
                    IsVisible = false,
                    SortOrder = 11,
                    CreateTime = DateTime.UtcNow
                };
                await _sysParamRepo.AddAsync(row);
                _logger.LogInformation(
                    "【需求-采购员轮询】已新建游标参数 {ParamCode}={Cursor}",
                    SysParamCodes.RfqPurchaserRoundRobinCursor,
                    cursor);
                return;
            }

            row.ValueString = cursor.ToString();
            row.ModifyTime = DateTime.UtcNow;
            await _sysParamRepo.UpdateAsync(row);
            _logger.LogInformation(
                "【需求-采购员轮询】已更新游标参数 {ParamCode}={Cursor}",
                SysParamCodes.RfqPurchaserRoundRobinCursor,
                cursor);
        }
    }
}
