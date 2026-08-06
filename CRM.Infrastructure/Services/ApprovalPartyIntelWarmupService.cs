using System.Text.Json;
using CRM.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Services;

/// <summary>
/// 提交审核后：若审批桌面会展示客户/供应商调查且尚无可用报告，则后台自动调查。
/// </summary>
public sealed class ApprovalPartyIntelWarmupService : IApprovalPartyIntelWarmupService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ApprovalPartyIntelWarmupService> _logger;

    public ApprovalPartyIntelWarmupService(
        IServiceScopeFactory scopeFactory,
        ILogger<ApprovalPartyIntelWarmupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void ScheduleAfterSubmit(string bizType, string businessId, string? userId)
    {
        var bt = (bizType ?? string.Empty).Trim().ToUpperInvariant();
        var id = (businessId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(bt) || string.IsNullOrEmpty(id)) return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var sp = scope.ServiceProvider;
                switch (bt)
                {
                    case "CUSTOMER":
                        await EnsureCustomerAsync(sp, id, userId);
                        break;
                    case "VENDOR":
                        await EnsureVendorAsync(sp, id, userId);
                        break;
                    case "SALES_ORDER":
                    {
                        var so = await sp.GetRequiredService<ISalesOrderService>().GetByIdAsync(id);
                        if (!string.IsNullOrWhiteSpace(so?.CustomerId))
                            await EnsureCustomerAsync(sp, so!.CustomerId, userId);
                        break;
                    }
                    case "PURCHASE_ORDER":
                    {
                        var po = await sp.GetRequiredService<IPurchaseOrderService>().GetByIdAsync(id);
                        if (!string.IsNullOrWhiteSpace(po?.VendorId))
                            await EnsureVendorAsync(sp, po!.VendorId, userId);
                        break;
                    }
                    case "FINANCE_RECEIPT":
                    {
                        var receipt = await sp.GetRequiredService<IFinanceReceiptService>().GetByIdAsync(id);
                        if (!string.IsNullOrWhiteSpace(receipt?.CustomerId))
                            await EnsureCustomerAsync(sp, receipt!.CustomerId, userId);
                        break;
                    }
                    case "FINANCE_PAYMENT":
                    {
                        var payment = await sp.GetRequiredService<IFinancePaymentService>().GetByIdAsync(id);
                        if (!string.IsNullOrWhiteSpace(payment?.VendorId))
                            await EnsureVendorAsync(sp, payment!.VendorId, userId);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "审批提交后客户/供应商调查预热失败 BizType={BizType} BusinessId={BusinessId}", bt, id);
            }
        });
    }

    private static async Task EnsureCustomerAsync(IServiceProvider sp, string customerId, string? userId)
    {
        var intel = sp.GetRequiredService<ICustomerIntelReportService>();
        var customers = sp.GetRequiredService<ICustomerService>();

        var latest = await intel.GetLatestByCustomerIdAsync(customerId);
        if (HasUsableReport(latest?.Report)) return;

        var customer = await customers.GetCustomerByIdAsync(customerId);
        if (customer == null) return;

        var companyName = (customer.OfficialName ?? customer.NickName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(companyName)) return;

        await intel.InvestigateAsync(
            new CustomerIntelInvestigateRequest
            {
                CustomerId = customerId,
                CompanyName = companyName,
                CreditCode = customer.CreditCode,
                Region = customer.City ?? customer.Region,
                ForceRefresh = false
            },
            userId);
    }

    private static async Task EnsureVendorAsync(IServiceProvider sp, string vendorId, string? userId)
    {
        var intel = sp.GetRequiredService<IVendorIntelReportService>();
        var vendors = sp.GetRequiredService<IVendorService>();

        var latest = await intel.GetLatestByVendorIdAsync(vendorId);
        if (HasUsableReport(latest?.Report)) return;

        var vendor = await vendors.GetByIdAsync(vendorId);
        if (vendor == null) return;

        var companyName = (vendor.OfficialName ?? vendor.NickName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(companyName)) return;

        await intel.InvestigateAsync(
            new VendorIntelInvestigateRequest
            {
                VendorId = vendorId,
                CompanyName = companyName,
                CreditCode = vendor.CreditCode,
                Region = null,
                ForceRefresh = false
            },
            userId);
    }

    private static bool HasUsableReport(object? report)
    {
        if (report == null) return false;

        if (report is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.Null || je.ValueKind == JsonValueKind.Undefined)
                return false;
            if (je.ValueKind == JsonValueKind.Object)
                return je.EnumerateObject().Any();
            if (je.ValueKind == JsonValueKind.Array)
                return je.GetArrayLength() > 0;
            if (je.ValueKind == JsonValueKind.String)
                return !string.IsNullOrWhiteSpace(je.GetString());
            return true;
        }

        try
        {
            var json = JsonSerializer.Serialize(report);
            if (string.IsNullOrWhiteSpace(json) || json is "{}" or "null" or "[]")
                return false;
            return true;
        }
        catch
        {
            return true;
        }
    }
}
