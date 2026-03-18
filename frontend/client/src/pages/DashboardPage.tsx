/**
 * DashboardPage — FrontCRM Deep Quantum Theme
 * Main control panel with KPI cards, charts, recent orders table
 */
import { useState, useEffect } from "react";
import DashboardLayout from "@/components/DashboardLayout";
import {
  AreaChart, Area, BarChart, Bar, XAxis, YAxis, CartesianGrid,
  Tooltip, ResponsiveContainer, PieChart, Pie, Cell
} from "recharts";
import {
  TrendingUp, TrendingDown, Package, ShoppingCart, Users,
  AlertTriangle, ArrowUpRight, ArrowDownRight, RefreshCw
} from "lucide-react";

const salesData = [
  { month: "1月", sales: 420, purchase: 280 },
  { month: "2月", sales: 380, purchase: 310 },
  { month: "3月", sales: 510, purchase: 350 },
  { month: "4月", sales: 470, purchase: 290 },
  { month: "5月", sales: 620, purchase: 420 },
  { month: "6月", sales: 580, purchase: 380 },
  { month: "7月", sales: 710, purchase: 460 },
  { month: "8月", sales: 680, purchase: 440 },
  { month: "9月", sales: 750, purchase: 510 },
  { month: "10月", sales: 820, purchase: 550 },
  { month: "11月", sales: 790, purchase: 520 },
  { month: "12月", sales: 950, purchase: 630 },
];

const inventoryPie = [
  { name: "正常库存", value: 65, color: "#00D4FF" },
  { name: "低库存预警", value: 20, color: "#C99A45" },
  { name: "超库存", value: 10, color: "#3295C9" },
  { name: "零库存", value: 5, color: "#C95745" },
];

const recentOrders = [
  { id: "PO-2026031001", type: "采购", supplier: "华联电子", amount: 128500, status: "已入库", date: "2026-03-10" },
  { id: "SO-2026031002", type: "销售", customer: "深圳科技有限公司", amount: 256000, status: "已发货", date: "2026-03-10" },
  { id: "PO-2026030901", type: "采购", supplier: "联创物资", amount: 85200, status: "待审核", date: "2026-03-09" },
  { id: "SO-2026030902", type: "销售", customer: "广州贸易集团", amount: 198000, status: "已完成", date: "2026-03-09" },
  { id: "PO-2026030801", type: "采购", supplier: "东方供应链", amount: 320000, status: "在途", date: "2026-03-08" },
  { id: "SO-2026030802", type: "销售", customer: "北京数码科技", amount: 145600, status: "待发货", date: "2026-03-08" },
];

const alerts = [
  { item: "iPhone 15 Pro Max 256G", stock: 3, min: 10, level: "danger" },
  { item: "联想ThinkPad X1 Carbon", stock: 5, min: 8, level: "warning" },
  { item: "三星Galaxy S24 Ultra", stock: 2, min: 10, level: "danger" },
  { item: "戴尔XPS 15笔记本", stock: 6, min: 10, level: "warning" },
];

function KpiCard({ title, value, unit, change, changeType, icon: Icon, color }: {
  title: string; value: string; unit: string; change: string;
  changeType: "up" | "down"; icon: React.ElementType; color: string;
}) {
  const [displayVal, setDisplayVal] = useState("0");

  useEffect(() => {
    const target = parseFloat(value.replace(/,/g, ""));
    const duration = 1500;
    const steps = 60;
    const step = target / steps;
    let current = 0;
    const timer = setInterval(() => {
      current += step;
      if (current >= target) {
        setDisplayVal(value);
        clearInterval(timer);
      } else {
        setDisplayVal(Math.floor(current).toLocaleString());
      }
    }, duration / steps);
    return () => clearInterval(timer);
  }, [value]);

  return (
    <div
      className="glass-card rounded-xl p-5 transition-all duration-300 cursor-default"
      style={{ animationDelay: "0.1s" }}
    >
      <div className="flex items-start justify-between mb-4">
        <div>
          <p className="text-xs mb-1" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>
            {title}
          </p>
          <div className="flex items-baseline gap-1">
            <span
              className="text-2xl font-bold"
              style={{ fontFamily: "Space Mono, monospace", color }}
            >
              {displayVal}
            </span>
            <span className="text-xs" style={{ color: "rgba(224,244,255,0.5)" }}>{unit}</span>
          </div>
        </div>
        <div
          className="w-10 h-10 rounded-lg flex items-center justify-center"
          style={{ background: `${color}15`, border: `1px solid ${color}30` }}
        >
          <Icon size={18} style={{ color }} />
        </div>
      </div>
      <div className="flex items-center gap-1">
        {changeType === "up" ? (
          <ArrowUpRight size={13} style={{ color: "#46BF91" }} />
        ) : (
          <ArrowDownRight size={13} style={{ color: "#C95745" }} />
        )}
        <span
          className="text-xs"
          style={{ color: changeType === "up" ? "#46BF91" : "#C95745" }}
        >
          {change}
        </span>
        <span className="text-xs" style={{ color: "rgba(200,216,232,0.45)" }}>
          较上月
        </span>
      </div>
    </div>
  );
}

const CustomTooltip = ({ active, payload, label }: any) => {
  if (active && payload && payload.length) {
    return (
      <div
        className="px-3 py-2 rounded text-xs"
        style={{
          background: "#0A1628",
          border: "1px solid rgba(0,212,255,0.2)",
          backdropFilter: "blur(8px)",
        }}
      >
        <p style={{ color: "rgba(0,212,255,0.7)" }} className="mb-1">{label}</p>
        {payload.map((p: any) => (
          <p key={p.name} style={{ color: p.color }}>
            {p.name === "sales" ? "销售额" : "采购额"}: ¥{p.value}万
          </p>
        ))}
      </div>
    );
  }
  return null;
};

export default function DashboardPage() {
  return (
    <DashboardLayout title="控制台">
      {/* KPI Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        {/* 范式1: 冰蓝 Ice Blue — 销售类指标 */}
        <KpiCard
          title="本月销售额"
          value="950"
          unit="万元"
          change="+18.5%"
          changeType="up"
          icon={TrendingUp}
          color="#50BBE3"
        />
        {/* 范式2: 钢青 Steel Cyan — 采购类指标 */}
        <KpiCard
          title="本月采购额"
          value="630"
          unit="万元"
          change="+12.3%"
          changeType="up"
          icon={ShoppingCart}
          color="#3295C9"
        />
        {/* 范式3: 薄荷灰 Mint Gray — 库存/数量类指标 */}
        <KpiCard
          title="库存总量"
          value="12,847"
          unit="件"
          change="-3.2%"
          changeType="down"
          icon={Package}
          color="#46BF91"
        />
        {/* 范式4: 琥珀灰 Amber Gray — 客户/关系类指标 */}
        <KpiCard
          title="活跃客户"
          value="1,284"
          unit="家"
          change="+8.7%"
          changeType="up"
          icon={Users}
          color="#C99A45"
        />
      </div>

      {/* Charts row */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 mb-6">
        {/* Sales trend */}
        <div className="lg:col-span-2 glass-card rounded-xl p-5">
          <div className="flex items-center justify-between mb-4">
            <div>
              <h3
                className="text-sm font-semibold"
                style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
              >
                年度销售 / 采购趋势
              </h3>
              <p className="text-xs mt-0.5" style={{ color: "rgba(200,216,232,0.45)" }}>
                2026年全年数据
              </p>
            </div>
            <button className="p-1.5 rounded" style={{ color: "rgba(200,216,232,0.6)" }}>
              <RefreshCw size={14} />
            </button>
          </div>
          <ResponsiveContainer width="100%" height={200}>
            <AreaChart data={salesData}>
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" />
              <XAxis
                dataKey="month"
                tick={{ fill: "#6B7A8D", fontSize: 11 }}
                axisLine={{ stroke: "rgba(255,255,255,0.08)" }}
                tickLine={false}
              />
              <YAxis
                tick={{ fill: "#6B7A8D", fontSize: 11 }}
                axisLine={false}
                tickLine={false}
              />
              <Tooltip content={<CustomTooltip />} />
              <Area
                type="monotone"
                dataKey="sales"
                stroke="#00D4FF"
                strokeWidth={2}
                fill="rgba(0,212,255,0.10)"
              />
              <Area
                type="monotone"
                dataKey="purchase"
                stroke="#0066FF"
                strokeWidth={2}
                fill="rgba(0,102,255,0.10)"
              />
            </AreaChart>
          </ResponsiveContainer>
          <div className="flex items-center gap-4 mt-2">
            <div className="flex items-center gap-1.5">
              <div className="w-3 h-0.5 rounded" style={{ background: "#00D4FF" }} />
                  <span className="text-xs" style={{ color: "rgba(200,216,232,0.6)" }}>销售额</span>
            </div>
            <div className="flex items-center gap-1.5">
              <div className="w-3 h-0.5 rounded" style={{ background: "#0066FF" }} />
                  <span className="text-xs" style={{ color: "rgba(200,216,232,0.6)" }}>采购额</span>
            </div>
          </div>
        </div>

        {/* Inventory pie */}
        <div className="glass-card rounded-xl p-5">
          <div className="mb-4">
            <h3 className="text-sm font-semibold" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
              库存状态分布
            </h3>
            <p className="text-xs mt-0.5" style={{ color: "rgba(200,216,232,0.45)" }}>实时库存概览</p>
          </div>
          <ResponsiveContainer width="100%" height={160}>
            <PieChart>
              <Pie
                data={inventoryPie}
                cx="50%"
                cy="50%"
                innerRadius={45}
                outerRadius={70}
                paddingAngle={3}
                dataKey="value"
              >
                {inventoryPie.map((entry, index) => (
                  <Cell key={index} fill={entry.color} opacity={0.85} />
                ))}
              </Pie>
              <Tooltip
                contentStyle={{
                  background: "#0A1628",
                  border: "1px solid rgba(0,212,255,0.2)",
                  borderRadius: "6px",
                  fontSize: "12px",
                  color: "#E0F4FF",
                }}
              />
            </PieChart>
          </ResponsiveContainer>
          <div className="space-y-1.5 mt-2">
            {inventoryPie.map((item) => (
              <div key={item.name} className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <div className="w-2 h-2 rounded-full" style={{ background: item.color }} />
                  <span className="text-xs" style={{ color: "rgba(200,216,232,0.65)", fontFamily: "Noto Sans SC" }}>
                    {item.name}
                  </span>
                </div>
                <span className="text-xs font-mono" style={{ color: item.color }}>
                  {item.value}%
                </span>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Bottom row */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        {/* Recent orders */}
        <div className="lg:col-span-2 glass-card rounded-xl p-5">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-sm font-semibold" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
              最近单据
            </h3>
            <button
              className="text-xs px-3 py-1 rounded"
              style={{
                background: "#162233",
                border: "1px solid rgba(0,212,255,0.2)",
                color: "#00D4FF",
              }}
            >
              查看全部
            </button>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead>
                <tr style={{ borderBottom: "1px solid rgba(0,212,255,0.1)" }}>
                  {["单据编号", "类型", "对象", "金额", "状态", "日期"].map((h) => (
                    <th
                      key={h}
                      className="text-left pb-2 pr-4 font-medium"
                  style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}
                  >
                    {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {recentOrders.map((order) => (
                  <tr
                    key={order.id}
                    className="table-row-hover"
                    style={{ borderBottom: "1px solid rgba(0,212,255,0.05)" }}
                  >
                    <td className="py-2.5 pr-4 font-mono" style={{ color: "#C8D8E8" }}>
                      {order.id}
                    </td>
                    <td className="py-2.5 pr-4">
                      <span
                        className="px-2 py-0.5 rounded text-xs"
                        style={{
                          background: order.type === "采购" ? "rgba(50,149,201,0.14)" : "rgba(80,187,227,0.14)",
                          color: order.type === "采购" ? "#3295C9" : "#50BBE3",
                          border: `1px solid ${order.type === "采购" ? "rgba(50,149,201,0.28)" : "rgba(80,187,227,0.28)"}`,
                        }}
                      >
                        {order.type}
                      </span>
                    </td>
                    <td className="py-2.5 pr-4" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
                      {order.supplier || order.customer}
                    </td>
                    <td className="py-2.5 pr-4 font-mono" style={{ color: "#E0F4FF" }}>
                      ¥{order.amount.toLocaleString()}
                    </td>
                    <td className="py-2.5 pr-4">
                      <span
                        className={`px-2 py-0.5 rounded text-xs ${
                          order.status === "已完成" || order.status === "已入库" || order.status === "已发货"
                            ? "badge-success"
                            : order.status === "待审核" || order.status === "待发货"
                            ? "badge-warning"
                            : "badge-info"
                        }`}
                      >
                        {order.status}
                      </span>
                    </td>
                    <td className="py-2.5" style={{ color: "rgba(200,216,232,0.55)" }}>
                      {order.date}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        {/* Alerts */}
        <div className="glass-card rounded-xl p-5">
          <div className="flex items-center gap-2 mb-4">
            <AlertTriangle size={15} style={{ color: "#C99A45" }} />
            <h3 className="text-sm font-semibold" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
              库存预警
            </h3>
            <span
              className="ml-auto text-xs px-2 py-0.5 rounded-full"
              style={{ background: "rgba(201,87,69,0.12)", color: "#C95745", border: "1px solid rgba(255,255,255,0.10)" }}
            >
              {alerts.length}
            </span>
          </div>
          <div className="space-y-3">
            {alerts.map((alert, i) => (
              <div
                key={i}
                className="p-3 rounded-lg"
                style={{
                  background: "#162233",
                  border: "1px solid rgba(255,255,255,0.10)",
                }}
              >
                <p className="text-xs mb-1.5 font-medium" style={{ color: "rgba(224,244,255,0.7)", fontFamily: "Noto Sans SC" }}>
                  {alert.item}
                </p>
                <div className="flex items-center justify-between mb-1.5">
                  <span className="text-xs" style={{ color: "rgba(200,216,232,0.55)" }}>
                    当前: <span style={{ color: alert.level === "danger" ? "#C95745" : "#C99A45" }}>{alert.stock}</span>
                    <span style={{ color: "rgba(200,216,232,0.5)" }}>{" / "}最低: {alert.min}</span>
                  </span>
                </div>
                <div
                  className="h-1 rounded-full overflow-hidden"
                  style={{ background: "rgba(255,255,255,0.08)" }}
                >
                  <div
                    className="h-full rounded-full"
                    style={{
                      width: `${(alert.stock / alert.min) * 100}%`,
                      background: alert.level === "danger" ? "#C95745" : "#C99A45",
                      boxShadow: `0 0 6px ${alert.level === "danger" ? "rgba(201,87,69,0.6)" : "rgba(201,154,69,0.6)"}`,
                    }}
                  />
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </DashboardLayout>
  );
}
