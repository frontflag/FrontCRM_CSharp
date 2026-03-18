/**
 * CustomerPage — FrontCRM Deep Quantum Theme
 * Customer management with real backend API integration
 * Design: Deep Quantum (#192A3F bg, #0A1628 card, #162233 inner panel)
 * Accent: #00D4FF (cyan), #3295C9 (steel blue), #50BBE3 (ice blue)
 */
import { useState, useEffect, useCallback } from "react";
import { useLocation } from "wouter";
import DashboardLayout from "@/components/DashboardLayout";
import { toast } from "sonner";
import {
  Search, Plus, Users, Phone, Mail, Building2, Star,
  ChevronLeft, ChevronRight, Edit2, Trash2, Eye,
  X, Check, AlertCircle, Loader2, RefreshCw,
  CreditCard, MapPin, Clock, MessageSquare, UserCheck, Ban
} from "lucide-react";
import {
  customerApi,
  type Customer,
  type CustomerContact,
  type CustomerAddress,
  type CustomerBankInfo,
  type CustomerContactHistory,
  type CreateCustomerRequest,
  type AddContactRequest,
  type AddAddressRequest,
  type AddBankRequest,
  type AddContactHistoryRequest,
} from "@/lib/customerApi";
import { PROVINCES, getCities } from "@/lib/chinaRegions";
// ─── Constants ────────────────────────────────────────────────────────────────

const LEVEL_MAP: Record<string, { label: string; color: string; bg: string; num: number }> = {
  "D":   { label: "D级",  color: "rgba(200,216,232,0.6)", bg: "rgba(200,216,232,0.06)", num: 1 },
  "C":   { label: "C级",  color: "#50BBE3",               bg: "rgba(80,187,227,0.1)",   num: 2 },
  "B":   { label: "B级",  color: "#3295C9",               bg: "rgba(50,149,201,0.1)",   num: 3 },
  "BPO": { label: "BPO",  color: "#6DBFA0",               bg: "rgba(109,191,160,0.1)",  num: 4 },
  "VIP": { label: "VIP",  color: "#C9A96E",               bg: "rgba(201,169,110,0.1)",  num: 5 },
  "VPO": { label: "VPO",  color: "#C97A6E",               bg: "rgba(201,122,110,0.1)",  num: 6 },
};

const CONTACT_TYPE_MAP: Record<string, string> = {
  call: "电话", visit: "拜访", email: "邮件", meeting: "会议", other: "其他",
};

const ADDRESS_TYPE_MAP: Record<number, string> = {
  1: "发货地址", 2: "收货地址", 3: "账单地址", 4: "注册地址",
};

// ─── Sub-components ───────────────────────────────────────────────────────────

function FieldBox({ label, value }: { label: string; value?: string | number | null }) {
  return (
    <div className="p-3 rounded-lg" style={{ background: "#162233", border: "1px solid rgba(255,255,255,0.08)" }}>
      <p className="text-xs mb-1" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>{label}</p>
      <p className="text-xs font-medium truncate" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
        {value || "—"}
      </p>
    </div>
  );
}

function FormInput({
  label, value, onChange, type = "text", required, placeholder, readOnly
}: {
  label: string; value: string; onChange: (v: string) => void;
  type?: string; required?: boolean; placeholder?: string; readOnly?: boolean;
}) {
  return (
    <div>
      <label className="block text-xs mb-1.5" style={{ color: "rgba(0,212,255,0.7)", fontFamily: "Noto Sans SC" }}>
        {label}{required && <span style={{ color: "#C97A6E" }}> *</span>}
      </label>
      <input
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        readOnly={readOnly}
        className="w-full px-3 py-2.5 rounded text-xs outline-none transition-all"
        style={{
          background: readOnly ? "rgba(22,34,51,0.5)" : "#162233",
          border: "1px solid rgba(0,212,255,0.2)",
          color: readOnly ? "rgba(224,244,255,0.4)" : "#E0F4FF",
          fontFamily: "Noto Sans SC",
          cursor: readOnly ? "not-allowed" : "text",
        }}
      />
    </div>
  );
}

function FormSelect({
  label, value, onChange, options
}: {
  label: string; value: string; onChange: (v: string) => void;
  options: { value: string; label: string }[];
}) {
  return (
    <div>
      <label className="block text-xs mb-1.5" style={{ color: "rgba(0,212,255,0.7)", fontFamily: "Noto Sans SC" }}>{label}</label>
      <select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="w-full px-3 py-2.5 rounded text-xs outline-none"
        style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.2)", color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
      >
        {options.map((o) => (
          <option key={o.value} value={o.value} style={{ background: "#162233" }}>{o.label}</option>
        ))}
      </select>
    </div>
  );
}

function ModalWrapper({ title, onClose, children, wide }: {
  title: string; onClose: () => void; children: React.ReactNode; wide?: boolean;
}) {
  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4"
      style={{ background: "rgba(10,22,40,0.85)", backdropFilter: "blur(8px)" }}
      onClick={onClose}
    >
      <div
        className={`w-full ${wide ? "max-w-3xl" : "max-w-lg"} rounded-xl overflow-hidden`}
        style={{ background: "#0A1628", border: "1px solid rgba(0,212,255,0.25)", boxShadow: "0 0 60px rgba(0,212,255,0.1)", maxHeight: "90vh", display: "flex", flexDirection: "column" }}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between px-6 py-4 flex-shrink-0" style={{ borderBottom: "1px solid rgba(0,212,255,0.12)" }}>
          <h3 className="text-sm font-semibold" style={{ color: "#00D4FF", fontFamily: "Orbitron" }}>{title}</h3>
          <button onClick={onClose} className="p-1 rounded hover:bg-white/5 transition-colors">
            <X size={16} style={{ color: "rgba(224,244,255,0.5)" }} />
          </button>
        </div>
        <div className="overflow-y-auto flex-1 px-6 py-5">{children}</div>
      </div>
    </div>
  );
}

// ─── Customer Detail Modal ────────────────────────────────────────────────────

function CustomerDetailModal({
  customer: initialCustomer,
  onClose,
  onUpdated,
}: {
  customer: Customer;
  onClose: () => void;
  onUpdated: () => void;
}) {
  const [customer, setCustomer] = useState<Customer>(initialCustomer);
  const [activeTab, setActiveTab] = useState<"info" | "contacts" | "addresses" | "banks" | "history">("info");
  const [loading, setLoading] = useState(false);

  // Sub-data states
  const [contacts, setContacts] = useState<CustomerContact[]>(initialCustomer.contacts || []);
  const [addresses, setAddresses] = useState<CustomerAddress[]>(initialCustomer.addresses || []);
  const [banks, setBanks] = useState<CustomerBankInfo[]>(initialCustomer.bankAccounts || []);
  const [history, setHistory] = useState<CustomerContactHistory[]>([]);
  const [historyLoaded, setHistoryLoaded] = useState(false);

  // Add forms
  const [showAddContact, setShowAddContact] = useState(false);
  const [showAddAddress, setShowAddAddress] = useState(false);
  const [showAddBank, setShowAddBank] = useState(false);
  const [showAddHistory, setShowAddHistory] = useState(false);
  // Address form state (lifted up to avoid re-mount on parent re-render)
  const [addressForm, setAddressForm] = useState<AddAddressRequest>({ addressType: 1, province: "", city: "", address: "", contactName: "", contactPhone: "", isDefault: false });
  const [addressSaving, setAddressSaving] = useState(false);
  const levelInfo = LEVEL_MAP[customer.customerLevel || "D"] || LEVEL_MAP["D"];

  const loadHistory = useCallback(async () => {
    if (historyLoaded) return;
    const res = await customerApi.getContactHistory(customer.id);
    if (res.success && res.data) {
      setHistory(res.data as CustomerContactHistory[]);
      setHistoryLoaded(true);
    }
  }, [customer.id, historyLoaded]);

  useEffect(() => {
    if (activeTab === "history") loadHistory();
  }, [activeTab, loadHistory]);

  // ── Add Contact ──────────────────────────────────────────────────────────────
  const AddContactForm = () => {
    const [form, setForm] = useState<AddContactRequest>({ contactName: "", mobile: "", email: "", position: "", department: "", isDefault: false });
    const [saving, setSaving] = useState(false);
    const set = (k: keyof AddContactRequest) => (v: string) => setForm((f) => ({ ...f, [k]: v }));

    const save = async () => {
      if (!form.contactName?.trim()) { toast.error("请输入联系人姓名"); return; }
      setSaving(true);
      const res = await customerApi.addContact(customer.id, form);
      setSaving(false);
      if (res.success && res.data) {
        setContacts((prev) => [...prev, res.data!]);
        setShowAddContact(false);
        toast.success("联系人添加成功");
      } else {
        toast.error(res.message || "添加失败");
      }
    };

    return (
      <div className="mt-4 p-4 rounded-lg" style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.15)" }}>
        <p className="text-xs font-medium mb-3" style={{ color: "#00D4FF", fontFamily: "Noto Sans SC" }}>新增联系人</p>
        <div className="grid grid-cols-2 gap-3">
          <FormInput label="姓名" value={form.contactName || ""} onChange={set("contactName")} required />
          <FormInput label="手机" value={form.mobile || ""} onChange={set("mobile")} />
          <FormInput label="邮箱" value={form.email || ""} onChange={set("email")} />
          <FormInput label="职位" value={form.position || ""} onChange={set("position")} />
          <FormInput label="部门" value={form.department || ""} onChange={set("department")} />
          <div className="flex items-center gap-2 pt-5">
            <input type="checkbox" checked={form.isDefault || false} onChange={(e) => setForm((f) => ({ ...f, isDefault: e.target.checked }))} />
            <span className="text-xs" style={{ color: "rgba(224,244,255,0.6)", fontFamily: "Noto Sans SC" }}>设为默认联系人</span>
          </div>
        </div>
        <div className="flex gap-2 mt-3">
          <button onClick={() => setShowAddContact(false)} className="flex-1 py-2 rounded text-xs" style={{ background: "#0A1628", border: "1px solid rgba(255,255,255,0.1)", color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>取消</button>
          <button onClick={save} disabled={saving} className="flex-1 py-2 rounded text-xs font-medium flex items-center justify-center gap-1" style={{ background: "rgba(0,212,255,0.15)", border: "1px solid rgba(0,212,255,0.4)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}>
            {saving ? <Loader2 size={12} className="animate-spin" /> : <Check size={12} />}保存
          </button>
        </div>
      </div>
    );
  };

  // ── Add Address ──────────────────────────────────────────────────────────────
  const AddAddressForm = () => {
    const form = addressForm;
    const setForm = setAddressForm;
    const saving = addressSaving;
    const setSaving = setAddressSaving;
    const availableCities = form.province ? getCities(form.province) : [];
    const set = (k: keyof AddAddressRequest) => (v: string) => setForm((f) => ({ ...f, [k]: v as any }));

    const save = async () => {
      if (!form.address?.trim()) { toast.error("请输入地址"); return; }
      setSaving(true);
      const res = await customerApi.addAddress(customer.id, form);
      setSaving(false);
      if (res.success && res.data) {
        setAddresses((prev) => [...prev, res.data!]);
        setShowAddAddress(false);
        setAddressForm({ addressType: 1, province: "", city: "", address: "", contactName: "", contactPhone: "", isDefault: false });
        toast.success("地址添加成功");
      } else {
        toast.error(res.message || "添加失败");
      }
    };

    return (
      <div className="mt-4 p-4 rounded-lg" style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.15)" }}>
        <p className="text-xs font-medium mb-3" style={{ color: "#00D4FF", fontFamily: "Noto Sans SC" }}>新增地址</p>
        <div className="grid grid-cols-2 gap-3">
          <FormSelect label="地址类型" value={String(form.addressType)} onChange={(v) => setForm((f) => ({ ...f, addressType: Number(v) }))} options={[{value:"1",label:"发货地址"},{value:"2",label:"收货地址"},{value:"3",label:"账单地址"},{value:"4",label:"注册地址"}]} />
          <FormSelect
            label="省份"
            value={form.province || ""}
            onChange={(v) => setForm((f) => ({ ...f, province: v, city: "" }))}
            options={[
              { value: "", label: "请选择省份" },
              ...PROVINCES.map((p) => ({ value: p, label: p }))
            ]}
          />
          <FormSelect
            label="城市"
            value={form.city || ""}
            onChange={set("city")}
            options={[
              { value: "", label: form.province ? "请选择城市" : "请先选择省份" },
              ...availableCities.map((c) => ({ value: c, label: c }))
            ]}
          />
          <FormInput label="联系人" value={form.contactName || ""} onChange={set("contactName")} />
          <div className="col-span-2"><FormInput label="详细地址" value={form.address || ""} onChange={set("address")} required /></div>
          <FormInput label="联系电话" value={form.contactPhone || ""} onChange={set("contactPhone")} />
          <div className="flex items-center gap-2 pt-5">
            <input type="checkbox" checked={form.isDefault || false} onChange={(e) => setForm((f) => ({ ...f, isDefault: e.target.checked }))} />
            <span className="text-xs" style={{ color: "rgba(224,244,255,0.6)", fontFamily: "Noto Sans SC" }}>设为默认地址</span>
          </div>
        </div>
        <div className="flex gap-2 mt-3">
          <button onClick={() => setShowAddAddress(false)} className="flex-1 py-2 rounded text-xs" style={{ background: "#0A1628", border: "1px solid rgba(255,255,255,0.1)", color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>取消</button>
          <button onClick={save} disabled={saving} className="flex-1 py-2 rounded text-xs font-medium flex items-center justify-center gap-1" style={{ background: "rgba(0,212,255,0.15)", border: "1px solid rgba(0,212,255,0.4)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}>
            {saving ? <Loader2 size={12} className="animate-spin" /> : <Check size={12} />}保存
          </button>
        </div>
      </div>
    );
  };

  // ── Add Bank ─────────────────────────────────────────────────────────────────
  const AddBankForm = () => {
    const [form, setForm] = useState<AddBankRequest>({ bankName: "", accountNumber: "", accountName: "", bankCode: "", isDefault: false });
    const [saving, setSaving] = useState(false);
    const set = (k: keyof AddBankRequest) => (v: string) => setForm((f) => ({ ...f, [k]: v as any }));

    const save = async () => {
      if (!form.accountNumber?.trim()) { toast.error("请输入银行账号"); return; }
      setSaving(true);
      const res = await customerApi.addBank(customer.id, form);
      setSaving(false);
      if (res.success && res.data) {
        setBanks((prev) => [...prev, res.data!]);
        setShowAddBank(false);
        toast.success("银行账户添加成功");
      } else {
        toast.error(res.message || "添加失败");
      }
    };

    return (
      <div className="mt-4 p-4 rounded-lg" style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.15)" }}>
        <p className="text-xs font-medium mb-3" style={{ color: "#00D4FF", fontFamily: "Noto Sans SC" }}>新增银行账户</p>
        <div className="grid grid-cols-2 gap-3">
          <FormInput label="银行名称" value={form.bankName || ""} onChange={set("bankName")} />
          <FormInput label="账户名称" value={form.accountName || ""} onChange={set("accountName")} />
          <FormInput label="银行账号" value={form.accountNumber || ""} onChange={set("accountNumber")} required />
          <FormInput label="支行/SWIFT" value={form.bankCode || ""} onChange={set("bankCode")} />
          <div className="flex items-center gap-2 pt-5">
            <input type="checkbox" checked={form.isDefault || false} onChange={(e) => setForm((f) => ({ ...f, isDefault: e.target.checked }))} />
            <span className="text-xs" style={{ color: "rgba(224,244,255,0.6)", fontFamily: "Noto Sans SC" }}>设为默认账户</span>
          </div>
        </div>
        <div className="flex gap-2 mt-3">
          <button onClick={() => setShowAddBank(false)} className="flex-1 py-2 rounded text-xs" style={{ background: "#0A1628", border: "1px solid rgba(255,255,255,0.1)", color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>取消</button>
          <button onClick={save} disabled={saving} className="flex-1 py-2 rounded text-xs font-medium flex items-center justify-center gap-1" style={{ background: "rgba(0,212,255,0.15)", border: "1px solid rgba(0,212,255,0.4)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}>
            {saving ? <Loader2 size={12} className="animate-spin" /> : <Check size={12} />}保存
          </button>
        </div>
      </div>
    );
  };

  // ── Add History ──────────────────────────────────────────────────────────────
  const AddHistoryForm = () => {
    const [form, setForm] = useState<AddContactHistoryRequest>({ contactType: "call", subject: "", content: "", contactPerson: "", result: "" });
    const [saving, setSaving] = useState(false);
    const set = (k: keyof AddContactHistoryRequest) => (v: string) => setForm((f) => ({ ...f, [k]: v }));

    const save = async () => {
      if (!form.content?.trim()) { toast.error("请输入联系内容"); return; }
      setSaving(true);
      const res = await customerApi.addContactHistory(customer.id, { ...form, contactTime: new Date().toISOString() });
      setSaving(false);
      if (res.success && res.data) {
        setHistory((prev) => [res.data as CustomerContactHistory, ...prev]);
        setShowAddHistory(false);
        toast.success("联系记录添加成功");
      } else {
        toast.error(res.message || "添加失败");
      }
    };

    return (
      <div className="mt-4 p-4 rounded-lg" style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.15)" }}>
        <p className="text-xs font-medium mb-3" style={{ color: "#00D4FF", fontFamily: "Noto Sans SC" }}>新增联系记录</p>
        <div className="grid grid-cols-2 gap-3">
          <FormSelect label="联系类型" value={form.contactType || "call"} onChange={set("contactType")} options={[{value:"call",label:"电话"},{value:"visit",label:"拜访"},{value:"email",label:"邮件"},{value:"meeting",label:"会议"},{value:"other",label:"其他"}]} />
          <FormInput label="联系人（客户方）" value={form.contactPerson || ""} onChange={set("contactPerson")} />
          <div className="col-span-2"><FormInput label="主题" value={form.subject || ""} onChange={set("subject")} /></div>
          <div className="col-span-2">
            <label className="block text-xs mb-1.5" style={{ color: "rgba(0,212,255,0.7)", fontFamily: "Noto Sans SC" }}>联系内容 <span style={{ color: "#C97A6E" }}>*</span></label>
            <textarea
              value={form.content || ""}
              onChange={(e) => setForm((f) => ({ ...f, content: e.target.value }))}
              rows={3}
              className="w-full px-3 py-2.5 rounded text-xs outline-none resize-none"
              style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.2)", color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
            />
          </div>
          <div className="col-span-2"><FormInput label="跟进结果" value={form.result || ""} onChange={set("result")} /></div>
        </div>
        <div className="flex gap-2 mt-3">
          <button onClick={() => setShowAddHistory(false)} className="flex-1 py-2 rounded text-xs" style={{ background: "#0A1628", border: "1px solid rgba(255,255,255,0.1)", color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>取消</button>
          <button onClick={save} disabled={saving} className="flex-1 py-2 rounded text-xs font-medium flex items-center justify-center gap-1" style={{ background: "rgba(0,212,255,0.15)", border: "1px solid rgba(0,212,255,0.4)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}>
            {saving ? <Loader2 size={12} className="animate-spin" /> : <Check size={12} />}保存
          </button>
        </div>
      </div>
    );
  };

  const tabs = [
    { key: "info",     label: "基本信息", icon: <Building2 size={12} /> },
    { key: "contacts", label: `联系人 (${contacts.length})`, icon: <UserCheck size={12} /> },
    { key: "addresses",label: `地址 (${addresses.length})`, icon: <MapPin size={12} /> },
    { key: "banks",    label: `银行账户 (${banks.length})`, icon: <CreditCard size={12} /> },
    { key: "history",  label: "联系历史", icon: <Clock size={12} /> },
  ] as const;

  return (
    <ModalWrapper title={`客户详情 — ${customer.customerCode}`} onClose={onClose} wide>
      {/* Header */}
      <div className="flex items-start gap-4 mb-5">
        <div
          className="w-14 h-14 rounded-xl flex items-center justify-center text-xl font-bold flex-shrink-0"
          style={{ background: levelInfo.bg, border: `1px solid ${levelInfo.color}40`, color: levelInfo.color, fontFamily: "Noto Sans SC" }}
        >
          {(customer.officialName || customer.customerName || "?").charAt(0)}
        </div>
        <div className="flex-1 min-w-0">
          <h4 className="text-sm font-semibold truncate" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
            {customer.officialName || customer.customerName}
          </h4>
          <div className="flex items-center gap-2 mt-1">
            <span className="text-xs px-2 py-0.5 rounded" style={{ background: levelInfo.bg, color: levelInfo.color, border: `1px solid ${levelInfo.color}30`, fontFamily: "Noto Sans SC" }}>
              {levelInfo.label}
            </span>
            <span className="text-xs" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>
              {customer.customerCode}
            </span>
            <span
              className="text-xs px-2 py-0.5 rounded"
              style={{
                background: customer.isActive ? "rgba(70,191,145,0.1)" : "rgba(201,87,69,0.1)",
                color: customer.isActive ? "#46BF91" : "#C95745",
                border: `1px solid ${customer.isActive ? "#46BF91" : "#C95745"}30`,
                fontFamily: "Noto Sans SC",
              }}
            >
              {customer.isActive ? "正常" : "停用"}
            </span>
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 mb-4 overflow-x-auto" style={{ borderBottom: "1px solid rgba(0,212,255,0.1)" }}>
        {tabs.map((tab) => (
          <button
            key={tab.key}
            onClick={() => setActiveTab(tab.key)}
            className="flex items-center gap-1.5 px-3 py-2 text-xs whitespace-nowrap transition-all"
            style={{
              color: activeTab === tab.key ? "#00D4FF" : "rgba(200,216,232,0.55)",
              borderBottom: activeTab === tab.key ? "2px solid #00D4FF" : "2px solid transparent",
              fontFamily: "Noto Sans SC",
            }}
          >
            {tab.icon}{tab.label}
          </button>
        ))}
      </div>

      {/* Tab Content */}
      {activeTab === "info" && (
        <div className="grid grid-cols-2 gap-3">
          <FieldBox label="客户编号" value={customer.customerCode} />
          <FieldBox label="客户等级" value={levelInfo.label} />
          <FieldBox label="公司全称" value={customer.officialName || customer.customerName} />
          <FieldBox label="公司简称" value={customer.nickName || customer.customerShortName} />
          <FieldBox label="行业" value={customer.industry} />
          <FieldBox label="主营产品" value={customer.product} />
          <FieldBox label="授信额度" value={customer.creditLimit != null ? `¥${customer.creditLimit.toLocaleString()}` : undefined} />
          <FieldBox label="账期" value={customer.paymentTerms != null ? `${customer.paymentTerms} 天` : undefined} />
          <FieldBox label="结算货币" value={customer.currency === 1 ? "人民币 CNY" : customer.currency === 2 ? "美元 USD" : undefined} />
          <FieldBox label="税率" value={customer.taxRate != null ? `${customer.taxRate}%` : undefined} />
          <FieldBox label="统一社会信用代码" value={customer.creditCode || customer.unifiedSocialCreditCode} />
          <FieldBox label="备注" value={customer.remark || customer.remarks} />
        </div>
      )}

      {activeTab === "contacts" && (
        <div>
          <div className="flex justify-end mb-3">
            <button
              onClick={() => setShowAddContact(!showAddContact)}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded text-xs"
              style={{ background: "rgba(0,212,255,0.1)", border: "1px solid rgba(0,212,255,0.3)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}
            >
              <Plus size={12} />新增联系人
            </button>
          </div>
          {showAddContact && <AddContactForm />}
          {contacts.length === 0 && !showAddContact ? (
            <p className="text-center py-8 text-xs" style={{ color: "rgba(200,216,232,0.4)", fontFamily: "Noto Sans SC" }}>暂无联系人</p>
          ) : (
            <div className="space-y-2 mt-3">
              {contacts.map((c) => (
                <div key={c.id} className="p-3 rounded-lg flex items-start justify-between" style={{ background: "#162233", border: "1px solid rgba(255,255,255,0.08)" }}>
                  <div className="flex items-start gap-3">
                    <div className="w-8 h-8 rounded-lg flex items-center justify-center text-xs font-bold flex-shrink-0" style={{ background: "rgba(0,212,255,0.1)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}>
                      {(c.name || c.contactName || "?").charAt(0)}
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="text-xs font-medium" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>{c.name || c.contactName}</span>
                        {(c.isMain || c.isDefault) && (
                          <span className="text-xs px-1.5 py-0.5 rounded" style={{ background: "rgba(70,191,145,0.1)", color: "#46BF91", border: "1px solid #46BF9130", fontFamily: "Noto Sans SC" }}>默认</span>
                        )}
                      </div>
                      <div className="flex items-center gap-3 mt-0.5">
                        {(c.title || c.position) && <span className="text-xs" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>{c.title || c.position}</span>}
                        {c.department && <span className="text-xs" style={{ color: "rgba(200,216,232,0.45)", fontFamily: "Noto Sans SC" }}>{c.department}</span>}
                      </div>
                      <div className="flex items-center gap-3 mt-0.5">
                        {(c.mobile || c.mobilePhone) && <span className="text-xs flex items-center gap-1" style={{ color: "rgba(200,216,232,0.55)" }}><Phone size={10} />{c.mobile || c.mobilePhone}</span>}
                        {c.email && <span className="text-xs flex items-center gap-1" style={{ color: "rgba(200,216,232,0.55)" }}><Mail size={10} />{c.email}</span>}
                      </div>
                    </div>
                  </div>
                  <button
                    onClick={async () => {
                      if (!confirm("确认删除该联系人？")) return;
                      const res = await customerApi.deleteContact(customer.id, c.id);
                      if (res.success) {
                        setContacts((prev) => prev.filter((x) => x.id !== c.id));
                        toast.success("联系人已删除");
                      } else {
                        toast.error(res.message || "删除失败");
                      }
                    }}
                    className="p-1.5 rounded hover:bg-white/5 transition-colors flex-shrink-0"
                  >
                    <Trash2 size={12} style={{ color: "#C95745" }} />
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {activeTab === "addresses" && (
        <div>
          <div className="flex justify-end mb-3">
            <button
              onClick={() => setShowAddAddress(!showAddAddress)}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded text-xs"
              style={{ background: "rgba(0,212,255,0.1)", border: "1px solid rgba(0,212,255,0.3)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}
            >
              <Plus size={12} />新增地址
            </button>
          </div>
          {showAddAddress && <AddAddressForm />}
          {addresses.length === 0 && !showAddAddress ? (
            <p className="text-center py-8 text-xs" style={{ color: "rgba(200,216,232,0.4)", fontFamily: "Noto Sans SC" }}>暂无地址</p>
          ) : (
            <div className="space-y-2 mt-3">
              {addresses.map((a) => (
                <div key={a.id} className="p-3 rounded-lg flex items-start justify-between" style={{ background: "#162233", border: "1px solid rgba(255,255,255,0.08)" }}>
                  <div className="flex items-start gap-3">
                    <MapPin size={16} style={{ color: "#50BBE3", marginTop: 2, flexShrink: 0 }} />
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="text-xs px-1.5 py-0.5 rounded" style={{ background: "rgba(80,187,227,0.1)", color: "#50BBE3", border: "1px solid rgba(80,187,227,0.3)", fontFamily: "Noto Sans SC" }}>
                          {ADDRESS_TYPE_MAP[a.addressType] || "地址"}
                        </span>
                        {a.isDefault && <span className="text-xs px-1.5 py-0.5 rounded" style={{ background: "rgba(70,191,145,0.1)", color: "#46BF91", border: "1px solid #46BF9130", fontFamily: "Noto Sans SC" }}>默认</span>}
                      </div>
                      <p className="text-xs mt-1" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
                        {[a.province, a.city, a.area, a.address].filter(Boolean).join(" ")}
                      </p>
                      {a.contactName && <p className="text-xs mt-0.5" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>{a.contactName} {a.contactPhone}</p>}
                    </div>
                  </div>
                  <button
                    onClick={async () => {
                      if (!confirm("确认删除该地址？")) return;
                      const res = await customerApi.deleteAddress(customer.id, a.id);
                      if (res.success) {
                        setAddresses((prev) => prev.filter((x) => x.id !== a.id));
                        toast.success("地址已删除");
                      } else {
                        toast.error(res.message || "删除失败");
                      }
                    }}
                    className="p-1.5 rounded hover:bg-white/5 transition-colors flex-shrink-0"
                  >
                    <Trash2 size={12} style={{ color: "#C95745" }} />
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {activeTab === "banks" && (
        <div>
          <div className="flex justify-end mb-3">
            <button
              onClick={() => setShowAddBank(!showAddBank)}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded text-xs"
              style={{ background: "rgba(0,212,255,0.1)", border: "1px solid rgba(0,212,255,0.3)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}
            >
              <Plus size={12} />新增银行账户
            </button>
          </div>
          {showAddBank && <AddBankForm />}
          {banks.length === 0 && !showAddBank ? (
            <p className="text-center py-8 text-xs" style={{ color: "rgba(200,216,232,0.4)", fontFamily: "Noto Sans SC" }}>暂无银行账户</p>
          ) : (
            <div className="space-y-2 mt-3">
              {banks.map((b) => (
                <div key={b.id} className="p-3 rounded-lg flex items-start justify-between" style={{ background: "#162233", border: "1px solid rgba(255,255,255,0.08)" }}>
                  <div className="flex items-start gap-3">
                    <CreditCard size={16} style={{ color: "#C9A96E", marginTop: 2, flexShrink: 0 }} />
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="text-xs font-medium" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>{b.bankName || "银行"}</span>
                        {b.isDefault && <span className="text-xs px-1.5 py-0.5 rounded" style={{ background: "rgba(70,191,145,0.1)", color: "#46BF91", border: "1px solid #46BF9130", fontFamily: "Noto Sans SC" }}>默认</span>}
                      </div>
                      <p className="text-xs mt-0.5 font-mono" style={{ color: "rgba(200,216,232,0.7)" }}>{b.bankAccount || b.accountNumber}</p>
                      {b.accountName && <p className="text-xs mt-0.5" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>户名：{b.accountName}</p>}
                      {(b.bankBranch || b.bankCode) && <p className="text-xs mt-0.5" style={{ color: "rgba(200,216,232,0.45)", fontFamily: "Noto Sans SC" }}>支行：{b.bankBranch || b.bankCode}</p>}
                    </div>
                  </div>
                  <button
                    onClick={async () => {
                      if (!confirm("确认删除该银行账户？")) return;
                      const res = await customerApi.deleteBank(customer.id, b.id);
                      if (res.success) {
                        setBanks((prev) => prev.filter((x) => x.id !== b.id));
                        toast.success("银行账户已删除");
                      } else {
                        toast.error(res.message || "删除失败");
                      }
                    }}
                    className="p-1.5 rounded hover:bg-white/5 transition-colors flex-shrink-0"
                  >
                    <Trash2 size={12} style={{ color: "#C95745" }} />
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {activeTab === "history" && (
        <div>
          <div className="flex justify-end mb-3">
            <button
              onClick={() => setShowAddHistory(!showAddHistory)}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded text-xs"
              style={{ background: "rgba(0,212,255,0.1)", border: "1px solid rgba(0,212,255,0.3)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}
            >
              <Plus size={12} />新增联系记录
            </button>
          </div>
          {showAddHistory && <AddHistoryForm />}
          {history.length === 0 && !showAddHistory ? (
            <p className="text-center py-8 text-xs" style={{ color: "rgba(200,216,232,0.4)", fontFamily: "Noto Sans SC" }}>暂无联系历史</p>
          ) : (
            <div className="space-y-2 mt-3">
              {history.map((h) => (
                <div key={h.id} className="p-3 rounded-lg" style={{ background: "#162233", border: "1px solid rgba(255,255,255,0.08)" }}>
                  <div className="flex items-center justify-between mb-1.5">
                    <div className="flex items-center gap-2">
                      <span className="text-xs px-1.5 py-0.5 rounded" style={{ background: "rgba(80,187,227,0.1)", color: "#50BBE3", border: "1px solid rgba(80,187,227,0.3)", fontFamily: "Noto Sans SC" }}>
                        {CONTACT_TYPE_MAP[h.type] || h.type}
                      </span>
                      {h.subject && <span className="text-xs font-medium" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>{h.subject}</span>}
                    </div>
                    <span className="text-xs" style={{ color: "rgba(200,216,232,0.45)", fontFamily: "Noto Sans SC" }}>
                      {new Date(h.time).toLocaleDateString("zh-CN")}
                    </span>
                  </div>
                  {h.content && <p className="text-xs" style={{ color: "rgba(224,244,255,0.7)", fontFamily: "Noto Sans SC" }}>{h.content}</p>}
                  {(h.contactPerson || h.result) && (
                    <div className="flex items-center gap-4 mt-1.5">
                      {h.contactPerson && <span className="text-xs" style={{ color: "rgba(200,216,232,0.5)", fontFamily: "Noto Sans SC" }}>联系人：{h.contactPerson}</span>}
                      {h.result && <span className="text-xs" style={{ color: "rgba(200,216,232,0.5)", fontFamily: "Noto Sans SC" }}>结果：{h.result}</span>}
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </ModalWrapper>
  );
}

// ─── Create Customer Modal ────────────────────────────────────────────────────

function CreateCustomerModal({ onClose, onCreated }: { onClose: () => void; onCreated: () => void }) {
  const [form, setForm] = useState<CreateCustomerRequest>({
    customerName: "",
    customerShortName: "",
    customerLevel: "B",
    customerType: 1,
    industry: "",
    remarks: "",
    creditLimit: 0,
    paymentTerms: 30,
    currency: 1,
  });
  const [saving, setSaving] = useState(false);
  const set = (k: keyof CreateCustomerRequest) => (v: string) => setForm((f) => ({ ...f, [k]: v as any }));

  const save = async () => {
    if (!form.customerName?.trim()) { toast.error("请输入客户名称"); return; }
    setSaving(true);
    const res = await customerApi.create(form);
    setSaving(false);
    if (res.success) {
      toast.success(`客户 ${res.data?.customerCode} 创建成功`);
      onCreated();
      onClose();
    } else {
      toast.error(res.message || "创建失败");
    }
  };

  return (
    <ModalWrapper title="新增客户" onClose={onClose}>
      <div className="grid grid-cols-2 gap-4">
        <div className="col-span-2">
          <FormInput label="公司全称" value={form.customerName || ""} onChange={set("customerName")} required placeholder="请输入公司全称" />
        </div>
        <FormInput label="公司简称" value={form.customerShortName || ""} onChange={set("customerShortName")} placeholder="可选" />
        <FormSelect label="客户等级" value={form.customerLevel || "B"} onChange={set("customerLevel")} options={[
          {value:"D",label:"D级"},{value:"C",label:"C级"},{value:"B",label:"B级"},
          {value:"BPO",label:"BPO"},{value:"VIP",label:"VIP"},{value:"VPO",label:"VPO"}
        ]} />
        <FormSelect label="客户类型" value={String(form.customerType || 1)} onChange={(v) => setForm((f) => ({ ...f, customerType: Number(v) }))} options={[
          {value:"1",label:"终端客户"},{value:"2",label:"贸易商"},{value:"3",label:"代理商"},{value:"4",label:"分销商"}
        ]} />
        <FormInput label="行业" value={form.industry || ""} onChange={set("industry")} placeholder="如：电子制造" />
        <FormInput label="授信额度" value={String(form.creditLimit || 0)} onChange={(v) => setForm((f) => ({ ...f, creditLimit: Number(v) || 0 }))} type="number" />
        <FormInput label="账期（天）" value={String(form.paymentTerms || 30)} onChange={(v) => setForm((f) => ({ ...f, paymentTerms: Number(v) || 30 }))} type="number" />
        <div className="col-span-2">
          <label className="block text-xs mb-1.5" style={{ color: "rgba(0,212,255,0.7)", fontFamily: "Noto Sans SC" }}>备注</label>
          <textarea
            value={form.remarks || ""}
            onChange={(e) => setForm((f) => ({ ...f, remarks: e.target.value }))}
            rows={3}
            className="w-full px-3 py-2.5 rounded text-xs outline-none resize-none"
            style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.2)", color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
          />
        </div>
      </div>
      <div className="flex gap-3 mt-5">
        <button onClick={onClose} className="flex-1 py-2.5 rounded text-xs" style={{ background: "#162233", border: "1px solid rgba(255,255,255,0.1)", color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>取消</button>
        <button
          onClick={save}
          disabled={saving}
          className="flex-1 py-2.5 rounded text-xs font-medium flex items-center justify-center gap-1.5"
          style={{ background: "linear-gradient(135deg, rgba(0,212,255,0.2), rgba(0,102,255,0.2))", border: "1px solid rgba(0,212,255,0.4)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}
        >
          {saving ? <Loader2 size={13} className="animate-spin" /> : <Check size={13} />}
          确认创建
        </button>
      </div>
    </ModalWrapper>
  );
}

// ─── Main Page ────────────────────────────────────────────────────────────────

export default function CustomerPage() {
  const [, navigate] = useLocation();
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [loading, setLoading] = useState(true);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const [search, setSearch] = useState("");
  const [searchInput, setSearchInput] = useState("");
  const [levelFilter, setLevelFilter] = useState("全部");

  const [stats, setStats] = useState({ totalCustomers: 0, activeCustomers: 0, newThisMonth: 0, byLevel: {} as Record<string, number> });
  const [viewCustomer, setViewCustomer] = useState<Customer | null>(null);
  const [showCreate, setShowCreate] = useState(false);

  const LEVEL_NUM_MAP: Record<string, number> = { "D": 1, "C": 2, "B": 3, "BPO": 4, "VIP": 5, "VPO": 6 };

  const loadCustomers = useCallback(async () => {
    setLoading(true);
    try {
      const params: Parameters<typeof customerApi.list>[0] = {
        pageNumber: page,
        pageSize,
        searchTerm: search || undefined,
        customerLevel: levelFilter !== "全部" ? LEVEL_NUM_MAP[levelFilter] as any : undefined,
      };
      const res = await customerApi.list(params);
      if (res.success && res.data) {
        setCustomers(res.data.items);
        setTotalCount(res.data.totalCount);
        setTotalPages(res.data.totalPages);
      } else {
        toast.error(res.message || "加载客户列表失败");
      }
    } catch (err) {
      toast.error("网络错误，无法连接到服务器");
    } finally {
      setLoading(false);
    }
  }, [page, search, levelFilter]);

  const loadStats = useCallback(async () => {
    const res = await customerApi.getStats();
    if (res.success && res.data) {
      setStats({
        totalCustomers: res.data.totalCustomers,
        activeCustomers: res.data.activeCustomers,
        newThisMonth: res.data.newThisMonth,
        byLevel: res.data.byLevel,
      });
    }
  }, []);

  useEffect(() => { loadCustomers(); }, [loadCustomers]);
  useEffect(() => { loadStats(); }, [loadStats]);

  const handleSearch = () => { setSearch(searchInput); setPage(1); };
  const handleKeyDown = (e: React.KeyboardEvent) => { if (e.key === "Enter") handleSearch(); };

  const handleDelete = async (id: string, code: string) => {
    if (!confirm(`确认删除客户 ${code}？此操作不可恢复。`)) return;
    const res = await customerApi.delete(id);
    if (res.success) {
      toast.success(`客户 ${code} 已删除`);
      loadCustomers();
      loadStats();
    } else {
      toast.error(res.message || "删除失败");
    }
  };

  const levels = ["全部", "VIP", "VPO", "BPO", "B", "C", "D"];
  const vipCount = stats.byLevel["VIP"] || 0;

  return (
    <DashboardLayout title="客户管理">
      {/* KPI Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        {[
          { label: "客户总数", value: String(stats.totalCustomers), unit: "家", color: "#00D4FF" },
          { label: "活跃客户", value: String(stats.activeCustomers), unit: "家", color: "#46BF91" },
          { label: "VIP客户", value: String(vipCount), unit: "家", color: "#C9A96E" },
          { label: "本月新增", value: String(stats.newThisMonth), unit: "家", color: "#3295C9" },
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

      {/* Customer List */}
      <div className="glass-card rounded-xl p-5">
        {/* Toolbar */}
        <div className="flex flex-wrap items-center gap-3 mb-5">
          <h3 className="text-sm font-semibold" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>客户列表</h3>

          <div
            className="flex items-center gap-2 px-3 py-2 rounded flex-1 min-w-[180px] max-w-xs"
            style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.15)" }}
          >
            <Search size={13} style={{ color: "rgba(200,216,232,0.55)" }} />
            <input
              type="text"
              placeholder="搜索客户名称/编号..."
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              onKeyDown={handleKeyDown}
              className="flex-1 bg-transparent text-xs outline-none"
              style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
            />
            {searchInput && (
              <button onClick={() => { setSearchInput(""); setSearch(""); setPage(1); }}>
                <X size={12} style={{ color: "rgba(200,216,232,0.4)" }} />
              </button>
            )}
          </div>

          <div className="flex items-center gap-1.5 flex-wrap">
            {levels.map((l) => (
              <button
                key={l}
                onClick={() => { setLevelFilter(l); setPage(1); }}
                className="px-3 py-1.5 rounded text-xs transition-all"
                style={{
                  background: levelFilter === l ? "rgba(0,212,255,0.15)" : "rgba(0,212,255,0.04)",
                  border: `1px solid ${levelFilter === l ? "rgba(0,212,255,0.4)" : "rgba(0,212,255,0.12)"}`,
                  color: levelFilter === l ? "#00D4FF" : "rgba(224,244,255,0.5)",
                  fontFamily: "Noto Sans SC",
                }}
              >
                {l}
              </button>
            ))}
          </div>

          <div className="flex items-center gap-2 ml-auto">
            <button
              onClick={() => navigate("/customer-recycle-bin")}
              className="flex items-center gap-1.5 px-3 py-2 rounded text-xs"
              style={{ background: "rgba(201,87,69,0.08)", border: "1px solid rgba(201,87,69,0.25)", color: "#C95745", fontFamily: "Noto Sans SC" }}
              title="客户回收站"
            >
              <Trash2 size={12} />回收站
            </button>
            <button
              onClick={() => navigate("/customer-blacklist")}
              className="flex items-center gap-1.5 px-3 py-2 rounded text-xs"
              style={{ background: "rgba(201,87,69,0.08)", border: "1px solid rgba(201,87,69,0.25)", color: "#C95745", fontFamily: "Noto Sans SC" }}
              title="客户黑名单"
            >
              <Ban size={12} />黑名单
            </button>
            <button
              onClick={() => { loadCustomers(); loadStats(); }}
              className="p-2 rounded"
              style={{ background: "#162233", border: "1px solid rgba(255,255,255,0.08)", color: "rgba(200,216,232,0.5)" }}
              title="刷新"
            >
              <RefreshCw size={13} />
            </button>
            <button
              className="flex items-center gap-1.5 px-4 py-2 rounded text-xs font-medium"
              style={{
                background: "linear-gradient(135deg, rgba(0,212,255,0.2), rgba(0,102,255,0.2))",
                border: "1px solid rgba(0,212,255,0.4)",
                color: "#00D4FF",
                fontFamily: "Noto Sans SC",
              }}
              onClick={() => setShowCreate(true)}
            >
              <Plus size={13} />新增客户
            </button>
          </div>
        </div>

        {/* Table */}
        {loading ? (
          <div className="flex items-center justify-center py-16">
            <Loader2 size={24} className="animate-spin" style={{ color: "#00D4FF" }} />
            <span className="ml-3 text-sm" style={{ color: "rgba(200,216,232,0.6)", fontFamily: "Noto Sans SC" }}>加载中...</span>
          </div>
        ) : customers.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16">
            <Users size={40} style={{ color: "rgba(200,216,232,0.2)" }} />
            <p className="mt-3 text-sm" style={{ color: "rgba(200,216,232,0.4)", fontFamily: "Noto Sans SC" }}>
              {search ? "未找到匹配的客户" : "暂无客户数据"}
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead>
                <tr style={{ borderBottom: "1px solid rgba(0,212,255,0.12)" }}>
                  {["客户信息", "行业", "客户等级", "授信额度", "账期", "状态", "操作"].map((h) => (
                    <th
                      key={h}
                      className="text-left pb-3 pr-4 font-medium"
                      style={{ color: "rgba(200,216,232,0.6)", fontFamily: "Noto Sans SC" }}
                    >
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {customers.map((c) => {
                  const lc = LEVEL_MAP[c.customerLevel || "D"] || LEVEL_MAP["D"];
                  return (
                    <tr
                      key={c.id}
                      className="table-row-hover"
                      style={{ borderBottom: "1px solid rgba(0,212,255,0.05)" }}
                    >
                      <td className="py-3 pr-4">
                        <div className="flex items-center gap-3">
                          <div
                            className="w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0 text-xs font-bold"
                            style={{ background: lc.bg, border: `1px solid ${lc.color}30`, color: lc.color, fontFamily: "Noto Sans SC" }}
                          >
                            {(c.officialName || c.customerName || "?").charAt(0)}
                          </div>
                          <div>
                            <p className="font-medium" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
                              {c.officialName || c.customerName}
                            </p>
                            <p style={{ color: "rgba(200,216,232,0.45)", fontFamily: "Space Mono", fontSize: "10px" }}>{c.customerCode}</p>
                          </div>
                        </div>
                      </td>
                      <td className="py-3 pr-4" style={{ color: "rgba(200,216,232,0.6)", fontFamily: "Noto Sans SC" }}>
                        {c.industry || "—"}
                      </td>
                      <td className="py-3 pr-4">
                        <span
                          className="px-2 py-0.5 rounded text-xs font-medium"
                          style={{ background: lc.bg, color: lc.color, border: `1px solid ${lc.color}30`, fontFamily: "Noto Sans SC" }}
                        >
                          {lc.label}
                        </span>
                      </td>
                      <td className="py-3 pr-4 font-mono" style={{ color: "#50BBE3" }}>
                        {c.creditLimit != null ? `¥${(c.creditLimit / 10000).toFixed(1)}万` : "—"}
                      </td>
                      <td className="py-3 pr-4" style={{ color: "rgba(200,216,232,0.6)", fontFamily: "Noto Sans SC" }}>
                        {c.paymentTerms != null ? `${c.paymentTerms}天` : "—"}
                      </td>
                      <td className="py-3 pr-4">
                        <span
                          className="px-2 py-0.5 rounded text-xs"
                          style={{
                            background: c.isActive ? "rgba(70,191,145,0.1)" : "rgba(201,87,69,0.1)",
                            color: c.isActive ? "#46BF91" : "#C95745",
                            border: `1px solid ${c.isActive ? "#46BF91" : "#C95745"}30`,
                            fontFamily: "Noto Sans SC",
                          }}
                        >
                          {c.isActive ? "正常" : "停用"}
                        </span>
                      </td>
                      <td className="py-3">
                        <div className="flex items-center gap-1.5">
                          <button
                            className="flex items-center gap-1 px-2 py-1 rounded text-xs"
                            style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.15)", color: "#00D4FF" }}
                            onClick={() => navigate("/customer/" + c.id)}
                            title="查看详情"
                          >
                            <Eye size={11} />详情
                          </button>
                          <button
                            className="p-1.5 rounded"
                            style={{ background: "#162233", border: "1px solid rgba(201,87,69,0.2)", color: "#C95745" }}
                            onClick={() => handleDelete(c.id, c.customerCode)}
                            title="删除"
                          >
                            <Trash2 size={11} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}

        {/* Pagination */}
        {!loading && totalPages > 1 && (
          <div className="flex items-center justify-between mt-4 pt-4" style={{ borderTop: "1px solid rgba(0,212,255,0.08)" }}>
            <span className="text-xs" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>
              共 {totalCount} 位客户，第 {page}/{totalPages} 页
            </span>
            <div className="flex items-center gap-1">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="w-7 h-7 rounded flex items-center justify-center"
                style={{
                  background: "rgba(0,212,255,0.04)",
                  border: "1px solid rgba(0,212,255,0.12)",
                  color: page <= 1 ? "rgba(224,244,255,0.2)" : "rgba(224,244,255,0.5)",
                }}
              >
                <ChevronLeft size={13} />
              </button>
              {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                const p = page <= 3 ? i + 1 : page + i - 2;
                if (p < 1 || p > totalPages) return null;
                return (
                  <button
                    key={p}
                    onClick={() => setPage(p)}
                    className="w-7 h-7 rounded text-xs"
                    style={{
                      background: p === page ? "rgba(0,212,255,0.15)" : "rgba(0,212,255,0.04)",
                      border: `1px solid ${p === page ? "rgba(0,212,255,0.4)" : "rgba(0,212,255,0.12)"}`,
                      color: p === page ? "#00D4FF" : "rgba(224,244,255,0.4)",
                      fontFamily: "Space Mono",
                    }}
                  >
                    {p}
                  </button>
                );
              })}
              <button
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page >= totalPages}
                className="w-7 h-7 rounded flex items-center justify-center"
                style={{
                  background: "rgba(0,212,255,0.04)",
                  border: "1px solid rgba(0,212,255,0.12)",
                  color: page >= totalPages ? "rgba(224,244,255,0.2)" : "rgba(224,244,255,0.5)",
                }}
              >
                <ChevronRight size={13} />
              </button>
            </div>
          </div>
        )}
        {!loading && totalPages <= 1 && totalCount > 0 && (
          <div className="mt-4 pt-4" style={{ borderTop: "1px solid rgba(0,212,255,0.08)" }}>
            <span className="text-xs" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>共 {totalCount} 位客户</span>
          </div>
        )}
      </div>

      {/* Modals */}
      {viewCustomer && (
        <CustomerDetailModal
          customer={viewCustomer}
          onClose={() => setViewCustomer(null)}
          onUpdated={() => { loadCustomers(); loadStats(); }}
        />
      )}
      {showCreate && (
        <CreateCustomerModal
          onClose={() => setShowCreate(false)}
          onCreated={() => { loadCustomers(); loadStats(); }}
        />
      )}
    </DashboardLayout>
  );
}
