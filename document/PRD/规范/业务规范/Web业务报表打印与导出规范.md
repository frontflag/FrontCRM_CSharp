# Web 业务报表 — 打印 / PDF / 邮件 — 项目业务规范

## 适用范围

凡在 **FrontCRM 壳层内**（顶栏、侧栏、多 Tab、左右辅助面板）打开的 **独立报表页**（如采购订单报表），需要支持：

- 浏览器 **打印**（`window.print()` 预览仅含报表版式，不含整站 UI）  
- **导出 PDF**（前端由 DOM 生成）  
- **发送邮件**（后端 SMTP 发附件，可选）

均应按本规范实现。**参考实现**：采购订单报表（路由 `purchase-orders/:id/report`）。

**价税与币别**：采购订单（及销售订单明细）打印与推算须遵守 [销售与采购订单增值税与币别规范](./销售与采购订单增值税与币别规范.md)（非 RMB 明细增值税为 0）。

---

## 一、信息架构与路由

- **独立路由**：报表使用单独 path（如 `/xxx/:id/report`），在 `AppLayout` 的 `router-view` 中渲染，与列表/详情页分离。  
- **入口**：业务列表「打印 / 报表」等操作 `router.push` 到该路由。  
- **页面职责拆分**：  
  - **报表页**（`*ReportPage.vue`）：拉数、工具栏（返回 / 打印 / 导出 PDF / 发邮件）、预览容器、弹窗。  
  - **报表文档**（`*ReportDocument.vue`）：纯展示版式（表头、表格、条款等），**不依赖**壳层布局，便于打印与截图。

---

## 二、DOM 与样式分层（强制）

| 层级 | 建议类名示例 | 作用 |
| ---- | ------------ | ---- |
| 报表页根 | `.{模块}-report-page` | 页背景、内边距；打印时改为白底、去 padding。 |
| 工具栏 / 弹窗 | 加 `no-print` | 打印时隐藏；页内 `@media print` 与全局打印样式双重保险。 |
| 预览外框 | `.print-root` | **仅屏幕预览**：灰底、圆角、内边距；**不是** PDF/打印的内容根。 |
| 可打印 / 可截图根 | `.{模块}-doc`（如 `.po-doc`） | **白底、A4 版式**；**打印与 PDF 仅针对此节点**。 |

**禁止**：对 `.print-root`（含灰底与 padding 的外框）做 `html2canvas` 或作为「整页打印」的唯一内容源，否则 PDF 会出现灰边或版式错误。

**彩色表头 / 品牌色**：在 `@media print` 内对表头单元格等使用 `print-color-adjust: exact` 与 `-webkit-print-color-adjust: exact`，避免浏览器默认去掉背景色。

---

## 三、正确打印（隐藏壳层）

### 3.1 原理

浏览器打印的是**当前文档整页**。若不处理，会把顶栏、侧栏、Tab 栏、左/右辅助面板一并打进纸张。  
做法是：在 **`@media print`** 下用 **body 上的开关类名** 限定作用域，将壳层元素 `display: none`，并把主内容区拉满宽度、去掉滚动裁剪。

### 3.2 与壳层一致的隐藏清单（须与 `AppLayout` 同步）

以下选择器在采购订单实现中已验证，**新增报表时应复制同一套规则**，仅替换 body 类名前缀：

- `.global-top-bar`、`.sidebar`、`.sidebar-collapse-bar`  
- `.app-layout-body > .col-splitter`、`.workspace-cols > .col-splitter`  
- `.aux-panel`（左/右辅助面板）  
- `.main-chrome-bar`（页签与侧栏开关条）  
- `.context-menu`  
- 打印时：`.el-loading-mask`、`.el-overlay`、`.el-dialog__wrapper`（避免蒙层、对话框进入纸张）  

同时设置：`.app-layout-body`、`.workspace-cols`、`.main-content`、`.content-wrapper` 在打印下的宽度与 `overflow: visible`，保证报表不被裁切。

### 3.3 独立样式文件与导入

- 每个（类）报表可有 `print-{模块}.scss`，**仅包含** `@media print` 及上述 body 作用域。  
- 在 `main.scss`（或全局样式入口）**末尾** `@import`，保证覆盖顺序正确。

### 3.4 报表页生命周期（强制）

- **`onMounted`**：`document.body.classList.add('<与 scss 一致的类名>')`，再执行数据加载。  
- **`onBeforeUnmount`**：`document.body.classList.remove(...)`，**避免**离开报表后其他页面打印仍带隐藏规则。  

**注意**：开关类名须在脚本中**显式定义为常量或字面量**，禁止引用未定义标识符，否则挂载报错会导致数据永不加载、预览空白。

### 3.5 页内补充

报表页组件可使用 **scoped** 的 `@media print`：将 `.no-print` 隐藏、`.print-root` 改为白底无 padding，与全局 `print-*.scss` 配合。

---

## 四、导出 PDF（前端）

- **截取节点**：仅 `querySelector('.{模块}-doc')`（与第二节一致）。  
- **实现参考**：`CRM.Web/src/utils/poReportPdf.ts`（`html2canvas` + `jspdf`）；新报表可复用同一工具函数，传入目标元素即可。  
- **权限**：金额等敏感列按权限位在文档组件内显示「—」或与后端约定不下发，与列表页策略一致。

---

## 五、发送邮件（后端）

- **接口**：由后端接收收件人、主题、正文及 **PDF 的 Base64**（或分块策略按项目约定），使用 **MailKit** 发信并挂附件。  
- **配置位置**：仅 **系统 → 公司信息 → 公司邮箱**（`sysparam`：`Company.Profile.SmtpEmail`），字段：`enabled`、`smtpHost`、`smtpPort`、`user`、`password`、`fromAddress`、`fromName`、`useSsl`。**不再读取** `appsettings` 的 `Email` 节。  
- **启用条件**：已保存记录且 `enabled`、SMTP 主机、发件人邮箱有效；若填写了 `user` 则须同时有密码；不满足时接口返回明确错误（见 `SmtpEmailSender`）。  
- **密钥**：授权码仅保存在公司参数中，注意权限（`rbac.manage` 才可改公司信息）。

---

## 六、数据接口约定（推荐）

- **单一聚合接口**（推荐）：`GET /api/v1/{资源}/{id}/report-data`，一次返回报表所需主实体 + 公司档案/默认仓库/印章等，减少前端多次请求与 404 风险。  
- 详情接口字段不足以支撑报表时，在聚合接口中扩展，避免前端拼装多源不一致。

---

## 七、新建报表检查清单

1. [ ] 新建 `XxxReportPage.vue` + `XxxReportDocument.vue`，文档根类名为 `.{模块}-doc`。  
2. [ ] 预览外框 `.print-root` 与文档根分离；工具栏/对话框带 `no-print`。  
3. [ ] 新增 `print-{模块}.scss`（或复用通用 body 类 + 仅替换 `.xxx-doc` 后缀样式），并在 `main.scss` 引入。  
4. [ ] 挂载/卸载时正确添加/移除 **body 打印开关类**，且类名与 SCSS 一致。  
5. [ ] `window.print()` 前确认 `load()` 已成功、`.{模块}-doc` 已渲染。  
6. [ ] PDF 仅对 `.{模块}-doc` 调用渲染工具。  
7. [ ] 若支持邮件：后端配置 `Email`、前端调用发送接口并处理「未启用」等业务提示。  
8. [ ] 打印预览中确认无侧栏、无 Tab 条、无灰底预览框；表头颜色保留。

---

## 八、参考文件（采购订单）

| 类型 | 路径 |
| ---- | ---- |
| 报表页 | `CRM.Web/src/views/RFQ/PurchaseOrderReportPage.vue` |
| 报表文档 | `CRM.Web/src/components/purchaseOrder/PurchaseOrderReportDocument.vue` |
| 打印样式 | `CRM.Web/src/assets/styles/print-purchase-order.scss` |
| 全局样式入口 | `CRM.Web/src/assets/styles/main.scss`（末尾 import 打印样式） |
| PDF 工具 | `CRM.Web/src/utils/poReportPdf.ts` |
| 邮件发送 | `CRM.API/Services/Implementations/SmtpEmailSender.cs`、`CRM.API/appsettings.json` → `Email` |

---

**维护**：若 `AppLayout` 增加新的全局壳层节点（如新的顶条、底栏），所有 `print-*.scss` 须同步增加 `display: none` 或等价处理，并更新本文档第三节清单。
