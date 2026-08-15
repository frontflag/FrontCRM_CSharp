namespace CRM.Core.Constants;

/// <summary>系统管理细粒度权限码（拆分原 rbac.manage）。</summary>
public static class SystemPermissionCodes
{
    public const string LegacyRbacManage = "rbac.manage";

    public const string OrgUsersRead = "system.org.users.read";
    public const string OrgUsersWrite = "system.org.users.write";
    public const string OrgUsersResetPassword = "system.org.users.reset-password";
    public const string OrgDepartmentsRead = "system.org.departments.read";
    public const string OrgDepartmentsWrite = "system.org.departments.write";
    public const string OrgUserConfigRead = "system.org.user-config.read";
    public const string OrgUserConfigWrite = "system.org.user-config.write";

    public const string RbacRolesRead = "system.rbac.roles.read";
    public const string RbacRolesWrite = "system.rbac.roles.write";
    public const string RbacPermissionsRead = "system.rbac.permissions.read";
    public const string RbacPermissionsWrite = "system.rbac.permissions.write";

    public const string ParamsCompanyRead = "system.params.company.read";
    public const string ParamsCompanyWrite = "system.params.company.write";
    public const string ParamsDictRead = "system.params.dict.read";
    public const string ParamsDictWrite = "system.params.dict.write";

    /// <summary>侧栏「销售参数」。</summary>
    public const string ParamsSalesRead = "system.params.sales.read";
    public const string ParamsSalesWrite = "system.params.sales.write";
    public const string ParamsSalesRefreshCustomerRead = "system.params.sales.refresh-customer.read";
    public const string ParamsSalesRefreshCustomerWrite = "system.params.sales.refresh-customer.write";

    /// <summary>侧栏「采购参数」。</summary>
    public const string ParamsPurchaseRead = "system.params.purchase.read";
    public const string ParamsPurchaseWrite = "system.params.purchase.write";
    public const string ParamsPurchaseAssigneeCountRead = "system.params.purchase.assignee-count.read";
    public const string ParamsPurchaseAssigneeCountWrite = "system.params.purchase.assignee-count.write";
    public const string ParamsPurchaseQuoterPoolRead = "system.params.purchase.quoter-pool.read";
    public const string ParamsPurchaseQuoterPoolWrite = "system.params.purchase.quoter-pool.write";
    public const string ParamsPurchaseDefaultAssignMethodRead = "system.params.purchase.default-assign-method.read";
    public const string ParamsPurchaseDefaultAssignMethodWrite = "system.params.purchase.default-assign-method.write";
    public const string ParamsPurchaseDemandProtectionRead = "system.params.purchase.demand-protection.read";
    public const string ParamsPurchaseDemandProtectionWrite = "system.params.purchase.demand-protection.write";
    public const string ParamsPurchaseRefreshVendorRead = "system.params.purchase.refresh-vendor.read";
    public const string ParamsPurchaseRefreshVendorWrite = "system.params.purchase.refresh-vendor.write";

    /// <summary>侧栏「财务参数」。</summary>
    public const string ParamsFinanceRead = "system.params.finance.read";
    public const string ParamsFinanceWrite = "system.params.finance.write";
    public const string ParamsFinanceExchangeRatesRead = "system.params.finance.exchange-rates.read";
    public const string ParamsFinanceExchangeRatesWrite = "system.params.finance.exchange-rates.write";
    public const string ParamsFinancePurchaseCostParamsRead = "system.params.finance.purchase-cost-params.read";
    public const string ParamsFinancePurchaseCostParamsWrite = "system.params.finance.purchase-cost-params.write";
    public const string ParamsFinancePaymentBanksRead = "system.params.finance.payment-banks.read";
    public const string ParamsFinancePaymentBanksWrite = "system.params.finance.payment-banks.write";

    public const string LogsLoginRead = "system.logs.login.read";
    public const string LogsOperationRead = "system.logs.operation.read";
    public const string LogsExportRead = "system.logs.export.read";

    public static bool IsSystemPermission(string? code) =>
        !string.IsNullOrWhiteSpace(code)
        && (code.StartsWith("system.", StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, LegacyRbacManage, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 参数模块页内子项：system.params.{sales|purchase|finance}.{feature}.(read|write)。
    /// 侧栏入口仍为 system.params.{area}.(read|write)（恰好 4 段）。
    /// 新增子菜单时按此命名即可被角色编辑页自动识别为「页内子项」。
    /// </summary>
    public static bool IsParamsPageSubPermission(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var parts = code.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 5) return false;
        if (!string.Equals(parts[0], "system", StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(parts[1], "params", StringComparison.OrdinalIgnoreCase)) return false;
        var area = parts[2];
        if (!string.Equals(area, "sales", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(area, "purchase", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(area, "finance", StringComparison.OrdinalIgnoreCase))
            return false;
        var action = parts[^1];
        return string.Equals(action, "read", StringComparison.OrdinalIgnoreCase)
               || string.Equals(action, "write", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsParamsModuleMenuPermission(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var parts = code.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 4) return false;
        if (!string.Equals(parts[0], "system", StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(parts[1], "params", StringComparison.OrdinalIgnoreCase)) return false;
        var area = parts[2];
        if (!string.Equals(area, "sales", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(area, "purchase", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(area, "finance", StringComparison.OrdinalIgnoreCase))
            return false;
        return string.Equals(parts[3], "read", StringComparison.OrdinalIgnoreCase)
               || string.Equals(parts[3], "write", StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<string> AllSystemPermissions { get; } = new[]
    {
        OrgUsersRead, OrgUsersWrite, OrgUsersResetPassword,
        OrgDepartmentsRead, OrgDepartmentsWrite,
        OrgUserConfigRead, OrgUserConfigWrite,
        RbacRolesRead, RbacRolesWrite,
        RbacPermissionsRead, RbacPermissionsWrite,
        ParamsCompanyRead, ParamsCompanyWrite,
        ParamsDictRead, ParamsDictWrite,
        ParamsSalesRead, ParamsSalesWrite,
        ParamsSalesRefreshCustomerRead, ParamsSalesRefreshCustomerWrite,
        ParamsPurchaseRead, ParamsPurchaseWrite,
        ParamsPurchaseAssigneeCountRead, ParamsPurchaseAssigneeCountWrite,
        ParamsPurchaseQuoterPoolRead, ParamsPurchaseQuoterPoolWrite,
        ParamsPurchaseDefaultAssignMethodRead, ParamsPurchaseDefaultAssignMethodWrite,
        ParamsPurchaseDemandProtectionRead, ParamsPurchaseDemandProtectionWrite,
        ParamsPurchaseRefreshVendorRead, ParamsPurchaseRefreshVendorWrite,
        ParamsFinanceRead, ParamsFinanceWrite,
        ParamsFinanceExchangeRatesRead, ParamsFinanceExchangeRatesWrite,
        ParamsFinancePurchaseCostParamsRead, ParamsFinancePurchaseCostParamsWrite,
        ParamsFinancePaymentBanksRead, ParamsFinancePaymentBanksWrite,
        LogsLoginRead, LogsOperationRead, LogsExportRead
    };

    /// <summary>Admin（SYS_MANAGER）默认开放：含销售/采购/财务参数及其现有页内子项。</summary>
    public static IReadOnlyList<string> DefaultAdminPermissions { get; } = new[]
    {
        OrgUsersRead, OrgUsersWrite, OrgUsersResetPassword,
        OrgDepartmentsRead, OrgDepartmentsWrite,
        OrgUserConfigRead, OrgUserConfigWrite,
        ParamsCompanyRead, ParamsCompanyWrite,
        ParamsDictRead, ParamsDictWrite,
        ParamsSalesRead, ParamsSalesWrite,
        ParamsSalesRefreshCustomerRead, ParamsSalesRefreshCustomerWrite,
        ParamsPurchaseRead, ParamsPurchaseWrite,
        ParamsPurchaseAssigneeCountRead, ParamsPurchaseAssigneeCountWrite,
        ParamsPurchaseQuoterPoolRead, ParamsPurchaseQuoterPoolWrite,
        ParamsPurchaseDefaultAssignMethodRead, ParamsPurchaseDefaultAssignMethodWrite,
        ParamsPurchaseDemandProtectionRead, ParamsPurchaseDemandProtectionWrite,
        ParamsPurchaseRefreshVendorRead, ParamsPurchaseRefreshVendorWrite,
        ParamsFinanceRead, ParamsFinanceWrite,
        ParamsFinanceExchangeRatesRead, ParamsFinanceExchangeRatesWrite,
        ParamsFinancePurchaseCostParamsRead, ParamsFinancePurchaseCostParamsWrite,
        ParamsFinancePaymentBanksRead, ParamsFinancePaymentBanksWrite,
        LogsLoginRead, LogsOperationRead, LogsExportRead
    };

    /// <summary>Manager（SYS_BIZ_MANAGER）默认仅员工管理。</summary>
    public static IReadOnlyList<string> DefaultManagerPermissions { get; } = new[]
    {
        OrgUsersRead, OrgUsersWrite, OrgUsersResetPassword
    };
}
