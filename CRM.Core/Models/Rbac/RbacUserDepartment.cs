using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Rbac
{
    [Table("sys_user_department")]
    public class RbacUserDepartment : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("UserDepartmentId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(36)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(36)]
        public string DepartmentId { get; set; } = string.Empty;

        public bool IsPrimary { get; set; } = false;
    }
}
