using CRM.Core.Interfaces;
using CRM.Core.Models.Vendor;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    public partial class VendorService
    {
        /// <inheritdoc />
        public async Task<VendorDuplicateCheckResult> CheckDuplicatesAsync(
            VendorDuplicateCheckRequest request,
            string? currentUserId = null,
            CancellationToken cancellationToken = default)
        {
            request ??= new VendorDuplicateCheckRequest();
            var official = request.OfficialName ?? request.Name;
            var credit = request.CreditCode ?? request.TaxNumber;
            if (!VendorDuplicateKeys.HasAnyKey(official, request.EnglishOfficialName, credit, request.Duns))
            {
                return new VendorDuplicateCheckResult
                {
                    Items = Array.Empty<VendorDuplicateMatchDto>(),
                    Truncated = false
                };
            }

            var rows = await _vendorListQuery.GetDuplicateCheckRowsAsync(cancellationToken);
            var exclude = string.IsNullOrWhiteSpace(request.ExcludeVendorId)
                ? null
                : request.ExcludeVendorId.Trim();

            var hits = new List<VendorDuplicateCheckRow>();
            foreach (var row in rows)
            {
                if (exclude != null && string.Equals(row.Id, exclude, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!VendorDuplicateKeys.IsMatch(
                        official,
                        request.EnglishOfficialName,
                        credit,
                        request.Duns,
                        row.OfficialName,
                        row.EnglishOfficialName,
                        row.CreditCode,
                        row.DUNS))
                    continue;
                hits.Add(row);
            }

            var truncated = hits.Count > VendorDuplicateKeys.MaxMatches;
            var page = hits
                .OrderByDescending(r => r.CreateTime)
                .Take(VendorDuplicateKeys.MaxMatches)
                .ToList();

            var items = new List<VendorDuplicateMatchDto>(page.Count);
            foreach (var row in page)
            {
                var purchaser = await ResolveDuplicatePurchaserNameAsync(row);
                var canView = false;
                if (!row.IsDeleted && !string.IsNullOrWhiteSpace(currentUserId))
                {
                    canView = await _dataPermissionService.CanAccessVendorAsync(
                        currentUserId,
                        new VendorInfo
                        {
                            Id = row.Id,
                            PurchaseUserId = row.PurchaseUserId,
                            AscriptionType = row.AscriptionType
                        });
                }

                items.Add(new VendorDuplicateMatchDto
                {
                    Id = row.Id,
                    Code = row.Code,
                    OfficialName = row.OfficialName,
                    EnglishOfficialName = row.EnglishOfficialName,
                    CreditCode = row.CreditCode,
                    Duns = row.DUNS,
                    PurchaserName = purchaser,
                    CreateTime = row.CreateTime,
                    IsDeleted = row.IsDeleted,
                    BlackList = row.BlackList,
                    CanViewDetail = canView
                });
            }

            return new VendorDuplicateCheckResult
            {
                Items = items,
                Truncated = truncated
            };
        }

        private async Task<string?> ResolveDuplicatePurchaserNameAsync(VendorDuplicateCheckRow row)
        {
            if (!string.IsNullOrWhiteSpace(row.PurchaserName))
                return row.PurchaserName.Trim();
            if (string.IsNullOrWhiteSpace(row.PurchaseUserId))
                return null;
            var u = await _userService.GetByIdAsync(row.PurchaseUserId.Trim());
            if (u == null)
                return null;
            if (!string.IsNullOrWhiteSpace(u.RealName))
                return u.RealName.Trim();
            if (!string.IsNullOrWhiteSpace(u.UserName))
                return u.UserName.Trim();
            return null;
        }
    }
}
