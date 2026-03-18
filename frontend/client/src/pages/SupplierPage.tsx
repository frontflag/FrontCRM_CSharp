/**
 * SupplierPage — FrontCRM Deep Quantum Theme
 * Supplier management with contact info, rating, and transaction history
 */
import { useState } from "react";
import DashboardLayout from "@/components/DashboardLayout";
import { toast } from "sonner";
import { Search, Plus, Star, Phone, Mail, MapPin, Eye, Building2 } from "lucide-react";

const suppliers = [
  { id: "S001", name: "华联电子科技有限公司", contact: "王建国", phone: "13800138001", email: "wjg@hualian.com", address: "深圳市南山区科技园", category: "电子元件", rating: 5, orders: 48, amount: 1280000, status: "合作中" },
  { id: "S002", name: "联创物资贸易公司", contact: "李美华", phone: "13900139002", email: "lmh@lianchuang.com", address: "广州市天河区", category: "办公物资", rating: 4, orders: 32, amount: 680000, status: "合作中" },
  { id: "S003", name: "东方供应链管理集团", contact: "张伟", phone: "13700137003", email: "zw@dongfang.com", address: "上海市浦东新区", category: "综合物资", rating: 5, orders: 85, amount: 3200000, status: "合作中" },
  { id: "S004", name: "深圳优质电子器件", contact: "陈小红", phone: "13600136004", email: "cxh@youzhi.com", address: "深圳市宝安区", category: "电子元件", rating: 3, orders: 18, amount: 320000, status: "暂停" },
  { id: "S005", name: "广州物流集团股份", contact: "刘大明", phone: "13500135005", email: "ldm@gzwl.com", address: "广州市白云区", category: "物流服务", rating: 4, orders: 56, amount: 980000, status: "合作中" },
  { id: "S006", name: "北京科技材料公司", contact: "赵丽", phone: "13400134006", email: "zl@bjkj.com", address: "北京市海淀区", category: "科技材料", rating: 5, orders: 72, amount: 2560000, status: "合作中" },
  { id: "S007", name: "上海精密仪器制造", contact: "孙浩", phone: "13300133007", email: "sh@jingmi.com", address: "上海市闵行区", category: "精密仪器", rating: 4, orders: 24, amount: 860000, status: "合作中" },
  { id: "S008", name: "成都电子元件厂", contact: "周明", phone: "13200132008", email: "zm@cddzj.com", address: "成都市武侯区", category: "电子元件", rating: 3, orders: 15, amount: 250000, status: "待评估" },
];

const statusColors: Record<string, { color: string; bg: string }> = {
  "合作中": { color: "#00FF88", bg: "rgba(0,255,136,0.1)" },
  "暂停": { color: "#FFA500", bg: "rgba(255,165,0,0.1)" },
  "待评估": { color: "#00D4FF", bg: "rgba(0,212,255,0.1)" },
};

function StarRating({ rating }: { rating: number }) {
  return (
    <div className="flex items-center gap-0.5">
      {[1, 2, 3, 4, 5].map((s) => (
        <Star
          key={s}
          size={11}
          fill={s <= rating ? "#FFA500" : "transparent"}
          style={{ color: s <= rating ? "#FFA500" : "rgba(255,165,0,0.2)" }}
        />
      ))}
    </div>
  );
}

export default function SupplierPage() {
  const [search, setSearch] = useState("");
  const [showModal, setShowModal] = useState(false);
  const [viewSupplier, setViewSupplier] = useState<typeof suppliers[0] | null>(null);

  const filtered = suppliers.filter(
    (s) => s.name.includes(search) || s.contact.includes(search) || s.id.includes(search)
  );

  return (
    <DashboardLayout title="供应商管理">
      {/* KPI */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        {[
          { label: "供应商总数", value: String(suppliers.length), unit: "家", color: "#00D4FF" },
          { label: "合作中", value: String(suppliers.filter(s => s.status === "合作中").length), unit: "家", color: "#00FF88" },
          { label: "采购总额", value: `¥${(suppliers.reduce((s, v) => s + v.amount, 0) / 10000).toFixed(0)}万`, unit: "", color: "#3295C9" },
          { label: "五星供应商", value: String(suppliers.filter(s => s.rating === 5).length), unit: "家", color: "#FFA500" },
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

      {/* Card grid */}
      <div className="glass-card rounded-xl p-5">
        <div className="flex flex-wrap items-center gap-3 mb-5">
          <h3 className="text-sm font-semibold" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>供应商列表</h3>
          <div
            className="flex items-center gap-2 px-3 py-2 rounded flex-1 min-w-[180px] max-w-xs"
            style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.15)" }}
          >
            <Search size={13} style={{ color: "rgba(200,216,232,0.55)" }} />
            <input
              type="text"
              placeholder="搜索供应商名称/联系人..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="flex-1 bg-transparent text-xs outline-none"
              style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
            />
          </div>
          <button
            className="ml-auto flex items-center gap-1.5 px-4 py-2 rounded text-xs font-medium"
            style={{
              background: "linear-gradient(135deg, rgba(0,212,255,0.2), rgba(0,102,255,0.2))",
              border: "1px solid rgba(0,212,255,0.4)",
              color: "#00D4FF",
              fontFamily: "Noto Sans SC",
            }}
            onClick={() => setShowModal(true)}
          >
            <Plus size={13} />
            新增供应商
          </button>
        </div>

        {/* Supplier cards grid */}
        <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-4">
          {filtered.map((s) => {
            const sc = statusColors[s.status] || statusColors["待评估"];
            return (
              <div
                key={s.id}
                className="rounded-xl p-4 transition-all duration-200 cursor-pointer"
                style={{
                  background: "#162233",
                  border: "1px solid rgba(0,212,255,0.1)",
                }}
                onMouseEnter={(e) => {
                  (e.currentTarget as HTMLElement).style.borderColor = "rgba(0,212,255,0.3)";
                  (e.currentTarget as HTMLElement).style.background = "rgba(0,212,255,0.06)";
                }}
                onMouseLeave={(e) => {
                  (e.currentTarget as HTMLElement).style.borderColor = "rgba(0,212,255,0.1)";
                  (e.currentTarget as HTMLElement).style.background = "rgba(0,212,255,0.03)";
                }}
                onClick={() => setViewSupplier(s)}
              >
                {/* Header */}
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center gap-3">
                    <div
                      className="w-10 h-10 rounded-lg flex items-center justify-center flex-shrink-0"
                      style={{ background: "rgba(0,212,255,0.1)", border: "1px solid rgba(0,212,255,0.2)" }}
                    >
                      <Building2 size={18} style={{ color: "#00D4FF" }} />
                    </div>
                    <div>
                      <p className="text-xs font-semibold leading-tight" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
                        {s.name}
                      </p>
                      <p className="text-xs mt-0.5" style={{ color: "rgba(200,216,232,0.55)" }}>{s.id}</p>
                    </div>
                  </div>
                  <span
                    className="text-xs px-2 py-0.5 rounded flex-shrink-0"
                    style={{ background: sc.bg, color: sc.color, border: `1px solid ${sc.color}30`, fontFamily: "Noto Sans SC" }}
                  >
                    {s.status}
                  </span>
                </div>

                {/* Rating */}
                <div className="flex items-center justify-between mb-3">
                  <StarRating rating={s.rating} />
                  <span
                    className="text-xs px-2 py-0.5 rounded"
                    style={{ background: "#162233", color: "#00D4FF", border: "1px solid rgba(0,212,255,0.15)", fontFamily: "Noto Sans SC" }}
                  >
                    {s.category}
                  </span>
                </div>

                {/* Contact info */}
                <div className="space-y-1.5 mb-3">
                  <div className="flex items-center gap-2 text-xs">
                    <Phone size={11} style={{ color: "rgba(200,216,232,0.55)", flexShrink: 0 }} />
                    <span style={{ color: "rgba(224,244,255,0.6)", fontFamily: "Noto Sans SC" }}>{s.contact}</span>
                    <span style={{ color: "rgba(200,216,232,0.55)" }}>{s.phone}</span>
                  </div>
                  <div className="flex items-center gap-2 text-xs">
                    <MapPin size={11} style={{ color: "rgba(200,216,232,0.55)", flexShrink: 0 }} />
                    <span className="truncate" style={{ color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>{s.address}</span>
                  </div>
                </div>

                {/* Stats */}
                <div
                  className="flex items-center justify-between pt-3"
                  style={{ borderTop: "1px solid rgba(0,212,255,0.08)" }}
                >
                  <div>
                    <p className="text-xs" style={{ color: "rgba(200,216,232,0.55)" }}>采购单数</p>
                    <p className="text-sm font-bold font-mono" style={{ color: "#00D4FF" }}>{s.orders}</p>
                  </div>
                  <div className="text-right">
                    <p className="text-xs" style={{ color: "rgba(200,216,232,0.55)" }}>累计采购额</p>
                    <p className="text-sm font-bold font-mono" style={{ color: "#E0F4FF" }}>
                      ¥{(s.amount / 10000).toFixed(1)}万
                    </p>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* Detail Modal */}
      {viewSupplier && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center"
          style={{ background: "#192A3F", backdropFilter: "blur(8px)" }}
          onClick={() => setViewSupplier(null)}
        >
          <div
            className="w-full max-w-lg mx-4 rounded-xl p-6"
            style={{ background: "#0A1628", border: "1px solid rgba(0,212,255,0.25)", boxShadow: "0 0 60px rgba(0,212,255,0.1)" }}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center gap-3 mb-5">
              <div
                className="w-12 h-12 rounded-xl flex items-center justify-center"
                style={{ background: "rgba(0,212,255,0.1)", border: "1px solid rgba(0,212,255,0.3)" }}
              >
                <Building2 size={22} style={{ color: "#00D4FF" }} />
              </div>
              <div>
                <h3 className="text-sm font-semibold" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
                  {viewSupplier.name}
                </h3>
                <StarRating rating={viewSupplier.rating} />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              {[
                { label: "供应商编号", value: viewSupplier.id },
                { label: "供应商分类", value: viewSupplier.category },
                { label: "联系人", value: viewSupplier.contact },
                { label: "联系电话", value: viewSupplier.phone },
                { label: "电子邮箱", value: viewSupplier.email },
                { label: "合作状态", value: viewSupplier.status },
                { label: "采购单数", value: `${viewSupplier.orders} 张` },
                { label: "累计采购额", value: `¥${viewSupplier.amount.toLocaleString()}` },
              ].map((item) => (
                <div key={item.label} className="p-3 rounded-lg" style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.1)" }}>
                  <p className="text-xs mb-1" style={{ color: "rgba(200,216,232,0.6)", fontFamily: "Noto Sans SC" }}>{item.label}</p>
                  <p className="text-xs font-medium" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>{item.value}</p>
                </div>
              ))}
            </div>
            <div className="mt-3 p-3 rounded-lg" style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.1)" }}>
              <p className="text-xs mb-1" style={{ color: "rgba(200,216,232,0.6)", fontFamily: "Noto Sans SC" }}>地址</p>
              <p className="text-xs" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>{viewSupplier.address}</p>
            </div>
            <button
              className="w-full mt-4 py-2.5 rounded text-xs"
              style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.15)", color: "rgba(0,212,255,0.6)", fontFamily: "Noto Sans SC" }}
              onClick={() => setViewSupplier(null)}
            >
              关闭
            </button>
          </div>
        </div>
      )}

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
            <h3 className="text-base font-semibold mb-5" style={{ color: "#00D4FF", fontFamily: "Orbitron" }}>新增供应商</h3>
            <div className="grid grid-cols-2 gap-4">
              {["供应商名称", "供应商分类", "联系人", "联系电话", "电子邮箱", "地址"].map((label) => (
                <div key={label} className={label === "地址" || label === "供应商名称" ? "col-span-2" : ""}>
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
                style={{ background: "linear-gradient(135deg, rgba(0,212,255,0.2), rgba(0,102,255,0.2))", border: "1px solid rgba(0,212,255,0.4)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}
                onClick={() => { setShowModal(false); toast.success("供应商添加成功"); }}
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
