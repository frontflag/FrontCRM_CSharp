using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Rbac
{
    [Table("sys_role")]
    public class RbacRole : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("RoleId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(50)]
        public string RoleCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public short Status { get; set; } = 1;
    }
}
