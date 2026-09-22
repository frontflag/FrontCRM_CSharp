using CRM.Core.Interfaces;
using CRM.Core.Models.Vendor;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    public partial class VendorService
    {
        /// <inheritdoc />
        public async Task<VendorImportPreviewResult> PreviewVendorImportAsync(
            VendorImportPreviewRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new VendorImportPreviewResult();
            if (request.Items == null || request.Items.Count == 0)
                return result;

            var lookup = await LoadImportLookupAsync(cancellationToken);
            foreach (var item in OrderPreviewItems(request.Items))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var name = item.Name?.Trim();
                var credit = string.IsNullOrWhiteSpace(item.CreditCode) ? null : item.CreditCode.Trim();
                var hit = lookup.Match(name, credit);
                if (hit != null)
                {
                    result.SkipCount++;
                    result.SkippedExcelRows.Add(item.ExcelRow);
                    continue;
                }

                result.InsertCount++;
                lookup.Add(name, credit, "", "");
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<VendorImportBatchResult> ImportVendorsBatchAsync(
            VendorImportBatchRequest request,
            string? actingUserId = null,
            CancellationToken cancellationToken = default)
        {
            var result = new VendorImportBatchResult();
            if (request.Items == null || request.Items.Count == 0)
                return result;

            var ordered = OrderImportItems(request.Items);
            var lookup = await LoadImportLookupAsync(cancellationToken);
            var ownerId = ActingUserIdNormalizer.Normalize(actingUserId);
            var purchaserFromUser = await ResolveImportPurchaserNameAsync(ownerId);

            var pending = new List<PreparedVendorImport>();
            var index = 0;
            foreach (var item in ordered)
            {
                cancellationToken.ThrowIfCancellationRequested();
                index++;
                var vendorReq = item.Vendor ?? new CreateVendorRequest();
                var name = (vendorReq.Name ?? vendorReq.OfficialName)?.Trim() ?? "";
                var credit = (vendorReq.CreditCode ?? vendorReq.TaxNumber)?.Trim();
                if (string.IsNullOrEmpty(credit))
                    credit = null;

                var validationError = ValidateImportVendor(vendorReq, name, credit);
                if (validationError != null)
                {
                    AddImportFailure(result, item, index, name, validationError);
                    continue;
                }

                var contactError = TryBuildImportContacts(item.Contacts, out var contacts);
                if (contactError != null)
                {
                    AddImportFailure(result, item, index, name, contactError);
                    continue;
                }

                string? emailSuffix;
                try
                {
                    emailSuffix = await ResolveUniqueCompanyEmailSuffixAsync(vendorReq.CompanyEmailSuffix, null);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    AddImportFailure(result, item, index, name, ToImportError(ex));
                    continue;
                }

                var hit = lookup.Match(name, credit);
                if (hit != null)
                {
                    result.Items.Add(new VendorImportItemResult
                    {
                        Index = index,
                        ExcelRow = item.ExcelRow,
                        VendorName = name,
                        Success = false,
                        Skipped = true,
                        ExistingVendorId = hit.IsPending ? null : hit.Id,
                        ExistingVendorCode = hit.IsPending ? null : hit.Code
                    });
                    result.SkipCount++;
                    continue;
                }

                pending.Add(new PreparedVendorImport
                {
                    Item = item,
                    Index = index,
                    Name = name,
                    CreditCode = credit,
                    Contacts = contacts,
                    EmailSuffix = emailSuffix,
                    Request = vendorReq
                });
                lookup.Add(name, credit, "", "");
            }

            if (pending.Count == 0)
                return result;

            var codes = await _serialNumberService.ReserveNextAsync(ModuleCodes.Vendor, pending.Count, cancellationToken);
            var entities = new List<VendorInfo>(pending.Count);
            for (var i = 0; i < pending.Count; i++)
            {
                var row = pending[i];
                var vendor = BuildImportVendor(row.Request, codes[i], ownerId, purchaserFromUser, row.EmailSuffix);
                foreach (var contact in row.Contacts)
                    contact.VendorId = vendor.Id;
                row.Vendor = vendor;
                entities.Add(vendor);
            }

            List<PreparedVendorImport> saved;
            try
            {
                foreach (var row in pending)
                {
                    await _repository.AddAsync(row.Vendor);
                    foreach (var contact in row.Contacts)
                        await _contactRepository.AddAsync(contact);
                }

                await _unitOfWork.SaveChangesAsync();
                saved = pending;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                _unitOfWork.DiscardPendingChanges();
                foreach (var row in pending)
                    ClearImportGraph(row);
                saved = new List<PreparedVendorImport>();
                foreach (var row in pending)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        await _repository.AddAsync(row.Vendor);
                        foreach (var contact in row.Contacts)
                            await _contactRepository.AddAsync(contact);
                        await _unitOfWork.SaveChangesAsync();
                        saved.Add(row);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception rowEx)
                    {
                        _unitOfWork.DiscardPendingChanges();
                        ClearImportGraph(row);
                        AddImportFailure(result, row.Item, row.Index, row.Name, ToImportError(rowEx));
                    }
                }
            }

            foreach (var row in saved)
                await AddImportSuccessAsync(result, row, actingUserId, cancellationToken);

            return result;
        }

        private async Task<VendorImportLookup> LoadImportLookupAsync(CancellationToken cancellationToken)
        {
            var rows = await _vendorListQuery.GetDuplicateCheckRowsAsync(cancellationToken);
            var lookup = new VendorImportLookup();
            foreach (var row in rows.Where(r => !r.IsDeleted).OrderBy(r => r.CreateTime).ThenBy(r => r.Id))
                lookup.Add(row.OfficialName, row.CreditCode, row.Id, row.Code ?? "");
            return lookup;
        }

        private async Task<string?> ResolveImportPurchaserNameAsync(string? ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                return null;
            var user = await _userService.GetByIdAsync(ownerId);
            if (user == null)
                return null;
            if (!string.IsNullOrWhiteSpace(user.RealName))
                return user.RealName.Trim();
            if (!string.IsNullOrWhiteSpace(user.UserName))
                return user.UserName.Trim();
            return null;
        }

        private static List<VendorImportBatchItem> OrderImportItems(IReadOnlyList<VendorImportBatchItem> items) =>
            items
                .Select((item, idx) => (item, idx))
                .OrderBy(x => x.item.ExcelRow > 0 ? x.item.ExcelRow : int.MaxValue)
                .ThenBy(x => x.idx)
                .Select(x => x.item)
                .ToList();

        private static List<VendorImportPreviewItem> OrderPreviewItems(IReadOnlyList<VendorImportPreviewItem> items) =>
            items
                .Select((item, idx) => (item, idx))
                .OrderBy(x => x.item.ExcelRow > 0 ? x.item.ExcelRow : int.MaxValue)
                .ThenBy(x => x.idx)
                .Select(x => x.item)
                .ToList();

        private static string? ValidateImportVendor(CreateVendorRequest request, string official, string? credit)
        {
            if (string.IsNullOrWhiteSpace(official))
                return "供应商名称不能为空";
            if (official.Length > 64)
                return "供应商名称超过 64 个字符";

            var nick = request.NickName?.Trim();
            if (nick != null && nick.Length > 64)
                return "供应商简称超过 64 个字符";
            if (credit != null && credit.Length > 50)
                return "统一社会信用代码超过 50 个字符";

            var website = request.Website?.Trim();
            if (website != null && website.Length > 300)
                return "官方网址超过 300 个字符";

            var address = request.OfficeAddress?.Trim();
            if (address != null && address.Length > 200)
                return "办公地址超过 200 个字符";

            var industry = request.Industry?.Trim();
            if (industry != null && industry.Length > 50)
                return "行业超过 50 个字符";

            return null;
        }

        private string? TryBuildImportContacts(
            IReadOnlyList<AddVendorContactRequest>? requests,
            out List<VendorContactInfo> contacts)
        {
            contacts = new List<VendorContactInfo>();
            if (requests == null || requests.Count == 0)
                return null;

            var anyMain = requests.Any(c => c != null && c.IsMain);
            var added = 0;
            foreach (var cr in requests)
            {
                if (cr == null)
                    continue;

                string? cName;
                string? eName;
                try
                {
                    (cName, eName) = ContactNameResolver.ResolveForCreate(cr.CName, cr.EName);
                }
                catch (ArgumentException ex)
                {
                    return ex.Message;
                }

                var mobile = cr.Mobile?.Trim();
                var tel = cr.Tel?.Trim();
                if (string.IsNullOrWhiteSpace(cName) && string.IsNullOrWhiteSpace(eName)
                    && string.IsNullOrWhiteSpace(mobile) && string.IsNullOrWhiteSpace(tel))
                    continue;

                if (!anyMain && added == 0)
                    cr.IsMain = true;

                var contact = new VendorContactInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    IsDeleted = false,
                    CName = cName,
                    EName = eName,
                    Gender = NormalizeContactGender(cr.Gender),
                    Title = string.IsNullOrWhiteSpace(cr.Title) ? null : cr.Title.Trim(),
                    Department = string.IsNullOrWhiteSpace(cr.Department) ? null : cr.Department.Trim(),
                    Mobile = string.IsNullOrWhiteSpace(mobile) ? null : mobile,
                    Tel = string.IsNullOrWhiteSpace(tel) ? null : tel,
                    Email = string.IsNullOrWhiteSpace(cr.Email) ? null : cr.Email.Trim(),
                    IsMain = cr.IsMain,
                    Remark = string.IsNullOrWhiteSpace(cr.Remark) ? null : cr.Remark.Trim(),
                    CreateTime = DateTime.UtcNow
                };

                var lengthError = ValidateImportContact(contact);
                if (lengthError != null)
                    return lengthError;

                contacts.Add(contact);
                added++;
            }

            return null;
        }

        private static string? ValidateImportContact(VendorContactInfo contact)
        {
            if (contact.CName != null && contact.CName.Length > 50)
                return "联系人姓名超过 50 个字符";
            if (contact.EName != null && contact.EName.Length > 100)
                return "联系人英文名超过 100 个字符";
            if (contact.Mobile != null && contact.Mobile.Length > 20)
                return "联系人手机超过 20 个字符";
            if (contact.Tel != null && contact.Tel.Length > 30)
                return "联系人固定电话超过 30 个字符";
            if (contact.Department != null && contact.Department.Length > 50)
                return "联系人部门超过 50 个字符";
            if (contact.Title != null && contact.Title.Length > 50)
                return "联系人职位超过 50 个字符";
            if (contact.Email != null && contact.Email.Length > 100)
                return "联系人邮箱超过 100 个字符";
            return null;
        }

        private static VendorInfo BuildImportVendor(
            CreateVendorRequest request,
            string code,
            string? ownerId,
            string? purchaserFromUser,
            string? emailSuffix)
        {
            var official = (request.Name ?? request.OfficialName)?.Trim();
            var tax = (request.CreditCode ?? request.TaxNumber)?.Trim();
            var currency = request.TradeCurrency ?? request.Currency;
            var purchaser = string.IsNullOrWhiteSpace(request.PurchaserName)
                ? purchaserFromUser
                : request.PurchaserName.Trim();

            var entity = new VendorInfo
            {
                Id = Guid.NewGuid().ToString(),
                IsDeleted = false,
                Code = code.Trim(),
                OfficialName = string.IsNullOrEmpty(official) ? null : official,
                EnglishOfficialName = string.IsNullOrWhiteSpace(request.EnglishOfficialName)
                    ? null
                    : request.EnglishOfficialName.Trim(),
                NickName = string.IsNullOrWhiteSpace(request.NickName) ? null : request.NickName.Trim(),
                Industry = string.IsNullOrWhiteSpace(request.Industry) ? null : request.Industry.Trim(),
                Level = VendorLevelCodes.NormalizeOrDefault(request.Level),
                Credit = request.Credit,
                Status = request.Status ?? 1,
                OfficeAddress = string.IsNullOrWhiteSpace(request.OfficeAddress) ? null : request.OfficeAddress.Trim(),
                Website = string.IsNullOrWhiteSpace(request.Website) ? null : request.Website.Trim(),
                PurchaserName = string.IsNullOrWhiteSpace(purchaser) ? null : purchaser,
                TradeCurrency = currency,
                PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? null : request.PaymentMethod.Trim(),
                Payment = request.PaymentDays,
                CreditCode = string.IsNullOrWhiteSpace(tax) ? null : tax,
                DUNS = string.IsNullOrWhiteSpace(request.Duns) ? null : request.Duns.Trim(),
                CompanyEmailSuffix = emailSuffix,
                CompanyInfo = string.IsNullOrWhiteSpace(request.CompanyInfo) ? null : request.CompanyInfo.Trim(),
                Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim(),
                CreateTime = DateTime.UtcNow,
                CreateByUserId = ownerId
            };

            if (!string.IsNullOrWhiteSpace(ownerId))
                entity.PurchaseUserId = ownerId;

            return entity;
        }

        private async Task AddImportSuccessAsync(
            VendorImportBatchResult result,
            PreparedVendorImport row,
            string? actingUserId,
            CancellationToken cancellationToken)
        {
            result.Items.Add(new VendorImportItemResult
            {
                Index = row.Index,
                ExcelRow = row.Item.ExcelRow,
                VendorName = row.Name,
                Success = true,
                VendorCode = row.Vendor.Code,
                VendorId = row.Vendor.Id
            });
            result.SuccessCount++;
            foreach (var contact in row.Contacts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await LogVendorContactAddedAsync(contact, actingUserId);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // 供应商与联系人已写入，变更日志失败不把本行记为导入失败。
                }
            }
        }

        private static void AddImportFailure(
            VendorImportBatchResult result,
            VendorImportBatchItem item,
            int index,
            string name,
            string error)
        {
            result.Items.Add(new VendorImportItemResult
            {
                Index = index,
                ExcelRow = item.ExcelRow,
                VendorName = name,
                Success = false,
                Error = error
            });
            result.FailCount++;
        }

        private static void ClearImportGraph(PreparedVendorImport row)
        {
            row.Vendor.Contacts?.Clear();
            foreach (var contact in row.Contacts)
                contact.Vendor = null;
        }

        private static string ToImportError(Exception ex)
        {
            var msg = ex.GetBaseException().Message?.Trim();
            if (string.IsNullOrEmpty(msg))
                msg = "导入失败";
            return msg.Length > 200 ? msg[..200] : msg;
        }

        private sealed class PreparedVendorImport
        {
            public VendorImportBatchItem Item { get; init; } = null!;
            public int Index { get; init; }
            public string Name { get; init; } = "";
            public string? CreditCode { get; init; }
            public CreateVendorRequest Request { get; init; } = null!;
            public List<VendorContactInfo> Contacts { get; init; } = new();
            public string? EmailSuffix { get; init; }
            public VendorInfo Vendor { get; set; } = null!;
        }
    }
}
