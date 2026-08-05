<template>
  <div class="wty-doc wty-doc--semicore">
    <WarrantyLetterReportBody v-bind="props" />
  </div>
</template>

<script setup lang="ts">
import WarrantyLetterReportBody from '../WarrantyLetterReportBody.vue'
import {
  warrantyLetterReportDocumentPropDefaults,
  type WarrantyLetterReportDocumentProps
} from '../types'

const props = withDefaults(
  defineProps<WarrantyLetterReportDocumentProps>(),
  warrantyLetterReportDocumentPropDefaults
)
</script>

<style lang="scss">
/* ecoinf 租户：绿点缀 + 深标题 */
.wty-doc--semicore {
  $accent: #a8d070;
  $ink: #101010;
  $border: #222;

  width: 210mm;
  min-height: 297mm;
  margin: 0 auto;
  padding: 10mm 14mm 14mm;
  box-sizing: border-box;
  background: #fff;
  color: $ink;
  font-size: 10.5pt;
  line-height: 1.55;
  font-family: 'Microsoft YaHei', 'SimSun', system-ui, sans-serif;

  .wty-doc__masthead {
    display: grid;
    grid-template-columns: 24mm 1fr 48mm;
    align-items: start;
    gap: 4mm;
    margin-bottom: 8mm;
    padding-bottom: 8px;
    border-bottom: 3px solid $accent;
  }

  .wty-doc__logo {
    max-height: 14mm;
    max-width: 28mm;
    object-fit: contain;
    display: block;
  }

  .wty-doc__masthead-center {
    text-align: center;
    padding-top: 2px;
  }

  .wty-doc__masthead-company {
    font-size: 15pt;
    font-weight: 700;
    color: $ink;
  }

  .wty-doc__masthead-title {
    font-size: 13pt;
    font-weight: 700;
    margin-top: 6px;
    letter-spacing: 0.12em;
  }

  .wty-doc__masthead-right {
    font-size: 9.5pt;
    text-align: right;
    line-height: 1.6;
  }

  .wty-doc__k {
    font-weight: 600;
    margin-right: 4px;
  }

  .wty-doc__to {
    margin-bottom: 6mm;
    padding: 6px 0 6px 8px;
    border-left: 3px solid $accent;
  }

  .wty-doc__to-line {
    margin-bottom: 6px;
  }

  .wty-doc__lbl {
    font-weight: 600;
    display: inline-block;
    min-width: 5em;
  }

  .wty-doc__body {
    margin: 6mm 0 10mm;
  }

  .wty-doc__p {
    margin: 0 0 10px;
    text-align: justify;
    text-indent: 2em;
  }

  .wty-doc__sign {
    margin-top: 12mm;
    max-width: 72mm;
  }

  .wty-doc__sign-t {
    font-weight: 600;
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
  .wty-doc--semicore {
    width: auto;
    min-height: auto;
    margin: 0;
    padding: 8mm 12mm;
  }
}
</style>
