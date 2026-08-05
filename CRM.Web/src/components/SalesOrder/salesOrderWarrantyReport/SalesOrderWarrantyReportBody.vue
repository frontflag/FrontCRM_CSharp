<template>
  <div class="wty-doc__inner">
    <header class="wty-doc__top">
      <div class="wty-doc__top-left">
        <img v-if="logoUrl" class="wty-doc__logo" :src="logoUrl" alt="" />
      </div>
      <div class="wty-doc__top-right">
        <div class="wty-doc__company">{{ companyName }}</div>
        <div v-if="companyAddress" class="wty-doc__company-addr">{{ companyAddress }}</div>
      </div>
    </header>

    <div class="wty-doc__title-block">
      <div class="wty-doc__title">{{ docTitle }}</div>
      <div v-if="docSubtitle" class="wty-doc__subtitle">{{ docSubtitle }}</div>
    </div>

    <section class="wty-doc__parties">
      <div><span class="wty-doc__lbl">{{ partyALabel }}</span>{{ partyAName }}</div>
      <div><span class="wty-doc__lbl">{{ partyBLabel }}</span>{{ partyBName }}</div>
    </section>

    <p class="wty-doc__intro">{{ introText }}</p>

    <section class="wty-doc__notes">
      <div v-if="notesHeading" class="wty-doc__notes-hd">{{ notesHeading }}</div>
      <ol v-if="notes.length" class="wty-doc__notes-list">
        <li v-for="(n, i) in notes" :key="i">{{ n }}</li>
      </ol>
      <p v-if="notesAfter" class="wty-doc__notes-after">{{ notesAfter }}</p>
      <div v-if="goodsLead" class="wty-doc__notes-hd">{{ goodsLead }}</div>
    </section>

    <table class="wty-doc__grid">
      <thead>
        <tr>
          <th class="w-pn">{{ colPn }}</th>
          <th class="w-brand">{{ colBrand }}</th>
          <th class="w-qty">{{ colQty }}</th>
          <th class="w-dc">{{ colDc }}</th>
          <th class="w-cpn">{{ colCustomerPn }}</th>
          <th class="w-cso">{{ colCustomerSo }}</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(line, i) in lines" :key="'l' + i">
          <td>{{ line.pn }}</td>
          <td>{{ line.brand }}</td>
          <td class="cen">{{ line.qty }}</td>
          <td class="cen">{{ line.dateCode }}</td>
          <td>{{ line.customerPn }}</td>
          <td>{{ line.customerSo }}</td>
        </tr>
        <tr v-if="lines.length === 0">
          <td colspan="6" class="wty-doc__empty">{{ emptyLinesHint }}</td>
        </tr>
      </tbody>
    </table>

    <section class="wty-doc__sign">
      <div class="wty-doc__sign-col">
        <div class="wty-doc__sign-party">{{ partyALabel }}{{ partyAName }}</div>
        <div class="wty-doc__sign-line"><span class="wty-doc__lbl">{{ signRepLabel }}</span>{{ partyARep }}</div>
        <div class="wty-doc__sign-line"><span class="wty-doc__lbl">{{ signPhoneLabel }}</span>{{ partyAPhone }}</div>
        <div class="wty-doc__sign-line"><span class="wty-doc__lbl">{{ signAddrLabel }}</span>{{ partyAAddress }}</div>
        <div class="wty-doc__sign-pad wty-doc__sign-pad--seal">
          <img v-if="showSeal && sealUrl" class="wty-doc__seal" :src="sealUrl" alt="" />
        </div>
      </div>
      <div class="wty-doc__sign-col">
        <div class="wty-doc__sign-party">{{ partyBLabel }}{{ partyBName }}</div>
        <div class="wty-doc__sign-line"><span class="wty-doc__lbl">{{ signRepLabel }}</span>{{ partyBRep }}</div>
        <div class="wty-doc__sign-line"><span class="wty-doc__lbl">{{ signPhoneLabel }}</span>{{ partyBPhone }}</div>
        <div class="wty-doc__sign-line"><span class="wty-doc__lbl">{{ signAddrLabel }}</span>{{ partyBAddress }}</div>
        <div class="wty-doc__sign-pad"></div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import {
  salesOrderWarrantyReportDocumentPropDefaults,
  type SalesOrderWarrantyReportDocumentProps
} from './types'

withDefaults(defineProps<SalesOrderWarrantyReportDocumentProps>(), salesOrderWarrantyReportDocumentPropDefaults)
</script>
