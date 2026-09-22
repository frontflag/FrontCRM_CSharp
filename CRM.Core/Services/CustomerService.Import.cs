using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    public partial class CustomerService
    {
        /// <inheritdoc />
        public async Task<CustomerImportPreviewResult> PreviewCustomerImportAsync(
            CustomerImportPreviewRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new CustomerImportPreviewResult();
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
        public async Task<CustomerImportBatchResult> ImportCustomersBatchAsync(
            CustomerImportBatchRequest request,
            string? actingUserId = null,
            CancellationToken cancellationToken = default)
        {
            var result = new CustomerImportBatchResult();
            if (request.Items == null || request.Items.Count == 0)
                return result;

            var ordered = OrderImportItems(request.Items);
            var lookup = await LoadImportLookupAsync(cancellationToken);
            var normalizedActor = ActingUserIdNormalizer.Normalize(actingUserId);

            var pending = new List<PreparedCustomerImport>();
            var index = 0;
            foreach (var item in ordered)
            {
                cancellationToken.ThrowIfCancellationRequested();
                index++;
                var customerReq = item.Customer ?? new CreateCustomerRequest();
                var name = (customerReq.OfficialName ?? customerReq.CustomerName)?.Trim() ?? "";
                var credit = customerReq.CreditCode?.Trim();
                if (string.IsNullOrEmpty(credit))
                    credit = null;

                var validationError = ValidateImportCustomer(customerReq, name, credit);
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
                    emailSuffix = await ResolveUniqueCompanyEmailSuffixAsync(customerReq.CompanyEmailSuffix, null);
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
                    result.Items.Add(new CustomerImportItemResult
                    {
                        Index = index,
                        ExcelRow = item.ExcelRow,
                        CustomerName = name,
                        Success = false,
                        Skipped = true,
                        ExistingCustomerId = hit.IsPending ? null : hit.Id,
                        ExistingCustomerCode = hit.IsPending ? null : hit.Code
                    });
                    result.SkipCount++;
                    continue;
                }

                var salesUserId = string.IsNullOrWhiteSpace(customerReq.SalesUserId)
                    ? normalizedActor
                    : customerReq.SalesUserId.Trim();

                pending.Add(new PreparedCustomerImport
                {
                    Item = item,
                    Index = index,
                    Name = name,
                    CreditCode = credit,
                    Contacts = contacts,
                    EmailSuffix = emailSuffix,
                    SalesUserId = salesUserId,
                    Request = customerReq
                });
                lookup.Add(name, credit, "", "");
            }

            if (pending.Count == 0)
                return result;

            var codes = await _serialNumberService.ReserveNextAsync(ModuleCodes.Customer, pending.Count, cancellationToken);
            for (var i = 0; i < pending.Count; i++)
            {
                var row = pending[i];
                var customer = BuildImportCustomer(row.Request, codes[i], normalizedActor, row.SalesUserId, row.EmailSuffix);
                foreach (var contact in row.Contacts)
                    contact.CustomerId = customer.Id;
                row.Customer = customer;
            }

            List<PreparedCustomerImport> saved;
            try
            {
                foreach (var row in pending)
                {
                    await _customerRepository.AddAsync(row.Customer);
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
                saved = new List<PreparedCustomerImport>();
                foreach (var row in pending)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        await _customerRepository.AddAsync(row.Customer);
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
            var rows = await _customerListQuery.GetImportMatchRowsAsync(cancellationToken);
            var lookup = new VendorImportLookup();
            foreach (var row in rows.Where(r => !r.IsDeleted).OrderBy(r => r.CreateTime).ThenBy(r => r.Id))
                lookup.Add(row.OfficialName, row.CreditCode, row.Id, row.CustomerCode ?? "");
            return lookup;
        }

        private static List<CustomerImportBatchItem> OrderImportItems(IReadOnlyList<CustomerImportBatchItem> items) =>
            items
                .Select((item, idx) => (item, idx))
                .OrderBy(x => x.item.ExcelRow > 0 ? x.item.ExcelRow : int.MaxValue)
                .ThenBy(x => x.idx)
                .Select(x => x.item)
                .ToList();

        private static List<CustomerImportPreviewItem> OrderPreviewItems(IReadOnlyList<CustomerImportPreviewItem> items) =>
            items
                .Select((item, idx) => (item, idx))
                .OrderBy(x => x.item.ExcelRow > 0 ? x.item.ExcelRow : int.MaxValue)
                .ThenBy(x => x.idx)
                .Select(x => x.item)
                .ToList();

        private static string? ValidateImportCustomer(CreateCustomerRequest request, string official, string? credit)
        {
            if (string.IsNullOrWhiteSpace(official))
                return "客户名称不能为空";
            if (official.Length > 128)
                return "客户名称超过 128 个字符";

            var nick = request.NickName?.Trim();
            if (nick != null && nick.Length > 64)
                return "客户简称超过 64 个字符";
            if (credit != null && credit.Length > 50)
                return "统一社会信用代码超过 50 个字符";

            var english = request.EnglishOfficialName?.Trim();
            if (english != null && english.Length > 128)
                return "客户英文名称超过 128 个字符";

            var province = request.Province?.Trim();
            if (province != null && province.Length > 50)
                return "省超过 50 个字符";
            var city = request.City?.Trim();
            if (city != null && city.Length > 50)
                return "市超过 50 个字符";
            var district = request.District?.Trim();
            if (district != null && district.Length > 50)
                return "区超过 50 个字符";

            var industry = request.Industry?.Trim();
            if (industry != null && industry.Length > 50)
                return "行业超过 50 个字符";

            var remark = request.Remark?.Trim();
            if (remark != null && remark.Length > 500)
                return "备注超过 500 个字符";

            return null;
        }

        private static string? TryBuildImportContacts(
            IReadOnlyList<AddContactRequest>? requests,
            out List<CustomerContactInfo> contacts)
        {
            contacts = new List<CustomerContactInfo>();
            if (requests == null || requests.Count == 0)
                return null;

            var anyMarkedDefault = requests.Any(c => c != null && c.IsDefault == true);
            var added = 0;
            foreach (var cr in requests)
            {
                if (cr == null)
                    continue;

                string? cName;
                string? eName;
                try
                {
                    (cName, eName) = ContactNameResolver.ResolveForCreate(cr.CName, cr.EName, cr.Name, cr.ContactName);
                }
                catch (ArgumentException ex)
                {
                    return ex.Message;
                }

                var mobile = cr.Mobile?.Trim();
                var tel = cr.Phone?.Trim();
                if (string.IsNullOrWhiteSpace(cName) && string.IsNullOrWhiteSpace(eName)
                    && string.IsNullOrWhiteSpace(mobile) && string.IsNullOrWhiteSpace(tel))
                    continue;

                if (!anyMarkedDefault && added == 0)
                    cr.IsDefault = true;

                var contact = new CustomerContactInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    IsDeleted = false,
                    CName = cName,
                    EName = eName,
                    Gender = cr.Gender,
                    Position = string.IsNullOrWhiteSpace(cr.Position) ? null : cr.Position.Trim(),
                    Department = string.IsNullOrWhiteSpace(cr.Department) ? null : cr.Department.Trim(),
                    Mobile = string.IsNullOrWhiteSpace(mobile) ? null : mobile,
                    Phone = string.IsNullOrWhiteSpace(tel) ? null : tel,
                    Email = string.IsNullOrWhiteSpace(cr.Email) ? null : cr.Email.Trim(),
                    IsDefault = cr.IsDefault ?? false,
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

        private static string? ValidateImportContact(CustomerContactInfo contact)
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

        private static CustomerInfo BuildImportCustomer(
            CreateCustomerRequest request,
            string code,
            string? normalizedActor,
            string? salesUserId,
            string? emailSuffix)
        {
            return new CustomerInfo
            {
                Id = Guid.NewGuid().ToString(),
                IsDeleted = false,
                CustomerCode = code.Trim(),
                OfficialName = request.OfficialName?.Trim(),
                EnglishOfficialName = string.IsNullOrWhiteSpace(request.EnglishOfficialName)
                    ? null
                    : request.EnglishOfficialName.Trim(),
                NickName = string.IsNullOrWhiteSpace(request.NickName) ? null : request.NickName.Trim(),
                Level = request.Level,
                Type = request.Type,
                Industry = string.IsNullOrWhiteSpace(request.Industry) ? null : request.Industry.Trim(),
                Product = string.IsNullOrWhiteSpace(request.Product) ? null : request.Product.Trim(),
                SalesUserId = salesUserId,
                CompanyInfo = string.IsNullOrWhiteSpace(request.CompanyInfo) ? null : request.CompanyInfo.Trim(),
                Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim(),
                CreditLine = request.CreditLine,
                Payment = request.Payment,
                TradeCurrency = request.TradeCurrency,
                CreditCode = string.IsNullOrWhiteSpace(request.CreditCode) ? null : request.CreditCode.Trim(),
                DUNS = string.IsNullOrWhiteSpace(request.Duns) ? null : request.Duns.Trim(),
                CompanyEmailSuffix = emailSuffix,
                Province = string.IsNullOrWhiteSpace(request.Province) ? null : request.Province.Trim(),
                City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
                District = string.IsNullOrWhiteSpace(request.District) ? null : request.District.Trim(),
                Status = 1,
                CreateTime = DateTime.UtcNow,
                CreateByUserId = normalizedActor
            };
        }

        private async Task AddImportSuccessAsync(
            CustomerImportBatchResult result,
            PreparedCustomerImport row,
            string? actingUserId,
            CancellationToken cancellationToken)
        {
            result.Items.Add(new CustomerImportItemResult
            {
                Index = row.Index,
                ExcelRow = row.Item.ExcelRow,
                CustomerName = row.Name,
                Success = true,
                CustomerCode = row.Customer.CustomerCode,
                CustomerId = row.Customer.Id
            });
            result.SuccessCount++;
            foreach (var contact in row.Contacts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await LogCustomerContactAddedAsync(contact, actingUserId);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // 客户与联系人已写入，变更日志失败不把本行记为导入失败。
                }
            }
        }

        private static void AddImportFailure(
            CustomerImportBatchResult result,
            CustomerImportBatchItem item,
            int index,
            string name,
            string error)
        {
            result.Items.Add(new CustomerImportItemResult
            {
                Index = index,
                ExcelRow = item.ExcelRow,
                CustomerName = name,
                Success = false,
                Error = error
            });
            result.FailCount++;
        }

        private static void ClearImportGraph(PreparedCustomerImport row)
        {
            row.Customer.Contacts?.Clear();
            foreach (var contact in row.Contacts)
                contact.Customer = null;
        }

        private static string ToImportError(Exception ex)
        {
            var msg = ex.GetBaseException().Message?.Trim();
            if (string.IsNullOrEmpty(msg))
                msg = "导入失败";
            return msg.Length > 200 ? msg[..200] : msg;
        }

        private sealed class PreparedCustomerImport
        {
            public CustomerImportBatchItem Item { get; init; } = null!;
            public int Index { get; init; }
            public string Name { get; init; } = "";
            public string? CreditCode { get; init; }
            public CreateCustomerRequest Request { get; init; } = null!;
            public List<CustomerContactInfo> Contacts { get; init; } = new();
            public string? EmailSuffix { get; init; }
            public string? SalesUserId { get; init; }
            public CustomerInfo Customer { get; set; } = null!;
        }
    }
}
