using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.System;

namespace CRM.Core.Services
{
    public class DisplayTimeZoneService : IDisplayTimeZoneService
    {
        private readonly IRepository<SysParam> _paramRepository;

        public DisplayTimeZoneService(IRepository<SysParam> paramRepository)
        {
            _paramRepository = paramRepository;
        }

        public async Task<string> GetDisplayTimeZoneIdAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rows = await _paramRepository.FindAsync(p =>
                p.ParamCode == SysParamCodes.DisplayTimeZoneId && p.Status == 1);
            var row = rows.FirstOrDefault();
            var v = row?.ValueString?.Trim();
            return string.IsNullOrEmpty(v) ? SysParamCodes.DefaultDisplayTimeZoneId : v;
        }
    }
}
