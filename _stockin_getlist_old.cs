        public async Task<IReadOnlyList<StockInListItemDto>> GetListAsync(StockInQueryRequest? request = null)
        {
            // 列表按创建时间降序（与业务列表「创建日期」口径一致）
            const short transferStockInType = 3;
            var raw = (await _stockInRepository.GetAllAsync())
                .Where(x => x.StockInType != transferStockInType)
                .OrderByDescending(x => x.CreateTime)
                .ThenByDescending(x => x.Id)
                .ToList();

            if (raw.Count == 0)
                return Array.Empty<StockInListItemDto>();

            var stockInIds = raw.Select(x => x.Id).ToList();
            var vendorIds = raw.Select(x => x.VendorId).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            var stockInItems = (await _stockInItemRepository.FindAsync(x => stockInIds.Contains(x.StockInId))).ToList();
            var stockInItemsMap = stockInItems
                .GroupBy(x => x.StockInId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var allExtends = (await _stockInItemExtendRepository.FindAsync(e => stockInIds.Contains(e.StockInId))).ToList();
            var primaryByStockIn = StockInItemExtendPrimaryPicker.PrimaryByStockInId(allExtends);

            var poLineIds = allExtends
                .Select(x => x.PurchaseOrderItemId)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var poItemsForStockIns = poLineIds.Count == 0
                ? new List<PurchaseOrderItem>()
                : (await _purchaseOrderItemRepository.FindAsync(x => poLineIds.Contains(x.Id))).ToList();
            var poLineEntityById = poItemsForStockIns
                .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                .ToDictionary(x => x.Id!.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

            var sellOnlyIds = allExtends
                .Where(e => string.IsNullOrWhiteSpace(e.PurchaseOrderItemId) && !string.IsNullOrWhiteSpace(e.SellOrderItemId))
                .Select(e => e.SellOrderItemId!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (sellOnlyIds.Count > 0)
            {
                var extraPoLines = (await _purchaseOrderItemRepository.FindAsync(x => x.SellOrderItemId != null && sellOnlyIds.Contains(x.SellOrderItemId)))
                    .ToList();
                foreach (var pl in extraPoLines)
                {
                    if (string.IsNullOrWhiteSpace(pl.Id)) continue;
                    var k = pl.Id.Trim();
                    if (!poLineEntityById.ContainsKey(k))
                        poLineEntityById[k] = pl;
                }
            }

            var poIdsForDisplay = poLineEntityById.Values
                .Select(v => v.PurchaseOrderId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var poDict = poIdsForDisplay.Count == 0
                ? new Dictionary<string, PurchaseOrder>(StringComparer.OrdinalIgnoreCase)
                : (await _purchaseOrderRepository.FindAsync(p => poIdsForDisplay.Contains(p.Id)))
                    .ToDictionary(p => p.Id.Trim(), p => p, StringComparer.OrdinalIgnoreCase);

            var poItemsMap = poIdsForDisplay.Count == 0
                ? new Dictionary<string, List<PurchaseOrderItem>>(StringComparer.OrdinalIgnoreCase)
                : (await _purchaseOrderItemRepository.FindAsync(x => poIdsForDisplay.Contains(x.PurchaseOrderId)))
                    .GroupBy(x => x.PurchaseOrderId, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            Dictionary<string, QCInfo> qcByStockInId;
            try
            {
                qcByStockInId = (await _qcRepository.FindAsync(q => q.StockInId != null && stockInIds.Contains(q.StockInId!)))
                    .GroupBy(q => q.StockInId!, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            }
            catch (Exception ex) when (PostgreSqlExceptionHelper.IsUndefinedObject(ex))
            {
                _logger.LogWarning(
                    ex,
                    "质检主表批量查询失败（多为 qcinfo 缺少 StockInPlanDate 列，请执行迁移 20260623100000_QcInfoStockInPlanDate 或 scripts/add_qc_stock_in_plan_date_postgresql.sql）；入库单列表将不展示关联质检信息。");
                qcByStockInId = new Dictionary<string, QCInfo>(StringComparer.Ordinal);
            }

            var venDict = vendorIds.Count == 0
                ? new Dictionary<string, VendorInfo>()
                : (await _vendorRepository.FindAsync(v => vendorIds.Contains(v.Id))).ToDictionary(v => v.Id);

            Dictionary<string, MaterialInfo> materialById = new(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var m in await _materialRepository.GetAllAsync())
                {
                    var id = m.Id?.Trim();
                    if (string.IsNullOrEmpty(id) || materialById.ContainsKey(id)) continue;
                    materialById[id] = m;
                }
            }
            catch
            {
                // 物料表不可用时仍返回列表，仅不展示型号/品牌
            }

            var sellOrderItemIds = poItemsMap.Values
                .SelectMany(x => x)
                .Select(x => x.SellOrderItemId)
                .Concat(allExtends.Select(e => e.SellOrderItemId))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var soItems = sellOrderItemIds.Count == 0
                ? new List<SellOrderItem>()
                : (await _sellOrderItemRepository.FindAsync(x => sellOrderItemIds.Contains(x.Id))).ToList();
            var soIdByItemId = soItems
                .Where(x => !string.IsNullOrWhiteSpace(x.SellOrderId))
                .ToDictionary(x => x.Id, x => x.SellOrderId!);
            var soIds = soIdByItemId.Values.Distinct().ToList();
            var soDict = soIds.Count == 0
                ? new Dictionary<string, SellOrder>()
                : (await _sellOrderRepository.FindAsync(x => soIds.Contains(x.Id))).ToDictionary(x => x.Id);

            var allUsers = (await _userService.GetAllAsync()).ToList();
            var userById = allUsers
                .Where(u => !string.IsNullOrWhiteSpace(u.Id))
                .ToDictionary(u => u.Id!.Trim(), u => u, StringComparer.OrdinalIgnoreCase);

            var result = new List<StockInListItemDto>(raw.Count);
            foreach (var s in raw)
            {
                var prim = primaryByStockIn.TryGetValue(s.Id, out var pe) ? pe : null;
                PurchaseOrderItem? headerPoLine = null;
                if (!string.IsNullOrWhiteSpace(prim?.PurchaseOrderItemId) &&
                    poLineEntityById.TryGetValue(prim.PurchaseOrderItemId.Trim(), out var plHeader))
                    headerPoLine = plHeader;
                else if (!string.IsNullOrWhiteSpace(prim?.SellOrderItemId))
                {
                    headerPoLine = poLineEntityById.Values.FirstOrDefault(v =>
                        !string.IsNullOrWhiteSpace(v.SellOrderItemId) &&
                        string.Equals(v.SellOrderItemId.Trim(), prim.SellOrderItemId!.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                var headerPoId = headerPoLine != null && !string.IsNullOrWhiteSpace(headerPoLine.PurchaseOrderId)
                    ? headerPoLine.PurchaseOrderId.Trim()
                    : (string?)null;

                string? sourceDisplay = null;
                if (!string.IsNullOrWhiteSpace(s.SourceCode))
                    sourceDisplay = s.SourceCode.Trim();
                if (string.IsNullOrWhiteSpace(sourceDisplay) && headerPoId != null &&
                    poDict.TryGetValue(headerPoId, out var poForDisp) &&
                    !string.IsNullOrWhiteSpace(poForDisp.PurchaseOrderCode))
                    sourceDisplay = poForDisp.PurchaseOrderCode.Trim();
                if (string.IsNullOrWhiteSpace(sourceDisplay) && qcByStockInId.TryGetValue(s.Id, out var qcLinked))
                    sourceDisplay = qcLinked.QcCode;
                if (string.IsNullOrWhiteSpace(sourceDisplay) && !string.IsNullOrWhiteSpace(s.QcCode))
                    sourceDisplay = s.QcCode.Trim();
                if (string.IsNullOrWhiteSpace(sourceDisplay) && !string.IsNullOrWhiteSpace(prim?.PurchaseOrderItemCode))
                    sourceDisplay = prim.PurchaseOrderItemCode.Trim();

                string? vendorName = null;
                if (!string.IsNullOrWhiteSpace(s.VendorId) && venDict.TryGetValue(s.VendorId!, out var v))
                {
                    vendorName = !string.IsNullOrWhiteSpace(v.OfficialName) ? v.OfficialName
                        : !string.IsNullOrWhiteSpace(v.NickName) ? v.NickName
                        : v.Code;
                }

                var salesOrderCodes = new List<string>();
                if (headerPoLine != null && !string.IsNullOrWhiteSpace(headerPoLine.SellOrderItemId) &&
                    soIdByItemId.TryGetValue(headerPoLine.SellOrderItemId.Trim(), out var soId0) &&
                    soDict.TryGetValue(soId0, out var so0) && !string.IsNullOrWhiteSpace(so0.SellOrderCode))
                    salesOrderCodes.Add(so0.SellOrderCode!);
                if (!string.IsNullOrWhiteSpace(prim?.SellOrderItemId) &&
                    (!headerPoLine?.SellOrderItemId?.Trim().Equals(prim.SellOrderItemId.Trim(), StringComparison.OrdinalIgnoreCase) ?? true) &&
                    soIdByItemId.TryGetValue(prim.SellOrderItemId.Trim(), out var soId1) &&
                    soDict.TryGetValue(soId1, out var so1) && !string.IsNullOrWhiteSpace(so1.SellOrderCode))
                    salesOrderCodes.Add(so1.SellOrderCode!);

                if (headerPoId != null &&
                    poItemsMap.TryGetValue(headerPoId, out var thisPoItems))
                {
                    foreach (var poi in thisPoItems)
                    {
                        if (string.IsNullOrWhiteSpace(poi.SellOrderItemId)) continue;
                        if (!soIdByItemId.TryGetValue(poi.SellOrderItemId.Trim(), out var soId)) continue;
                        if (!soDict.TryGetValue(soId, out var so)) continue;
                        if (string.IsNullOrWhiteSpace(so.SellOrderCode)) continue;
                        salesOrderCodes.Add(so.SellOrderCode);
                    }
                }

                var salesOrderCode = string.Join(", ", salesOrderCodes.Distinct(StringComparer.OrdinalIgnoreCase));

                string? purchaseOrderCode = null;
                if (headerPoId != null &&
                    poDict.TryGetValue(headerPoId, out var poForPoCode) &&
                    !string.IsNullOrWhiteSpace(poForPoCode.PurchaseOrderCode))
                    purchaseOrderCode = poForPoCode.PurchaseOrderCode.Trim();

                string? modelSummary = null;
                string? brandSummary = null;
                if (stockInItemsMap.TryGetValue(s.Id, out var silForDisplay) && silForDisplay.Count > 0)
                {
                    IReadOnlyList<PurchaseOrderItem>? poLinesForS = null;
                    if (headerPoId != null &&
                        poItemsMap.TryGetValue(headerPoId, out var pl0))
                        poLinesForS = pl0;

                    var models = new List<string>();
                    var brands = new List<string>();
                    foreach (var line in silForDisplay)
                    {
                        var mid = line.MaterialId?.Trim();
                        if (string.IsNullOrEmpty(mid)) continue;
                        ResolveStockInLineModelBrand(mid, materialById, poLinesForS, out var m1, out var b1);
                        if (!string.IsNullOrWhiteSpace(m1)) models.Add(m1);
                        if (!string.IsNullOrWhiteSpace(b1)) brands.Add(b1);
                    }

                    if (models.Count > 0)
                        modelSummary = string.Join(", ", models.Distinct(StringComparer.OrdinalIgnoreCase));
                    if (brands.Count > 0)
                        brandSummary = string.Join(", ", brands.Distinct(StringComparer.OrdinalIgnoreCase));
                }

                decimal displayTotalAmount = s.TotalAmount;
                if (displayTotalAmount == 0m && stockInItemsMap.TryGetValue(s.Id, out var silAmt) && silAmt.Count > 0)
                {
                    displayTotalAmount = silAmt.Sum(line =>
                        line.Amount != 0m ? line.Amount : line.Quantity * line.Price);
                    // 历史数据：明细单价为 0 但 MaterialId 为采购行 Id 时，用采购单价回算展示金额
                    if (displayTotalAmount == 0m && headerPoId != null &&
                        poItemsMap.TryGetValue(headerPoId, out var poLinesForAmt) &&
                        poLinesForAmt.Count > 0)
                    {
                        var poById = poLinesForAmt
                            .Where(p => !string.IsNullOrWhiteSpace(p.Id))
                            .ToDictionary(p => p.Id!.Trim(), p => p, StringComparer.OrdinalIgnoreCase);
                        displayTotalAmount = silAmt.Sum(line =>
                        {
                            var mid = line.MaterialId?.Trim();
                            if (!string.IsNullOrEmpty(mid) && poById.TryGetValue(mid, out var poi))
                                return line.Quantity * poi.Cost;
                            return line.Amount != 0m ? line.Amount : line.Quantity * line.Price;
                        });
                    }
                }

                string? createUserName = null;
                if (!string.IsNullOrWhiteSpace(s.CreatedBy) && userById.TryGetValue(s.CreatedBy.Trim(), out var cu))
                    createUserName = EntityLookupService.FormatUserLoginName(cu) ?? s.CreatedBy.Trim();

                short? currencyCode = null;
                if (headerPoLine != null)
                    currencyCode = headerPoLine.Currency;
                else if (stockInItemsMap.TryGetValue(s.Id, out var silCur) && silCur.Count > 0)
                {
                    foreach (var line in silCur)
                    {
                        var midCur = line.MaterialId?.Trim();
                        if (string.IsNullOrEmpty(midCur)) continue;
                        if (poLineEntityById.TryGetValue(midCur, out var plCur))
                        {
                            currencyCode = plCur.Currency;
                            break;
                        }
                    }
                }

                result.Add(new StockInListItemDto
                {
                    Id = s.Id,
                    StockInCode = s.StockInCode,
                    StockInType = s.StockInType,
                    SourceDisplayNo = sourceDisplay,
                    WarehouseId = s.WarehouseId,
                    VendorId = s.VendorId,
                    VendorName = vendorName,
                    PurchaseOrderCode = purchaseOrderCode,
                    SalesOrderCode = string.IsNullOrWhiteSpace(salesOrderCode) ? null : salesOrderCode,
                    MaterialModelSummary = modelSummary,
                    MaterialBrandSummary = brandSummary,
                    StockInDate = s.StockInDate,
                    TotalQuantity = s.TotalQuantity,
                    TotalAmount = displayTotalAmount,
                    CurrencyCode = currencyCode,
                    Status = s.Status,
                    Remark = s.Remark,
                    CreateTime = s.CreateTime,
                    CreateUserName = createUserName
                });
            }

            var modelKeyword = request?.Model?.Trim();
            var vendorKeyword = request?.VendorName?.Trim();
            var poCodeKeyword = request?.PurchaseOrderCode?.Trim();
            var soCodeKeyword = request?.SalesOrderCode?.Trim();
            var stockInCodeKeyword = request?.StockInCode?.Trim();
            var sourceDisplayKeyword = request?.SourceDisplayNo?.Trim();
            var warehouseId = request?.WarehouseId?.Trim();
            var stockInDateStart = request?.StockInDateStart?.Date;
            var stockInDateEnd = request?.StockInDateEnd?.Date;
            var remarkKeyword = request?.Remark?.Trim();

            return result.Where(x =>
            {
                if (!string.IsNullOrWhiteSpace(modelKeyword))
                {
                    var idHit = stockInItemsMap.TryGetValue(x.Id, out var items)
                        && items.Any(i => !string.IsNullOrWhiteSpace(i.MaterialId)
                                          && i.MaterialId.Contains(modelKeyword, StringComparison.OrdinalIgnoreCase));
                    var textHit =
                        (x.MaterialModelSummary?.Contains(modelKeyword, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (x.MaterialBrandSummary?.Contains(modelKeyword, StringComparison.OrdinalIgnoreCase) ?? false);
                    if (!idHit && !textHit) return false;
                }
                if (!string.IsNullOrWhiteSpace(vendorKeyword)
                    && !(x.VendorName?.Contains(vendorKeyword, StringComparison.OrdinalIgnoreCase) ?? false))
                    return false;
                if (!string.IsNullOrWhiteSpace(poCodeKeyword)
                    && !((x.PurchaseOrderCode?.Contains(poCodeKeyword, StringComparison.OrdinalIgnoreCase) ?? false)
                         || (x.SourceDisplayNo?.Contains(poCodeKeyword, StringComparison.OrdinalIgnoreCase) ?? false)))
                    return false;
                if (!string.IsNullOrWhiteSpace(soCodeKeyword)
                    && !(x.SalesOrderCode?.Contains(soCodeKeyword, StringComparison.OrdinalIgnoreCase) ?? false))
                    return false;
                if (!string.IsNullOrWhiteSpace(stockInCodeKeyword)
                    && !(x.StockInCode?.Contains(stockInCodeKeyword, StringComparison.OrdinalIgnoreCase) ?? false))
                    return false;
                if (!string.IsNullOrWhiteSpace(sourceDisplayKeyword)
                    && !(x.SourceDisplayNo?.Contains(sourceDisplayKeyword, StringComparison.OrdinalIgnoreCase) ?? false))
                    return false;
                if (!string.IsNullOrWhiteSpace(warehouseId)
                    && !string.Equals(x.WarehouseId?.Trim(), warehouseId, StringComparison.OrdinalIgnoreCase))
                    return false;
                var stockInDate = x.StockInDate.Date;
                if (stockInDateStart.HasValue && stockInDate < stockInDateStart.Value)
                    return false;
                if (stockInDateEnd.HasValue && stockInDate > stockInDateEnd.Value)
                    return false;
                if (!string.IsNullOrWhiteSpace(remarkKeyword)
                    && !(x.Remark?.Contains(remarkKeyword, StringComparison.OrdinalIgnoreCase) ?? false))
                    return false;
                return true;
            }).ToList();
        }

