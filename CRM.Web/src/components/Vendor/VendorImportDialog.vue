<template>
  <el-dialog
    v-model="visibleInner"
    title="通过 Excel 导入供应商"
    width="560px"
    destroy-on-close
    class="vendor-import-dialog"
    @closed="onClosed"
  >
    <div class="import-body">
      <p class="hint">
        请先下载模板，按「供应商」「联系人」两个工作表填写。供应商与联系人通过「序号 / 供应商序号」关联。
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
          <span>本次将导入供应商</span>
          <strong>{{ vendorCount }}</strong>
          <span>家</span>
        </div>
        <div class="preview-row">
          <span>供应商联系人</span>
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
import { vendorApi } from '@/api/vendor';
import { VENDOR_LEVEL_OPTIONS, VENDOR_IDENTITY_OPTIONS } from '@/constants/vendorEnums';
import { VENDOR_INDUSTRY_FILTER_VALUES } from '@/constants/vendorIndustry';

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
  items: Array<{ vendor: Record<string, unknown>; contacts: Array<Record<string, unknown>> }>;
} | null>(null);
const vendorCount = ref(0);
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
  vendorCount.value = 0;
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
      return String(v).trim();
    }
  }
  return '';
}

function parseSeq(v: string): number | null {
  if (!v) return null;
  const n = parseInt(String(v).replace(/\s/g, ''), 10);
  return Number.isFinite(n) && n > 0 ? n : null;
}

function parseVendorLevel(raw: string): number {
  const s = raw.trim();
  if (!s) return 10;
  const n = parseInt(s, 10);
  if (n >= 1 && n <= 13) return n;
  const opt = VENDOR_LEVEL_OPTIONS.find((o) => o.label === s);
  return opt?.value ?? 10;
}

function parseVendorCredit(raw: string): number {
  const s = raw.trim();
  if (!s) return 1;
  const n = parseInt(s, 10);
  if (n >= 1 && n <= 10) return n;
  const opt = VENDOR_IDENTITY_OPTIONS.find((o) => o.label === s);
  return opt?.value ?? 1;
}

function parseVendorIndustry(raw: string): string {
  const s = raw.trim();
  if (!s) return 'Semiconductors';
  const map: Record<string, string> = {
    IGUS: 'IGUS',
    'LED照明、光电设备及显示器': 'LedLightingOptoDisplay',
    半导体: 'Semiconductors',
    '工具及设备': 'ToolsEquipment',
    工控: 'IndustrialControl',
    '开发套件和工具': 'DevKitsTools',
    '显示市场': 'DisplayMarket',
    '机电编码器': 'MechatronicEncoders',
    '测试和测量': 'TestMeasurement',
    '热管理': 'ThermalManagement',
    '电线及电缆': 'WiresCables',
    '电路保护': 'CircuitProtection',
    '结构件': 'StructuralParts',
    '网络通讯器件': 'NetworkCommDevices',
    '计算机外设、机电': 'ComputerPeripheralsMech',
    '负极、电源': 'CathodePower',
    '电子/半导体': 'Semiconductors',
    '机械/设备': 'ToolsEquipment',
    '化工/材料': 'IndustrialControl',
    '纺织/服装': 'StructuralParts',
    '食品/农业': 'ToolsEquipment',
    '建筑/工程': 'StructuralParts',
    '贸易/零售': 'ElectronicComponentsTrading',
    '科技/IT': 'ComputerPeripheralsMech',
    '医疗/健康': 'ToolsEquipment',
    其他: 'Semiconductors'
  };
  const codes = VENDOR_INDUSTRY_FILTER_VALUES as readonly string[];
  return map[s] || (codes.includes(s) ? s : 'Semiconductors');
}

function parseMainFlag(raw: string): boolean {
  const s = raw.trim().toLowerCase();
  return s === '是' || s === 'y' || s === 'yes' || s === '1' || s === 'true';
}

function getVendorContactName(row: Record<string, unknown>): string {
  return getCell(row, '联系人姓名', '联系人姓');
}

function downloadTemplate() {
  const vendorHeaders = [
    '序号',
    '供应商名称(必填)',
    '供应商简称',
    '等级',
    '身份',
    '行业',
    '办公地址',
    '备注',
    '统一社会信用代码'
  ];
  const contactHeaders = [
    '供应商序号(必填)',
    '联系人姓名(必填)',
    '手机',
    '固定电话',
    '部门',
    '职位',
    '邮箱',
    '是否主联系人（是/否）'
  ];
  const ws1 = XLSX.utils.aoa_to_sheet([
    vendorHeaders,
    [
      '1',
      '示例供应商有限公司',
      '示例供应商',
      'A',
      '原厂',
      '半导体',
      '深圳市南山区示例路1号',
      '可删除本行后填写真实数据',
      ''
    ]
  ]);
  const ws2 = XLSX.utils.aoa_to_sheet([
    contactHeaders,
    ['1', '李四', '13800138000', '', '采购部', '主管', '', '是']
  ]);
  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws1, '供应商');
  XLSX.utils.book_append_sheet(wb, ws2, '联系人');
  XLSX.writeFile(wb, '供应商导入模板.xlsx');
}

function onFileChange(ev: Event) {
  const input = ev.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file) return;
  fileName.value = file.name;
  parseErrors.value = [];
  batchPayload.value = null;
  vendorCount.value = 0;
  contactCount.value = 0;

  const reader = new FileReader();
  reader.onload = (e) => {
    try {
      const data = new Uint8Array(e.target?.result as ArrayBuffer);
      const wb = XLSX.read(data, { type: 'array' });
      const nameVendor =
        wb.SheetNames.find((n) => n.includes('供应商') && !n.includes('联系')) || wb.SheetNames[0];
      const nameContact = wb.SheetNames.find((n) => n.includes('联系人'));

      if (!nameVendor) {
        parseErrors.value = ['工作簿中未找到「供应商」表'];
        return;
      }
      const shV = wb.Sheets[nameVendor];
      const rowsV = XLSX.utils.sheet_to_json<Record<string, unknown>>(shV, { defval: '', raw: false });
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
          level: number;
          credit: number;
          industry: string;
          officeAddress: string;
          remark: string;
          creditCode: string;
        }
      >();

      for (let i = 0; i < rowsV.length; i++) {
        const row = rowsV[i];
        const seqStr = getCell(row, '序号');
        const name = getCell(row, '供应商名称');
        if (!seqStr && !name) continue;
        const seq = parseSeq(seqStr);
        if (seq == null) {
          errors.push(`供应商表第 ${i + 2} 行：序号无效或为空`);
          continue;
        }
        if (!name) {
          errors.push(`供应商表第 ${i + 2} 行：供应商名称不能为空`);
          continue;
        }
        if (bySeq.has(seq)) {
          errors.push(`供应商表：序号 ${seq} 重复`);
          continue;
        }
        bySeq.set(seq, {
          seq,
          name,
          shortName: getCell(row, '供应商简称'),
          level: parseVendorLevel(getCell(row, '等级')),
          credit: parseVendorCredit(getCell(row, '身份')),
          industry: parseVendorIndustry(getCell(row, '行业')),
          officeAddress: getCell(row, '办公地址'),
          remark: getCell(row, '备注'),
          creditCode: getCell(row, '统一社会信用代码')
        });
      }

      if (bySeq.size === 0) {
        errors.push(
          '未解析到有效供应商行：请确认「供应商」表第 1 行为表头、从第 2 行起填写数据，且「序号」「供应商名称」列有值。'
        );
      }

      const contactsBySeq = new Map<number, Array<Record<string, unknown>>>();

      for (let i = 0; i < rowsCt.length; i++) {
        const row = rowsCt[i];
        const cseqStr = getCell(row, '供应商序号');
        const cname = getVendorContactName(row);
        if (!cseqStr && !cname) continue;
        const cseq = parseSeq(cseqStr);
        if (cseq == null) {
          errors.push(`联系人表第 ${i + 2} 行：供应商序号无效`);
          continue;
        }
        if (!bySeq.has(cseq)) {
          errors.push(
            `联系人表第 ${i + 2} 行：供应商序号 ${cseq} 在「供应商」表中不存在（请核对两表序号一致）。`
          );
          continue;
        }
        if (!getVendorContactName(row)) {
          errors.push(`联系人表第 ${i + 2} 行：联系人姓名不能为空`);
          continue;
        }
        const contact = {
          cName: getVendorContactName(row),
          mobile: getCell(row, '手机'),
          tel: getCell(row, '固定电话'),
          department: getCell(row, '部门'),
          title: getCell(row, '职位'),
          email: getCell(row, '邮箱'),
          isMain: parseMainFlag(getCell(row, '是否主联系人'))
        };
        if (!contactsBySeq.has(cseq)) contactsBySeq.set(cseq, []);
        contactsBySeq.get(cseq)!.push(contact);
      }

      const sortedSeq = [...bySeq.keys()].sort((a, b) => a - b);
      const items: Array<{ vendor: Record<string, unknown>; contacts: Array<Record<string, unknown>> }> = [];

      for (const seq of sortedSeq) {
        const v = bySeq.get(seq)!;
        items.push({
          vendor: {
            name: v.name,
            nickName: v.shortName || undefined,
            level: v.level,
            credit: v.credit,
            industry: v.industry,
            officeAddress: v.officeAddress || undefined,
            remark: v.remark || undefined,
            creditCode: v.creditCode || undefined
          },
          contacts: contactsBySeq.get(seq) || []
        });
      }

      parseErrors.value = errors;
      if (errors.length > 0) {
        batchPayload.value = null;
        vendorCount.value = 0;
        contactCount.value = 0;
        return;
      }
      batchPayload.value = { items };
      vendorCount.value = items.length;
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
      `本次将导入供应商 ${vendorCount.value} 家，供应商联系人 ${contactCount.value} 条。确认提交到系统吗？`,
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
    const res = await vendorApi.importVendorsBatch(batchPayload.value);
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
