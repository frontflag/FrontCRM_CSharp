using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
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
        private readonly IRepository<Quote> _quoteRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRbacService _rbacService;
        private readonly IPurchaseQuoterPoolService _purchaseQuoterPoolService;
        private readonly IRfqMainListQuery _rfqMainListQuery;
        private readonly IRfqItemListQuery _rfqItemListQuery;
        private readonly ILogger<RFQService> _logger;
        private readonly ILogOperationAppendService _logOperationAppend;
        private readonly IBizBrandService _bizBrandService;

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
            IRepository<Quote> quoteRepo,
            IRepository<User> userRepo,
            IRbacService rbacService,
            IPurchaseQuoterPoolService purchaseQuoterPoolService,
            IRfqMainListQuery rfqMainListQuery,
            IRfqItemListQuery rfqItemListQuery,
            ILogger<RFQService> logger,
            ILogOperationAppendService logOperationAppend,
            IBizBrandService bizBrandService)
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
            _quoteRepo = quoteRepo;
            _userRepo = userRepo;
            _rbacService = rbacService;
            _purchaseQuoterPoolService = purchaseQuoterPoolService;
            _rfqMainListQuery = rfqMainListQuery;
            _rfqItemListQuery = rfqItemListQuery;
            _logger = logger;
            _logOperationAppend = logOperationAppend;
            _bizBrandService = bizBrandService;
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

            // 每个需求从报价员池取连续 N 名采购员，写入该需求下全部明细；游标全局 +N
            var (purchaser1, purchaser2) = await TakeNextRoundRobinPurchasersAsync();

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
                    await ApplyRfqItemFromRequestAsync(item, itemReq, i);
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
                CurrentUserId = request.CurrentUserId
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
                    rfq.AssignMethod = 2;
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
        /// 从报价员池取连续 N 名采购员（同一需求下所有明细相同）；游标 +N。
        /// 池为空时返回 (null,null) 且不推进游标。
        /// </summary>
        private async Task<(string? UserId1, string? UserId2)> TakeNextRoundRobinPurchasersAsync()
        {
            var pool = await _purchaseQuoterPoolService.GetOrderedActivePoolUserIdsAsync();
            var n = pool.Count;
            if (n == 0)
            {
                _logger.LogWarning(
                    "【需求-采购员轮询】报价员池为空，跳过分配。请在「采购参数 → 报价员池」中配置可参与轮询的采购员。");
                return (null, null);
            }

            var assignCount = await _purchaseQuoterPoolService.GetAssigneeCountAsync();
            if (assignCount is not (1 or 2))
                assignCount = 2;

            var cursor = await GetRoundRobinCursorAsync();
            var ids = new List<string>(assignCount);
            for (var i = 0; i < assignCount; i++)
                ids.Add(pool[(cursor + i) % n]);

            await SaveRoundRobinCursorAsync(cursor + assignCount);

            var a1 = ids[0];
            var a2 = assignCount >= 2 ? ids[1] : null;
            _logger.LogInformation(
                "【需求-采购员轮询】本笔取值：池人数={PoolCount} 分配人数={AssignCount} CursorBefore={CursorBefore} " +
                "UserId1={UserId1} UserId2={UserId2} CursorAfter={CursorAfter}",
                n,
                assignCount,
                cursor,
                a1,
                a2 ?? "(null)",
                cursor + assignCount);

            return (a1, a2);
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
                _logger.LogInformation(
                    "【需求-采购员轮询】编辑需求新增明细，取轮询对：RfqId={RfqId} RfqCode={RfqCode} 新增行数={ItemCount}",
                    rfq.Id,
                    rfq.RfqCode,
                    newItemRequests.Count);
                (purchaser1, purchaser2) = await TakeNextRoundRobinPurchasersAsync();
            }

            foreach (var (itemReq, index) in newItemRequests)
            {
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

            if (purchaser1 != null && inserted.Count > 0)
            {
                _logger.LogInformation(
                    "【需求-采购员轮询】编辑需求新增明细已写入：RfqId={RfqId} AssignedPurchaserUserId1={P1} AssignedPurchaserUserId2={P2}",
                    rfq.Id,
                    purchaser1 ?? "(null)",
                    purchaser2 ?? "(null)");
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
    }
}
