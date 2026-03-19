using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Favorite
{
    [Table("user_favorite")]
    public class UserFavorite : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("FavoriteId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public long UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string EntityType { get; set; } = string.Empty;

        [Required]
        [StringLength(36)]
        public string EntityId { get; set; } = string.Empty;
    }
}
