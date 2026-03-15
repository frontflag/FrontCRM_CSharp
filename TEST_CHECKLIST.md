# 测试清单 - 每次修改代码前必须执行

## 后端API测试
```powershell
cd d:\MyProject\FrontCRM_CSharp
powershell -ExecutionPolicy Bypass -File .\run_api_test.ps1
```

## 前端功能测试

### 1. 认证功能
- [ ] 注册用户 (`/register`)
- [ ] 登录 (`/login`)
- [ ] 获取当前用户 (`/auth/me`)

### 2. 客户管理
- [ ] 创建客户 (`/customers/create`)
- [ ] 查看客户列表 (`/customers`)
- [ ] 编辑客户 (`/customers/:id/edit`)
- [ ] 删除客户

### 3. 字段映射验证
- [ ] customerName ↔ officialName
- [ ] customerShortName ↔ nickName
- [ ] customerLevel ↔ level
- [ ] customerType ↔ type
- [ ] salesPersonId ↔ salesUserId
- [ ] remarks ↔ remark
- [ ] creditLimit ↔ creditLine
- [ ] paymentTerms ↔ payment
- [ ] currency ↔ tradeCurrency
- [ ] unifiedSocialCreditCode ↔ creditCode

## 回归测试命令

### 完整测试
```powershell
# 1. 停止所有服务
taskkill /F /IM dotnet.exe 2>nul
taskkill /F /IM node.exe 2>nul

# 2. 构建后端
dotnet build

# 3. 启动后端API
cd CRM.API\bin\Debug\net9.0
start dotnet CRM.API.dll --urls http://localhost:5000

# 4. 运行API测试
cd ..\..\..
powershell -ExecutionPolicy Bypass -File .\run_api_test.ps1

# 5. 启动前端
cd CRM.Web
npm run dev
```

## 测试通过标准
- [ ] 所有API测试通过
- [ ] 所有字段映射正确
- [ ] 没有控制台报错
- [ ] 没有网络请求失败

---
**注意：不通过测试的代码不能提交！**
