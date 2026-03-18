/**
 * ProfilePage — 个人设置
 * Deep Quantum Theme: dark navy bg, cyan glow accents
 * Tabs: 基本信息 / 修改密码 / 通知偏好 / 安全设置
 */
import { useState } from "react";
import { toast } from "sonner";
import DashboardLayout from "@/components/DashboardLayout";
import {
  User,
  Lock,
  Bell,
  Shield,
  Camera,
  Save,
  Eye,
  EyeOff,
  Mail,
  Phone,
  Building2,
  MapPin,
  CheckCircle2,
  Smartphone,
  Monitor,
  Globe,
} from "lucide-react";

const TABS = [
  { id: "basic", label: "基本信息", icon: User },
  { id: "password", label: "修改密码", icon: Lock },
  { id: "notification", label: "通知偏好", icon: Bell },
  { id: "security", label: "安全设置", icon: Shield },
];

// ─── 基本信息 Tab ───────────────────────────────────────────────
function BasicInfoTab() {
  const [form, setForm] = useState({
    name: "系统管理员",
    username: "admin",
    email: "admin@frontcrm.com",
    phone: "138-0000-0000",
    department: "信息技术部",
    position: "系统管理员",
    location: "上海市浦东新区",
    bio: "负责 FrontCRM 系统的日常运维与管理工作。",
  });

  const handleSave = () => {
    toast.success("个人信息已保存");
  };

  return (
    <div className="space-y-6">
      {/* 头像区域 */}
      <div
        className="flex items-center gap-6 p-6 rounded-lg"
        style={{ background: "#0D1F35", border: "1px solid rgba(0,212,255,0.12)" }}
      >
        <div className="relative">
          <div
            className="w-20 h-20 rounded-full flex items-center justify-center text-2xl font-bold"
            style={{
              background: "linear-gradient(135deg, #00D4FF, #0066FF)",
              color: "#0A1628",
              boxShadow: "0 0 24px rgba(0,212,255,0.4)",
            }}
          >
            管
          </div>
          <button
            className="absolute bottom-0 right-0 w-7 h-7 rounded-full flex items-center justify-center transition-all hover:scale-110"
            style={{ background: "#162233", border: "2px solid rgba(0,212,255,0.4)" }}
            onClick={() => toast.info("头像上传功能即将上线")}
          >
            <Camera size={13} style={{ color: "#00D4FF" }} />
          </button>
        </div>
        <div>
          <div className="text-base font-semibold" style={{ color: "#E0F4FF" }}>{form.name}</div>
          <div className="text-sm mt-1" style={{ color: "rgba(200,216,232,0.55)" }}>{form.email}</div>
          <div
            className="inline-flex items-center gap-1 mt-2 px-2 py-0.5 rounded text-xs"
            style={{ background: "rgba(0,212,255,0.1)", color: "#00D4FF", border: "1px solid rgba(0,212,255,0.2)" }}
          >
            <CheckCircle2 size={11} />
            已认证账号
          </div>
        </div>
      </div>

      {/* 表单 */}
      <div
        className="p-6 rounded-lg space-y-5"
        style={{ background: "#0D1F35", border: "1px solid rgba(0,212,255,0.12)" }}
      >
        <div className="text-sm font-semibold mb-4" style={{ color: "#00D4FF" }}>基本资料</div>
        <div className="grid grid-cols-2 gap-4">
          {[
            { label: "姓名", key: "name", icon: User },
            { label: "用户名", key: "username", icon: User },
            { label: "邮箱", key: "email", icon: Mail },
            { label: "手机号", key: "phone", icon: Phone },
            { label: "所属部门", key: "department", icon: Building2 },
            { label: "职位", key: "position", icon: Building2 },
            { label: "所在地区", key: "location", icon: MapPin },
          ].map(({ label, key, icon: Icon }) => (
            <div key={key}>
              <label className="block text-xs mb-1.5" style={{ color: "rgba(200,216,232,0.6)" }}>{label}</label>
              <div className="relative">
                <Icon size={13} className="absolute left-3 top-1/2 -translate-y-1/2" style={{ color: "rgba(0,212,255,0.4)" }} />
                <input
                  className="w-full pl-8 pr-3 py-2 rounded text-sm outline-none transition-all"
                  style={{
                    background: "#162233",
                    border: "1px solid rgba(0,212,255,0.15)",
                    color: "#E0F4FF",
                  }}
                  value={(form as any)[key]}
                  onChange={e => setForm(f => ({ ...f, [key]: e.target.value }))}
                  onFocus={e => (e.target.style.borderColor = "rgba(0,212,255,0.5)")}
                  onBlur={e => (e.target.style.borderColor = "rgba(0,212,255,0.15)")}
                />
              </div>
            </div>
          ))}
        </div>

        {/* 个人简介 */}
        <div>
          <label className="block text-xs mb-1.5" style={{ color: "rgba(200,216,232,0.6)" }}>个人简介</label>
          <textarea
            rows={3}
            className="w-full px-3 py-2 rounded text-sm outline-none resize-none transition-all"
            style={{
              background: "#162233",
              border: "1px solid rgba(0,212,255,0.15)",
              color: "#E0F4FF",
            }}
            value={form.bio}
            onChange={e => setForm(f => ({ ...f, bio: e.target.value }))}
            onFocus={e => (e.target.style.borderColor = "rgba(0,212,255,0.5)")}
            onBlur={e => (e.target.style.borderColor = "rgba(0,212,255,0.15)")}
          />
        </div>

        <div className="flex justify-end pt-2">
          <button
            className="flex items-center gap-2 px-5 py-2 rounded text-sm font-medium transition-all hover:scale-105"
            style={{
              background: "linear-gradient(135deg, rgba(0,212,255,0.2), rgba(0,102,255,0.2))",
              border: "1px solid rgba(0,212,255,0.4)",
              color: "#00D4FF",
              boxShadow: "0 0 12px rgba(0,212,255,0.15)",
            }}
            onClick={handleSave}
          >
            <Save size={14} />
            保存修改
          </button>
        </div>
      </div>
    </div>
  );
}

// ─── 修改密码 Tab ───────────────────────────────────────────────
function PasswordTab() {
  const [form, setForm] = useState({ current: "", next: "", confirm: "" });
  const [show, setShow] = useState({ current: false, next: false, confirm: false });

  const strength = (() => {
    const p = form.next;
    if (!p) return 0;
    let s = 0;
    if (p.length >= 8) s++;
    if (/[A-Z]/.test(p)) s++;
    if (/[0-9]/.test(p)) s++;
    if (/[^A-Za-z0-9]/.test(p)) s++;
    return s;
  })();

  const strengthLabel = ["", "弱", "中", "强", "非常强"][strength];
  const strengthColor = ["", "#FF6B35", "#FFB800", "#00D4FF", "#00FF88"][strength];

  const handleSave = () => {
    if (!form.current) { toast.error("请输入当前密码"); return; }
    if (form.next.length < 8) { toast.error("新密码至少 8 位"); return; }
    if (form.next !== form.confirm) { toast.error("两次密码不一致"); return; }
    toast.success("密码修改成功");
    setForm({ current: "", next: "", confirm: "" });
  };

  return (
    <div
      className="p-6 rounded-lg space-y-5 max-w-lg"
      style={{ background: "#0D1F35", border: "1px solid rgba(0,212,255,0.12)" }}
    >
      <div className="text-sm font-semibold mb-2" style={{ color: "#00D4FF" }}>修改登录密码</div>
      <p className="text-xs" style={{ color: "rgba(200,216,232,0.5)" }}>
        为保障账号安全，建议定期更换密码，且不要与其他平台使用相同密码。
      </p>

      {[
        { label: "当前密码", key: "current" as const },
        { label: "新密码", key: "next" as const },
        { label: "确认新密码", key: "confirm" as const },
      ].map(({ label, key }) => (
        <div key={key}>
          <label className="block text-xs mb-1.5" style={{ color: "rgba(200,216,232,0.6)" }}>{label}</label>
          <div className="relative">
            <Lock size={13} className="absolute left-3 top-1/2 -translate-y-1/2" style={{ color: "rgba(0,212,255,0.4)" }} />
            <input
              type={show[key] ? "text" : "password"}
              className="w-full pl-8 pr-10 py-2 rounded text-sm outline-none transition-all"
              style={{
                background: "#162233",
                border: "1px solid rgba(0,212,255,0.15)",
                color: "#E0F4FF",
              }}
              placeholder={`请输入${label}`}
              value={form[key]}
              onChange={e => setForm(f => ({ ...f, [key]: e.target.value }))}
              onFocus={e => (e.target.style.borderColor = "rgba(0,212,255,0.5)")}
              onBlur={e => (e.target.style.borderColor = "rgba(0,212,255,0.15)")}
            />
            <button
              className="absolute right-3 top-1/2 -translate-y-1/2"
              style={{ color: "rgba(0,212,255,0.4)" }}
              onClick={() => setShow(s => ({ ...s, [key]: !s[key] }))}
            >
              {show[key] ? <EyeOff size={14} /> : <Eye size={14} />}
            </button>
          </div>
          {/* 密码强度条 */}
          {key === "next" && form.next && (
            <div className="mt-2">
              <div className="flex gap-1 mb-1">
                {[1, 2, 3, 4].map(i => (
                  <div
                    key={i}
                    className="h-1 flex-1 rounded-full transition-all"
                    style={{ background: i <= strength ? strengthColor : "rgba(255,255,255,0.1)" }}
                  />
                ))}
              </div>
              <span className="text-xs" style={{ color: strengthColor }}>密码强度：{strengthLabel}</span>
            </div>
          )}
        </div>
      ))}

      <div className="flex justify-end pt-2">
        <button
          className="flex items-center gap-2 px-5 py-2 rounded text-sm font-medium transition-all hover:scale-105"
          style={{
            background: "linear-gradient(135deg, rgba(0,212,255,0.2), rgba(0,102,255,0.2))",
            border: "1px solid rgba(0,212,255,0.4)",
            color: "#00D4FF",
            boxShadow: "0 0 12px rgba(0,212,255,0.15)",
          }}
          onClick={handleSave}
        >
          <Save size={14} />
          确认修改
        </button>
      </div>
    </div>
  );
}

// ─── 通知偏好 Tab ───────────────────────────────────────────────
function NotificationTab() {
  const [prefs, setPrefs] = useState({
    newOrder: true,
    lowStock: true,
    paymentDue: true,
    systemUpdate: false,
    emailDigest: true,
    smsAlert: false,
    browserPush: true,
  });

  const toggle = (key: keyof typeof prefs) => setPrefs(p => ({ ...p, [key]: !p[key] }));

  const groups = [
    {
      title: "业务通知",
      icon: Bell,
      items: [
        { key: "newOrder" as const, label: "新订单提醒", desc: "有新的销售或采购订单时通知" },
        { key: "lowStock" as const, label: "库存预警", desc: "库存低于安全库存时通知" },
        { key: "paymentDue" as const, label: "付款到期提醒", desc: "付款单到期前 3 天通知" },
        { key: "systemUpdate" as const, label: "系统更新通知", desc: "系统版本更新时通知" },
      ],
    },
    {
      title: "通知渠道",
      icon: Smartphone,
      items: [
        { key: "emailDigest" as const, label: "邮件日报", desc: "每日汇总邮件发送至注册邮箱" },
        { key: "smsAlert" as const, label: "短信提醒", desc: "重要事项发送短信至绑定手机" },
        { key: "browserPush" as const, label: "浏览器推送", desc: "在浏览器中显示桌面通知" },
      ],
    },
  ];

  return (
    <div className="space-y-4">
      {groups.map(({ title, icon: Icon, items }) => (
        <div
          key={title}
          className="p-6 rounded-lg"
          style={{ background: "#0D1F35", border: "1px solid rgba(0,212,255,0.12)" }}
        >
          <div className="flex items-center gap-2 mb-4">
            <Icon size={15} style={{ color: "#00D4FF" }} />
            <span className="text-sm font-semibold" style={{ color: "#00D4FF" }}>{title}</span>
          </div>
          <div className="space-y-4">
            {items.map(({ key, label, desc }) => (
              <div key={key} className="flex items-center justify-between">
                <div>
                  <div className="text-sm" style={{ color: "#E0F4FF" }}>{label}</div>
                  <div className="text-xs mt-0.5" style={{ color: "rgba(200,216,232,0.45)" }}>{desc}</div>
                </div>
                <button
                  onClick={() => toggle(key)}
                  className="relative w-11 h-6 rounded-full transition-all flex-shrink-0"
                  style={{
                    background: prefs[key]
                      ? "linear-gradient(135deg, #00D4FF, #0066FF)"
                      : "rgba(255,255,255,0.1)",
                    boxShadow: prefs[key] ? "0 0 10px rgba(0,212,255,0.4)" : "none",
                  }}
                >
                  <span
                    className="absolute top-1 w-4 h-4 rounded-full transition-all"
                    style={{
                      background: "#fff",
                      left: prefs[key] ? "calc(100% - 20px)" : "4px",
                      boxShadow: "0 1px 4px rgba(0,0,0,0.3)",
                    }}
                  />
                </button>
              </div>
            ))}
          </div>
        </div>
      ))}
      <div className="flex justify-end">
        <button
          className="flex items-center gap-2 px-5 py-2 rounded text-sm font-medium transition-all hover:scale-105"
          style={{
            background: "linear-gradient(135deg, rgba(0,212,255,0.2), rgba(0,102,255,0.2))",
            border: "1px solid rgba(0,212,255,0.4)",
            color: "#00D4FF",
            boxShadow: "0 0 12px rgba(0,212,255,0.15)",
          }}
          onClick={() => toast.success("通知偏好已保存")}
        >
          <Save size={14} />
          保存偏好
        </button>
      </div>
    </div>
  );
}

// ─── 安全设置 Tab ───────────────────────────────────────────────
function SecurityTab() {
  const sessions = [
    { device: "Chrome / Windows 11", location: "上海市", ip: "101.88.xx.xx", time: "当前会话", current: true },
    { device: "Safari / iPhone 15", location: "上海市", ip: "101.88.xx.xx", time: "2 小时前", current: false },
    { device: "Chrome / macOS", location: "北京市", ip: "120.52.xx.xx", time: "昨天 14:32", current: false },
  ];

  return (
    <div className="space-y-4">
      {/* 两步验证 */}
      <div
        className="p-6 rounded-lg"
        style={{ background: "#0D1F35", border: "1px solid rgba(0,212,255,0.12)" }}
      >
        <div className="flex items-center gap-2 mb-4">
          <Shield size={15} style={{ color: "#00D4FF" }} />
          <span className="text-sm font-semibold" style={{ color: "#00D4FF" }}>两步验证</span>
        </div>
        <div className="flex items-center justify-between">
          <div>
            <div className="text-sm" style={{ color: "#E0F4FF" }}>短信验证码（已绑定 138****0000）</div>
            <div className="text-xs mt-0.5" style={{ color: "rgba(200,216,232,0.45)" }}>登录时需要输入手机验证码，提升账号安全性</div>
          </div>
          <div
            className="px-3 py-1 rounded text-xs font-medium"
            style={{ background: "rgba(0,212,255,0.1)", color: "#00D4FF", border: "1px solid rgba(0,212,255,0.2)" }}
          >
            已开启
          </div>
        </div>
        <div className="mt-4 flex gap-3">
          <button
            className="px-4 py-1.5 rounded text-xs transition-all hover:scale-105"
            style={{ background: "rgba(0,212,255,0.1)", border: "1px solid rgba(0,212,255,0.2)", color: "#00D4FF" }}
            onClick={() => toast.info("更换手机号功能即将上线")}
          >
            更换手机号
          </button>
          <button
            className="px-4 py-1.5 rounded text-xs transition-all hover:scale-105"
            style={{ background: "rgba(255,107,53,0.1)", border: "1px solid rgba(255,107,53,0.2)", color: "#FF6B35" }}
            onClick={() => toast.warning("关闭两步验证会降低账号安全性")}
          >
            关闭验证
          </button>
        </div>
      </div>

      {/* 登录设备 */}
      <div
        className="p-6 rounded-lg"
        style={{ background: "#0D1F35", border: "1px solid rgba(0,212,255,0.12)" }}
      >
        <div className="flex items-center gap-2 mb-4">
          <Monitor size={15} style={{ color: "#00D4FF" }} />
          <span className="text-sm font-semibold" style={{ color: "#00D4FF" }}>登录设备管理</span>
        </div>
        <div className="space-y-3">
          {sessions.map((s, i) => (
            <div
              key={i}
              className="flex items-center justify-between p-3 rounded"
              style={{ background: "#162233", border: `1px solid ${s.current ? "rgba(0,212,255,0.3)" : "rgba(0,212,255,0.08)"}` }}
            >
              <div className="flex items-center gap-3">
                <Globe size={16} style={{ color: s.current ? "#00D4FF" : "rgba(0,212,255,0.4)" }} />
                <div>
                  <div className="text-xs font-medium" style={{ color: s.current ? "#00D4FF" : "#E0F4FF" }}>
                    {s.device}
                    {s.current && (
                      <span
                        className="ml-2 px-1.5 py-0.5 rounded text-xs"
                        style={{ background: "rgba(0,212,255,0.15)", color: "#00D4FF", fontSize: "10px" }}
                      >
                        当前
                      </span>
                    )}
                  </div>
                  <div className="text-xs mt-0.5" style={{ color: "rgba(200,216,232,0.45)" }}>
                    {s.location} · {s.ip} · {s.time}
                  </div>
                </div>
              </div>
              {!s.current && (
                <button
                  className="text-xs px-3 py-1 rounded transition-all"
                  style={{ background: "rgba(255,107,53,0.1)", color: "#FF6B35", border: "1px solid rgba(255,107,53,0.2)" }}
                  onClick={() => toast.success("已强制退出该设备")}
                >
                  强制退出
                </button>
              )}
            </div>
          ))}
        </div>
        <button
          className="mt-3 text-xs px-4 py-1.5 rounded transition-all"
          style={{ background: "rgba(255,107,53,0.1)", color: "#FF6B35", border: "1px solid rgba(255,107,53,0.2)" }}
          onClick={() => toast.success("已退出所有其他设备")}
        >
          退出所有其他设备
        </button>
      </div>
    </div>
  );
}

// ─── 主页面 ─────────────────────────────────────────────────────
export default function ProfilePage() {
  const [activeTab, setActiveTab] = useState("basic");

  const renderTab = () => {
    switch (activeTab) {
      case "basic": return <BasicInfoTab />;
      case "password": return <PasswordTab />;
      case "notification": return <NotificationTab />;
      case "security": return <SecurityTab />;
      default: return null;
    }
  };

  return (
    <DashboardLayout title="个人设置">
      <div className="max-w-3xl">
        {/* Tab 导航 */}
        <div
          className="flex gap-1 p-1 rounded-lg mb-6"
          style={{ background: "#0D1F35", border: "1px solid rgba(0,212,255,0.12)", display: "inline-flex" }}
        >
          {TABS.map(({ id, label, icon: Icon }) => {
            const active = activeTab === id;
            return (
              <button
                key={id}
                onClick={() => setActiveTab(id)}
                className="flex items-center gap-2 px-4 py-2 rounded text-sm transition-all"
                style={{
                  background: active
                    ? "linear-gradient(135deg, rgba(0,212,255,0.15), rgba(0,102,255,0.15))"
                    : "transparent",
                  color: active ? "#00D4FF" : "rgba(200,216,232,0.55)",
                  border: active ? "1px solid rgba(0,212,255,0.3)" : "1px solid transparent",
                  boxShadow: active ? "0 0 12px rgba(0,212,255,0.1)" : "none",
                }}
              >
                <Icon size={14} />
                {label}
              </button>
            );
          })}
        </div>

        {/* Tab 内容 */}
        {renderTab()}
      </div>
    </DashboardLayout>
  );
}
