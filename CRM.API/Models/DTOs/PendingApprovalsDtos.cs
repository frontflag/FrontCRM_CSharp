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

        /// <summary>最近一次通过/驳回的审批人显示名。</summary>
        public string? Approver { get; set; }

        /// <summary>最近一次通过/驳回的审批时间（UTC）。</summary>
        public DateTime? ApprovedAt { get; set; }

        public short Status { get; set; } // current status
        public DateTime CreatedAt { get; set; }

        /// <summary>当前用户是否可对该条执行通过/驳回（仅有读权限、仅查看本人提交时为 false）。</summary>
        public bool CanDecide { get; set; }

        /// <summary>采购订单类型：1=客单 2=备货 3=样品；非采购单为 null。</summary>
        public short? PurchaseOrderType { get; set; }
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

        /// <summary>提交日期起（含，按日）。</summary>
        public DateTime? SubmittedFrom { get; set; }

        /// <summary>提交日期止（含，按日）。</summary>
        public DateTime? SubmittedTo { get; set; }

        /// <summary>单据编号（模糊）。</summary>
        public string? DocumentCode { get; set; }

        /// <summary>提交人（显示名/用户名模糊）。</summary>
        public string? Submitter { get; set; }

        /// <summary>审批人（显示名/用户名模糊）。</summary>
        public string? Approver { get; set; }

        /// <summary>排序字段：submittedAt / createdAt（提交时间，默认）| approvedAt（审批日期）。</summary>
        public string? SortBy { get; set; }

        /// <summary>是否升序；默认 false（降序，新的在前）。</summary>
        public bool? SortAsc { get; set; }

        /// <summary>排序方向：asc | desc（优先于 <see cref="SortAsc"/>，避免 query 布尔绑定歧义）。</summary>
        public string? SortDir { get; set; }

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

