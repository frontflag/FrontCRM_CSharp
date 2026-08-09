<template>
  <div class="po-doc po-doc--landscape" :class="themeClass">
    <!-- ===== Idesemi：与竖版 PackingReportSkinIdesemi 同色系/结构 ===== -->
    <template v-if="theme === 'idesemi'">
      <header class="po-doc__masthead">
        <div class="po-doc__brand-bar">
          <div class="po-doc__brand-bar-inner">
            <img v-if="logoUrl" class="po-doc__logo" :src="logoUrl" alt="" />
            <div class="po-doc__brand-text">
              <div class="po-doc__masthead-company">{{ headerCompanyName }}</div>
              <div v-if="headerWarehouseAddress" class="po-doc__masthead-warehouse-addr">{{ headerWarehouseAddress }}</div>
            </div>
          </div>
          <div class="po-doc__amber-line" aria-hidden="true" />
        </div>
        <div class="po-doc__masthead-row">
          <div class="po-doc__title-block">
            <div class="po-doc__masthead-title">{{ docTitle }}</div>
            <div v-if="docSubtitle" class="po-doc__masthead-sub">{{ docSubtitle }}</div>
          </div>
          <aside class="po-doc__meta-card">
            <div><span class="po-doc__k">{{ labels.date }}</span>{{ docDate }}</div>
            <div class="po-doc__meta-line--nowrap">
              <span class="po-doc__k">{{ labels.packingNo }}</span>{{ docNo }}
            </div>
            <div class="po-doc__meta-line--nowrap">
              <span class="po-doc__k">{{ labels.shipMethod }}</span>{{ shipmentMethodDisplay }}
            </div>
          </aside>
        </div>
      </header>

      <div class="po-doc__addr">
        <div class="po-doc__addr-col">
          <div class="po-doc__addr-hd">{{ labels.billTo }}</div>
          <div class="po-doc__addr-body">
            <div v-for="(t, i) in billToLines" :key="'bt' + i" class="po-doc__tri-line">{{ t }}</div>
          </div>
        </div>
        <div class="po-doc__addr-col">
          <div class="po-doc__addr-hd">{{ labels.shipTo }}</div>
          <div class="po-doc__addr-body">
            <div v-for="(t, i) in shipToLines" :key="'st' + i" class="po-doc__tri-line">{{ t }}</div>
          </div>
        </div>
      </div>
    </template>

    <!-- ===== Semicore 橙表：与竖版 PackingReportSkinSemicore 同结构 ===== -->
    <template v-else-if="theme === 'semicore'">
      <header class="po-doc__masthead">
        <div class="po-doc__masthead-top po-doc__masthead-top--sc">
          <div class="po-doc__masthead-logo-wrap">
            <img v-if="logoUrl" class="po-doc__logo" :src="logoUrl" alt="" />
          </div>
          <div class="po-doc__masthead-center">
            <div class="po-doc__masthead-company">{{ headerCompanyName }}</div>
            <div v-if="headerWarehouseAddress" class="po-doc__masthead-warehouse-addr">{{ headerWarehouseAddress }}</div>
            <div class="po-doc__masthead-title-gap" aria-hidden="true"></div>
            <div class="po-doc__masthead-title">{{ docTitle }}</div>
            <div v-if="docSubtitle" class="po-doc__masthead-sub">{{ docSubtitle }}</div>
          </div>
        </div>
        <div class="po-doc__masthead-meta">
          <div><span class="po-doc__k">{{ labels.date }}</span>{{ docDate }}</div>
          <div class="po-doc__meta-line--nowrap">
            <span class="po-doc__k">{{ labels.packingNo }}</span>{{ docNo }}
          </div>
          <div class="po-doc__meta-line--nowrap">
            <span class="po-doc__k">{{ labels.shipMethod }}</span>{{ shipmentMethodDisplay }}
          </div>
        </div>
        <div class="po-doc__masthead-meta-gap" aria-hidden="true"></div>
      </header>

      <table class="po-doc__tri po-doc__tri--addr">
        <thead>
          <tr>
            <th>{{ labels.billTo }}</th>
            <th>{{ labels.shipTo }}</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td class="po-doc__tri-cell">
              <div v-for="(t, i) in billToLines" :key="'bt' + i" class="po-doc__tri-line">{{ t }}</div>
            </td>
            <td class="po-doc__tri-cell">
              <div v-for="(t, i) in shipToLines" :key="'st' + i" class="po-doc__tri-line">{{ t }}</div>
            </td>
          </tr>
        </tbody>
      </table>
    </template>

    <!-- ===== Ecoinf：与竖版 PackingReportSkinEcoinf 同结构 ===== -->
    <template v-else>
      <header class="po-doc__masthead">
        <div class="po-doc__masthead-top po-doc__masthead-top--eco">
          <div class="po-doc__title-stack">
            <div class="po-doc__masthead-title">{{ docTitle }}</div>
            <div class="po-doc__masthead-company">{{ headerCompanyName }}</div>
            <div v-if="headerWarehouseAddress" class="po-doc__masthead-warehouse-addr">{{ headerWarehouseAddress }}</div>
            <div v-if="docSubtitle" class="po-doc__masthead-sub">{{ docSubtitle }}</div>
          </div>
          <div class="po-doc__masthead-logo-wrap">
            <img v-if="logoUrl" class="po-doc__logo" :src="logoUrl" alt="" />
          </div>
        </div>
        <div class="po-doc__meta-row">
          <div><span class="po-doc__k">{{ labels.date }}</span>{{ docDate }}</div>
          <div class="po-doc__meta-line--nowrap">
            <span class="po-doc__k">{{ labels.packingNo }}</span>{{ docNo }}
          </div>
          <div class="po-doc__meta-line--nowrap">
            <span class="po-doc__k">{{ labels.shipMethod }}</span>{{ shipmentMethodDisplay }}
          </div>
        </div>
      </header>

      <div class="po-doc__addr po-doc__addr--eco">
        <section class="po-doc__addr-block">
          <div class="po-doc__section-label">{{ labels.billTo }}</div>
          <div class="po-doc__addr-body">
            <div v-for="(t, i) in billToLines" :key="'bt' + i" class="po-doc__tri-line">{{ t }}</div>
          </div>
        </section>
        <section class="po-doc__addr-block">
          <div class="po-doc__section-label">{{ labels.shipTo }}</div>
          <div class="po-doc__addr-body">
            <div v-for="(t, i) in shipToLines" :key="'st' + i" class="po-doc__tri-line">{{ t }}</div>
          </div>
        </section>
      </div>
    </template>

    <!-- 横版 14 列明细（三主题共用 markup，样式按 theme class） -->
    <table class="po-doc__ls-grid">
      <thead>
        <tr>
          <th>{{ labels.sNo }}</th>
          <th>{{ labels.customerPo }}</th>
          <th>{{ labels.partNumber }}</th>
          <th>{{ labels.customerPn }}</th>
          <th>{{ labels.brand }}</th>
          <th class="num">{{ labels.qtyPcs }}</th>
          <th>{{ labels.dc }}</th>
          <th>{{ labels.co }}</th>
          <th>{{ labels.cod }}</th>
          <th>{{ labels.size }}</th>
          <th class="num">{{ labels.nwKg }}</th>
          <th class="num">{{ labels.gwKg }}</th>
          <th>{{ labels.cartonCount }}</th>
          <th>{{ labels.remark }}</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="line in lines" :key="'l' + line.index">
          <td class="cen">{{ line.index }}</td>
          <td>{{ line.customerPo }}</td>
          <td>{{ line.partNumber }}</td>
          <td>{{ line.customerPn }}</td>
          <td>{{ line.brand }}</td>
          <td class="num">{{ line.qty }}</td>
          <td class="cen">{{ line.dc }}</td>
          <td class="cen">{{ line.co }}</td>
          <td class="cen">{{ line.cod }}</td>
          <td class="cen">{{ line.size }}</td>
          <td class="num">{{ line.nw }}</td>
          <td class="num">{{ line.gw }}</td>
          <td class="cen">{{ line.carton }}</td>
          <td>{{ line.remark }}</td>
        </tr>
        <tr v-if="lines.length === 0">
          <td colspan="14" class="po-doc__empty">{{ labels.noItems }}</td>
        </tr>
        <tr v-else class="po-doc__sum-row">
          <td colspan="5" class="cen">{{ labels.total }}</td>
          <td class="num">{{ totalQty }}</td>
          <td colspan="4"></td>
          <td class="num">{{ totalNw }}</td>
          <td class="num">{{ totalGw }}</td>
          <td class="cen">{{ totalCarton }}</td>
          <td></td>
        </tr>
      </tbody>
    </table>

    <!-- QC / Remarks / Sign：按主题复用竖版结构 -->
    <template v-if="theme === 'idesemi'">
      <section v-if="withShipmentInspection" class="po-doc__qc">
        <div class="po-doc__section-title">{{ labels.outboundInspection }}</div>
        <table class="po-doc__qc-grid">
          <thead>
            <tr>
              <th class="w-qc-i">{{ labels.no }}</th>
              <th class="w-qc-item">{{ labels.item }}</th>
              <th class="w-qc-j">{{ labels.result }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(item, idx) in labels.qcItems" :key="'qc' + idx">
              <td class="cen">
                <span class="po-doc__qc-dot">{{ idx + 1 }}</span>
              </td>
              <td class="qc-item-cell">{{ item }}</td>
              <td class="po-doc__qc-check">&nbsp;</td>
            </tr>
          </tbody>
        </table>
        <div class="po-doc__qc-foot">
          <span>{{ labels.qcInspector }}</span>
          <span>{{ labels.qcDate }}</span>
        </div>
      </section>

      <section class="po-doc__addon">
        <div class="po-doc__section-title">{{ labels.remarks }}</div>
        <div class="po-doc__addon-body">
          <div v-for="(t, i) in notes" :key="'n' + i" class="po-doc__term-line">{{ t }}</div>
        </div>
      </section>

      <section class="po-doc__sign">
        <div class="po-doc__sign-col">
          <div class="po-doc__sign-t">{{ labels.shipperSign }}</div>
          <div class="po-doc__sign-pad po-doc__sign-pad--seal">
            <img v-if="showSeal && sealUrl" class="po-doc__seal" :src="sealUrl" alt="" />
          </div>
          <div class="po-doc__sign-foot">{{ labels.date }} {{ signDate }}</div>
        </div>
        <div class="po-doc__sign-col">
          <div class="po-doc__sign-t">{{ labels.consigneeSign }}</div>
          <div class="po-doc__sign-pad"></div>
          <div class="po-doc__sign-foot">{{ labels.date }}</div>
        </div>
      </section>
    </template>

    <template v-else-if="theme === 'semicore'">
      <section v-if="withShipmentInspection" class="po-doc__qc">
        <div class="po-doc__panel po-doc__panel--qc">
          <div class="po-doc__addon-bar">{{ labels.outboundInspection }}</div>
          <table class="po-doc__qc-grid">
            <thead>
              <tr>
                <th class="w-qc-i">{{ labels.no }}</th>
                <th class="w-qc-item">{{ labels.item }}</th>
                <th class="w-qc-j">{{ labels.result }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(item, idx) in labels.qcItems" :key="'qc' + idx">
                <td class="cen">{{ idx + 1 }}</td>
                <td class="qc-item-cell">{{ item }}</td>
                <td>&nbsp;</td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="po-doc__qc-foot">
          <span>{{ labels.qcInspector }}</span>
          <span>{{ labels.qcDate }}</span>
        </div>
      </section>

      <section class="po-doc__addon po-doc__panel">
        <div class="po-doc__addon-bar">{{ labels.remarks }}</div>
        <div class="po-doc__addon-body">
          <div v-for="(t, i) in notes" :key="'n' + i" class="po-doc__term-line">{{ t }}</div>
        </div>
      </section>

      <section class="po-doc__sign po-doc__sign--sc">
        <div class="po-doc__sign-t po-doc__sign-t--left">{{ labels.shipperSign }}</div>
        <div class="po-doc__sign-t po-doc__sign-t--right">{{ labels.consigneeSign }}</div>
        <div class="po-doc__sign-pad po-doc__sign-pad--left po-doc__sign-pad--seal">
          <img v-if="showSeal && sealUrl" class="po-doc__seal" :src="sealUrl" alt="" />
        </div>
        <div class="po-doc__sign-pad po-doc__sign-pad--right"></div>
        <div class="po-doc__sign-foot po-doc__sign-foot--left">{{ labels.date }} {{ signDate }}</div>
        <div class="po-doc__sign-foot po-doc__sign-foot--right">{{ labels.date }}</div>
      </section>
    </template>

    <template v-else>
      <section v-if="withShipmentInspection" class="po-doc__qc">
        <div class="po-doc__section-label">{{ labels.outboundInspection }}</div>
        <ul class="po-doc__qc-list">
          <li v-for="(item, idx) in labels.qcItems" :key="'qc' + idx" class="po-doc__qc-li">
            <span class="po-doc__qc-box" aria-hidden="true" />
            <span class="po-doc__qc-idx">{{ idx + 1 }}.</span>
            <span class="po-doc__qc-text">{{ item }}</span>
            <span class="po-doc__qc-result">{{ labels.result }}</span>
          </li>
        </ul>
        <div class="po-doc__qc-foot">
          <span>{{ labels.qcInspector }}</span>
          <span>{{ labels.qcDate }}</span>
        </div>
      </section>

      <section class="po-doc__addon">
        <div class="po-doc__section-label">{{ labels.remarks }}</div>
        <div class="po-doc__addon-body">
          <div v-for="(t, i) in notes" :key="'n' + i" class="po-doc__term-line">{{ t }}</div>
        </div>
      </section>

      <section class="po-doc__sign">
        <div class="po-doc__sign-col">
          <div class="po-doc__sign-t">{{ labels.shipperSign }}</div>
          <div class="po-doc__sign-line po-doc__sign-pad--seal">
            <img v-if="showSeal && sealUrl" class="po-doc__seal" :src="sealUrl" alt="" />
          </div>
          <div class="po-doc__sign-foot">{{ labels.date }} {{ signDate }}</div>
        </div>
        <div class="po-doc__sign-col">
          <div class="po-doc__sign-t">{{ labels.consigneeSign }}</div>
          <div class="po-doc__sign-line"></div>
          <div class="po-doc__sign-foot">{{ labels.date }}</div>
        </div>
      </section>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import {
  packingReportDocumentPropDefaults,
  type PackingReportLandscapeDocumentProps
} from './types'

const props = withDefaults(defineProps<PackingReportLandscapeDocumentProps>(), {
  ...packingReportDocumentPropDefaults,
  theme: 'semicore'
})

const themeClass = computed(() => `po-doc--ls-${props.theme}`)

function formatSum(nums: Array<number | null>, asInt = false): string {
  const vals = nums.filter((n): n is number => n != null && Number.isFinite(n))
  if (!vals.length) return ''
  const sum = vals.reduce((a, b) => a + b, 0)
  if (asInt) return sum.toLocaleString('zh-CN', { maximumFractionDigits: 0 })
  return sum.toLocaleString('zh-CN', { maximumFractionDigits: 4 })
}

const totalQty = computed(() =>
  props.lines.reduce((a, l) => a + (l.qtyNum || 0), 0).toLocaleString('zh-CN', { maximumFractionDigits: 4 })
)
const totalNw = computed(() => formatSum(props.lines.map((l) => l.nwNum)))
const totalGw = computed(() => formatSum(props.lines.map((l) => l.gwNum)))
const totalCarton = computed(() => formatSum(props.lines.map((l) => l.cartonNum), true))
</script>

<style scoped lang="scss">
/* —— 公共：横版纸张 —— */
.po-doc--landscape {
  width: 297mm;
  min-height: 210mm;
  margin: 0 auto;
  padding: 8mm 10mm 10mm;
  box-sizing: border-box;
  background: #fff;
  color: #111;
  font-size: 9pt;
  line-height: 1.4;
}

.cen {
  text-align: center;
}
.num {
  text-align: right;
  white-space: nowrap;
}

.po-doc__ls-grid {
  width: 100%;
  border-collapse: collapse;
  table-layout: fixed;
  margin-bottom: 10px;
  font-size: 7.2pt;
}

.po-doc__ls-grid th,
.po-doc__ls-grid td {
  padding: 3px 2px;
  vertical-align: middle;
  word-break: break-all;
}

.po-doc__ls-grid thead th {
  font-weight: 700;
  text-align: center;
  font-size: 6.8pt;
  line-height: 1.25;
  padding: 5px 2px;
}

.po-doc__empty {
  text-align: center;
  padding: 12px !important;
  color: #666;
}

.po-doc__meta-line--nowrap {
  white-space: nowrap;
}

.w-qc-i {
  width: 10%;
}
.w-qc-item {
  width: 70%;
}
.w-qc-j {
  width: 20%;
}

/* ========== Idesemi（对齐竖版 SkinIdesemi） ========== */
$ide-purple: #2d1b4e;
$ide-purple-deep: #1a0a2e;
$ide-header-bg: #0d1f35;
$ide-accent: #020612;
$ide-amber: $ide-accent;
$ide-amber-soft: #0a1628;
$ide-border: #c4b5d4;
$ide-text: #1f1235;

.po-doc--ls-idesemi {
  color: $ide-text;
  font-family: 'Segoe UI', 'Microsoft YaHei', system-ui, sans-serif;

  .po-doc__brand-bar {
    margin: -8mm -10mm 3mm;
    padding: 4mm 10mm 0;
    background: $ide-header-bg;
    color: #fff;
  }

  .po-doc__brand-bar-inner {
    display: flex;
    align-items: center;
    gap: 4mm;
    padding-bottom: 3mm;
  }

  .po-doc__logo {
    max-height: 11mm;
    max-width: 24mm;
    object-fit: contain;
    display: block;
    background: rgba(255, 255, 255, 0.92);
    padding: 2px 4px;
    border-radius: 2px;
  }

  .po-doc__brand-text {
    flex: 1;
    min-width: 0;
  }

  .po-doc__masthead-company {
    font-size: 13pt;
    font-weight: 700;
    letter-spacing: 0.04em;
    color: #fff;
  }

  .po-doc__masthead-warehouse-addr {
    margin-top: 2px;
    font-size: 8.5pt;
    color: rgba(255, 255, 255, 0.82);
  }

  .po-doc__amber-line {
    height: 3px;
    background: linear-gradient(90deg, $ide-amber 0%, $ide-amber-soft 45%, transparent 100%);
  }

  .po-doc__masthead-row {
    display: grid;
    grid-template-columns: 1fr 58mm;
    gap: 5mm;
    align-items: start;
    margin-bottom: 4mm;
  }

  .po-doc__masthead-title {
    font-size: 15pt;
    font-weight: 700;
    letter-spacing: 0.12em;
    color: $ide-purple-deep;
  }

  .po-doc__masthead-sub {
    margin-top: 2px;
    font-size: 9pt;
    color: #5b4a6e;
  }

  .po-doc__meta-card {
    border: 1px solid $ide-border;
    background: #faf7ff;
    padding: 2.5mm 3mm;
    font-size: 8.5pt;
    line-height: 1.6;
    border-left: 3px solid $ide-amber;
  }

  .po-doc__k {
    font-weight: 700;
    color: $ide-purple;
  }

  .po-doc__addr {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 4mm;
    margin-bottom: 4mm;
  }

  .po-doc__addr-col {
    border: 1px solid $ide-border;
    min-height: 16mm;
  }

  .po-doc__addr-hd {
    display: flex;
    align-items: center;
    gap: 6px;
    padding: 3px 8px;
    font-weight: 700;
    font-size: 9pt;
    color: $ide-purple-deep;
    background: #f3eef9;
    border-bottom: 1px solid $ide-border;
  }

  .po-doc__addr-hd::before {
    content: '';
    width: 3px;
    height: 11px;
    background: $ide-amber;
    border-radius: 1px;
  }

  .po-doc__addr-body {
    padding: 5px 8px;
    font-size: 8.5pt;
  }

  .po-doc__tri-line {
    margin-bottom: 2px;
  }

  .po-doc__ls-grid th,
  .po-doc__ls-grid td {
    border: 1px solid $ide-border;
  }

  .po-doc__ls-grid thead th {
    background: $ide-header-bg;
    color: #fff;
    border-bottom: 2.5px solid $ide-accent;
  }

  .po-doc__sum-row td {
    font-weight: 700;
    background: #faf7ff;
  }

  .po-doc__section-title {
    font-weight: 700;
    font-size: 9.5pt;
    color: $ide-purple-deep;
    padding: 4px 0 4px 8px;
    border-left: 3px solid $ide-amber;
  }

  .po-doc__qc {
    margin: 0 0 8px;
  }

  .po-doc__qc-grid {
    width: 100%;
    border-collapse: collapse;
    table-layout: fixed;
    margin-top: 3px;
  }

  .po-doc__qc-grid th,
  .po-doc__qc-grid td {
    border: 1px solid $ide-border;
    padding: 4px 4px;
    font-size: 8pt;
    vertical-align: middle;
  }

  .po-doc__qc-grid thead th {
    background: #f3eef9;
    color: $ide-purple-deep;
    font-weight: 700;
    text-align: center;
  }

  .po-doc__qc-dot {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 16px;
    height: 16px;
    border-radius: 50%;
    background: $ide-accent;
    color: #fff;
    font-size: 8pt;
    font-weight: 700;
  }

  .po-doc__qc-check {
    background: repeating-linear-gradient(
      -45deg,
      transparent,
      transparent 3px,
      rgba(2, 6, 18, 0.06) 3px,
      rgba(2, 6, 18, 0.06) 6px
    );
  }

  .qc-item-cell {
    font-size: 8pt;
    line-height: 1.4;
    text-align: justify;
  }

  .po-doc__qc-foot {
    display: flex;
    justify-content: space-between;
    margin-top: 6px;
    font-size: 8.5pt;
    padding: 0 4px;
  }

  .po-doc__addon {
    margin-bottom: 8px;
  }

  .po-doc__addon-body {
    border: 1px solid $ide-border;
    padding: 6px 10px;
    font-size: 8.5pt;
    margin-top: 0;
  }

  .po-doc__term-line {
    font-size: 8pt;
    line-height: 1.4;
    margin-bottom: 2px;
    text-align: justify;
  }

  .po-doc__sign {
    display: grid;
    grid-template-columns: 1fr 1fr;
    column-gap: 6mm;
    margin-top: 5mm;
    font-size: 8.5pt;
  }

  .po-doc__sign-col {
    border: 1px solid $ide-border;
    padding: 3mm;
    background: #fcfaff;
  }

  .po-doc__sign-t {
    font-weight: 700;
    color: $ide-purple;
    margin-bottom: 3px;
  }

  .po-doc__sign-pad {
    min-height: 18mm;
    margin: 3px 0 4px;
    position: relative;
  }

  .po-doc__sign-pad--seal {
    background-color: #fff;
    isolation: isolate;
  }

  .po-doc__seal {
    position: absolute;
    left: 0;
    bottom: 0;
    max-height: 20mm;
    max-width: 26mm;
    object-fit: contain;
  }

  .po-doc__sign-foot {
    font-size: 8pt;
    color: #5b4a6e;
  }
}

/* ========== Semicore 橙表（对齐竖版 SkinSemicore） ========== */
$po-orange: #a8d070;
$po-border: #222;
$po-head-fg: #111;
$po-company: #101010;
$po-radius: 6px;

.po-doc--ls-semicore {
  color: $po-head-fg;
  font-family: 'Microsoft YaHei', 'SimHei', 'SimSun', system-ui, sans-serif;

  .po-doc__masthead-top--sc {
    display: grid;
    grid-template-columns: 32mm 1fr;
    align-items: start;
    gap: 4mm;
  }

  .po-doc__logo {
    max-height: 12mm;
    max-width: 26mm;
    object-fit: contain;
    display: block;
  }

  .po-doc__masthead-center {
    text-align: center;
  }

  .po-doc__masthead-company {
    font-size: 14pt;
    font-weight: 700;
    color: $po-company;
  }

  .po-doc__masthead-warehouse-addr {
    margin-top: 2px;
    font-size: 9pt;
    color: #333;
  }

  .po-doc__masthead-title-gap {
    height: 0.8em;
  }

  .po-doc__masthead-title {
    font-size: 15pt;
    letter-spacing: 0.2em;
    text-indent: 0.2em;
  }

  .po-doc__masthead-sub {
    margin-top: 2px;
    font-size: 9pt;
    color: #333;
  }

  .po-doc__masthead-meta {
    margin-top: 2mm;
    font-size: 9pt;
    line-height: 1.55;
  }

  .po-doc__masthead-meta-gap {
    height: 0.8em;
  }

  .po-doc__k {
    font-weight: 600;
  }

  .po-doc__tri {
    width: 100%;
    border-collapse: separate;
    border-spacing: 0;
    margin-bottom: 8px;
    table-layout: fixed;
    border-radius: $po-radius;
    overflow: hidden;
  }

  .po-doc__tri th {
    background: $po-orange;
    color: $po-head-fg;
    font-weight: 700;
    border: 1px solid $po-border;
    padding: 5px 8px;
    text-align: center;
    font-size: 9.5pt;
  }

  .po-doc__tri td {
    border: 1px solid $po-border;
    border-top: none;
    padding: 6px 8px;
    vertical-align: top;
    font-size: 8.5pt;
  }

  .po-doc__tri--addr th:first-child,
  .po-doc__tri--addr tbody td:first-child {
    border-right: none;
  }

  .po-doc__tri--addr th:last-child,
  .po-doc__tri--addr tbody td:last-child {
    border-left: none;
  }

  .po-doc__tri-line {
    margin-bottom: 2px;
  }

  .po-doc__ls-grid {
    border-collapse: separate;
    border-spacing: 0;
    border-radius: $po-radius;
    overflow: hidden;
  }

  .po-doc__ls-grid th,
  .po-doc__ls-grid td {
    border: 1px solid $po-border;
    border-top: none;
    border-left: none;
  }

  .po-doc__ls-grid th:first-child,
  .po-doc__ls-grid td:first-child {
    border-left: 1px solid $po-border;
  }

  .po-doc__ls-grid thead th {
    background: $po-orange;
    color: $po-head-fg;
    border-top: 1px solid $po-border;
  }

  .po-doc__sum-row td {
    font-weight: 700;
    background: #f7faf0;
  }

  .po-doc__addon-bar {
    background: $po-orange;
    color: $po-head-fg;
    font-weight: 700;
    padding: 4px 8px;
    border: 1px solid $po-border;
    border-bottom: none;
    font-size: 9pt;
  }

  .po-doc__panel {
    margin-bottom: 8px;
  }

  .po-doc__qc-grid {
    width: 100%;
    border-collapse: collapse;
  }

  .po-doc__qc-grid th,
  .po-doc__qc-grid td {
    border: 1px solid $po-border;
    padding: 4px;
    font-size: 8pt;
  }

  .po-doc__qc-grid thead th {
    background: rgba($po-orange, 0.35);
    font-weight: 700;
    text-align: center;
  }

  .po-doc__qc-foot {
    display: flex;
    justify-content: space-between;
    margin-top: 6px;
    font-size: 8.5pt;
  }

  .po-doc__addon-body {
    border: 1px solid $po-border;
    padding: 6px 10px;
    font-size: 8.5pt;
  }

  .po-doc__term-line {
    font-size: 8pt;
    line-height: 1.4;
    margin-bottom: 2px;
  }

  .po-doc__sign--sc {
    display: grid;
    grid-template-columns: 1fr 1fr;
    column-gap: 8mm;
    row-gap: 2px;
    margin-top: 5mm;
    font-size: 8.5pt;
  }

  .po-doc__sign-t {
    font-weight: 600;
  }

  .po-doc__sign-pad {
    min-height: 18mm;
    border-bottom: 1px solid #999;
    position: relative;
  }

  .po-doc__seal {
    position: absolute;
    left: 0;
    bottom: 0;
    max-height: 20mm;
    max-width: 26mm;
    object-fit: contain;
  }
}

/* ========== Ecoinf（对齐竖版 SkinEcoinf） ========== */
$eco-accent: #6dc5f6;
$eco-title: #11161f;
$eco-ink: #111;
$eco-muted: #525252;
$eco-line: #d4d4d4;
$eco-zebra: #f5f5f5;

.po-doc--ls-ecoinf {
  color: $eco-ink;
  font-family: 'IBM Plex Sans', 'Segoe UI', 'Microsoft YaHei', system-ui, sans-serif;

  .po-doc__masthead {
    margin-bottom: 4mm;
    padding-bottom: 2.5mm;
    border-bottom: 2px solid $eco-ink;
  }

  .po-doc__masthead-top--eco {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 6mm;
  }

  .po-doc__title-stack {
    flex: 1;
    min-width: 0;
  }

  .po-doc__masthead-title {
    font-size: 16pt;
    font-weight: 700;
    letter-spacing: 0.28em;
    text-transform: uppercase;
    color: $eco-title;
  }

  .po-doc__masthead-company {
    margin-top: 2mm;
    font-size: 9pt;
    font-weight: 600;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: $eco-muted;
  }

  .po-doc__masthead-warehouse-addr {
    margin-top: 2px;
    font-size: 8.5pt;
    color: $eco-muted;
    text-transform: none;
    font-weight: 400;
    letter-spacing: 0;
  }

  .po-doc__logo {
    max-height: 12mm;
    max-width: 28mm;
    object-fit: contain;
    display: block;
  }

  .po-doc__meta-row {
    display: flex;
    flex-wrap: wrap;
    gap: 2mm 8mm;
    margin-top: 2.5mm;
    font-size: 8.5pt;
  }

  .po-doc__k {
    font-weight: 700;
  }

  .po-doc__section-label {
    display: flex;
    align-items: center;
    gap: 6px;
    font-size: 8.5pt;
    font-weight: 700;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    margin: 0 0 2mm;
  }

  .po-doc__section-label::before {
    content: '';
    width: 3px;
    height: 10px;
    background: $eco-accent;
    flex-shrink: 0;
  }

  .po-doc__addr--eco {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 3.5mm;
    margin-bottom: 4mm;
  }

  .po-doc__addr-block {
    border: 1px solid $eco-line;
    padding: 2mm 3mm;
  }

  .po-doc__addr-body {
    font-size: 8.5pt;
  }

  .po-doc__tri-line {
    margin-bottom: 2px;
  }

  .po-doc__ls-grid th,
  .po-doc__ls-grid td {
    border: none;
    border-bottom: 1px solid $eco-line;
    font-size: 7.2pt;
  }

  .po-doc__ls-grid thead th {
    background: transparent;
    color: $eco-ink;
    text-align: left;
    letter-spacing: 0.04em;
    text-transform: uppercase;
    border-top: 2px solid $eco-ink;
    border-bottom: 2px solid $eco-ink;
  }

  .po-doc__ls-grid thead th.num {
    text-align: right;
  }

  .po-doc__ls-grid tbody tr:nth-child(odd) td {
    background: $eco-zebra;
  }

  .po-doc__ls-grid tbody tr.po-doc__sum-row td {
    background: #fff;
    font-weight: 700;
  }

  .po-doc__qc {
    margin-bottom: 8px;
  }

  .po-doc__qc-list {
    list-style: none;
    margin: 0;
    padding: 0;
  }

  .po-doc__qc-li {
    display: flex;
    align-items: flex-start;
    gap: 6px;
    margin-bottom: 3px;
    font-size: 8pt;
  }

  .po-doc__qc-box {
    width: 10px;
    height: 10px;
    border: 1.5px solid $eco-accent;
    flex-shrink: 0;
    margin-top: 2px;
  }

  .po-doc__qc-idx {
    flex-shrink: 0;
    font-weight: 700;
  }

  .po-doc__qc-text {
    flex: 1;
  }

  .po-doc__qc-result {
    flex-shrink: 0;
    color: #888;
    min-width: 40px;
    text-align: right;
  }

  .po-doc__qc-foot {
    display: flex;
    justify-content: space-between;
    margin-top: 6px;
    font-size: 8.5pt;
  }

  .po-doc__addon {
    margin-bottom: 8px;
  }

  .po-doc__addon-body {
    border-top: 1px solid $eco-line;
    padding: 4px 0;
    font-size: 8.5pt;
  }

  .po-doc__term-line {
    font-size: 8pt;
    margin-bottom: 2px;
  }

  .po-doc__sign {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 8mm;
    margin-top: 5mm;
    font-size: 8.5pt;
  }

  .po-doc__sign-t {
    font-weight: 700;
    margin-bottom: 4px;
  }

  .po-doc__sign-line {
    min-height: 18mm;
    border-bottom: 1px solid $eco-line;
    position: relative;
  }

  .po-doc__seal {
    position: absolute;
    left: 0;
    bottom: 0;
    max-height: 20mm;
    max-width: 26mm;
    object-fit: contain;
  }

  .po-doc__sign-foot {
    margin-top: 4px;
    color: $eco-muted;
    font-size: 8pt;
  }
}

@media print {
  .po-doc--landscape {
    width: 297mm;
    min-height: auto;
    padding: 6mm 8mm;
    box-shadow: none;
  }

  .po-doc--ls-idesemi .po-doc__brand-bar {
    margin: -6mm -8mm 3mm;
    padding: 3.5mm 8mm 0;
  }
}
</style>
