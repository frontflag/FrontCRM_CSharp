# AI 客户情报调查（customer.intel.lookup）设计与实现

**文档版本：** v1.4  
**更新日期：** 2026-07-14  
**项目名称：** FrontCRM_CSharp  
**状态：** Phase 1 已实现；Phase 2 进行中（客户首页 AI 调查、字段 Key 自动翻译、**输入引导与帮助文档** 已落地）  
**关联文档：** [AI模块架构与实现](./AI模块架构与实现.md) · [AI物料情报查询-设计与实现](./AI物料情报查询-设计与实现.md)

---

## 1. 文档目的与范围

本文档描述通过 **Kimi API（Moonshot 联网搜索）** 查询指定客户公开信息、生成结构化调查报告的产品约定与技术实施方案。

**业务目标：**

- 帮助业务员深入了解客户
- 帮助业务员挖掘商机
- 为交易风控、拓客追单提供可引用的章节化情报（第一期只读，不写回 CRM）

**读者：** 产品、前端、后端、运维。

**不在本文范围：** AI 平台通用编排（见 [AI模块架构与实现](./AI模块架构与实现.md)）；独立路由 `/customers/intel` **已取消**，改为嵌入客户首页 `/custome`（见 §7.6）。

---

## 2. 产品决策汇总（已拍板）

| 主题 | 结论 |
|------|------|
| 未建档客户 | 允许销售随意查询 |
| 报告归属 | **全公司共享** |
| 写回 CRM | **第一期只读**（不导入联系人、不写 `CompanyInfo`） |
| 列表交互 | **单击选中行 + 右侧调查**；双击仍进详情 |
| 报告存储 | **C：最新 + 历史**（默认展示最新，可查看历史时间线） |
| 未建档去重 | `company_name + credit_code` 合并为一份共享报告；支持事后关联 `customer_id`（第二期 UI） |
| 重新调查 | 有 AI 权限即可；24h 内默认走缓存；可「强制刷新」；计入全站 `daily_quota` |
| 第一期 UI | **客户列表/详情右栏「调查」**；**客户首页 `/custome` 嵌入 AI 调查**（对标 RFQ 首页） |
| 首页 AI 缓存 | 客户首页 **默认 `forceRefresh: false`**（90 天缓存）；强制刷新仅在右栏 |
| 首页状态隔离 | 首页使用 **页面内局部状态**，不写入 `customerIntelLookup` store |
| 第一期章节 | **8 章 MVP**（见 §5.1）；**已扩展为 13 章**（见 §5.2、§5.4） |
| 列表选中态 | 明显行高亮；从详情返回列表 **session 级记住**选中；无选中时右栏 **空态引导** |
| CRM 对照 | 调查区顶部展示简要对照条（名称、信用代码、业务员、黑名单/冻结） |
| 黑名单/冻结客户 | **允许**调查；顶部醒目提示 CRM 状态 |
| 权限 | 单权限 `biz.ai.customer_intel.lookup`：能查、能看、能刷新 |
| 联系方式/关键人 | 允许 **复制**；第一期不一键导入 |
| 免责声明 | 报告顶部 **固定展示**（不做首次勾选） |
| AI 调用缓存 TTL | **90 天** |
| 查询输入 | **名称必填**；信用代码、地区等选填；意图第一期固定「全面了解」 |
| Mock | **需要**，与 `material.intel.lookup` 一致，无 Key 可联调 |
| 报告语言 | 简体中文 |
| 强制刷新 | 按钮「重新调查」；24h 内有缓存时二次确认「将消耗 AI 配额」 |
| 历史报告 | 不删除；第一期不做归档策略 |
| 调查完成通知 | 第一期不做 |
| 章节复制 | 初版支持「复制本章」；**当前 UI 已移除**（第二期可重做） |
| 配额 | 与物料情报共用 `ai_global_config.daily_quota_limit` |

---

## 3. 产品概述

### 3.1 用户场景

销售/经理在以下入口发起 **AI 客户调查**，获取章节化公开情报：

- **客户首页**（`/custome`）：输入企业名称，点击「AI 调查」（未建档亦可）
- **客户列表/详情右栏**：单击选中已建档客户，在「调查」页签查看或刷新报告

- 基础档案、经营业务、企业规模
- 合规风险、商机线索、联系方式、发展历程
- AI 综合评估与行动建议

**与 `entity.parse.customer` 的区别：** 实体解析是「粘贴文本 → 预填新建客户」；客户情报是「联网调查 → 结构化只读报告」。

### 3.2 入口与权限

| 入口 | 路由 | 说明 |
|------|------|------|
| **客户首页 AI 调查** | `/custome`（`CustomerHome`） | 药丸搜索条内「AI 调查」；**对标 RFQ 首页** `RFQHome` |
| 客户列表右栏 | `/customers`（`CustomerList`） | 单击选中客户 → 右栏「调查」页签 |
| 客户详情右栏 | `/customers/:id`（`CustomerDetail`） | 同上，自动绑定当前客户 |
| Debug | `/debug/customer-intel` | 契约校验、Mock/实网对比 |

> 路由名 `custome` 为历史拼写，与侧栏「客户管理」首页一致，非 `/customers`。

| 项 | 值 |
|----|-----|
| 场景码 | `customer.intel.lookup` |
| 权限码 | `biz.ai.customer_intel.lookup` |

### 3.3 交互要点

**客户首页（`/custome`）**

```
药丸搜索条
  [🔍 企业名称输入] [AI 调查] [搜索客户] [进入列表查询]
  Enter → 有 AI 权限则 AI 调查；否则跳转客户列表搜索

AI 调查中
  加载文案 + 秒级计时（与 RFQ 首页一致）

结果区（页面内居中卡片）
  CustomerIntelResultPanel（layout=centered，show-close）
  ├─ 无 CRM 对照条（首页不绑定客户）
  ├─ 标题 + 缓存/实时标签 + 全部展开/收起 + [关闭]
  ├─ 固定免责声明
  └─ 13 章卡片

关闭结果 / 再次调查
  关闭清空页面结果；再次点击「AI 调查」仍 forceRefresh=false（走 90 天缓存）

统计卡片
  调查结果展示时，下方客户/应收/待出库概要卡片仍可见（与 RFQ 首页一致）
```

**客户列表 / 详情右栏**

```
客户列表
  单击行 → 行高亮 + 右栏加载该客户「最新报告」（无则空态 +「发起调查」）
  双击行 → 进入客户详情（不变）

右栏「调查」
  ├─ CRM 对照条（公司名琥珀色强调、信用代码、业务员、黑名单/冻结）
  ├─ [发起调查] / [重新调查] + [强制刷新]
  ├─ [历史报告 ▼]（多条时）
  ├─ 报告标题行：客户情报调查 + 缓存/实时标签 + 全部展开/收起（↑↓）
  ├─ 固定免责声明
  └─ 13 章独立卡片（摘要 + Key-Value；单章/全部收起展开）
```

**已取消：** 独立路由 `/customers/intel` Google 式搜索页；未建档查询能力由客户首页承担。

---

## 4. 总体架构

```
┌──────────────────────────────────────────────────────────────────┐
│  CustomerHome（/custome）— 页面内局部状态，不经过右栏 store        │
│  输入 companyName → customerIntelApi.investigate(forceRefresh=false)│
│  CustomerIntelResultPanel（centered + show-close）                │
└────────────────────────────┬─────────────────────────────────────┘
                             │ POST /api/v1/customer-intel-reports/investigate
┌──────────────────────────────────────────────────────────────────┐
│  CustomerList / CustomerDetail + AppLayout 右栏                   │
│  CustomerIntelPanel → customerIntelLookup store                   │
│  CustomerIntelResultPanel（embedded）+ CRM 对照条                 │
└────────────────────────────┬─────────────────────────────────────┘
                             │ POST /api/v1/customer-intel-reports/investigate
                             │ GET  /api/v1/customers/{id}/intel-reports
┌────────────────────────────▼─────────────────────────────────────┐
│  AiOrchestrator（customer.intel.lookup）                          │
│  · 权限 / 限流 / PG 缓存（90 天，指纹见 §4.2）                    │
│  · Moonshot 联网 $web_search                                      │
│  · 解析 JSON → ai_invocation_log                                 │
└────────────────────────────┬─────────────────────────────────────┘
                             │ 实时调查成功后写入
┌────────────────────────────▼─────────────────────────────────────┐
│  customer_intel_report（业务存档：最新 + 历史，全公司共享）        │
└──────────────────────────────────────────────────────────────────┘
```

**双层存储：**

| 层 | 表 | 用途 |
|----|-----|------|
| AI 调用缓存 | `ai_invocation_cache` | 同指纹 90 天内复用 LLM 响应，省钱提速 |
| 业务报告存档 | `customer_intel_report` | 全公司可见、历史时间线、关联客户 |

---

## 5. 输出契约：章节节点

### 5.1 第一期（8 章 MVP）

| `id` | 标题 | 用途 |
|------|------|------|
| `registry` | 基础档案 | 建档核对、身份校验 |
| `business` | 经营业务 | 产品匹配、RFQ 方向 |
| `scale` | 企业规模 | 客户分级、产能判断 |
| `compliance_risks` | 合规与司法风险 | **交易风控** |
| `opportunities` | 商机线索 | **拓客追单** |
| `contacts` | 联系方式 | 拜访、复制电话邮箱 |
| `timeline` | 发展历程 | 扩产/新品节点 |
| `ai_assessment` | AI 综合评估 | 评分、拜访策略、下一步 |

### 5.2 第二期补全（5 章，**已实现契约与 Prompt**）

| `id` | 标题 |
|------|------|
| `ownership` | 股权结构 |
| `certifications` | 资质与认证 |
| `market_risks` | 经营与市场风险 |
| `procurement_signals` | 采购与供应链信号 |
| `key_people` | 关键人与组织 |

### 5.3 完整章节顺序（13 章，`schema_version` **1.1**）

```
registry → ownership → business → scale → certifications → timeline → contacts
→ compliance_risks → market_risks → procurement_signals → opportunities
→ key_people → ai_assessment
```

### 5.4 顶层 JSON 结构

```json
{
  "meta": {
    "schema_version": "1.1",
    "company_name_primary": "string",
    "company_name_aliases": ["string"],
    "credit_code": "string|null",
    "region": "string|null",
    "generated_at": "ISO8601",
    "data_freshness": "high|mixed|low",
    "overall_confidence": "high|medium-high|medium|low"
  },
  "query": {
    "company_name": "string",
    "credit_code": "string|null",
    "region": "string|null",
    "intent": "full"
  },
  "sections": [
    {
      "id": "registry",
      "title": "基础档案",
      "summary": "string",
      "confidence": "high|medium|low",
      "content": {},
      "sources": [{ "type": "string", "title": "string", "url": "string|null", "date": "string|null" }]
    }
  ],
  "relations": {
    "section_order": ["registry","ownership","business","scale","certifications","timeline","contacts","compliance_risks","market_risks","procurement_signals","opportunities","key_people","ai_assessment"],
    "for_risk_control": ["registry","ownership","compliance_risks","market_risks"],
    "for_sales_followup": ["opportunities","procurement_signals","timeline","key_people","ai_assessment"]
  },
  "disclaimer": "本信息来自公开渠道及 AI 整理，仅供参考，请人工核实后用于业务决策。"
}
```

**Prompt 硬性要求：**

1. **必须输出上述 13 个 `sections`**；无数据则空对象/空数组 + `confidence: low`（旧 8 章报告仍可展示，校验仅 warn）
2. `compliance_risks.checks[].count` 查不到填 `null`，`status: "unknown"`，禁止编造
3. `ai_assessment.dimensions[].basis_section_ids` 必须引用事实章节
4. 禁止输出 Markdown，仅 JSON

**前端契约文件：** `CRM.Web/src/utils/customerIntelSchema.ts`（章节 ID、中文标签、字段标签自动翻译 §7.7、契约校验、章节排序）。

**Prompt 升级 SQL：** `scripts/ensure_customer_intel_lookup_schema_v2_postgresql.sql`；迁移 `20260714200000_CustomerIntelLookupSchemaV2.cs`。

---

## 6. 后端设计

### 6.1 AI 场景配置（`ai_scenario`）

| 字段 | 值 |
|------|-----|
| `code` | `customer.intel.lookup` |
| `name` | AI 客户情报调查 |
| `provider_code` | `moonshot` / `mock` |
| `model` | `kimi-k2.5` |
| `enable_web_search` | `true` |
| `cache_ttl_seconds` | `7776000`（90 天） |
| `cache_key_fields` | `["company_name","credit_code"]` |
| `allowed_input_fields` | `["company_name","credit_code","region","customer_id"]` |
| `permission_code` | `biz.ai.customer_intel.lookup` |
| `rate_limit_per_user_per_min` | `10` |

**Invoke 请求体示例：**

```json
{
  "scenarioCode": "customer.intel.lookup",
  "input": {
    "company_name": "日月元科技（深圳）有限公司",
    "credit_code": "91440300676691745X",
    "region": "广东省深圳市宝安区",
    "customer_id": "uuid-or-null"
  },
  "bizType": "CUSTOMER",
  "bizId": "customer_id-or-null",
  "triggerType": "manual",
  "forceRefresh": false
}
```

**缓存与强制刷新：**

- `forceRefresh=false`：优先 `ai_invocation_cache`（90 天）
- `forceRefresh=true`：跳过缓存调 LLM；24h 内建议前端二次确认
- 实时成功后 **始终新增** `customer_intel_report` 一行（历史留存）

**常量（待增）：** `AiCodes.cs` → `CustomerIntelLookup`、`AiPermissionCodes.CustomerIntelLookup`。

**种子 SQL（待建）：** `scripts/ai_customer_intel_lookup_postgresql.sql`。

### 6.2 业务表 `customer_intel_report`

| 列 | 类型 | 说明 |
|----|------|------|
| `id` | uuid | PK |
| `customer_id` | uuid nullable | 已建档客户；未建档为空 |
| `company_name` | varchar | 查询主名称 |
| `credit_code` | varchar nullable | 去重键之一 |
| `query_fingerprint` | varchar | SHA256(company_name + credit_code)，唯一索引（非 customer 维度去重） |
| `report_json` | jsonb | 完整契约 JSON |
| `schema_version` | varchar | 如 `1.0` |
| `source` | varchar | `cache` / `live` |
| `invocation_log_id` | uuid nullable | 关联 `ai_invocation_log` |
| `is_latest` | boolean | 同 fingerprint 下仅一条为 true |
| `created_by` | varchar | 发起人 userId |
| `created_at` | timestamptz | UTC |

**索引建议：**

- `UNIQUE (query_fingerprint) WHERE is_latest = true` — 快速取最新
- `(customer_id, created_at DESC)` — 按客户查历史
- `(query_fingerprint, created_at DESC)` — 按公司查历史

**写入规则：**

1. 新报告插入前，将同 `query_fingerprint` 的旧记录 `is_latest=false`
2. 新记录 `is_latest=true`
3. 若调用时带 `customer_id`，同时写入；未建档仅写 `company_name`/`credit_code`
4. 第二期「关联到客户」：更新 `customer_id`，不改动 `report_json`

### 6.3 业务 API（第一期）

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/v1/ai/invoke` | 发起调查（已有，新场景码） |
| GET | `/api/v1/customers/{id}/intel-reports/latest` | 已建档客户最新报告 |
| GET | `/api/v1/customers/{id}/intel-reports` | 历史列表（摘要） |
| GET | `/api/v1/customer-intel-reports/{reportId}` | 报告详情 |
| GET | `/api/v1/customer-intel-reports/latest?companyName=&creditCode=` | 未建档取最新 |

**权限：** 以上均要求 `biz.ai.customer_intel.lookup`；数据 **全公司共享**（不按发起人过滤）。

**控制器建议：** `CustomerIntelReportsController` 或扩展 `CustomersController`。

---

## 7. 前端设计

### 7.1 右栏集成（对标物料情报）

| 文件 | 职责 | 对标 |
|------|------|------|
| `layouts/AppLayout.vue` | 右栏页签 `r-customer-intel`、路由显隐 | 物料情报 `r-material` |
| `components/Customer/CustomerIntelPanel.vue` | 调查页签容器：权限/空态、工具栏、历史、加载态 | `RfqItemMaterialPanel.vue` |
| `components/Customer/CustomerIntelResultPanel.vue` | 报告渲染：标题行、免责、13 章卡片、收起展开 | `MaterialIntelResultPanel.vue` |
| `components/Customer/CustomerIntelCrmContextBar.vue` | CRM 对照条 | 新建 |
| `components/Customer/CustomerIntelSectionContent.vue` | 章节 content → 中文 Key-Value | 新建 |
| `stores/customerIntelLookup.ts` | 绑定客户、拉取/调查/历史 | `materialIntelLookup.ts` |
| `utils/customerIntelSchema.ts` | 13 章契约、字段 Key 自动翻译、校验、排序 | `materialIntelSchema.ts` |
| `api/customerIntel.ts` | `investigate` / `latest` / `list` / `getById` | `materialIntel.ts` |

**AppLayout 挂载：**

```vue
<CustomerIntelPanel v-show="showCustomerIntelPanel" />
```

- `showCustomerIntelPanel`：`rightActiveTabId === 'r-customer-intel'` 且路由为 `CustomerList` / `CustomerDetail`
- 客户路由 `rightTabs` 首项：`{ id: 'r-customer-intel', labelKey: 'layout.auxTabs.customerIntel' }`（文案「调查」）
- 列表单击客户时 `workspaceLayout.setRightActiveTab('r-customer-intel')` 并展开右栏

### 7.2 列表选中与上下文绑定

| 行为 | 实现 |
|------|------|
| 单击行 | `CustomerList.onCustomerRowClick` → `customerIntelLookupStore.bindContext(...)` |
| 行高亮 | `customerListRowClassName` → `crm-list-row--clicked`（对比 `boundCustomerId`） |
| 双击 | 仍 `router.push` 详情（不变） |
| 返回列表 | `tryRestoreIntelSelection()` 读 `sessionStorage['customer-intel-selected-id']` 恢复绑定 |
| 详情页 | `CustomerDetail` 加载后 `bindContext`（`customerId` + 公司名等） |
| 无选中 | `CustomerIntelPanel` 空态：`selectCustomerHint` |

**`CustomerIntelCrmContext` 字段：**

| 字段 | 来源 |
|------|------|
| `customerId` | 列表行 / 详情 `id` |
| `companyName` | `customerName` 或 `customerShortName` |
| `creditCode` | `unifiedSocialCreditCode` |
| `region` | `city` / `region` |
| `salesPersonName` | 业务员姓名 |
| `blackList` / `disenableStatus` | CRM 黑名单 / 冻结 |

### 7.3 客户列表右栏「调查」页签——布局与状态（**已实现**）

#### 7.3.1 信息架构（自上而下）

```
┌─ CustomerIntelPanel（右栏页签根）────────────────────────────┐
│  [无权限] / [请选择客户] 空态                                  │
│  ┌─ CustomerIntelCrmContextBar ─────────────────────────────┐ │
│  │  深圳市××有限公司          （14px 琥珀色加粗）            │ │
│  │  信用代码 … · Philip · [黑名单] [已冻结]                  │ │
│  └──────────────────────────────────────────────────────────┘ │
│  [发起调查|重新调查]  [强制刷新]                               │
│  历史报告 [下拉]（仅 historyReports.length > 1）               │
│  加载中 / 错误+重试 / 暂无报告空态                             │
│  ┌─ CustomerIntelResultPanel ──────────────────────────────┐ │
│  │  客户情报调查 [实时|缓存]                    [↑ 全部收起] │ │
│  │  ⚠ 免责声明条                                            │ │
│  │  ┌ 章节卡片 registry ──────────────────────── [↑] ───┐  │ │
│  │  │  摘要段落（13px 次要色）                           │  │ │
│  │  │  （空 1.6em 行高）                                 │  │ │
│  │  │  Key · Value 列表（CustomerIntelSectionContent）   │  │ │
│  │  └───────────────────────────────────────────────────┘  │ │
│  │  … 共 13 章（独立卡片，默认全部展开）                    │ │
│  └──────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
```

#### 7.3.2 设计需求（UI 拍板与迭代）

| 区域 | 需求 | 实现要点 |
|------|------|----------|
| CRM 对照条 | 突出当前调查对象 | 去掉「CRM」「业务员」前缀；公司名 **14px + `$color-amber`**；第二行信用代码、业务员姓名、黑名单/冻结标签 |
| 工具栏 | 发起/刷新调查 | 主按钮「发起调查」/「重新调查」；有报告时显示「强制刷新」；强制刷新 `ElMessageBox` 二次确认 |
| 历史 | 多份报告切换 | `el-select` 展示 `创建时间 · 调查人`；切换调用 `selectReportById` |
| 报告标题行 | 状态 + 全局折叠 | 左：**标题 + 缓存/实时标签**（标签紧跟标题）；右：**↑/↓ 全部收起/展开**（`ArrowUp`/`ArrowDown` + tooltip） |
| 免责声明 | 固定可见 | `el-alert` warning，不可关闭 |
| 章节卡片 | 独立面板、非手风琴 | 每章 `article` 圆角卡片；**默认全部展开**；切换报告时重置展开状态 |
| 章节摘要 | 与 KV 区分 | 摘要 `line-height: 1.6`；摘要与 KV 间距 **`margin-top: 1.6em`**（1 行空白） |
| 置信度 | 章节级标签 | 文案「高/中/低」；`el-tooltip` 悬停说明；颜色 success/warning/info |
| 章节内容 | 中文 Key-Value | `buildCustomerIntelContentView` 扁平化 `content`；字段 Key **自动中文化**（§7.7）；`http(s)://` 值自动外链 |
| 枚举值 | 英文转中文 | `CUSTOMER_INTEL_VALUE_LABELS`：`high/medium/low/unknown/clear` 等 |
| 收起展开 | 单章 + 全部 | 章节头可点；右侧 ↑ 收起 / ↓ 展开；标题行右侧控制全部章节 |
| 章节复制 | 初版有、现移除 | 无「复制本章」按钮（i18n 键保留） |

#### 7.3.3 组件职责与数据流

```
CustomerList 单击
    → bindContext(CustomerIntelCrmContext)
    → loadLatest(customerId) + loadHistory()   // watch boundCustomerId
    → CustomerIntelPanel 渲染（仅展示当前客户槽位 bound* 状态）

用户点击「发起调查」
    → store.investigate({ force })   // 快照 context，按 customerId 写入 slotByCustomerId
    → POST /api/v1/customer-intel-reports/investigate（后台可继续，切换行不取消）
    → boundCurrentReport 更新 → CustomerIntelResultPanel :data="reportData"
```

**右栏切换行（对齐需求明细「物料」页签）：** `customerIntelLookup` 按 `customerId` 分槽（`slotByCustomerId`、`investigatingCustomerIds`、`inFlightInvestigate`）。客户 A 调查中切换到 B 时，右栏展示 B 的状态并可对 B「发起调查」；A 的请求在后台完成后写入 A 的槽位。

```
CustomerIntelResultPanel
    → extractCustomerIntelSections(data)  // 按 relations.section_order 或 13 章默认序排序
    → 每章 section.summary + CustomerIntelSectionContent(section.content)
```

**收起展开状态（仅前端，不持久化）：**

- `collapsedSectionIds: Set<string>`，键为 `section.id` 或 `section-${idx}`
- `toggleSection` / `toggleAllSections`；`watch(props.data)` 清空 Set

#### 7.3.4 章节内容渲染（通用 KV，非专用组件）

当前 Phase 1/2 右栏采用 **统一 Key-Value 渲染**（`CustomerIntelSectionContent`），未按章节约束独立组件（时间线、风险徽章等为后续增强）。

| 能力 | 实现位置 |
|------|----------|
| 章节标题 | `CUSTOMER_INTEL_SECTION_LABELS[id]` |
| 字段标签 | `resolveCustomerIntelFieldLabel`（§7.7 自动翻译） |
| 嵌套对象/数组 | `collectContentRows` → 顶层 `rows` + `listBlocks`（如「股东 1」「里程碑 1」） |
| 空内容 | 显示 `—` |

> 字段 Key 自动翻译细则见 **§7.7**。

#### 7.3.5 样式约定

| 元素 | 样式 |
|------|------|
| 对照条背景 | `var(--crm-accent-004)` 系浅色底 + 细边框 |
| 章节卡片 | `$layer-1` 底、`$border-panel` 边框、`border-radius: 10px` |
| KV 标签 | 12px `$text-muted`；值 13px `$text-primary` |
| 右栏根 | `customer-intel-side-panel` 可滚动 `overflow: auto` |

### 7.4 权限与空态

| 条件 | 展示 |
|------|------|
| 无 `biz.ai.customer_intel.lookup` | `noPermission` |
| 有权限未绑定客户 | `selectCustomerHint` |
| 已绑定无报告 | 工具栏 + `emptyReport` |
| 调查中 | Loading + 已耗时秒数 |
| 加载失败 | 错误文案 +「重试」 |

### 7.5 章节 UI 增强（待做，非阻塞）

| 章节 | 建议组件 |
|------|----------|
| `registry` | 键值表格（已用通用 KV） |
| `compliance_risks` | 风险徽章表 + `attention_items` 高亮 |
| `contacts` | 地址表 + 复制按钮 |
| `timeline` | 时间线组件 |
| `ai_assessment` | 星级维度 + 拜访策略折叠 |

### 7.6 客户首页 `/custome` AI 调查——布局与实现（**已实现**）

对标 [AI物料情报查询-设计与实现](./AI物料情报查询-设计与实现.md) 中 RFQ 首页 `RFQHome.vue` 的嵌入模式：在模块首页药丸搜索条内提供 AI 能力，结果在页面主区居中展示，**不占用右栏、不污染右栏 store**。

#### 7.6.1 产品定位与决策

| 主题 | 结论 |
|------|------|
| 页面 | `CustomerHome.vue`，路由 `/custome`（`name: CustomerHome`） |
| 与独立页关系 | **不建** `/customers/intel`；首页承担「按名称调查未建档企业」 |
| 对标 | `RFQHome.vue` + `MaterialIntelResultPanel` |
| 输入 | 搜索框 **企业名称必填**（`keyword.trim()`）；信用代码/地区首页 v1 不传 |
| 缓存策略 | **`forceRefresh: false`**；同公司 90 天内重复点击走缓存 |
| 强制刷新 | **仅右栏**「重新调查 / 强制刷新」提供；首页不提供 |
| CRM 上下文 | **不展示** `CustomerIntelCrmContextBar`；`customerId` 固定 `null` |
| 历史报告 | 首页 v1 **不做** 历史下拉；需历史请进列表选中客户用右栏 |
| 统计区 | 有调查结果时，下方客户/应收/待出库概要卡片 **仍展示** |

#### 7.6.2 信息架构（自上而下）

```
┌─ CustomerHome（/custome）───────────────────────────────────────┐
│  药丸搜索条（customer-home__pill）                               │
│  [🔍 输入] [AI 调查] [搜索客户] [进入列表查询]  │ [新建客户 ▼]   │
│  AI 调查提示（药丸下方，左对齐，有 AI 权限时显示）                 │
│                                                                  │
│  （调查中）Loading + 「正在调查客户情报…（N 秒）」                 │
│                                                                  │
│  ┌─ CustomerIntelResultPanel（layout=centered）──────────────┐  │
│  │  客户情报调查 [实时|缓存]        [↑ 全部收起] [关闭]       │  │
│  │  ⚠ 免责声明                                                │  │
│  │  13 章卡片（与右栏同一渲染组件，无 CRM 对照条）             │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  客户 / 应收款 / 待出库 统计卡片（与无结果时相同）                 │
└──────────────────────────────────────────────────────────────────┘
```

**药丸按钮文案（i18n `customerHome.*`）：**

| 控件 | 中文 | 说明 |
|------|------|------|
| 主按钮 | AI 调查 | 需 `biz.ai.customer_intel.lookup` |
| 次按钮 | 搜索客户 | 跳转 `CustomerList` 带 `searchTerm`（原「搜索」） |
| 链接 | 进入列表查询 | 空条件进列表 |

#### 7.6.3 设计需求（UI / 交互）

| 区域 | 需求 | 实现要点 |
|------|------|----------|
| AI 按钮 | 无权限不可用 | `el-tooltip` + `disabled`；文案 `aiSearchNoPermission` |
| Enter 键 | 智能分流 | `onSearchEnter`：有 AI 权限 → `handleAiSearch`；否则 → `handleSearch` |
| 空输入 | 阻止调查 | `ElMessage.warning(aiSearchNeedCompany)` |
| 加载态 | 秒级计时 | `setInterval` 1s；文案 `customerHome.aiLoading` |
| 结果卡片 | 居中、可关闭 | `CustomerIntelResultPanel`：`layout="centered"`、`show-close`；样式对齐 `MaterialIntelResultPanel`（`max-width: 2/3`、圆角面板） |
| 关闭 | 清空页面结果 | `@close` → `clearAiResult()`；不影响右栏、不写 store |
| 搜索客户 | 列表查询 | `buildCustomerListQuery({ searchTerm: keyword })` → `CustomerList` |
| 再次调查 | 缓存优先 | 同名称重复点「AI 调查」仍 `forceRefresh: false` |
| **输入引导** | 提升匹配准确度 | 药丸下方 `customer-home__ai-hint`；i18n `customerHome.aiSearchHint`；**左对齐**、`white-space: pre-line` 三行展示；仅 `canAiIntelLookup` 时显示 |

**页面内提示文案（`customerHome.aiSearchHint`，zh-CN）：**

```
AI 调查提示：
请尽量输入工商登记全称（如「华为技术有限公司」），避免仅用简称或品牌名；
若知道统一社会信用代码或所在地区，可在名称后一并填写，有助于提高匹配准确度。
```

#### 7.6.4 组件职责与数据流

```
CustomerHome.vue（页面局部 ref，不用 customerIntelLookup store）
├── keyword                          // 搜索框双向绑定
├── aiLoading / aiLoadingSeconds     // 调查中 + 计时
├── aiResultData / aiFromCache       // 报告 JSON + 是否缓存
│
├── handleAiSearch()
│     → customerIntelApi.investigate({
│           companyName: keyword.trim(),
│           customerId: null,
│           forceRefresh: false
│       })
│     → aiResultData = result.report.report
│     → aiFromCache = result.fromCache
│
├── handleSearch()                   // 「搜索客户」→ 列表
├── onSearchEnter()                  // Enter 分流
└── clearAiResult()                  // 关闭结果面板

CustomerIntelResultPanel.vue（复用，双模式）
├── layout="embedded"   // 右栏 CustomerIntelPanel（默认）
└── layout="centered"   // 客户首页：外边距、面板底、showClose + emit('close')
```

**与右栏差异：**

| 维度 | 客户首页 | 右栏「调查」 |
|------|----------|--------------|
| 状态 | 页面 `ref` | `customerIntelLookup` store |
| `customerId` | 始终 `null` | 列表/详情绑定 |
| `forceRefresh` | 固定 `false` | 支持强制刷新 |
| CRM 对照条 | 无 | `CustomerIntelCrmContextBar` |
| 历史报告 | 无 | `el-select` 切换 |
| 面板布局 | `centered` + 关闭 | `embedded` |
| API | `POST .../investigate` | 同上 + `GET .../intel-reports` |

#### 7.6.5 样式约定（首页专有）

| 元素 | 样式 / 类名 |
|------|-------------|
| AI 主按钮 | `customer-home__pill-btn`（渐变主色，与 RFQ 一致） |
| 列表搜索次按钮 | `customer-home__pill-btn--secondary`（灰底描边） |
| 无权限包裹 | `customer-home__pill-btn-wrap`（避免 disabled 按钮不触发 tooltip） |
| **输入引导** | `customer-home__ai-hint`：`font-size: 12px`、`color: $text-muted`、`text-align: left`、`white-space: pre-line`、`padding: 0 16px`；与药丸同宽（在 `pill-wrap` 内） |
| 加载行 | `customer-home__ai-loading`：`margin: -64px auto 48px`，`max-width: 2/3` |
| 结果面板 | `CustomerIntelResultPanel` 非 embedded 时：`padding 20px 22px`、`border-radius: 14px` |

#### 7.6.6 右侧「帮助」页签

客户首页（及客户列表/详情）右侧辅助栏 **「帮助」** 由 `HelpManualPanel` 按路由加载 Markdown：

| 项 | 说明 |
|----|------|
| 源文件 | `help/pages/客户_MENU_CUSTOMER_HOME.md` |
| 注册 | `help/menu-registry.json` → `MENU_CUSTOMER_HOME`（`CustomerHome`、`CustomerList`、`CustomerDetail` 等） |
| 同步 | `scripts/sync-help.mjs` → `CRM.Web/public/help/` |
| 内容重点 | **AI 调查使用方法**、输入建议、首页与右栏调查对比、列表操作 |

用户可见操作说明与 §7.6、§7.7 技术约定保持一致；首页三行提示为简版，帮助页为完整版。

#### 7.6.7 首页 v1 明确不做

- 历史报告下拉（`GET history?companyName=` 未接首页）
- 根据名称自动匹配 CRM 客户并关联 `customerId`
- 首页「强制刷新」与配额二次确认
- 独立路由 `/customers/intel`

### 7.7 字段 Key 自动翻译——设计需求与实现（**已实现**）

AI 返回的 `sections[].content` 中，字段名（Key）可能为英文 snake_case、camelCase 或带空格短语。产品要求：**优先显示中文标签**，减少业务员阅读成本；同时 **不隐藏数据行**，Value 始终展示。

#### 7.7.1 产品规则（拍板）

| 规则 | 说明 |
|------|------|
| 适用范围 | 右栏「调查」、客户首页 `/custome` 结果区——凡经 `CustomerIntelSectionContent` 渲染的 KV |
| 纯中文 Key | **原样保留**（如 AI 直接返回 `企业名称`） |
| 可译英文 Key | 显示中文标签（如 `english name` → `英文名称`，`rd_investment` → `研发投入`） |
| 部分可译 | **中文 + 英文混排**（如 `foo employees` → `foo 员工人数`） |
| 完全译不出 | **保留英文**；下划线 / camelCase 转为空格（如 `some_field` → `some field`） |
| Value | **始终展示**，与 Key 是否译出无关；空值显示 `—` |
| 不隐藏行 | 译不出时 **不删除** 该 Key-Value 行，仅标签侧按上表处理 |
| 枚举值 | 字段 **值** 中的 `high`/`medium`/`low` 等走 `CUSTOMER_INTEL_VALUE_LABELS`，与 Key 翻译独立 |
| 维护策略 | 高频字段可追加到 `CUSTOMER_INTEL_FIELD_LABELS`；其余依赖自动拆分 + 词根词典，**无需逐个手工加** |

#### 7.7.2 渲染链路

```
sections[].content (JSON 对象)
    → buildCustomerIntelContentView(content)
        → collectContentRows() 递归扁平化
            → 每个字段 k 调用 resolveCustomerIntelFieldLabel(k) 作为 row.label
            → 标量 / URL / value+unit / 对象列表 → rows 或 listBlocks
    → CustomerIntelSectionContent.vue
        → <dt>{{ row.label }}</dt><dd>{{ row.value }}</dd>
```

**调用入口（统一）：** 右栏 `CustomerIntelResultPanel`、首页内嵌同一 `CustomerIntelResultPanel` → `CustomerIntelSectionContent`，无第二套标签逻辑。

#### 7.7.3 翻译算法（`resolveCustomerIntelFieldLabel`）

实现文件：`CRM.Web/src/utils/customerIntelSchema.ts`。

```
输入 raw Key
  ├─ 空 → 「字段」
  ├─ 纯中文（含汉字、无拉丁字母）→ 原样返回
  ├─ 精确词典命中 → 返回中文
  │     CUSTOMER_INTEL_FIELD_LABELS（客户情报专用，含 13 章常见字段）
  │     FIELD_LABEL_ZH（自 jsonLabels.ts，物料/通用 JSON 字段复用）
  ├─ 规范化后精确命中
  │     normalizeCustomerIntelFieldKey：camelCase → snake_case、空格/连字符 → _
  ├─ 自动拆分翻译 translateEnglishFieldKeyTokens
  │     splitFieldKeyTokens → token 列表
  │     贪心短语匹配（2～4 个 token 拼成 phrase_key 查词典）
  │     单词回退 lookupSingleFieldToken（含复数：employees → employee）
  │     未命中 token → 保留英文原词
  │     composeFieldLabelParts：全中文无空格拼接；含英文则空格连接
  └─ 兜底 humanizeEnglishFieldKey(raw)（下划线/camelCase → 空格）
```

**核心函数：**

| 函数 | 职责 |
|------|------|
| `resolveCustomerIntelFieldLabel` | 对外入口，报告 KV 标签唯一解析点 |
| `normalizeCustomerIntelFieldKey` | Key 规范化（camelCase、空格、中英混合） |
| `splitFieldKeyTokens` | 拆分为单词 token |
| `lookupFieldLabelExact` | 整词 / 整短语精确查表 |
| `lookupSingleFieldToken` | 单词 + 复数形式查表 |
| `translateEnglishFieldKeyTokens` | 贪心短语 + 单词自动组合 |
| `composeFieldLabelParts` | 全中文紧凑拼接；中英混排用空格 |
| `humanizeEnglishFieldKey` | 完全无法结构化翻译时的英文可读化 |

#### 7.7.4 词典分层

| 层级 | 常量 | 说明 |
|------|------|------|
| 专用完整字段 | `CUSTOMER_INTEL_FIELD_LABELS` | 如 `english_name`、`established_date`、`rd_investment`、`registered_capital` |
| 英文词根 | `FIELD_LABEL_FALLBACK_WORDS` | 拆分后单词级回退，如 `employee`、`investment`、`category`、`ceo` |
| 通用 JSON 字段 | `FIELD_LABEL_ZH`（`jsonLabels.ts`） | 与物料情报等模块复用，如 `name`、`description`、`url` |
| 字段值枚举 | `CUSTOMER_INTEL_VALUE_LABELS` | 仅翻译 **值**，非 Key |

**扩展方式：** 若某英文 Key 反复出现且自动拆分结果不理想，在 `CUSTOMER_INTEL_FIELD_LABELS` 增加 `snake_case` 整词映射即可（优先级最高）。

#### 7.7.5 展示示例

| AI 返回 Key | 左侧标签（label） | 右侧 Value |
|-------------|-------------------|------------|
| `english name` | 英文名称 | Huawei Technologies Co., Ltd. |
| `established_date` | 成立日期 | 1987-09-15 |
| `category` | 业务类别 | 运营商业务 |
| `employees` | 员工人数 | 约 20.7 万人… |
| `rd investment` | 研发投入 | 约 1647 亿元… |
| `企业名称` | 企业名称 | （原样） |
| `foobar` | foobar | （某数值，行仍展示） |
| `some_unknown_field` | some unknown field | （行仍展示） |

#### 7.7.6 明确不做

- 不调用外部翻译 API（离线词典 + 规则，无网络依赖）
- 不因 Key 译不出而隐藏整行 Key-Value
- 不在 Prompt 层强制 AI 只输出中文字段名（前端兼容英文 Key）

---

## 8. 分期实施计划

### Phase 1（本期）

- [x] `customer.intel.lookup` 场景 + Mock + 权限 SQL
- [x] `customer_intel_report` 迁移 + 写入服务
- [x] 业务 API（latest / history / by-query）
- [x] `CustomerIntelPanel` + 结果渲染（8 章）
- [x] `CustomerList` / `CustomerDetail` 右栏 + 单击选中
- [x] `DebugCustomerIntel.vue`
- [x] 文档与 `AiCodes` 常量

### Phase 2

- [x] **客户首页 `/custome` 嵌入 AI 调查**（对标 RFQ 首页；取消独立 `/customers/intel`）
- [x] 客户首页 **AI 调查输入引导**（药丸下三行提示 + 帮助页）
- [x] 契约补 5 章 + Prompt 升级（`schema_version` 1.1）
- [ ] 「关联到客户」UI
- [ ] 「应用到客户」写回 `CompanyInfo` / `Product` / `Application`
- [ ] 风控摘要卡片（AI + CRM 黑名单/额度）
- [ ] 报告 diff（两次调查对比）
- [x] 右栏「调查」页签 UI 细化（对照条、13 章 KV、收起展开、字段中文化）
- [x] **字段 Key 自动翻译**（§7.7：`resolveCustomerIntelFieldLabel` + 多层词典）
- [ ] 客户首页：历史报告下拉、按名称匹配已建档客户

---

## 9. 测试与验收（Phase 1）

| 项 | 验收标准 |
|----|----------|
| 权限 | 无权限不可见调查页签、接口 403 |
| 已建档 | 单击客户 → 右栏展示最新报告或空态 |
| 调查 | 发起后 13 章结构化展示 + 顶部免责 + 单章/全部收起展开 |
| 缓存 | 90 天内同公司二次调查默认 `fromCache` |
| 强制刷新 | 消耗配额并新增历史记录 |
| 历史 | 可切换时间线，最新标记正确 |
| 黑名单 | 允许调查，对照条显示黑名单状态 |
| 字段标签 | 英文 Key 优先中文；纯中文 Key 原样；译不出保留英文；**Value 始终展示、不隐藏行** |
| 字段值枚举 | `high`/`medium`/`low` 等显示中文 |
| Mock | 无 `AI_MOONSHOT_API_KEY` 时 Mock 可完整联调（13 章） |
| **客户首页** | 输入企业名 → AI 调查 → 居中 13 章 + 关闭；无权限按钮禁用 |
| **首页缓存** | 同公司重复调查默认 `fromCache`；无强制刷新按钮 |
| **首页隔离** | 首页调查不改动右栏 store 与选中客户 |
| **Enter 分流** | 有 AI 权限 Enter 走调查；无权限 Enter 走列表搜索 |
| **输入引导** | 有 AI 权限时药丸下显示三行左对齐 `aiSearchHint` |
| **帮助页** | `/custome` 右侧「帮助」含 AI 调查完整操作说明 |

---

## 10. 关键文件索引（规划）

| 层级 | 路径 |
|------|------|
| 设计文档 | `document/System/AI客户情报调查-设计与实现.md`（本文） |
| SQL（初装） | `scripts/ai_customer_intel_lookup_postgresql.sql` |
| SQL（13 章 Prompt） | `scripts/ensure_customer_intel_lookup_schema_v2_postgresql.sql` |
| 常量 | `CRM.Core/Constants/AiCodes.cs` |
| 实体 | `CRM.Core/Models/Customer/CustomerIntelReport.cs` |
| 服务 | `CRM.Infrastructure/Services/CustomerIntelReportService.cs` |
| API | `CRM.API/Controllers/CustomerIntelReportsController.cs`、`CustomersController`（intel-reports） |
| 前端 API | `CRM.Web/src/api/customerIntel.ts` |
| **客户首页** | `CRM.Web/src/views/Customer/CustomerHome.vue` |
| 右栏页签根 | `CRM.Web/src/components/Customer/CustomerIntelPanel.vue` |
| 报告渲染 | `CRM.Web/src/components/Customer/CustomerIntelResultPanel.vue`（`embedded` / `centered` + `showClose`） |
| CRM 对照条 | `CRM.Web/src/components/Customer/CustomerIntelCrmContextBar.vue` |
| 章节 KV | `CRM.Web/src/components/Customer/CustomerIntelSectionContent.vue` |
| 右栏挂载 | `CRM.Web/src/layouts/AppLayout.vue` |
| 列表绑定 | `CRM.Web/src/views/Customer/CustomerList.vue` |
| 详情绑定 | `CRM.Web/src/views/Customer/CustomerDetail.vue` |
| Store | `CRM.Web/src/stores/customerIntelLookup.ts` |
| Schema | `CRM.Web/src/utils/customerIntelSchema.ts`（含 §7.7 `resolveCustomerIntelFieldLabel`） |
| 通用字段中文 | `CRM.Web/src/utils/jsonLabels.ts` → `FIELD_LABEL_ZH` |
| i18n | `CRM.Web/src/locales/zh-CN.ts`、`en-US.ts` → `customerIntel.*`、**`customerHome.aiSearch*`**（含 `aiSearchHint`） |
| 用户帮助 | `help/pages/客户_MENU_CUSTOMER_HOME.md`（§7.6.6） |
| Debug | `CRM.Web/src/views/Debug/DebugCustomerIntel.vue` |

---

## 11. 修订记录

| 版本 | 日期 | 说明 |
|------|------|------|
| 1.0 | 2026-07-14 | 初版：产品决策拍板、8 章契约、双层存储、Phase 1 实施方案 |
| 1.1 | 2026-07-14 | 补充右栏「调查」页签设计与实现（§7.3）；13 章契约与 Prompt v2；UI 迭代（对照条、收起展开、字段中文化） |
| 1.2 | 2026-07-14 | 补充客户首页 `/custome` AI 调查（§7.6）；取消独立 `/customers/intel`；更新入口表、架构图、验收与文件索引 |
| 1.3 | 2026-07-14 | 补充字段 Key 自动翻译（§7.7）：设计规则、算法链路、词典分层、兜底保留英文；更新验收标准 |
| 1.4 | 2026-07-14 | 客户首页输入引导（§7.6.3/7.6.5 三行左对齐 `aiSearchHint`）；右侧帮助文档（§7.6.6）；帮助页 `客户_MENU_CUSTOMER_HOME.md` 增补 AI 调查说明 |
