/**
 * DashboardLayout — FrontCRM Deep Quantum Theme
 * Left 240px sidebar + top 60px header + main content
 * Glassmorphism cards, cyan glow borders, dark navy background
 */
import { useState } from "react";
import { useLocation } from "wouter";
import { toast } from "sonner";
import {
  LayoutDashboard,
  ShoppingCart,
  TrendingUp,
  Users,
  Building2,
  BarChart3,
  Settings,
  Bell,
  Search,
  ChevronDown,
  LogOut,
  Menu,
  X,
  Zap,
  ClipboardList,
  FileSearch,
  PackagePlus,
  PackageMinus,
  Warehouse,
  CreditCard,
  Banknote,
  UserCog,
} from "lucide-react";

const navItems = [
  // 控制台（无分组标题）
  { icon: LayoutDashboard, label: "控制台", path: "/dashboard", group: "" },
  // 基础资料
  { icon: Users, label: "客户管理", path: "/customer", group: "基础资料" },
  { icon: Building2, label: "供应商管理", path: "/supplier", group: "基础资料" },
  // 询价
  { icon: ClipboardList, label: "需求管理", path: "/requirement", group: "询价" },
  { icon: FileSearch, label: "报价管理", path: "/quotation", group: "询价" },
  // 订单
  { icon: TrendingUp, label: "销售管理", path: "/sales", group: "订单" },
  { icon: ShoppingCart, label: "采购管理", path: "/purchase", group: "订单" },
  // 库存
  { icon: PackagePlus, label: "入库管理", path: "/stock-in", group: "库存" },
  { icon: Warehouse, label: "库存管理", path: "/inventory", group: "库存" },
  { icon: PackageMinus, label: "出库管理", path: "/stock-out", group: "库存" },
  // 财务
  { icon: CreditCard, label: "付款管理", path: "/finance-payment", group: "财务" },
  { icon: Banknote, label: "收款管理", path: "/finance-receipt", group: "财务" },
  // 数据分析
  { icon: BarChart3, label: "分析报表", path: "/reports", group: "数据分析" },
  // 系统
  { icon: Settings, label: "系统设置", path: "/settings", group: "系统" },
];

interface DashboardLayoutProps {
  children: React.ReactNode;
  title?: string;
}

export default function DashboardLayout({ children, title }: DashboardLayoutProps) {
  const [location, navigate] = useLocation();
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [userMenuOpen, setUserMenuOpen] = useState(false);

  const groups = Array.from(new Set(navItems.map(i => i.group)));

  const handleNav = (path: string) => {
    if (path === "/settings") {
      toast.info("系统设置功能即将上线");
      return;
    }
    navigate(path);
  };

  return (
    <div
      className="flex h-screen overflow-hidden bg-grid"
      style={{ background: "#192A3F" }}
    >
      {/* Sidebar */}
      <aside
        className="flex flex-col flex-shrink-0 transition-all duration-300 overflow-hidden"
        style={{
          width: sidebarOpen ? "240px" : "0px",
          background: "#0A1628",
          borderRight: "1px solid rgba(0, 212, 255, 0.15)",
          boxShadow: "4px 0 24px rgba(0, 212, 255, 0.06)",
        }}
      >
        {/* Logo */}
        <div
          className="flex items-center gap-3 px-5 py-5 flex-shrink-0"
          style={{ borderBottom: "1px solid rgba(0, 212, 255, 0.1)" }}
        >
          <div
            className="w-9 h-9 rounded flex items-center justify-center flex-shrink-0"
            style={{
              background: "linear-gradient(135deg, rgba(0,212,255,0.2), rgba(0,102,255,0.2))",
              border: "1px solid rgba(0,212,255,0.4)",
              boxShadow: "0 0 15px rgba(0,212,255,0.2)",
            }}
          >
            <Zap size={18} style={{ color: "#00D4FF" }} />
          </div>
          <div>
            <div
              className="text-sm font-bold tracking-widest"
              style={{ fontFamily: "Orbitron, sans-serif", color: "#00D4FF" }}
            >
              FrontCRM
            </div>
            <div className="text-xs" style={{ color: "rgba(0,212,255,0.5)" }}>
              智能进销存系统
            </div>
          </div>
        </div>

        {/* Nav */}
        <nav className="flex-1 overflow-y-auto py-4 px-2">
          {groups.map((group) => (
            <div key={group} className="mb-4">
              <div
                className="px-3 mb-1 text-xs font-semibold tracking-widest uppercase"
                style={{ color: "rgba(200,216,232,0.5)" }}
              >
                {group}
              </div>
              {navItems
                .filter((item) => item.group === group)
                .map((item) => {
                  const isActive = location === item.path;
                  return (
                    <button
                      key={item.path}
                      onClick={() => handleNav(item.path)}
                      className={`w-full flex items-center gap-3 px-3 py-2.5 rounded text-sm mb-0.5 sidebar-item ${isActive ? "active" : ""}`}
                      style={{
                        color: isActive ? "#00D4FF" : "rgba(224,244,255,0.65)",
                        fontFamily: "Noto Sans SC, sans-serif",
                      }}
                    >
                      <item.icon
                        size={16}
                        style={{ color: isActive ? "#00D4FF" : "rgba(0,212,255,0.5)", flexShrink: 0 }}
                      />
                      <span className="whitespace-nowrap">{item.label}</span>
                      {isActive && (
                        <div
                          className="ml-auto w-1.5 h-1.5 rounded-full pulse-dot"
                          style={{ background: "#00D4FF" }}
                        />
                      )}
                    </button>
                  );
                })}
            </div>
          ))}
        </nav>


      </aside>

      {/* Main area */}
      <div className="flex-1 flex flex-col overflow-hidden min-w-0">
        {/* Top header */}
        <header
          className="flex items-center gap-4 px-6 flex-shrink-0"
          style={{
            height: "60px",
            background: "#0A1628",
            backdropFilter: "blur(12px)",
            borderBottom: "1px solid rgba(0, 212, 255, 0.12)",
            boxShadow: "0 2px 20px rgba(0, 212, 255, 0.06)",
          }}
        >
          {/* Toggle sidebar */}
          <button
            onClick={() => setSidebarOpen(!sidebarOpen)}
            className="p-1.5 rounded transition-colors"
            style={{ color: "rgba(0,212,255,0.6)" }}
          >
            {sidebarOpen ? <X size={18} /> : <Menu size={18} />}
          </button>

          {/* Page title */}
          <div
            className="text-sm font-semibold tracking-wide"
            style={{ fontFamily: "Orbitron, sans-serif", color: "#00D4FF" }}
          >
            {title || "控制台"}
          </div>

          {/* Spacer */}
          <div className="flex-1" />

          {/* Search */}
          <div
            className="flex items-center gap-2 px-3 py-1.5 rounded text-sm"
            style={{
              background: "#162233",
              border: "1px solid rgba(0,212,255,0.15)",
              color: "rgba(0,212,255,0.5)",
              minWidth: "200px",
            }}
          >
            <Search size={14} />
            <span className="text-xs">搜索...</span>
          </div>

          {/* Notification */}
          <button
            className="relative p-2 rounded transition-colors"
            style={{ color: "rgba(0,212,255,0.6)" }}
            onClick={() => toast.info("暂无新消息")}
          >
            <Bell size={18} />
            <span
              className="absolute top-1 right-1 w-2 h-2 rounded-full"
              style={{ background: "#FF6B35" }}
            />
          </button>

          {/* User menu */}
          <div className="relative">
            <button
              className="flex items-center gap-2 px-3 py-1.5 rounded text-sm transition-colors"
              style={{
                background: "#162233",
                border: "1px solid rgba(0,212,255,0.15)",
                color: "#E0F4FF",
              }}
              onClick={() => setUserMenuOpen(!userMenuOpen)}
            >
              <div
                className="w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold"
                style={{ background: "linear-gradient(135deg, #00D4FF, #0066FF)", color: "#0A1628" }}
              >
                管
              </div>
              <span className="text-xs">管理员</span>
              <ChevronDown size={12} style={{ color: "rgba(0,212,255,0.5)" }} />
            </button>
            {userMenuOpen && (
              <div
                className="absolute right-0 top-full mt-1 rounded py-1 z-50"
                style={{
                  background: "#0A1628",
                  border: "1px solid rgba(0,212,255,0.2)",
                  backdropFilter: "blur(12px)",
                  minWidth: "160px",
                  boxShadow: "0 8px 32px rgba(0,0,0,0.5)",
                }}
              >
                {/* 用户信息头部 */}
                <div
                  className="px-4 py-3 mb-1"
                  style={{ borderBottom: "1px solid rgba(0,212,255,0.1)" }}
                >
                  <div className="text-xs font-medium" style={{ color: "#E0F4FF" }}>系统管理员</div>
                  <div className="text-xs mt-0.5" style={{ color: "rgba(200,216,232,0.5)" }}>admin@frontcrm.com</div>
                </div>
                {/* 个人设置 */}
                <button
                  className="w-full flex items-center gap-2 px-4 py-2 text-xs text-left transition-colors hover:bg-white/5"
                  style={{ color: "rgba(224,244,255,0.8)" }}
                  onClick={() => { toast.info("个人设置功能即将上线"); setUserMenuOpen(false); }}
                >
                  <UserCog size={13} style={{ color: "rgba(0,212,255,0.6)" }} />
                  个人设置
                </button>
                {/* 分隔线 */}
                <div style={{ borderTop: "1px solid rgba(0,212,255,0.08)", margin: "4px 0" }} />
                {/* 退出系统 */}
                <button
                  className="w-full flex items-center gap-2 px-4 py-2 text-xs text-left transition-colors hover:bg-white/5"
                  style={{ color: "#FF6B35" }}
                  onClick={() => { navigate("/login"); setUserMenuOpen(false); }}
                >
                  <LogOut size={13} />
                  退出系统
                </button>
              </div>
            )}
          </div>
        </header>

        {/* Page content */}
        <main
          className="flex-1 overflow-y-auto p-6"
          style={{ background: "transparent" }}
        >
          {children}
        </main>
      </div>
    </div>
  );
}
