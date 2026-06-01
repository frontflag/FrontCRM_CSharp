using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Rbac
{
    [Table("sys_department")]
    public class RbacDepartment : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("DepartmentId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string DepartmentName { get; set; } = string.Empty;

        [StringLength(36)]
        public string? ParentId { get; set; }

        [StringLength(500)]
        public string? Path { get; set; }

        public int Level { get; set; } = 1;

        /// <summary>0=全部,1=自己,2=本部门,3=本部门及下级,4=禁止</summary>
        public short SaleDataScope { get; set; } = 1;

        /// <summary>销售数据访问：0=读写,1=只读（与 SaleDataScope 独立；只读时剥离销售侧写权限码）</summary>
        public short SaleDataAccess { get; set; } = 0;

        /// <summary>隐藏客户管理菜单并拦截客户模块路由（与 SaleDataScope 独立）</summary>
        public bool HideCustomerManagement { get; set; }

        /// <summary>0=全部,1=自己,2=本部门,3=本部门及下级,4=禁止</summary>
        public short PurchaseDataScope { get; set; } = 1;

        /// <summary>采购数据访问：0=读写,1=只读（与 PurchaseDataScope 独立；只读时剥离采购侧写权限码）</summary>
        public short PurchaseDataAccess { get; set; } = 0;

        /// <summary>隐藏供应商管理菜单并拦截供应商模块路由（与 PurchaseDataScope 独立）</summary>
        public bool HideVendorManagement { get; set; }

        /// <summary>0=全部,1=自己,2=本部门,3=本部门及下级,4=禁止（入库/出库/库存/报关菜单组）</summary>
        public short LogisticsDataScope { get; set; } = 0;

        /// <summary>物流数据访问：0=读写,1=只读（与 LogisticsDataScope 独立）</summary>
        public short LogisticsDataAccess { get; set; } = 0;

        /// <summary>0=全部,1=自己,2=本部门,3=本部门及下级,4=禁止（付款管理/收款管理菜单组）</summary>
        public short FinanceDataScope { get; set; } = 0;

        /// <summary>财务数据访问：0=读写,1=只读（与 FinanceDataScope 独立）</summary>
        public short FinanceDataAccess { get; set; } = 0;

        /// <summary>0=None,1=Sales,2=Purchaser,3=PurchaseAssistant,4=CustService,5=Finance,6=Logistics</summary>
        public short IdentityType { get; set; } = 0;

        public short Status { get; set; } = 1;
    }
}
