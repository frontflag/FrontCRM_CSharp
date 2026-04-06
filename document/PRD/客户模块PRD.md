# 客户模块产品需求文档（PRD）

**文档版本：** v1.0
**编写日期：** 2026-03-16
**项目名称：** AI智销系统（FrontCRM_CSharp）
**技术栈：** Vue 3 + TypeScript + Element Plus + Pinia + Vite
**适用对象：** CodeBuddy / 前端开发工程师

---

## 一、模块概述

客户模块是 AI智销系统的核心基础资料模块，负责管理企业的全量客户数据。模块涵盖客户档案的完整生命周期管理，包括客户列表查询、新建客户、编辑客户、查看客户详情，以及客户下挂的联系人、地址、银行账户等子资料的增删改查。

本模块的前端代码位于 `CRM.Web/src/views/Customer/`，后端 API 基地址为 `/api/v1/customers`，通过 Vite 代理转发至 `http://localhost:5000`。

---

## 二、路由结构

所有客户模块页面均嵌套在 `AppLayout` 布局组件内（含左侧主菜单和顶部栏），路由定义如下：

| 路由路径 | 组件文件 | 说明 |
|---------|---------|------|
| `/customers` | `CustomerList.vue` | 客户列表页（默认入口） |
| `/customers/create` | `CustomerEdit.vue` | 新建客户页 |
| `/customers/:id` | `CustomerDetail.vue` | 客户详情页 |
| `/customers/:id/edit` | `CustomerEdit.vue` | 编辑客户页（`isEdit = true`） |

路由守卫要求用户已登录（`localStorage` 中存在有效 `token`），未登录时重定向至 `/login`。

---

## 三、数据模型

### 3.1 客户主体（Customer）

```typescript
interface Customer {
  id: string                        // 客户 UUID
  customerCode: string              // 客户编号（系统自动生成）
  customerName: string              // 客户全称（对应后端 officialName）
  customerShortName?: string        // 客户简称（对应后端 nickName）
  customerType: number              // 客户类型（枚举，见 3.5）
  customerLevel: string             // 客户等级（枚举字符串，见 3.5）
  industry?: string                 // 行业（见 3.5）
  region?: string                   // 地区（省/市/区拼接）
  province?: string                 // 省
  city?: string                     // 市
  district?: string                 // 区
  address?: string                  // 详细地址
  country?: string                  // 国家
  unifiedSocialCreditCode?: string  // 统一社会信用代码
  salesPersonId?: string            // 业务员 ID
  salesPersonName?: string          // 业务员姓名
  creditLimit?: number              // 信用额度（元）
  paymentTerms?: number             // 账期（天）
  currency?: number                 // 结算币种（枚举，见 3.5）
  taxRate?: number                  // 税率（%）
  invoiceType?: number              // 发票类型（枚举，见 3.5）
  isActive: boolean                 // 是否启用
  balance?: number                  // 账户余额（元，负数表示欠款）
  remarks?: string                  // 备注
  contacts: CustomerContactInfo[]   // 联系人列表
  addresses: CustomerAddress[]      // 地址列表
  bankAccounts: CustomerBankInfo[]  // 银行账户列表（注意：详情页用 banks 字段）
  createdAt?: string                // 创建时间（ISO 8601）
  updatedAt?: string                // 更新时间（ISO 8601）
}
```

> **重要兼容说明：** 后端 API 实际返回的字段名与前端存在映射关系。前端在读取时需做以下兼容处理：
> - `customerName` ← `officialName`
> - `customerShortName` ← `nickName`
> - `salesPersonId` ← `salesUserId`
> - `unifiedSocialCreditCode` ← `creditCode`
> - `creditLimit` ← `creditLine`
> - `paymentTerms` ← `payment`
> - `currency` ← `tradeCurrency`
> - `remarks` ← `remark`

### 3.2 联系人（CustomerContactInfo）

```typescript
interface CustomerContactInfo {
  id: string
  contactName: string     // 姓名（必填）
  gender: number          // 性别：0=保密、1=男、2=女
  department?: string     // 部门
  position?: string       // 职位
  mobilePhone: string     // 手机（必填）
  phone?: string          // 固话
  email?: string          // 邮箱
  fax?: string            // 传真
  socialAccount?: string  // 社交账号
  isDefault: boolean      // 是否默认联系人
  isDecisionMaker: boolean // 是否决策人
  remarks?: string        // 备注
}
```

### 3.3 地址（CustomerAddress）

```typescript
interface CustomerAddress {
  id: string
  addressType: string    // 地址类型：Office/Billing/Shipping/Registered
  country?: string       // 国家
  province?: string      // 省
  city?: string          // 市
  district?: string      // 区
  streetAddress: string  // 详细街道地址（必填）
  zipCode?: string       // 邮编
  contactPerson?: string // 联系人
  contactPhone?: string  // 联系电话
  isDefault: boolean     // 是否默认地址
}
```

### 3.4 银行账户（CustomerBankInfo）

```typescript
interface CustomerBankInfo {
  id: string
  accountName: string   // 账户名称（必填）
  bankName: string      // 开户银行（必填）
  bankBranch: string    // 开户支行（必填）
  accountNumber: string // 银行账号（必填）
  currency: number      // 币种（枚举，见 3.5）
  swiftCode?: string    // SWIFT 代码
  isDefault: boolean    // 是否默认银行账户
}
```

### 3.5 枚举定义

以下枚举值为前后端共同约定，**必须严格遵守**，不可使用旧版 0/1/2 映射。

**客户类型（customerType）**

| 值 | 标签 |
|----|------|
| 1 | OEM |
| 2 | ODM |
| 3 | 终端用户 |
| 4 | IDH |
| 5 | 贸易商 |
| 6 | 代理商 |

> 默认值为 `1`（OEM），**禁止使用 `0` 作为默认值**（后端枚举从 1 开始）。

**客户等级（customerLevel）**

前端使用字符串，提交到后端时通过 `mapCustomerLevelToInt()` 函数转换为数字：

| 字符串值 | 后端数字 | 显示标签 |
|---------|---------|---------|
| `D` | 1 | D级 |
| `C` | 2 | C级 |
| `B` | 3 | B级 |
| `BPO` | 4 | BPO |
| `VIP` | 5 | VIP |
| `VPO` | 6 | VPO |

> 兼容旧值：`Normal`→3，`Important`→5，`Lead`→1。默认值为 `'Normal'`（映射为 3/B级）。

**行业（industry）**

| 值 | 标签 |
|----|------|
| `Manufacturing` | 制造业 |
| `Trading` | 贸易/零售 |
| `Technology` | 科技/IT |
| `Construction` | 建筑/工程 |
| `Healthcare` | 医疗/健康 |
| `Education` | 教育 |
| `Finance` | 金融 |
| `Other` | 其他 |

**币种（currency）**

| 值 | 标签 |
|----|------|
| 1 | RMB |
| 2 | USD |
| 3 | EUR |
| 4 | GBP |
| 5 | JPY |
| 6 | HKD |

**发票类型（invoiceType）**

| 值 | 标签 |
|----|------|
| 1 | 增值税专用发票 |
| 2 | 增值税普通发票 |
| 3 | 电子发票 |

**地址类型（addressType）**

| 值 | 标签 |
|----|------|
| `Office` | 办公地址 |
| `Billing` | 开票地址 |
| `Shipping` | 收货地址 |
| `Registered` | 注册地址 |

---

## 四、后端 API 接口规范

### 4.1 HTTP 客户端配置

所有请求通过 `src/api/client.ts` 中的 `ApiClient` 单例发送：

- **baseURL：** 空字符串（使用相对路径，走 Vite 代理）
- **timeout：** 10000ms
- **认证：** 请求拦截器自动从 `localStorage.getItem('token')` 读取并注入 `Authorization: Bearer {token}` 头
- **响应处理：** 后端统一返回 `{ success: boolean, data: T, message: string }` 格式，拦截器自动解包，调用方直接获得 `data`
- **401 处理：** 自动清除 token，跳转 `/login`

### 4.2 客户主体接口

| 方法 | URL | 说明 | 请求体/参数 |
|------|-----|------|------------|
| `GET` | `/api/v1/customers?{params}` | 分页查询客户列表 | 见 4.2.1 |
| `POST` | `/api/v1/customers` | 新建客户 | 见 4.2.2 |
| `GET` | `/api/v1/customers/{id}` | 获取客户详情 | 路径参数 id |
| `PUT` | `/api/v1/customers/{id}` | 更新客户 | 见 4.2.3 |
| `DELETE` | `/api/v1/customers/{id}` | 删除客户 | 路径参数 id |
| `POST` | `/api/v1/customers/{id}/activate` | 启用客户 | 无请求体 |
| `POST` | `/api/v1/customers/{id}/deactivate` | 停用客户 | 无请求体 |
| `GET` | `/api/v1/customers/statistics` | 获取统计数据 | 无 |
| `GET` | `/api/v1/customers/{id}/contact-history` | 获取联系历史 | 路径参数 id |
| `POST` | `/api/v1/customers/{id}/contact-history` | 添加联系记录 | 联系记录对象 |

#### 4.2.1 查询参数（searchCustomers）

```
pageNumber: number     // 页码，从 1 开始
pageSize: number       // 每页条数，默认 20
searchTerm?: string    // 关键字（客户名称/编号模糊搜索）
customerType?: number  // 客户类型筛选（1-6）
customerLevel?: string // 客户等级筛选（D/C/B/BPO/VIP/VPO）
industry?: string      // 行业筛选
region?: string        // 地区筛选
isActive?: boolean     // 状态筛选
sortBy?: string        // 排序字段
sortDescending?: boolean // 是否降序
```

**响应格式：**
```json
{
  "items": [...],
  "totalCount": 100,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

> 若后端返回 `null`，前端需容错处理，返回 `{ items: [], totalCount: 0 }`。

#### 4.2.2 新建客户请求体（createCustomer）

```json
{
  "officialName": "客户全称",
  "nickName": "简称",
  "level": 3,
  "type": 1,
  "salesUserId": "user-001",
  "creditLine": 100000,
  "payment": 30,
  "tradeCurrency": 1,
  "creditCode": "统一社会信用代码",
  "remark": "备注",
  "contacts": [
    {
      "contactName": "张三",
      "gender": 0,
      "mobilePhone": "13800138000",
      "isDefault": true,
      "isDecisionMaker": false
    }
  ]
}
```

> **关键：** `contacts` 数组必须包含在请求体中，即使为空数组 `[]` 也需显式发送。

#### 4.2.3 更新客户请求体（updateCustomer）

与新建请求体结构相同，通过 `PUT /api/v1/customers/{id}` 提交，同样需包含 `contacts` 数组。

### 4.3 联系人接口

| 方法 | URL | 说明 |
|------|-----|------|
| `GET` | `/api/v1/customers/{customerId}/contacts` | 获取联系人列表 |
| `POST` | `/api/v1/customers/{customerId}/contacts` | 新建联系人 |
| `PUT` | `/api/v1/contacts/{contactId}` | 更新联系人 |
| `DELETE` | `/api/v1/contacts/{contactId}` | 删除联系人 |
| `POST` | `/api/v1/contacts/{contactId}/set-default` | 设为默认联系人 |

### 4.4 地址接口

| 方法 | URL | 说明 |
|------|-----|------|
| `GET` | `/api/v1/customers/{customerId}/addresses` | 获取地址列表 |
| `POST` | `/api/v1/customers/{customerId}/addresses` | 新建地址 |
| `PUT` | `/api/v1/addresses/{addressId}` | 更新地址 |
| `DELETE` | `/api/v1/addresses/{addressId}` | 删除地址 |
| `POST` | `/api/v1/addresses/{addressId}/set-default` | 设为默认地址 |

### 4.5 银行账户接口

| 方法 | URL | 说明 |
|------|-----|------|
| `GET` | `/api/v1/customers/{customerId}/banks` | 获取银行账户列表 |
| `POST` | `/api/v1/customers/{customerId}/banks` | 新建银行账户 |
| `PUT` | `/api/v1/banks/{bankId}` | 更新银行账户 |
| `DELETE` | `/api/v1/banks/{bankId}` | 删除银行账户 |
| `POST` | `/api/v1/banks/{bankId}/set-default` | 设为默认银行账户 |

---

## 五、页面功能规格

### 5.1 客户列表页（CustomerList.vue）

#### 5.1.1 统计卡片区

页面顶部展示 4 个统计卡片，数据来源为 `GET /api/v1/customers/statistics`：

| 卡片 | 字段 | 说明 |
|------|------|------|
| 总客户数 | `totalCustomers` | 所有客户总数 |
| 活跃客户 | `activeCustomers` | `isActive=true` 的客户数 |
| 本月新增 | `newThisMonth` | 当月新建客户数 |
| 应收余额 | `totalBalance` | 所有客户 balance 之和（格式化为货币） |

#### 5.1.2 搜索筛选区

搜索面板包含以下 5 个筛选项，点击"搜索"按钮或按 Enter 键触发查询，点击"重置"清空所有筛选条件并重新查询：

| 字段 | 控件类型 | 绑定参数 |
|------|---------|---------|
| 关键字 | `el-input`（回车触发） | `searchParams.searchTerm` |
| 客户类型 | `el-select` | `searchParams.customerType`（1-6，见枚举） |
| 客户等级 | `el-select` | `searchParams.customerLevel`（D/C/B/BPO/VIP/VPO） |
| 行业 | `el-select` | `searchParams.industry` |
| 状态 | `el-select` | `searchParams.isActive`（true=启用/false=停用） |

#### 5.1.3 数据表格

表格列定义如下（按显示顺序）：

| 列名 | 宽度 | 内容说明 |
|------|------|---------|
| 客户编号 | 130px | `customerCode`，等宽字体（Space Mono） |
| 客户名称 | min-width 200px | 左侧字母头像 + 客户名称 + 等级徽章 |
| 类型 | 100px | `customerType` 枚举标签徽章 |
| 行业 | 120px | `industry` 枚举标签 |
| 联系人 | 160px | 默认联系人姓名 + 手机号（取 `contacts[0]`） |
| 地区 | 120px | `region` 字段 |
| 信用额度 | 120px | `creditLimit`，右对齐，货币格式 |
| 账户余额 | 120px | `balance`，右对齐，负数显示红色 |
| 状态 | 80px | `el-switch` 开关，点击触发启用/停用 API |
| 操作 | 180px | 查看 / 编辑 / 更多（下拉菜单） |

**更多下拉菜单项：**
- 添加联系人（跳转详情页联系人标签）
- 添加地址（跳转详情页地址标签）
- 创建报价（跳转 `/quotes/create?customerId={id}`）
- 创建订单（跳转 `/orders/create?customerId={id}`）
- 删除（弹窗确认后调用 DELETE 接口，成功后刷新列表）

#### 5.1.4 分页

使用 `el-pagination`，支持 `total, sizes, prev, pager, next, jumper` 布局，每页条数选项为 `[10, 20, 50, 100]`，默认每页 20 条。切换页码或每页条数时重新请求列表数据。

#### 5.1.5 操作按钮（右上角）

- **导出**：当前版本为占位按钮，点击提示"功能开发中"
- **新增客户**：跳转 `/customers/create`

---

### 5.2 新建/编辑客户页（CustomerEdit.vue）

`isEdit` 状态由路由参数 `route.params.id` 是否存在决定。编辑模式下，`onMounted` 时调用 `GET /api/v1/customers/{id}` 获取现有数据并回填表单。

#### 5.2.1 页面头部

- 左侧：返回按钮（`router.back()`）+ 页面标题（新建客户 / 编辑客户）
- 右侧：取消按钮（`router.back()`）+ 保存按钮（触发 `handleSave`）

#### 5.2.2 基本信息区块

| 字段 | 控件 | 必填 | 验证规则 | 默认值 |
|------|------|------|---------|-------|
| 客户名称 | `el-input` | ✅ | 2-100 字符 | 空 |
| 客户简称 | `el-input` | ❌ | — | 空 |
| 客户类型 | `el-select` | ✅ | 必选 | `1`（OEM） |
| 客户等级 | `el-select` | ✅ | 必选 | `'Normal'` |
| 行业 | `el-select` | ❌ | — | 空 |
| 统一社会信用代码 | `el-input` | ❌ | — | 空 |
| 所属业务员 | `el-select` | ❌ | — | 空 |
| 地区（省/市/区） | `el-cascader` | ❌ | — | 空 |
| 详细地址 | `el-input` | ❌ | — | 空 |
| 备注 | `el-input type="textarea"` | ❌ | — | 空 |

#### 5.2.3 财务信息区块

| 字段 | 控件 | 必填 | 默认值 |
|------|------|------|-------|
| 信用额度 | `el-input-number` | ❌ | `0` |
| 账期（天） | `el-input-number` | ❌ | `30` |
| 结算币种 | `el-select` | ❌ | `1`（RMB） |
| 税率（%） | `el-input-number`（0-100，精度2位） | ❌ | `13` |
| 发票类型 | `el-select` | ❌ | `2`（增值税普通发票） |

#### 5.2.4 联系人信息区块

联系人为动态列表，支持添加多条，至少可以为空（不强制要求）。每条联系人包含以下字段：

| 字段 | 控件 | 必填 |
|------|------|------|
| 姓名 | `el-input` | ✅ |
| 性别 | `el-select`（男/女/保密） | ❌ |
| 手机 | `el-input` | ✅ |
| 邮箱 | `el-input` | ❌ |
| 部门 | `el-input` | ❌ |
| 职位 | `el-input` | ❌ |
| 固话 | `el-input` | ❌ |
| 设为默认联系人 | `el-checkbox` | ❌ |

第一条联系人默认勾选"设为默认联系人"。点击"删除"按钮从列表移除该条联系人。

#### 5.2.5 保存逻辑

1. 调用 `formRef.value?.validate()` 进行表单验证
2. 验证通过后：
   - **新建模式：** 调用 `POST /api/v1/customers`，成功后跳转 `/customers`
   - **编辑模式：** 调用 `PUT /api/v1/customers/{id}`，成功后跳转 `/customers`
3. 成功提示 `ElMessage.success`，失败提示 `ElMessage.error`

---

### 5.3 客户详情页（CustomerDetail.vue）

#### 5.3.1 页面头部

- 左侧：返回按钮（跳转 `/customers`）+ 客户头像（名称首字母）+ 客户名称 + 编号 + 状态徽章 + 等级徽章
- 右侧：编辑按钮（跳转编辑页）+ 创建报价按钮（跳转 `/quotes/create?customerId={id}`）+ 创建订单按钮（跳转 `/orders/create?customerId={id}`）

#### 5.3.2 基本信息卡片

以 3 列网格展示以下字段：

客户编号、客户名称、客户类型、统一社会信用代码、行业、地区、所属业务员、信用额度、账期（天）、账户余额、创建时间、更新时间。

其中金额字段使用 `formatCurrency()` 格式化（`¥1,234.56`），时间字段使用 `toLocaleString('zh-CN')` 格式化。

#### 5.3.3 标签页

共 4 个标签页，默认激活"联系人"：

**联系人标签页**

- 右上角"添加联系人"按钮，点击弹出 `ContactDialog`
- 表格列：姓名、性别、部门、职位、手机、邮箱、是否默认、操作（编辑/删除）
- 删除前弹出 `ElMessageBox.confirm` 确认框

**地址信息标签页**

- 右上角"添加地址"按钮，点击弹出 `AddressDialog`
- 表格列：地址类型、完整地址（省市区街道拼接）、邮编、联系人、联系电话、是否默认、操作（编辑/删除）

**银行信息标签页**

- 右上角"添加银行信息"按钮，点击弹出 `BankDialog`
- 表格列：账户名称、开户银行、开户支行、银行账号（等宽字体）、币种、是否默认、操作（编辑/删除）

**业务记录标签页**

- 时间线展示，每条记录包含：类型（primary/success/warning）、时间（格式：`YYYY-MM-DD HH:mm`）、内容描述
- 当前版本为静态示例数据，后续对接 `GET /api/v1/customers/{id}/contact-history`

#### 5.3.4 子资料对话框

详情页使用 3 个子对话框组件（位于 `src/views/Customer/components/`）：

| 组件 | Props | Emits | 说明 |
|------|-------|-------|------|
| `ContactDialog.vue` | `modelValue: boolean, customerId: string, contact?: CustomerContactInfo` | `update:modelValue, success` | 新建/编辑联系人 |
| `AddressDialog.vue` | `modelValue: boolean, customerId: string, address?: CustomerAddress` | `update:modelValue, success` | 新建/编辑地址 |
| `BankDialog.vue` | `modelValue: boolean, customerId: string, bank?: CustomerBankInfo` | `update:modelValue, success` | 新建/编辑银行账户 |

对话框通过 `v-model` 控制显示/隐藏，操作成功后触发 `success` 事件，父组件监听后调用 `fetchCustomerDetail()` 刷新数据。

---

## 六、核心辅助函数

以下函数定义在 `src/api/customer.ts` 中，**必须导出并复用**，不可在各页面重复实现：

### mapCustomerLevelToInt(level)

将客户等级字符串转换为后端数字枚举，用于 `createCustomer` 和 `updateCustomer` 请求体构建：

```typescript
export function mapCustomerLevelToInt(level: string | number | undefined): number {
  if (typeof level === 'number' && level > 0) return level
  const map: Record<string, number> = {
    'D': 1, 'C': 2, 'B': 3, 'BPO': 4, 'VIP': 5, 'VPO': 6,
    'Normal': 3, 'Important': 5, 'Lead': 1  // 旧版兼容
  }
  return map[level as string] ?? 3  // 默认 B级(3)
}
```

### mapCustomerTypeToLabel(type)

将客户类型数字枚举转换为显示标签：

```typescript
export function mapCustomerTypeToLabel(type: number | undefined): string {
  const map: Record<number, string> = {
    1: 'OEM', 2: 'ODM', 3: '终端用户', 4: 'IDH', 5: '贸易商', 6: '代理商'
  }
  return map[type as number] ?? '未知'
}
```

---

## 七、设计规范

本模块采用**深海量子风（Deep Ocean Quantum）**设计体系，所有页面必须严格遵守以下规范：

### 7.1 色彩系统

SCSS 变量定义在 `src/assets/styles/variables.scss`：

| 变量名 | 色值 | 用途 |
|--------|------|------|
| `$layer-1` | `#192A3F` | 页面背景色 |
| `$layer-2` | `#0A1628` | 卡片/面板背景色 |
| `$layer-3` | `#162233` | 输入框/表格行背景色 |
| `$ice-blue` | `#50BBE3` | 主要强调色（链接、标签） |
| `$steel-cyan` | `#3295C9` | 次要强调色（按钮渐变起点） |
| `$mint-green` | `#46BF91` | 成功/激活状态色 |
| `$amber` | `#C99A45` | 警告/财务信息色 |
| `$red-brown` | `#C95745` | 危险/删除操作色 |

### 7.2 字体规范

```html
<!-- 在 index.html 中引入 Google Fonts -->
<link href="https://fonts.googleapis.com/css2?family=Orbitron:wght@400;700&family=Noto+Sans+SC:wght@300;400;500&family=Space+Mono&display=swap" rel="stylesheet">
```

| 字体 | 用途 |
|------|------|
| Orbitron | 品牌标题、页面大标题 |
| Noto Sans SC | 正文、表单标签、说明文字 |
| Space Mono | 编号、金额、时间戳等数字内容 |

### 7.3 组件样式约定

**Element Plus 深色覆写**（在 `src/assets/styles/main.scss` 中全局定义）：

- 输入框背景：`$layer-3`，边框：`rgba(255,255,255,0.10)`，聚焦边框：`$ice-blue`
- 下拉菜单背景：`$layer-2`，选项 hover 背景：`rgba(0,212,255,0.08)`
- 表格背景：透明，表头背景：`$layer-2`，行分割线：`rgba(255,255,255,0.05)`
- 按钮主色：蓝→青渐变（`#0066FF → #00D4FF`），危险色：`$red-brown`

**等级徽章颜色：**

| 等级 | 背景色 |
|------|-------|
| VIP / VPO | `rgba(201,154,69,0.2)` + `$amber` 文字 |
| BPO / B | `rgba(50,149,201,0.2)` + `$steel-cyan` 文字 |
| C / D | `rgba(80,187,227,0.15)` + `$ice-blue` 文字 |

---

## 八、测试规范

本模块已配置 Vitest 测试框架，测试文件位于 `src/tests/`：

| 文件 | 测试数 | 覆盖内容 |
|------|--------|---------|
| `customer-helpers.test.ts` | 21 | `mapCustomerLevelToInt` 和 `mapCustomerTypeToLabel` 函数 |
| `customer-api.test.ts` | 24 | `searchCustomers`、`createCustomer`、`updateCustomer`、`deleteCustomer`、`activateCustomer`、`deactivateCustomer`、`getCustomerById` 接口 |

运行测试命令：`npm test`（执行 `vitest run`）。

新增功能时，**必须同步编写对应测试用例**，确保测试覆盖率不降低。

---

## 九、已知限制与待办事项

| 编号 | 优先级 | 说明 |
|------|--------|------|
| T1 | 🟡 中 | 业务员下拉为静态数据（张三/李四/王五），需对接 `GET /api/v1/users` 获取真实用户列表 |
| T2 | 🟡 中 | 地区级联为静态数据（仅覆盖北京/上海/广东），需引入完整省市区数据或对接地区 API |
| T3 | 🟢 低 | 导出功能为占位按钮，需实现 Excel 导出（调用 `GET /api/v1/customers/export`） |
| T4 | 🟢 低 | 业务记录标签页为静态示例数据，需对接 `GET /api/v1/customers/{id}/contact-history` |
| T5 | 🟢 低 | `CustomerDetail.vue` 中 `getTypeLabel` 仍使用旧版 0=企业/1=个人/2=政府 映射，需统一为新枚举 |

---

*本文档由 Manus AI 根据源代码逆向分析生成，版本对应 Git commit `f0a53ee`（develop 分支）。*
