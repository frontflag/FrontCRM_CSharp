using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Rbac
{
    [Table("sys_user_role")]
    public class RbacUserRole : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("UserRoleId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(36)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(36)]
        public string RoleId { get; set; } = string.Empty;
    }
}
