using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Rbac
{
    [Table("sys_permission")]
    public class RbacPermission : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("PermissionId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string PermissionCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string PermissionName { get; set; } = string.Empty;

        /// <summary>menu/api/button/data</summary>
        [StringLength(20)]
        public string PermissionType { get; set; } = "api";

        [StringLength(200)]
        public string? Resource { get; set; }

        [StringLength(50)]
        public string? Action { get; set; }

        public short Status { get; set; } = 1;
    }
}
