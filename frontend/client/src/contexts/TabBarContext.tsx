/**
 * TabBarContext — 多标签页状态管理
 * 管理已打开的页面标签列表，支持：
 * - 打开/激活标签
 * - 关闭单个标签（自动切换到相邻标签）
 * - 全部关闭
 */
import { createContext, useContext, useState, useCallback, ReactNode } from "react";
import { useLocation } from "wouter";

export interface TabItem {
  path: string;
  label: string;
}

interface TabBarContextValue {
  tabs: TabItem[];
  activeTab: string;
  openTab: (tab: TabItem) => void;
  closeTab: (path: string) => void;
  closeAllTabs: () => void;
}

const TabBarContext = createContext<TabBarContextValue | null>(null);

export function TabBarProvider({ children }: { children: ReactNode }) {
  const [, navigate] = useLocation();
  const [tabs, setTabs] = useState<TabItem[]>([
    { path: "/dashboard", label: "控制台" },
  ]);
  const [activeTab, setActiveTab] = useState("/dashboard");

  const openTab = useCallback((tab: TabItem) => {
    setTabs(prev => {
      const exists = prev.find(t => t.path === tab.path);
      if (!exists) return [...prev, tab];
      return prev;
    });
    setActiveTab(tab.path);
    navigate(tab.path);
  }, [navigate]);

  const closeTab = useCallback((path: string) => {
    setTabs(prev => {
      const idx = prev.findIndex(t => t.path === path);
      if (idx === -1) return prev;
      const next = prev.filter(t => t.path !== path);
      // 如果关闭的是当前激活标签，切换到相邻标签
      if (path === activeTab && next.length > 0) {
        const newActive = next[Math.min(idx, next.length - 1)];
        setActiveTab(newActive.path);
        navigate(newActive.path);
      } else if (next.length === 0) {
        setActiveTab("");
        navigate("/dashboard");
      }
      return next;
    });
  }, [activeTab, navigate]);

  const closeAllTabs = useCallback(() => {
    // 保留控制台
    setTabs([{ path: "/dashboard", label: "控制台" }]);
    setActiveTab("/dashboard");
    navigate("/dashboard");
  }, [navigate]);

  return (
    <TabBarContext.Provider value={{ tabs, activeTab, openTab, closeTab, closeAllTabs }}>
      {children}
    </TabBarContext.Provider>
  );
}

export function useTabBar() {
  const ctx = useContext(TabBarContext);
  if (!ctx) throw new Error("useTabBar must be used within TabBarProvider");
  return ctx;
}
