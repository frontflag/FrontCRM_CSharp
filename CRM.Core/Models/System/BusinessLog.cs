using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System
{
    /// <summary>
    /// 业务操作日志主表
    /// 记录所有业务动作的操作日志
    /// </summary>
    [Table("businesslog")]
    public class BusinessLog : BaseGuidEntity
    {
        /// <summary>
        /// 日志ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("LogId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 业务模块类型
        /// 1:客户管理 2:供应商管理 3:询价管理 4:报价管理 
        /// 5:销售管理 6:采购管理 7:物料管理 8:库存管理 9:财务管理
        /// </summary>
        [Required]
        public short BusinessModule { get; set; }

        /// <summary>
        /// 业务动作类型
        /// 1:新增 2:修改 3:删除 4:查询 5:审核 6:反审核 
        /// 7:提交 8:撤回 9:导入 10:导出 11:打印 12:发送
        /// </summary>
        [Required]
        public short ActionType { get; set; }

        /// <summary>
        /// 业务单据类型
        /// 如: CustomerInfo, RFQ, Quote, SellOrder, PurchaseOrder 等
        /// </summary>
        [Required]
        [StringLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        /// <summary>
        /// 业务数据ID (被操作的数据记录ID)
        /// </summary>
        [Required]
        [StringLength(36)]
        public string BusinessDataId { get; set; } = string.Empty;

        /// <summary>
        /// 业务单据编号
        /// 如: 客户编码、订单号、询价单号等
        /// </summary>
        [StringLength(50)]
        public string? DocumentCode { get; set; }

        /// <summary>
        /// 操作人ID
        /// </summary>
        [Required]
        [StringLength(36)]
        public string OperatorId { get; set; } = string.Empty;

        /// <summary>
        /// 操作人名称
        /// </summary>
        [StringLength(50)]
        public string? OperatorName { get; set; }

        /// <summary>
        /// 操作时间
        /// </summary>
        public DateTime OperationTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 操作IP地址
        /// </summary>
        [StringLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// 操作设备信息
        /// </summary>
        [StringLength(200)]
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// 操作结果
        /// 0:失败 1:成功
        /// </summary>
        public bool OperationResult { get; set; } = true;

        /// <summary>
        /// 操作状态描述
        /// 成功/失败原因
        /// </summary>
        [StringLength(200)]
        public string? ResultMessage { get; set; }

        /// <summary>
        /// 操作描述
        /// 如: "创建客户[ABC公司]", "修改订单[SO2024001]状态为已审核"
        /// </summary>
        [StringLength(500)]
        public string? OperationDescription { get; set; }

        /// <summary>
        /// 操作内容摘要
        /// 记录关键字段变更信息
        /// </summary>
        [StringLength(1000)]
        public string? OperationSummary { get; set; }

        /// <summary>
        /// 关联单据ID
        /// 用于关联上下游单据，如询价单→报价单→订单
        /// </summary>
        [StringLength(36)]
        public string? RelatedDocumentId { get; set; }

        /// <summary>
        /// 关联单据类型
        /// </summary>
        [StringLength(50)]
        public string? RelatedDocumentType { get; set; }

        /// <summary>
        /// 审批流程ID
        /// 如果是审批操作，关联审批流程
        /// </summary>
        [StringLength(36)]
        public string? ApprovalFlowId { get; set; }

        /// <summary>
        /// 审批节点ID
        /// </summary>
        [StringLength(36)]
        public string? ApprovalNodeId { get; set; }

        /// <summary>
        /// 数据来源
        /// 1:PC端 2:移动端 3:API接口 4:系统定时任务 5:数据导入
        /// </summary>
        public short DataSource { get; set; } = 1;

        /// <summary>
        /// 租户ID (多租户支持)
        /// </summary>
        [StringLength(36)]
        public string? TenantId { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        // 导航属性
        public virtual ICollection<BusinessLogDetail> Details { get; set; } = new List<BusinessLogDetail>();
    }

    /// <summary>
    /// 业务操作日志明细表
    /// 记录字段级别的变更详情
    /// </summary>
    [Table("businesslogdetail")]
    public class BusinessLogDetail : BaseGuidEntity
    {
        /// <summary>
        /// 明细ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("DetailId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 日志ID (外键)
        /// </summary>
        [Required]
        [StringLength(36)]
        public string LogId { get; set; } = string.Empty;

        /// <summary>
        /// 字段名称
        /// </summary>
        [Required]
        [StringLength(50)]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// 字段描述
        /// </summary>
        [StringLength(100)]
        public string? FieldDescription { get; set; }

        /// <summary>
        /// 旧值
        /// </summary>
        [StringLength(1000)]
        public string? OldValue { get; set; }

        /// <summary>
        /// 新值
        /// </summary>
        [StringLength(1000)]
        public string? NewValue { get; set; }

        /// <summary>
        /// 变更类型
        /// 1:新增 2:修改 3:删除
        /// </summary>
        public short ChangeType { get; set; } = 2;

        /// <summary>
        /// 数据类型
        /// string/int/decimal/datetime/bool等
        /// </summary>
        [StringLength(20)]
        public string? DataType { get; set; }

        /// <summary>
        /// 是否关键字段
        /// </summary>
        public bool IsKeyField { get; set; } = false;

        // 导航属性
        [ForeignKey("LogId")]
        public virtual BusinessLog? BusinessLog { get; set; }
    }

    /// <summary>
    /// 业务日志查询视图模型
    /// </summary>
    public class BusinessLogViewModel
    {
        public string LogId { get; set; } = string.Empty;
        public string BusinessModuleName { get; set; } = string.Empty;
        public string ActionTypeName { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string DocumentCode { get; set; } = string.Empty;
        public string OperatorName { get; set; } = string.Empty;
        public DateTime OperationTime { get; set; }
        public string OperationDescription { get; set; } = string.Empty;
        public bool OperationResult { get; set; }
    }

    /// <summary>
    /// 业务模块枚举
    /// </summary>
    public enum BusinessModule
    {
        客户管理 = 1,
        供应商管理 = 2,
        询价管理 = 3,
        报价管理 = 4,
        销售管理 = 5,
        采购管理 = 6,
        物料管理 = 7,
        库存管理 = 8,
        财务管理 = 9,
        系统管理 = 10
    }

    /// <summary>
    /// 业务动作类型枚举
    /// </summary>
    public enum ActionType
    {
        新增 = 1,
        修改 = 2,
        删除 = 3,
        查询 = 4,
        审核 = 5,
        反审核 = 6,
        提交 = 7,
        撤回 = 8,
        导入 = 9,
        导出 = 10,
        打印 = 11,
        发送 = 12,
        启用 = 13,
        禁用 = 14,
        归档 = 15,
        恢复 = 16
    }

    /// <summary>
    /// 数据来源枚举
    /// </summary>
    public enum DataSource
    {
        PC端 = 1,
        移动端 = 2,
        API接口 = 3,
        系统定时任务 = 4,
        数据导入 = 5
    }
}
