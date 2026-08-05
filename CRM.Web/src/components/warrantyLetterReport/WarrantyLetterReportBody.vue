<template>
  <div class="wty-doc__inner">
    <header class="wty-doc__masthead">
      <div class="wty-doc__masthead-left">
        <img v-if="logoUrl" class="wty-doc__logo" :src="logoUrl" alt="" />
      </div>
      <div class="wty-doc__masthead-center">
        <div v-if="issuerName" class="wty-doc__masthead-company">{{ issuerName }}</div>
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
import {
  warrantyLetterReportDocumentPropDefaults,
  type WarrantyLetterReportDocumentProps
} from './types'

withDefaults(defineProps<WarrantyLetterReportDocumentProps>(), warrantyLetterReportDocumentPropDefaults)
</script>
