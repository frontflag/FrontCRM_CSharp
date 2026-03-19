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

        /// <summary>0=全部,1=自己,2=本部门,3=本部门及下级,4=禁止</summary>
        public short PurchaseDataScope { get; set; } = 1;

        /// <summary>0=None,1=Sales,2=Purchaser,3=PurchaseAssistant,4=CustService,5=Finance,6=Logistics</summary>
        public short IdentityType { get; set; } = 0;

        public short Status { get; set; } = 1;
    }
}
