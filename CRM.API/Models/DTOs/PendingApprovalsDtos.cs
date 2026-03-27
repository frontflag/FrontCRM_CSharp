namespace CRM.API.Models.DTOs
{
    public class PendingApprovalItemDto
    {
        public string BizType { get; set; } = string.Empty; // e.g. VENDOR / SALES_ORDER
        public string BizTypeName { get; set; } = string.Empty; // Chinese name

        public string BusinessId { get; set; } = string.Empty; // entity primary id
        public string DocumentCode { get; set; } = string.Empty; // order/receipt/vendor code, etc.

        public string? Title { get; set; } // display title (customer/vendor/order etc)
        public string? CounterpartyName { get; set; } // customer/vendor for financial/order modules

        public decimal? Amount { get; set; } // optional amount for table display
        public short? Currency { get; set; } // optional currency id

        public string? Submitter { get; set; } // submitter user id/name

        public short Status { get; set; } // current status
        public DateTime CreatedAt { get; set; }
    }

    public class PendingApprovalsPageDto
    {
        public List<PendingApprovalItemDto> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class PendingApprovalsQueryRequest
    {
        // VENDOR / SALES_ORDER / PURCHASE_ORDER / CUSTOMER / FINANCE_RECEIPT / FINANCE_PAYMENT
        public string? BizType { get; set; }

        // pending | approved | rejected
        public string? State { get; set; } = "pending";

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class DecidePendingApprovalRequest
    {
        public string BizType { get; set; } = string.Empty;
        public string BusinessId { get; set; } = string.Empty;

        // approve | reject
        public string Decision { get; set; } = string.Empty;

        public string? Remark { get; set; }
    }
}

