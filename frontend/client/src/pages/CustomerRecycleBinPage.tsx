/**
 * CustomerRecycleBinPage — FrontCRM Deep Quantum Theme
 * 客户回收站：显示已删除客户，支持恢复为正常状态
 * Design: Deep Quantum (#192A3F bg, #0A1628 card, #162233 inner panel)
 */
import { useState, useEffect } from "react";
import { useLocation } from "wouter";
import DashboardLayout from "@/components/DashboardLayout";
import { toast } from "sonner";
import {
  ArrowLeft, Trash2, RotateCcw, Loader2, Search, Building2, Calendar, User, FileText,
} from "lucide-react";
import { customerApi } from "@/lib/customerApi";

type DeletedCustomer = {
  id: string;
  customerCode: string;
  customerName: string;
  officialName?: string;
  customerLevel?: string;
  industry?: string;
  deletedAt?: string;
  deletedByUserName?: string;
  deleteReason?: string;
};

const LEVEL_MAP: Record<string, { label: string; color: string }> = {
  "D":   { label: "D级",  color: "rgba(200,216,232,0.6)" },
  "C":   { label: "C级",  color: "#50BBE3" },
  "B":   { label: "B级",  color: "#3295C9" },
  "BPO": { label: "BPO",  color: "#6DBFA0" },
  "VIP": { label: "VIP",  color: "#C9A96E" },
  "VPO": { label: "VPO",  color: "#C97A6E" },
};

export default function CustomerRecycleBinPage() {
  const [, navigate] = useLocation();
  const [customers, setCustomers] = useState<DeletedCustomer[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [restoringId, setRestoringId] = useState<string | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setLoading(true);
    const res = await customerApi.getRecycleBin(1, 100);
    setLoading(false);
    if (res.success && res.data) {
      const data = res.data as { items: DeletedCustomer[] };
      setCustomers(data.items || []);
    } else {
      toast.error("加载回收站失败");
    }
  };

  const handleRestore = async (id: string, name: string) => {
    setRestoringId(id);
    const res = await customerApi.restoreCustomer(id);
    setRestoringId(null);
    if (res.success) {
      toast.success(`已恢复客户：${name}`);
      setCustomers((prev) => prev.filter((c) => c.id !== id));
    } else {
      toast.error(res.message || "恢复失败");
    }
  };

  const filtered = customers.filter((c) => {
    const q = search.toLowerCase();
    return (
      !q ||
      (c.customerCode || "").toLowerCase().includes(q) ||
      (c.customerName || "").toLowerCase().includes(q) ||
      (c.officialName || "").toLowerCase().includes(q)
    );
  });

  return (
    <DashboardLayout>
      <div className="max-w-5xl mx-auto px-4 py-6 space-y-5">
        {/* Header */}
        <div className="flex items-center gap-4">
          <button
            onClick={() => navigate("/customer")}
            className="flex items-center gap-2 px-3 py-2 rounded-lg text-xs transition-all"
            style={{ background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.08)", color: "rgba(224,244,255,0.6)", fontFamily: "Noto Sans SC" }}
          >
            <ArrowLeft size={13} />
            返回客户列表
          </button>
          <div>
            <h1 className="text-lg font-bold flex items-center gap-2" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
              <Trash2 size={18} style={{ color: "#C95745" }} />
              客户回收站
            </h1>
            <p className="text-xs mt-0.5" style={{ color: "rgba(200,216,232,0.45)", fontFamily: "Noto Sans SC" }}>
              已删除的客户可在此恢复为正常状态
            </p>
          </div>
        </div>

        {/* Search Bar */}
        <div className="relative">
          <Search size={13} className="absolute left-3 top-1/2 -translate-y-1/2" style={{ color: "rgba(200,216,232,0.4)" }} />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="搜索客户编号、名称..."
            className="w-full pl-8 pr-4 py-2.5 rounded-lg text-xs outline-none"
            style={{ background: "#0A1628", border: "1px solid rgba(0,212,255,0.15)", color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
          />
        </div>

        {/* Stats */}
        <div className="flex items-center gap-3">
          <span className="text-xs px-3 py-1.5 rounded-full" style={{ background: "rgba(201,87,69,0.1)", border: "1px solid rgba(201,87,69,0.3)", color: "#C95745", fontFamily: "Noto Sans SC" }}>
            共 {customers.length} 条已删除记录
          </span>
          {search && (
            <span className="text-xs" style={{ color: "rgba(200,216,232,0.4)", fontFamily: "Noto Sans SC" }}>
              筛选后 {filtered.length} 条
            </span>
          )}
        </div>

        {/* Content */}
        {loading ? (
          <div className="flex items-center justify-center py-20">
            <Loader2 size={24} className="animate-spin" style={{ color: "#00D4FF" }} />
            <span className="ml-3 text-sm" style={{ color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>加载中...</span>
          </div>
        ) : filtered.length === 0 ? (
          <div className="text-center py-20 rounded-xl" style={{ background: "#0A1628", border: "1px solid rgba(0,212,255,0.1)" }}>
            <Trash2 size={40} className="mx-auto mb-3 opacity-20" style={{ color: "#C95745" }} />
            <p className="text-sm" style={{ color: "rgba(200,216,232,0.4)", fontFamily: "Noto Sans SC" }}>
              {search ? "没有匹配的已删除客户" : "回收站为空"}
            </p>
          </div>
        ) : (
          <div className="space-y-3">
            {filtered.map((c) => {
              const levelInfo = LEVEL_MAP[c.customerLevel || "D"] || LEVEL_MAP["D"];
              return (
                <div
                  key={c.id}
                  className="rounded-xl p-4"
                  style={{ background: "#0A1628", border: "1px solid rgba(201,87,69,0.2)" }}
                >
                  <div className="flex items-start justify-between gap-4">
                    {/* Left: Customer Info */}
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-1.5">
                        <span className="text-xs font-mono" style={{ color: "rgba(200,216,232,0.4)" }}>{c.customerCode}</span>
                        <span className="text-xs px-1.5 py-0.5 rounded" style={{ color: levelInfo.color, background: `${levelInfo.color}18`, border: `1px solid ${levelInfo.color}40`, fontFamily: "Noto Sans SC" }}>
                          {levelInfo.label}
                        </span>
                        <span className="text-xs px-1.5 py-0.5 rounded" style={{ background: "rgba(201,87,69,0.1)", color: "#C95745", border: "1px solid rgba(201,87,69,0.3)", fontFamily: "Noto Sans SC" }}>
                          已删除
                        </span>
                      </div>
                      <h3 className="text-sm font-semibold truncate" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
                        {c.officialName || c.customerName}
                      </h3>
                      {c.industry && (
                        <p className="text-xs mt-0.5" style={{ color: "rgba(200,216,232,0.4)", fontFamily: "Noto Sans SC" }}>
                          <Building2 size={11} className="inline mr-1" />
                          {c.industry}
                        </p>
                      )}

                      {/* Delete Info */}
                      <div className="mt-3 p-3 rounded-lg space-y-1.5" style={{ background: "#162233", border: "1px solid rgba(201,87,69,0.15)" }}>
                        {c.deletedAt && (
                          <div className="flex items-center gap-2 text-xs" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>
                            <Calendar size={11} style={{ color: "#C95745" }} />
                            <span>删除时间：{new Date(c.deletedAt).toLocaleString("zh-CN")}</span>
                          </div>
                        )}
                        {c.deletedByUserName && (
                          <div className="flex items-center gap-2 text-xs" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>
                            <User size={11} style={{ color: "#C95745" }} />
                            <span>删除人：{c.deletedByUserName}</span>
                          </div>
                        )}
                        {c.deleteReason && (
                          <div className="flex items-start gap-2 text-xs" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>
                            <FileText size={11} className="mt-0.5 flex-shrink-0" style={{ color: "#C95745" }} />
                            <span>删除理由：{c.deleteReason}</span>
                          </div>
                        )}
                      </div>
                    </div>

                    {/* Right: Restore Button */}
                    <button
                      onClick={() => handleRestore(c.id, c.officialName || c.customerName)}
                      disabled={restoringId === c.id}
                      className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-xs font-medium flex-shrink-0 transition-all"
                      style={{
                        background: "rgba(0,212,255,0.08)",
                        border: "1px solid rgba(0,212,255,0.3)",
                        color: "#00D4FF",
                        fontFamily: "Noto Sans SC",
                      }}
                    >
                      {restoringId === c.id ? (
                        <Loader2 size={13} className="animate-spin" />
                      ) : (
                        <RotateCcw size={13} />
                      )}
                      恢复客户
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </DashboardLayout>
  );
}
