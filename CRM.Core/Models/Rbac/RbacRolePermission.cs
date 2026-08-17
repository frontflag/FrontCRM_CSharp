using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Rbac
{
    [Table("sys_role_permission")]
    public class RbacRolePermission : BaseGuidEntity, ISoftDeletable
    {
        [Key]
        [StringLength(36)]
        [Column("RolePermissionId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(36)]
        public string RoleId { get; set; } = string.Empty;

        [Required]
        [StringLength(36)]
        public string PermissionId { get; set; } = string.Empty;

        [Column("is_deleted")]
        public bool IsDeleted { get; set; }
    }
}
