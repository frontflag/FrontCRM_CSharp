using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace CRM.Core.Models.System
{
    /// <summary>
    /// 数据类型枚举
    /// </summary>
    public enum ParamDataType
    {
        /// <summary>
        /// 字符串
        /// </summary>
        String = 1,

        /// <summary>
        /// 整数
        /// </summary>
        Integer = 2,

        /// <summary>
        /// 实数(带小数)
        /// </summary>
        Decimal = 3,

        /// <summary>
        /// 布尔
        /// </summary>
        Boolean = 4,

        /// <summary>
        /// JSON对象
        /// </summary>
        Json = 5
    }

    /// <summary>
    /// 系统参数主表
    /// 支持多种数据类型和数组
    /// </summary>
    [Table("sysparam")]
    public class SysParam : BaseGuidEntity
    {
        /// <summary>
        /// 参数编码
        /// 格式建议：Group.SubGroup.ParamName
        /// 例如：System.Email.SMTPServer
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Column("ParamCode")]
        public string ParamCode { get; set; } = string.Empty;

        /// <summary>
        /// 参数名称
        /// </summary>
        [Required]
        [MaxLength(200)]
        [Column("ParamName")]
        public string ParamName { get; set; } = string.Empty;

        /// <summary>
        /// 所属分组ID
        /// </summary>
        [Column("GroupId")]
        public Guid? GroupId { get; set; }

        /// <summary>
        /// 数据类型
        /// 枚举：1=字符串, 2=整数, 3=实数, 4=布尔, 5=JSON
        /// </summary>
        [Column("DataType")]
        public ParamDataType DataType { get; set; } = ParamDataType.String;

        /// <summary>
        /// 字符串类型值
        /// </summary>
        [MaxLength(500)]
        [Column("ValueString")]
        public string? ValueString { get; set; }

        /// <summary>
        /// 整数类型值
        /// </summary>
        [Column("ValueInt")]
        public long? ValueInt { get; set; }

        /// <summary>
        /// 实数类型值(带小数)
        /// 数据库字段类型: decimal(18,4)
        /// </summary>
        [Column("ValueDecimal")]
        public decimal? ValueDecimal { get; set; }

        /// <summary>
        /// JSON格式值（用于数组或复杂对象）
        /// </summary>
        [Column("ValueJson")]
        public string? ValueJson { get; set; }

        /// <summary>
        /// 默认值
        /// </summary>
        [MaxLength(500)]
        [Column("DefaultValue")]
        public string? DefaultValue { get; set; }

        /// <summary>
        /// 参数说明
        /// </summary>
        [MaxLength(500)]
        [Column("Description")]
        public string? Description { get; set; }

        /// <summary>
        /// 是否为数组类型
        /// true=是数组, false=单值
        /// </summary>
        [Column("IsArray")]
        public bool IsArray { get; set; } = false;

        /// <summary>
        /// 是否系统内置参数
        /// true=系统参数, false=自定义参数
        /// </summary>
        [Column("IsSystem")]
        public bool IsSystem { get; set; } = false;

        /// <summary>
        /// 是否可编辑
        /// true=可编辑, false=只读
        /// </summary>
        [Column("IsEditable")]
        public bool IsEditable { get; set; } = true;

        /// <summary>
        /// 是否可见
        /// true=可见, false=隐藏
        /// </summary>
        [Column("IsVisible")]
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// 排序序号
        /// </summary>
        [Column("SortOrder")]
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// 状态
        /// 枚举：0=禁用, 1=启用
        /// </summary>
        [Column("Status")]
        public short Status { get; set; } = 1;

        /// <summary>
        /// 最后修改时间
        /// </summary>
        [Column("ModifyTime")]
        public DateTime? ModifyTime { get; set; }

        // 导航属性
        /// <summary>
        /// 所属分组
        /// </summary>
        [ForeignKey("GroupId")]
        public virtual SysParamGroup? Group { get; set; }

        /// <summary>
        /// 历史记录
        /// </summary>
        public virtual ICollection<SysParamHistory> Histories { get; set; } = new List<SysParamHistory>();

        #region 便捷方法

        /// <summary>
        /// 获取字符串值
        /// </summary>
        public string GetStringValue()
        {
            if (IsArray)
            {
                throw new InvalidOperationException("此参数为数组类型，请使用 GetStringArray() 方法");
            }
            return DataType == ParamDataType.String ? ValueString ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// 获取整数值
        /// </summary>
        public long GetIntValue()
        {
            if (IsArray)
            {
                throw new InvalidOperationException("此参数为数组类型，请使用 GetIntArray() 方法");
            }
            return DataType == ParamDataType.Integer ? (ValueInt ?? 0) : 0;
        }

        /// <summary>
        /// 获取实数值
        /// </summary>
        public decimal GetDecimalValue()
        {
            if (IsArray)
            {
                throw new InvalidOperationException("此参数为数组类型，请使用 GetDecimalArray() 方法");
            }
            return DataType == ParamDataType.Decimal ? (ValueDecimal ?? 0m) : 0m;
        }

        /// <summary>
        /// 获取布尔值
        /// </summary>
        public bool GetBoolValue()
        {
            if (IsArray)
            {
                throw new InvalidOperationException("此参数为数组类型");
            }
            if (DataType == ParamDataType.Boolean)
            {
                return bool.TryParse(ValueString, out var result) && result;
            }
            return false;
        }

        /// <summary>
        /// 获取字符串数组
        /// </summary>
        public List<string> GetStringArray()
        {
            if (!IsArray || string.IsNullOrEmpty(ValueJson))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(ValueJson) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 获取整数数组
        /// </summary>
        public List<long> GetIntArray()
        {
            if (!IsArray || string.IsNullOrEmpty(ValueJson))
                return new List<long>();

            try
            {
                return JsonSerializer.Deserialize<List<long>>(ValueJson) ?? new List<long>();
            }
            catch
            {
                return new List<long>();
            }
        }

        /// <summary>
        /// 获取实数数组
        /// </summary>
        public List<decimal> GetDecimalArray()
        {
            if (!IsArray || string.IsNullOrEmpty(ValueJson))
                return new List<decimal>();

            try
            {
                return JsonSerializer.Deserialize<List<decimal>>(ValueJson) ?? new List<decimal>();
            }
            catch
            {
                return new List<decimal>();
            }
        }

        /// <summary>
        /// 获取JSON对象
        /// </summary>
        public T? GetJsonValue<T>() where T : class
        {
            if (DataType != ParamDataType.Json || string.IsNullOrEmpty(ValueJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<T>(ValueJson);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 设置字符串值
        /// </summary>
        public void SetStringValue(string value)
        {
            DataType = ParamDataType.String;
            ValueString = value;
            IsArray = false;
        }

        /// <summary>
        /// 设置整数值
        /// </summary>
        public void SetIntValue(long value)
        {
            DataType = ParamDataType.Integer;
            ValueInt = value;
            IsArray = false;
        }

        /// <summary>
        /// 设置实数值
        /// </summary>
        public void SetDecimalValue(decimal value)
        {
            DataType = ParamDataType.Decimal;
            ValueDecimal = value;
            IsArray = false;
        }

        /// <summary>
        /// 设置布尔值
        /// </summary>
        public void SetBoolValue(bool value)
        {
            DataType = ParamDataType.Boolean;
            ValueString = value.ToString().ToLower();
            IsArray = false;
        }

        /// <summary>
        /// 设置字符串数组
        /// </summary>
        public void SetStringArray(List<string> values)
        {
            DataType = ParamDataType.String;
            ValueJson = JsonSerializer.Serialize(values);
            IsArray = true;
        }

        /// <summary>
        /// 设置整数数组
        /// </summary>
        public void SetIntArray(List<long> values)
        {
            DataType = ParamDataType.Integer;
            ValueJson = JsonSerializer.Serialize(values);
            IsArray = true;
        }

        /// <summary>
        /// 设置实数数组
        /// </summary>
        public void SetDecimalArray(List<decimal> values)
        {
            DataType = ParamDataType.Decimal;
            ValueJson = JsonSerializer.Serialize(values);
            IsArray = true;
        }

        /// <summary>
        /// 设置JSON对象
        /// </summary>
        public void SetJsonValue<T>(T value) where T : class
        {
            DataType = ParamDataType.Json;
            ValueJson = JsonSerializer.Serialize(value);
            IsArray = false;
        }

        #endregion
    }
}
