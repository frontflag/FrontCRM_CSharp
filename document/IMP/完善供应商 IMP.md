
应商模块 按 客户模块 的功能更改对齐。 

### 供应商模块补齐 – 任务计划清单

#### 一、供应商主档与详情

1. **Vendor 列表增强**
   - 梳理并补充筛选条件（状态已有，后续视需求增加类型/行业等）。
   - 列表字段对齐客户（主联系人、地区、信用额度/余额等，按供应商场景适配）。

2. **供应商详情页 `VendorDetail.vue`**
   - 新建详情页，展示：编号、名称、状态、黑名单标记、等级、行业、地区、采购员等。
   - 从 `VendorList.vue` 跳转到 `/vendors/:id` 查看详情。
   - 预留 Tab 区域：联系人、地址、银行、联系历史、文档、操作日志等。

3. **启用/停用供应商**
   - 后端：在 `VendorsController` 增加  
     - `POST /api/v1/vendors/{id}/activate`  
     - `POST /api/v1/vendors/{id}/deactivate`  
     调用 `VendorService.UpdateStatusAsync(id, status)`。
   - 前端：在供应商列表和详情页增加启用/停用操作（按钮/开关）。

---

#### 二、供应商黑名单与回收站

4. **供应商黑名单**
   - 后端：
     - 确认并使用 `VendorInfo.BlackList` 字段。
     - 新增接口：  
       - `POST /api/v1/vendors/{id}/blacklist`（带 `reason`）  
       - `DELETE /api/v1/vendors/{id}/blacklist`  
       - `GET /api/v1/vendors/blacklist`（分页 + 关键字）。
   - 前端：
     - `vendorApi.addToBlacklist/removeFromBlacklist/getBlacklist`。
     - 详情页顶部“加入/解除黑名单”按钮及黑名单标识。
     - 黑名单列表页面。

5. **供应商软删除 + 回收站**
   - 后端：
     - 为 `VendorInfo` 增加/启用软删除字段（`IsDeleted`、`DeleteTime`、`DeleteReason` 等）。  
     - 删除改为软删；新增：  
       - `GET /api/v1/vendors/recycle-bin`  
       - `POST /api/v1/vendors/{id}/restore`。
   - 前端：
     - 列表页删除供应商 → 弹出填写删除原因 → 软删。  
     - 新建 `VendorRecycleBin.vue` 展示回收站供应商，可搜索与恢复。

---

#### 三、供应商子信息（联系人 / 地址 / 银行）

6. **供应商联系人管理**
   - 后端：
     - 检查并补全接口：  
       - `GET /api/v1/vendors/{vendorId}/contacts`  
       - `POST /api/v1/vendors/{vendorId}/contacts`  
       - `PUT /api/v1/vendor-contacts/{contactId}`  
       - `DELETE /api/v1/vendor-contacts/{contactId}`  
       - （可选）`POST /api/v1/vendor-contacts/{contactId}/set-main`。
   - 前端：
     - 在 `VendorDetail.vue` 增加“联系人”Tab：表格展示联系人信息。  
     - 支持新增/编辑/删除/设为主联系人（使用 `VendorContactDialog.vue`）。

7. **供应商地址管理**
   - 后端：
     - 使用 `VendorAddress`，新增接口类似客户地址：  
       - `GET /api/v1/vendors/{vendorId}/addresses`  
       - `POST /api/v1/vendors/{vendorId}/addresses`  
       - `PUT /api/v1/vendor-addresses/{addressId}`  
       - `DELETE /api/v1/vendor-addresses/{addressId}`  
       - `POST /api/v1/vendor-addresses/{addressId}/set-default`。
   - 前端：
     - `VendorDetail.vue` 增加“地址信息”Tab。  
     - 新建 `VendorAddressDialog.vue` 处理地址表单。

8. **供应商银行信息管理**
   - 后端：
     - 使用 `VendorBankInfo`，增加接口：  
       - `GET /api/v1/vendors/{vendorId}/banks`  
       - `POST /api/v1/vendors/{vendorId}/banks`  
       - `PUT /api/v1/vendor-banks/{bankId}`  
       - `DELETE /api/v1/vendor-banks/{bankId}`  
       - `POST /api/v1/vendor-banks/{bankId}/set-default`。
   - 前端：
     - `VendorDetail.vue` 增加“银行信息”Tab。  
     - 新建 `VendorBankDialog.vue` 处理银行账户增改。

---

#### 四、供应商联系历史与日志

9. **供应商联系历史（跟进记录）**
   - 后端：
     - 设计 `VendorContactHistory` 表或复用通用联系历史表 + `BizType=Vendor`。  
     - 接口：  
       - `GET /api/v1/vendors/{vendorId}/contact-history`  
       - `POST /api/v1/vendors/{vendorId}/contact-history`  
       - `PUT /api/v1/vendors/{vendorId}/contact-history/{historyId}`  
       - `DELETE /api/v1/vendors/{vendorId}/contact-history/{historyId}`。
   - 前端：
     - `VendorDetail.vue` 增加“联系历史”Tab：时间线/表格展示 + 新增/编辑/删除。

10. **供应商操作日志 / 字段变更日志**
    - 后端：
      - 在现有日志体系（如 `BusinessLog`）中加入供应商相关记录。  
      - 提供：  
        - `GET /api/v1/vendors/{id}/operation-logs`  
        - `GET /api/v1/vendors/{id}/field-change-logs`。
    - 前端：
      - `VendorDetail.vue` 增加“操作日志/变更日志”Tab，复用客户的展示模式。

---

#### 五、供应商统计与仪表盘

11. **供应商统计接口与展示**
    - 后端：
      - `GET /api/v1/vendors/statistics`：总数、活跃供应商、本月新增、按等级/行业分布等。
    - 前端：
      - 在 `VendorList.vue` 顶部增加统计卡片。  
      - 在 Dashboard 中增加供应商概览模块（可选）。

---

后续我在实现时，就会按这个清单逐条推进，每完成一条（比如“供应商详情页 + 启用/停用”）就给你单独的简短进度说明。