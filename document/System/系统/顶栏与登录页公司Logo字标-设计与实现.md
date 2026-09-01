# 顶栏与登录页公司 Logo 字标 — 设计与实现

**状态：** 已实现  
**页面：** 登录 `/login`；登录后全局顶栏（任意业务页）  
**公司信息：** `/system/company-info`（公司 Logo 上传）  
**测试对照：** [顶栏与登录页公司Logo字标-测试对照说明](../../QA/系统/顶栏与登录页公司Logo字标-测试对照说明.md)  
**帮助：** [公司信息](../../../help/pages/公司信息_MENU_COMPANY_INFO.md)

---

## 1. 目标

**semicore** 构建：顶栏左侧品牌图改为较宽**字标**（标题仍在右侧），登录页左侧品牌图同步换成公司信息中的 Logo。图片**保持原横纵比**，不压成正方形小标。

**idesemi / ecoinf：** 顶栏仍为 36×36 小标；ecoinf 登录页为独立组件，不改。idesemi 登录页仍为白底小标卡。

---

## 2. 图源

与装箱单抬头选取规则一致（默认且已上传；否则任一有文件）：

| 项 | 约定 |
|----|------|
| URL | `GET /api/v1/company-profile/login-logo`（匿名，原文件流，非缩略图） |
| 失败 | 回退内置 `CRM.Web/src/assets/brand/semicore-login-logo.png` |
| 透明 | 须上传真透明 PNG；JPEG 或带底色文件无法在页面上变成透明 |

顶栏与登录页同源，无需公司信息菜单权限即可展示（登录页匿名）。

---

## 3. 布局（仅 semicore）

| 位置 | 约定 |
|------|------|
| 顶栏 | 高度 **24px**，宽度随图、`max-width: 220px`；`object-fit: contain`；无 36×36、无圆角裁切。标题仍在右侧；semicore 去掉与字标重复的「Semicore」前缀（显示 `AI Intelligent System`，**不加粗** `font-weight: 400`）。浏览器标签仍用完整名称 |
| 登录页左侧 | 去掉白底圆角卡；高度约 40–72px，宽度随图、`max-width` 约 420px；透明底叠在品牌区上 |

判定：`usesCompanyProfileWordmarkLogo()` ← `VITE_TENANT_ID === 'semicore'`。

---

## 4. 实现索引

| 层 | 位置 |
|----|------|
| 租户门控 | `CRM.Web/src/config/loginTenant.ts` `usesCompanyProfileWordmarkLogo`、`appBrandHeaderTitle` |
| 顶栏 | `CRM.Web/src/layouts/AppLayout.vue` `.global-logo-mark--wordmark` |
| 登录页 | `CRM.Web/src/views/Auth/LoginView.vue` `.slogan-brand-mark--wordmark`（`.login-view--semicore`） |
| 后端选取 | `CompanyProfileController.GetLoginLogo` / `PickLoginLogoDocumentId` |
| 单测 | `CRM.Web/src/tests/company-profile-wordmark.test.ts` |

无新 API、无新权限、无迁移。

---

*文档维护：2026-09-01*
