<template>
  <div class="wty-doc">
    <header class="wty-doc__masthead">
      <div class="wty-doc__masthead-left">
        <img v-if="logoUrl" class="wty-doc__logo" :src="logoUrl" alt="" />
      </div>
      <div class="wty-doc__masthead-center">
        <div class="wty-doc__masthead-company">{{ issuerName }}</div>
        <div class="wty-doc__masthead-title">{{ docTitle }}</div>
      </div>
      <div class="wty-doc__masthead-right">
        <div><span class="wty-doc__k">{{ noLabel }}</span>{{ docNo }}</div>
        <div><span class="wty-doc__k">{{ dateLabel }}</span>{{ issueDate }}</div>
      </div>
    </header>

    <section class="wty-doc__to">
      <div class="wty-doc__to-line"><span class="wty-doc__lbl">{{ toNameLabel }}</span>{{ vendorName }}</div>
      <div v-if="vendorCode" class="wty-doc__to-line"><span class="wty-doc__lbl">{{ codeLabel }}</span>{{ vendorCode }}</div>
      <div class="wty-doc__to-line"><span class="wty-doc__lbl">{{ addrLabel }}</span>{{ vendorAddress }}</div>
    </section>

    <section class="wty-doc__body">
      <p v-for="(p, i) in paragraphs" :key="i" class="wty-doc__p">{{ p }}</p>
    </section>

    <section class="wty-doc__sign">
      <div class="wty-doc__sign-cell">
        <div class="wty-doc__sign-t">{{ issuerSignLabel }}</div>
        <div class="wty-doc__sign-pad wty-doc__sign-pad--seal">
          <img v-if="showSeal && sealUrl" class="wty-doc__seal" :src="sealUrl" alt="" />
        </div>
        <div class="wty-doc__sign-date">{{ dateLabel }} {{ issueDate }}</div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  issuerName: string
  docTitle: string
  docNo: string
  issueDate: string
  noLabel: string
  dateLabel: string
  toNameLabel: string
  codeLabel: string
  addrLabel: string
  vendorName: string
  vendorCode: string
  vendorAddress: string
  paragraphs: string[]
  issuerSignLabel: string
  sealUrl: string | null
  logoUrl: string | null
  showSeal: boolean
}>()
</script>

<style scoped lang="scss">
$wty-border: #222;
$wty-head: #111;

.wty-doc {
  width: 210mm;
  min-height: 297mm;
  margin: 0 auto;
  padding: 10mm 14mm 14mm;
  box-sizing: border-box;
  background: #fff;
  color: $wty-head;
  font-size: 10.5pt;
  line-height: 1.55;
  font-family: 'Microsoft YaHei', 'SimSun', 'Times New Roman', system-ui, sans-serif;
}

.wty-doc__masthead {
  display: grid;
  grid-template-columns: 24mm 1fr 48mm;
  align-items: start;
  gap: 4mm;
  margin-bottom: 10mm;
  padding-bottom: 8px;
  border-bottom: 2px solid $wty-border;
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
  margin-bottom: 8mm;
  padding: 8px 0;
}

.wty-doc__to-line {
  margin-bottom: 6px;
  font-size: 10.5pt;
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

@media print {
  .wty-doc {
    width: auto;
    min-height: auto;
    margin: 0;
    padding: 8mm 12mm;
  }
}
</style>
