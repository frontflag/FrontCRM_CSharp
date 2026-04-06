using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers
{
    /// <summary>
    /// 数据字典（下拉选项等）
    /// </summary>
    [ApiController]
    [Route("api/v1/dictionaries")]
    [Authorize]
    public class DictionariesController : ControllerBase
    {
        private readonly IDictionaryService _dictionaryService;
        private readonly ILogger<DictionariesController> _logger;

        public DictionariesController(IDictionaryService dictionaryService, ILogger<DictionariesController> logger)
        {
            _dictionaryService = dictionaryService;
            _logger = logger;
        }

        /// <summary>
        /// 按类别获取字典项（逗号分隔多个 Category）
        /// </summary>
        [HttpGet("batch")]
        public async Task<ActionResult<ApiResponse<Dictionary<string, List<DictionaryItemDto>>>>> GetBatch(
            [FromQuery] string categories,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(categories))
            {
                return BadRequest(ApiResponse<Dictionary<string, List<DictionaryItemDto>>>.Fail("categories 必填"));
            }

            var list = categories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (list.Count == 0)
            {
                return BadRequest(ApiResponse<Dictionary<string, List<DictionaryItemDto>>>.Fail("categories 无效"));
            }

            try
            {
                var preferEn = PreferEnglish(Request);
                var map = await _dictionaryService.GetBatchAsync(list, preferEn, cancellationToken);
                var body = map.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.ToList(),
                    StringComparer.Ordinal);
                return Ok(ApiResponse<Dictionary<string, List<DictionaryItemDto>>>.Ok(body, "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "批量读取字典失败 categories={Categories}", categories);
                return StatusCode(500,
                    ApiResponse<Dictionary<string, List<DictionaryItemDto>>>.Fail("读取字典失败"));
            }
        }

        /// <summary>
        /// 供应商编辑等常用字典一次拉取
        /// </summary>
        [HttpGet("vendor-form")]
        public async Task<ActionResult<ApiResponse<Dictionary<string, List<DictionaryItemDto>>>>> GetVendorForm(
            CancellationToken cancellationToken)
        {
            var list = new[]
            {
                DictCategories.VendorIndustry,
                DictCategories.VendorLevel,
                DictCategories.VendorIdentity,
                DictCategories.VendorPaymentMethod
            };

            try
            {
                var preferEn = PreferEnglish(Request);
                var map = await _dictionaryService.GetBatchAsync(list, preferEn, cancellationToken);
                var body = map.ToDictionary(kv => kv.Key, kv => kv.Value.ToList(), StringComparer.Ordinal);
                return Ok(ApiResponse<Dictionary<string, List<DictionaryItemDto>>>.Ok(body, "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取供应商表单字典失败");
                return StatusCode(500,
                    ApiResponse<Dictionary<string, List<DictionaryItemDto>>>.Fail("读取字典失败"));
            }
        }

        /// <summary>
        /// 客户编辑/列表筛选等常用字典一次拉取
        /// </summary>
        [HttpGet("customer-form")]
        public async Task<ActionResult<ApiResponse<Dictionary<string, List<DictionaryItemDto>>>>> GetCustomerForm(
            CancellationToken cancellationToken)
        {
            var list = new[]
            {
                DictCategories.CustomerType,
                DictCategories.CustomerLevel,
                DictCategories.CustomerIndustry,
                DictCategories.CustomerTaxRate,
                DictCategories.CustomerInvoiceType
            };

            try
            {
                var preferEn = PreferEnglish(Request);
                var map = await _dictionaryService.GetBatchAsync(list, preferEn, cancellationToken);
                var body = map.ToDictionary(kv => kv.Key, kv => kv.Value.ToList(), StringComparer.Ordinal);
                return Ok(ApiResponse<Dictionary<string, List<DictionaryItemDto>>>.Ok(body, "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取客户表单字典失败");
                return StatusCode(500,
                    ApiResponse<Dictionary<string, List<DictionaryItemDto>>>.Fail("读取字典失败"));
            }
        }

        /// <summary>
        /// 需求/报价/销售订单等「生产日期」下拉一次拉取（Category: MaterialProductionDate）
        /// </summary>
        [HttpGet("material-form")]
        public async Task<ActionResult<ApiResponse<Dictionary<string, List<DictionaryItemDto>>>>> GetMaterialForm(
            CancellationToken cancellationToken)
        {
            var list = new[] { DictCategories.MaterialProductionDate };

            try
            {
                var preferEn = PreferEnglish(Request);
                var map = await _dictionaryService.GetBatchAsync(list, preferEn, cancellationToken);
                var body = map.ToDictionary(kv => kv.Key, kv => kv.Value.ToList(), StringComparer.Ordinal);
                return Ok(ApiResponse<Dictionary<string, List<DictionaryItemDto>>>.Ok(body, "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取物料生产日期字典失败");
                return StatusCode(500,
                    ApiResponse<Dictionary<string, List<DictionaryItemDto>>>.Fail("读取字典失败"));
            }
        }

        /// <summary>
        /// 到货通知等「来货方式」「快递方式」字典一次拉取
        /// </summary>
        [HttpGet("logistics-form")]
        public async Task<ActionResult<ApiResponse<Dictionary<string, List<DictionaryItemDto>>>>> GetLogisticsForm(
            CancellationToken cancellationToken)
        {
            var list = new[]
            {
                DictCategories.LogisticsArrivalMethod,
                DictCategories.LogisticsExpressMethod
            };

            try
            {
                var preferEn = PreferEnglish(Request);
                var map = await _dictionaryService.GetBatchAsync(list, preferEn, cancellationToken);
                var body = map.ToDictionary(kv => kv.Key, kv => kv.Value.ToList(), StringComparer.Ordinal);
                return Ok(ApiResponse<Dictionary<string, List<DictionaryItemDto>>>.Ok(body, "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取物流字典失败");
                return StatusCode(500,
                    ApiResponse<Dictionary<string, List<DictionaryItemDto>>>.Fail("读取字典失败"));
            }
        }

        /// <summary>
        /// 按业务类型 + 编码解析字典项（含已禁用），用于历史值在表单中回显
        /// </summary>
        [HttpGet("lookup")]
        public async Task<ActionResult<ApiResponse<DictionaryItemDto?>>> Lookup(
            [FromQuery] string category,
            [FromQuery] string code,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(ApiResponse<DictionaryItemDto?>.Fail("category 与 code 必填"));
            }

            try
            {
                var preferEn = PreferEnglish(Request);
                var row = await _dictionaryService.LookupByCodeAsync(category, code, preferEn, cancellationToken);
                return Ok(ApiResponse<DictionaryItemDto?>.Ok(row, "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "字典 lookup 失败 category={Category} code={Code}", category, code);
                return StatusCode(500, ApiResponse<DictionaryItemDto?>.Fail("读取字典失败"));
            }
        }

        private static bool PreferEnglish(HttpRequest request)
        {
            var langs = request.Headers.AcceptLanguage.ToString();
            return langs.Contains("en", StringComparison.OrdinalIgnoreCase);
        }
    }
}
