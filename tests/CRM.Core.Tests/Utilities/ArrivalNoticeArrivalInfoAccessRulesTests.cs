using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class ArrivalNoticeArrivalInfoAccessRulesTests
{
    [Fact]
    public void SysAdmin_CanEdit()
    {
        Assert.True(ArrivalNoticeArrivalInfoAccessRules.CanEdit(new UserPermissionSummaryDto
        {
            IsSysAdmin = true,
            IdentityType = 1
        }));
    }

    [Fact]
    public void SysManager_CanEdit()
    {
        Assert.True(ArrivalNoticeArrivalInfoAccessRules.CanEdit(new UserPermissionSummaryDto
        {
            IsSysManager = true,
            IdentityType = 1
        }));
    }

    [Fact]
    public void BizManager_CanEdit()
    {
        Assert.True(ArrivalNoticeArrivalInfoAccessRules.CanEdit(new UserPermissionSummaryDto
        {
            IsBizManager = true,
            IdentityType = 1,
            LogisticsDataAccess = 1
        }));
    }

    [Fact]
    public void LogisticsDirector_CanEdit_WhenWritable()
    {
        Assert.True(ArrivalNoticeArrivalInfoAccessRules.CanEdit(new UserPermissionSummaryDto
        {
            IdentityType = 6,
            RoleCodes = new[] { "DEPT_DIRECTOR" },
            LogisticsDataAccess = 0
        }));
    }

    [Fact]
    public void LogisticsManager_CanEdit_WhenWritable()
    {
        Assert.True(ArrivalNoticeArrivalInfoAccessRules.CanEdit(new UserPermissionSummaryDto
        {
            IdentityType = 6,
            RoleCodes = new[] { "DEPT_MANAGER" },
            LogisticsDataAccess = 0
        }));
    }

    [Fact]
    public void LogisticsLead_Denied_WhenDepartmentReadOnly()
    {
        Assert.False(ArrivalNoticeArrivalInfoAccessRules.CanEdit(new UserPermissionSummaryDto
        {
            IdentityType = 6,
            RoleCodes = new[] { "DEPT_DIRECTOR" },
            LogisticsDataAccess = 1
        }));
    }

    [Fact]
    public void LogisticsEmployee_Denied()
    {
        Assert.False(ArrivalNoticeArrivalInfoAccessRules.CanEdit(new UserPermissionSummaryDto
        {
            IdentityType = 6,
            RoleCodes = new[] { "DEPT_EMPLOYEE" },
            LogisticsDataAccess = 0
        }));
    }

    [Fact]
    public void SalesDirector_Denied()
    {
        Assert.False(ArrivalNoticeArrivalInfoAccessRules.CanEdit(new UserPermissionSummaryDto
        {
            IdentityType = 1,
            RoleCodes = new[] { "DEPT_DIRECTOR" }
        }));
    }

    [Fact]
    public void DomainAdmin_WithoutListedRole_Denied()
    {
        Assert.False(ArrivalNoticeArrivalInfoAccessRules.CanEdit(new UserPermissionSummaryDto
        {
            IdentityType = 6,
            RoleCodes = new[] { "SYS_MGR_LOGISTICS" },
            LogisticsDataAccess = 0
        }));
    }
}
