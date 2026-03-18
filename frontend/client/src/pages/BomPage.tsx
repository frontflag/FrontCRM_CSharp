/**
 * BomPage — FrontCRM Deep Quantum Theme
 * BOM 批量需求报价：支持手动输入/文件上传物料清单，一键批量匹配报价
 * 参考 Semicore BOM Batch Search 功能
 */
import { useState, useRef } from "react";
import DashboardLayout from "@/components/DashboardLayout";
import { toast } from "sonner";
import {
  Upload, FileText, Plus, Trash2, Search, Zap, CheckCircle2,
  RefreshCw, Clock, AlertTriangle, Download, ShoppingCart,
  ChevronDown, ChevronUp, ExternalLink, X
} from "lucide-react";

const EXCHANGE_RATE = 7.24;

interface BomItem {
  id: string;
  partNumber: string;
  description: string;
  qty: number;
  unit: string;
  status: "pending" | "searching" | "found" | "partial" | "notfound";
  bestPrice?: number;
  bestSupplier?: string;
  totalPrice?: number;
  suppliersCount?: number;
}

const initialItems: BomItem[] = [
  { id: "1", partNumber: "iPhone 15 Pro Max", description: "苹果手机 256G 深空黑", qty: 50, unit: "台", status: "pending" },
  { id: "2", partNumber: "ThinkPad X1", description: "联想笔记本 32G/1T", qty: 20, unit: "台", status: "pending" },
  { id: "3", partNumber: "iPad Air M2", description: "苹果平板 256G WiFi", qty: 30, unit: "台", status: "pending" },
  { id: "4", partNumber: "Sony WH-1000XM5", description: "索尼降噪耳机", qty: 100, unit: "个", status: "pending" },
  { id: "5", partNumber: "Logitech MX Master 3", description: "罗技无线鼠标", qty: 200, unit: "个", status: "pending" },
];

const mockPrices: Record<string, { price: number; supplier: string; count: number }> = {
  "iPhone 15 Pro Max": { price: 9399, supplier: "华强北数码城", count: 5 },
  "ThinkPad X1": { price: 14799, supplier: "联想官方旗舰店", count: 3 },
  "iPad Air M2": { price: 4599, supplier: "京东企业购", count: 4 },
  "Sony WH-1000XM5": { price: 2299, supplier: "苏宁易购B2B", count: 4 },
  "Logitech MX Master 3": { price: 599, supplier: "天猫企业店", count: 3 },
};

const statusConfig: Record<string, { color: string; bg: string; label: string; icon: React.ElementType }> = {
  pending: { color: "rgba(224,244,255,0.4)", bg: "rgba(224,244,255,0.06)", label: "待查询", icon: Clock },
  searching: { color: "#50BBE3", bg: "rgba(0,212,255,0.1)", label: "查询中", icon: RefreshCw },
  found: { color: "#46BF91", bg: "rgba(70,191,145,0.12)", label: "已找到", icon: CheckCircle2 },
  partial: { color: "#C99A45", bg: "rgba(201,154,69,0.12)", label: "部分匹配", icon: AlertTriangle },
  notfound: { color: "#C95745", bg: "rgba(201,87,69,0.12)", label: "未找到", icon: X },
};

export default function BomPage() {
  const [items, setItems] = useState<BomItem[]>(initialItems);
  const [newPart, setNewPart] = useState("");
  const [newDesc, setNewDesc] = useState("");
  const [newQty, setNewQty] = useState("1");
  const [newUnit, setNewUnit] = useState("台");
  const [isSearchingAll, setIsSearchingAll] = useState(false);
  const [overallProgress, setOverallProgress] = useState(0);
  const [expandedRows, setExpandedRows] = useState<Set<string>>(new Set());
  const [showAddRow, setShowAddRow] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const toggleExpand = (id: string) => {
    setExpandedRows(prev => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  };

  const addItem = () => {
    if (!newPart.trim()) { toast.warning("请输入物料型号"); return; }
    const item: BomItem = {
      id: Date.now().toString(),
      partNumber: newPart.trim(),
      description: newDesc.trim(),
      qty: parseInt(newQty) || 1,
      unit: newUnit,
      status: "pending",
    };
    setItems(prev => [...prev, item]);
    setNewPart(""); setNewDesc(""); setNewQty("1");
    setShowAddRow(false);
    toast.success("物料已添加到清单");
  };

  const removeItem = (id: string) => {
    setItems(prev => prev.filter(i => i.id !== id));
  };

  const searchAll = () => {
    const pendingItems = items.filter(i => i.status === "pending" || i.status === "notfound");
    if (pendingItems.length === 0) { toast.info("所有物料已完成报价查询"); return; }

    setIsSearchingAll(true);
    setOverallProgress(0);

    // Set all pending to searching
    setItems(prev => prev.map(i =>
      i.status === "pending" || i.status === "notfound"
        ? { ...i, status: "searching" as const }
        : i
    ));

    let completed = 0;
    const total = pendingItems.length;

    pendingItems.forEach((item, idx) => {
      const delay = (idx + 1) * 800 + Math.random() * 400;
      setTimeout(() => {
        const mockData = Object.entries(mockPrices).find(([k]) =>
          item.partNumber.toLowerCase().includes(k.toLowerCase()) ||
          k.toLowerCase().includes(item.partNumber.toLowerCase())
        );

        setItems(prev => prev.map(i => {
          if (i.id !== item.id) return i;
          if (mockData) {
            const [, data] = mockData;
            return {
              ...i,
              status: "found" as const,
              bestPrice: data.price,
              bestSupplier: data.supplier,
              totalPrice: data.price * i.qty,
              suppliersCount: data.count,
            };
          }
          return { ...i, status: "partial" as const, bestPrice: 0, suppliersCount: 0 };
        }));

        completed++;
        setOverallProgress(Math.round((completed / total) * 100));

        if (completed === total) {
          setIsSearchingAll(false);
          toast.success(`批量报价完成！共查询 ${total} 种物料`);
        }
      }, delay);
    });
  };

  const foundItems = items.filter(i => i.status === "found");
  const totalAmount = foundItems.reduce((sum, i) => sum + (i.totalPrice || 0), 0);
  const completionRate = items.length > 0 ? Math.round((foundItems.length / items.length) * 100) : 0;

  return (
    <DashboardLayout title="BOM 批量报价">
      {/* Header banner */}
      <div
        className="rounded-xl p-5 mb-6 relative overflow-hidden"
        style={{
          background: "#192A3F",
          border: "1px solid rgba(0,212,255,0.2)",
        }}
      >
        <div
          className="absolute inset-0 opacity-5"
          style={{
            backgroundImage: "linear-gradient(rgba(0,212,255,0.3) 1px, transparent 1px), linear-gradient(90deg, rgba(0,212,255,0.3) 1px, transparent 1px)",
            backgroundSize: "40px 40px",
          }}
        />
        <div className="relative z-10 flex flex-wrap items-center justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 mb-1">
              <FileText size={16} style={{ color: "#50BBE3" }} />
              <h2 className="text-base font-bold" style={{ color: "#50BBE3", fontFamily: "Orbitron" }}>
                BOM 批量需求报价
              </h2>
            </div>
            <p className="text-xs" style={{ color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>
              上传 BOM 文件或手动输入物料清单，一键批量匹配全网供应商报价
            </p>
          </div>
          <div className="flex items-center gap-2">
            <input
              ref={fileInputRef}
              type="file"
              accept=".xlsx,.xls,.csv"
              className="hidden"
              onChange={() => toast.info("文件解析功能即将上线")}
            />
            <button
              className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-xs"
              style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.2)", color: "rgba(0,212,255,0.7)", fontFamily: "Noto Sans SC" }}
              onClick={() => fileInputRef.current?.click()}
            >
              <Upload size={13} />
              上传 Excel/CSV
            </button>
            <button
              className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-xs font-medium"
              style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.2)", color: "rgba(0,212,255,0.7)", fontFamily: "Noto Sans SC" }}
              onClick={() => toast.info("导出报价单功能即将上线")}
            >
              <Download size={13} />
              导出报价单
            </button>
          </div>
        </div>
      </div>

      {/* Stats row */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        {[
          { label: "物料总数", value: String(items.length), unit: "种", color: "#50BBE3" },
          { label: "已报价", value: String(foundItems.length), unit: "种", color: "#46BF91" },
          { label: "完成率", value: `${completionRate}%`, unit: "", color: completionRate === 100 ? "#00FF88" : "#C99A45" },
          { label: "预计总金额", value: totalAmount > 0 ? `¥${(totalAmount / 10000).toFixed(1)}万` : "—", unit: "", color: "#3295C9" },
        ].map((kpi) => (
          <div key={kpi.label} className="glass-card rounded-xl px-5 py-4">
            <p className="text-xs mb-1" style={{ color: "rgba(200,216,232,0.6)", fontFamily: "Noto Sans SC" }}>{kpi.label}</p>
            <div className="flex items-baseline gap-1">
              <span className="text-xl font-bold" style={{ fontFamily: "Space Mono", color: kpi.color }}>{kpi.value}</span>
              {kpi.unit && <span className="text-xs" style={{ color: "rgba(224,244,255,0.4)" }}>{kpi.unit}</span>}
            </div>
          </div>
        ))}
      </div>

      {/* Overall progress */}
      {isSearchingAll && (
        <div className="glass-card rounded-xl p-4 mb-4">
          <div className="flex items-center justify-between mb-2">
            <div className="flex items-center gap-2">
              <RefreshCw size={13} className="animate-spin" style={{ color: "#50BBE3" }} />
              <span className="text-xs" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>正在批量查询报价...</span>
            </div>
            <span className="text-xs font-mono" style={{ color: "#50BBE3" }}>{overallProgress}%</span>
          </div>
          <div className="h-2 rounded-full overflow-hidden" style={{ background: "#162233" }}>
            <div
              className="h-full rounded-full transition-all duration-500"
              style={{
                width: `${overallProgress}%`,
                background: "linear-gradient(90deg, #50BBE3, #3295C9, #46BF91)",
                boxShadow: "0 0 8px rgba(0,212,255,0.5)",
              }}
            />
          </div>
        </div>
      )}

      {/* BOM Table */}
      <div className="glass-card rounded-xl p-5">
        {/* Toolbar */}
        <div className="flex items-center justify-between mb-4 flex-wrap gap-3">
          <h3 className="text-sm font-semibold" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
            物料清单 BOM
          </h3>
          <div className="flex items-center gap-2">
            <button
              className="flex items-center gap-1.5 px-3 py-2 rounded text-xs"
              style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.15)", color: "rgba(0,212,255,0.6)", fontFamily: "Noto Sans SC" }}
              onClick={() => setShowAddRow(true)}
            >
              <Plus size={13} />
              手动添加
            </button>
            <button
              className="flex items-center gap-1.5 px-4 py-2 rounded text-xs font-medium"
              style={{
                background: isSearchingAll
                  ? "rgba(0,212,255,0.06)"
                  : "linear-gradient(135deg, rgba(0,212,255,0.2), rgba(0,102,255,0.2))",
                border: "1px solid rgba(0,212,255,0.4)",
                color: "#50BBE3",
                fontFamily: "Noto Sans SC",
                opacity: isSearchingAll ? 0.6 : 1,
              }}
              onClick={searchAll}
              disabled={isSearchingAll}
            >
              {isSearchingAll ? (
                <><RefreshCw size={13} className="animate-spin" />查询中...</>
              ) : (
                <><Zap size={13} />一键批量报价</>
              )}
            </button>
          </div>
        </div>

        {/* Add row form */}
        {showAddRow && (
          <div
            className="rounded-xl p-4 mb-4"
            style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.2)" }}
          >
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 mb-3">
              <div className="sm:col-span-1">
                <label className="block text-xs mb-1" style={{ color: "rgba(200,216,232,0.6)", fontFamily: "Noto Sans SC" }}>物料型号 *</label>
                <input
                  type="text"
                  placeholder="如：iPhone 15 Pro Max"
                  value={newPart}
                  onChange={(e) => setNewPart(e.target.value)}
                  className="w-full px-3 py-2 rounded text-xs outline-none"
                  style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.2)", color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
                />
              </div>
              <div className="sm:col-span-1">
                <label className="block text-xs mb-1" style={{ color: "rgba(200,216,232,0.6)", fontFamily: "Noto Sans SC" }}>物料描述</label>
                <input
                  type="text"
                  placeholder="规格描述（可选）"
                  value={newDesc}
                  onChange={(e) => setNewDesc(e.target.value)}
                  className="w-full px-3 py-2 rounded text-xs outline-none"
                  style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.2)", color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
                />
              </div>
              <div>
                <label className="block text-xs mb-1" style={{ color: "rgba(200,216,232,0.6)", fontFamily: "Noto Sans SC" }}>需求数量</label>
                <input
                  type="number"
                  min="1"
                  value={newQty}
                  onChange={(e) => setNewQty(e.target.value)}
                  className="w-full px-3 py-2 rounded text-xs outline-none"
                  style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.2)", color: "#E0F4FF" }}
                />
              </div>
              <div>
                <label className="block text-xs mb-1" style={{ color: "rgba(200,216,232,0.6)", fontFamily: "Noto Sans SC" }}>单位</label>
                <select
                  value={newUnit}
                  onChange={(e) => setNewUnit(e.target.value)}
                  className="w-full px-3 py-2 rounded text-xs outline-none"
                  style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.2)", color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
                >
                  {["台", "个", "套", "件", "箱"].map(u => (
                    <option key={u} value={u} style={{ background: "#0A1628" }}>{u}</option>
                  ))}
                </select>
              </div>
            </div>
            <div className="flex gap-2">
              <button
                className="px-4 py-2 rounded text-xs"
                style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.15)", color: "rgba(0,212,255,0.6)", fontFamily: "Noto Sans SC" }}
                onClick={() => setShowAddRow(false)}
              >
                取消
              </button>
              <button
                className="px-4 py-2 rounded text-xs font-medium"
                style={{ background: "linear-gradient(135deg, rgba(0,212,255,0.2), rgba(0,102,255,0.2))", border: "1px solid rgba(0,212,255,0.4)", color: "#50BBE3", fontFamily: "Noto Sans SC" }}
                onClick={addItem}
              >
                确认添加
              </button>
            </div>
          </div>
        )}

        {/* Table */}
        <div className="overflow-x-auto">
          <table className="w-full text-xs">
            <thead>
              <tr style={{ borderBottom: "1px solid rgba(0,212,255,0.12)" }}>
                {["#", "物料型号", "描述", "需求数量", "状态", "最优单价", "最优供应商", "小计金额", "操作"].map((h) => (
                  <th
                    key={h}
                    className="text-left pb-3 pr-3 font-medium whitespace-nowrap"
                    style={{ color: "rgba(200,216,232,0.6)", fontFamily: "Noto Sans SC" }}
                  >
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {items.map((item, idx) => {
                const sc = statusConfig[item.status];
                const StatusIcon = sc.icon;
                const isExpanded = expandedRows.has(item.id);

                return (
                  <>
                    <tr
                      key={item.id}
                      className="transition-all"
                      style={{ borderBottom: isExpanded ? "none" : "1px solid rgba(0,212,255,0.05)" }}
                      onMouseEnter={(e) => (e.currentTarget.style.background = "rgba(0,212,255,0.03)")}
                      onMouseLeave={(e) => (e.currentTarget.style.background = "transparent")}
                    >
                      <td className="py-3 pr-3 font-mono" style={{ color: "rgba(200,216,232,0.55)" }}>{idx + 1}</td>
                      <td className="py-3 pr-3">
                        <span className="font-medium" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>{item.partNumber}</span>
                      </td>
                      <td className="py-3 pr-3" style={{ color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC", maxWidth: "160px" }}>
                        <span className="truncate block">{item.description || "—"}</span>
                      </td>
                      <td className="py-3 pr-3 font-mono" style={{ color: "#E0F4FF" }}>
                        {item.qty} <span style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>{item.unit}</span>
                      </td>
                      <td className="py-3 pr-3">
                        <span
                          className="flex items-center gap-1 px-2 py-0.5 rounded w-fit text-xs"
                          style={{ background: sc.bg, color: sc.color, border: `1px solid ${sc.color}30`, fontFamily: "Noto Sans SC" }}
                        >
                          <StatusIcon size={10} className={item.status === "searching" ? "animate-spin" : ""} />
                          {sc.label}
                        </span>
                      </td>
                      <td className="py-3 pr-3">
                        {item.bestPrice ? (
                          <div>
                            <p className="font-mono font-bold" style={{ color: "#46BF91" }}>¥{item.bestPrice.toLocaleString()}</p>
                            <p className="font-mono text-xs" style={{ color: "rgba(0,255,136,0.4)" }}>
                              ${(item.bestPrice / EXCHANGE_RATE).toFixed(0)} USD
                            </p>
                          </div>
                        ) : <span style={{ color: "rgba(224,244,255,0.2)" }}>—</span>}
                      </td>
                      <td className="py-3 pr-3">
                        {item.bestSupplier ? (
                          <div>
                            <p style={{ color: "#50BBE3", fontFamily: "Noto Sans SC" }}>{item.bestSupplier}</p>
                            {item.suppliersCount && (
                              <p className="text-xs" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>
                                共 {item.suppliersCount} 家报价
                              </p>
                            )}
                          </div>
                        ) : <span style={{ color: "rgba(224,244,255,0.2)" }}>—</span>}
                      </td>
                      <td className="py-3 pr-3">
                        {item.totalPrice ? (
                          <span className="font-mono font-bold" style={{ color: "#E0F4FF" }}>
                            ¥{item.totalPrice.toLocaleString()}
                          </span>
                        ) : <span style={{ color: "rgba(224,244,255,0.2)" }}>—</span>}
                      </td>
                      <td className="py-3">
                        <div className="flex items-center gap-1.5">
                          {item.status === "found" && (
                            <button
                              className="p-1.5 rounded"
                              style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.15)", color: "#50BBE3" }}
                              title="展开报价详情"
                              onClick={() => toggleExpand(item.id)}
                            >
                              {isExpanded ? <ChevronUp size={11} /> : <ChevronDown size={11} />}
                            </button>
                          )}
                          <button
                            className="p-1.5 rounded"
                            style={{ background: "rgba(255,107,53,0.06)", border: "1px solid rgba(255,255,255,0.10)", color: "#C95745" }}
                            title="删除"
                            onClick={() => removeItem(item.id)}
                          >
                            <Trash2 size={11} />
                          </button>
                        </div>
                      </td>
                    </tr>

                    {/* Expanded detail row */}
                    {isExpanded && item.status === "found" && (
                      <tr key={`${item.id}-detail`} style={{ borderBottom: "1px solid rgba(0,212,255,0.05)" }}>
                        <td colSpan={9} className="pb-3 pr-3">
                          <div
                            className="rounded-lg p-3 ml-6"
                            style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.1)" }}
                          >
                            <p className="text-xs mb-2" style={{ color: "rgba(200,216,232,0.6)", fontFamily: "Noto Sans SC" }}>
                              {item.partNumber} — 供应商报价摘要
                            </p>
                            <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
                              {[
                                { label: "最优单价", value: `¥${item.bestPrice?.toLocaleString()}`, color: "#46BF91" },
                                { label: "最优供应商", value: item.bestSupplier || "—", color: "#50BBE3" },
                                { label: "需求数量", value: `${item.qty} ${item.unit}`, color: "#E0F4FF" },
                                { label: "小计金额", value: `¥${item.totalPrice?.toLocaleString()}`, color: "#C99A45" },
                              ].map((d) => (
                                <div key={d.label} className="p-2 rounded" style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.08)" }}>
                                  <p className="text-xs" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>{d.label}</p>
                                  <p className="text-xs font-bold mt-0.5" style={{ color: d.color, fontFamily: "Noto Sans SC" }}>{d.value}</p>
                                </div>
                              ))}
                            </div>
                            <div className="flex items-center gap-2 mt-2">
                              <button
                                className="flex items-center gap-1.5 px-3 py-1.5 rounded text-xs font-medium"
                                style={{ background: "linear-gradient(135deg, rgba(0,212,255,0.2), rgba(0,102,255,0.2))", border: "1px solid rgba(0,212,255,0.4)", color: "#50BBE3", fontFamily: "Noto Sans SC" }}
                                onClick={() => toast.success(`${item.partNumber} 已加入采购计划`)}
                              >
                                <ShoppingCart size={11} />
                                加入采购计划
                              </button>
                              <button
                                className="flex items-center gap-1.5 px-3 py-1.5 rounded text-xs"
                                style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.15)", color: "rgba(0,212,255,0.6)", fontFamily: "Noto Sans SC" }}
                                onClick={() => window.open(`https://www.jd.com/search?keyword=${encodeURIComponent(item.partNumber)}`, "_blank")}
                              >
                                <ExternalLink size={11} />
                                查看详细报价
                              </button>
                            </div>
                          </div>
                        </td>
                      </tr>
                    )}
                  </>
                );
              })}
            </tbody>
          </table>
        </div>

        {/* Summary footer */}
        {foundItems.length > 0 && (
          <div
            className="mt-4 pt-4 flex items-center justify-between flex-wrap gap-3"
            style={{ borderTop: "1px solid rgba(0,212,255,0.1)" }}
          >
            <div className="flex items-center gap-4">
              <span className="text-xs" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>
                已报价 {foundItems.length}/{items.length} 种物料
              </span>
              <div className="h-1.5 w-32 rounded-full overflow-hidden" style={{ background: "#162233" }}>
                <div
                  className="h-full rounded-full"
                  style={{
                    width: `${completionRate}%`,
                    background: "linear-gradient(90deg, #50BBE3, #46BF91)",
                    boxShadow: "0 0 4px rgba(0,212,255,0.4)",
                  }}
                />
              </div>
              <span className="text-xs font-mono" style={{ color: "#50BBE3" }}>{completionRate}%</span>
            </div>
            <div className="flex items-center gap-4">
              <div className="text-right">
                <p className="text-xs" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>预计总金额</p>
                <p className="text-lg font-bold font-mono" style={{ color: "#46BF91" }}>
                  ¥{totalAmount.toLocaleString()}
                </p>
              </div>
              <button
                className="flex items-center gap-1.5 px-4 py-2.5 rounded text-xs font-medium"
                style={{
                  background: "linear-gradient(135deg, rgba(0,212,255,0.2), rgba(0,102,255,0.2))",
                  border: "1px solid rgba(0,212,255,0.4)",
                  color: "#50BBE3",
                  fontFamily: "Noto Sans SC",
                }}
                onClick={() => toast.success("已生成采购申请单，等待审批")}
              >
                <ShoppingCart size={13} />
                一键生成采购申请
              </button>
            </div>
          </div>
        )}
      </div>
    </DashboardLayout>
  );
}
