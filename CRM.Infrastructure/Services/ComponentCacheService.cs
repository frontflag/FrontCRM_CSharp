using System.Text.Json;
using CRM.Core.Interfaces;
using CRM.Core.Models.Component;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Services
{
    /// <summary>
    /// 物料缓存服务
    /// 缓存策略：
    ///   - 数据库中存在且 FetchedAt 在24小时内 → 直接返回缓存，并增加查询计数
    ///   - 数据库中不存在，或 FetchedAt 超过24小时 → 调用外部 API，写入/更新数据库
    /// </summary>
    public class ComponentCacheService : IComponentCacheService
    {
        private const int CacheHours = 24;

        private readonly ApplicationDbContext _db;
        private readonly IComponentDataService _dataService;
        private readonly ILogger<ComponentCacheService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public ComponentCacheService(
            ApplicationDbContext db,
            IComponentDataService dataService,
            ILogger<ComponentCacheService> logger)
        {
            _db = db;
            _dataService = dataService;
            _logger = logger;
        }

        /// <summary>
        /// 查询物料信息（带缓存）
        /// </summary>
        public async Task<ComponentDetailDto?> GetByMpnAsync(string mpn)
        {
            if (string.IsNullOrWhiteSpace(mpn))
                return null;

            var normalizedMpn = mpn.Trim().ToUpperInvariant();
            var cacheExpiry = DateTime.UtcNow.AddHours(-CacheHours);

            // 1. 查数据库缓存
            var cached = await _db.ComponentCaches
                .FirstOrDefaultAsync(c => c.Mpn == normalizedMpn);

            if (cached != null && cached.FetchedAt >= cacheExpiry)
            {
                // 缓存有效，直接返回
                _logger.LogInformation("[ComponentCache] HIT - MPN={Mpn}, FetchedAt={FetchedAt}, QueryCount={Count}",
                    normalizedMpn, cached.FetchedAt, cached.QueryCount);

                // 更新查询计数（fire-and-forget，不影响响应速度）
                cached.QueryCount++;
                await _db.SaveChangesAsync();

                return DeserializeCache(cached, isFromCache: true);
            }

            // 2. 缓存不存在或已过期，调用外部数据服务
            _logger.LogInformation("[ComponentCache] MISS - MPN={Mpn}, calling {Source}",
                normalizedMpn, _dataService.SourceName);

            ComponentDetailDto? freshData;
            try
            {
                freshData = await _dataService.FetchByMpnAsync(normalizedMpn);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ComponentCache] External API failed for MPN={Mpn}", normalizedMpn);

                // 如果有过期缓存，降级返回旧数据
                if (cached != null)
                {
                    _logger.LogWarning("[ComponentCache] Returning stale cache for MPN={Mpn}", normalizedMpn);
                    return DeserializeCache(cached, isFromCache: true);
                }
                return null;
            }

            if (freshData == null)
            {
                _logger.LogWarning("[ComponentCache] No data returned for MPN={Mpn}", normalizedMpn);
                return null;
            }

            // 3. 写入/更新数据库缓存
            await UpsertCacheAsync(normalizedMpn, freshData, cached);

            freshData.IsFromCache = false;
            return freshData;
        }

        /// <summary>
        /// 强制刷新缓存（忽略24小时限制）
        /// </summary>
        public async Task<ComponentDetailDto?> RefreshByMpnAsync(string mpn)
        {
            if (string.IsNullOrWhiteSpace(mpn))
                return null;

            var normalizedMpn = mpn.Trim().ToUpperInvariant();

            _logger.LogInformation("[ComponentCache] FORCE REFRESH - MPN={Mpn}", normalizedMpn);

            ComponentDetailDto? freshData;
            try
            {
                freshData = await _dataService.FetchByMpnAsync(normalizedMpn);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ComponentCache] Force refresh failed for MPN={Mpn}", normalizedMpn);
                return null;
            }

            if (freshData == null) return null;

            var existing = await _db.ComponentCaches
                .FirstOrDefaultAsync(c => c.Mpn == normalizedMpn);

            await UpsertCacheAsync(normalizedMpn, freshData, existing);

            freshData.IsFromCache = false;
            return freshData;
        }

        /// <summary>
        /// 获取缓存元信息
        /// </summary>
        public async Task<ComponentCacheMetaDto?> GetCacheMetaAsync(string mpn)
        {
            var normalizedMpn = mpn.Trim().ToUpperInvariant();
            var cached = await _db.ComponentCaches
                .FirstOrDefaultAsync(c => c.Mpn == normalizedMpn);

            if (cached == null) return null;

            var expiryTime = cached.FetchedAt.AddHours(CacheHours);
            var hoursLeft = (expiryTime - DateTime.UtcNow).TotalHours;

            return new ComponentCacheMetaDto
            {
                Mpn = cached.Mpn,
                FetchedAt = cached.FetchedAt,
                DataSource = cached.DataSource,
                QueryCount = cached.QueryCount,
                IsExpired = hoursLeft <= 0,
                HoursUntilExpiry = Math.Max(0, hoursLeft)
            };
        }

        // ─────────────────────────────────────────────────────────
        // 私有辅助方法
        // ─────────────────────────────────────────────────────────

        private async Task UpsertCacheAsync(string mpn, ComponentDetailDto data, ComponentCache? existing)
        {
            var now = DateTime.UtcNow;

            if (existing == null)
            {
                // 新增
                var newCache = new ComponentCache
                {
                    Mpn = mpn,
                    ManufacturerName = data.ManufacturerName,
                    ShortDescription = data.ShortDescription,
                    Description = data.Description,
                    LifecycleStatus = data.LifecycleStatus,
                    PackageType = data.PackageType,
                    IsRoHSCompliant = data.IsRoHSCompliant,
                    SpecsJson = JsonSerializer.Serialize(data.Specs, _jsonOptions),
                    SellersJson = JsonSerializer.Serialize(data.Sellers, _jsonOptions),
                    AlternativesJson = JsonSerializer.Serialize(data.Alternatives, _jsonOptions),
                    ApplicationsJson = JsonSerializer.Serialize(data.Applications, _jsonOptions),
                    PriceTrendJson = JsonSerializer.Serialize(data.PriceTrend, _jsonOptions),
                    NewsJson = JsonSerializer.Serialize(data.News, _jsonOptions),
                    DataSource = data.DataSource,
                    FetchedAt = now,
                    CreateTime = now,
                    QueryCount = 1
                };
                _db.ComponentCaches.Add(newCache);
            }
            else
            {
                // 更新
                existing.ManufacturerName = data.ManufacturerName;
                existing.ShortDescription = data.ShortDescription;
                existing.Description = data.Description;
                existing.LifecycleStatus = data.LifecycleStatus;
                existing.PackageType = data.PackageType;
                existing.IsRoHSCompliant = data.IsRoHSCompliant;
                existing.SpecsJson = JsonSerializer.Serialize(data.Specs, _jsonOptions);
                existing.SellersJson = JsonSerializer.Serialize(data.Sellers, _jsonOptions);
                existing.AlternativesJson = JsonSerializer.Serialize(data.Alternatives, _jsonOptions);
                existing.ApplicationsJson = JsonSerializer.Serialize(data.Applications, _jsonOptions);
                existing.PriceTrendJson = JsonSerializer.Serialize(data.PriceTrend, _jsonOptions);
                existing.NewsJson = JsonSerializer.Serialize(data.News, _jsonOptions);
                existing.DataSource = data.DataSource;
                existing.FetchedAt = now;
                existing.UpdateTime = now;
                existing.QueryCount++;
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("[ComponentCache] Saved to DB - MPN={Mpn}, Source={Source}", mpn, data.DataSource);
        }

        private static ComponentDetailDto DeserializeCache(ComponentCache cache, bool isFromCache)
        {
            return new ComponentDetailDto
            {
                Mpn = cache.Mpn,
                ManufacturerName = cache.ManufacturerName,
                ShortDescription = cache.ShortDescription,
                Description = cache.Description,
                LifecycleStatus = cache.LifecycleStatus,
                PackageType = cache.PackageType,
                IsRoHSCompliant = cache.IsRoHSCompliant,
                DataSource = cache.DataSource,
                FetchedAt = cache.FetchedAt,
                IsFromCache = isFromCache,
                Specs = DeserializeJson<List<ComponentSpec>>(cache.SpecsJson) ?? new(),
                Sellers = DeserializeJson<List<DistributorPricing>>(cache.SellersJson) ?? new(),
                Alternatives = DeserializeJson<List<AlternativeComponent>>(cache.AlternativesJson) ?? new(),
                Applications = DeserializeJson<List<ApplicationScenario>>(cache.ApplicationsJson) ?? new(),
                PriceTrend = DeserializeJson<List<PriceTrendPoint>>(cache.PriceTrendJson) ?? new(),
                News = DeserializeJson<List<ComponentNews>>(cache.NewsJson) ?? new()
            };
        }

        private static T? DeserializeJson<T>(string? json) where T : class
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch
            {
                return null;
            }
        }
    }
}
