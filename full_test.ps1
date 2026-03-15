# CRM 全业务流程测试脚本
# 测试内容：用户注册、客户管理、联系人、地址、银行信息

$ErrorActionPreference = "Stop"
$apiBase = "http://localhost:5028/api/v1"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "    CRM 全业务流程测试" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 生成唯一测试数据
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$testUsername = "testuser_$timestamp"
$testEmail = "test_$timestamp@example.com"
$testPassword = "Test123!@#"
$customerCode = "CUST$timestamp"

Write-Host "测试用户名: $testUsername" -ForegroundColor Yellow
Write-Host "客户编码: $customerCode" -ForegroundColor Yellow
Write-Host ""

# ==================== 1. 用户注册测试 ====================
Write-Host "【1. 用户注册测试】" -ForegroundColor Green

$registerBody = @{
    userName = $testUsername
    email = $testEmail
    password = $testPassword
    confirmPassword = $testPassword
} | ConvertTo-Json

try {
    $registerResponse = Invoke-RestMethod -Uri "$apiBase/auth/register" -Method POST -ContentType "application/json" -Body $registerBody
    Write-Host "  注册响应: $($registerResponse.message)" -ForegroundColor Gray
    
    if ($registerResponse.success -eq $true) {
        Write-Host "  注册成功!" -ForegroundColor Green
    } else {
        Write-Host "  注册失败: $($registerResponse.message)" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "  注册请求失败: $_" -ForegroundColor Red
    exit 1
}

# ==================== 2. 用户登录测试 ====================
Write-Host "`n【2. 用户登录测试】" -ForegroundColor Green

$loginBody = @{
    username = $testUsername
    password = $testPassword
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$apiBase/auth/login" -Method POST -ContentType "application/json" -Body $loginBody
    
    if ($loginResponse.success -eq $true) {
        $token = $loginResponse.data.token
        Write-Host "  登录成功!" -ForegroundColor Green
        Write-Host "  Token获取成功" -ForegroundColor Gray
    } else {
        Write-Host "  登录失败: $($loginResponse.message)" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "  登录请求失败: $_" -ForegroundColor Red
    exit 1
}

# 设置认证头
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# ==================== 3. 创建客户测试 ====================
Write-Host "`n【3. 创建客户测试】" -ForegroundColor Green

$customerBody = @{
    customerCode = $customerCode
    customerName = "测试客户_$timestamp"
    customerShortName = "测试_$timestamp"
    customerLevel = "VIP"
    customerType = 1
    industry = "电子"
    email = "customer_$timestamp@test.com"
    phone = "13800138000"
    creditLimit = 100000
    paymentTerms = 30
    currency = 1
    unifiedSocialCreditCode = "91110000XXXXXXXX$timestamp"
    remarks = "这是测试客户"
} | ConvertTo-Json

try {
    $customerResponse = Invoke-RestMethod -Uri "$apiBase/customers" -Method POST -Headers $headers -Body $customerBody
    
    if ($customerResponse.success -eq $true) {
        $customerId = $customerResponse.data.id
        Write-Host "  客户创建成功! ID: $customerId" -ForegroundColor Green
    } else {
        Write-Host "  客户创建失败: $($customerResponse.message)" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "  创建客户请求失败: $_" -ForegroundColor Red
    exit 1
}

# ==================== 4. 创建联系人测试 ====================
Write-Host "`n【4. 创建联系人测试】" -ForegroundColor Green

$contactBody = @{
    name = "张三"
    gender = 1
    department = "采购部"
    position = "经理"
    phone = "010-12345678"
    mobile = "13800138001"
    email = "zhangsan@test.com"
    isDefault = $true
} | ConvertTo-Json

try {
    $contactResponse = Invoke-RestMethod -Uri "$apiBase/customers/$customerId/contacts" -Method POST -Headers $headers -Body $contactBody
    
    if ($contactResponse.success -eq $true) {
        $contactId = $contactResponse.data.id
        Write-Host "  联系人创建成功! ID: $contactId" -ForegroundColor Green
    } else {
        Write-Host "  联系人创建失败: $($contactResponse.message)" -ForegroundColor Red
    }
} catch {
    Write-Host "  创建联系人请求失败: $_" -ForegroundColor Red
}

# 创建第二个联系人
$contactBody2 = @{
    name = "李四"
    gender = 2
    department = "技术部"
    position = "工程师"
    phone = "010-87654321"
    mobile = "13900139001"
    email = "lisi@test.com"
    isDefault = $false
} | ConvertTo-Json

try {
    $contactResponse2 = Invoke-RestMethod -Uri "$apiBase/customers/$customerId/contacts" -Method POST -Headers $headers -Body $contactBody2
    
    if ($contactResponse2.success -eq $true) {
        $contactId2 = $contactResponse2.data.id
        Write-Host "  联系人2创建成功! ID: $contactId2" -ForegroundColor Green
    } else {
        Write-Host "  联系人2创建失败: $($contactResponse2.message)" -ForegroundColor Red
    }
} catch {
    Write-Host "  创建联系人2请求失败: $_" -ForegroundColor Red
}

# ==================== 5. 创建地址测试 ====================
Write-Host "`n【5. 创建地址测试】" -ForegroundColor Green

$addressBody = @{
    addressType = 1
    country = 156
    province = "广东省"
    city = "深圳市"
    area = "南山区"
    address = "科技园南路88号"
    contactName = "张三"
    contactPhone = "13800138001"
    isDefault = $true
} | ConvertTo-Json

try {
    $addressResponse = Invoke-RestMethod -Uri "$apiBase/customers/$customerId/addresses" -Method POST -Headers $headers -Body $addressBody
    
    if ($addressResponse.success -eq $true) {
        $addressId = $addressResponse.data.id
        Write-Host "  地址创建成功! ID: $addressId" -ForegroundColor Green
    } else {
        Write-Host "  地址创建失败: $($addressResponse.message)" -ForegroundColor Red
    }
} catch {
    Write-Host "  创建地址请求失败: $_" -ForegroundColor Red
}

# 创建第二个地址
$addressBody2 = @{
    addressType = 2
    country = 156
    province = "上海市"
    city = "上海市"
    area = "浦东新区"
    address = "张江高科技园区"
    contactName = "李四"
    contactPhone = "13900139001"
    isDefault = $false
} | ConvertTo-Json

try {
    $addressResponse2 = Invoke-RestMethod -Uri "$apiBase/customers/$customerId/addresses" -Method POST -Headers $headers -Body $addressBody2
    
    if ($addressResponse2.success -eq $true) {
        $addressId2 = $addressResponse2.data.id
        Write-Host "  地址2创建成功! ID: $addressId2" -ForegroundColor Green
    } else {
        Write-Host "  地址2创建失败: $($addressResponse2.message)" -ForegroundColor Red
    }
} catch {
    Write-Host "  创建地址2请求失败: $_" -ForegroundColor Red
}

# ==================== 6. 创建银行信息测试 ====================
Write-Host "`n【6. 创建银行信息测试】" -ForegroundColor Green

$bankBody = @{
    bankName = "中国工商银行"
    bankAccount = "6222021234567890123"
    accountName = "测试公司_$timestamp"
    bankBranch = "深圳南山支行"
    currency = 1
    isDefault = $true
    remark = "主要收款账户"
} | ConvertTo-Json

try {
    $bankResponse = Invoke-RestMethod -Uri "$apiBase/customers/$customerId/banks" -Method POST -Headers $headers -Body $bankBody
    
    if ($bankResponse.success -eq $true) {
        $bankId = $bankResponse.data.id
        Write-Host "  银行信息创建成功! ID: $bankId" -ForegroundColor Green
    } else {
        Write-Host "  银行信息创建失败: $($bankResponse.message)" -ForegroundColor Red
    }
} catch {
    Write-Host "  创建银行信息请求失败: $_" -ForegroundColor Red
}

# 创建第二个银行账户
$bankBody2 = @{
    bankName = "中国建设银行"
    bankAccount = "6227009876543210987"
    accountName = "测试公司_$timestamp"
    bankBranch = "深圳福田支行"
    currency = 1
    isDefault = $false
    remark = "备用收款账户"
} | ConvertTo-Json

try {
    $bankResponse2 = Invoke-RestMethod -Uri "$apiBase/customers/$customerId/banks" -Method POST -Headers $headers -Body $bankBody2
    
    if ($bankResponse2.success -eq $true) {
        $bankId2 = $bankResponse2.data.id
        Write-Host "  银行信息2创建成功! ID: $bankId2" -ForegroundColor Green
    } else {
        Write-Host "  银行信息2创建失败: $($bankResponse2.message)" -ForegroundColor Red
    }
} catch {
    Write-Host "  创建银行信息2请求失败: $_" -ForegroundColor Red
}

# ==================== 7. 获取客户详情验证 ====================
Write-Host "`n【7. 获取客户详情验证】" -ForegroundColor Green

try {
    $detailResponse = Invoke-RestMethod -Uri "$apiBase/customers/$customerId" -Method GET -Headers $headers
    
    if ($detailResponse.success -eq $true) {
        $customer = $detailResponse.data
        Write-Host "  客户名称: $($customer.officialName)" -ForegroundColor Gray
        Write-Host "  联系人数量: $($customer.contacts.Count)" -ForegroundColor Gray
        Write-Host "  地址数量: $($customer.addresses.Count)" -ForegroundColor Gray
        Write-Host "  银行账户数量: $($customer.bankAccounts.Count)" -ForegroundColor Gray
        Write-Host "  验证成功!" -ForegroundColor Green
    } else {
        Write-Host "  获取详情失败: $($detailResponse.message)" -ForegroundColor Red
    }
} catch {
    Write-Host "  获取详情请求失败: $_" -ForegroundColor Red
}

# ==================== 8. 查询列表验证 ====================
Write-Host "`n【8. 查询列表验证】" -ForegroundColor Green

try {
    $listResponse = Invoke-RestMethod -Uri "$apiBase/customers?pageNumber=1&pageSize=10" -Method GET -Headers $headers
    
    if ($listResponse.success -eq $true) {
        Write-Host "  客户列表总数: $($listResponse.data.totalCount)" -ForegroundColor Gray
        Write-Host "  当前页数量: $($listResponse.data.items.Count)" -ForegroundColor Gray
        Write-Host "  查询成功!" -ForegroundColor Green
    } else {
        Write-Host "  查询失败: $($listResponse.message)" -ForegroundColor Red
    }
} catch {
    Write-Host "  查询请求失败: $_" -ForegroundColor Red
}

# ==================== 9. 更新联系人测试 ====================
Write-Host "`n【9. 更新联系人测试】" -ForegroundColor Green

if ($contactId) {
    $updateContactBody = @{
        name = "张三(已更新)"
        position = "总监"
        mobile = "13800138002"
    } | ConvertTo-Json

    try {
        $updateContactResponse = Invoke-RestMethod -Uri "$apiBase/contacts/$contactId" -Method PUT -Headers $headers -Body $updateContactBody
        
        if ($updateContactResponse.success -eq $true) {
            Write-Host "  联系人更新成功!" -ForegroundColor Green
        } else {
            Write-Host "  联系人更新失败: $($updateContactResponse.message)" -ForegroundColor Red
        }
    } catch {
        Write-Host "  更新联系人请求失败: $_" -ForegroundColor Red
    }
}

# ==================== 10. 更新地址测试 ====================
Write-Host "`n【10. 更新地址测试】" -ForegroundColor Green

if ($addressId) {
    $updateAddressBody = @{
    address = "科技园南路88号(已更新)"
        contactPhone = "13800138003"
    } | ConvertTo-Json

    try {
        $updateAddressResponse = Invoke-RestMethod -Uri "$apiBase/addresses/$addressId" -Method PUT -Headers $headers -Body $updateAddressBody
        
        if ($updateAddressResponse.success -eq $true) {
            Write-Host "  地址更新成功!" -ForegroundColor Green
        } else {
            Write-Host "  地址更新失败: $($updateAddressResponse.message)" -ForegroundColor Red
        }
    } catch {
        Write-Host "  更新地址请求失败: $_" -ForegroundColor Red
    }
}

# ==================== 测试完成 ====================
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "    全业务流程测试完成!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "测试数据汇总:" -ForegroundColor Yellow
Write-Host "  - 注册用户: $testUsername" -ForegroundColor White
Write-Host "  - 客户编码: $customerCode" -ForegroundColor White
Write-Host "  - 客户ID: $customerId" -ForegroundColor White
if ($contactId) { Write-Host "  - 联系人ID: $contactId" -ForegroundColor White }
if ($addressId) { Write-Host "  - 地址ID: $addressId" -ForegroundColor White }
if ($bankId) { Write-Host "  - 银行信息ID: $bankId" -ForegroundColor White }
Write-Host ""
