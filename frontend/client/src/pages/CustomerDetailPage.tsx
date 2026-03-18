/**
 * CustomerDetailPage — FrontCRM Deep Quantum Theme
 * 客户详情独立页面，路由 /customer/:id
 * Design: Deep Quantum (#192A3F bg, #0A1628 card, #162233 inner panel)
 * Accent: #00D4FF (cyan), #3295C9 (steel blue), #50BBE3 (ice blue)
 */
import { useState, useEffect, useCallback } from "react";
import { useParams, useLocation } from "wouter";
import DashboardLayout from "@/components/DashboardLayout";
import { toast } from "sonner";
import {
  ArrowLeft, Building2, UserCheck, MapPin, CreditCard, Clock,
  Plus, Trash2, Check, Loader2, Phone, Mail, Edit2, X,
  AlertTriangle, ShieldOff, ShieldCheck, Ban, FileText, Activity,
} from "lucide-react";
import {
  customerApi,
  type Customer,
  type CustomerContact,
  type CustomerAddress,
  type CustomerBankInfo,
  type CustomerContactHistory,
  type AddContactRequest,
  type AddAddressRequest,
  type AddBankRequest,
  type AddContactHistoryRequest,
  type UpdateCustomerRequest,
} from "@/lib/customerApi";

// ─── Log Types ────────────────────────────────────────────────────────────────
type OperationLog = {
  id: string;
  operationType: string;
  operationDesc: string;
  operatorUserName: string;
  operationTime: string;
  remark?: string;
};
type ChangeLog = {
  id: string;
  fieldLabel: string;
  oldValue: string;
  newValue: string;
  changedByUserName: string;
  changedAt: string;
};
import { PROVINCES, getCities } from "@/lib/chinaRegions";

// ─── Constants ────────────────────────────────────────────────────────────────
const LEVEL_MAP: Record<string, { label: string; color: string; bg: string }> = {
  "D":   { label: "D级",  color: "rgba(200,216,232,0.6)", bg: "rgba(200,216,232,0.06)" },
  "C":   { label: "C级",  color: "#50BBE3",               bg: "rgba(80,187,227,0.1)"   },
  "B":   { label: "B级",  color: "#3295C9",               bg: "rgba(50,149,201,0.1)"   },
  "BPO": { label: "BPO",  color: "#6DBFA0",               bg: "rgba(109,191,160,0.1)"  },
  "VIP": { label: "VIP",  color: "#C9A96E",               bg: "rgba(201,169,110,0.1)"  },
  "VPO": { label: "VPO",  color: "#C97A6E",               bg: "rgba(201,122,110,0.1)"  },
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
      <p className="text-xs font-medium" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
        {value || "—"}
      </p>
    </div>
  );
}

function FormInput({
  label, value, onChange, type = "text", required, placeholder, readOnly,
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
  label, value, onChange, options,
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

// ─── Inline Edit Button ───────────────────────────────────────────────────────
function EditBtn({ onClick }: { onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      className="p-1.5 rounded hover:bg-white/5 transition-colors flex-shrink-0"
      title="编辑"
    >
      <Edit2 size={13} style={{ color: "#3295C9" }} />
    </button>
  );
}

// ─── Main Page ────────────────────────────────────────────────────────────────
export default function CustomerDetailPage() {
  const params = useParams<{ id: string }>();
  const [, navigate] = useLocation();
  const customerId = params.id;

  const [customer, setCustomer] = useState<Customer | null>(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<"info" | "contacts" | "addresses" | "banks" | "history" | "logs">("info");

  // Sub-data states
  const [contacts, setContacts] = useState<CustomerContact[]>([]);
  const [addresses, setAddresses] = useState<CustomerAddress[]>([]);
  const [banks, setBanks] = useState<CustomerBankInfo[]>([]);
  const [history, setHistory] = useState<CustomerContactHistory[]>([]);
  const [historyLoaded, setHistoryLoaded] = useState(false);

  // Add forms visibility
  const [showAddContact, setShowAddContact] = useState(false);
  const [showAddAddress, setShowAddAddress] = useState(false);
  const [showAddBank, setShowAddBank] = useState(false);
  const [showAddHistory, setShowAddHistory] = useState(false);

  // Address form state (lifted up to avoid re-mount reset)
  const [addressForm, setAddressForm] = useState<AddAddressRequest>({
    addressType: 1, province: "", city: "", address: "", contactName: "", contactPhone: "", isDefault: false,
  });
  const [addressSaving, setAddressSaving] = useState(false);

  // ── Basic Info Edit State ──────────────────────────────────────────────────
  const [infoEditing, setInfoEditing] = useState(false);
  const [infoForm, setInfoForm] = useState<UpdateCustomerRequest>({});
  const [infoSaving, setInfoSaving] = useState(false);

  // ── Inline Edit States (id of the record being edited) ────────────────────
  const [editingContactId, setEditingContactId] = useState<string | null>(null);
  const [editingContactForm, setEditingContactForm] = useState<AddContactRequest>({});
  const [contactSaving, setContactSaving] = useState(false);

  const [editingAddressId, setEditingAddressId] = useState<string | null>(null);
  const [editingAddressForm, setEditingAddressForm] = useState<AddAddressRequest>({ addressType: 1 });
  const [editAddressSaving, setEditAddressSaving] = useState(false);

  const [editingBankId, setEditingBankId] = useState<string | null>(null);
  const [editingBankForm, setEditingBankForm] = useState<AddBankRequest>({});
  const [bankSaving, setBankSaving] = useState(false);

  // ── History Edit States ────────────────────────────────────────────────────
  const [editingHistoryId, setEditingHistoryId] = useState<string | null>(null);
  const [editingHistoryForm, setEditingHistoryForm] = useState<AddContactHistoryRequest>({});
  const [historySaving, setHistorySaving] = useState(false);

  // ── Customer Delete / Status States ───────────────────────────────────────
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [deleteReason, setDeleteReason] = useState("");
  const [deleting, setDeleting] = useState(false);
  const [statusChanging, setStatusChanging] = useState(false);
  const [showBlacklistModal, setShowBlacklistModal] = useState(false);
  const [blacklistReason, setBlacklistReason] = useState("");
  // ── Logs States ───────────────────────────────────────────────────────────
  const [operationLogs, setOperationLogs] = useState<OperationLog[]>([]);
  const [changeLogs, setChangeLogs] = useState<ChangeLog[]>([]);
  const [logsLoading, setLogsLoading] = useState(false);
  const [logsLoaded, setLogsLoaded] = useState(false);

  // Load customer detail
  useEffect(() => {
    if (!customerId) return;
    setLoading(true);
    customerApi.get(customerId).then((res) => {
      if (res.success && res.data) {
        const c = res.data as Customer;
        setCustomer(c);
        setContacts(c.contacts || []);
        setAddresses(c.addresses || []);
        setBanks(c.bankAccounts || []);
      } else {
        toast.error("客户信息加载失败");
        navigate("/customer");
      }
      setLoading(false);
    });
  }, [customerId, navigate]);

  const loadHistory = useCallback(async () => {
    if (historyLoaded || !customerId) return;
    const res = await customerApi.getContactHistory(customerId);
    if (res.success && res.data) {
      setHistory(res.data as CustomerContactHistory[]);
      setHistoryLoaded(true);
    }
  }, [customerId, historyLoaded]);

  useEffect(() => {
    if (activeTab === "history") loadHistory();
  }, [activeTab, loadHistory]);

  // ── Load Logs Effect (must be before early returns to comply with React Hooks rules) ────────
  useEffect(() => {
    if (activeTab === "logs" && !logsLoaded && customerId) {
      setLogsLoading(true);
      Promise.all([
        customerApi.getOperationLogs(customerId),
        customerApi.getChangeLogs(customerId),
      ]).then(([opRes, chRes]) => {
        setLogsLoading(false);
        if (opRes.success && opRes.data) setOperationLogs(opRes.data as OperationLog[]);
        if (chRes.success && chRes.data) setChangeLogs(chRes.data as ChangeLog[]);
        setLogsLoaded(true);
      });
    }
  }, [activeTab, logsLoaded, customerId]);

  if (loading) {
    return (
      <DashboardLayout>
        <div className="flex items-center justify-center h-64">
          <Loader2 size={24} className="animate-spin" style={{ color: "#00D4FF" }} />
          <span className="ml-3 text-sm" style={{ color: "rgba(224,244,255,0.6)", fontFamily: "Noto Sans SC" }}>加载中...</span>
        </div>
      </DashboardLayout>
    );
  }

  if (!customer) return null;

  const levelInfo = LEVEL_MAP[customer.customerLevel || "D"] || LEVEL_MAP["D"];

  const tabs = [
    { key: "info",      label: "基本信息",              icon: <Building2 size={13} /> },
    { key: "contacts",  label: `联系人 (${contacts.length})`,  icon: <UserCheck size={13} /> },
    { key: "addresses", label: `地址 (${addresses.length})`,   icon: <MapPin size={13} /> },
    { key: "banks",     label: `银行账户 (${banks.length})`,   icon: <CreditCard size={13} /> },
    { key: "history",   label: "联系历史",               icon: <Clock size={13} /> },
    { key: "logs",      label: "操作日志",               icon: <Activity size={13} /> },
  ] as const;

  // ── Basic Info Edit Handlers ───────────────────────────────────────────────
  const startInfoEdit = () => {
    setInfoForm({
      customerName: customer.officialName || customer.customerName || "",
      customerShortName: customer.nickName || customer.customerShortName || "",
      customerLevel: customer.customerLevel || "D",
      industry: customer.industry || "",
      product: customer.product || "",
      creditLimit: customer.creditLimit ?? 0,
      paymentTerms: customer.paymentTerms ?? 30,
      currency: customer.currency ?? 1,
      unifiedSocialCreditCode: customer.creditCode || customer.unifiedSocialCreditCode || "",
      remarks: customer.remark || customer.remarks || "",
    });
    setInfoEditing(true);
  };

  const cancelInfoEdit = () => { setInfoEditing(false); setInfoForm({}); };

  const saveInfo = async () => {
    if (!infoForm.customerName?.trim()) { toast.error("请输入公司全称"); return; }
    setInfoSaving(true);
    const res = await customerApi.update(customer.id, infoForm);
    setInfoSaving(false);
    if (res.success) {
      // Reload customer data
      const reload = await customerApi.get(customer.id);
      if (reload.success && reload.data) setCustomer(reload.data as Customer);
      setInfoEditing(false);
      toast.success("客户信息已更新");
    } else {
      toast.error(res.message || "保存失败");
    }
  };

  // ── Contact Edit Handlers ──────────────────────────────────────────────────
  const startContactEdit = (c: CustomerContact) => {
    setEditingContactId(c.id);
    setEditingContactForm({
      contactName: c.name || c.contactName || "",
      mobile: c.phone || c.mobile || "",
      email: c.email || "",
      position: c.title || c.position || "",
      department: c.department || "",
      isDefault: c.isMain || c.isDefault || false,
    });
    setShowAddContact(false);
  };

  const saveContact = async (contactId: string) => {
    if (!editingContactForm.contactName?.trim()) { toast.error("请输入联系人姓名"); return; }
    setContactSaving(true);
    const res = await customerApi.updateContact(customer.id, contactId, editingContactForm);
    setContactSaving(false);
    if (res.success && res.data) {
      setContacts((prev) => prev.map((c) => c.id === contactId ? (res.data as CustomerContact) : c));
      setEditingContactId(null);
      toast.success("联系人已更新");
    } else {
      toast.error(res.message || "更新失败");
    }
  };

  // ── Address Edit Handlers ──────────────────────────────────────────────────
  const startAddressEdit = (a: CustomerAddress) => {
    setEditingAddressId(a.id);
    setEditingAddressForm({
      addressType: a.addressType || 1,
      province: a.province || "",
      city: a.city || "",
      address: a.address || "",
      contactName: a.contactName || "",
      contactPhone: a.contactPhone || "",
      isDefault: a.isDefault || false,
    });
    setShowAddAddress(false);
  };

  const saveAddress_edit = async (addressId: string) => {
    if (!editingAddressForm.address?.trim()) { toast.error("请输入详细地址"); return; }
    setEditAddressSaving(true);
    const res = await customerApi.updateAddress(customer.id, addressId, editingAddressForm);
    setEditAddressSaving(false);
    if (res.success && res.data) {
      setAddresses((prev) => prev.map((a) => a.id === addressId ? (res.data as CustomerAddress) : a));
      setEditingAddressId(null);
      toast.success("地址已更新");
    } else {
      toast.error(res.message || "更新失败");
    }
  };

  // ── Bank Edit Handlers ─────────────────────────────────────────────────────
  const startBankEdit = (b: CustomerBankInfo) => {
    setEditingBankId(b.id);
    setEditingBankForm({
      bankName: b.bankName || "",
      accountNumber: b.bankAccount || b.accountNumber || "",
      accountName: b.accountName || "",
      bankCode: b.bankBranch || b.bankCode || "",
      isDefault: b.isDefault || false,
    });
    setShowAddBank(false);
  };

  const saveBank = async (bankId: string) => {
    if (!editingBankForm.bankName?.trim()) { toast.error("请输入银行名称"); return; }
    setBankSaving(true);
    const res = await customerApi.updateBank(customer.id, bankId, editingBankForm);
    setBankSaving(false);
    if (res.success && res.data) {
      setBanks((prev) => prev.map((b) => b.id === bankId ? (res.data as CustomerBankInfo) : b));
      setEditingBankId(null);
      toast.success("银行账户已更新");
    } else {
      toast.error(res.message || "更新失败");
    }
  };

  // ── Add Contact Form ──────────────────────────────────────────────────────
  const AddContactForm = () => {
    const [form, setForm] = useState<AddContactRequest>({
      contactName: "", mobile: "", email: "", position: "", department: "", isDefault: false,
    });
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
            <input type="checkbox" id="contact-default-add" checked={form.isDefault || false}
              onChange={(e) => setForm((f) => ({ ...f, isDefault: e.target.checked }))}
              className="rounded" style={{ accentColor: "#00D4FF" }} />
            <label htmlFor="contact-default-add" className="text-xs" style={{ color: "rgba(224,244,255,0.7)", fontFamily: "Noto Sans SC" }}>设为默认联系人</label>
          </div>
        </div>
        <div className="flex gap-2 mt-3">
          <button onClick={() => setShowAddContact(false)} className="flex-1 py-2 rounded text-xs"
            style={{ background: "#0A1628", border: "1px solid rgba(255,255,255,0.1)", color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>取消</button>
          <button onClick={save} disabled={saving} className="flex-1 py-2 rounded text-xs font-medium flex items-center justify-center gap-1"
            style={{ background: "rgba(0,212,255,0.15)", border: "1px solid rgba(0,212,255,0.4)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}>
            {saving ? <Loader2 size={12} className="animate-spin" /> : <Check size={12} />}保存
          </button>
        </div>
      </div>
    );
  };

  // ── Add Address Form ──────────────────────────────────────────────────────
  const availableCities = addressForm.province ? getCities(addressForm.province) : [];
  const saveAddress = async () => {
    if (!addressForm.address?.trim()) { toast.error("请输入详细地址"); return; }
    setAddressSaving(true);
    const res = await customerApi.addAddress(customer.id, addressForm);
    setAddressSaving(false);
    if (res.success && res.data) {
      setAddresses((prev) => [...prev, res.data!]);
      setShowAddAddress(false);
      setAddressForm({ addressType: 1, province: "", city: "", address: "", contactName: "", contactPhone: "", isDefault: false });
      toast.success("地址添加成功");
    } else {
      toast.error(res.message || "添加失败");
    }
  };

  // ── Add Bank Form ─────────────────────────────────────────────────────────
  const AddBankForm = () => {
    const [form, setForm] = useState<AddBankRequest>({ bankName: "", accountNumber: "", accountName: "", bankCode: "", isDefault: false });
    const [saving, setSaving] = useState(false);
    const set = (k: keyof AddBankRequest) => (v: string) => setForm((f) => ({ ...f, [k]: v }));
    const save = async () => {
      if (!form.bankName?.trim()) { toast.error("请输入银行名称"); return; }
      if (!form.accountNumber?.trim()) { toast.error("请输入账号"); return; }
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
          <FormInput label="银行名称" value={form.bankName || ""} onChange={set("bankName")} required />
          <FormInput label="账号" value={form.accountNumber || ""} onChange={set("accountNumber")} required />
          <FormInput label="户名" value={form.accountName || ""} onChange={set("accountName")} />
          <FormInput label="支行" value={form.bankCode || ""} onChange={set("bankCode")} />
          <div className="flex items-center gap-2 pt-5">
            <input type="checkbox" id="bank-default-add" checked={form.isDefault || false}
              onChange={(e) => setForm((f) => ({ ...f, isDefault: e.target.checked }))}
              style={{ accentColor: "#00D4FF" }} />
            <label htmlFor="bank-default-add" className="text-xs" style={{ color: "rgba(224,244,255,0.7)", fontFamily: "Noto Sans SC" }}>设为默认账户</label>
          </div>
        </div>
        <div className="flex gap-2 mt-3">
          <button onClick={() => setShowAddBank(false)} className="flex-1 py-2 rounded text-xs"
            style={{ background: "#0A1628", border: "1px solid rgba(255,255,255,0.1)", color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>取消</button>
          <button onClick={save} disabled={saving} className="flex-1 py-2 rounded text-xs font-medium flex items-center justify-center gap-1"
            style={{ background: "rgba(0,212,255,0.15)", border: "1px solid rgba(0,212,255,0.4)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}>
            {saving ? <Loader2 size={12} className="animate-spin" /> : <Check size={12} />}保存
          </button>
        </div>
      </div>
    );
  };

  // ── Add History Form ──────────────────────────────────────────────────────
  const AddHistoryForm = () => {
    const [form, setForm] = useState<AddContactHistoryRequest>({ contactType: "call", subject: "", content: "", contactPerson: "", result: "" });
    const [saving, setSaving] = useState(false);
    const set = (k: keyof AddContactHistoryRequest) => (v: string) => setForm((f) => ({ ...f, [k]: v }));
    const save = async () => {
      if (!form.content?.trim()) { toast.error("请输入联系内容"); return; }
      setSaving(true);
      const res = await customerApi.addContactHistory(customer.id, form);
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
          <FormSelect label="联系类型" value={form.contactType || "call"} onChange={set("contactType")} options={[
            { value: "call", label: "电话" }, { value: "visit", label: "拜访" },
            { value: "email", label: "邮件" }, { value: "meeting", label: "会议" }, { value: "other", label: "其他" },
          ]} />
          <FormInput label="联系人（客户方）" value={form.contactPerson || ""} onChange={set("contactPerson")} />
          <div className="col-span-2"><FormInput label="主题" value={form.subject || ""} onChange={set("subject")} /></div>
          <div className="col-span-2">
            <label className="block text-xs mb-1.5" style={{ color: "rgba(0,212,255,0.7)", fontFamily: "Noto Sans SC" }}>
              联系内容 <span style={{ color: "#C97A6E" }}>*</span>
            </label>
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
          <button onClick={() => setShowAddHistory(false)} className="flex-1 py-2 rounded text-xs"
            style={{ background: "#0A1628", border: "1px solid rgba(255,255,255,0.1)", color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>取消</button>
          <button onClick={save} disabled={saving} className="flex-1 py-2 rounded text-xs font-medium flex items-center justify-center gap-1"
            style={{ background: "rgba(0,212,255,0.15)", border: "1px solid rgba(0,212,255,0.4)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}>
            {saving ? <Loader2 size={12} className="animate-spin" /> : <Check size={12} />}保存
          </button>
        </div>
      </div>
    );
  };

  // ── History Edit Handlers ────────────────────────────────────────────────
  const startHistoryEdit = (h: CustomerContactHistory) => {
    setEditingHistoryId(h.id);
    setEditingHistoryForm({
      contactType: h.type || "call",
      subject: h.subject || "",
      content: h.content || "",
      contactPerson: h.contactPerson || "",
      result: h.result || "",
    });
    setShowAddHistory(false);
  };

  const saveHistory = async (historyId: string) => {
    if (!editingHistoryForm.content?.trim()) { toast.error("请输入联系内容"); return; }
    setHistorySaving(true);
    const res = await customerApi.updateContactHistory(customer!.id, historyId, editingHistoryForm);
    setHistorySaving(false);
    if (res.success) {
      setHistory((prev) => prev.map((h) =>
        h.id === historyId
          ? { ...h, type: editingHistoryForm.contactType || h.type, subject: editingHistoryForm.subject, content: editingHistoryForm.content, contactPerson: editingHistoryForm.contactPerson, result: editingHistoryForm.result }
          : h
      ));
      setEditingHistoryId(null);
      toast.success("联系记录已更新");
    } else {
      toast.error(res.message || "更新失败");
    }
  };

  // ── Customer Delete Handler ──────────────────────────────────────────────
  const handleDeleteCustomer = async () => {
    // 非新建状态（status !== 0）必须填写删除理由
    const isNew = customer!.status === 0 || (!customer!.isActive && !customer!.status);
    if (!isNew && !deleteReason.trim()) {
      toast.error("请填写删除理由");
      return;
    }
    setDeleting(true);
    // 调用带理由的删除接口
    const res = await customerApi.deleteWithReason(customer!.id, deleteReason.trim() || undefined);
    setDeleting(false);
    if (res.success) {
      toast.success("客户已删除，可在回收站中恢复");
      navigate("/customer");
    } else {
      toast.error(res.message || "删除失败");
      setShowDeleteConfirm(false);
    }
  };

  // ── Customer Status Handler ─────────────────────────────────────────────
  const handleStatusChange = async (action: "activate" | "deactivate" | "blacklist") => {
    if (action === "blacklist") {
      setShowBlacklistModal(true);
      return;
    }
    setStatusChanging(true);
    let res;
    if (action === "activate") res = await customerApi.activate(customer!.id);
    else res = await customerApi.deactivate(customer!.id);
    setStatusChanging(false);
    if (res.success) {
      const msg = action === "activate" ? "已激活客户" : "已停用客户";
      toast.success(msg);
      const reload = await customerApi.get(customer!.id);
      if (reload.success && reload.data) setCustomer(reload.data as Customer);
    } else {
      toast.error(res.message || "操作失败");
    }
  };

  const handleConfirmBlacklist = async () => {
    if (!blacklistReason.trim()) { toast.error("请填写加入黑名单的理由"); return; }
    setStatusChanging(true);
    const res = await customerApi.setBlacklist(customer!.id, blacklistReason.trim());
    setStatusChanging(false);
    if (res.success) {
      toast.success("已将客户加入黑名单");
      setShowBlacklistModal(false);
      setBlacklistReason("");
      const reload = await customerApi.get(customer!.id);
      if (reload.success && reload.data) setCustomer(reload.data as Customer);
    } else {
      toast.error(res.message || "操作失败");
    }
  };

  // ── Load Logs (moved to useEffect above early returns) ─────────────────────────────────
  // The useEffect for loading logs is placed before early returns above (line ~239)

  // ── Edit Address Inline Form ───────────────────────────────────────────────
  const editAddrCities = editingAddressForm.province ? getCities(editingAddressForm.province) : [];

  return (
    <DashboardLayout>
      <div className="max-w-5xl mx-auto px-4 py-6">
        {/* Back navigation */}
        <button
          onClick={() => navigate("/customer")}
          className="flex items-center gap-2 mb-6 px-3 py-2 rounded-lg transition-all hover:bg-white/5"
          style={{ color: "rgba(200,216,232,0.6)", fontFamily: "Noto Sans SC" }}
        >
          <ArrowLeft size={15} />
          <span className="text-sm">返回客户列表</span>
        </button>

        {/* Customer Header Card */}
        <div
          className="rounded-xl p-6 mb-6"
          style={{ background: "#0A1628", border: "1px solid rgba(0,212,255,0.2)", boxShadow: "0 0 40px rgba(0,212,255,0.05)" }}
        >
          <div className="flex items-start gap-5">
            <div
              className="w-16 h-16 rounded-xl flex items-center justify-center text-2xl font-bold flex-shrink-0"
              style={{ background: levelInfo.bg, border: `1px solid ${levelInfo.color}40`, color: levelInfo.color, fontFamily: "Noto Sans SC" }}
            >
              {(customer.officialName || customer.customerName || "?").charAt(0)}
            </div>
            <div className="flex-1 min-w-0">
              <h1 className="text-lg font-bold mb-2" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
                {customer.officialName || customer.customerName}
              </h1>
              <div className="flex flex-wrap items-center gap-2">
                <span className="text-xs px-2.5 py-1 rounded-md"
                  style={{ background: levelInfo.bg, color: levelInfo.color, border: `1px solid ${levelInfo.color}30`, fontFamily: "Noto Sans SC" }}>
                  {levelInfo.label}
                </span>
                <span className="text-xs font-mono px-2.5 py-1 rounded-md"
                  style={{ background: "rgba(0,212,255,0.06)", color: "#00D4FF", border: "1px solid rgba(0,212,255,0.2)" }}>
                  {customer.customerCode}
                </span>
                <span className="text-xs px-2.5 py-1 rounded-md"
                  style={{
                    background: customer.blackList ? "rgba(201,122,110,0.12)" : customer.isActive ? "rgba(70,191,145,0.1)" : "rgba(201,87,69,0.1)",
                    color: customer.blackList ? "#C97A6E" : customer.isActive ? "#46BF91" : "#C95745",
                    border: `1px solid ${customer.blackList ? "#C97A6E" : customer.isActive ? "#46BF91" : "#C95745"}30`,
                    fontFamily: "Noto Sans SC",
                  }}>
                  {customer.blackList ? "黑名单" : customer.isActive ? "正常" : "停用"}
                </span>
                {customer.industry && (
                  <span className="text-xs px-2.5 py-1 rounded-md"
                    style={{ background: "rgba(80,187,227,0.08)", color: "#50BBE3", border: "1px solid rgba(80,187,227,0.2)", fontFamily: "Noto Sans SC" }}>
                    {customer.industry}
                  </span>
                )}
              </div>
            </div>
            {/* Quick stats + Action buttons */}
            <div className="flex flex-col items-end gap-3 flex-shrink-0">
              <div className="hidden md:flex items-center gap-6">
                <div className="text-right">
                  <p className="text-xs mb-0.5" style={{ color: "rgba(200,216,232,0.5)", fontFamily: "Noto Sans SC" }}>授信额度</p>
                  <p className="text-sm font-semibold" style={{ color: "#C9A96E", fontFamily: "Space Mono" }}>
                    ¥{customer.creditLimit != null ? (customer.creditLimit / 10000).toFixed(1) + "万" : "—"}
                  </p>
                </div>
                <div className="text-right">
                  <p className="text-xs mb-0.5" style={{ color: "rgba(200,216,232,0.5)", fontFamily: "Noto Sans SC" }}>账期</p>
                  <p className="text-sm font-semibold" style={{ color: "#50BBE3", fontFamily: "Space Mono" }}>
                    {customer.paymentTerms != null ? customer.paymentTerms + "天" : "—"}
                  </p>
                </div>
              </div>
              {/* Status action buttons */}
              <div className="flex items-center gap-2">
                {statusChanging ? (
                  <span className="flex items-center gap-1 text-xs" style={{ color: "rgba(200,216,232,0.5)", fontFamily: "Noto Sans SC" }}>
                    <Loader2 size={12} className="animate-spin" />处理中...
                  </span>
                ) : (
                  <>
                    {customer.blackList ? (
                      <button
                        onClick={() => handleStatusChange("activate")}
                        className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs transition-all hover:opacity-80"
                        style={{ background: "rgba(70,191,145,0.1)", border: "1px solid rgba(70,191,145,0.3)", color: "#46BF91", fontFamily: "Noto Sans SC" }}
                      >
                        <ShieldCheck size={12} />解除黑名单
                      </button>
                    ) : customer.isActive ? (
                      <>
                        <button
                          onClick={() => handleStatusChange("deactivate")}
                          className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs transition-all hover:opacity-80"
                          style={{ background: "rgba(201,87,69,0.08)", border: "1px solid rgba(201,87,69,0.25)", color: "#C95745", fontFamily: "Noto Sans SC" }}
                        >
                          <ShieldOff size={12} />停用
                        </button>
                        <button
                          onClick={() => handleStatusChange("blacklist")}
                          className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs transition-all hover:opacity-80"
                          style={{ background: "rgba(201,122,110,0.1)", border: "1px solid rgba(201,122,110,0.3)", color: "#C97A6E", fontFamily: "Noto Sans SC" }}
                        >
                          <Ban size={12} />加入黑名单
                        </button>
                      </>
                    ) : (
                      <button
                        onClick={() => handleStatusChange("activate")}
                        className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs transition-all hover:opacity-80"
                        style={{ background: "rgba(70,191,145,0.1)", border: "1px solid rgba(70,191,145,0.3)", color: "#46BF91", fontFamily: "Noto Sans SC" }}
                      >
                        <ShieldCheck size={12} />激活
                      </button>
                    )}
                    <button
                      onClick={() => setShowDeleteConfirm(true)}
                      className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs transition-all hover:opacity-80"
                      style={{ background: "rgba(201,87,69,0.08)", border: "1px solid rgba(201,87,69,0.2)", color: "#C95745", fontFamily: "Noto Sans SC" }}
                    >
                      <Trash2 size={12} />删除客户
                    </button>
                  </>
                )}
              </div>
            </div>
          </div>
        </div>

        {/* Delete Confirmation Modal */}
        {showDeleteConfirm && (
          <div className="fixed inset-0 z-50 flex items-center justify-center" style={{ background: "rgba(0,0,0,0.7)" }}>
            <div className="rounded-xl p-6 max-w-md w-full mx-4" style={{ background: "#0A1628", border: "1px solid rgba(201,87,69,0.4)" }}>
              <div className="flex items-center gap-3 mb-4">
                <AlertTriangle size={20} style={{ color: "#C95745" }} />
                <h3 className="text-sm font-semibold" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>确认删除客户</h3>
              </div>
              <p className="text-xs leading-relaxed mb-4" style={{ color: "rgba(200,216,232,0.65)", fontFamily: "Noto Sans SC" }}>
                即将删除客户 <span style={{ color: "#E0F4FF", fontWeight: 600 }}>{customer.officialName || customer.customerName}</span>。
                删除后可在<span style={{ color: "#00D4FF" }}>回收站</span>中查看并恢复。
              </p>
              {customer.status !== 0 && (
                <div className="mb-4">
                  <label className="block text-xs mb-1.5" style={{ color: "rgba(201,87,69,0.9)", fontFamily: "Noto Sans SC" }}>
                    删除理由 <span style={{ color: "#C95745" }}>*</span>（非新建状态必填）
                  </label>
                  <textarea
                    value={deleteReason}
                    onChange={(e) => setDeleteReason(e.target.value)}
                    rows={3}
                    placeholder="请说明删除原因，如：客户已注销、重复录入等..."
                    className="w-full px-3 py-2.5 rounded text-xs outline-none resize-none"
                    style={{ background: "#162233", border: "1px solid rgba(201,87,69,0.3)", color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
                  />
                </div>
              )}
              <div className="flex gap-3">
                <button
                  onClick={() => { setShowDeleteConfirm(false); setDeleteReason(""); }}
                  className="flex-1 py-2.5 rounded-lg text-xs"
                  style={{ background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.1)", color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}
                >
                  取消
                </button>
                <button
                  onClick={handleDeleteCustomer}
                  disabled={deleting}
                  className="flex-1 py-2.5 rounded-lg text-xs font-medium flex items-center justify-center gap-1.5"
                  style={{ background: "rgba(201,87,69,0.2)", border: "1px solid rgba(201,87,69,0.5)", color: "#C95745", fontFamily: "Noto Sans SC" }}
                >
                  {deleting ? <Loader2 size={13} className="animate-spin" /> : <Trash2 size={13} />}
                  确认删除
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Blacklist Modal */}
        {showBlacklistModal && (
          <div className="fixed inset-0 z-50 flex items-center justify-center" style={{ background: "rgba(0,0,0,0.7)" }}>
            <div className="rounded-xl p-6 max-w-md w-full mx-4" style={{ background: "#0A1628", border: "1px solid rgba(255,165,0,0.4)" }}>
              <div className="flex items-center gap-3 mb-4">
                <AlertTriangle size={20} style={{ color: "#FFA500" }} />
                <h3 className="text-sm font-semibold" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>加入黑名单</h3>
              </div>
              <p className="text-xs leading-relaxed mb-4" style={{ color: "rgba(200,216,232,0.65)", fontFamily: "Noto Sans SC" }}>
                将客户 <span style={{ color: "#E0F4FF", fontWeight: 600 }}>{customer.officialName || customer.customerName}</span> 加入黑名单后，
                可在<span style={{ color: "#00D4FF" }}>黑名单管理</span>中查看并移出。
              </p>
              <div className="mb-4">
                <label className="block text-xs mb-1.5" style={{ color: "rgba(255,165,0,0.9)", fontFamily: "Noto Sans SC" }}>
                  黑名单理由 <span style={{ color: "#FFA500" }}>*</span>（必填）
                </label>
                <textarea
                  value={blacklistReason}
                  onChange={(e) => setBlacklistReason(e.target.value)}
                  rows={3}
                  placeholder="请说明加入黑名单的原因，如：恶意欠款、虚假信息、违规操作等..."
                  className="w-full px-3 py-2.5 rounded text-xs outline-none resize-none"
                  style={{ background: "#162233", border: "1px solid rgba(255,165,0,0.3)", color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
                />
              </div>
              <div className="flex gap-3">
                <button
                  onClick={() => { setShowBlacklistModal(false); setBlacklistReason(""); }}
                  className="flex-1 py-2.5 rounded-lg text-xs"
                  style={{ background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.1)", color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}
                >
                  取消
                </button>
                <button
                  onClick={handleConfirmBlacklist}
                  disabled={statusChanging}
                  className="flex-1 py-2.5 rounded-lg text-xs font-medium flex items-center justify-center gap-1.5"
                  style={{ background: "rgba(255,165,0,0.15)", border: "1px solid rgba(255,165,0,0.5)", color: "#FFA500", fontFamily: "Noto Sans SC" }}
                >
                  {statusChanging ? <Loader2 size={13} className="animate-spin" /> : <AlertTriangle size={13} />}
                  确认加入黑名单
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Tabs */}
        <div
          className="rounded-xl overflow-hidden"
          style={{ background: "#0A1628", border: "1px solid rgba(0,212,255,0.15)" }}
        >
          {/* Tab bar */}
          <div className="flex overflow-x-auto" style={{ borderBottom: "1px solid rgba(0,212,255,0.1)" }}>
            {tabs.map((tab) => (
              <button
                key={tab.key}
                onClick={() => setActiveTab(tab.key)}
                className="flex items-center gap-2 px-5 py-3.5 text-xs whitespace-nowrap transition-all flex-shrink-0"
                style={{
                  color: activeTab === tab.key ? "#00D4FF" : "rgba(200,216,232,0.5)",
                  borderBottom: activeTab === tab.key ? "2px solid #00D4FF" : "2px solid transparent",
                  background: activeTab === tab.key ? "rgba(0,212,255,0.04)" : "transparent",
                  fontFamily: "Noto Sans SC",
                }}
              >
                {tab.icon}
                <span>{tab.label}</span>
              </button>
            ))}
          </div>

          {/* Tab content */}
          <div className="p-6">

            {/* ── 基本信息 ── */}
            {activeTab === "info" && (
              <div>
                {/* Edit / Cancel / Save buttons */}
                <div className="flex justify-end mb-4 gap-2">
                  {infoEditing ? (
                    <>
                      <button
                        onClick={cancelInfoEdit}
                        className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-xs"
                        style={{ background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.12)", color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}
                      >
                        <X size={13} />取消
                      </button>
                      <button
                        onClick={saveInfo}
                        disabled={infoSaving}
                        className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-xs font-medium"
                        style={{ background: "rgba(0,212,255,0.15)", border: "1px solid rgba(0,212,255,0.4)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}
                      >
                        {infoSaving ? <Loader2 size={13} className="animate-spin" /> : <Check size={13} />}保存修改
                      </button>
                    </>
                  ) : (
                    <button
                      onClick={startInfoEdit}
                      className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-xs"
                      style={{ background: "rgba(50,149,201,0.1)", border: "1px solid rgba(50,149,201,0.3)", color: "#3295C9", fontFamily: "Noto Sans SC" }}
                    >
                      <Edit2 size={13} />编辑信息
                    </button>
                  )}
                </div>

                {infoEditing ? (
                  /* ── Edit Mode ── */
                  <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                    <FormInput label="公司全称" value={infoForm.customerName || ""} onChange={(v) => setInfoForm((f) => ({ ...f, customerName: v }))} required />
                    <FormInput label="公司简称" value={infoForm.customerShortName || ""} onChange={(v) => setInfoForm((f) => ({ ...f, customerShortName: v }))} />
                    <div>
                      <label className="block text-xs mb-1.5" style={{ color: "rgba(0,212,255,0.7)", fontFamily: "Noto Sans SC" }}>客户等级</label>
                      <select
                        value={infoForm.customerLevel || "D"}
                        onChange={(e) => setInfoForm((f) => ({ ...f, customerLevel: e.target.value }))}
                        className="w-full px-3 py-2.5 rounded text-xs outline-none"
                        style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.2)", color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
                      >
                        {["D", "C", "B", "BPO", "VIP", "VPO"].map((l) => (
                          <option key={l} value={l} style={{ background: "#162233" }}>{LEVEL_MAP[l]?.label || l}</option>
                        ))}
                      </select>
                    </div>
                    <FormInput label="行业" value={infoForm.industry || ""} onChange={(v) => setInfoForm((f) => ({ ...f, industry: v }))} />
                    <FormInput label="主营产品" value={infoForm.product || ""} onChange={(v) => setInfoForm((f) => ({ ...f, product: v }))} />
                    <FormInput label="授信额度（元）" type="number" value={String(infoForm.creditLimit ?? "")} onChange={(v) => setInfoForm((f) => ({ ...f, creditLimit: Number(v) }))} />
                    <FormInput label="账期（天）" type="number" value={String(infoForm.paymentTerms ?? "")} onChange={(v) => setInfoForm((f) => ({ ...f, paymentTerms: Number(v) }))} />
                    <div>
                      <label className="block text-xs mb-1.5" style={{ color: "rgba(0,212,255,0.7)", fontFamily: "Noto Sans SC" }}>结算货币</label>
                      <select
                        value={String(infoForm.currency ?? 1)}
                        onChange={(e) => setInfoForm((f) => ({ ...f, currency: Number(e.target.value) }))}
                        className="w-full px-3 py-2.5 rounded text-xs outline-none"
                        style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.2)", color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
                      >
                        <option value="1" style={{ background: "#162233" }}>人民币 CNY</option>
                        <option value="2" style={{ background: "#162233" }}>美元 USD</option>
                        <option value="3" style={{ background: "#162233" }}>欧元 EUR</option>
                      </select>
                    </div>
                    <FormInput label="统一社会信用代码" value={infoForm.unifiedSocialCreditCode || ""} onChange={(v) => setInfoForm((f) => ({ ...f, unifiedSocialCreditCode: v }))} />
                    <div className="col-span-2 md:col-span-3">
                      <label className="block text-xs mb-1.5" style={{ color: "rgba(0,212,255,0.7)", fontFamily: "Noto Sans SC" }}>备注</label>
                      <textarea
                        value={infoForm.remarks || ""}
                        onChange={(e) => setInfoForm((f) => ({ ...f, remarks: e.target.value }))}
                        rows={3}
                        className="w-full px-3 py-2.5 rounded text-xs outline-none resize-none"
                        style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.2)", color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
                      />
                    </div>
                  </div>
                ) : (
                  /* ── View Mode ── */
                  <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                    <FieldBox label="客户编号" value={customer.customerCode} />
                    <FieldBox label="客户等级" value={levelInfo.label} />
                    <FieldBox label="公司全称" value={customer.officialName || customer.customerName} />
                    <FieldBox label="公司简称" value={customer.nickName || customer.customerShortName} />
                    <FieldBox label="行业" value={customer.industry} />
                    <FieldBox label="主营产品" value={customer.product} />
                    <FieldBox label="授信额度" value={customer.creditLimit != null ? `¥${customer.creditLimit.toLocaleString()}` : undefined} />
                    <FieldBox label="账期" value={customer.paymentTerms != null ? `${customer.paymentTerms} 天` : undefined} />
                    <FieldBox label="结算货币" value={customer.currency === 1 ? "人民币 CNY" : customer.currency === 2 ? "美元 USD" : customer.currency === 3 ? "欧元 EUR" : undefined} />
                    <FieldBox label="税率" value={customer.taxRate != null ? `${customer.taxRate}%` : undefined} />
                    <FieldBox label="统一社会信用代码" value={customer.creditCode || customer.unifiedSocialCreditCode} />
                    <FieldBox label="备注" value={customer.remark || customer.remarks} />
                  </div>
                )}
              </div>
            )}

            {/* ── 联系人 ── */}
            {activeTab === "contacts" && (
              <div>
                <div className="flex justify-end mb-4">
                  <button
                    onClick={() => { setShowAddContact(!showAddContact); setEditingContactId(null); }}
                    className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-xs"
                    style={{ background: "rgba(0,212,255,0.1)", border: "1px solid rgba(0,212,255,0.3)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}
                  >
                    <Plus size={13} />新增联系人
                  </button>
                </div>
                {showAddContact && <AddContactForm />}
                {contacts.length === 0 && !showAddContact ? (
                  <div className="text-center py-16">
                    <UserCheck size={32} style={{ color: "rgba(200,216,232,0.2)", margin: "0 auto 12px" }} />
                    <p className="text-sm" style={{ color: "rgba(200,216,232,0.4)", fontFamily: "Noto Sans SC" }}>暂无联系人</p>
                  </div>
                ) : (
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3 mt-4">
                    {contacts.map((c) => (
                      <div key={c.id}>
                        {editingContactId === c.id ? (
                          /* ── Contact Edit Inline Form ── */
                          <div className="p-4 rounded-lg" style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.25)" }}>
                            <p className="text-xs font-medium mb-3" style={{ color: "#00D4FF", fontFamily: "Noto Sans SC" }}>编辑联系人</p>
                            <div className="grid grid-cols-2 gap-3">
                              <FormInput label="姓名" value={editingContactForm.contactName || ""} onChange={(v) => setEditingContactForm((f) => ({ ...f, contactName: v }))} required />
                              <FormInput label="手机" value={editingContactForm.mobile || ""} onChange={(v) => setEditingContactForm((f) => ({ ...f, mobile: v }))} />
                              <FormInput label="邮箱" value={editingContactForm.email || ""} onChange={(v) => setEditingContactForm((f) => ({ ...f, email: v }))} />
                              <FormInput label="职位" value={editingContactForm.position || ""} onChange={(v) => setEditingContactForm((f) => ({ ...f, position: v }))} />
                              <FormInput label="部门" value={editingContactForm.department || ""} onChange={(v) => setEditingContactForm((f) => ({ ...f, department: v }))} />
                              <div className="flex items-center gap-2 pt-5">
                                <input type="checkbox" id={`contact-edit-default-${c.id}`} checked={editingContactForm.isDefault || false}
                                  onChange={(e) => setEditingContactForm((f) => ({ ...f, isDefault: e.target.checked }))}
                                  style={{ accentColor: "#00D4FF" }} />
                                <label htmlFor={`contact-edit-default-${c.id}`} className="text-xs" style={{ color: "rgba(224,244,255,0.7)", fontFamily: "Noto Sans SC" }}>设为默认联系人</label>
                              </div>
                            </div>
                            <div className="flex gap-2 mt-3">
                              <button onClick={() => setEditingContactId(null)} className="flex-1 py-2 rounded text-xs"
                                style={{ background: "#0A1628", border: "1px solid rgba(255,255,255,0.1)", color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>取消</button>
                              <button onClick={() => saveContact(c.id)} disabled={contactSaving} className="flex-1 py-2 rounded text-xs font-medium flex items-center justify-center gap-1"
                                style={{ background: "rgba(0,212,255,0.15)", border: "1px solid rgba(0,212,255,0.4)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}>
                                {contactSaving ? <Loader2 size={12} className="animate-spin" /> : <Check size={12} />}保存
                              </button>
                            </div>
                          </div>
                        ) : (
                          /* ── Contact Display Card ── */
                          <div className="p-4 rounded-lg flex items-start justify-between"
                            style={{ background: "#162233", border: "1px solid rgba(255,255,255,0.08)" }}>
                            <div className="flex items-start gap-3">
                              <div className="w-9 h-9 rounded-lg flex items-center justify-center text-sm font-bold flex-shrink-0"
                                style={{ background: "rgba(0,212,255,0.1)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}>
                                {(c.name || c.contactName || "?").charAt(0)}
                              </div>
                              <div>
                                <div className="flex items-center gap-2 mb-1">
                                  <span className="text-sm font-medium" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>{c.name || c.contactName}</span>
                                  {(c.isMain || c.isDefault) && (
                                    <span className="text-xs px-1.5 py-0.5 rounded"
                                      style={{ background: "rgba(70,191,145,0.1)", color: "#46BF91", border: "1px solid #46BF9130", fontFamily: "Noto Sans SC" }}>默认</span>
                                  )}
                                </div>
                                <div className="flex flex-wrap gap-2 text-xs" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>
                                  {(c.title || c.position) && <span>{c.title || c.position}</span>}
                                  {c.department && <span>{c.department}</span>}
                                </div>
                                <div className="flex flex-wrap gap-3 mt-1.5 text-xs" style={{ color: "rgba(200,216,232,0.6)" }}>
                                  {(c.phone || c.mobile) && (
                                    <span className="flex items-center gap-1">
                                      <Phone size={10} />{c.phone || c.mobile}
                                    </span>
                                  )}
                                  {c.email && (
                                    <span className="flex items-center gap-1">
                                      <Mail size={10} />{c.email}
                                    </span>
                                  )}
                                </div>
                              </div>
                            </div>
                            <div className="flex items-center gap-1 flex-shrink-0">
                              <EditBtn onClick={() => startContactEdit(c)} />
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
                                className="p-1.5 rounded hover:bg-white/5 transition-colors"
                              >
                                <Trash2 size={13} style={{ color: "#C95745" }} />
                              </button>
                            </div>
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}

            {/* ── 地址 ── */}
            {activeTab === "addresses" && (
              <div>
                <div className="flex justify-end mb-4">
                  <button
                    onClick={() => { setShowAddAddress(!showAddAddress); setEditingAddressId(null); }}
                    className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-xs"
                    style={{ background: "rgba(0,212,255,0.1)", border: "1px solid rgba(0,212,255,0.3)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}
                  >
                    <Plus size={13} />新增地址
                  </button>
                </div>
                {showAddAddress && (
                  <div className="mt-4 p-4 rounded-lg" style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.15)" }}>
                    <p className="text-xs font-medium mb-3" style={{ color: "#00D4FF", fontFamily: "Noto Sans SC" }}>新增地址</p>
                    <div className="grid grid-cols-2 gap-3">
                      <FormSelect label="地址类型" value={String(addressForm.addressType || 1)}
                        onChange={(v) => setAddressForm((f) => ({ ...f, addressType: Number(v) }))}
                        options={[{ value: "1", label: "发货地址" }, { value: "2", label: "收货地址" }, { value: "3", label: "账单地址" }, { value: "4", label: "注册地址" }]} />
                      <div>
                        <label className="block text-xs mb-1.5" style={{ color: "rgba(0,212,255,0.7)", fontFamily: "Noto Sans SC" }}>省份</label>
                        <select value={addressForm.province || ""}
                          onChange={(e) => setAddressForm((f) => ({ ...f, province: e.target.value, city: "" }))}
                          className="w-full px-3 py-2.5 rounded text-xs outline-none"
                          style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.2)", color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
                          <option value="" style={{ background: "#162233" }}>请选择省份</option>
                          {PROVINCES.map((p) => <option key={p} value={p} style={{ background: "#162233" }}>{p}</option>)}
                        </select>
                      </div>
                      <div>
                        <label className="block text-xs mb-1.5" style={{ color: "rgba(0,212,255,0.7)", fontFamily: "Noto Sans SC" }}>城市</label>
                        <select value={addressForm.city || ""}
                          onChange={(e) => setAddressForm((f) => ({ ...f, city: e.target.value }))}
                          className="w-full px-3 py-2.5 rounded text-xs outline-none"
                          style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.2)", color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
                          disabled={!addressForm.province}>
                          <option value="" style={{ background: "#162233" }}>{addressForm.province ? "请选择城市" : "请先选择省份"}</option>
                          {availableCities.map((c) => <option key={c} value={c} style={{ background: "#162233" }}>{c}</option>)}
                        </select>
                      </div>
                      <FormInput label="联系人" value={addressForm.contactName || ""} onChange={(v) => setAddressForm((f) => ({ ...f, contactName: v }))} />
                      <div className="col-span-2">
                        <FormInput label="详细地址" value={addressForm.address || ""} onChange={(v) => setAddressForm((f) => ({ ...f, address: v }))} required />
                      </div>
                      <FormInput label="联系电话" value={addressForm.contactPhone || ""} onChange={(v) => setAddressForm((f) => ({ ...f, contactPhone: v }))} />
                      <div className="flex items-center gap-2 pt-5">
                        <input type="checkbox" id="addr-default-add" checked={addressForm.isDefault || false}
                          onChange={(e) => setAddressForm((f) => ({ ...f, isDefault: e.target.checked }))}
                          style={{ accentColor: "#00D4FF" }} />
                        <label htmlFor="addr-default-add" className="text-xs" style={{ color: "rgba(224,244,255,0.7)", fontFamily: "Noto Sans SC" }}>设为默认地址</label>
                      </div>
                    </div>
                    <div className="flex gap-2 mt-3">
                      <button onClick={() => { setShowAddAddress(false); setAddressForm({ addressType: 1, province: "", city: "", address: "", contactName: "", contactPhone: "", isDefault: false }); }}
                        className="flex-1 py-2 rounded text-xs"
                        style={{ background: "#0A1628", border: "1px solid rgba(255,255,255,0.1)", color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>取消</button>
                      <button onClick={saveAddress} disabled={addressSaving}
                        className="flex-1 py-2 rounded text-xs font-medium flex items-center justify-center gap-1"
                        style={{ background: "rgba(0,212,255,0.15)", border: "1px solid rgba(0,212,255,0.4)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}>
                        {addressSaving ? <Loader2 size={12} className="animate-spin" /> : <Check size={12} />}保存
                      </button>
                    </div>
                  </div>
                )}
                {addresses.length === 0 && !showAddAddress ? (
                  <div className="text-center py-16">
                    <MapPin size={32} style={{ color: "rgba(200,216,232,0.2)", margin: "0 auto 12px" }} />
                    <p className="text-sm" style={{ color: "rgba(200,216,232,0.4)", fontFamily: "Noto Sans SC" }}>暂无地址</p>
                  </div>
                ) : (
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3 mt-4">
                    {addresses.map((a) => (
                      <div key={a.id}>
                        {editingAddressId === a.id ? (
                          /* ── Address Edit Inline Form ── */
                          <div className="p-4 rounded-lg" style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.25)" }}>
                            <p className="text-xs font-medium mb-3" style={{ color: "#00D4FF", fontFamily: "Noto Sans SC" }}>编辑地址</p>
                            <div className="grid grid-cols-2 gap-3">
                              <FormSelect label="地址类型" value={String(editingAddressForm.addressType || 1)}
                                onChange={(v) => setEditingAddressForm((f) => ({ ...f, addressType: Number(v) }))}
                                options={[{ value: "1", label: "发货地址" }, { value: "2", label: "收货地址" }, { value: "3", label: "账单地址" }, { value: "4", label: "注册地址" }]} />
                              <div>
                                <label className="block text-xs mb-1.5" style={{ color: "rgba(0,212,255,0.7)", fontFamily: "Noto Sans SC" }}>省份</label>
                                <select value={editingAddressForm.province || ""}
                                  onChange={(e) => setEditingAddressForm((f) => ({ ...f, province: e.target.value, city: "" }))}
                                  className="w-full px-3 py-2.5 rounded text-xs outline-none"
                                  style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.2)", color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
                                  <option value="" style={{ background: "#162233" }}>请选择省份</option>
                                  {PROVINCES.map((p) => <option key={p} value={p} style={{ background: "#162233" }}>{p}</option>)}
                                </select>
                              </div>
                              <div>
                                <label className="block text-xs mb-1.5" style={{ color: "rgba(0,212,255,0.7)", fontFamily: "Noto Sans SC" }}>城市</label>
                                <select value={editingAddressForm.city || ""}
                                  onChange={(e) => setEditingAddressForm((f) => ({ ...f, city: e.target.value }))}
                                  className="w-full px-3 py-2.5 rounded text-xs outline-none"
                                  style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.2)", color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
                                  disabled={!editingAddressForm.province}>
                                  <option value="" style={{ background: "#162233" }}>{editingAddressForm.province ? "请选择城市" : "请先选择省份"}</option>
                                  {editAddrCities.map((c) => <option key={c} value={c} style={{ background: "#162233" }}>{c}</option>)}
                                </select>
                              </div>
                              <FormInput label="联系人" value={editingAddressForm.contactName || ""} onChange={(v) => setEditingAddressForm((f) => ({ ...f, contactName: v }))} />
                              <div className="col-span-2">
                                <FormInput label="详细地址" value={editingAddressForm.address || ""} onChange={(v) => setEditingAddressForm((f) => ({ ...f, address: v }))} required />
                              </div>
                              <FormInput label="联系电话" value={editingAddressForm.contactPhone || ""} onChange={(v) => setEditingAddressForm((f) => ({ ...f, contactPhone: v }))} />
                              <div className="flex items-center gap-2 pt-5">
                                <input type="checkbox" id={`addr-edit-default-${a.id}`} checked={editingAddressForm.isDefault || false}
                                  onChange={(e) => setEditingAddressForm((f) => ({ ...f, isDefault: e.target.checked }))}
                                  style={{ accentColor: "#00D4FF" }} />
                                <label htmlFor={`addr-edit-default-${a.id}`} className="text-xs" style={{ color: "rgba(224,244,255,0.7)", fontFamily: "Noto Sans SC" }}>设为默认地址</label>
                              </div>
                            </div>
                            <div className="flex gap-2 mt-3">
                              <button onClick={() => setEditingAddressId(null)} className="flex-1 py-2 rounded text-xs"
                                style={{ background: "#0A1628", border: "1px solid rgba(255,255,255,0.1)", color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>取消</button>
                              <button onClick={() => saveAddress_edit(a.id)} disabled={editAddressSaving} className="flex-1 py-2 rounded text-xs font-medium flex items-center justify-center gap-1"
                                style={{ background: "rgba(0,212,255,0.15)", border: "1px solid rgba(0,212,255,0.4)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}>
                                {editAddressSaving ? <Loader2 size={12} className="animate-spin" /> : <Check size={12} />}保存
                              </button>
                            </div>
                          </div>
                        ) : (
                          /* ── Address Display Card ── */
                          <div className="p-4 rounded-lg flex items-start justify-between"
                            style={{ background: "#162233", border: "1px solid rgba(255,255,255,0.08)" }}>
                            <div className="flex items-start gap-3">
                              <MapPin size={16} style={{ color: "#3295C9", marginTop: 2, flexShrink: 0 }} />
                              <div>
                                <div className="flex items-center gap-2 mb-1">
                                  <span className="text-xs px-1.5 py-0.5 rounded"
                                    style={{ background: "rgba(50,149,201,0.1)", color: "#3295C9", border: "1px solid rgba(50,149,201,0.3)", fontFamily: "Noto Sans SC" }}>
                                    {ADDRESS_TYPE_MAP[a.addressType] || "地址"}
                                  </span>
                                  {a.isDefault && (
                                    <span className="text-xs px-1.5 py-0.5 rounded"
                                      style={{ background: "rgba(70,191,145,0.1)", color: "#46BF91", border: "1px solid #46BF9130", fontFamily: "Noto Sans SC" }}>默认</span>
                                  )}
                                </div>
                                <p className="text-sm font-medium" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>
                                  {[a.province, a.city, a.address].filter(Boolean).join(" ")}
                                </p>
                                {(a.contactName || a.contactPhone) && (
                                  <p className="text-xs mt-1" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>
                                    {a.contactName}{a.contactName && a.contactPhone ? " · " : ""}{a.contactPhone}
                                  </p>
                                )}
                              </div>
                            </div>
                            <div className="flex items-center gap-1 flex-shrink-0">
                              <EditBtn onClick={() => startAddressEdit(a)} />
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
                                className="p-1.5 rounded hover:bg-white/5 transition-colors"
                              >
                                <Trash2 size={13} style={{ color: "#C95745" }} />
                              </button>
                            </div>
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}

            {/* ── 银行账户 ── */}
            {activeTab === "banks" && (
              <div>
                <div className="flex justify-end mb-4">
                  <button
                    onClick={() => { setShowAddBank(!showAddBank); setEditingBankId(null); }}
                    className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-xs"
                    style={{ background: "rgba(0,212,255,0.1)", border: "1px solid rgba(0,212,255,0.3)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}
                  >
                    <Plus size={13} />新增银行账户
                  </button>
                </div>
                {showAddBank && <AddBankForm />}
                {banks.length === 0 && !showAddBank ? (
                  <div className="text-center py-16">
                    <CreditCard size={32} style={{ color: "rgba(200,216,232,0.2)", margin: "0 auto 12px" }} />
                    <p className="text-sm" style={{ color: "rgba(200,216,232,0.4)", fontFamily: "Noto Sans SC" }}>暂无银行账户</p>
                  </div>
                ) : (
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3 mt-4">
                    {banks.map((b) => (
                      <div key={b.id}>
                        {editingBankId === b.id ? (
                          /* ── Bank Edit Inline Form ── */
                          <div className="p-4 rounded-lg" style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.25)" }}>
                            <p className="text-xs font-medium mb-3" style={{ color: "#00D4FF", fontFamily: "Noto Sans SC" }}>编辑银行账户</p>
                            <div className="grid grid-cols-2 gap-3">
                              <FormInput label="银行名称" value={editingBankForm.bankName || ""} onChange={(v) => setEditingBankForm((f) => ({ ...f, bankName: v }))} required />
                              <FormInput label="账号" value={editingBankForm.accountNumber || ""} onChange={(v) => setEditingBankForm((f) => ({ ...f, accountNumber: v }))} required />
                              <FormInput label="户名" value={editingBankForm.accountName || ""} onChange={(v) => setEditingBankForm((f) => ({ ...f, accountName: v }))} />
                              <FormInput label="支行" value={editingBankForm.bankCode || ""} onChange={(v) => setEditingBankForm((f) => ({ ...f, bankCode: v }))} />
                              <div className="flex items-center gap-2 pt-5">
                                <input type="checkbox" id={`bank-edit-default-${b.id}`} checked={editingBankForm.isDefault || false}
                                  onChange={(e) => setEditingBankForm((f) => ({ ...f, isDefault: e.target.checked }))}
                                  style={{ accentColor: "#00D4FF" }} />
                                <label htmlFor={`bank-edit-default-${b.id}`} className="text-xs" style={{ color: "rgba(224,244,255,0.7)", fontFamily: "Noto Sans SC" }}>设为默认账户</label>
                              </div>
                            </div>
                            <div className="flex gap-2 mt-3">
                              <button onClick={() => setEditingBankId(null)} className="flex-1 py-2 rounded text-xs"
                                style={{ background: "#0A1628", border: "1px solid rgba(255,255,255,0.1)", color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>取消</button>
                              <button onClick={() => saveBank(b.id)} disabled={bankSaving} className="flex-1 py-2 rounded text-xs font-medium flex items-center justify-center gap-1"
                                style={{ background: "rgba(0,212,255,0.15)", border: "1px solid rgba(0,212,255,0.4)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}>
                                {bankSaving ? <Loader2 size={12} className="animate-spin" /> : <Check size={12} />}保存
                              </button>
                            </div>
                          </div>
                        ) : (
                          /* ── Bank Display Card ── */
                          <div className="p-4 rounded-lg flex items-start justify-between"
                            style={{ background: "#162233", border: "1px solid rgba(255,255,255,0.08)" }}>
                            <div className="flex items-start gap-3">
                              <CreditCard size={16} style={{ color: "#C9A96E", marginTop: 2, flexShrink: 0 }} />
                              <div>
                                <div className="flex items-center gap-2 mb-1">
                                  <span className="text-sm font-medium" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>{b.bankName || "银行"}</span>
                                  {b.isDefault && (
                                    <span className="text-xs px-1.5 py-0.5 rounded"
                                      style={{ background: "rgba(70,191,145,0.1)", color: "#46BF91", border: "1px solid #46BF9130", fontFamily: "Noto Sans SC" }}>默认</span>
                                  )}
                                </div>
                                <p className="text-xs font-mono mt-0.5" style={{ color: "rgba(200,216,232,0.7)" }}>{b.bankAccount || b.accountNumber}</p>
                                {b.accountName && <p className="text-xs mt-0.5" style={{ color: "rgba(200,216,232,0.55)", fontFamily: "Noto Sans SC" }}>户名：{b.accountName}</p>}
                                {(b.bankBranch || b.bankCode) && <p className="text-xs mt-0.5" style={{ color: "rgba(200,216,232,0.45)", fontFamily: "Noto Sans SC" }}>支行：{b.bankBranch || b.bankCode}</p>}
                              </div>
                            </div>
                            <div className="flex items-center gap-1 flex-shrink-0">
                              <EditBtn onClick={() => startBankEdit(b)} />
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
                                className="p-1.5 rounded hover:bg-white/5 transition-colors"
                              >
                                <Trash2 size={13} style={{ color: "#C95745" }} />
                              </button>
                            </div>
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}

            {/* ── 联系历史 ── */}
            {activeTab === "history" && (
              <div>
                <div className="flex justify-end mb-4">
                  <button
                    onClick={() => setShowAddHistory(!showAddHistory)}
                    className="flex items-center gap-1.5 px-4 py-2 rounded-lg text-xs"
                    style={{ background: "rgba(0,212,255,0.1)", border: "1px solid rgba(0,212,255,0.3)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}
                  >
                    <Plus size={13} />新增联系记录
                  </button>
                </div>
                {showAddHistory && <AddHistoryForm />}
                {history.length === 0 && !showAddHistory ? (
                  <div className="text-center py-16">
                    <Clock size={32} style={{ color: "rgba(200,216,232,0.2)", margin: "0 auto 12px" }} />
                    <p className="text-sm" style={{ color: "rgba(200,216,232,0.4)", fontFamily: "Noto Sans SC" }}>暂无联系历史</p>
                  </div>
                ) : (
                  <div className="space-y-3 mt-4">
                    {history.map((h) => (
                      <div key={h.id}>
                        {editingHistoryId === h.id ? (
                          /* ── History Edit Inline Form ── */
                          <div className="p-4 rounded-lg" style={{ background: "#162233", border: "1px solid rgba(0,212,255,0.25)" }}>
                            <p className="text-xs font-medium mb-3" style={{ color: "#00D4FF", fontFamily: "Noto Sans SC" }}>编辑联系记录</p>
                            <div className="grid grid-cols-2 gap-3">
                              <div>
                                <label className="block text-xs mb-1.5" style={{ color: "rgba(0,212,255,0.7)", fontFamily: "Noto Sans SC" }}>联系类型</label>
                                <select
                                  value={editingHistoryForm.contactType || "call"}
                                  onChange={(e) => setEditingHistoryForm((f) => ({ ...f, contactType: e.target.value }))}
                                  className="w-full px-3 py-2.5 rounded text-xs outline-none"
                                  style={{ background: "#0A1628", border: "1px solid rgba(0,212,255,0.2)", color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
                                >
                                  <option value="call" style={{ background: "#0A1628" }}>电话</option>
                                  <option value="visit" style={{ background: "#0A1628" }}>拜访</option>
                                  <option value="email" style={{ background: "#0A1628" }}>邮件</option>
                                  <option value="meeting" style={{ background: "#0A1628" }}>会议</option>
                                  <option value="other" style={{ background: "#0A1628" }}>其他</option>
                                </select>
                              </div>
                              <FormInput label="联系人（客户方）" value={editingHistoryForm.contactPerson || ""} onChange={(v) => setEditingHistoryForm((f) => ({ ...f, contactPerson: v }))} />
                              <div className="col-span-2"><FormInput label="主题" value={editingHistoryForm.subject || ""} onChange={(v) => setEditingHistoryForm((f) => ({ ...f, subject: v }))} /></div>
                              <div className="col-span-2">
                                <label className="block text-xs mb-1.5" style={{ color: "rgba(0,212,255,0.7)", fontFamily: "Noto Sans SC" }}>
                                  联系内容 <span style={{ color: "#C97A6E" }}>*</span>
                                </label>
                                <textarea
                                  value={editingHistoryForm.content || ""}
                                  onChange={(e) => setEditingHistoryForm((f) => ({ ...f, content: e.target.value }))}
                                  rows={3}
                                  className="w-full px-3 py-2.5 rounded text-xs outline-none resize-none"
                                  style={{ background: "#0A1628", border: "1px solid rgba(0,212,255,0.2)", color: "#E0F4FF", fontFamily: "Noto Sans SC" }}
                                />
                              </div>
                              <div className="col-span-2"><FormInput label="跟进结果" value={editingHistoryForm.result || ""} onChange={(v) => setEditingHistoryForm((f) => ({ ...f, result: v }))} /></div>
                            </div>
                            <div className="flex gap-2 mt-3">
                              <button onClick={() => setEditingHistoryId(null)} className="flex-1 py-2 rounded text-xs"
                                style={{ background: "#0A1628", border: "1px solid rgba(255,255,255,0.1)", color: "rgba(224,244,255,0.5)", fontFamily: "Noto Sans SC" }}>取消</button>
                              <button onClick={() => saveHistory(h.id)} disabled={historySaving} className="flex-1 py-2 rounded text-xs font-medium flex items-center justify-center gap-1"
                                style={{ background: "rgba(0,212,255,0.15)", border: "1px solid rgba(0,212,255,0.4)", color: "#00D4FF", fontFamily: "Noto Sans SC" }}>
                                {historySaving ? <Loader2 size={12} className="animate-spin" /> : <Check size={12} />}保存
                              </button>
                            </div>
                          </div>
                        ) : (
                          /* ── History Display Card ── */
                          <div className="p-4 rounded-lg"
                            style={{ background: "#162233", border: "1px solid rgba(255,255,255,0.08)" }}>
                            <div className="flex items-start justify-between mb-2">
                              <div className="flex items-center gap-2">
                                <span className="text-xs px-2 py-0.5 rounded"
                                  style={{ background: "rgba(0,212,255,0.08)", color: "#00D4FF", border: "1px solid rgba(0,212,255,0.2)", fontFamily: "Noto Sans SC" }}>
                                  {CONTACT_TYPE_MAP[h.type || "other"] || h.type}
                                </span>
                                {h.subject && (
                                  <span className="text-xs font-medium" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>{h.subject}</span>
                                )}
                              </div>
                              <div className="flex items-center gap-2">
                                <span className="text-xs" style={{ color: "rgba(200,216,232,0.4)", fontFamily: "Space Mono" }}>
                                  {h.time ? new Date(h.time).toLocaleDateString("zh-CN") : ""}
                                </span>
                                <EditBtn onClick={() => startHistoryEdit(h)} />
                                <button
                                  onClick={async () => {
                                    if (!confirm("确认删除该联系记录？")) return;
                                    const res = await customerApi.deleteContactHistory(customer!.id, h.id);
                                    if (res.success) {
                                      setHistory((prev) => prev.filter((x) => x.id !== h.id));
                                      toast.success("联系记录已删除");
                                    } else {
                                      toast.error(res.message || "删除失败");
                                    }
                                  }}
                                  className="p-1.5 rounded hover:bg-white/5 transition-colors"
                                >
                                  <Trash2 size={13} style={{ color: "#C95745" }} />
                                </button>
                              </div>
                            </div>
                            <p className="text-xs leading-relaxed" style={{ color: "rgba(200,216,232,0.7)", fontFamily: "Noto Sans SC" }}>{h.content}</p>
                            {(h.contactPerson || h.result) && (
                              <div className="flex flex-wrap gap-4 mt-2 text-xs" style={{ color: "rgba(200,216,232,0.45)", fontFamily: "Noto Sans SC" }}>
                                {h.contactPerson && <span>联系人：{h.contactPerson}</span>}
                                {h.result && <span>结果：{h.result}</span>}
                              </div>
                            )}
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}

            {/* ── 操作日志 ── */}
            {activeTab === "logs" && (
              <div>
                <div className="flex items-center justify-between mb-5">
                  <h3 className="text-sm font-semibold" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>客户日志</h3>
                </div>
                {logsLoading ? (
                  <div className="flex items-center justify-center py-12">
                    <Loader2 size={20} className="animate-spin" style={{ color: "#00D4FF" }} />
                    <span className="ml-2 text-xs" style={{ color: "rgba(200,216,232,0.5)", fontFamily: "Noto Sans SC" }}>加载日志...</span>
                  </div>
                ) : (
                  <div className="space-y-6">
                    {/* 操作日志 */}
                    <div>
                      <h4 className="text-xs font-semibold mb-3 flex items-center gap-2" style={{ color: "#00D4FF", fontFamily: "Noto Sans SC" }}>
                        <Activity size={13} />操作日志
                      </h4>
                      {operationLogs.length === 0 ? (
                        <div className="text-center py-8" style={{ color: "rgba(200,216,232,0.3)", fontFamily: "Noto Sans SC" }}>
                          <Activity size={28} className="mx-auto mb-2 opacity-30" />
                          <p className="text-xs">暂无操作记录</p>
                        </div>
                      ) : (
                        <div className="space-y-2">
                          {operationLogs.map((log) => (
                            <div key={log.id} className="flex items-start gap-3 p-3 rounded-lg"
                              style={{ background: "#162233", border: "1px solid rgba(255,255,255,0.06)" }}>
                              <div className="w-1.5 h-1.5 rounded-full mt-1.5 flex-shrink-0" style={{ background: "#00D4FF" }} />
                              <div className="flex-1 min-w-0">
                                <div className="flex items-center justify-between gap-2">
                                  <span className="text-xs font-medium" style={{ color: "#E0F4FF", fontFamily: "Noto Sans SC" }}>{log.operationDesc}</span>
                                  <span className="text-xs flex-shrink-0" style={{ color: "rgba(200,216,232,0.4)", fontFamily: "Space Mono" }}>
                                    {new Date(log.operationTime).toLocaleString("zh-CN")}
                                  </span>
                                </div>
                                <div className="flex items-center gap-3 mt-1">
                                  <span className="text-xs px-1.5 py-0.5 rounded" style={{ background: "rgba(0,212,255,0.08)", color: "#00D4FF", border: "1px solid rgba(0,212,255,0.2)", fontFamily: "Noto Sans SC" }}>
                                    {log.operationType}
                                  </span>
                                  <span className="text-xs" style={{ color: "rgba(200,216,232,0.45)", fontFamily: "Noto Sans SC" }}>
                                    操作人：{log.operatorUserName || "系统"}
                                  </span>
                                </div>
                                {log.remark && (
                                  <p className="text-xs mt-1" style={{ color: "rgba(200,216,232,0.5)", fontFamily: "Noto Sans SC" }}>{log.remark}</p>
                                )}
                              </div>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                    {/* 变更日志 */}
                    <div>
                      <h4 className="text-xs font-semibold mb-3 flex items-center gap-2" style={{ color: "#A78BFA", fontFamily: "Noto Sans SC" }}>
                        <FileText size={13} />字段变更日志
                      </h4>
                      {changeLogs.length === 0 ? (
                        <div className="text-center py-8" style={{ color: "rgba(200,216,232,0.3)", fontFamily: "Noto Sans SC" }}>
                          <FileText size={28} className="mx-auto mb-2 opacity-30" />
                          <p className="text-xs">暂无字段变更记录</p>
                        </div>
                      ) : (
                        <div className="space-y-2">
                          {changeLogs.map((log) => (
                            <div key={log.id} className="p-3 rounded-lg"
                              style={{ background: "#162233", border: "1px solid rgba(255,255,255,0.06)" }}>
                              <div className="flex items-center justify-between gap-2 mb-2">
                                <span className="text-xs font-medium px-1.5 py-0.5 rounded" style={{ background: "rgba(167,139,250,0.1)", color: "#A78BFA", border: "1px solid rgba(167,139,250,0.2)", fontFamily: "Noto Sans SC" }}>
                                  {log.fieldLabel}
                                </span>
                                <span className="text-xs" style={{ color: "rgba(200,216,232,0.4)", fontFamily: "Space Mono" }}>
                                  {new Date(log.changedAt).toLocaleString("zh-CN")}
                                </span>
                              </div>
                              <div className="flex items-center gap-2 text-xs" style={{ fontFamily: "Noto Sans SC" }}>
                                <span className="px-2 py-1 rounded" style={{ background: "rgba(201,87,69,0.08)", color: "rgba(201,87,69,0.8)", border: "1px solid rgba(201,87,69,0.2)" }}>
                                  {log.oldValue || "（空）"}
                                </span>
                                <span style={{ color: "rgba(200,216,232,0.3)" }}>→</span>
                                <span className="px-2 py-1 rounded" style={{ background: "rgba(0,212,255,0.08)", color: "#00D4FF", border: "1px solid rgba(0,212,255,0.2)" }}>
                                  {log.newValue || "（空）"}
                                </span>
                              </div>
                              <p className="text-xs mt-1.5" style={{ color: "rgba(200,216,232,0.4)", fontFamily: "Noto Sans SC" }}>
                                变更人：{log.changedByUserName || "系统"}
                              </p>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  </div>
                )}
              </div>
            )}

          </div>
        </div>
      </div>
    </DashboardLayout>
  );
}
