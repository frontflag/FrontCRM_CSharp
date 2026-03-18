/**
 * ReportsPage — FrontCRM Deep Quantum Theme
 * Business analytics with multiple chart types and data insights
 */
import DashboardLayout from "@/components/DashboardLayout";
import {
  AreaChart, Area, BarChart, Bar, LineChart, Line,
  XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
  PieChart, Pie, Cell, RadarChart, Radar, PolarGrid,
  PolarAngleAxis, PolarRadiusAxis
} from "recharts";
import { TrendingUp, BarChart3, PieChart as PieIcon, Activity } from "lucide-react";

const monthlyData = [
  { month: "1月", sales: 420, purchase: 280, profit: 140 },
  { month: "2月", sales: 380, purchase: 310, profit: 70 },
  { month: "3月", sales: 510, purchase: 350, profit: 160 },
  { month: "4月", sales: 470, purchase: 290, profit: 180 },
  { month: "5月", sales: 620, purchase: 420, profit: 200 },
  { month: "6月", sales: 580, purchase: 380, profit: 200 },
  { month: "7月", sales: 710, purchase: 460, profit: 250 },
  { month: "8月", sales: 680, purchase: 440, profit: 240 },
  { month: "9月", sales: 750, purchase: 510, profit: 240 },
  { month: "10月", sales: 820, purchase: 550, profit: 270 },
  { month: "11月", sales: 790, purchase: 520, profit: 270 },
  { month: "12月", sales: 950, purchase: 630, profit: 320 },
];

const categoryData = [
  { name: "手机", value: 35, color: "#50BBE3" },
  { name: "笔记本", value: 25, color: "#3295C9" },
  { name: "平板", value: 18, color: "#46BF91" },
  { name: "音频", value: 12, color: "#C99A45" },
  { name: "外设", value: 10, color: "#C95745" },
];

const topCustomers = [
  { name: "杭州互联网公司", amount: 560, orders: 78 },
  { name: "北京数码科技", amount: 420, orders: 62 },
  { name: "广州贸易集团", amount: 280, orders: 45 },
  { name: "成都智能科技", amount: 192, orders: 33 },
  { name: "深圳科技有限公司", amount: 156, orders: 28 },
];

const radarData = [
  { subject: "销售额", A: 90, fullMark: 100 },
  { subject: "采购效率", A: 75, fullMark: 100 },
  { subject: "库存周转", A: 82, fullMark: 100 },
  { subject: "客户满意度", A: 88, fullMark: 100 },
  { subject: "利润率", A: 68, fullMark: 100 },
  { subject: "供应商评分", A: 85, fullMark: 100 },
];

const CustomTooltip = ({ active, payload, label }: any) => {
  if (active && payload && payload.length) {
    return (
      <div
        className="px-3 py-2 rounded text-xs"
        style={{ background: "#0A1628", border: "1px solid rgba(0,212,255,0.2)", backdropFilter: "blur(8px)" }}
      >
        <p style={{ color: "rgba(0,212,255,0.7)" }} className="mb-1">{label}</p>
        {payload.map((p: any) => (
          <p key={p.name} style={{ color: p.color }}>
            {p.name}: ¥{p.value}万
          </p>
        ))}
      </div>
    );
  }
  return null;
};

export default function ReportsPage() {
  const totalSales = monthlyData.reduce((s, d) => s + d.sales, 0);
  const totalProfit = monthlyData.reduce((s, d) => s + d.profit, 0);
  const profitRate = ((totalProfit / totalSales) * 100).toFixed(1);

  return (
    <DashboardLayout title="报表分析">
      {/* Summary KPIs */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        {[
          { label: "年度销售总额", value: `¥${totalSales}万`, color: "#50BBE3", icon: TrendingUp },
          { label: "年度采购总额", value: `¥${monthlyData.reduce((s, d) => s + d.purchase, 0)}万`, color: "#3295C9", icon: BarChart3 },
          { label: "年度利润", value: `¥${totalProfit}万`, color: "#46BF91", icon: Activity },
          { label: "综合利润率", value: `${profitRate}%`, color: "#C99A45", icon: PieIcon },
        ].map((kpi) => (
          <div key={kpi.label} className="glass-card rounded-xl px-5 py-4">
            <div className="flex items-center justify-between mb-2">
              <p className="text-xs" style={{ color: "rgba(200,216,232,0.6)", fontFamily: "Noto Sans SC" }}>{kpi.label}</p>
              <kpi.icon size={14} style={{ color: kpi.color }} />
            </div>
            <span className="text-xl font-bold" style={{ fontFamily: "Space Mono", color: kpi.color }}>{kpi.value}</span>
          </div>
        ))}
      </div>

      {/* Main charts row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 mb-4">
        {/* Annual trend */}
        <div className="glass-card rounded-xl p-5">
          <h3 className="text-sm font-semibold mb-4" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
            年度销售/采购/利润趋势
          </h3>
          <ResponsiveContainer width="100%" height={220}>
            <AreaChart data={monthlyData}>
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" />
              <XAxis dataKey="month" tick={{ fill: "#6B7A8D", fontSize: 10 }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fill: "#6B7A8D", fontSize: 10 }} axisLine={false} tickLine={false} />
              <Tooltip content={<CustomTooltip />} />
              <Area type="monotone" dataKey="sales" stroke="#50BBE3" strokeWidth={1.5} fill="rgba(0,212,255,0.10)" name="销售额" />
              <Area type="monotone" dataKey="purchase" stroke="#0066FF" strokeWidth={1.5} fill="rgba(0,102,255,0.10)" name="采购额" />
              <Area type="monotone" dataKey="profit" stroke="#46BF91" strokeWidth={1.5} fill="rgba(70,191,145,0.10)" name="利润" />
            </AreaChart>
          </ResponsiveContainer>
          <div className="flex items-center gap-4 mt-2">
            {[["#00D4FF", "销售额"], ["#0066FF", "采购额"], ["#00FF88", "利润"]].map(([color, label]) => (
              <div key={label} className="flex items-center gap-1.5">
                <div className="w-3 h-0.5 rounded" style={{ background: color }} />
                <span className="text-xs" style={{ color: "rgba(200,216,232,0.6)" }}>{label}</span>
              </div>
            ))}
          </div>
        </div>

        {/* Category distribution */}
        <div className="glass-card rounded-xl p-5">
          <h3 className="text-sm font-semibold mb-4" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
            商品类别销售占比
          </h3>
          <div className="flex items-center gap-4">
            <ResponsiveContainer width="50%" height={200}>
              <PieChart>
                <Pie
                  data={categoryData}
                  cx="50%"
                  cy="50%"
                  innerRadius={55}
                  outerRadius={80}
                  paddingAngle={3}
                  dataKey="value"
                >
                  {categoryData.map((entry, index) => (
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
            <div className="flex-1 space-y-2">
              {categoryData.map((item) => (
                <div key={item.name}>
                  <div className="flex items-center justify-between mb-1">
                    <div className="flex items-center gap-2">
                      <div className="w-2 h-2 rounded-full" style={{ background: item.color }} />
                      <span className="text-xs" style={{ color: "rgba(224,244,255,0.7)", fontFamily: "Noto Sans SC" }}>{item.name}</span>
                    </div>
                    <span className="text-xs font-mono" style={{ color: item.color }}>{item.value}%</span>
                  </div>
                  <div className="h-1 rounded-full overflow-hidden" style={{ background: "rgba(255,255,255,0.06)" }}>
                    <div
                      className="h-full rounded-full"
                      style={{ width: `${item.value}%`, background: item.color, boxShadow: `0 0 4px ${item.color}` }}
                    />
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* Bottom row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Top customers bar */}
        <div className="glass-card rounded-xl p-5">
          <h3 className="text-sm font-semibold mb-4" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
            TOP 5 客户销售额（万元）
          </h3>
          <ResponsiveContainer width="100%" height={200}>
            <BarChart data={topCustomers} layout="vertical" barSize={16}>
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(0,212,255,0.06)" horizontal={false} />
              <XAxis type="number" tick={{ fill: "#6B7A8D", fontSize: 10 }} axisLine={false} tickLine={false} />
              <YAxis
                type="category"
                dataKey="name"
                tick={{ fill: "rgba(224,244,255,0.6)", fontSize: 10, fontFamily: "Noto Sans SC" }}
                axisLine={false}
                tickLine={false}
                width={100}
              />
              <Tooltip
                contentStyle={{
                  background: "#0A1628",
                  border: "1px solid rgba(0,212,255,0.2)",
                  borderRadius: "6px",
                  fontSize: "12px",
                  color: "#E0F4FF",
                }}
              />
              <Bar dataKey="amount" radius={[0, 3, 3, 0]}>
                {topCustomers.map((_, index) => (
                  <Cell
                    key={index}
                    fill={`rgba(0,${212 - index * 20},255,${0.8 - index * 0.1})`}
                  />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>

        {/* Radar chart */}
        <div className="glass-card rounded-xl p-5">
          <h3 className="text-sm font-semibold mb-4" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
            业务综合评分雷达图
          </h3>
          <ResponsiveContainer width="100%" height={200}>
            <RadarChart data={radarData}>
              <PolarGrid stroke="rgba(255,255,255,0.05)" />
              <PolarAngleAxis
                dataKey="subject"
                tick={{ fill: "#6B7A8D", fontSize: 10, fontFamily: "Noto Sans SC" }}
              />
              <PolarRadiusAxis
                angle={30}
                domain={[0, 100]}
                tick={{ fill: "#6B7A8D", fontSize: 9 }}
                axisLine={false}
              />
              <Radar
                name="评分"
                dataKey="A"
                stroke="#50BBE3"
                fill="#00D4FF"
                fillOpacity={0.15}
                strokeWidth={1.5}
              />
              <Tooltip
                contentStyle={{
                  background: "#0A1628",
                  border: "1px solid rgba(0,212,255,0.2)",
                  borderRadius: "6px",
                  fontSize: "12px",
                  color: "#E0F4FF",
                }}
              />
            </RadarChart>
          </ResponsiveContainer>
        </div>
      </div>
    </DashboardLayout>
  );
}
