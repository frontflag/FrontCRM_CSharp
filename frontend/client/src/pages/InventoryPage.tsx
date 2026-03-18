/**
 * InventoryPage — FrontCRM Deep Quantum Theme
 * Inventory management with stock levels, alerts, and product list
 */
import { useState } from "react";
import DashboardLayout from "@/components/DashboardLayout";
import { toast } from "sonner";
import { Search, Plus, AlertTriangle, Package, Eye, ArrowUp, ArrowDown } from "lucide-react";

const products = [
  { id: "P001", name: "iPhone 15 Pro Max 256G", category: "手机", unit: "台", stock: 3, minStock: 10, maxStock: 100, cost: 8999, price: 11999, location: "A-01-01" },
  { id: "P002", name: "联想ThinkPad X1 Carbon", category: "笔记本", unit: "台", stock: 5, minStock: 8, maxStock: 50, cost: 12000, price: 15999, location: "A-02-01" },
  { id: "P003", name: "三星Galaxy S24 Ultra", category: "手机", unit: "台", stock: 2, minStock: 10, maxStock: 80, cost: 7500, price: 9999, location: "A-01-02" },
  { id: "P004", name: "戴尔XPS 15笔记本", category: "笔记本", unit: "台", stock: 6, minStock: 10, maxStock: 40, cost: 10000, price: 13999, location: "A-02-02" },
  { id: "P005", name: "华为MatePad Pro 12.6", category: "平板", unit: "台", stock: 28, minStock: 15, maxStock: 100, cost: 4500, price: 5999, location: "B-01-01" },
  { id: "P006", name: "索尼WH-1000XM5耳机", category: "音频", unit: "个", stock: 45, minStock: 20, maxStock: 200, cost: 1800, price: 2499, location: "C-01-01" },
  { id: "P007", name: "罗技MX Master 3鼠标", category: "外设", unit: "个", stock: 82, minStock: 30, maxStock: 300, cost: 400, price: 699, location: "C-02-01" },
  { id: "P008", name: "苹果AirPods Pro 2", category: "音频", unit: "个", stock: 15, minStock: 20, maxStock: 150, cost: 1500, price: 1999, location: "C-01-02" },
  { id: "P009", name: "小米14 Ultra", category: "手机", unit: "台", stock: 38, minStock: 15, maxStock: 100, cost: 5500, price: 6999, location: "A-01-03" },
  { id: "P010", name: "iPad Air M2", category: "平板", unit: "台", stock: 22, minStock: 10, maxStock: 80, cost: 4200, price: 5499, location: "B-01-02" },
];

const categories = ["全部", "手机", "笔记本", "平板", "音频", "外设"];

export default function InventoryPage() {
  const [search, setSearch] = useState("");
  const [catFilter, setCatFilter] = useState("全部");
  const [showModal, setShowModal] = useState(false);

  const filtered = products.filter((p) => {
    const matchSearch = p.name.includes(search) || p.id.includes(search);
    const matchCat = catFilter === "全部" || p.category === catFilter;
    return matchSearch && matchCat;
  });

  const totalValue = products.reduce((sum, p) => sum + p.stock * p.cost, 0);
  const lowStockCount = products.filter((p) => p.stock < p.minStock).length;
  const totalItems = products.reduce((sum, p) => sum + p.stock, 0);

  const getStockStatus = (p: typeof products[0]) => {
    if (p.stock === 0) return { label: "零库存", color: "#C95745", bg: "rgba(201,87,69,0.12)" };
    if (p.stock < p.minStock) return { label: "低库存", color: "#C99A45", bg: "rgba(201,154,69,0.12)" };
    if (p.stock > p.maxStock * 0.9) return { label: "超库存", color: "#3295C9", bg: "rgba(0,102,255,0.1)" };
    return { label: "正常", color: "#46BF91", bg: "rgba(70,191,145,0.12)" };
  };

  return (
    <DashboardLayout title="库存管理">
      {/* KPI */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        {[
          { label: "商品种类", value: String(products.length), unit: "种", color: "#50BBE3" },
          { label: "库存总量", value: totalItems.toLocaleString(), unit: "件", color: "#3295C9" },
          { label: "库存总值", value: `¥${(totalValue / 10000).toFixed(1)}万`, unit: "", color: "#46BF91" },
          { label: "预警商品", value: String(lowStockCount), unit: "种", color: "#C99A45" },
        ].map((kpi) => (
          <div key={kpi.label} className="glass-card rounded-xl px-5 py-4">
            <p className="text-xs mb-1" style={{ color: "rgba(200,216,232,0.6)", fontFamily: "Noto Sans SC" }}>
              {kpi.label}
            </p>
            <div className="flex items-baseline gap-1">
              <span className="text-xl font-bold" style={{ fontFamily: "Space Mono", color: kpi.color }}>
                {kpi.value}
              </span>
              {kpi.unit && <span className="text-xs" style={{ color: "rgba(224,244,255,0.4)" }}>{kpi.unit}</span>}
            </div>
          </div>
        ))}
      </div>

      {/* Table */}
      <div className="glass-card rounded-xl p-5">
        {/* Toolbar */}
        <div className="flex flex-wrap items-center gap-3 mb-5">
          <h3 className="text-sm font-semibold" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
            商品库存列表
          </h3>

          <div
            className="flex items-center gap-2 px-3 py-2 rounded flex-1 min-w-[180px] max-w-xs"
            style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.15)" }}
          >
            <Search size={13} style={{ color: "rgba(200,216,232,0.55)" }} />
            <input
              type="text"
              placeholder="搜索商品名称/编号..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="flex-1 bg-transparent text-xs outline-none"
              style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
            />
          </div>

          <div className="flex items-center gap-1.5">
            {categories.map((c) => (
              <button
                key={c}
                onClick={() => setCatFilter(c)}
                className="px-3 py-1.5 rounded text-xs transition-all"
                style={{
                  background: catFilter === c ? "rgba(0,212,255,0.15)" : "rgba(0,212,255,0.04)",
                  border: `1px solid ${catFilter === c ? "rgba(0,212,255,0.4)" : "rgba(0,212,255,0.12)"}`,
                  color: catFilter === c ? "#50BBE3" : "rgba(224,244,255,0.5)",
                  fontFamily: "Noto Sans SC",
                }}
              >
                {c}
              </button>
            ))}
          </div>

          <div className="ml-auto flex gap-2">
            <button
              className="flex items-center gap-1.5 px-3 py-2 rounded text-xs"
              style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.15)", color: "rgba(0,212,255,0.6)" }}
              onClick={() => toast.info("入库功能即将上线")}
            >
              <ArrowDown size={13} />
              入库
            </button>
            <button
              className="flex items-center gap-1.5 px-3 py-2 rounded text-xs"
              style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.15)", color: "rgba(0,212,255,0.6)" }}
              onClick={() => toast.info("出库功能即将上线")}
            >
              <ArrowUp size={13} />
              出库
            </button>
            <button
              className="flex items-center gap-1.5 px-4 py-2 rounded text-xs font-medium"
              style={{
                background: "linear-gradient(135deg, rgba(0,212,255,0.2), rgba(0,102,255,0.2))",
                border: "1px solid rgba(0,212,255,0.4)",
                color: "#50BBE3",
                fontFamily: "Noto Sans SC",
              }}
              onClick={() => setShowModal(true)}
            >
              <Plus size={13} />
              新增商品
            </button>
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-xs">
            <thead>
              <tr style={{ borderBottom: "1px solid rgba(0,212,255,0.12)" }}>
                {["商品编号", "商品名称", "分类", "单位", "当前库存", "库存状态", "库存进度", "成本价", "售价", "库位", "操作"].map((h) => (
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
              {filtered.map((p) => {
                const status = getStockStatus(p);
                const progress = Math.min((p.stock / p.maxStock) * 100, 100);
                return (
                  <tr
                    key={p.id}
                    className="table-row-hover"
                    style={{ borderBottom: "1px solid rgba(0,212,255,0.05)" }}
                  >
                    <td className="py-3 pr-3 font-mono" style={{ color: "#50BBE3" }}>{p.id}</td>
                    <td className="py-3 pr-3" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC", maxWidth: "160px" }}>
                      <div className="flex items-center gap-2">
                        <Package size={12} style={{ color: "rgba(200,216,232,0.55)", flexShrink: 0 }} />
                        <span className="truncate">{p.name}</span>
                      </div>
                    </td>
                    <td className="py-3 pr-3">
                      <span
                        className="px-2 py-0.5 rounded text-xs"
                        style={{ background: "#162233", color: "#50BBE3", border: "1px solid rgba(0,212,255,0.2)", fontFamily: "Noto Sans SC" }}
                      >
                        {p.category}
                      </span>
                    </td>
                    <td className="py-3 pr-3" style={{ color: "rgba(224,244,255,0.6)", fontFamily: "Noto Sans SC" }}>{p.unit}</td>
                    <td className="py-3 pr-3">
                      <div className="flex items-center gap-1">
                        {p.stock < p.minStock && (
                          <AlertTriangle size={11} style={{ color: "#C99A45" }} />
                        )}
                        <span className="font-mono font-bold" style={{ color: p.stock < p.minStock ? "#C99A45" : "#E0F4FF" }}>
                          {p.stock}
                        </span>
                      </div>
                    </td>
                    <td className="py-3 pr-3">
                      <span
                        className="px-2 py-0.5 rounded text-xs"
                        style={{ background: status.bg, color: status.color, border: `1px solid ${status.color}30`, fontFamily: "Noto Sans SC" }}
                      >
                        {status.label}
                      </span>
                    </td>
                    <td className="py-3 pr-3" style={{ minWidth: "80px" }}>
                      <div className="h-1.5 rounded-full overflow-hidden" style={{ background: "rgba(255,255,255,0.08)" }}>
                        <div
                          className="h-full rounded-full"
                          style={{
                            width: `${progress}%`,
                            background: p.stock < p.minStock
                              ? "linear-gradient(90deg, #C95745, #C99A45)"
                              : "linear-gradient(90deg, #50BBE3, #3295C9)",
                            boxShadow: `0 0 4px ${p.stock < p.minStock ? "#C99A45" : "#00D4FF"}`,
                          }}
                        />
                      </div>
                      <div className="text-xs mt-0.5" style={{ color: "rgba(200,216,232,0.5)" }}>
                        {p.stock}/{p.maxStock}
                      </div>
                    </td>
                    <td className="py-3 pr-3 font-mono" style={{ color: "rgba(224,244,255,0.6)" }}>
                      ¥{p.cost.toLocaleString()}
                    </td>
                    <td className="py-3 pr-3 font-mono" style={{ color: "#E0F4FF" }}>
                      ¥{p.price.toLocaleString()}
                    </td>
                    <td className="py-3 pr-3 font-mono" style={{ color: "rgba(200,216,232,0.6)" }}>{p.location}</td>
                    <td className="py-3">
                      <button
                        className="flex items-center gap-1 px-2 py-1 rounded text-xs"
                        style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.15)", color: "#50BBE3" }}
                        onClick={() => toast.info(`查看商品 ${p.name}`)}
                      >
                        <Eye size={11} />
                        详情
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        <div className="flex items-center justify-between mt-4 pt-4" style={{ borderTop: "1px solid rgba(0,212,255,0.08)" }}>
          <span className="text-xs" style={{ color: "rgba(200,216,232,0.55)" }}>共 {filtered.length} 种商品</span>
          <div className="flex items-center gap-1">
            {[1, 2].map((p) => (
              <button
                key={p}
                className="w-7 h-7 rounded text-xs"
                style={{
                  background: p === 1 ? "rgba(0,212,255,0.15)" : "rgba(0,212,255,0.04)",
                  border: `1px solid ${p === 1 ? "rgba(0,212,255,0.4)" : "rgba(0,212,255,0.12)"}`,
                  color: p === 1 ? "#50BBE3" : "rgba(224,244,255,0.4)",
                }}
              >
                {p}
              </button>
            ))}
          </div>
        </div>
      </div>

      {showModal && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center"
          style={{ background: "#192A3F", backdropFilter: "blur(8px)" }}
          onClick={() => setShowModal(false)}
        >
          <div
            className="w-full max-w-lg mx-4 rounded-xl p-6"
            style={{ background: "#0A1628", border: "1px solid rgba(0,212,255,0.25)", boxShadow: "0 0 60px rgba(0,212,255,0.1)" }}
            onClick={(e) => e.stopPropagation()}
          >
            <h3 className="text-base font-semibold mb-5" style={{ color: "#50BBE3", fontFamily: "Orbitron" }}>
              新增商品
            </h3>
            <div className="grid grid-cols-2 gap-4">
              {["商品名称", "商品分类", "单位", "成本价", "售价", "最低库存", "最高库存", "库位"].map((label) => (
                <div key={label}>
                  <label className="block text-xs mb-1.5" style={{ color: "rgba(0,212,255,0.6)", fontFamily: "Noto Sans SC" }}>{label}</label>
                  <input
                    type="text"
                    className="w-full px-3 py-2.5 rounded text-xs outline-none"
                    style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.2)", color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
                  />
                </div>
              ))}
            </div>
            <div className="flex gap-3 mt-6">
              <button
                className="flex-1 py-2.5 rounded text-xs"
                style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.15)", color: "rgba(0,212,255,0.6)", fontFamily: "Noto Sans SC" }}
                onClick={() => setShowModal(false)}
              >
                取消
              </button>
              <button
                className="flex-1 py-2.5 rounded text-xs font-medium"
                style={{ background: "linear-gradient(135deg, rgba(0,212,255,0.2), rgba(0,102,255,0.2))", border: "1px solid rgba(0,212,255,0.4)", color: "#50BBE3", fontFamily: "Noto Sans SC" }}
                onClick={() => { setShowModal(false); toast.success("商品添加成功"); }}
              >
                确认添加
              </button>
            </div>
          </div>
        </div>
      )}
    </DashboardLayout>
  );
}
