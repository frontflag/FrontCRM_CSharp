/**
 * RequirementPage — FrontCRM Deep Quantum Theme
 * 采购需求管理：需求录入、状态跟踪、匹配报价入口
 */
import { useState } from "react";
import DashboardLayout from "@/components/DashboardLayout";
import { toast } from "sonner";
import { useLocation } from "wouter";
import {
  Search, Plus, FileText, Clock, CheckCircle2, XCircle,
  AlertTriangle, ChevronRight, Zap, Package, Calendar, User
} from "lucide-react";

const requirements = [
  {
    id: "REQ-2026-001", title: "Q1 手机类备货需求", creator: "张采购", dept: "采购部",
    createDate: "2026-03-01", deadline: "2026-03-15", priority: "高",
    status: "待报价", items: 8, totalQty: 500, estimatedAmount: 2800000,
    description: "Q1季度手机类产品备货，包含iPhone、三星、华为等主流机型",
  },
  {
    id: "REQ-2026-002", title: "笔记本电脑补货申请", creator: "李采购", dept: "采购部",
    createDate: "2026-03-03", deadline: "2026-03-20", priority: "中",
    status: "报价中", items: 5, totalQty: 200, estimatedAmount: 1500000,
    description: "笔记本电脑库存告急，需紧急补货，优先联想、戴尔品牌",
  },
  {
    id: "REQ-2026-003", title: "音频设备季度采购", creator: "王采购", dept: "采购部",
    createDate: "2026-03-05", deadline: "2026-03-25", priority: "低",
    status: "已完成", items: 12, totalQty: 800, estimatedAmount: 960000,
    description: "季度音频设备采购，含耳机、音箱等品类",
  },
  {
    id: "REQ-2026-004", title: "外设配件紧急补货", creator: "陈采购", dept: "采购部",
    createDate: "2026-03-07", deadline: "2026-03-12", priority: "紧急",
    status: "待审批", items: 6, totalQty: 1200, estimatedAmount: 480000,
    description: "鼠标、键盘、耳机等外设配件库存严重不足，需紧急处理",
  },
  {
    id: "REQ-2026-005", title: "平板电脑新品引进", creator: "刘采购", dept: "采购部",
    createDate: "2026-03-08", deadline: "2026-03-30", priority: "中",
    status: "待报价", items: 4, totalQty: 300, estimatedAmount: 1200000,
    description: "引进最新款平板电脑产品线，包含iPad系列和华为MatePad",
  },
  {
    id: "REQ-2026-006", title: "智能穿戴设备采购", creator: "赵采购", dept: "采购部",
    createDate: "2026-03-09", deadline: "2026-04-01", priority: "低",
    status: "草稿", items: 3, totalQty: 150, estimatedAmount: 320000,
    description: "智能手表、手环等穿戴设备首次引进测试",
  },
];

const statusConfig: Record<string, { color: string; bg: string; icon: React.ElementType }> = {
  "草稿": { color: "rgba(224,244,255,0.4)", bg: "rgba(224,244,255,0.06)", icon: FileText },
  "待审批": { color: "#C99A45", bg: "rgba(201,154,69,0.12)", icon: Clock },
  "待报价": { color: "#50BBE3", bg: "rgba(0,212,255,0.1)", icon: Zap },
  "报价中": { color: "#3295C9", bg: "rgba(0,102,255,0.15)", icon: AlertTriangle },
  "已完成": { color: "#46BF91", bg: "rgba(70,191,145,0.12)", icon: CheckCircle2 },
  "已取消": { color: "#C95745", bg: "rgba(201,87,69,0.12)", icon: XCircle },
};

const priorityConfig: Record<string, { color: string; bg: string }> = {
  "紧急": { color: "#C95745", bg: "rgba(255,107,53,0.12)" },
  "高": { color: "#C99A45", bg: "rgba(201,154,69,0.12)" },
  "中": { color: "#50BBE3", bg: "rgba(0,212,255,0.1)" },
  "低": { color: "rgba(224,244,255,0.4)", bg: "rgba(224,244,255,0.06)" },
};

export default function RequirementPage() {
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("全部");
  const [showModal, setShowModal] = useState(false);
  const [, navigate] = useLocation();

  const statuses = ["全部", "草稿", "待审批", "待报价", "报价中", "已完成"];

  const filtered = requirements.filter((r) => {
    const matchSearch = r.title.includes(search) || r.id.includes(search);
    const matchStatus = statusFilter === "全部" || r.status === statusFilter;
    return matchSearch && matchStatus;
  });

  const kpis = [
    { label: "需求总数", value: String(requirements.length), unit: "条", color: "#50BBE3" },
    { label: "待报价", value: String(requirements.filter(r => r.status === "待报价").length), unit: "条", color: "#C99A45" },
    { label: "报价中", value: String(requirements.filter(r => r.status === "报价中").length), unit: "条", color: "#3295C9" },
    { label: "预计总金额", value: `¥${(requirements.reduce((s, r) => s + r.estimatedAmount, 0) / 10000).toFixed(0)}万`, unit: "", color: "#46BF91" },
  ];

  return (
    <DashboardLayout title="需求管理">
      {/* KPI */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        {kpis.map((kpi) => (
          <div key={kpi.label} className="glass-card rounded-xl px-5 py-4">
            <p className="text-xs mb-1" style={{ color: "rgba(200,216,232,0.6)", fontFamily: "Noto Sans SC" }}>{kpi.label}</p>
            <div className="flex items-baseline gap-1">
              <span className="text-xl font-bold" style={{ fontFamily: "Space Mono", color: kpi.color }}>{kpi.value}</span>
              {kpi.unit && <span className="text-xs" style={{ color: "rgba(224,244,255,0.4)" }}>{kpi.unit}</span>}
            </div>
          </div>
        ))}
      </div>

      {/* List */}
      <div className="glass-card rounded-xl p-5">
        {/* Toolbar */}
        <div className="flex flex-wrap items-center gap-3 mb-5">
          <h3 className="text-sm font-semibold" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>采购需求列表</h3>
          <div
            className="flex items-center gap-2 px-3 py-2 rounded flex-1 min-w-[180px] max-w-xs"
            style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.15)" }}
          >
            <Search size={13} style={{ color: "rgba(200,216,232,0.55)" }} />
            <input
              type="text"
              placeholder="搜索需求标题/编号..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="flex-1 bg-transparent text-xs outline-none"
              style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
            />
          </div>
          <div className="flex items-center gap-1.5 flex-wrap">
            {statuses.map((s) => (
              <button
                key={s}
                onClick={() => setStatusFilter(s)}
                className="px-3 py-1.5 rounded text-xs transition-all"
                style={{
                  background: statusFilter === s ? "rgba(0,212,255,0.15)" : "rgba(0,212,255,0.04)",
                  border: `1px solid ${statusFilter === s ? "rgba(0,212,255,0.4)" : "rgba(0,212,255,0.12)"}`,
                  color: statusFilter === s ? "#50BBE3" : "rgba(224,244,255,0.5)",
                  fontFamily: "Noto Sans SC",
                }}
              >
                {s}
              </button>
            ))}
          </div>
          <button
            className="ml-auto flex items-center gap-1.5 px-4 py-2 rounded text-xs font-medium"
            style={{
              background: "linear-gradient(135deg, rgba(0,212,255,0.2), rgba(0,102,255,0.2))",
              border: "1px solid rgba(0,212,255,0.4)",
              color: "#50BBE3",
              fontFamily: "Noto Sans SC",
            }}
            onClick={() => setShowModal(true)}
          >
            <Plus size={13} />
            新建需求
          </button>
        </div>

        {/* Requirement cards */}
        <div className="space-y-3">
          {filtered.map((req) => {
            const sc = statusConfig[req.status] || statusConfig["草稿"];
            const pc = priorityConfig[req.priority] || priorityConfig["低"];
            const StatusIcon = sc.icon;
            return (
              <div
                key={req.id}
                className="rounded-xl p-4 transition-all duration-200"
                style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.1)" }}
                onMouseEnter={(e) => {
                  (e.currentTarget as HTMLElement).style.borderColor = "rgba(0,212,255,0.25)";
                  (e.currentTarget as HTMLElement).style.background = "rgba(0,212,255,0.05)";
                }}
                onMouseLeave={(e) => {
                  (e.currentTarget as HTMLElement).style.borderColor = "rgba(0,212,255,0.1)";
                  (e.currentTarget as HTMLElement).style.background = "rgba(0,212,255,0.03)";
                }}
              >
                <div className="flex items-start justify-between gap-4">
                  {/* Left info */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-1.5 flex-wrap">
                      <span className="font-mono text-xs" style={{ color: "rgba(200,216,232,0.6)" }}>{req.id}</span>
                      <span
                        className="text-xs px-2 py-0.5 rounded"
                        style={{ background: pc.bg, color: pc.color, border: `1px solid ${pc.color}30`, fontFamily: "Noto Sans SC" }}
                      >
                        {req.priority}
                      </span>
                      <span
                        className="flex items-center gap-1 text-xs px-2 py-0.5 rounded"
                        style={{ background: sc.bg, color: sc.color, border: `1px solid ${sc.color}30`, fontFamily: "Noto Sans SC" }}
                      >
                        <StatusIcon size={10} />
                        {req.status}
                      </span>
                    </div>
                    <h4 className="text-sm font-semibold mb-1" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
                      {req.title}
                    </h4>
                    <p className="text-xs mb-3" style={{ color: "rgba(224,244,255,0.45)", fontFamily: "Noto Sans SC" }}>
                      {req.description}
                    </p>
                    <div className="flex items-center gap-4 flex-wrap">
                      <div className="flex items-center gap-1.5 text-xs">
                        <User size={11} style={{ color: "rgba(200,216,232,0.55)" }} />
                        <span style={{ color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>{req.creator} · {req.dept}</span>
                      </div>
                      <div className="flex items-center gap-1.5 text-xs">
                        <Calendar size={11} style={{ color: "rgba(200,216,232,0.55)" }} />
                        <span style={{ color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>截止 {req.deadline}</span>
                      </div>
                      <div className="flex items-center gap-1.5 text-xs">
                        <Package size={11} style={{ color: "rgba(200,216,232,0.55)" }} />
                        <span style={{ color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>{req.items} 种物料 · {req.totalQty} 件</span>
                      </div>
                    </div>
                  </div>

                  {/* Right stats + actions */}
                  <div className="flex flex-col items-end gap-3 flex-shrink-0">
                    <div className="text-right">
                      <p className="text-xs" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>预估金额</p>
                      <p className="text-base font-bold font-mono" style={{ color: "#50BBE3" }}>
                        ¥{(req.estimatedAmount / 10000).toFixed(1)}万
                      </p>
                    </div>
                    <div className="flex items-center gap-2">
                      {(req.status === "待报价" || req.status === "报价中") && (
                        <button
                          className="flex items-center gap-1.5 px-3 py-1.5 rounded text-xs font-medium"
                          style={{
                            background: "linear-gradient(135deg, rgba(0,212,255,0.2), rgba(0,102,255,0.2))",
                            border: "1px solid rgba(0,212,255,0.4)",
                            color: "#50BBE3",
                            fontFamily: "Noto Sans SC",
                          }}
                          onClick={() => navigate("/quotation")}
                        >
                          <Zap size={11} />
                          匹配报价
                        </button>
                      )}
                      <button
                        className="flex items-center gap-1 px-3 py-1.5 rounded text-xs"
                        style={{
                          background: "#162233",
                          border: "1px solid rgba(0,212,255,0.15)",
                          color: "rgba(0,212,255,0.6)",
                          fontFamily: "Noto Sans SC",
                        }}
                        onClick={() => toast.info(`查看需求 ${req.id}`)}
                      >
                        详情
                        <ChevronRight size={11} />
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            );
          })}
        </div>

        <div className="flex items-center justify-between mt-4 pt-4" style={{ borderTop: "1px solid rgba(0,212,255,0.08)" }}>
          <span className="text-xs" style={{ color: "rgba(200,216,232,0.55)" }}>共 {filtered.length} 条需求</span>
        </div>
      </div>

      {/* New Requirement Modal */}
      {showModal && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center"
          style={{ background: "#192A3F", backdropFilter: "blur(8px)" }}
          onClick={() => setShowModal(false)}
        >
          <div
            className="w-full max-w-xl mx-4 rounded-xl p-6"
            style={{ background: "#0A1628", border: "1px solid rgba(0,212,255,0.25)", boxShadow: "0 0 60px rgba(0,212,255,0.1)" }}
            onClick={(e) => e.stopPropagation()}
          >
            <h3 className="text-base font-semibold mb-5" style={{ color: "#50BBE3", fontFamily: "Orbitron" }}>新建采购需求</h3>
            <div className="grid grid-cols-2 gap-4">
              {[
                { label: "需求标题", span: true },
                { label: "需求描述", span: true },
                { label: "优先级", span: false },
                { label: "截止日期", span: false },
                { label: "申请部门", span: false },
                { label: "申请人", span: false },
              ].map(({ label, span }) => (
                <div key={label} className={span ? "col-span-2" : ""}>
                  <label className="block text-xs mb-1.5" style={{ color: "rgba(0,212,255,0.6)", fontFamily: "Noto Sans SC" }}>{label}</label>
                  {label === "需求描述" ? (
                    <textarea
                      rows={3}
                      className="w-full px-3 py-2.5 rounded text-xs outline-none resize-none"
                      style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.2)", color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
                    />
                  ) : (
                    <input
                      type="text"
                      className="w-full px-3 py-2.5 rounded text-xs outline-none"
                      style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.2)", color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
                    />
                  )}
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
                onClick={() => { setShowModal(false); toast.success("需求创建成功，等待审批"); }}
              >
                提交审批
              </button>
            </div>
          </div>
        </div>
      )}
    </DashboardLayout>
  );
}
