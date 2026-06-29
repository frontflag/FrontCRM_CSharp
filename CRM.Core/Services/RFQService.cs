using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Interfaces.RfqAssignment;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Quote;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.System;
using CRM.Core.Services.RfqAssignment;
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
        private readonly IRepository<Quote> _quoteRepo;
        private readonly IRepository<RfqCloseRecord> _closeRecordRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRbacService _rbacService;
        private readonly IRfqPurchaserAssignmentOrchestrator _purchaserAssignmentOrchestrator;
        private readonly IRfqMainListQuery _rfqMainListQuery;
        private readonly IRfqItemListQuery _rfqItemListQuery;
        private readonly ILogger<RFQService> _logger;
        private readonly ILogOperationAppendService _logOperationAppend;
        private readonly IBizBrandService _bizBrandService;
        private readonly IRfqTagService _rfqTagService;

        public RFQService(
            IRepository<RFQ> rfqRepo,
            IRepository<RFQItem> itemRepo,
            IRepository<CustomerInfo> customerRepo,
            IEntityLookupService entityLookup,
            IUnitOfWork unitOfWork,
            ISerialNumberService serialNumberService,
            IDataPermissionService dataPermissionService,
            IUserService userService,
            IRepository<Quote> quoteRepo,
            IRepository<RfqCloseRecord> closeRecordRepo,
            IRepository<User> userRepo,
            IRbacService rbacService,
            IRfqPurchaserAssignmentOrchestrator purchaserAssignmentOrchestrator,
            IRfqMainListQuery rfqMainListQuery,
            IRfqItemListQuery rfqItemListQuery,
            ILogger<RFQService> logger,
            ILogOperationAppendService logOperationAppend,
            IBizBrandService bizBrandService,
            IRfqTagService rfqTagService)
        {
            _rfqRepo = rfqRepo;
            _itemRepo = itemRepo;
            _customerRepo = customerRepo;
            _entityLookup = entityLookup;
            _unitOfWork = unitOfWork;
            _serialNumberService = serialNumberService;
            _dataPermissionService = dataPermissionService;
            _userService = userService;
            _quoteRepo = quoteRepo;
            _closeRecordRepo = closeRecordRepo;
            _userRepo = userRepo;
            _rbacService = rbacService;
            _purchaserAssignmentOrchestrator = purchaserAssignmentOrchestrator;
            _rfqMainListQuery = rfqMainListQuery;
            _rfqItemListQuery = rfqItemListQuery;
            _logger = logger;
            _logOperationAppend = logOperationAppend;
            _bizBrandService = bizBrandService;
            _rfqTagService = rfqTagService;
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

            // 按策略分配询价采购员（默认：条目轮询）
            var assignMethod = ResolveAssignMethod(request.AssignMethod);
            var anyAssigned = false;

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
                Status = 0,
                ItemCount = request.Items?.Count ?? 0,
                CreateTime = DateTime.UtcNow,
                CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId)
            };

            await _rfqRepo.AddAsync(rfq);

            if (request.Items != null && request.Items.Count > 0)
            {
                var assignmentContext = BuildAssignmentContext(
                    rfq.Id,
                    rfq.RfqCode,
                    RfqAssignmentTrigger.Create,
                    request.Items.Select((itemReq, i) => (
                        LineNo: itemReq.LineNo > 0 ? itemReq.LineNo : i + 1,
                        Brand: NormalizeLineString(itemReq.Brand),
                        BrandId: itemReq.BrandId)));

                var assignmentOutcome = await _purchaserAssignmentOrchestrator.AssignAsync(
                    assignMethod,
                    assignmentContext);

                for (int i = 0; i < request.Items.Count; i++)
                {
                    var itemReq = request.Items[i];
                    var lineNo = itemReq.LineNo > 0 ? itemReq.LineNo : i + 1;
                    var assigned = assignmentOutcome.Assignments[i];
                    if (!string.IsNullOrWhiteSpace(assigned.PurchaserUserId1))
                        anyAssigned = true;

                    var item = new RFQItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        RfqId = rfq.Id,
                        LineNo = lineNo,
                        CustomerMpn = string.IsNullOrWhiteSpace(itemReq.CustomerMpn) ? null : itemReq.CustomerMpn.Trim(),
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
                        AssignedPurchaserUserId1 = assigned.PurchaserUserId1,
                        AssignedPurchaserUserId2 = assigned.PurchaserUserId2,
                        CreateTime = DateTime.UtcNow
                    };
                    await ApplyRfqItemFromRequestAsync(item, itemReq, i);
                    await _itemRepo.AddAsync(item);
                }
            }

            if (anyAssigned)
            {
                rfq.AssignMethod = assignMethod;
                rfq.Status = 1;
            }

            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "【需求-采购员轮询】新建需求已保存：RfqId={RfqId} RfqCode={RfqCode} Status={Status}(0待分配/1已分配) AssignMethod={AssignMethod} 明细行数={ItemCount} AnyAssigned={AnyAssigned}",
                rfq.Id,
                rfq.RfqCode,
                rfq.Status,
                rfq.AssignMethod,
                request.Items?.Count ?? 0,
                anyAssigned);

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
            rfq.Items = items.Where(i => !i.IsDeleted).OrderBy(i => i.LineNo).ToList();

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
                rfq.SalesUserName = await _entityLookup.GetUserLoginNameAsync(rfq.SalesUserId);

            if (!string.IsNullOrWhiteSpace(rfq.CreateByUserId))
                rfq.CreateUserName = await _entityLookup.GetUserLoginNameAsync(rfq.CreateByUserId);

            if (canViewCustomer && !string.IsNullOrWhiteSpace(rfq.ContactId))
            {
                var contact = await _entityLookup.GetCustomerContactByIdAsync(rfq.ContactId);
                if (contact != null)
                    rfq.ContactPersonName = contact.Name;
            }

            foreach (var it in rfq.Items)
            {
                it.AssignedPurchaserName1 = await _entityLookup.GetUserLoginNameAsync(it.AssignedPurchaserUserId1);
                it.AssignedPurchaserName2 = await _entityLookup.GetUserLoginNameAsync(it.AssignedPurchaserUserId2);
            }

            if (!string.IsNullOrWhiteSpace(viewerUserId) && !canViewCustomer)
                MaskRfqCustomerFieldsForViewer(rfq);

            var createByUserIdForTags = rfq.CreateByUserId;
            var salesUserIdForTags = rfq.SalesUserId;

            if (!string.IsNullOrWhiteSpace(viewerUserId))
            {
                var s = await _rbacService.GetUserPermissionSummaryAsync(viewerUserId.Trim());
                if (SaleSensitiveFieldMask521.ShouldMask(s))
                {
                    rfq.SalesUserId = null;
                    rfq.SalesUserName = null;
                }
            }

            if (!string.IsNullOrWhiteSpace(viewerUserId))
            {
                rfq.CanViewRfqTags = await _dataPermissionService.CanViewRfqTagsAsync(
                    viewerUserId, createByUserIdForTags, salesUserIdForTags);
                rfq.CanEditRfqTags = await _dataPermissionService.CanEditRfqTagsAsync(
                    viewerUserId, createByUserIdForTags, salesUserIdForTags);
                if (rfq.CanViewRfqTags)
                    rfq.Tags = (await _rfqTagService.GetTagsForRfqAsync(id, viewerUserId)).ToList();
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

        public async Task<RFQListPagedResult> GetPagedAsync(RFQQueryRequest request)
        {
            var canViewCustomerInList = string.IsNullOrWhiteSpace(request.CurrentUserId)
                || await UserCanViewCustomerInRfqContextAsync(request.CurrentUserId!);
            var effectiveCustomerIdFilter = canViewCustomerInList ? request.CustomerId : null;

            var queryReq = new RFQQueryRequest
            {
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
                Keyword = request.Keyword,
                Status = request.Status,
                CustomerId = effectiveCustomerIdFilter,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CurrentUserId = request.CurrentUserId,
                TagIds = request.TagIds
            };

            var page = await _rfqMainListQuery.GetPagedWithAggregatesAsync(queryReq, default);

            var customerIds = page.Items.Where(r => r.CustomerId != null).Select(r => r.CustomerId!).Distinct().ToList();
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

            var listItems = page.Items.Select(r =>
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
                    SalesUserName = EntityLookupService.FormatUserLoginName(salesUser),
                    CreateByUserId = r.CreateByUserId,
                    CreateUserName = EntityLookupService.FormatUserLoginName(createUser)
                };
            }).ToList();

            if (!canViewCustomerInList)
            {
                foreach (var it in listItems)
                    MaskRfqListItemCustomerFields(it);
            }

            if (!string.IsNullOrWhiteSpace(request.CurrentUserId))
            {
                var s = await _rbacService.GetUserPermissionSummaryAsync(request.CurrentUserId.Trim());
                if (SaleSensitiveFieldMask521.ShouldMask(s))
                {
                    foreach (var it in listItems)
                    {
                        it.SalesUserId = null;
                        it.SalesUserName = null;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(request.CurrentUserId) && listItems.Count > 0)
            {
                var tagPermissionRows = page.Items.Select(r => (r.Id, r.CreateByUserId, r.SalesUserId));
                var tagMap = await _rfqTagService.GetTagsForRfqIdsAsync(
                    listItems.Select(i => i.Id),
                    request.CurrentUserId,
                    tagPermissionRows);
                foreach (var it in listItems)
                {
                    if (tagMap.TryGetValue(it.Id, out var tags))
                        it.Tags = tags.ToList();
                }
            }

            return new RFQListPagedResult
            {
                Items = listItems,
                TotalCount = page.TotalCount,
                PageIndex = page.PageIndex,
                PageSize = page.PageSize,
                Aggregates = page.Aggregates
            };
        }

        public async Task<PagedResult<RFQItemListItem>> GetPagedItemsAsync(RFQItemQueryRequest request)
        {
            var canViewCustomerInList = string.IsNullOrWhiteSpace(request.CurrentUserId)
                || await UserCanViewCustomerInRfqContextAsync(request.CurrentUserId!);
            var itemReq = new RFQItemQueryRequest
            {
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CustomerKeyword = canViewCustomerInList ? request.CustomerKeyword : null,
                MaterialModel = request.MaterialModel,
                SalesUserId = request.SalesUserId,
                SalesUserKeyword = request.SalesUserKeyword,
                PurchaserUserId = request.PurchaserUserId,
                HasQuotesOnly = request.HasQuotesOnly,
                Status = request.Status,
                RfqCode = request.RfqCode,
                CurrentUserId = request.CurrentUserId,
                CanViewCustomerInList = canViewCustomerInList
            };

            var result = await _rfqItemListQuery.GetPagedAsync(itemReq, default);
            var pagedItems = result.Items.ToList();

            if (!canViewCustomerInList)
            {
                foreach (var r in pagedItems)
                    MaskRfqItemListRowCustomerFields(r);
            }

            // 需求明细列表：采购方向仍展示主表业务员，便于询价协同（不对 SalesUserId/Name 做 §5.2.1 脱敏）

            return new PagedResult<RFQItemListItem>
            {
                Items = pagedItems,
                TotalCount = result.TotalCount,
                PageIndex = result.PageIndex,
                PageSize = result.PageSize,
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

            List<RFQItem>? deletedLines = null;
            if (request.Items != null && request.Items.Count > 0)
            {
                foreach (var line in request.Items)
                {
                    if (string.IsNullOrWhiteSpace(line.Mpn))
                        throw new ArgumentException("需求明细中的物料型号(MPN)不能为空");
                    if (string.IsNullOrWhiteSpace(NormalizeLineString(line.Brand)))
                        throw new ArgumentException("需求明细中的品牌不能为空");
                }

                var sync = await SyncRfqItemsOnUpdateAsync(rfq, id, request.Items, actingUserId);
                deletedLines = sync.Deleted;

                if (sync.Inserted.Count > 0)
                {
                    rfq.AssignMethod = ResolveAssignMethod(rfq.AssignMethod);
                    if (rfq.Status == 0)
                        rfq.Status = 1;
                }

                var activeCount = (await _itemRepo.FindAsync(i => i.RfqId == id)).Count(i => !i.IsDeleted);
                rfq.ItemCount = activeCount;
            }

            await _rfqRepo.UpdateAsync(rfq);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();

            if (deletedLines is { Count: > 0 })
            {
                await AppendRfqItemDeleteOperationLogsAsync(
                    rfq,
                    deletedLines,
                    actingUserId,
                    OperationLogActionTypes.RfqItemDelete,
                    $"编辑需求 {rfq.RfqCode} 时删除明细行");
            }

            return rfq;
        }

        // ─── Delete ──────────────────────────────────────────────────────────────
        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID不能为空");
            var rfq = await _rfqRepo.GetByIdAsync(id);
            if (rfq == null) throw new InvalidOperationException($"需求 {id} 不存在");

            // 级联删除明细
            var items = (await _itemRepo.FindAsync(i => i.RfqId == id)).ToList();
            foreach (var item in items)
                await _itemRepo.DeleteAsync(item.Id);

            await _rfqRepo.DeleteAsync(id);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();

            await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
            {
                BizType = BusinessLogTypes.Rfq,
                RecordId = rfq.Id,
                RecordCode = rfq.RfqCode,
                EntityDisplayName = DeleteLogEntityNames.Rfq,
                ExtraDetail = $"明细行数={items.Count}"
            });
        }

        // ─── Status ──────────────────────────────────────────────────────────────
        public async Task UpdateStatusAsync(string id, short status, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID不能为空");
            var rfq = await _rfqRepo.GetByIdAsync(id);
            if (rfq == null) throw new InvalidOperationException($"需求 {id} 不存在");
            // 历史 6（旧「已关闭」）已废弃，统一为 7（与迁移脚本及前端筛选一致）
            if (status == (short)RfqMainStatus.LegacyObsoleteClosed)
                status = (short)RfqMainStatus.Closed;
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

            rfq.AssignMethod = RfqAssignMethodCodes.DesignatedPurchaser;
            if (rfq.Status == 0)
                rfq.Status = 1;
            rfq.ModifyTime = DateTime.UtcNow;
            rfq.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
            await _rfqRepo.UpdateAsync(rfq);

            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();

            return await GetByIdAsync(rfqId) ?? rfq;
        }

        /// <inheritdoc />
        public async Task<RFQItem> MarkNoQuoteAsync(string itemId, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                throw new ArgumentException("明细ID不能为空");

            var id = itemId.Trim();
            var item = await _itemRepo.GetByIdAsync(id);
            if (item == null || item.IsDeleted)
                throw new InvalidOperationException("需求明细不存在");

            if (item.Status != (short)RfqItemStatus.Pending)
                throw new InvalidOperationException("仅待报价状态可标记查无报价");

            var quotes = await _quoteRepo.FindAsync(q => q.RFQItemId == id);
            if (quotes.Any(q => !q.IsDeleted))
                throw new InvalidOperationException("该明细已有报价记录，无法标记查无报价");

            var rfq = await _rfqRepo.GetByIdAsync(item.RfqId);
            if (rfq == null)
                throw new InvalidOperationException("关联需求不存在");
            if (rfq.Status == (short)RfqMainStatus.Closed || rfq.Status == (short)RfqMainStatus.Cancelled)
                throw new InvalidOperationException("需求已关闭或已取消，无法操作");

            var actorId = ActingUserIdNormalizer.Normalize(actingUserId);
            if (string.IsNullOrEmpty(actorId))
                throw new UnauthorizedAccessException("未登录或无法识别当前用户");

            if (!await _dataPermissionService.CanAccessRFQAsync(actorId, rfq))
                throw new UnauthorizedAccessException("无权限操作该需求明细");

            var summary = await _rbacService.GetUserPermissionSummaryAsync(actorId);
            if (!RfqItemQuoteAccessRules.CanQuote(summary, item, actorId))
                throw new UnauthorizedAccessException("无权标记该需求明细为查无报价");

            item.Status = (short)RfqItemStatus.NoQuoteFound;
            item.ModifyTime = DateTime.UtcNow;
            await _itemRepo.UpdateAsync(item);
            if (_unitOfWork != null)
                await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "需求明细已标记查无报价。RFQItemId={ItemId} RfqId={RfqId} Status={Status}",
                id, item.RfqId, item.Status);

            return item;
        }

        /// <inheritdoc />
        public async Task<RfqCloseRecordListItem> CloseRfqAsync(string rfqId, CloseRfqRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(rfqId))
                throw new ArgumentException("ID不能为空");
            if (request == null || string.IsNullOrWhiteSpace(request.CloseReason))
                throw new ArgumentException("请填写关闭原因");

            var closeType = request.CloseType;
            if (closeType is not (1 or 2 or 3 or 9))
                throw new ArgumentException("关闭类型无效");

            var id = rfqId.Trim();
            var rfq = await _rfqRepo.GetByIdAsync(id);
            if (rfq == null)
                throw new InvalidOperationException("需求不存在");

            if (rfq.Status == (short)RfqMainStatus.Closed || rfq.Status == (short)RfqMainStatus.Cancelled)
                throw new InvalidOperationException("需求已关闭或已取消，无法重复关闭");

            var actorId = ActingUserIdNormalizer.Normalize(actingUserId);
            var reason = request.CloseReason.Trim();
            var record = new RfqCloseRecord
            {
                RfqId = id,
                CloseType = closeType,
                CloseReason = reason,
                Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim(),
                ClosedByUserId = actorId,
                CreateTime = DateTime.UtcNow,
            };

            rfq.Status = closeType == 2
                ? (short)RfqMainStatus.Cancelled
                : (short)RfqMainStatus.Closed;
            rfq.ModifyTime = DateTime.UtcNow;
            rfq.ModifyByUserId = actorId;

            await _closeRecordRepo.AddAsync(record);
            await _rfqRepo.UpdateAsync(rfq);
            if (_unitOfWork != null)
                await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "需求已关闭。RfqId={RfqId} CloseType={CloseType} NewStatus={Status} RecordId={RecordId}",
                id, closeType, rfq.Status, record.Id);

            return await MapCloseRecordAsync(record);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<RfqCloseRecordListItem>> GetCloseRecordsAsync(string rfqId)
        {
            if (string.IsNullOrWhiteSpace(rfqId))
                return Array.Empty<RfqCloseRecordListItem>();

            var id = rfqId.Trim();
            var records = (await _closeRecordRepo.FindAsync(r => r.RfqId == id))
                .OrderByDescending(r => r.CreateTime)
                .ToList();

            var result = new List<RfqCloseRecordListItem>(records.Count);
            foreach (var record in records)
                result.Add(await MapCloseRecordAsync(record));
            return result;
        }

        private async Task<RfqCloseRecordListItem> MapCloseRecordAsync(RfqCloseRecord record)
        {
            var closedByName = await _entityLookup.GetUserDisplayNameAsync(record.ClosedByUserId);
            var closedAt = record.CreateTime;
            return new RfqCloseRecordListItem
            {
                Id = record.Id,
                RfqId = record.RfqId,
                CloseType = record.CloseType,
                CloseReason = record.CloseReason,
                Reason = record.CloseReason,
                ClosedBy = record.ClosedByUserId,
                ClosedByName = closedByName,
                OperatorName = closedByName,
                ClosedAt = closedAt,
                CreatedAt = closedAt,
                Remark = record.Remark,
            };
        }

        private sealed record RfqItemSyncResult(
            List<RFQItem> Inserted,
            List<RFQItem> Updated,
            List<RFQItem> Deleted);

        private async Task<RfqItemSyncResult> SyncRfqItemsOnUpdateAsync(
            RFQ rfq,
            string rfqId,
            List<CreateRFQItemRequest> requestItems,
            string? actingUserId)
        {
            var existingActive = (await _itemRepo.FindAsync(i => i.RfqId == rfqId))
                .Where(i => !i.IsDeleted)
                .ToDictionary(i => i.Id, StringComparer.OrdinalIgnoreCase);

            var inserted = new List<RFQItem>();
            var updated = new List<RFQItem>();
            var keptIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var newItemRequests = new List<(CreateRFQItemRequest Req, int Index)>();

            for (var i = 0; i < requestItems.Count; i++)
            {
                var itemReq = requestItems[i];
                var reqId = itemReq.Id?.Trim();
                if (!string.IsNullOrEmpty(reqId))
                {
                    if (!existingActive.TryGetValue(reqId, out var existing))
                        throw new InvalidOperationException($"需求明细 {reqId} 不存在或已删除");

                    keptIds.Add(reqId);
                    await ApplyRfqItemFromRequestAsync(existing, itemReq, i);
                    existing.ModifyTime = DateTime.UtcNow;
                    await _itemRepo.UpdateAsync(existing);
                    updated.Add(existing);
                }
                else
                {
                    newItemRequests.Add((itemReq, i));
                }
            }

            string? purchaser1 = null;
            string? purchaser2 = null;
            if (newItemRequests.Count > 0)
            {
                var assignMethod = ResolveAssignMethod(rfq.AssignMethod);
                var existingBrandAssignees = BuildExistingBrandAssignees(existingActive.Values);
                var assignmentContext = BuildAssignmentContext(
                    rfqId,
                    rfq.RfqCode,
                    RfqAssignmentTrigger.AddItems,
                    newItemRequests.Select(x => (
                        LineNo: x.Req.LineNo > 0 ? x.Req.LineNo : x.Index + 1,
                        Brand: NormalizeLineString(x.Req.Brand),
                        BrandId: x.Req.BrandId)),
                    existingBrandAssignees);

                var assignmentOutcome = await _purchaserAssignmentOrchestrator.AssignAsync(
                    assignMethod,
                    assignmentContext);

                for (var j = 0; j < newItemRequests.Count; j++)
                {
                    var (itemReq, index) = newItemRequests[j];
                    var assigned = assignmentOutcome.Assignments[j];
                    purchaser1 = assigned.PurchaserUserId1;
                    purchaser2 = assigned.PurchaserUserId2;

                    var item = new RFQItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        RfqId = rfqId,
                        LineNo = itemReq.LineNo > 0 ? itemReq.LineNo : index + 1,
                        Status = 0,
                        AssignedPurchaserUserId1 = purchaser1,
                        AssignedPurchaserUserId2 = purchaser2,
                        CreateTime = DateTime.UtcNow
                    };
                    await ApplyRfqItemFromRequestAsync(item, itemReq, index);
                    await _itemRepo.AddAsync(item);
                    inserted.Add(item);
                }

                _logger.LogInformation(
                    "【需求-采购员分配】编辑需求新增明细完成：RfqId={RfqId} RfqCode={RfqCode} 新增行数={ItemCount} AssignMethod={AssignMethod}",
                    rfq.Id,
                    rfq.RfqCode,
                    newItemRequests.Count,
                    assignMethod);
            }

            var deleted = new List<RFQItem>();
            foreach (var existing in existingActive.Values)
            {
                if (keptIds.Contains(existing.Id))
                    continue;

                existing.IsDeleted = true;
                existing.ModifyTime = DateTime.UtcNow;
                await _itemRepo.UpdateAsync(existing);
                deleted.Add(existing);
            }

            return new RfqItemSyncResult(inserted, updated, deleted);
        }

        private async Task ApplyRfqItemFromRequestAsync(RFQItem target, CreateRFQItemRequest itemReq, int index)
        {
            target.LineNo = itemReq.LineNo > 0 ? itemReq.LineNo : index + 1;
            target.CustomerMpn = string.IsNullOrWhiteSpace(itemReq.CustomerMpn) ? null : itemReq.CustomerMpn.Trim();
            target.Mpn = NormalizeLineString(itemReq.Mpn);
            target.CustomerBrand = NormalizeLineString(itemReq.CustomerBrand);
            target.TargetPrice = itemReq.TargetPrice;
            target.PriceCurrency = itemReq.PriceCurrency;
            target.Quantity = itemReq.Quantity;
            target.ProductionDate = itemReq.ProductionDate;
            target.ExpiryDate = PostgreSqlDateTime.ToUtc(itemReq.ExpiryDate);
            target.MinPackageQty = itemReq.MinPackageQty;
            target.Moq = itemReq.Moq;
            target.Alternatives = itemReq.Alternatives;
            target.Remark = itemReq.Remark;
            await ApplyRfqItemBrandAsync(target, itemReq);
        }

        private async Task ApplyRfqItemBrandAsync(RFQItem target, CreateRFQItemRequest itemReq)
        {
            if (!itemReq.BrandId.HasValue || itemReq.BrandId.Value <= 0)
                throw new ArgumentException("供应品牌未选择");

            var brand = await _bizBrandService.GetByIdAsync(itemReq.BrandId.Value);
            if (brand == null)
                throw new ArgumentException($"品牌不存在（ID={itemReq.BrandId}）");

            target.BrandId = brand.Id;
            target.Brand = NormalizeLineString(
                brand.StandardBrand ?? brand.BrandEName ?? brand.BrandCName);
        }

        private async Task AppendRfqItemDeleteOperationLogsAsync(
            RFQ rfq,
            IReadOnlyList<RFQItem> deletedItems,
            string? actingUserId,
            string actionType,
            string descriptionPrefix)
        {
            var (actorId, actorName) = await ResolveActorAsync(actingUserId);
            foreach (var d in deletedItems)
            {
                var lineCode = $"{rfq.RfqCode}-L{d.LineNo}";
                await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
                {
                    BizType = BusinessLogTypes.RfqItem,
                    RecordId = d.Id,
                    RecordCode = lineCode,
                    EntityDisplayName = DeleteLogEntityNames.RfqItem,
                    ActionTypeOverride = actionType,
                    OperatorUserId = actorId,
                    OperatorUserName = actorName,
                    OperationDescOverride = $"{descriptionPrefix} {lineCode}"
                });
            }
        }

        private async Task<(string? UserId, string UserName)> ResolveActorAsync(string? actingUserId)
        {
            var id = ActingUserIdNormalizer.Normalize(actingUserId);
            if (string.IsNullOrEmpty(id))
                return (null, "系统");
            var user = await _userService.GetByIdAsync(id);
            return (id, string.IsNullOrWhiteSpace(user?.UserName) ? id : user!.UserName!.Trim());
        }

        private static short ResolveAssignMethod(short assignMethod) =>
            assignMethod > 0 ? assignMethod : RfqAssignMethodCodes.ItemRoundRobin;

        private static Dictionary<string, (string? PurchaserUserId1, string? PurchaserUserId2)> BuildExistingBrandAssignees(
            IEnumerable<RFQItem> existingItems) =>
            existingItems
                .Where(i => !string.IsNullOrWhiteSpace(i.AssignedPurchaserUserId1))
                .GroupBy(i => RfqAssignmentBrandKey.Resolve(i.BrandId, i.Brand))
                .ToDictionary(
                    g => g.Key,
                    g => (g.First().AssignedPurchaserUserId1, g.First().AssignedPurchaserUserId2));

        private static RfqAssignmentContext BuildAssignmentContext(
            string rfqId,
            string? rfqCode,
            RfqAssignmentTrigger trigger,
            IEnumerable<(int LineNo, string Brand, long? BrandId)> lines,
            IReadOnlyDictionary<string, (string? PurchaserUserId1, string? PurchaserUserId2)>? existingBrandAssignees = null) =>
            new()
            {
                RfqId = rfqId,
                RfqCode = rfqCode,
                Trigger = trigger,
                ExistingBrandAssignees = existingBrandAssignees,
                Items = lines.Select(x => new RfqItemAssignmentInput
                {
                    ItemKey = x.LineNo.ToString(),
                    LineNo = x.LineNo,
                    Brand = x.Brand,
                    BrandId = x.BrandId
                }).ToList()
            };
    }
}
