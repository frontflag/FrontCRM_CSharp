<template>
  <div class="wty-doc wty-doc--ecoinf">
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
/* idesemi 租户：工业极简 + 青点缀 */
.wty-doc--ecoinf {
  $accent: #6dc5f6;
  $title: #11161f;
  $ink: #111;
  $muted: #525252;
  $line: #d4d4d4;

  width: 210mm;
  min-height: 297mm;
  margin: 0 auto;
  padding: 12mm 14mm 14mm;
  box-sizing: border-box;
  background: #fff;
  color: $ink;
  font-size: 10.5pt;
  line-height: 1.5;
  font-family: 'IBM Plex Sans', 'Segoe UI', 'Microsoft YaHei', system-ui, sans-serif;

  .wty-doc__masthead {
    display: grid;
    grid-template-columns: 28mm 1fr 48mm;
    align-items: start;
    gap: 4mm;
    margin-bottom: 6mm;
    padding-bottom: 4mm;
    border-bottom: 2px solid $ink;
  }

  .wty-doc__logo {
    max-height: 14mm;
    max-width: 32mm;
    object-fit: contain;
    display: block;
  }

  .wty-doc__masthead-center {
    text-align: left;
  }

  .wty-doc__masthead-company {
    font-size: 10pt;
    font-weight: 600;
    letter-spacing: 0.12em;
    text-transform: uppercase;
    color: $muted;
  }

  .wty-doc__masthead-title {
    font-size: 16pt;
    font-weight: 700;
    margin-top: 4px;
    letter-spacing: 0.08em;
    color: $title;
  }

  .wty-doc__masthead-right {
    font-size: 9pt;
    text-align: right;
    line-height: 1.6;
    color: $muted;
  }

  .wty-doc__k {
    font-weight: 700;
    color: $ink;
    margin-right: 4px;
  }

  .wty-doc__to {
    margin-bottom: 6mm;
  }

  .wty-doc__to-line {
    margin-bottom: 6px;
  }

  .wty-doc__lbl {
    font-weight: 700;
    display: inline-block;
    min-width: 5em;
  }

  .wty-doc__lbl::before {
    content: '';
    display: inline-block;
    width: 3px;
    height: 0.9em;
    background: $accent;
    margin-right: 6px;
    vertical-align: -0.1em;
  }

  .wty-doc__body {
    margin: 4mm 0 10mm;
    border-top: 1px solid $line;
    padding-top: 4mm;
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
    font-weight: 700;
    letter-spacing: 0.06em;
    margin-bottom: 4px;
  }

  .wty-doc__sign-pad {
    min-height: 20mm;
    margin: 8px 0 8px;
    border-bottom: 1.5px solid $ink;
    position: relative;
  }

  .wty-doc__sign-pad--seal {
    isolation: isolate;
  }

  .wty-doc__seal {
    position: absolute;
    left: 0;
    bottom: 2px;
    max-height: 24mm;
    max-width: 30mm;
    object-fit: contain;
  }

  .wty-doc__sign-date {
    font-size: 9.5pt;
    color: $muted;
  }
}

@media print {
  .wty-doc--ecoinf {
    width: auto;
    min-height: auto;
    margin: 0;
    padding: 8mm 10mm;
  }
}
</style>
