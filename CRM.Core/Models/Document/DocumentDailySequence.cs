using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Document
{
    /// <summary>
    /// 文档存储文件名当日流水号（yyMMdd + 6位序号，并发安全）
    /// </summary>
    [Table("document_daily_sequence")]
    public class DocumentDailySequence
    {
        [Key]
        [Column("TheDate")]
        public DateTime TheDate { get; set; }

        public int CurrentSequence { get; set; }
    }
}
