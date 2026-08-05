<template>
  <div class="wty-doc wty-doc--idesemi">
    <div class="wty-doc__brand-bar">
      <div class="wty-doc__brand-bar-inner">
        <img v-if="logoUrl" class="wty-doc__logo" :src="logoUrl" alt="" />
        <div class="wty-doc__brand-text">
          <div class="wty-doc__company">{{ issuerName }}</div>
        </div>
      </div>
      <div class="wty-doc__accent-line" aria-hidden="true" />
    </div>
    <WarrantyLetterReportBody v-bind="bodyBind" />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import WarrantyLetterReportBody from '../WarrantyLetterReportBody.vue'
import {
  warrantyLetterReportDocumentPropDefaults,
  type WarrantyLetterReportDocumentProps
} from '../types'

const props = withDefaults(
  defineProps<WarrantyLetterReportDocumentProps>(),
  warrantyLetterReportDocumentPropDefaults
)

const bodyBind = computed(() => ({
  ...props,
  logoUrl: null as string | null,
  issuerName: ''
}))
</script>

<style lang="scss">
/* semicore 租户：深色顶栏 */
.wty-doc--idesemi {
  $header: #0d1f35;
  $accent: #020612;
  $border: #c4b5d4;
  $ink: #1f1235;

  width: 210mm;
  min-height: 297mm;
  margin: 0 auto;
  padding: 0 11mm 12mm;
  box-sizing: border-box;
  background: #fff;
  color: $ink;
  font-size: 10.5pt;
  line-height: 1.55;
  font-family: 'Segoe UI', 'Microsoft YaHei', system-ui, sans-serif;

  .wty-doc__brand-bar {
    margin: 0 -11mm 6mm;
    padding: 5mm 11mm 0;
    background: $header;
    color: #fff;
  }

  .wty-doc__brand-bar-inner {
    display: flex;
    align-items: center;
    gap: 4mm;
    padding-bottom: 3.5mm;
  }

  .wty-doc__logo {
    max-height: 12mm;
    max-width: 26mm;
    object-fit: contain;
    background: rgba(255, 255, 255, 0.92);
    padding: 2px 4px;
    border-radius: 2px;
  }

  .wty-doc__company {
    font-size: 13pt;
    font-weight: 700;
    color: #fff;
  }

  .wty-doc__accent-line {
    height: 3px;
    background: linear-gradient(90deg, $accent 0%, #0a1628 45%, transparent 100%);
  }

  .wty-doc__masthead {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 4mm;
    margin-bottom: 6mm;
    padding-bottom: 6px;
    border-bottom: 1px solid $border;
  }

  .wty-doc__masthead-left {
    display: none;
  }

  .wty-doc__masthead-center {
    flex: 1;
    text-align: left;
  }

  .wty-doc__masthead-company {
    display: none;
  }

  .wty-doc__masthead-title {
    font-size: 14pt;
    font-weight: 700;
    letter-spacing: 0.1em;
    color: $header;
  }

  .wty-doc__masthead-right {
    font-size: 9.5pt;
    text-align: right;
    line-height: 1.6;
  }

  .wty-doc__k {
    font-weight: 700;
    color: $header;
    margin-right: 4px;
  }

  .wty-doc__to {
    margin-bottom: 6mm;
    padding: 4mm;
    border: 1px solid $border;
    background: #fcfaff;
  }

  .wty-doc__to-line {
    margin-bottom: 6px;
  }

  .wty-doc__lbl {
    font-weight: 700;
    color: $header;
    display: inline-block;
    min-width: 5em;
  }

  .wty-doc__body {
    margin: 4mm 0 10mm;
  }

  .wty-doc__p {
    margin: 0 0 10px;
    text-align: justify;
    text-indent: 2em;
  }

  .wty-doc__sign {
    margin-top: 10mm;
    max-width: 80mm;
    border: 1px solid $border;
    padding: 4mm;
    background: #fcfaff;
  }

  .wty-doc__sign-t {
    font-weight: 700;
    color: $header;
    margin-bottom: 4px;
  }

  .wty-doc__sign-pad {
    min-height: 22mm;
    margin: 6px 0 8px;
    position: relative;
  }

  .wty-doc__sign-pad--seal {
    min-height: 26mm;
    background: #fff;
    isolation: isolate;
  }

  .wty-doc__seal {
    position: absolute;
    left: 0;
    bottom: 0;
    max-height: 26mm;
    max-width: 32mm;
    object-fit: contain;
  }

  .wty-doc__sign-date {
    font-size: 9.5pt;
  }
}

@media print {
  .wty-doc--idesemi {
    width: auto;
    min-height: auto;
    margin: 0;
    padding: 0 10mm 8mm;

    .wty-doc__brand-bar {
      margin: 0 -10mm 6mm;
      padding: 5mm 10mm 0;
    }
  }
}
</style>
