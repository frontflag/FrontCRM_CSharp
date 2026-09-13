using CRM.Core.Models.RFQ;
using CRM.Core.Models.Sales;

namespace CRM.Core.Utilities;

public static class WorkCalendarDotRules
{
    public static bool CountsAsRfqAssignedDot(short status) =>
        status is
            (short)RfqMainStatus.Assigned or
            (short)RfqMainStatus.Quoting or
            (short)RfqMainStatus.Quoted or
            (short)RfqMainStatus.PriceSelected or
            (short)RfqMainStatus.ConvertedToOrder or
            (short)RfqMainStatus.Closed;

    public static bool CountsAsSalesOrderApprovedDot(SellOrderMainStatus status) =>
        status is
            SellOrderMainStatus.Approved or
            SellOrderMainStatus.InProgress or
            SellOrderMainStatus.Completed;

    public static bool CountsAsSalesOrderApprovedDot(short status) =>
        CountsAsSalesOrderApprovedDot((SellOrderMainStatus)status);
}
