using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Models;

namespace CRM.Core.Models.Document
{
    /// <summary>
    /// 上传文档元数据（与业务关联）
    /// </summary>
    [Table("upload_document")]
    public class UploadDocument : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("DocumentId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>业务类型：Customer, Vendor, SalesOrder, PurchaseOrder, StockIn 等</summary>
        [Required]
        [StringLength(50)]
        public string BizType { get; set; } = string.Empty;

        /// <summary>业务主键或业务编号</summary>
        [Required]
        [StringLength(36)]
        public string BizId { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string StoredFileName { get; set; } = string.Empty;

        /// <summary>相对 RootPath 的路径，如 UploadFile/Customer/xxx/file.pdf</summary>
        [Required]
        [StringLength(500)]
        public string RelativePath { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [StringLength(20)]
        public string? FileExtension { get; set; }

        [StringLength(100)]
        public string? MimeType { get; set; }

        /// <summary>缩略图相对路径（图片时）</summary>
        [StringLength(500)]
        public string? ThumbnailRelativePath { get; set; }

        [StringLength(256)]
        public string? Remark { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeleteTime { get; set; }

        [StringLength(36)]
        public string? DeleteUserId { get; set; }

        /// <summary>上传人ID（GUID 或 用户标识）</summary>
        [StringLength(36)]
        public string? UploadUserId { get; set; }
    }
}
