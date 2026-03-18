/**
 * PurchasePage — FrontCRM Deep Quantum Theme
 * 采购订单管理：列表、新建、详情、以销定采关联、状态流转
 * 状态流：草稿(0)→待审批(1)→已审批(2)→已确认(3)→待入库(4)→已入库(5)→已完成(6)/已取消(-1)
 */
import { useState, useEffect, useCallback } from "react";
import DashboardLayout from "@/components/DashboardLayout";
import { toast } from "sonner";
import {
  Plus, Search, Eye, FileText, CheckCircle, Clock,
  XCircle, ChevronDown, ChevronUp, Trash2, X, Package,
  ArrowRight, DollarSign, RefreshCw, Link2, Warehouse
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";

interface PurchaseOrderItem {
  id?: string;
  sellOrderItemId?: string;
  vendorId?: string;
  pn: string;
  brand: string;
  qty: number;
  cost: number;
  currency: number;
  deliveryDate?: string;
  comment?: string;
  linkedSellOrderCode?: string;
  linkedPn?: string;
}

interface PurchaseOrder {
  id: string;
  purchaseOrderCode: string;
  vendorId: string;
  vendorName: string;
  vendorCode?: string;
  purchaseUserName?: string;
  type: number;
  currency: number;
  deliveryDate?: string;
  deliveryAddress?: string;
  total: number;
  itemRows: number;
  status: number;
  stockInStatus?: number;
  invoiceStatus?: number;
  paymentStatus?: number;
  comment?: string;
  createTime: string;
  items?: PurchaseOrderItem[];
}

const API_BASE = "http://localhost:5000/api/v1";

const STATUS_MAP: Record<number, { label: string; color: string; icon: React.ReactNode }> = {
  [-1]: { label: "已取消", color: "bg-red-500/20 text-red-400 border-red-500/30", icon: <XCircle className="w-3 h-3" /> },
  0:    { label: "草稿",   color: "bg-zinc-500/20 text-zinc-400 border-zinc-500/30", icon: <FileText className="w-3 h-3" /> },
  1:    { label: "待审批", color: "bg-yellow-500/20 text-yellow-400 border-yellow-500/30", icon: <Clock className="w-3 h-3" /> },
  2:    { label: "已审批", color: "bg-blue-500/20 text-blue-400 border-blue-500/30", icon: <CheckCircle className="w-3 h-3" /> },
  3:    { label: "已确认", color: "bg-cyan-500/20 text-cyan-400 border-cyan-500/30", icon: <CheckCircle className="w-3 h-3" /> },
  4:    { label: "待入库", color: "bg-orange-500/20 text-orange-400 border-orange-500/30", icon: <Package className="w-3 h-3" /> },
  5:    { label: "已入库", color: "bg-purple-500/20 text-purple-400 border-purple-500/30", icon: <Warehouse className="w-3 h-3" /> },
  6:    { label: "已完成", color: "bg-emerald-500/20 text-emerald-400 border-emerald-500/30", icon: <CheckCircle className="w-3 h-3" /> },
};

const CURRENCY_MAP: Record<number, string> = { 1: "CNY", 2: "USD", 3: "EUR", 4: "HKD" };
const TYPE_MAP: Record<number, string> = { 1: "现货", 2: "期货", 3: "样品" };

const NEXT_STATUS: Record<number, { status: number; label: string; color: string }[]> = {
  0:  [{ status: 1, label: "提交审批", color: "bg-yellow-600 hover:bg-yellow-500" }, { status: -1, label: "取消", color: "bg-red-700 hover:bg-red-600" }],
  1:  [{ status: 2, label: "审批通过", color: "bg-blue-600 hover:bg-blue-500" }, { status: -1, label: "取消", color: "bg-red-700 hover:bg-red-600" }],
  2:  [{ status: 3, label: "供应商确认", color: "bg-cyan-600 hover:bg-cyan-500" }, { status: -1, label: "取消", color: "bg-red-700 hover:bg-red-600" }],
  3:  [{ status: 4, label: "申请入库", color: "bg-orange-600 hover:bg-orange-500" }],
  4:  [{ status: 5, label: "确认入库", color: "bg-purple-600 hover:bg-purple-500" }],
  5:  [{ status: 6, label: "完成订单", color: "bg-emerald-600 hover:bg-emerald-500" }],
  6:  [],
  [-1]: [],
};

const STOCK_IN_MAP: Record<number, { label: string; color: string }> = {
  0: { label: "未入库", color: "text-zinc-400" },
  1: { label: "部分入库", color: "text-yellow-400" },
  2: { label: "全部入库", color: "text-emerald-400" },
};

const PAYMENT_MAP: Record<number, { label: string; color: string }> = {
  0: { label: "未付款", color: "text-zinc-400" },
  1: { label: "部分付款", color: "text-yellow-400" },
  2: { label: "已付款", color: "text-emerald-400" },
};

const emptyItem = (): PurchaseOrderItem => ({ pn: "", brand: "", qty: 1, cost: 0, currency: 1 });

const MOCK_ORDERS: PurchaseOrder[] = [
  {
    id: "po-001", purchaseOrderCode: "PO-20260310-001", vendorId: "v1", vendorName: "深圳宇顺电子",
    purchaseUserName: "王采购", type: 1, currency: 1, total: 125000, itemRows: 2, status: 4,
    stockInStatus: 0, paymentStatus: 0,
    deliveryDate: "2026-03-18T00:00:00Z", createTime: "2026-03-10T09:00:00Z",
    items: [
      { pn: "STM32F103C8T6", brand: "ST", qty: 1000, cost: 10.5, currency: 1, sellOrderItemId: "soi-001", linkedSellOrderCode: "SO-20260310-001", linkedPn: "STM32F103C8T6" },
      { pn: "GD32F103C8T6", brand: "GigaDevice", qty: 500, cost: 8.0, currency: 1 },
    ],
  },
  {
    id: "po-002", purchaseOrderCode: "PO-20260309-001", vendorId: "v2", vendorName: "广州立创电子",
    purchaseUserName: "李采购", type: 2, currency: 2, total: 89000, itemRows: 1, status: 6,
    stockInStatus: 2, paymentStatus: 2,
    createTime: "2026-03-09T11:00:00Z",
    items: [{ pn: "TPS63020DSJR", brand: "TI", qty: 2000, cost: 44.5, currency: 1 }],
  },
  {
    id: "po-003", purchaseOrderCode: "PO-20260308-001", vendorId: "v3", vendorName: "上海贝能电子",
    purchaseUserName: "王采购", type: 1, currency: 1, total: 56000, itemRows: 3, status: 1,
    stockInStatus: 0, paymentStatus: 0,
    createTime: "2026-03-08T10:00:00Z", items: [],
  },
  {
    id: "po-004", purchaseOrderCode: "PO-20260307-001", vendorId: "v1", vendorName: "深圳宇顺电子",
    purchaseUserName: "李采购", type: 3, currency: 1, total: 3200, itemRows: 1, status: 0,
    createTime: "2026-03-07T15:00:00Z", items: [],
  },
];

export default function PurchasePage() {
  const [orders, setOrders] = useState<PurchaseOrder[]>([]);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [detailOrder, setDetailOrder] = useState<PurchaseOrder | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [showDetail, setShowDetail] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [form, setForm] = useState({
    purchaseOrderCode: "", vendorId: "", vendorName: "", purchaseUserName: "",
    type: "1", currency: "1", deliveryDate: "", deliveryAddress: "", comment: "",
    items: [emptyItem()],
  });

  const loadOrders = useCallback(async () => {
    setLoading(true);
    try {
      const res = await fetch(`${API_BASE}/purchase-orders`);
      if (res.ok) {
        const data = await res.json();
        setOrders(data.data || []);
      } else {
        setOrders(MOCK_ORDERS);
      }
    } catch {
      setOrders(MOCK_ORDERS);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { loadOrders(); }, [loadOrders]);

  const filtered = orders.filter(o => {
    const ms = !search || o.purchaseOrderCode.toLowerCase().includes(search.toLowerCase()) || o.vendorName.toLowerCase().includes(search.toLowerCase());
    return ms && (statusFilter === "all" || String(o.status) === statusFilter);
  });

  const stats = {
    total: orders.length,
    pending: orders.filter(o => o.status === 1).length,
    confirmed: orders.filter(o => o.status === 3).length,
    completed: orders.filter(o => o.status === 6).length,
    totalAmount: orders.reduce((s, o) => s + (o.total || 0), 0),
    unpaidCount: orders.filter(o => (o.paymentStatus ?? 0) < 2 && o.status >= 3).length,
  };

  const handleCreate = async () => {
    if (!form.purchaseOrderCode.trim()) { toast.error("请填写采购单号"); return; }
    if (!form.vendorId.trim()) { toast.error("请填写供应商ID"); return; }
    if (form.items.some(i => !i.pn.trim())) { toast.error("请填写所有明细行的物料型号"); return; }
    setSubmitting(true);
    try {
      const payload = {
        purchaseOrderCode: form.purchaseOrderCode.trim(),
        vendorId: form.vendorId.trim(),
        vendorName: form.vendorName.trim(),
        purchaseUserName: form.purchaseUserName.trim(),
        type: parseInt(form.type),
        currency: parseInt(form.currency),
        deliveryDate: form.deliveryDate || null,
        deliveryAddress: form.deliveryAddress,
        comment: form.comment,
        items: form.items.map(i => ({
          pn: i.pn, brand: i.brand, qty: Number(i.qty), cost: Number(i.cost),
          currency: Number(i.currency), deliveryDate: i.deliveryDate || null,
          sellOrderItemId: i.sellOrderItemId || null,
        })),
      };
      const res = await fetch(`${API_BASE}/purchase-orders`, {
        method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload),
      });
      if (res.ok) {
        toast.success("采购订单创建成功"); setShowCreate(false); resetForm(); loadOrders();
      } else {
        const err = await res.json(); toast.error(err.message || "创建失败");
      }
    } catch {
      const newOrder: PurchaseOrder = {
        id: `demo-${Date.now()}`, purchaseOrderCode: form.purchaseOrderCode, vendorId: form.vendorId,
        vendorName: form.vendorName, purchaseUserName: form.purchaseUserName,
        type: parseInt(form.type), currency: parseInt(form.currency), deliveryDate: form.deliveryDate,
        total: form.items.reduce((s, i) => s + Number(i.qty) * Number(i.cost), 0),
        itemRows: form.items.length, status: 0, comment: form.comment,
        createTime: new Date().toISOString(), items: [...form.items],
      };
      setOrders(prev => [newOrder, ...prev]);
      toast.success("采购订单已创建（演示模式）"); setShowCreate(false); resetForm();
    } finally {
      setSubmitting(false);
    }
  };

  const resetForm = () => setForm({
    purchaseOrderCode: `PO-${new Date().toISOString().slice(0, 10).replace(/-/g, "")}`,
    vendorId: "", vendorName: "", purchaseUserName: "", type: "1", currency: "1",
    deliveryDate: "", deliveryAddress: "", comment: "", items: [emptyItem()],
  });

  const handleStatusChange = async (orderId: string, newStatus: number) => {
    try {
      await fetch(`${API_BASE}/purchase-orders/${orderId}/status`, {
        method: "PATCH", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ status: newStatus }),
      });
    } catch { /* offline */ }
    setOrders(prev => prev.map(o => o.id === orderId ? { ...o, status: newStatus } : o));
    if (detailOrder?.id === orderId) setDetailOrder(prev => prev ? { ...prev, status: newStatus } : null);
    toast.success(`状态已更新：${STATUS_MAP[newStatus]?.label}`);
  };

  const handleDelete = async (orderId: string) => {
    if (!confirm("确认删除此采购订单？")) return;
    try { await fetch(`${API_BASE}/purchase-orders/${orderId}`, { method: "DELETE" }); } catch { /* offline */ }
    setOrders(prev => prev.filter(o => o.id !== orderId));
    toast.success("已删除");
  };

  const addItem = () => setForm(f => ({ ...f, items: [...f.items, emptyItem()] }));
  const removeItem = (idx: number) => setForm(f => ({ ...f, items: f.items.filter((_, i) => i !== idx) }));
  const updateItem = (idx: number, field: keyof PurchaseOrderItem, value: string | number) =>
    setForm(f => ({ ...f, items: f.items.map((item, i) => i === idx ? { ...item, [field]: value } : item) }));
  const totalCost = form.items.reduce((s, i) => s + Number(i.qty) * Number(i.cost), 0);

  return (
    <DashboardLayout>
      <div className="p-6 space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-white tracking-tight">采购订单</h1>
            <p className="text-zinc-400 text-sm mt-0.5">管理供应商采购订单，支持以销定采关联</p>
          </div>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" onClick={loadOrders} className="border-zinc-700 text-zinc-300 hover:bg-zinc-800">
              <RefreshCw className="w-4 h-4 mr-1" /> 刷新
            </Button>
            <Button size="sm" onClick={() => { resetForm(); setShowCreate(true); }} className="bg-orange-600 hover:bg-orange-500 text-white">
              <Plus className="w-4 h-4 mr-1" /> 新建采购单
            </Button>
          </div>
        </div>

        <div className="grid grid-cols-2 lg:grid-cols-6 gap-3">
          {[
            { label: "全部订单", value: stats.total, icon: <FileText className="w-4 h-4" />, color: "text-zinc-300" },
            { label: "待审批", value: stats.pending, icon: <Clock className="w-4 h-4" />, color: "text-yellow-400" },
            { label: "已确认", value: stats.confirmed, icon: <CheckCircle className="w-4 h-4" />, color: "text-cyan-400" },
            { label: "已完成", value: stats.completed, icon: <CheckCircle className="w-4 h-4" />, color: "text-emerald-400" },
            { label: "总采购额", value: `¥${(stats.totalAmount / 10000).toFixed(1)}万`, icon: <DollarSign className="w-4 h-4" />, color: "text-orange-400" },
            { label: "待付款", value: stats.unpaidCount, icon: <DollarSign className="w-4 h-4" />, color: "text-red-400" },
          ].map((s, i) => (
            <div key={i} className="bg-zinc-900 border border-zinc-800 rounded-xl p-4">
              <div className={`flex items-center gap-2 ${s.color} mb-2`}>{s.icon}<span className="text-xs">{s.label}</span></div>
              <div className={`text-2xl font-bold ${s.color}`}>{s.value}</div>
            </div>
          ))}
        </div>

        <div className="flex gap-3 flex-wrap">
          <div className="relative flex-1 min-w-48">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-zinc-500" />
            <Input value={search} onChange={e => setSearch(e.target.value)} placeholder="搜索采购单号、供应商名称..."
              className="pl-9 bg-zinc-900 border-zinc-700 text-white placeholder:text-zinc-500" />
          </div>
          <Select value={statusFilter} onValueChange={setStatusFilter}>
            <SelectTrigger className="w-36 bg-zinc-900 border-zinc-700 text-zinc-300">
              <SelectValue placeholder="全部状态" />
            </SelectTrigger>
            <SelectContent className="bg-zinc-900 border-zinc-700">
              <SelectItem value="all">全部状态</SelectItem>
              {Object.entries(STATUS_MAP).map(([k, v]) => (
                <SelectItem key={k} value={k}>{v.label}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="space-y-2">
          {loading ? (
            <div className="text-center py-16 text-zinc-500">
              <RefreshCw className="w-8 h-8 animate-spin mx-auto mb-3" />加载中...
            </div>
          ) : filtered.length === 0 ? (
            <div className="text-center py-16 text-zinc-500">
              <Package className="w-12 h-12 mx-auto mb-3 opacity-30" />
              <p>暂无采购订单</p>
              <Button size="sm" className="mt-4 bg-orange-600 hover:bg-orange-500" onClick={() => { resetForm(); setShowCreate(true); }}>
                <Plus className="w-4 h-4 mr-1" /> 创建第一张采购单
              </Button>
            </div>
          ) : filtered.map(order => {
            const st = STATUS_MAP[order.status] ?? STATUS_MAP[0];
            const isExpanded = expandedId === order.id;
            const nextSteps = NEXT_STATUS[order.status] ?? [];
            const hasLinked = order.items?.some(i => i.sellOrderItemId);
            return (
              <div key={order.id} className="bg-zinc-900 border border-zinc-800 rounded-xl overflow-hidden hover:border-zinc-700 transition-colors">
                <div className="flex items-center gap-4 px-4 py-3">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-3 flex-wrap">
                      <span className="font-mono font-semibold text-white text-sm">{order.purchaseOrderCode}</span>
                      <Badge className={`text-xs border ${st.color} flex items-center gap-1`}>{st.icon}{st.label}</Badge>
                      {hasLinked && (
                        <Badge className="text-xs border bg-blue-500/10 text-blue-400 border-blue-500/20 flex items-center gap-1">
                          <Link2 className="w-3 h-3" />以销定采
                        </Badge>
                      )}
                      <span className="text-xs text-zinc-500">{TYPE_MAP[order.type] ?? "现货"}</span>
                      <span className="text-xs text-zinc-500">{CURRENCY_MAP[order.currency] ?? "CNY"}</span>
                    </div>
                    <div className="flex items-center gap-4 mt-1 text-xs text-zinc-400">
                      <span>{order.vendorName}</span>
                      <span>{order.itemRows} 行</span>
                      <span className="text-orange-400 font-semibold">¥{(order.total || 0).toLocaleString()}</span>
                      {order.stockInStatus !== undefined && (
                        <span className={STOCK_IN_MAP[order.stockInStatus]?.color ?? "text-zinc-400"}>
                          {STOCK_IN_MAP[order.stockInStatus]?.label}
                        </span>
                      )}
                      {order.paymentStatus !== undefined && (
                        <span className={PAYMENT_MAP[order.paymentStatus]?.color ?? "text-zinc-400"}>
                          {PAYMENT_MAP[order.paymentStatus]?.label}
                        </span>
                      )}
                      {order.deliveryDate && <span>交期: {order.deliveryDate.slice(0, 10)}</span>}
                      <span>{order.createTime?.slice(0, 10)}</span>
                    </div>
                  </div>
                  <div className="flex items-center gap-2 shrink-0">
                    {nextSteps.slice(0, 1).map(ns => (
                      <Button key={ns.status} size="sm" onClick={() => handleStatusChange(order.id, ns.status)}
                        className={`text-xs h-7 px-2 text-white ${ns.color}`}>
                        <ArrowRight className="w-3 h-3 mr-1" />{ns.label}
                      </Button>
                    ))}
                    <Button variant="ghost" size="sm" className="text-zinc-400 hover:text-white h-7 w-7 p-0"
                      onClick={() => { setDetailOrder(order); setShowDetail(true); }}>
                      <Eye className="w-4 h-4" />
                    </Button>
                    <Button variant="ghost" size="sm" className="text-zinc-400 hover:text-red-400 h-7 w-7 p-0"
                      onClick={() => handleDelete(order.id)}>
                      <Trash2 className="w-4 h-4" />
                    </Button>
                    <Button variant="ghost" size="sm" className="text-zinc-400 hover:text-white h-7 w-7 p-0"
                      onClick={() => setExpandedId(isExpanded ? null : order.id)}>
                      {isExpanded ? <ChevronUp className="w-4 h-4" /> : <ChevronDown className="w-4 h-4" />}
                    </Button>
                  </div>
                </div>
                {isExpanded && (
                  <div className="border-t border-zinc-800 px-4 py-3">
                    {nextSteps.length > 0 && (
                      <div className="flex gap-2 mb-3 flex-wrap">
                        <span className="text-xs text-zinc-500 self-center">状态流转：</span>
                        {nextSteps.map(ns => (
                          <Button key={ns.status} size="sm" onClick={() => handleStatusChange(order.id, ns.status)}
                            className={`text-xs h-7 px-3 text-white ${ns.color}`}>{ns.label}</Button>
                        ))}
                      </div>
                    )}
                    {order.items && order.items.length > 0 ? (
                      <table className="w-full text-xs text-zinc-300">
                        <thead>
                          <tr className="text-zinc-500 border-b border-zinc-800">
                            <th className="text-left pb-2 font-normal">型号</th>
                            <th className="text-left pb-2 font-normal">品牌</th>
                            <th className="text-right pb-2 font-normal">数量</th>
                            <th className="text-right pb-2 font-normal">采购价</th>
                            <th className="text-right pb-2 font-normal">小计</th>
                            <th className="text-left pb-2 font-normal pl-4">关联销售单</th>
                          </tr>
                        </thead>
                        <tbody>
                          {order.items.map((item, idx) => (
                            <tr key={idx} className="border-b border-zinc-800/50 last:border-0">
                              <td className="py-1.5 font-mono">{item.pn}</td>
                              <td className="py-1.5">{item.brand}</td>
                              <td className="py-1.5 text-right">{item.qty}</td>
                              <td className="py-1.5 text-right">¥{Number(item.cost).toFixed(4)}</td>
                              <td className="py-1.5 text-right text-orange-400">¥{(Number(item.qty) * Number(item.cost)).toLocaleString()}</td>
                              <td className="py-1.5 pl-4">
                                {item.linkedSellOrderCode ? (
                                  <span className="text-blue-400 flex items-center gap-1">
                                    <Link2 className="w-3 h-3" />{item.linkedSellOrderCode}
                                  </span>
                                ) : <span className="text-zinc-600">—</span>}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    ) : (
                      <p className="text-zinc-500 text-xs">暂无明细数据</p>
                    )}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>

      <Dialog open={showCreate} onOpenChange={setShowCreate}>
        <DialogContent className="bg-zinc-950 border-zinc-800 text-white max-w-3xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle className="text-white">新建采购订单</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-2">
            <div className="grid grid-cols-2 gap-3">
              {[
                { label: "采购单号 *", key: "purchaseOrderCode", placeholder: "PO-20260318-001" },
                { label: "供应商ID *", key: "vendorId", placeholder: "VENDOR-001" },
                { label: "供应商名称", key: "vendorName", placeholder: "供应商公司名称" },
                { label: "采购员", key: "purchaseUserName", placeholder: "采购员姓名" },
              ].map(({ label, key, placeholder }) => (
                <div key={key}>
                  <Label className="text-zinc-400 text-xs">{label}</Label>
                  <Input value={(form as unknown as Record<string, string>)[key]}
                    onChange={e => setForm(f => ({ ...f, [key]: e.target.value }))}
                    className="mt-1 bg-zinc-900 border-zinc-700 text-white" placeholder={placeholder} />
                </div>
              ))}
              <div>
                <Label className="text-zinc-400 text-xs">采购类型</Label>
                <Select value={form.type} onValueChange={v => setForm(f => ({ ...f, type: v }))}>
                  <SelectTrigger className="mt-1 bg-zinc-900 border-zinc-700 text-zinc-300"><SelectValue /></SelectTrigger>
                  <SelectContent className="bg-zinc-900 border-zinc-700">
                    <SelectItem value="1">现货</SelectItem>
                    <SelectItem value="2">期货</SelectItem>
                    <SelectItem value="3">样品</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label className="text-zinc-400 text-xs">币种</Label>
                <Select value={form.currency} onValueChange={v => setForm(f => ({ ...f, currency: v }))}>
                  <SelectTrigger className="mt-1 bg-zinc-900 border-zinc-700 text-zinc-300"><SelectValue /></SelectTrigger>
                  <SelectContent className="bg-zinc-900 border-zinc-700">
                    <SelectItem value="1">CNY 人民币</SelectItem>
                    <SelectItem value="2">USD 美元</SelectItem>
                    <SelectItem value="3">EUR 欧元</SelectItem>
                    <SelectItem value="4">HKD 港币</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label className="text-zinc-400 text-xs">交货日期</Label>
                <Input type="date" value={form.deliveryDate}
                  onChange={e => setForm(f => ({ ...f, deliveryDate: e.target.value }))}
                  className="mt-1 bg-zinc-900 border-zinc-700 text-white" />
              </div>
              <div>
                <Label className="text-zinc-400 text-xs">收货地址</Label>
                <Input value={form.deliveryAddress}
                  onChange={e => setForm(f => ({ ...f, deliveryAddress: e.target.value }))}
                  className="mt-1 bg-zinc-900 border-zinc-700 text-white" placeholder="仓库地址" />
              </div>
            </div>
            <div>
              <Label className="text-zinc-400 text-xs">备注</Label>
              <Textarea value={form.comment} onChange={e => setForm(f => ({ ...f, comment: e.target.value }))}
                className="mt-1 bg-zinc-900 border-zinc-700 text-white resize-none" rows={2} placeholder="采购备注" />
            </div>
            <div>
              <div className="flex items-center justify-between mb-2">
                <Label className="text-zinc-300 text-sm font-semibold">明细行</Label>
                <Button size="sm" variant="outline" onClick={addItem}
                  className="border-zinc-700 text-zinc-300 hover:bg-zinc-800 h-7 text-xs">
                  <Plus className="w-3 h-3 mr-1" /> 添加行
                </Button>
              </div>
              <div className="space-y-2">
                {form.items.map((item, idx) => (
                  <div key={idx} className="bg-zinc-900 rounded-lg p-2 space-y-2">
                    <div className="grid grid-cols-12 gap-2 items-center">
                      <div className="col-span-3">
                        <Input value={item.pn} onChange={e => updateItem(idx, "pn", e.target.value)}
                          className="bg-zinc-800 border-zinc-700 text-white text-xs h-8" placeholder="型号 *" />
                      </div>
                      <div className="col-span-2">
                        <Input value={item.brand} onChange={e => updateItem(idx, "brand", e.target.value)}
                          className="bg-zinc-800 border-zinc-700 text-white text-xs h-8" placeholder="品牌" />
                      </div>
                      <div className="col-span-2">
                        <Input type="number" value={item.qty} onChange={e => updateItem(idx, "qty", e.target.value)}
                          className="bg-zinc-800 border-zinc-700 text-white text-xs h-8" placeholder="数量" min={1} />
                      </div>
                      <div className="col-span-2">
                        <Input type="number" value={item.cost} onChange={e => updateItem(idx, "cost", e.target.value)}
                          className="bg-zinc-800 border-zinc-700 text-white text-xs h-8" placeholder="采购价" step="0.0001" />
                      </div>
                      <div className="col-span-2 text-right text-xs text-orange-400 font-semibold">
                        ¥{(Number(item.qty) * Number(item.cost)).toLocaleString()}
                      </div>
                      <div className="col-span-1 flex justify-end">
                        {form.items.length > 1 && (
                          <Button variant="ghost" size="sm" onClick={() => removeItem(idx)}
                            className="text-red-500 hover:text-red-400 h-7 w-7 p-0">
                            <X className="w-3 h-3" />
                          </Button>
                        )}
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Link2 className="w-3 h-3 text-blue-400 shrink-0" />
                      <Input value={item.sellOrderItemId || ""}
                        onChange={e => updateItem(idx, "sellOrderItemId", e.target.value)}
                        className="bg-zinc-800 border-zinc-700 text-white text-xs h-7 flex-1"
                        placeholder="关联销售明细ID（以销定采，可选）" />
                    </div>
                  </div>
                ))}
              </div>
              <div className="flex justify-end mt-2 text-sm text-zinc-300">
                合计：<span className="text-orange-400 font-bold ml-1">¥{totalCost.toLocaleString()}</span>
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowCreate(false)}
              className="border-zinc-700 text-zinc-300 hover:bg-zinc-800">取消</Button>
            <Button onClick={handleCreate} disabled={submitting}
              className="bg-orange-600 hover:bg-orange-500 text-white">
              {submitting ? "提交中..." : "创建采购单"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={showDetail} onOpenChange={setShowDetail}>
        <DialogContent className="bg-zinc-950 border-zinc-800 text-white max-w-2xl max-h-[90vh] overflow-y-auto">
          {detailOrder && (
            <>
              <DialogHeader>
                <DialogTitle className="text-white flex items-center gap-3">
                  <span className="font-mono">{detailOrder.purchaseOrderCode}</span>
                  <Badge className={`text-xs border ${STATUS_MAP[detailOrder.status]?.color}`}>
                    {STATUS_MAP[detailOrder.status]?.label}
                  </Badge>
                </DialogTitle>
              </DialogHeader>
              <div className="space-y-4">
                <div className="grid grid-cols-2 gap-3 text-sm">
                  {[
                    ["供应商", detailOrder.vendorName],
                    ["采购员", detailOrder.purchaseUserName || "—"],
                    ["类型", TYPE_MAP[detailOrder.type] ?? "—"],
                    ["币种", CURRENCY_MAP[detailOrder.currency] ?? "—"],
                    ["交货日期", detailOrder.deliveryDate?.slice(0, 10) || "—"],
                    ["收货地址", detailOrder.deliveryAddress || "—"],
                    ["总采购额", `¥${(detailOrder.total || 0).toLocaleString()}`],
                    ["入库状态", STOCK_IN_MAP[detailOrder.stockInStatus ?? 0]?.label ?? "—"],
                    ["付款状态", PAYMENT_MAP[detailOrder.paymentStatus ?? 0]?.label ?? "—"],
                    ["创建时间", detailOrder.createTime?.slice(0, 10)],
                  ].map(([label, value]) => (
                    <div key={label} className="bg-zinc-900 rounded-lg p-3">
                      <div className="text-zinc-500 text-xs mb-1">{label}</div>
                      <div className="text-white font-medium">{value}</div>
                    </div>
                  ))}
                </div>
                {detailOrder.comment && (
                  <div className="bg-zinc-900 rounded-lg p-3">
                    <div className="text-zinc-500 text-xs mb-1">备注</div>
                    <div className="text-zinc-300 text-sm">{detailOrder.comment}</div>
                  </div>
                )}
                {(NEXT_STATUS[detailOrder.status] ?? []).length > 0 && (
                  <div className="bg-zinc-900 rounded-lg p-3">
                    <div className="text-zinc-500 text-xs mb-2">状态操作</div>
                    <div className="flex gap-2 flex-wrap">
                      {(NEXT_STATUS[detailOrder.status] ?? []).map(ns => (
                        <Button key={ns.status} size="sm"
                          onClick={() => { handleStatusChange(detailOrder.id, ns.status); setShowDetail(false); }}
                          className={`text-xs text-white ${ns.color}`}>{ns.label}</Button>
                      ))}
                    </div>
                  </div>
                )}
                {detailOrder.items && detailOrder.items.length > 0 && (
                  <div>
                    <div className="text-zinc-400 text-sm font-semibold mb-2">明细行（{detailOrder.items.length} 行）</div>
                    <table className="w-full text-xs text-zinc-300">
                      <thead>
                        <tr className="text-zinc-500 border-b border-zinc-800">
                          <th className="text-left pb-2 font-normal">型号</th>
                          <th className="text-left pb-2 font-normal">品牌</th>
                          <th className="text-right pb-2 font-normal">数量</th>
                          <th className="text-right pb-2 font-normal">采购价</th>
                          <th className="text-right pb-2 font-normal">小计</th>
                          <th className="text-left pb-2 font-normal pl-3">关联销售</th>
                        </tr>
                      </thead>
                      <tbody>
                        {detailOrder.items.map((item, idx) => (
                          <tr key={idx} className="border-b border-zinc-800/50 last:border-0">
                            <td className="py-2 font-mono">{item.pn}</td>
                            <td className="py-2">{item.brand}</td>
                            <td className="py-2 text-right">{item.qty}</td>
                            <td className="py-2 text-right">¥{Number(item.cost).toFixed(4)}</td>
                            <td className="py-2 text-right text-orange-400">¥{(Number(item.qty) * Number(item.cost)).toLocaleString()}</td>
                            <td className="py-2 pl-3">
                              {item.linkedSellOrderCode ? (
                                <span className="text-blue-400 flex items-center gap-1 text-xs">
                                  <Link2 className="w-3 h-3" />{item.linkedSellOrderCode}
                                </span>
                              ) : <span className="text-zinc-600">—</span>}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            </>
          )}
        </DialogContent>
      </Dialog>
    </DashboardLayout>
  );
}
