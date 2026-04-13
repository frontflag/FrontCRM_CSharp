<template>
  <el-dialog
    v-model="visibleInner"
    title="通过 Excel 导入客户"
    width="560px"
    destroy-on-close
    class="customer-import-dialog"
    @closed="onClosed"
  >
    <div class="import-body">
      <p class="hint">
        请先下载模板，按「客户」「联系人」两个工作表填写。客户与联系人通过「序号 / 客户序号」关联。
        「结算币别」可留空；填写时须为 RMB、USD、HKD、EUR 之一（大小写均可）。「官方网址」将写入备注前缀（官网：…），可与原备注合并。
        正式环境请删除或覆盖模板中的示例行后再导入。
      </p>
      <div class="actions-row">
        <button type="button" class="btn-template" @click="downloadTemplate">下载 Excel 模板</button>
        <label class="btn-upload">
          <input
            ref="fileInputRef"
            type="file"
            accept=".xlsx,.xls"
            class="file-input"
            @change="onFileChange"
          />
          选择 Excel 文件
        </label>
      </div>

      <div v-if="fileName" class="file-name">已选：{{ fileName }}</div>

      <div v-if="parseErrors.length" class="error-box">
        <div class="error-title">解析问题</div>
        <ul>
          <li v-for="(e, i) in parseErrors" :key="i">{{ e }}</li>
        </ul>
      </div>

      <div v-if="previewReady" class="preview-box">
        <div class="preview-row">
          <span>本次将导入客户</span>
          <strong>{{ customerCount }}</strong>
          <span>家</span>
        </div>
        <div class="preview-row">
          <span>客户联系人</span>
          <strong>{{ contactCount }}</strong>
          <span>条</span>
        </div>
      </div>
    </div>

    <template #footer>
      <el-button @click="visibleInner = false">取消</el-button>
      <el-button
        type="primary"
        :disabled="!canSubmit"
        :loading="submitting"
        @click="confirmAndSubmit"
      >
        确认导入
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import * as XLSX from 'xlsx';
import { ElMessageBox, ElNotification } from 'element-plus';
import { customerApi } from '@/api/customer';
import { industryCellToStorageLabel } from '@/utils/customerIndustryStorage';
import { CurrencyCode } from '@/constants/currency';

const props = defineProps<{
  modelValue: boolean;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', v: boolean): void;
  (e: 'success'): void;
}>();

const visibleInner = computed({
  get: () => props.modelValue,
  set: (v: boolean) => emit('update:modelValue', v)
});

const fileInputRef = ref<HTMLInputElement | null>(null);
const fileName = ref('');
const parseErrors = ref<string[]>([]);
const batchPayload = ref<{
  items: Array<{ customer: Record<string, unknown>; contacts: Array<Record<string, unknown>> }>;
} | null>(null);
const customerCount = ref(0);
const contactCount = ref(0);
const submitting = ref(false);

const previewReady = computed(
  () =>
    !!batchPayload.value &&
    batchPayload.value.items.length > 0 &&
    parseErrors.value.length === 0
);

const canSubmit = computed(() => previewReady.value && !submitting.value);

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return;
    resetState();
  }
);

function resetState() {
  fileName.value = '';
  parseErrors.value = [];
  batchPayload.value = null;
  customerCount.value = 0;
  contactCount.value = 0;
  if (fileInputRef.value) fileInputRef.value.value = '';
}

function onClosed() {
  resetState();
}

function normalizeHeaderKey(k: string): string {
  return k.replace(/（必填）/g, '').replace(/\([^)]*\)/g, '').trim();
}

function getCell(row: Record<string, unknown>, ...candidates: string[]): string {
  const keys = Object.keys(row);
  for (const cand of candidates) {
    const hit = keys.find((k) => normalizeHeaderKey(k) === cand || normalizeHeaderKey(k).includes(cand));
    if (hit) {
      const v = row[hit];
      if (v == null) return '';
      const s = String(v).trim();
      return s;
    }
  }
  return '';
}

function parseSeq(v: string): number | null {
  if (!v) return null;
  const n = parseInt(String(v).replace(/\s/g, ''), 10);
  return Number.isFinite(n) && n > 0 ? n : null;
}

function parseCustomerType(raw: string): number {
  const s = raw.trim();
  if (!s) return 2;
  const n = parseInt(s, 10);
  if (n >= 1 && n <= 11) return n;
  const map: Record<string, number> = {
    OEM: 1,
    ODM: 2,
    终端用户: 3,
    终端: 3,
    IDH: 4,
    贸易商: 5,
    代理商: 6,
    EMS: 7,
    非行业: 8,
    科研机构: 9,
    供应链: 10,
    原厂: 11
  };
  return map[s] ?? 2;
}

function parseCustomerLevel(raw: string): string {
  const s = raw.trim().toUpperCase();
  if (!s) return 'B';
  const ok = ['D', 'C', 'B', 'BPO', 'VIP', 'VPO'];
  return ok.includes(s) ? s : 'B';
}

/** 导入「行业」列：英文 code / 中文别名 / 标准中文 label → 与库表一致的中文 label */
function parseIndustry(raw: string): string {
  return industryCellToStorageLabel(raw);
}

function parseDefaultFlag(raw: string): boolean {
  const s = raw.trim().toLowerCase();
  return s === '是' || s === 'y' || s === 'yes' || s === '1' || s === 'true';
}

/** 与后端 CurrencyCode 一致；空串表示不填，创建时默认 RMB */
function parseSettlementCurrency(raw: string): { ok: true; value?: number } | { ok: false; message: string } {
  const s = raw.trim().toUpperCase();
  if (!s) return { ok: true, value: undefined };
  const map: Record<string, number> = {
    RMB: CurrencyCode.RMB,
    USD: CurrencyCode.USD,
    EUR: CurrencyCode.EUR,
    HKD: CurrencyCode.HKD
  };
  if (map[s] != null) return { ok: true, value: map[s] };
  return {
    ok: false,
    message: `结算币别须为 RMB、USD、HKD、EUR 之一，当前为「${raw.trim() || '(空)'}」`
  };
}

/** Excel 列宽截断时可能只有「联系人姓」，与「联系人姓名」等价识别 */
function getContactPersonName(row: Record<string, unknown>): string {
  return getCell(row, '联系人姓名', '联系人姓');
}

function downloadTemplate() {
  const customerHeaders = [
    '序号',
    '客户名称(必填)',
    '客户简称',
    '客户类型',
    '客户级别',
    '行业',
    '省',
    '市',
    '备注',
    '统一社会信用代码',
    '结算币别',
    '官方网址'
  ];
  const contactHeaders = [
    '客户序号(必填)',
    '联系人姓名(必填)',
    '手机',
    '固定电话',
    '部门',
    '职位',
    '邮箱',
    '是否默认（是/否）'
  ];
  const ws1 = XLSX.utils.aoa_to_sheet([
    customerHeaders,
    [
      '1',
      '示例科技有限公司',
      '示例科技',
      'ODM',
      'B',
      '消费电子',
      '广东省',
      '深圳市',
      '可删除本行后填写真实数据',
      '',
      'RMB',
      'https://www.example.com'
    ]
  ]);
  const ws2 = XLSX.utils.aoa_to_sheet([
    contactHeaders,
    ['1', '张三', '13800138000', '', '销售部', '经理', '', '是']
  ]);
  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws1, '客户');
  XLSX.utils.book_append_sheet(wb, ws2, '联系人');
  XLSX.writeFile(wb, '客户导入模板.xlsx');
}

function onFileChange(ev: Event) {
  const input = ev.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file) return;
  fileName.value = file.name;
  parseErrors.value = [];
  batchPayload.value = null;
  customerCount.value = 0;
  contactCount.value = 0;

  const reader = new FileReader();
  reader.onload = (e) => {
    try {
      const data = new Uint8Array(e.target?.result as ArrayBuffer);
      const wb = XLSX.read(data, { type: 'array' });
      const nameCustomer = wb.SheetNames.find((n) => n.includes('客户') && !n.includes('联系')) || wb.SheetNames[0];
      const nameContact = wb.SheetNames.find((n) => n.includes('联系人'));

      if (!nameCustomer) {
        parseErrors.value = ['工作簿中未找到「客户」表'];
        return;
      }
      const shC = wb.Sheets[nameCustomer];
      const rowsC = XLSX.utils.sheet_to_json<Record<string, unknown>>(shC, { defval: '', raw: false });
      const shCt = nameContact ? wb.Sheets[nameContact] : undefined;
      const rowsCt = shCt
        ? XLSX.utils.sheet_to_json<Record<string, unknown>>(shCt, { defval: '', raw: false })
        : [];

      const errors: string[] = [];
      const bySeq = new Map<
        number,
        {
          seq: number;
          name: string;
          shortName: string;
          type: number;
          level: string;
          industry: string;
          province: string;
          city: string;
          remark: string;
          credit: string;
          tradeCurrency?: number;
        }
      >();

      for (let i = 0; i < rowsC.length; i++) {
        const row = rowsC[i];
        const seqStr = getCell(row, '序号');
        const name = getCell(row, '客户名称');
        if (!seqStr && !name) continue;
        const seq = parseSeq(seqStr);
        if (seq == null) {
          errors.push(`客户表第 ${i + 2} 行：序号无效或为空`);
          continue;
        }
        if (!name) {
          errors.push(`客户表第 ${i + 2} 行：客户名称不能为空`);
          continue;
        }
        if (bySeq.has(seq)) {
          errors.push(`客户表：序号 ${seq} 重复`);
          continue;
        }
        const curRaw = getCell(row, '结算币别', '结算货币');
        const curParsed = parseSettlementCurrency(curRaw);
        if (!curParsed.ok) {
          errors.push(`客户表第 ${i + 2} 行：${curParsed.message}`);
          continue;
        }
        const typeRaw = getCell(row, '客户类型');
        const levelRaw = getCell(row, '客户级别');
        const baseRemark = getCell(row, '备注');
        const website = getCell(row, '官方网址', '网址', '网站');
        let remark = baseRemark;
        if (website) {
          const prefix = `官网：${website}`;
          remark = baseRemark ? `${prefix}\n${baseRemark}` : prefix;
        }
        bySeq.set(seq, {
          seq,
          name,
          shortName: getCell(row, '客户简称'),
          type: parseCustomerType(typeRaw),
          level: parseCustomerLevel(levelRaw),
          industry: parseIndustry(getCell(row, '行业')),
          province: getCell(row, '省'),
          city: getCell(row, '市'),
          remark,
          credit: getCell(row, '统一社会信用代码'),
          tradeCurrency: curParsed.value
        });
      }

      if (bySeq.size === 0) {
        errors.push(
          '未解析到有效客户行：请确认「客户」表第 1 行为表头、从第 2 行起填写数据，且「序号」「客户名称」列有值（列名可与模板略有差异，但不要整列为空）。'
        );
      }

      const contactsBySeq = new Map<number, Array<Record<string, unknown>>>();

      for (let i = 0; i < rowsCt.length; i++) {
        const row = rowsCt[i];
        const cseqStr = getCell(row, '客户序号');
        const cname = getContactPersonName(row);
        if (!cseqStr && !cname) continue;
        const cseq = parseSeq(cseqStr);
        if (cseq == null) {
          errors.push(`联系人表第 ${i + 2} 行：客户序号无效`);
          continue;
        }
        if (!bySeq.has(cseq)) {
          errors.push(
            `联系人表第 ${i + 2} 行：客户序号 ${cseq} 在「客户」表中不存在（请核对两表序号一致，且客户表该行未被空行跳过）。`
          );
          continue;
        }
        if (!getContactPersonName(row)) {
          errors.push(`联系人表第 ${i + 2} 行：联系人姓名不能为空`);
          continue;
        }
        const contact = {
          contactName: getContactPersonName(row),
          mobile: getCell(row, '手机'),
          phone: getCell(row, '固定电话'),
          department: getCell(row, '部门'),
          position: getCell(row, '职位'),
          email: getCell(row, '邮箱'),
          isDefault: parseDefaultFlag(getCell(row, '是否默认'))
        };
        if (!contactsBySeq.has(cseq)) contactsBySeq.set(cseq, []);
        contactsBySeq.get(cseq)!.push(contact);
      }

      const sortedSeq = [...bySeq.keys()].sort((a, b) => a - b);
      const items: Array<{ customer: Record<string, unknown>; contacts: Array<Record<string, unknown>> }> = [];

      for (const seq of sortedSeq) {
        const c = bySeq.get(seq)!;
        const customerPayload: Record<string, unknown> = {
          customerName: c.name,
          customerShortName: c.shortName || undefined,
          customerType: c.type,
          customerLevel: c.level,
          industry: c.industry,
          province: c.province || undefined,
          city: c.city || undefined,
          remarks: c.remark || undefined,
          unifiedSocialCreditCode: c.credit || undefined
        };
        if (c.tradeCurrency != null) customerPayload.currency = c.tradeCurrency;
        items.push({
          customer: customerPayload,
          contacts: contactsBySeq.get(seq) || []
        });
      }

      parseErrors.value = errors;
      if (errors.length > 0) {
        batchPayload.value = null;
        customerCount.value = 0;
        contactCount.value = 0;
        return;
      }
      batchPayload.value = { items };
      customerCount.value = items.length;
      contactCount.value = items.reduce((n, it) => n + it.contacts.length, 0);
    } catch (err: unknown) {
      parseErrors.value = [err instanceof Error ? err.message : '解析 Excel 失败'];
    }
  };
  reader.readAsArrayBuffer(file);
}

async function confirmAndSubmit() {
  if (!batchPayload.value?.items.length) return;
  try {
    await ElMessageBox.confirm(
      `本次将导入客户 ${customerCount.value} 家，客户联系人 ${contactCount.value} 条。确认提交到系统吗？`,
      '确认导入',
      {
        type: 'warning',
        confirmButtonText: '确认上传',
        cancelButtonText: '取消'
      }
    );
  } catch {
    return;
  }

  submitting.value = true;
  try {
    const res = await customerApi.importCustomersBatch(batchPayload.value);
    ElNotification.success({
      title: '导入完成',
      message: `成功 ${res.successCount} 条，失败 ${res.failCount} 条`
    });
    visibleInner.value = false;
    emit('success');
  } catch (e: unknown) {
    ElNotification.error({
      title: '导入失败',
      message: e instanceof Error ? e.message : '请求失败'
    });
  } finally {
    submitting.value = false;
  }
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.hint {
  font-size: 13px;
  color: $text-muted;
  line-height: 1.5;
  margin: 0 0 16px;
}

.actions-row {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  align-items: center;
  margin-bottom: 12px;
}

.btn-template,
.btn-upload {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  cursor: pointer;
  border: 1px solid $border-panel;
  background: $layer-2;
  color: $text-primary;
}

.btn-upload {
  position: relative;
  overflow: hidden;
  border-color: rgba(0, 212, 255, 0.35);
  color: $cyan-primary;
}

.file-input {
  position: absolute;
  inset: 0;
  opacity: 0;
  cursor: pointer;
}

.file-name {
  font-size: 12px;
  color: $text-muted;
  margin-bottom: 12px;
}

.error-box {
  background: rgba(255, 80, 80, 0.08);
  border: 1px solid rgba(255, 80, 80, 0.25);
  border-radius: $border-radius-md;
  padding: 10px 12px;
  margin-bottom: 12px;
  font-size: 12px;
  color: $text-secondary;

  .error-title {
    font-weight: 600;
    margin-bottom: 6px;
    color: #f56c6c;
  }

  ul {
    margin: 0;
    padding-left: 18px;
  }
}

.preview-box {
  background: rgba(0, 212, 255, 0.06);
  border: 1px solid rgba(0, 212, 255, 0.2);
  border-radius: $border-radius-md;
  padding: 12px 14px;
  font-size: 13px;
  color: $text-primary;

  .preview-row {
    display: flex;
    align-items: baseline;
    gap: 6px;
    margin-bottom: 6px;
    &:last-child {
      margin-bottom: 0;
    }
  }

  strong {
    font-size: 18px;
    color: $cyan-primary;
  }
}
</style>
