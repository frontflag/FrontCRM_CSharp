import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { dictionaryApi } from '@/api/dictionary';
import { buildCustomerFormDictFallback } from '@/constants/customerDictFallback';

const K = {
  type: 'CustomerType',
  level: 'CustomerLevel',
  industry: 'CustomerIndustry',
  taxRate: 'CustomerTaxRate',
  invoice: 'CustomerInvoiceType'
} as const;

export const useCustomerDictStore = defineStore('customerDict', () => {
  const categories = ref<Record<string, { code: string; label: string }[]>>({});
  const mergedExtras = ref<Record<string, { code: string; label: string }[]>>({});
  const loaded = ref(false);
  let loading: Promise<void> | null = null;

  function getList(cat: string) {
    return categories.value[cat] ?? [];
  }

  function mergedList(cat: string) {
    const base = getList(cat);
    const extra = mergedExtras.value[cat] ?? [];
    const codes = new Set(base.map((x) => x.code));
    const extraOnly = extra.filter((e) => !codes.has(e.code));
    return [...extraOnly, ...base];
  }

  async function ensureLoaded() {
    if (loaded.value) return;
    if (loading) return loading;
    loading = (async () => {
      try {
        const data = await dictionaryApi.fetchCustomerForm();
        categories.value = { ...data };
      } catch {
        categories.value = buildCustomerFormDictFallback();
      }
      loaded.value = true;
    })();
    try {
      await loading;
    } finally {
      loading = null;
    }
  }

  async function ensureExtraOption(category: string, code: string | number | null | undefined) {
    if (code == null || code === '') return;
    const s = String(code);
    const base = getList(category);
    if (base.some((x) => x.code === s)) return;
    const extras = mergedExtras.value[category] ?? [];
    if (extras.some((x) => x.code === s)) return;
    try {
      const row = await dictionaryApi.lookup(category, s);
      if (row?.code) {
        mergedExtras.value = {
          ...mergedExtras.value,
          [category]: [...extras, { code: row.code, label: row.label }]
        };
      }
    } catch {
      /* 忽略 */
    }
  }

  async function hydrateCustomerEditForm(data: {
    customerType?: number | null | undefined;
    customerLevel?: string | null | undefined;
    industry?: string | null | undefined;
    taxRate?: number | string | null | undefined;
    invoiceType?: number | string | null | undefined;
  }) {
    await ensureLoaded();
    await ensureExtraOption(K.type, data.customerType);
    await ensureExtraOption(K.level, data.customerLevel);
    await ensureExtraOption(K.industry, data.industry);
    await ensureExtraOption(K.taxRate, data.taxRate);
    await ensureExtraOption(K.invoice, data.invoiceType);
  }

  const typeSelectOptions = computed(() =>
    mergedList(K.type)
      .map((x) => {
        const n = parseInt(x.code, 10);
        return Number.isFinite(n) ? { value: n, label: x.label } : null;
      })
      .filter((x): x is { value: number; label: string } => x !== null)
  );

  const levelStringOptions = computed(() =>
    mergedList(K.level).map((x) => ({ value: x.code, label: x.label }))
  );

  const industryOptions = computed(() =>
    mergedList(K.industry).map((x) => ({ value: x.code, label: x.label }))
  );

  const taxRateOptions = computed(() =>
    mergedList(K.taxRate)
      .map((x) => {
        const n = parseFloat(x.code);
        return Number.isFinite(n) ? { value: n, label: x.label } : null;
      })
      .filter((x): x is { value: number; label: string } => x !== null)
  );

  const invoiceTypeOptions = computed(() =>
    mergedList(K.invoice)
      .map((x) => {
        const n = parseInt(x.code, 10);
        return Number.isFinite(n) ? { value: n, label: x.label } : null;
      })
      .filter((x): x is { value: number; label: string } => x !== null)
  );

  function labelFor(category: string, code: string | null | undefined) {
    if (code == null || code === '') return '';
    const row = getList(category).find((x) => x.code === code);
    if (row) return row.label;
    const ex = (mergedExtras.value[category] ?? []).find((x) => x.code === code);
    return ex?.label ?? '';
  }

  function typeLabel(type: number | null | undefined) {
    if (type == null || type === 0) return '--';
    const v = labelFor(K.type, String(type));
    return v || String(type);
  }

  function levelLabel(level: string | null | undefined) {
    if (!level) return '--';
    return labelFor(K.level, level) || level;
  }

  function industryLabel(code: string | null | undefined) {
    if (!code) return '--';
    return labelFor(K.industry, code) || code;
  }

  function taxRateLabel(rate: number | null | undefined) {
    if (rate == null || Number.isNaN(rate)) return '--';
    const key = String(rate);
    const exact = labelFor(K.taxRate, key);
    if (exact) return exact;
    const rounded = rate.toFixed(2).replace(/\.?0+$/, '');
    const hit = labelFor(K.taxRate, rounded);
    if (hit) return hit;
    return `${rate}%`;
  }

  function invoiceTypeLabel(v: number | null | undefined) {
    if (v == null) return '--';
    return labelFor(K.invoice, String(v)) || String(v);
  }

  return {
    loaded,
    ensureLoaded,
    ensureExtraOption,
    hydrateCustomerEditForm,
    typeSelectOptions,
    levelStringOptions,
    industryOptions,
    taxRateOptions,
    invoiceTypeOptions,
    labelFor,
    typeLabel,
    levelLabel,
    industryLabel,
    taxRateLabel,
    invoiceTypeLabel,
    getList
  };
});
