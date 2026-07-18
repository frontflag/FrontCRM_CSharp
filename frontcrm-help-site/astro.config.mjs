import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';

// https://astro.build/config
export default defineConfig({
  site: 'https://help.frontcrm.com',
  integrations: [
    starlight({
      title: 'FrontCRM Help',
      logo: {
        light: './src/assets/logo.svg',
        dark: './src/assets/logo.svg',
        replacesTitle: false,
      },
      defaultLocale: 'zh',
      locales: {
        en: {
          label: 'English',
          lang: 'en',
        },
        zh: {
          label: '简体中文',
          lang: 'zh-CN',
        },
      },
      head: [
        {
          tag: 'script',
          content: `try{if(!localStorage.getItem('starlight-theme'))localStorage.setItem('starlight-theme','light')}catch(e){}`,
        },
        {
          tag: 'link',
          attrs: {
            rel: 'preconnect',
            href: 'https://fonts.googleapis.com',
          },
        },
        {
          tag: 'link',
          attrs: {
            rel: 'preconnect',
            href: 'https://fonts.gstatic.com',
            crossorigin: true,
          },
        },
        {
          tag: 'link',
          attrs: {
            rel: 'stylesheet',
            href: 'https://fonts.googleapis.com/css2?family=Sora:wght@500;600;700;800&family=Noto+Sans+SC:wght@400;500;600;700&display=swap',
          },
        },
      ],
      social: {
        github: 'https://github.com/your-org/frontcrm',
      },
      editLink: {
        baseUrl: 'https://github.com/your-org/frontcrm/edit/developV3/frontcrm-help-site/',
      },
      lastUpdated: true,
      pagination: true,
      tableOfContents: {
        minHeadingLevel: 2,
        maxHeadingLevel: 3,
      },
      customCss: ['./src/styles/custom.css'],
      sidebar: [
        {
          label: 'Start here',
          translations: {
            'zh-CN': '从这里开始',
          },
          items: [
            { label: 'Introduction', link: '/', translations: { 'zh-CN': '简介' } },
          ],
        },
        {
          label: 'Modules',
          translations: {
            'zh-CN': '功能模块',
          },
          items: [
            { label: 'Dashboard', link: '/dashboard', translations: { 'zh-CN': '工作台' } },
            { label: 'Customer', link: '/customer', translations: { 'zh-CN': '客户' } },
            { label: 'Vendor', link: '/vendor', translations: { 'zh-CN': '供应商' } },
            { label: 'RFQ & Quote', link: '/rfq-quote', translations: { 'zh-CN': '询价与报价' } },
            { label: 'Sales Order', link: '/sales-order', translations: { 'zh-CN': '销售订单' } },
            { label: 'Purchase Order', link: '/purchase-order', translations: { 'zh-CN': '采购订单' } },
            { label: 'Inventory', link: '/inventory', translations: { 'zh-CN': '库存' } },
            { label: 'Logistics', link: '/logistics', translations: { 'zh-CN': '物流' } },
            { label: 'Customs', link: '/customs', translations: { 'zh-CN': '报关' } },
            { label: 'Finance', link: '/finance', translations: { 'zh-CN': '财务' } },
            { label: 'Accumulated', link: '/accumulated', translations: { 'zh-CN': '累计' } },
          ],
        },
        {
          label: 'Profile & System',
          translations: {
            'zh-CN': '个人与系统',
          },
          items: [
            { label: 'Profile', link: '/profile', translations: { 'zh-CN': '个人中心' } },
            { label: 'System', link: '/system', translations: { 'zh-CN': '系统' } },
            { label: 'Release Notes', link: '/release-notes', translations: { 'zh-CN': '版本日志' } },
          ],
        },
      ],
    }),
  ],
});
