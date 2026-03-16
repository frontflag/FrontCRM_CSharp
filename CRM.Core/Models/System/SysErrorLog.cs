using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System
{
    /// <summary>
    /// 系统错误日志表
    /// 记录每次业务操作失败的详细信息
    /// </summary>
    [Table("sys_error_log")]
    public class SysErrorLog
    {
        /// <summary>
        /// 主键ID（自增）
        /// </summary>
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// 错误发生时间
        /// </summary>
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 业务模块名称（如：客户管理、采购订单等）
        /// </summary>
        [StringLength(100)]
        public string ModuleName { get; set; } = string.Empty;

        /// <summary>
        /// 操作类型（如：创建、更新、删除）
        /// </summary>
        [StringLength(50)]
        public string? OperationType { get; set; }

        /// <summary>
        /// 错误信息摘要
        /// </summary>
        [StringLength(500)]
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 完整错误堆栈（Inner Exception 等）
        /// </summary>
        public string? ErrorDetail { get; set; }

        /// <summary>
        /// 相关单据号码（如客户编号、订单号等）
        /// </summary>
        [StringLength(50)]
        public string? DocumentNo { get; set; }

        /// <summary>
        /// 相关数据ID（如客户ID、订单ID等）
        /// </summary>
        [StringLength(36)]
        public string? DataId { get; set; }

        /// <summary>
        /// 操作用户ID
        /// </summary>
        [StringLength(36)]
        public string? UserId { get; set; }

        /// <summary>
        /// 操作用户名
        /// </summary>
        [StringLength(50)]
        public string? UserName { get; set; }

        /// <summary>
        /// 请求路径（API URL）
        /// </summary>
        [StringLength(200)]
        public string? RequestPath { get; set; }

        /// <summary>
        /// 请求参数（JSON）
        /// </summary>
        public string? RequestBody { get; set; }

        /// <summary>
        /// 是否已处理
        /// </summary>
        public bool IsResolved { get; set; } = false;

        /// <summary>
        /// 处理备注
        /// </summary>
        [StringLength(200)]
        public string? ResolveRemark { get; set; }
    }
}
