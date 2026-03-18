/**
 * DashboardLayout — FrontCRM Deep Quantum Theme
 * Left 240px sidebar + top 60px header + Tab Bar + main content
 * Tab Bar 功能：
 *   - 多标签页卡片显示
 *   - X 关闭单个标签（自动切换相邻）
 *   - 拖拽排序（@dnd-kit/sortable）
 *   - localStorage 持久化（刷新恢复）
 *   - 溢出下拉菜单
 *   - 全部关闭
 */
import { useState, useRef, useEffect, useCallback } from "react";
import { useLocation } from "wouter";
import { toast } from "sonner";
import {
  DndContext,
  closestCenter,
  PointerSensor,
  useSensor,
  useSensors,
  DragEndEvent,
  DragOverlay,
  DragStartEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  horizontalListSortingStrategy,
  useSortable,
  arrayMove,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import {
  LayoutDashboard, ShoppingCart, TrendingUp, Users, Building2,
  BarChart3, Settings, Bell, Search, ChevronDown, LogOut, Menu,
  X, Zap, ClipboardList, FileSearch, PackagePlus, PackageMinus,
  Warehouse, CreditCard, Banknote, UserCog,
} from "lucide-react";

// ─── 导航项配置 ────────────────────────────────────────────────
const navItems = [
  { icon: LayoutDashboard, label: "控制台", path: "/dashboard", group: "" },
  { icon: Users, label: "客户管理", path: "/customer", group: "基础资料" },
  { icon: Building2, label: "供应商管理", path: "/supplier", group: "基础资料" },
  { icon: ClipboardList, label: "需求管理", path: "/requirement", group: "询价" },
  { icon: FileSearch, label: "报价管理", path: "/quotation", group: "询价" },
  { icon: TrendingUp, label: "销售管理", path: "/sales", group: "订单" },
  { icon: ShoppingCart, label: "采购管理", path: "/purchase", group: "订单" },
  { icon: PackagePlus, label: "入库管理", path: "/stock-in", group: "库存" },
  { icon: Warehouse, label: "库存管理", path: "/inventory", group: "库存" },
  { icon: PackageMinus, label: "出库管理", path: "/stock-out", group: "库存" },
  { icon: CreditCard, label: "付款管理", path: "/finance-payment", group: "财务" },
  { icon: Banknote, label: "收款管理", path: "/finance-receipt", group: "财务" },
  { icon: BarChart3, label: "分析报表", path: "/reports", group: "数据分析" },
  { icon: Settings, label: "系统设置", path: "/settings", group: "系统" },
];

const STORAGE_KEY = "frontcrm_tabs";
const ACTIVE_KEY = "frontcrm_active_tab";
const DEFAULT_TAB = { path: "/dashboard", label: "控制台" };

export interface TabItem {
  path: string;
  label: string;
}

// ─── 从 localStorage 恢复标签 ──────────────────────────────────
function loadTabs(): TabItem[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as TabItem[];
      if (Array.isArray(parsed) && parsed.length > 0) return parsed;
    }
  } catch {}
  return [DEFAULT_TAB];
}

function loadActiveTab(): string {
  return localStorage.getItem(ACTIVE_KEY) || "/dashboard";
}

// ─── 单个可拖拽标签组件 ────────────────────────────────────────
interface SortableTabProps {
  tab: TabItem;
  isActive: boolean;
  onSwitch: (path: string) => void;
  onClose: (e: React.MouseEvent, path: string) => void;
  isDragging?: boolean;
  onContextMenu?: (e: React.MouseEvent) => void;
}

function SortableTab({ tab, isActive, onSwitch, onClose, isDragging, onContextMenu }: SortableTabProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging: isSelfDragging } =
    useSortable({ id: tab.path });

  const style: React.CSSProperties = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isSelfDragging ? 0.4 : 1,
    cursor: isDragging ? "grabbing" : "grab",
  };

  return (
    <div
      ref={setNodeRef}
      style={style}
      className="flex items-center gap-1.5 px-3 py-1.5 rounded text-xs flex-shrink-0 transition-colors group select-none"
      {...attributes}
      {...listeners}
      onClick={() => onSwitch(tab.path)}
      onContextMenu={onContextMenu}
      title={tab.label}
      data-active={isActive}
    >
      <span
        className="truncate max-w-[80px]"
        style={{
          color: isActive ? "#00D4FF" : "rgba(200,216,232,0.55)",
          fontWeight: isActive ? 600 : 400,
          letterSpacing: isActive ? "0.02em" : "normal",
          textShadow: isActive ? "0 0 10px rgba(0,212,255,0.6)" : "none",
          transition: "all 0.2s ease",
        }}
      >
        {tab.label}
      </span>
      <span
        className="flex-shrink-0 w-4 h-4 rounded flex items-center justify-center opacity-40 group-hover:opacity-100 transition-opacity"
        style={{ color: isActive ? "#00D4FF" : "rgba(200,216,232,0.5)" }}
        onClick={e => { e.stopPropagation(); onClose(e, tab.path); }}
        onPointerDown={e => e.stopPropagation()} // 防止触发拖拽
      >
        <X size={10} />
      </span>
    </div>
  );
}

// ─── 拖拽覆盖层（幽灵卡片）────────────────────────────────────
function DragOverlayTab({ label }: { label: string }) {
  return (
    <div
      className="flex items-center gap-1.5 px-3 py-1.5 rounded text-xs"
      style={{
        background: "linear-gradient(135deg, rgba(0,212,255,0.2), rgba(0,102,255,0.15))",
        border: "1px solid rgba(0,212,255,0.5)",
        color: "#00D4FF",
        boxShadow: "0 4px 20px rgba(0,212,255,0.3)",
        cursor: "grabbing",
      }}
    >
      <span className="truncate max-w-[80px]">{label}</span>
      <X size={10} style={{ opacity: 0.5 }} />
    </div>
  );
}

// ─── 主布局组件 ───────────────────────────────────────────────
interface DashboardLayoutProps {
  children: React.ReactNode;
  title?: string;
}

export default function DashboardLayout({ children }: DashboardLayoutProps) {
  const [location, navigate] = useLocation();
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [overflowMenuOpen, setOverflowMenuOpen] = useState(false);

  // ── 右键菜单状态 ──────────────────────────────────────────────
  const [contextMenu, setContextMenu] = useState<{
    x: number;
    y: number;
    tabPath: string;
  } | null>(null);

  // ── Tab 状态（从 localStorage 恢复）──────────────────────────
  const [tabs, setTabs] = useState<TabItem[]>(loadTabs);
  const [activeTab, setActiveTab] = useState<string>(loadActiveTab);
  const [dragActiveId, setDragActiveId] = useState<string | null>(null);

  // 溢出检测
  const tabBarRef = useRef<HTMLDivElement>(null);
  const [visibleCount, setVisibleCount] = useState(999);

  // dnd-kit sensors（拖拽至少移动 5px 才触发，避免误触点击）
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } })
  );

  // ── 持久化到 localStorage ─────────────────────────────────────
  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(tabs));
  }, [tabs]);

  useEffect(() => {
    localStorage.setItem(ACTIVE_KEY, activeTab);
  }, [activeTab]);

  // ── 溢出检测 ──────────────────────────────────────────────────
  useEffect(() => {
    const measure = () => {
      const bar = tabBarRef.current;
      if (!bar) return;
      const available = bar.clientWidth - 180; // 留给溢出按钮和全部关闭
      setVisibleCount(Math.max(1, Math.floor(available / 130)));
    };
    measure();
    const ro = new ResizeObserver(measure);
    if (tabBarRef.current) ro.observe(tabBarRef.current);
    return () => ro.disconnect();
  }, []);

  // ── 路由变化时自动打开标签 ────────────────────────────────────
  useEffect(() => {
    const nav = navItems.find(n => n.path === location);
    if (!nav) return;
    setTabs(prev => {
      if (prev.find(t => t.path === nav.path)) return prev;
      return [...prev, { path: nav.path, label: nav.label }];
    });
    setActiveTab(location);
  }, [location]);

  // ── 切换标签 ──────────────────────────────────────────────────
  const switchTab = useCallback((path: string) => {
    setActiveTab(path);
    navigate(path);
    setOverflowMenuOpen(false);
  }, [navigate]);

  // ── 关闭标签 ──────────────────────────────────────────────────
  const closeTab = useCallback((e: React.MouseEvent, path: string) => {
    e.stopPropagation();
    setTabs(prev => {
      const idx = prev.findIndex(t => t.path === path);
      const next = prev.filter(t => t.path !== path);
      if (next.length === 0) {
        setActiveTab("/dashboard");
        navigate("/dashboard");
        return [DEFAULT_TAB];
      }
      if (path === activeTab) {
        const newActive = next[Math.min(idx, next.length - 1)];
        setActiveTab(newActive.path);
        navigate(newActive.path);
      }
      return next;
    });
  }, [activeTab, navigate]);

  // ── 关闭其他标签 ─────────────────────────────────────────────
  const closeOtherTabs = useCallback((keepPath: string) => {
    const keepTab = tabs.find(t => t.path === keepPath);
    if (!keepTab) return;
    setTabs([keepTab]);
    setActiveTab(keepPath);
    navigate(keepPath);
    setContextMenu(null);
  }, [tabs, navigate]);

  // ── 关闭右侧标签 ─────────────────────────────────────────────
  const closeRightTabs = useCallback((fromPath: string) => {
    const idx = tabs.findIndex(t => t.path === fromPath);
    if (idx < 0) return;
    const kept = tabs.slice(0, idx + 1);
    setTabs(kept);
    if (!kept.find(t => t.path === activeTab)) {
      const last = kept[kept.length - 1];
      setActiveTab(last.path);
      navigate(last.path);
    }
    setContextMenu(null);
  }, [tabs, activeTab, navigate]);

  // ── 全部关闭 ──────────────────────────────────────────────────
  const closeAllTabs = useCallback(() => {
    setTabs([DEFAULT_TAB]);
    setActiveTab("/dashboard");
    navigate("/dashboard");
    setOverflowMenuOpen(false);
  }, [navigate]);

  // ── 打开/切换标签（菜单点击）─────────────────────────────────
  const handleNav = useCallback((path: string, label: string) => {
    if (path === "/settings") {
      toast.info("系统设置功能即将上线");
      return;
    }
    setTabs(prev => {
      if (prev.find(t => t.path === path)) return prev;
      return [...prev, { path, label }];
    });
    setActiveTab(path);
    navigate(path);
  }, [navigate]);

  // ── 拖拽结束排序 ──────────────────────────────────────────────
  const handleDragStart = (event: DragStartEvent) => {
    setDragActiveId(event.active.id as string);
  };

  const handleDragEnd = (event: DragEndEvent) => {
    setDragActiveId(null);
    const { active, over } = event;
    if (!over || active.id === over.id) return;
    setTabs(prev => {
      const oldIdx = prev.findIndex(t => t.path === active.id);
      const newIdx = prev.findIndex(t => t.path === over.id);
      return arrayMove(prev, oldIdx, newIdx);
    });
  };

  const groups = Array.from(new Set(navItems.map(i => i.group)));
  const visibleTabs = tabs.slice(0, visibleCount);
  const overflowTabs = tabs.slice(visibleCount);
  const dragActiveTab = tabs.find(t => t.path === dragActiveId);

  return (
    <div
      className="flex h-screen overflow-hidden"
      style={{ background: "#192A3F" }}
      onClick={() => { setUserMenuOpen(false); setOverflowMenuOpen(false); setContextMenu(null); }}
    >
      {/* ── Sidebar ─────────────────────────────────────────── */}
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
              {group && (
                <div
                  className="px-3 mb-1 text-xs font-semibold tracking-widest uppercase"
                  style={{ color: "rgba(200,216,232,0.5)" }}
                >
                  {group}
                </div>
              )}
              {navItems
                .filter((item) => item.group === group)
                .map((item) => {
                  const isActive = location === item.path;
                  return (
                    <button
                      key={item.path}
                      onClick={() => handleNav(item.path, item.label)}
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

      {/* ── Main area ────────────────────────────────────────── */}
      <div className="flex-1 flex flex-col overflow-hidden min-w-0">
        {/* ── 第一行：顶部工具栏 ──────────────────────────────── */}
        <header
          className="flex items-center gap-3 px-4 flex-shrink-0"
          style={{
            height: "52px",
            background: "#0A1628",
            backdropFilter: "blur(12px)",
            borderBottom: "1px solid rgba(0, 212, 255, 0.08)",
            boxShadow: "0 2px 20px rgba(0, 212, 255, 0.04)",
          }}
          onClick={e => e.stopPropagation()}
        >
          {/* Toggle sidebar */}
          <button
            onClick={() => setSidebarOpen(!sidebarOpen)}
            className="p-1.5 rounded transition-colors flex-shrink-0"
            style={{ color: "rgba(0,212,255,0.6)" }}
          >
            {sidebarOpen ? <Menu size={18} /> : <Menu size={18} />}
          </button>

          {/* 弹性空间 */}
          <div className="flex-1" />

          {/* Search */}
          <div
            className="flex items-center gap-2 px-3 py-1.5 rounded text-sm flex-shrink-0"
            style={{
              background: "#162233",
              border: "1px solid rgba(0,212,255,0.15)",
              color: "rgba(0,212,255,0.5)",
              minWidth: "150px",
            }}
          >
            <Search size={14} />
            <span className="text-xs">搜索...</span>
          </div>

          {/* Notification */}
          <button
            className="relative p-2 rounded transition-colors flex-shrink-0"
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
          <div className="relative flex-shrink-0">
            <button
              className="flex items-center gap-2 px-3 py-1.5 rounded text-sm transition-colors"
              style={{
                background: "#162233",
                border: "1px solid rgba(0,212,255,0.15)",
                color: "#E0F4FF",
              }}
              onClick={e => { e.stopPropagation(); setUserMenuOpen(!userMenuOpen); }}
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
                onClick={e => e.stopPropagation()}
              >
                <div
                  className="px-4 py-3 mb-1"
                  style={{ borderBottom: "1px solid rgba(0,212,255,0.1)" }}
                >
                  <div className="text-xs font-medium" style={{ color: "#E0F4FF" }}>系统管理员</div>
                  <div className="text-xs mt-0.5" style={{ color: "rgba(200,216,232,0.5)" }}>admin@frontcrm.com</div>
                </div>
                <button
                  className="w-full flex items-center gap-2 px-4 py-2 text-xs text-left transition-colors hover:bg-white/5"
                  style={{ color: "rgba(224,244,255,0.8)" }}
                  onClick={() => { handleNav("/profile", "个人设置"); setUserMenuOpen(false); }}
                >
                  <UserCog size={13} style={{ color: "rgba(0,212,255,0.6)" }} />
                  个人设置
                </button>
                <div style={{ borderTop: "1px solid rgba(0,212,255,0.08)", margin: "4px 0" }} />
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

        {/* ── 第二行：Tab Bar ──────────────────────────────────── */}
        <div
          className="flex items-center gap-1 px-3 flex-shrink-0"
          style={{
            height: "40px",
            background: "#0D1F35",
            borderBottom: "1px solid rgba(0, 212, 255, 0.12)",
            boxShadow: "0 2px 12px rgba(0, 212, 255, 0.04)",
          }}
          onClick={e => e.stopPropagation()}
        >
          <DndContext
            sensors={sensors}
            collisionDetection={closestCenter}
            onDragStart={handleDragStart}
            onDragEnd={handleDragEnd}
          >
            <SortableContext
              items={visibleTabs.map(t => t.path)}
              strategy={horizontalListSortingStrategy}
            >
              <div
                ref={tabBarRef}
                className="flex items-center gap-1 flex-1 overflow-hidden min-w-0"
              >
                {/* 可见标签 */}
                {visibleTabs.map(tab => {
                  const isActive = activeTab === tab.path;
                  return (
                    <div
                      key={tab.path}
                      style={{
                        position: "relative",
                        background: isActive
                          ? "linear-gradient(160deg, rgba(0,212,255,0.22) 0%, rgba(0,102,255,0.16) 100%)"
                          : "rgba(255,255,255,0.03)",
                        border: isActive
                          ? "1px solid rgba(0,212,255,0.6)"
                          : "1px solid rgba(0,212,255,0.08)",
                        borderRadius: "6px",
                        boxShadow: isActive
                          ? "0 0 14px rgba(0,212,255,0.25), inset 0 1px 0 rgba(0,212,255,0.2)"
                          : "none",
                        transform: isActive ? "translateY(-1px)" : "translateY(0)",
                        transition: "all 0.2s ease",
                      }}
                    >
                      {/* 激活底部指示条 */}
                      {isActive && (
                        <div
                          style={{
                            position: "absolute",
                            bottom: "-1px",
                            left: "20%",
                            right: "20%",
                            height: "2px",
                            background: "linear-gradient(90deg, transparent, #00D4FF, transparent)",
                            borderRadius: "2px",
                            boxShadow: "0 0 8px rgba(0,212,255,0.8)",
                          }}
                        />
                      )}
                      <SortableTab
                        tab={tab}
                        isActive={isActive}
                        onSwitch={switchTab}
                        onClose={closeTab}
                        isDragging={!!dragActiveId}
                        onContextMenu={(e) => {
                          e.preventDefault();
                          e.stopPropagation();
                          setContextMenu({ x: e.clientX, y: e.clientY, tabPath: tab.path });
                        }}
                      />
                    </div>
                  );
                })}
              </div>
            </SortableContext>

            {/* 拖拽幽灵卡片 */}
            <DragOverlay>
              {dragActiveTab ? <DragOverlayTab label={dragActiveTab.label} /> : null}
            </DragOverlay>
          </DndContext>

          {/* 溢出下拉 */}
          {overflowTabs.length > 0 && (
            <div className="relative flex-shrink-0">
              <button
                onClick={e => { e.stopPropagation(); setOverflowMenuOpen(!overflowMenuOpen); }}
                className="flex items-center gap-1 px-2 py-1 rounded text-xs transition-all"
                style={{
                  background: "rgba(255,255,255,0.04)",
                  border: "1px solid rgba(0,212,255,0.15)",
                  color: "rgba(0,212,255,0.7)",
                }}
              >
                <span>+{overflowTabs.length}</span>
                <ChevronDown size={11} />
              </button>
              {overflowMenuOpen && (
                <div
                  className="absolute left-0 top-full mt-1 rounded py-1 z-50"
                  style={{
                    background: "#0A1628",
                    border: "1px solid rgba(0,212,255,0.2)",
                    minWidth: "160px",
                    boxShadow: "0 8px 32px rgba(0,0,0,0.5)",
                  }}
                  onClick={e => e.stopPropagation()}
                >
                  {overflowTabs.map(tab => (
                    <div
                      key={tab.path}
                      className="flex items-center justify-between px-3 py-2 hover:bg-white/5 cursor-pointer"
                      onClick={() => switchTab(tab.path)}
                    >
                      <span
                        className="text-xs truncate"
                        style={{ color: activeTab === tab.path ? "#00D4FF" : "rgba(200,216,232,0.8)" }}
                      >
                        {tab.label}
                      </span>
                      <button
                        className="ml-2 flex-shrink-0"
                        style={{ color: "rgba(200,216,232,0.4)" }}
                        onClick={e => closeTab(e, tab.path)}
                      >
                        <X size={11} />
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}

          {/* 全部关闭 */}
          <button
            onClick={closeAllTabs}
            className="flex-shrink-0 ml-1 px-2 py-1 rounded text-xs transition-all"
            style={{
              color: "rgba(200,216,232,0.35)",
              border: "1px solid transparent",
            }}
            title="全部关闭"
            onMouseEnter={e => {
              (e.currentTarget as HTMLButtonElement).style.color = "#FF6B35";
              (e.currentTarget as HTMLButtonElement).style.borderColor = "rgba(255,107,53,0.3)";
            }}
            onMouseLeave={e => {
              (e.currentTarget as HTMLButtonElement).style.color = "rgba(200,216,232,0.35)";
              (e.currentTarget as HTMLButtonElement).style.borderColor = "transparent";
            }}
          >
            全部关闭
          </button>
        </div>

          {/* ── 右键上下文菜单 ──────────────────────────────── */}
          {contextMenu && (
            <div
              className="fixed z-[9999] rounded py-1"
              style={{
                left: contextMenu.x,
                top: contextMenu.y,
                background: "#0A1628",
                border: "1px solid rgba(0,212,255,0.25)",
                boxShadow: "0 8px 32px rgba(0,0,0,0.6), 0 0 0 1px rgba(0,212,255,0.05)",
                minWidth: "160px",
                backdropFilter: "blur(12px)",
              }}
              onClick={e => e.stopPropagation()}
            >
              {/* 菜单标题 */}
              <div
                className="px-4 py-2 text-xs"
                style={{
                  color: "rgba(0,212,255,0.5)",
                  borderBottom: "1px solid rgba(0,212,255,0.1)",
                  marginBottom: "4px",
                }}
              >
                {tabs.find(t => t.path === contextMenu.tabPath)?.label}
              </div>

              {/* 关闭当前 */}
              <button
                className="w-full flex items-center gap-2.5 px-4 py-2 text-xs text-left transition-colors hover:bg-white/5"
                style={{ color: "rgba(224,244,255,0.8)" }}
                onClick={() => {
                  const e = { stopPropagation: () => {} } as React.MouseEvent;
                  closeTab(e, contextMenu.tabPath);
                  setContextMenu(null);
                }}
              >
                <X size={12} style={{ color: "rgba(0,212,255,0.6)" }} />
                关闭当前
              </button>

              {/* 关闭其他 */}
              <button
                className="w-full flex items-center gap-2.5 px-4 py-2 text-xs text-left transition-colors hover:bg-white/5"
                style={{ color: "rgba(224,244,255,0.8)" }}
                onClick={() => closeOtherTabs(contextMenu.tabPath)}
              >
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="rgba(0,212,255,0.6)" strokeWidth="2">
                  <path d="M18 6L6 18M6 6l12 12" />
                  <rect x="2" y="2" width="20" height="20" rx="2" strokeDasharray="4 2" />
                </svg>
                关闭其他
              </button>

              {/* 关闭右侧 */}
              <button
                className="w-full flex items-center gap-2.5 px-4 py-2 text-xs text-left transition-colors hover:bg-white/5"
                style={{
                  color: tabs.findIndex(t => t.path === contextMenu.tabPath) >= tabs.length - 1
                    ? "rgba(200,216,232,0.3)"
                    : "rgba(224,244,255,0.8)",
                  cursor: tabs.findIndex(t => t.path === contextMenu.tabPath) >= tabs.length - 1
                    ? "not-allowed"
                    : "pointer",
                }}
                disabled={tabs.findIndex(t => t.path === contextMenu.tabPath) >= tabs.length - 1}
                onClick={() => closeRightTabs(contextMenu.tabPath)}
              >
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="rgba(0,212,255,0.6)" strokeWidth="2">
                  <polyline points="9 18 15 12 9 6" />
                  <line x1="15" y1="6" x2="15" y2="18" />
                </svg>
                关闭右侧
              </button>

              <div style={{ borderTop: "1px solid rgba(0,212,255,0.08)", margin: "4px 0" }} />

              {/* 全部关闭 */}
              <button
                className="w-full flex items-center gap-2.5 px-4 py-2 text-xs text-left transition-colors hover:bg-white/5"
                style={{ color: "#FF6B35" }}
                onClick={() => { closeAllTabs(); setContextMenu(null); }}
              >
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="#FF6B35" strokeWidth="2">
                  <line x1="18" y1="6" x2="6" y2="18" />
                  <line x1="6" y1="6" x2="18" y2="18" />
                </svg>
                全部关闭
              </button>
            </div>
          )}

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
