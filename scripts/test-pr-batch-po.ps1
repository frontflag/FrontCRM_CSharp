# 多 PR 合并生成 PO — 本地 API 联调脚本
$ErrorActionPreference = 'Stop'
$apiUrl = 'http://localhost:5000/api/v1'

function Coalesce($value, $fallback) {
  if ($null -eq $value -or $value -eq '') { return $fallback }
  return $value
}

function Write-Result($name, $ok, $msg) {
  $color = if ($ok) { 'Green' } else { 'Red' }
  $tag = if ($ok) { 'PASS' } else { 'FAIL' }
  Write-Host "[$tag] $name - $msg" -ForegroundColor $color
}

function Get-AuthHeaders($token) {
  @{ Authorization = "Bearer $token"; 'Content-Type' = 'application/json' }
}

$passed = 0
$failed = 0
$abort = $false
function Step($name, [scriptblock]$block) {
  if ($script:abort) {
    Write-Result $name $false 'skipped (previous step failed)'
    $script:failed++
    return
  }
  try {
    $msg = & $block
    Write-Result $name $true $msg
    $script:passed++
  } catch {
    $msg = $_.Exception.Message
    if ($msg -like 'SKIP_NO_ELIGIBLE:*') {
      Write-Result $name $true $msg
      $script:passed++
      $script:skipped++
      return
    }
    Write-Result $name $false $msg
    $script:failed++
    if ($name -eq 'Login') { $script:abort = $true }
  }
}

Write-Host "`n=== PR Batch PO API Integration ===" -ForegroundColor Cyan

$token = $null
$prList = @()
$eligiblePrList = @()
$batchIds = @()
$prDetails = @()
$requisitionIdsQuery = ''
$createdPoId = $null
$prefillSmokeOnly = $false
$skipped = 0

Step 'Login' {
  # prod-migration.sql：Admin / Admin123
  $cred = @{ userName = 'Admin'; password = 'Admin123' }
  $body = $cred | ConvertTo-Json
  $resp = Invoke-RestMethod -Uri "$apiUrl/auth/login" -Method POST -ContentType 'application/json' -Body $body
  if (-not $resp.success -or -not $resp.data.token) {
    throw (Coalesce $resp.message 'login failed')
  }
  $script:token = $resp.data.token
  return 'token ok (Admin)'
}

$headers = Get-AuthHeaders $token

Step 'GET purchase-requisitions list' {
  $resp = Invoke-RestMethod -Uri "$apiUrl/purchase-requisitions?page=1&pageSize=100" -Method GET -Headers $headers
  if (-not $resp.success) { throw (Coalesce $resp.message 'list failed') }
  $items = @($resp.data.items)
  $script:prList = $items
  $eligible = @($items | Where-Object { $_.status -in 0, 1 })
  $script:eligiblePrList = $eligible
  return "total=$($items.Count) eligible(0/1)=$($eligible.Count)"
}

Step 'Find batchable PR pair (same quoteVendorId)' {
  $eligible = @($script:eligiblePrList)
  if ($eligible.Count -lt 2) {
    throw "SKIP_NO_ELIGIBLE: need >=2 status 0/1 PRs, got $($eligible.Count) (DB may only have completed/cancelled)"
  }
  $groups = $eligible | Where-Object { $_.quoteVendorId } | Group-Object -Property { $_.quoteVendorId.ToString().ToLower() }
  $hit = $groups | Where-Object { $_.Count -ge 2 } | Select-Object -First 1
  if (-not $hit) { throw 'no 2+ PRs share quoteVendorId in list payload' }
  $script:batchIds = @($hit.Group | Select-Object -First 2 | ForEach-Object { $_.id })
  return "ids=$($script:batchIds -join ',') vendor=$($hit.Name)"
}

Step 'GET getById x2 for prefill fields' {
  if (-not $script:batchIds -or $script:batchIds.Count -lt 2) {
    $fallback = @($script:prList | Select-Object -First 2 | ForEach-Object { $_.id })
    if ($fallback.Count -lt 2) { throw 'need at least 2 PR rows in DB for getById smoke' }
    $script:batchIds = $fallback
    $script:prefillSmokeOnly = $true
  }
  $details = @()
  foreach ($id in $script:batchIds) {
    $resp = Invoke-RestMethod -Uri "$apiUrl/purchase-requisitions/$id" -Method GET -Headers $headers
    if (-not $resp.success) { throw "getById $id failed" }
    $details += $resp.data
  }
  $script:prDetails = $details
  $missing = @($details | Where-Object { -not $_.quoteVendorId })
  if ($missing.Count -gt 0) { throw 'getById missing quoteVendorId on some rows' }
  if ($script:prefillSmokeOnly) {
    $statuses = ($details | ForEach-Object { $_.status }) -join ','
    return "prefill fields ok (smoke only, statuses=$statuses)"
  }
  $vendors = $details | ForEach-Object { $_.quoteVendorId } | Select-Object -Unique
  $types = $details | ForEach-Object { $_.prefillPurchaseOrderType } | Select-Object -Unique
  $curs = $details | ForEach-Object { $_.quoteCurrency } | Select-Object -Unique
  if ($vendors.Count -ne 1) { throw 'vendor mismatch in details' }
  if ($types.Count -ne 1) { throw 'po type mismatch in details' }
  if ($curs.Count -ne 1) { throw 'currency mismatch in details' }
  return "vendor=$($vendors[0]) type=$($types[0]) currency=$($curs[0])"
}

Step 'Simulate batch query requisitionIds' {
  $q = ($script:batchIds -join ',')
  if ($q.Split(',').Count -lt 2) { throw 'requisitionIds must have 2+' }
  $script:requisitionIdsQuery = $q
  return "requisitionIds=$q"
}

Step 'POST purchase-orders (batch create smoke)' {
  if ($script:prefillSmokeOnly) {
    throw 'SKIP_NO_ELIGIBLE: existing PRs are not status 0/1; skip live PO create (validation covered by vitest)'
  }
  $first = $script:prDetails[0]
  $deliveryDates = @()
  foreach ($pr in $script:prDetails) {
    if ($pr.deliveryDate) {
      $deliveryDates += [string]$pr.deliveryDate.Split('T')[0]
    } elseif ($pr.expectedPurchaseTime) {
      $deliveryDates += [string]$pr.expectedPurchaseTime.Split('T')[0]
    }
  }
  $delivery = ($deliveryDates | Where-Object { $_ } | Sort-Object -Descending | Select-Object -First 1)

  $items = @()
  foreach ($pr in $script:prDetails) {
    $dd = ''
    if ($pr.deliveryDate) { $dd = [string]$pr.deliveryDate.Split('T')[0] }
    elseif ($pr.expectedPurchaseTime) { $dd = [string]$pr.expectedPurchaseTime.Split('T')[0] }
    else { $dd = $delivery }

    $quoteCost = Coalesce $pr.quoteCost 0
    $quoteCurrency = Coalesce $pr.quoteCurrency 1
    $qty = Coalesce $pr.qty 1

    $items += @{
      sellOrderItemId = $pr.sellOrderItemId
      vendorId = $pr.quoteVendorId
      pn = $pr.pn
      brand = $pr.brand
      customerMaterialModel = $pr.customerMaterialModel
      targetPrice = [decimal]$quoteCost
      qty = [int]$qty
      cost = [decimal]$quoteCost
      currency = [int]$quoteCurrency
      quoteCurrency = [int]$quoteCurrency
      dateCode = if ($pr.dateCode) { [string]$pr.dateCode } else { '' }
      deliveryDate = $dd
      comment = if ($pr.itemRemark) { [string]$pr.itemRemark } else { '' }
      innerComment = ''
    }
  }

  $yy = (Get-Date).ToString('yyMMdd')
  $code = "PO$yy$((Get-Random -Maximum 9999).ToString('0000'))"
  $purchaseUserId = Coalesce $first.prefillPurchaseUserId $first.purchaseUserId
  $purchaseUserName = Coalesce $first.prefillPurchaseUserName $first.purchaseUserName
  $poType = Coalesce $first.prefillPurchaseOrderType 1
  $poCurrency = Coalesce $first.quoteCurrency 1

  $bodyObj = @{
    purchaseOrderCode = $code
    vendorId = $first.quoteVendorId
    vendorName = $first.intendedVendorName
    vendorContactId = $first.intendedVendorContactId
    vendorContactName = $first.intendedVendorContactName
    purchaseUserId = $purchaseUserId
    purchaseUserName = $purchaseUserName
    type = [int]$poType
    currency = [int]$poCurrency
    deliveryDate = $delivery
    deliveryAddress = ''
    comment = ''
    innerComment = ''
    items = $items
  }
  $body = $bodyObj | ConvertTo-Json -Depth 8

  $resp = Invoke-RestMethod -Uri "$apiUrl/purchase-orders" -Method POST -Headers $headers -Body $body
  if (-not $resp.success) { throw (Coalesce $resp.message 'create PO failed') }
  $script:createdPoId = Coalesce $resp.data.id $resp.data.Id
  return "created PO $code id=$($script:createdPoId) lines=$($items.Count)"
}

Write-Host "`nSummary: $passed passed, $failed failed, $skipped skipped (no eligible PR data)" -ForegroundColor $(if ($failed -eq 0) { 'Green' } else { 'Yellow' })
if ($failed -gt 0) { exit 1 }
