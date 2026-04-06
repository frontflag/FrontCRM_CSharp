import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { dictionaryApi } from '@/api/dictionary';
import { buildVendorFormDictFallback } from '@/constants/vendorDictFallback';

const K = {
  industry: 'VendorIndustry',
  level: 'VendorLevel',
  identity: 'VendorIdentity',
  payment: 'VendorPaymentMethod'
} as const;

export const useVendorDictStore = defineStore('vendorDict', () => {
  const categories = ref<Record<string, { code: string; label: string }[]>>({});
  /** 已禁用但当前记录仍持有的编码，合并进选项以便 el-select 回显 */
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
        const data = await dictionaryApi.fetchVendorForm();
        categories.value = { ...data };
      } catch {
        categories.value = buildVendorFormDictFallback();
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

  async function hydrateVendorEditForm(data: {
    industry?: string;
    level?: number | null | undefined;
    credit?: number | null | undefined;
    paymentMethod?: string;
  }) {
    await ensureLoaded();
    await ensureExtraOption(K.industry, data.industry);
    await ensureExtraOption(K.level, data.level);
    await ensureExtraOption(K.identity, data.credit);
    await ensureExtraOption(K.payment, data.paymentMethod);
  }

  const industryOptions = computed(() =>
    mergedList(K.industry).map((x) => ({ value: x.code, label: x.label }))
  );

  const levelSelectOptions = computed(() =>
    mergedList(K.level)
      .map((x) => {
        const n = parseInt(x.code, 10);
        return Number.isFinite(n) ? { value: n, label: x.label } : null;
      })
      .filter((x): x is { value: number; label: string } => x !== null)
  );

  const identitySelectOptions = computed(() =>
    mergedList(K.identity)
      .map((x) => {
        const n = parseInt(x.code, 10);
        return Number.isFinite(n) ? { value: n, label: x.label } : null;
      })
      .filter((x): x is { value: number; label: string } => x !== null)
  );

  const paymentMethodOptions = computed(() =>
    mergedList(K.payment).map((x) => ({ value: x.code, label: x.label }))
  );

  function labelFor(category: string, code: string | null | undefined) {
    if (code == null || code === '') return '';
    const row = getList(category).find((x) => x.code === code);
    if (row) return row.label;
    const ex = (mergedExtras.value[category] ?? []).find((x) => x.code === code);
    return ex?.label ?? '';
  }

  function levelLabel(level: number | null | undefined) {
    if (level == null || level === 0) return '--';
    return labelFor(K.level, String(level)) || String(level);
  }

  function identityLabel(id: number | null | undefined) {
    if (id == null || id === 0) return '--';
    const v = labelFor(K.identity, String(id));
    return v || String(id);
  }

  function industryLabel(code: string | null | undefined) {
    if (!code) return '--';
    return labelFor(K.industry, code) || code;
  }

  function paymentLabel(code: string | null | undefined) {
    if (!code) return '--';
    return labelFor(K.payment, code) || code;
  }

  return {
    loaded,
    ensureLoaded,
    ensureExtraOption,
    hydrateVendorEditForm,
    industryOptions,
    levelSelectOptions,
    identitySelectOptions,
    paymentMethodOptions,
    labelFor,
    levelLabel,
    identityLabel,
    industryLabel,
    paymentLabel,
    getList
  };
});
