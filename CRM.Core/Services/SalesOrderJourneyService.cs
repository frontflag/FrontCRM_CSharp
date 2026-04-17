using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Quote;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Sales;

namespace CRM.Core.Services
{
    public class SalesOrderJourneyService : ISalesOrderJourneyService
    {
        private readonly IRepository<SellOrder> _soRepo;
        private readonly IRepository<SellOrderItem> _soItemRepo;
        private readonly IRepository<Quote> _quoteRepo;
        private readonly IRepository<RFQ> _rfqRepo;
        private readonly IRepository<PurchaseRequisition> _prRepo;
        private readonly IRepository<PurchaseOrder> _poRepo;
        private readonly IRepository<PurchaseOrderItem> _poItemRepo;
        private readonly IRepository<StockOutRequest> _sorRepo;
        private readonly IRepository<StockInNotify> _arrivalRepo;
        private readonly IRepository<QCInfo> _qcRepo;
        private readonly IRepository<StockIn> _stockInRepo;
        private readonly IRepository<StockInItemExtend> _stockInItemExtendRepo;
        private readonly IRepository<StockOut> _stockOutRepo;
        private readonly IRepository<FinancePayment> _payRepo;
        private readonly IRepository<FinancePaymentItem> _payItemRepo;
        private readonly IRepository<FinanceReceipt> _receiptRepo;
        private readonly IRepository<FinanceReceiptItem> _receiptItemRepo;
        private readonly IRepository<FinancePurchaseInvoice> _pinvRepo;
        private readonly IRepository<FinancePurchaseInvoiceItem> _pinvItemRepo;
        private readonly IRepository<FinanceSellInvoice> _sinvRepo;
        private readonly IRepository<SellInvoiceItem> _sinvItemRepo;
        private readonly IUserService _userService;
        private readonly IDataPermissionService _permission;

        public SalesOrderJourneyService(
            IRepository<SellOrder> soRepo,
            IRepository<SellOrderItem> soItemRepo,
            IRepository<Quote> quoteRepo,
            IRepository<RFQ> rfqRepo,
            IRepository<PurchaseRequisition> prRepo,
            IRepository<PurchaseOrder> poRepo,
            IRepository<PurchaseOrderItem> poItemRepo,
            IRepository<StockOutRequest> sorRepo,
            IRepository<StockInNotify> arrivalRepo,
            IRepository<QCInfo> qcRepo,
            IRepository<StockIn> stockInRepo,
            IRepository<StockInItemExtend> stockInItemExtendRepo,
            IRepository<StockOut> stockOutRepo,
            IRepository<FinancePayment> payRepo,
            IRepository<FinancePaymentItem> payItemRepo,
            IRepository<FinanceReceipt> receiptRepo,
            IRepository<FinanceReceiptItem> receiptItemRepo,
            IRepository<FinancePurchaseInvoice> pinvRepo,
            IRepository<FinancePurchaseInvoiceItem> pinvItemRepo,
            IRepository<FinanceSellInvoice> sinvRepo,
            IRepository<SellInvoiceItem> sinvItemRepo,
            IUserService userService,
            IDataPermissionService permission)
        {
            _soRepo = soRepo;
            _soItemRepo = soItemRepo;
            _quoteRepo = quoteRepo;
            _rfqRepo = rfqRepo;
            _prRepo = prRepo;
            _poRepo = poRepo;
            _poItemRepo = poItemRepo;
            _sorRepo = sorRepo;
            _arrivalRepo = arrivalRepo;
            _qcRepo = qcRepo;
            _stockInRepo = stockInRepo;
            _stockInItemExtendRepo = stockInItemExtendRepo;
            _stockOutRepo = stockOutRepo;
            _payRepo = payRepo;
            _payItemRepo = payItemRepo;
            _receiptRepo = receiptRepo;
            _receiptItemRepo = receiptItemRepo;
            _pinvRepo = pinvRepo;
            _pinvItemRepo = pinvItemRepo;
            _sinvRepo = sinvRepo;
            _sinvItemRepo = sinvItemRepo;
            _userService = userService;
            _permission = permission;
        }

        private async Task<string?> ResolveUserNameAsync(long? userId)
        {
            if (userId == null) return null;
            var u = await _userService.GetByIdAsync(userId.Value.ToString());
            return u?.RealName ?? u?.UserName;
        }

        private static string NodeId(string type, string bizId) => $"{type}:{bizId}";

        public async Task<SalesOrderJourneyResponseDto> GetJourneyAsync(string sellOrderId, string? currentUserId = null)
        {
            if (string.IsNullOrWhiteSpace(sellOrderId))
                return new SalesOrderJourneyResponseDto();

            var so = await _soRepo.GetByIdAsync(sellOrderId);
            if (so == null) return new SalesOrderJourneyResponseDto();

            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                var ok = await _permission.CanAccessSalesOrderAsync(currentUserId, so);
                if (!ok) throw new UnauthorizedAccessException("无权限访问该销售订单");
            }

            var nodes = new Dictionary<string, SalesOrderJourneyNodeDto>();
            var edges = new List<SalesOrderJourneyEdgeDto>();

            void AddNode(SalesOrderJourneyNodeDto n)
            {
                if (string.IsNullOrWhiteSpace(n.Id)) return;
                nodes[n.Id] = n;
            }

            void AddEdge(string source, string target, string? id = null)
            {
                if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target)) return;
                edges.Add(new SalesOrderJourneyEdgeDto
                {
                    Id = id ?? $"{source}->{target}",
                    Source = source,
                    Target = target
                });
            }

            // SalesOrder（当前节点）
            var soNodeId = NodeId("SALES_ORDER", so.Id);
            AddNode(new SalesOrderJourneyNodeDto
            {
                Id = soNodeId,
                Type = "SALES_ORDER",
                Title = $"销售订单 {so.SellOrderCode}",
                StatusText = ((short)so.Status).ToString(),
                CreateDate = so.CreateTime.ToString("yyyy-MM-dd"),
                CreatorName = await ResolveUserNameAsync(so.CreateUserId) ?? "—",
                Amount = so.Total,
                Quantity = so.ItemRows,
                IsCurrent = true
            });

            // 上游：Quote / RFQ（从 SellOrderItem.QuoteId 推导）
            var soItems = (await _soItemRepo.FindAsync(i => i.SellOrderId == so.Id)).ToList();
            var quoteIds = soItems.Select(i => i.QuoteId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList()!;
            var quotes = quoteIds.Count == 0 ? new List<Quote>() : (await _quoteRepo.FindAsync(q => quoteIds.Contains(q.Id))).ToList();
            foreach (var q in quotes)
            {
                var qNodeId = NodeId("QUOTE", q.Id);
                AddNode(new SalesOrderJourneyNodeDto
                {
                    Id = qNodeId,
                    Type = "QUOTE",
                    Title = $"报价 {q.QuoteCode}",
                    StatusText = q.Status.ToString(),
                    CreateDate = q.CreateTime.ToString("yyyy-MM-dd"),
                    CreatorName = await ResolveUserNameAsync(q.CreateUserId) ?? "—"
                });
                AddEdge(qNodeId, soNodeId);
            }

            var rfqIds = quotes.Select(x => x.RFQId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList()!;
            var rfqs = rfqIds.Count == 0 ? new List<RFQ>() : (await _rfqRepo.FindAsync(r => rfqIds.Contains(r.Id))).ToList();
            foreach (var r in rfqs)
            {
                var rNodeId = NodeId("RFQ", r.Id);
                AddNode(new SalesOrderJourneyNodeDto
                {
                    Id = rNodeId,
                    Type = "RFQ",
                    Title = $"需求 {r.RfqCode}",
                    StatusText = ((short)r.Status).ToString(),
                    CreateDate = r.CreateTime.ToString("yyyy-MM-dd"),
                    CreatorName = await ResolveUserNameAsync(r.CreateUserId) ?? "—"
                });

                foreach (var q in quotes.Where(x => x.RFQId == r.Id))
                {
                    AddEdge(rNodeId, NodeId("QUOTE", q.Id));
                }
            }

            // 采购申请（PR）
            var prs = (await _prRepo.FindAsync(p => p.SellOrderId == so.Id)).ToList();
            foreach (var pr in prs)
            {
                var prNodeId = NodeId("PURCHASE_REQUISITION", pr.Id);
                AddNode(new SalesOrderJourneyNodeDto
                {
                    Id = prNodeId,
                    Type = "PURCHASE_REQUISITION",
                    Title = $"采购申请 {pr.BillCode}",
                    StatusText = pr.Status.ToString(),
                    CreateDate = pr.CreateTime.ToString("yyyy-MM-dd"),
                    CreatorName = await ResolveUserNameAsync(pr.CreateUserId) ?? "—",
                    Quantity = pr.Qty,
                    Amount = pr.QuoteCost != 0m ? pr.QuoteCost * pr.Qty : null
                });
                AddEdge(soNodeId, prNodeId);
            }

            // 采购订单（PO）：通过 POItem.SellOrderItemId -> SOItem
            var soItemIds = soItems.Select(i => i.Id).ToList();
            var poItems = soItemIds.Count == 0 ? new List<PurchaseOrderItem>() : (await _poItemRepo.FindAsync(i => i.SellOrderItemId != null && soItemIds.Contains(i.SellOrderItemId!))).ToList();
            var poIds = poItems.Select(i => i.PurchaseOrderId).Distinct().ToList();
            var pos = poIds.Count == 0 ? new List<PurchaseOrder>() : (await _poRepo.FindAsync(p => poIds.Contains(p.Id))).ToList();

            foreach (var po in pos)
            {
                var poNodeId = NodeId("PURCHASE_ORDER", po.Id);
                AddNode(new SalesOrderJourneyNodeDto
                {
                    Id = poNodeId,
                    Type = "PURCHASE_ORDER",
                    Title = $"采购订单 {po.PurchaseOrderCode}",
                    StatusText = po.Status.ToString(),
                    CreateDate = po.CreateTime.ToString("yyyy-MM-dd"),
                    CreatorName = await ResolveUserNameAsync(po.CreateUserId) ?? "—",
                    Amount = po.Total,
                    Quantity = po.ItemRows
                });
                AddEdge(soNodeId, poNodeId);
            }

            // 到货通知（ArrivalNotice）/ 质检（QC）
            var arrivals = poIds.Count == 0 ? new List<StockInNotify>() : (await _arrivalRepo.FindAsync(a => poIds.Contains(a.PurchaseOrderId))).ToList();
            foreach (var a in arrivals)
            {
                var aNodeId = NodeId("ARRIVAL_NOTICE", a.Id);
                AddNode(new SalesOrderJourneyNodeDto
                {
                    Id = aNodeId,
                    Type = "ARRIVAL_NOTICE",
                    Title = $"到货通知 {a.NoticeCode}",
                    StatusText = a.Status.ToString(),
                    CreateDate = a.CreateTime.ToString("yyyy-MM-dd"),
                    CreatorName = await ResolveUserNameAsync(a.CreateUserId) ?? "—",
                    Quantity = a.ExpectQty,
                    Amount = a.ExpectTotal
                });
                if (!string.IsNullOrWhiteSpace(a.PurchaseOrderId))
                    AddEdge(NodeId("PURCHASE_ORDER", a.PurchaseOrderId), aNodeId);
            }

            var arrivalIds = arrivals.Select(x => x.Id).ToList();
            var qcs = arrivalIds.Count == 0 ? new List<QCInfo>() : (await _qcRepo.FindAsync(q => arrivalIds.Contains(q.StockInNotifyId))).ToList();
            foreach (var qc in qcs)
            {
                var qcNodeId = NodeId("QC", qc.Id);
                AddNode(new SalesOrderJourneyNodeDto
                {
                    Id = qcNodeId,
                    Type = "QC",
                    Title = $"质检 {qc.QcCode}",
                    StatusText = qc.Status.ToString(),
                    CreateDate = qc.CreateTime.ToString("yyyy-MM-dd"),
                    CreatorName = await ResolveUserNameAsync(qc.CreateUserId) ?? "—",
                    Quantity = qc.PassQty + qc.RejectQty
                });
                AddEdge(NodeId("ARRIVAL_NOTICE", qc.StockInNotifyId), qcNodeId);
            }

            // 入库（StockIn）：通过 stockinitemextend 上的采购/销售明细行关联到本单的采购订单
            var stockIns = (await _stockInRepo.GetAllAsync()).ToList();
            var allSiExt = (await _stockInItemExtendRepo.GetAllAsync()).ToList();
            var extByStockIn = allSiExt.GroupBy(e => e.StockInId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            var poLineById = poItems
                .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                .ToDictionary(x => x.Id.Trim(), x => x, StringComparer.OrdinalIgnoreCase);
            var poLineIdsForSo = new HashSet<string>(poItems.Select(x => x.Id.Trim()), StringComparer.OrdinalIgnoreCase);
            var sellIdsForSo = new HashSet<string>(
                soItems.Select(x => x.Id.Trim()).Where(x => !string.IsNullOrEmpty(x)),
                StringComparer.OrdinalIgnoreCase);
            var poCodesForSo = new HashSet<string>(
                pos.Select(p => p.PurchaseOrderCode.Trim()).Where(x => !string.IsNullOrEmpty(x)),
                StringComparer.OrdinalIgnoreCase);

            bool StockInExtMatchesSo(StockIn s)
            {
                if (!extByStockIn.TryGetValue(s.Id, out var rows) || rows.Count == 0)
                    return false;
                foreach (var e in rows)
                {
                    if (!string.IsNullOrWhiteSpace(e.PurchaseOrderItemId) &&
                        poLineIdsForSo.Contains(e.PurchaseOrderItemId.Trim()))
                        return true;
                    if (!string.IsNullOrWhiteSpace(e.SellOrderItemId) &&
                        sellIdsForSo.Contains(e.SellOrderItemId.Trim()))
                        return true;
                    if (!string.IsNullOrWhiteSpace(e.PurchaseOrderItemCode) &&
                        poCodesForSo.Count > 0 &&
                        poItems.Any(pl =>
                            !string.IsNullOrWhiteSpace(pl.PurchaseOrderItemCode) &&
                            string.Equals(pl.PurchaseOrderItemCode.Trim(), e.PurchaseOrderItemCode!.Trim(), StringComparison.OrdinalIgnoreCase) &&
                            poIds.Contains(pl.PurchaseOrderId)))
                        return true;
                }

                return false;
            }

            var stockInsForPo = poIds.Count == 0
                ? new List<StockIn>()
                : stockIns.Where(StockInExtMatchesSo).ToList();

            foreach (var si in stockInsForPo)
            {
                var siNodeId = NodeId("STOCK_IN", si.Id);
                AddNode(new SalesOrderJourneyNodeDto
                {
                    Id = siNodeId,
                    Type = "STOCK_IN",
                    Title = $"入库 {si.StockInCode}",
                    StatusText = si.Status.ToString(),
                    CreateDate = si.CreateTime.ToString("yyyy-MM-dd"),
                    CreatorName = si.CreatedBy ?? (await ResolveUserNameAsync(si.CreateUserId)) ?? "—",
                    Quantity = si.TotalQuantity,
                    Amount = si.TotalAmount
                });

                string? edgePoId = null;
                if (extByStockIn.TryGetValue(si.Id, out var siExtRows))
                {
                    foreach (var e in siExtRows)
                    {
                        if (!string.IsNullOrWhiteSpace(e.PurchaseOrderItemId) &&
                            poLineById.TryGetValue(e.PurchaseOrderItemId.Trim(), out var plEdge) &&
                            poIds.Contains(plEdge.PurchaseOrderId))
                        {
                            edgePoId = plEdge.PurchaseOrderId.Trim();
                            break;
                        }

                        if (!string.IsNullOrWhiteSpace(e.SellOrderItemId))
                        {
                            var plFromSell = poItems.FirstOrDefault(p =>
                                !string.IsNullOrWhiteSpace(p.SellOrderItemId) &&
                                string.Equals(p.SellOrderItemId.Trim(), e.SellOrderItemId!.Trim(), StringComparison.OrdinalIgnoreCase));
                            if (plFromSell != null && poIds.Contains(plFromSell.PurchaseOrderId))
                            {
                                edgePoId = plFromSell.PurchaseOrderId.Trim();
                                break;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(e.PurchaseOrderItemCode))
                        {
                            var plFromCode = poItems.FirstOrDefault(p =>
                                !string.IsNullOrWhiteSpace(p.PurchaseOrderItemCode) &&
                                string.Equals(p.PurchaseOrderItemCode.Trim(), e.PurchaseOrderItemCode!.Trim(), StringComparison.OrdinalIgnoreCase));
                            if (plFromCode != null && poIds.Contains(plFromCode.PurchaseOrderId))
                            {
                                edgePoId = plFromCode.PurchaseOrderId.Trim();
                                break;
                            }
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(edgePoId))
                    AddEdge(NodeId("PURCHASE_ORDER", edgePoId), siNodeId);
            }

            // 出库通知（StockOutRequest）
            var sors = (await _sorRepo.FindAsync(r => r.SalesOrderId == so.Id)).ToList();
            foreach (var sor in sors)
            {
                var sorNodeId = NodeId("STOCK_OUT_NOTIFY", sor.Id);
                AddNode(new SalesOrderJourneyNodeDto
                {
                    Id = sorNodeId,
                    Type = "STOCK_OUT_NOTIFY",
                    Title = $"出库通知 {sor.RequestCode}",
                    StatusText = sor.Status.ToString(),
                    CreateDate = sor.CreateTime.ToString("yyyy-MM-dd"),
                    CreatorName = await ResolveUserNameAsync(sor.CreateUserId) ?? "—",
                    Quantity = sor.Quantity
                });
                AddEdge(soNodeId, sorNodeId);
            }

            // 出库（StockOut）：SourceId 可能是 SalesOrderId；SourceCode 可能是订单号
            var stockOuts = await _stockOutRepo.GetAllAsync();
            var stockOutsForSo = stockOuts
                .Where(s => (!string.IsNullOrWhiteSpace(s.SourceId) && s.SourceId == so.Id) || (!string.IsNullOrWhiteSpace(s.SourceCode) && s.SourceCode == so.SellOrderCode))
                .ToList();
            foreach (var sox in stockOutsForSo)
            {
                var soxNodeId = NodeId("STOCK_OUT", sox.Id);
                AddNode(new SalesOrderJourneyNodeDto
                {
                    Id = soxNodeId,
                    Type = "STOCK_OUT",
                    Title = $"出库 {sox.StockOutCode}",
                    StatusText = sox.Status.ToString(),
                    CreateDate = sox.CreateTime.ToString("yyyy-MM-dd"),
                    CreatorName = await ResolveUserNameAsync(sox.CreateUserId) ?? "—",
                    Quantity = sox.TotalQuantity,
                    Amount = sox.TotalAmount
                });
                AddEdge(soNodeId, soxNodeId);
            }

            // 付款（FinancePayment）：通过 PaymentItem.PurchaseOrderId 关联
            var payItems = poIds.Count == 0 ? new List<FinancePaymentItem>() : (await _payItemRepo.FindAsync(i => i.PurchaseOrderId != null && poIds.Contains(i.PurchaseOrderId))).ToList();
            var payIds = payItems.Select(i => i.FinancePaymentId).Distinct().ToList();
            var pays = payIds.Count == 0 ? new List<FinancePayment>() : (await _payRepo.FindAsync(p => payIds.Contains(p.Id))).ToList();
            foreach (var p in pays)
            {
                var pNodeId = NodeId("FINANCE_PAYMENT", p.Id);
                AddNode(new SalesOrderJourneyNodeDto
                {
                    Id = pNodeId,
                    Type = "FINANCE_PAYMENT",
                    Title = $"申请付款 {p.FinancePaymentCode}",
                    StatusText = p.Status.ToString(),
                    CreateDate = p.CreateTime.ToString("yyyy-MM-dd"),
                    CreatorName = await ResolveUserNameAsync(p.CreateUserId) ?? "—",
                    Amount = p.PaymentAmountToBe
                });

                foreach (var pi in payItems.Where(x => x.FinancePaymentId == p.Id).Select(x => x.PurchaseOrderId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
                {
                    AddEdge(NodeId("PURCHASE_ORDER", pi!), pNodeId);
                }
            }

            // 收款（FinanceReceipt）：ReceiptItem.SellOrderId 关联
            var receiptItems = (await _receiptItemRepo.FindAsync(i => i.SellOrderId == so.Id)).ToList();
            var receiptIds = receiptItems.Select(i => i.FinanceReceiptId).Distinct().ToList();
            var receipts = receiptIds.Count == 0 ? new List<FinanceReceipt>() : (await _receiptRepo.FindAsync(r => receiptIds.Contains(r.Id))).ToList();
            foreach (var r in receipts)
            {
                var rNodeId = NodeId("FINANCE_RECEIPT", r.Id);
                AddNode(new SalesOrderJourneyNodeDto
                {
                    Id = rNodeId,
                    Type = "FINANCE_RECEIPT",
                    Title = $"收款 {r.FinanceReceiptCode}",
                    StatusText = r.Status.ToString(),
                    CreateDate = r.CreateTime.ToString("yyyy-MM-dd"),
                    CreatorName = await ResolveUserNameAsync(r.CreateUserId) ?? "—",
                    Amount = r.ReceiptAmount
                });
                AddEdge(soNodeId, rNodeId);
            }

            // 进项发票（FinancePurchaseInvoice）：通过 PurchaseInvoiceItem.PurchaseOrderCode / StockInId/StockInCode 关联
            var stockInIds = stockInsForPo.Select(x => x.Id).ToList();
            var pinvItems = (await _pinvItemRepo.GetAllAsync()).ToList();
            var pinvItemFiltered = pinvItems.Where(i =>
                (!string.IsNullOrWhiteSpace(i.StockInId) && stockInIds.Contains(i.StockInId)) ||
                (!string.IsNullOrWhiteSpace(i.PurchaseOrderCode) && pos.Any(p => p.PurchaseOrderCode == i.PurchaseOrderCode))
            ).ToList();
            var pinvIds = pinvItemFiltered.Select(i => i.FinancePurchaseInvoiceId).Distinct().ToList();
            var pinvs = pinvIds.Count == 0 ? new List<FinancePurchaseInvoice>() : (await _pinvRepo.FindAsync(x => pinvIds.Contains(x.Id))).ToList();
            foreach (var inv in pinvs)
            {
                var invNodeId = NodeId("PURCHASE_INVOICE", inv.Id);
                AddNode(new SalesOrderJourneyNodeDto
                {
                    Id = invNodeId,
                    Type = "PURCHASE_INVOICE",
                    Title = $"进项发票 {inv.InvoiceNo ?? inv.Id}",
                    StatusText = inv.ConfirmStatus.ToString(),
                    CreateDate = inv.CreateTime.ToString("yyyy-MM-dd"),
                    CreatorName = await ResolveUserNameAsync(inv.CreateUserId) ?? "—",
                    Amount = inv.InvoiceAmount
                });

                foreach (var siId in pinvItemFiltered.Where(x => x.FinancePurchaseInvoiceId == inv.Id).Select(x => x.StockInId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
                {
                    AddEdge(NodeId("STOCK_IN", siId!), invNodeId);
                }
            }

            // 销项发票（FinanceSellInvoice）：目前缺少直接 SellOrderId 关联字段，先按 ReceiptItem.FinanceSellInvoiceId/InvoiceId 反推
            var sellInvIds = receiptItems.Select(x => x.FinanceSellInvoiceId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList()!;
            var sellInvs = sellInvIds.Count == 0 ? new List<FinanceSellInvoice>() : (await _sinvRepo.FindAsync(x => sellInvIds.Contains(x.Id))).ToList();
            foreach (var inv in sellInvs)
            {
                var invNodeId = NodeId("SELL_INVOICE", inv.Id);
                AddNode(new SalesOrderJourneyNodeDto
                {
                    Id = invNodeId,
                    Type = "SELL_INVOICE",
                    Title = $"销项发票 {inv.InvoiceNo ?? inv.InvoiceCode ?? inv.Id}",
                    StatusText = inv.InvoiceStatus.ToString(),
                    CreateDate = inv.CreateTime.ToString("yyyy-MM-dd"),
                    CreatorName = await ResolveUserNameAsync(inv.CreateUserId) ?? "—",
                    Amount = inv.InvoiceTotal
                });
                AddEdge(soNodeId, invNodeId);
            }

            // 仅保留两端节点均已存在的边，避免入库单仅按行号命中但未能解析到本图内 PO 时出现悬空边，
            // 进而导致 @antv/graphlib 在布局/遍历时抛出 “Node not found for id”。
            var nodeIds = new HashSet<string>(nodes.Keys, StringComparer.Ordinal);
            var safeEdges = edges
                .Where(e => !string.IsNullOrWhiteSpace(e.Source)
                    && !string.IsNullOrWhiteSpace(e.Target)
                    && nodeIds.Contains(e.Source)
                    && nodeIds.Contains(e.Target))
                .ToList();

            return new SalesOrderJourneyResponseDto
            {
                Nodes = nodes.Values.ToList(),
                Edges = safeEdges
            };
        }
    }
}

