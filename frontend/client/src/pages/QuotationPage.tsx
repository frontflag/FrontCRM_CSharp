/**
 * QuotationPage — FrontCRM Deep Quantum Theme
 * 需求匹配报价：物料搜索 + 多供应商报价对比 + 价格趋势图 + 库存可用性
 * 参考 Semicore Component Intelligence 风格
 */
import { useState, useRef, useEffect } from "react";
import DashboardLayout from "@/components/DashboardLayout";
import { toast } from "sonner";
import {
  Search, Zap, TrendingUp, Package, CheckCircle2, AlertTriangle,
  ExternalLink, Star, RefreshCw, Clock, ChevronDown, ChevronUp,
  BarChart3, ShoppingCart, Info
} from "lucide-react";
import {
  AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer, BarChart, Bar, Cell
} from "recharts";

// ─── Mock data ────────────────────────────────────────────────────────────────
const EXCHANGE_RATE = 7.24; // USD → CNY

const mockResults: Record<string, {
  partNumber: string;
  description: string;
  category: string;
  manufacturer: string;
  datasheet: string;
  specs: { label: string; value: string }[];
  suppliers: {
    id: string; name: string; logo: string; website: string;
    price_usd: number; moq: number; stock: number; leadTime: string;
    rating: number; delivery: string; status: "available" | "limited" | "out";
  }[];
  priceTrend: { month: string; price: number }[];
  stockTrend: { month: string; stock: number }[];
}> = {
  "iPhone 15 Pro Max": {
    partNumber: "iPhone 15 Pro Max 256G",
    description: "Apple iPhone 15 Pro Max 256GB 深空黑色 智能手机",
    category: "智能手机",
    manufacturer: "Apple Inc.",
    datasheet: "#",
    specs: [
      { label: "屏幕尺寸", value: "6.7英寸 Super Retina XDR" },
      { label: "处理器", value: "A17 Pro 芯片" },
      { label: "存储容量", value: "256GB" },
      { label: "摄像头", value: "4800万像素主摄" },
      { label: "电池容量", value: "4422mAh" },
      { label: "操作系统", value: "iOS 17" },
    ],
    suppliers: [
      { id: "s1", name: "京东企业购", logo: "JD", website: "https://www.jd.com/", price_usd: 1380, moq: 10, stock: 520, leadTime: "1-3天", rating: 4.8, delivery: "次日达", status: "available" },
      { id: "s2", name: "苏宁易购B2B", logo: "SN", website: "https://www.suning.com/", price_usd: 1365, moq: 20, stock: 280, leadTime: "2-5天", rating: 4.6, delivery: "2日达", status: "available" },
      { id: "s3", name: "天猫企业店", logo: "TM", website: "https://www.tmall.com/", price_usd: 1355, moq: 5, stock: 45, leadTime: "3-7天", rating: 4.5, delivery: "3日达", status: "limited" },
      { id: "s4", name: "华强北数码城", logo: "HQ", website: "https://www.huaqiangbei.com/", price_usd: 1320, moq: 50, stock: 1200, leadTime: "当天", rating: 4.2, delivery: "自提", status: "available" },
      { id: "s5", name: "拼多多批发", logo: "PDD", website: "https://www.pinduoduo.com/", price_usd: 1298, moq: 100, stock: 0, leadTime: "7-15天", rating: 3.8, delivery: "快递", status: "out" },
    ],
    priceTrend: [
      { month: "9月", price: 9999 }, { month: "10月", price: 9899 }, { month: "11月", price: 9799 },
      { month: "12月", price: 9699 }, { month: "1月", price: 9599 }, { month: "2月", price: 9499 },
      { month: "3月", price: 9399 },
    ],
    stockTrend: [
      { month: "9月", stock: 800 }, { month: "10月", stock: 650 }, { month: "11月", stock: 450 },
      { month: "12月", stock: 280 }, { month: "1月", stock: 520 }, { month: "2月", stock: 680 },
      { month: "3月", stock: 520 },
    ],
  },
  "ThinkPad X1": {
    partNumber: "ThinkPad X1 Carbon Gen 12",
    description: "联想 ThinkPad X1 Carbon Gen 12 超薄商务笔记本",
    category: "笔记本电脑",
    manufacturer: "Lenovo",
    datasheet: "#",
    specs: [
      { label: "处理器", value: "Intel Core Ultra 7 165U" },
      { label: "内存", value: "32GB LPDDR5" },
      { label: "存储", value: "1TB NVMe SSD" },
      { label: "屏幕", value: "14英寸 2.8K OLED" },
      { label: "重量", value: "1.12kg" },
      { label: "操作系统", value: "Windows 11 Pro" },
    ],
    suppliers: [
      { id: "s1", name: "联想官方旗舰店", logo: "LX", website: "https://www.lenovo.com.cn/", price_usd: 1950, moq: 5, stock: 180, leadTime: "3-5天", rating: 4.9, delivery: "顺丰", status: "available" },
      { id: "s2", name: "京东企业购", logo: "JD", website: "https://www.jd.com/", price_usd: 1920, moq: 10, stock: 95, leadTime: "1-3天", rating: 4.8, delivery: "次日达", status: "available" },
      { id: "s3", name: "IT分销商城", logo: "IT", website: "https://www.it168.com/", price_usd: 1880, moq: 20, stock: 32, leadTime: "5-7天", rating: 4.4, delivery: "快递", status: "limited" },
    ],
    priceTrend: [
      { month: "9月", price: 15999 }, { month: "10月", price: 15799 }, { month: "11月", price: 15599 },
      { month: "12月", price: 15399 }, { month: "1月", price: 15199 }, { month: "2月", price: 14999 },
      { month: "3月", price: 14799 },
    ],
    stockTrend: [
      { month: "9月", stock: 300 }, { month: "10月", stock: 240 }, { month: "11月", stock: 180 },
      { month: "12月", stock: 120 }, { month: "1月", stock: 200 }, { month: "2月", stock: 160 },
      { month: "3月", stock: 95 },
    ],
  },
};

const SUGGESTIONS = ["iPhone 15 Pro Max", "ThinkPad X1", "Samsung Galaxy S24", "iPad Air M2", "Sony WH-1000XM5", "MacBook Pro M3"];

// ─── Sub-components ───────────────────────────────────────────────────────────
function SupplierRow({ s, partNumber }: { s: typeof mockResults["iPhone 15 Pro Max"]["suppliers"][0]; partNumber: string }) {
  const priceCNY = (s.price_usd * EXCHANGE_RATE).toFixed(0);
  const statusColor = s.status === "available" ? "#00FF88" : s.status === "limited" ? "#C99A45" : "#C95745";
  const statusLabel = s.status === "available" ? "现货" : s.status === "limited" ? "少量" : "缺货";

  const handleSupplierClick = () => {
    const searchUrl = `${s.website}search?q=${encodeURIComponent(partNumber)}`;
    window.open(s.website, "_blank");
  };

  return (
    <tr
      className="transition-all"
      style={{ borderBottom: "1px solid rgba(0,212,255,0.06)" }}
      onMouseEnter={(e) => (e.currentTarget.style.background = "rgba(0,212,255,0.04)")}
      onMouseLeave={(e) => (e.currentTarget.style.background = "transparent")}
    >
      <td className="py-3 pr-4">
        <button
          className="flex items-center gap-2 group"
          onClick={handleSupplierClick}
          title={`访问 ${s.name} 并搜索 ${partNumber}`}
        >
          <div
            className="w-8 h-8 rounded flex items-center justify-center text-xs font-bold flex-shrink-0"
            style={{ background: "rgba(0,212,255,0.1)", border: "1px solid rgba(0,212,255,0.2)", color: "#50BBE3" }}
          >
            {s.logo}
          </div>
          <span
            className="text-xs font-medium group-hover:underline"
            style={{ color: "#50BBE3", fontFamily: "Noto Sans SC" }}
          >
            {s.name}
          </span>
          <ExternalLink size={10} style={{ color: "rgba(200,216,232,0.55)" }} />
        </button>
      </td>
      <td className="py-3 pr-4">
        <div>
          <p className="text-sm font-bold font-mono" style={{ color: "#E0F4FF" }}>
            ¥{Number(priceCNY).toLocaleString()}
          </p>
          <p className="text-xs font-mono" style={{ color: "rgba(200,216,232,0.6)" }}>
            ${s.price_usd.toLocaleString()} USD
          </p>
        </div>
      </td>
      <td className="py-3 pr-4 font-mono text-xs" style={{ color: "rgba(224,244,255,0.6)" }}>{s.moq}</td>
      <td className="py-3 pr-4">
        <div className="flex items-center gap-1.5">
          <span
            className="w-1.5 h-1.5 rounded-full flex-shrink-0"
            style={{ background: statusColor, boxShadow: `0 0 4px ${statusColor}` }}
          />
          <span className="font-mono text-xs" style={{ color: s.stock > 0 ? "#E0F4FF" : "#C95745" }}>
            {s.stock > 0 ? s.stock.toLocaleString() : "0"}
          </span>
          <span
            className="text-xs px-1.5 py-0.5 rounded"
            style={{ background: `${statusColor}15`, color: statusColor, border: `1px solid ${statusColor}30`, fontFamily: "Noto Sans SC" }}
          >
            {statusLabel}
          </span>
        </div>
      </td>
      <td className="py-3 pr-4 text-xs" style={{ color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>{s.leadTime}</td>
      <td className="py-3 pr-4">
        <div className="flex items-center gap-0.5">
          {[1, 2, 3, 4, 5].map((star) => (
            <Star
              key={star}
              size={10}
              fill={star <= Math.floor(s.rating) ? "#C99A45" : "transparent"}
              style={{ color: star <= Math.floor(s.rating) ? "#C99A45" : "rgba(255,165,0,0.2)" }}
            />
          ))}
          <span className="text-xs ml-1 font-mono" style={{ color: "rgba(224,244,255,0.5)" }}>{s.rating}</span>
        </div>
      </td>
      <td className="py-3">
        <button
          className="flex items-center gap-1 px-3 py-1.5 rounded text-xs font-medium"
          style={{
            background: s.status !== "out" ? "linear-gradient(135deg, rgba(0,212,255,0.2), rgba(0,102,255,0.2))" : "rgba(255,255,255,0.04)",
            border: `1px solid ${s.status !== "out" ? "rgba(0,212,255,0.4)" : "rgba(255,255,255,0.1)"}`,
            color: s.status !== "out" ? "#50BBE3" : "rgba(224,244,255,0.3)",
            fontFamily: "Noto Sans SC",
          }}
          disabled={s.status === "out"}
          onClick={() => s.status !== "out" && toast.success(`已加入采购计划：${s.name}`)}
        >
          <ShoppingCart size={11} />
          {s.status !== "out" ? "加入采购" : "缺货"}
        </button>
      </td>
    </tr>
  );
}

// ─── Main Page ────────────────────────────────────────────────────────────────
export default function QuotationPage() {
  const [query, setQuery] = useState("");
  const [searching, setSearching] = useState(false);
  const [result, setResult] = useState<typeof mockResults["iPhone 15 Pro Max"] | null>(null);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [progress, setProgress] = useState(0);
  const [progressLabel, setProgressLabel] = useState("");
  const [expandSpecs, setExpandSpecs] = useState(false);
  const [queriedPart, setQueriedPart] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);

  const simulateSearch = (q: string) => {
    if (!q.trim()) { toast.warning("请输入物料名称或型号"); return; }
    setSearching(true);
    setResult(null);
    setProgress(0);
    setQueriedPart(q);

    const steps = [
      { pct: 15, label: "正在解析物料型号..." },
      { pct: 35, label: "查询京东企业购..." },
      { pct: 55, label: "查询苏宁易购B2B..." },
      { pct: 70, label: "查询天猫企业店..." },
      { pct: 85, label: "查询华强北数码城..." },
      { pct: 95, label: "汇总报价数据..." },
      { pct: 100, label: "分析完成" },
    ];

    let i = 0;
    const tick = () => {
      if (i < steps.length) {
        setProgress(steps[i].pct);
        setProgressLabel(steps[i].label);
        i++;
        setTimeout(tick, 400 + Math.random() * 300);
      } else {
        // Find best match
        const key = Object.keys(mockResults).find((k) =>
          q.toLowerCase().includes(k.toLowerCase()) || k.toLowerCase().includes(q.toLowerCase())
        );
        const data = key ? mockResults[key] : mockResults["iPhone 15 Pro Max"];
        setTimeout(() => {
          setSearching(false);
          setResult(data);
        }, 300);
      }
    };
    tick();
  };

  const CustomTooltip = ({ active, payload, label }: any) => {
    if (active && payload && payload.length) {
      return (
        <div className="px-3 py-2 rounded text-xs" style={{ background: "#0A1628", border: "1px solid rgba(0,212,255,0.2)" }}>
          <p style={{ color: "rgba(0,212,255,0.7)" }}>{label}</p>
          {payload.map((p: any) => (
            <p key={p.name} style={{ color: p.color }}>
              {p.name === "price" ? `¥${p.value.toLocaleString()}` : `${p.value} 件`}
            </p>
          ))}
        </div>
      );
    }
    return null;
  };

  const bestPrice = result ? Math.min(...result.suppliers.filter(s => s.status !== "out").map(s => s.price_usd)) : 0;

  return (
    <DashboardLayout title="需求匹配报价">
      {/* ── Search Hero ── */}
      <div
        className="rounded-xl p-6 mb-6 relative overflow-hidden"
        style={{
          background: "linear-gradient(135deg, rgba(0,20,50,0.9) 0%, rgba(0,10,30,0.95) 100%)",
          border: "1px solid rgba(0,212,255,0.2)",
          boxShadow: "0 0 60px rgba(0,212,255,0.06)",
        }}
      >
        {/* Decorative grid */}
        <div
          className="absolute inset-0 opacity-5"
          style={{
            backgroundImage: "linear-gradient(rgba(0,212,255,0.3) 1px, transparent 1px), linear-gradient(90deg, rgba(0,212,255,0.3) 1px, transparent 1px)",
            backgroundSize: "40px 40px",
          }}
        />
        <div className="relative z-10">
          <div className="flex items-center gap-2 mb-2">
            <Zap size={16} style={{ color: "#50BBE3" }} />
            <h2 className="text-base font-bold" style={{ color: "#50BBE3", fontFamily: "Orbitron", letterSpacing: "0.05em" }}>
              智能物料报价匹配
            </h2>
          </div>
          <p className="text-xs mb-5" style={{ color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>
            输入物料名称或型号，系统自动匹配全网供应商报价，实时对比价格、库存与交期
          </p>

          {/* Search input */}
          <div className="relative max-w-2xl">
            <div
              className="flex items-center gap-3 px-4 py-3 rounded-xl"
              style={{
                background: "#162233",
                border: "1px solid rgba(0,212,255,0.3)",
                boxShadow: "0 0 20px rgba(0,212,255,0.08)",
              }}
            >
              <Search size={16} style={{ color: "rgba(0,212,255,0.6)", flexShrink: 0 }} />
              <input
                ref={inputRef}
                type="text"
                placeholder="输入物料名称或型号，例如：iPhone 15 Pro Max、ThinkPad X1..."
                value={query}
                onChange={(e) => { setQuery(e.target.value); setShowSuggestions(true); }}
                onKeyDown={(e) => { if (e.key === "Enter") { setShowSuggestions(false); simulateSearch(query); } }}
                onFocus={() => setShowSuggestions(true)}
                onBlur={() => setTimeout(() => setShowSuggestions(false), 150)}
                className="flex-1 bg-transparent text-sm outline-none"
                style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
              />
              <button
                className="px-5 py-2 rounded-lg text-sm font-medium flex items-center gap-2 flex-shrink-0"
                style={{
                  background: "linear-gradient(135deg, rgba(0,212,255,0.3), rgba(0,102,255,0.3))",
                  border: "1px solid rgba(0,212,255,0.5)",
                  color: "#50BBE3",
                  fontFamily: "Noto Sans SC",
                }}
                onClick={() => { setShowSuggestions(false); simulateSearch(query); }}
              >
                <Search size={14} />
                搜索报价
              </button>
            </div>

            {/* Suggestions dropdown */}
            {showSuggestions && (
              <div
                className="absolute top-full left-0 right-0 mt-1 rounded-xl overflow-hidden z-20"
                style={{ background: "#0A1628", border: "1px solid rgba(0,212,255,0.2)", boxShadow: "0 8px 32px rgba(0,0,0,0.5)" }}
              >
                <div className="px-3 py-2" style={{ borderBottom: "1px solid rgba(0,212,255,0.08)" }}>
                  <span className="text-xs" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>热门搜索</span>
                </div>
                {SUGGESTIONS.map((s) => (
                  <button
                    key={s}
                    className="w-full flex items-center gap-3 px-4 py-2.5 text-left text-xs transition-all"
                    style={{ color: "rgba(224,244,255,0.7)", fontFamily: "Noto Sans SC" }}
                    onMouseEnter={(e) => (e.currentTarget.style.background = "rgba(0,212,255,0.08)")}
                    onMouseLeave={(e) => (e.currentTarget.style.background = "transparent")}
                    onMouseDown={() => { setQuery(s); setShowSuggestions(false); simulateSearch(s); }}
                  >
                    <Search size={11} style={{ color: "rgba(200,216,232,0.45)" }} />
                    {s}
                  </button>
                ))}
              </div>
            )}
          </div>

          {/* Quick tags */}
          <div className="flex items-center gap-2 mt-3 flex-wrap">
            <span className="text-xs" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>快速搜索：</span>
            {SUGGESTIONS.slice(0, 4).map((s) => (
              <button
                key={s}
                className="px-3 py-1 rounded-full text-xs transition-all"
                style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.15)", color: "rgba(0,212,255,0.6)", fontFamily: "Noto Sans SC" }}
                onMouseEnter={(e) => { (e.currentTarget as HTMLElement).style.borderColor = "rgba(80,187,227,0.3)"; (e.currentTarget as HTMLElement).style.color = "#50BBE3"; }}
                onMouseLeave={(e) => { (e.currentTarget as HTMLElement).style.borderColor = "rgba(0,212,255,0.15)"; (e.currentTarget as HTMLElement).style.color = "rgba(0,212,255,0.6)"; }}
                onClick={() => { setQuery(s); simulateSearch(s); }}
              >
                {s}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* ── Progress ── */}
      {searching && (
        <div className="glass-card rounded-xl p-5 mb-6">
          <div className="flex items-center justify-between mb-3">
            <div className="flex items-center gap-2">
              <RefreshCw size={14} className="animate-spin" style={{ color: "#50BBE3" }} />
              <span className="text-sm font-medium" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
                正在查询：<span style={{ color: "#50BBE3" }}>{queriedPart}</span>
              </span>
            </div>
            <span className="text-xs font-mono" style={{ color: "#50BBE3" }}>{progress}%</span>
          </div>
          <div className="h-2 rounded-full overflow-hidden mb-2" style={{ background: "#162233" }}>
            <div
              className="h-full rounded-full transition-all duration-500"
              style={{
                width: `${progress}%`,
                background: "linear-gradient(90deg, #50BBE3, #3295C9)",
                boxShadow: "0 0 8px rgba(0,212,255,0.6)",
              }}
            />
          </div>
          <p className="text-xs" style={{ color: "rgba(200,216,232,0.6)", fontFamily: "Noto Sans SC" }}>{progressLabel}</p>

          {/* Supplier progress indicators */}
          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-2 mt-4">
            {["京东企业购", "苏宁易购B2B", "天猫企业店", "华强北数码城", "拼多多批发"].map((name, i) => {
              const done = progress > (i + 1) * 18;
              const active = progress > i * 18 && !done;
              return (
                <div
                  key={name}
                  className="flex items-center gap-2 px-3 py-2 rounded-lg text-xs"
                  style={{
                    background: done ? "rgba(0,255,136,0.06)" : active ? "rgba(0,212,255,0.08)" : "rgba(0,212,255,0.03)",
                    border: `1px solid ${done ? "rgba(0,255,136,0.2)" : active ? "rgba(0,212,255,0.25)" : "rgba(0,212,255,0.08)"}`,
                  }}
                >
                  {done ? (
                    <CheckCircle2 size={11} style={{ color: "#46BF91", flexShrink: 0 }} />
                  ) : active ? (
                    <RefreshCw size={11} className="animate-spin" style={{ color: "#50BBE3", flexShrink: 0 }} />
                  ) : (
                    <Clock size={11} style={{ color: "rgba(200,216,232,0.45)", flexShrink: 0 }} />
                  )}
                  <span style={{ color: done ? "#46BF91" : active ? "#50BBE3" : "rgba(0,212,255,0.4)", fontFamily: "Noto Sans SC" }}>
                    {name}
                  </span>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* ── Results ── */}
      {result && !searching && (
        <div className="space-y-4">
          {/* Part header */}
          <div
            className="rounded-xl p-5"
            style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.15)" }}
          >
            <div className="flex items-start justify-between gap-4 flex-wrap">
              <div>
                <div className="flex items-center gap-2 mb-1">
                  <span className="text-xs px-2 py-0.5 rounded" style={{ background: "rgba(0,212,255,0.1)", color: "#50BBE3", border: "1px solid rgba(0,212,255,0.2)", fontFamily: "Noto Sans SC" }}>
                    {result.category}
                  </span>
                  <span className="text-xs" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>查询物料：{queriedPart}</span>
                </div>
                <h3 className="text-lg font-bold mb-1" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
                  {result.partNumber}
                </h3>
                <p className="text-xs" style={{ color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>
                  {result.description}
                </p>
                <p className="text-xs mt-1" style={{ color: "rgba(200,216,232,0.6)", fontFamily: "Noto Sans SC" }}>
                  制造商：{result.manufacturer}
                </p>
              </div>
              <div className="flex flex-col items-end gap-2">
                <div className="text-right">
                  <p className="text-xs" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>最优报价</p>
                  <p className="text-2xl font-bold font-mono" style={{ color: "#46BF91" }}>
                    ¥{(bestPrice * EXCHANGE_RATE).toLocaleString()}
                  </p>
                  <p className="text-xs font-mono" style={{ color: "rgba(0,255,136,0.5)" }}>
                    ${bestPrice.toLocaleString()} USD
                  </p>
                </div>
                <div className="flex items-center gap-1.5 text-xs" style={{ color: "rgba(200,216,232,0.55)" }}>
                  <Info size={11} />
                  <span style={{ fontFamily: "Noto Sans SC" }}>汇率 1 USD = {EXCHANGE_RATE} CNY</span>
                </div>
              </div>
            </div>

            {/* Specs toggle */}
            <button
              className="flex items-center gap-1.5 mt-3 text-xs"
              style={{ color: "rgba(200,216,232,0.6)", fontFamily: "Noto Sans SC" }}
              onClick={() => setExpandSpecs(!expandSpecs)}
            >
              {expandSpecs ? <ChevronUp size={13} /> : <ChevronDown size={13} />}
              {expandSpecs ? "收起规格参数" : "展开规格参数"}
            </button>

            {expandSpecs && (
              <div className="grid grid-cols-2 sm:grid-cols-3 gap-2 mt-3">
                {result.specs.map((spec) => (
                  <div key={spec.label} className="px-3 py-2 rounded-lg" style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.1)" }}>
                    <p className="text-xs" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>{spec.label}</p>
                    <p className="text-xs font-medium mt-0.5" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>{spec.value}</p>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Supplier quotes table */}
          <div className="glass-card rounded-xl p-5">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-sm font-semibold" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
                供应商报价对比
                <span className="ml-2 text-xs font-normal" style={{ color: "rgba(200,216,232,0.55)" }}>
                  共 {result.suppliers.length} 家供应商
                </span>
              </h3>
              <div className="flex items-center gap-1.5 text-xs" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>
                <CheckCircle2 size={11} style={{ color: "#46BF91" }} />
                点击供应商名称可访问官网验价
              </div>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full text-xs">
                <thead>
                  <tr style={{ borderBottom: "1px solid rgba(0,212,255,0.12)" }}>
                    {["供应商", "单价（含税）", "最小起订量", "库存", "交期", "评分", "操作"].map((h) => (
                      <th
                        key={h}
                        className="text-left pb-3 pr-4 font-medium whitespace-nowrap"
                        style={{ color: "rgba(200,216,232,0.6)", fontFamily: "Noto Sans SC" }}
                      >
                        {h}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {result.suppliers.map((s) => (
                    <SupplierRow key={s.id} s={s} partNumber={result.partNumber} />
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* Charts row */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
            {/* Price trend */}
            <div className="glass-card rounded-xl p-5">
              <div className="flex items-center gap-2 mb-4">
                <TrendingUp size={14} style={{ color: "#50BBE3" }} />
                <h3 className="text-sm font-semibold" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
                  近7个月价格趋势
                </h3>
              </div>
              <ResponsiveContainer width="100%" height={180}>
                <AreaChart data={result.priceTrend}>
                  <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" />
                  <XAxis dataKey="month" tick={{ fill: "#6B7A8D", fontSize: 10 }} axisLine={false} tickLine={false} />
                  <YAxis
                    tick={{ fill: "#6B7A8D", fontSize: 10 }}
                    axisLine={false}
                    tickLine={false}
                    tickFormatter={(v) => `¥${(v / 1000).toFixed(0)}k`}
                  />
                  <Tooltip content={<CustomTooltip />} />
                  <Area type="monotone" dataKey="price" stroke="#50BBE3" strokeWidth={2} fill="rgba(0,212,255,0.10)" name="price" dot={{ fill: "#00D4FF", r: 3 }} />
                </AreaChart>
              </ResponsiveContainer>
            </div>

            {/* Stock trend */}
            <div className="glass-card rounded-xl p-5">
              <div className="flex items-center gap-2 mb-4">
                <Package size={14} style={{ color: "#3295C9" }} />
                <h3 className="text-sm font-semibold" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
                  近7个月库存趋势
                </h3>
              </div>
              <ResponsiveContainer width="100%" height={180}>
                <BarChart data={result.stockTrend} barSize={20}>
                  <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" />
                  <XAxis dataKey="month" tick={{ fill: "#6B7A8D", fontSize: 10 }} axisLine={false} tickLine={false} />
                  <YAxis tick={{ fill: "#6B7A8D", fontSize: 10 }} axisLine={false} tickLine={false} />
                  <Tooltip content={<CustomTooltip />} />
                  <Bar dataKey="stock" radius={[3, 3, 0, 0]} name="stock">
                    {result.stockTrend.map((_, i) => (
                      <Cell key={i} fill={`rgba(0,${102 + i * 15},255,${0.6 + i * 0.05})`} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            </div>
          </div>

          {/* Price comparison bar */}
          <div className="glass-card rounded-xl p-5">
            <div className="flex items-center gap-2 mb-4">
              <BarChart3 size={14} style={{ color: "#C99A45" }} />
              <h3 className="text-sm font-semibold" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
                供应商报价横向对比（CNY）
              </h3>
            </div>
            <div className="space-y-3">
              {result.suppliers
                .filter(s => s.status !== "out")
                .sort((a, b) => a.price_usd - b.price_usd)
                .map((s, i) => {
                  const priceCNY = s.price_usd * EXCHANGE_RATE;
                  const maxPrice = Math.max(...result.suppliers.filter(x => x.status !== "out").map(x => x.price_usd * EXCHANGE_RATE));
                  const pct = (priceCNY / maxPrice) * 100;
                  const isLowest = i === 0;
                  return (
                    <div key={s.id}>
                      <div className="flex items-center justify-between mb-1">
                        <span className="text-xs" style={{ color: "rgba(224,244,255,0.7)", fontFamily: "Noto Sans SC" }}>
                          {s.name}
                          {isLowest && (
                            <span className="ml-2 text-xs px-1.5 py-0.5 rounded" style={{ background: "rgba(70,191,145,0.12)", color: "#46BF91", border: "1px solid rgba(0,255,136,0.2)" }}>
                              最低价
                            </span>
                          )}
                        </span>
                        <span className="text-xs font-mono font-bold" style={{ color: isLowest ? "#46BF91" : "#E0F4FF" }}>
                          ¥{priceCNY.toLocaleString("zh-CN", { maximumFractionDigits: 0 })}
                        </span>
                      </div>
                      <div className="h-2 rounded-full overflow-hidden" style={{ background: "rgba(255,255,255,0.06)" }}>
                        <div
                          className="h-full rounded-full transition-all duration-700"
                          style={{
                            width: `${pct}%`,
                            background: isLowest
                              ? "linear-gradient(90deg, #46BF91, #50BBE3)"
                              : "linear-gradient(90deg, #3295C9, #50BBE3)",
                            boxShadow: isLowest ? "0 0 6px rgba(0,255,136,0.4)" : "0 0 4px rgba(0,212,255,0.3)",
                          }}
                        />
                      </div>
                    </div>
                  );
                })}
            </div>
          </div>
        </div>
      )}

      {/* Empty state */}
      {!result && !searching && (
        <div
          className="rounded-xl p-12 text-center"
          style={{ background: "rgba(0,212,255,0.02)", border: "1px dashed rgba(0,212,255,0.15)" }}
        >
          <Search size={40} style={{ color: "rgba(200,216,232,0.3)", margin: "0 auto 16px" }} />
          <p className="text-sm font-medium mb-2" style={{ color: "rgba(224,244,255,0.4)", fontFamily: "Noto Sans SC" }}>
            输入物料名称或型号开始搜索
          </p>
          <p className="text-xs" style={{ color: "rgba(200,216,232,0.45)", fontFamily: "Noto Sans SC" }}>
            系统将自动匹配多家供应商报价，对比价格、库存与交期
          </p>
        </div>
      )}
    </DashboardLayout>
  );
}
