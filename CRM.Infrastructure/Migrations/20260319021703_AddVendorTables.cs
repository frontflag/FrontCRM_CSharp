using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "document_daily_sequence",
                columns: table => new
                {
                    TheDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentSequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_daily_sequence", x => x.TheDate);
                });

            migrationBuilder.CreateTable(
                name: "financepayment",
                columns: table => new
                {
                    FinancePaymentId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    FinancePaymentCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    VendorId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    VendorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    PaymentAmountToBe = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentTotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentCurrency = table.Column<byte>(type: "smallint", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    PaymentMode = table.Column<short>(type: "smallint", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financepayment", x => x.FinancePaymentId);
                });

            migrationBuilder.CreateTable(
                name: "financepurchaseinvoice",
                columns: table => new
                {
                    FinancePurchaseInvoiceId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    VendorId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    VendorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InvoiceNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    InvoiceAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BillAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ExcludTaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmStatus = table.Column<byte>(type: "smallint", nullable: false),
                    RedInvoiceStatus = table.Column<short>(type: "smallint", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financepurchaseinvoice", x => x.FinancePurchaseInvoiceId);
                });

            migrationBuilder.CreateTable(
                name: "financereceipt",
                columns: table => new
                {
                    FinanceReceiptId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    FinanceReceiptCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SalesUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    PurchaseGroupId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    ReceiptAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReceiptCurrency = table.Column<byte>(type: "smallint", nullable: false),
                    ReceiptDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceiptUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ReceiptMode = table.Column<short>(type: "smallint", nullable: false),
                    ReceiptBankId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financereceipt", x => x.FinanceReceiptId);
                });

            migrationBuilder.CreateTable(
                name: "financesellinvoice",
                columns: table => new
                {
                    FinanceSellInvoiceId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InvoiceCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    InvoiceNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    InvoiceTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MakeInvoiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceiveStatus = table.Column<byte>(type: "smallint", nullable: false),
                    ReceiveDone = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReceiveToBe = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<byte>(type: "smallint", nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    InvoiceStatus = table.Column<short>(type: "smallint", nullable: false),
                    SellInvoiceType = table.Column<short>(type: "smallint", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financesellinvoice", x => x.FinanceSellInvoiceId);
                });

            migrationBuilder.CreateTable(
                name: "stock",
                columns: table => new
                {
                    StockId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    MaterialId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    WarehouseId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    LocationId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BatchNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProductionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AvailableQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    LockedQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Qty = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    QtyStockOut = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    QtyOccupy = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    QtySales = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    QtyRepertory = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    QtyRepertoryAvailable = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock", x => x.StockId);
                });

            migrationBuilder.CreateTable(
                name: "stockin",
                columns: table => new
                {
                    StockInId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    StockInCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StockInType = table.Column<short>(type: "smallint", nullable: false),
                    SourceCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    WarehouseId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    VendorId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    StockInDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    TotalQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    InspectStatus = table.Column<short>(type: "smallint", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ApprovedBy = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ApprovedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stockin", x => x.StockInId);
                });

            migrationBuilder.CreateTable(
                name: "stockout",
                columns: table => new
                {
                    StockOutId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    StockOutCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StockOutType = table.Column<short>(type: "smallint", nullable: false),
                    SourceCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    SourceId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    WarehouseId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    StockOutDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    PickerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    PickedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmedBy = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ConfirmedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stockout", x => x.StockOutId);
                });

            migrationBuilder.CreateTable(
                name: "stockoutrequest",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    RequestCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SalesOrderId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    RequestUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    RequestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stockoutrequest", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "upload_document",
                columns: table => new
                {
                    DocumentId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    BizType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BizId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RelativePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FileExtension = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ThumbnailRelativePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Remark = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeleteUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    UploadUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_upload_document", x => x.DocumentId);
                });

            migrationBuilder.CreateTable(
                name: "vendorcontacthistory",
                columns: table => new
                {
                    HistoryId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    VendorId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Content = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ContactPerson = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextFollowUpTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Result = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OperatorId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendorcontacthistory", x => x.HistoryId);
                });

            migrationBuilder.CreateTable(
                name: "vendorinfo",
                columns: table => new
                {
                    VendorId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    OfficialName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    NickName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    VendorIdCrm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Level = table.Column<short>(type: "smallint", nullable: true),
                    Scale = table.Column<short>(type: "smallint", nullable: true),
                    Background = table.Column<short>(type: "smallint", nullable: true),
                    CompanyClass = table.Column<short>(type: "smallint", nullable: true),
                    Country = table.Column<short>(type: "smallint", nullable: true),
                    LocationType = table.Column<short>(type: "smallint", nullable: true),
                    Industry = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Product = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OfficeAddress = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TradeCurrency = table.Column<short>(type: "smallint", nullable: true),
                    TradeType = table.Column<short>(type: "smallint", nullable: true),
                    Payment = table.Column<short>(type: "smallint", nullable: true),
                    ExternalNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Credit = table.Column<short>(type: "smallint", nullable: true),
                    QualityPrejudgement = table.Column<short>(type: "smallint", nullable: true),
                    Traceability = table.Column<short>(type: "smallint", nullable: true),
                    AfterSalesService = table.Column<short>(type: "smallint", nullable: true),
                    DegreeAdaptability = table.Column<short>(type: "smallint", nullable: true),
                    ISCPFlag = table.Column<bool>(type: "boolean", nullable: false),
                    Strategy = table.Column<short>(type: "smallint", nullable: true),
                    SelfSupport = table.Column<bool>(type: "boolean", nullable: false),
                    BlackList = table.Column<bool>(type: "boolean", nullable: false),
                    IsDisenable = table.Column<bool>(type: "boolean", nullable: false),
                    Longitude = table.Column<decimal>(type: "numeric(10,6)", nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric(10,6)", nullable: true),
                    CompanyInfo = table.Column<string>(type: "text", nullable: true),
                    ListingCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VendorScope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsControl = table.Column<bool>(type: "boolean", nullable: false),
                    CreditCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AscriptionType = table.Column<short>(type: "smallint", nullable: false),
                    PurchaseUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    PurchaseGroupId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeleteReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendorinfo", x => x.VendorId);
                });

            migrationBuilder.CreateTable(
                name: "financepaymentitem",
                columns: table => new
                {
                    FinancePaymentItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    FinancePaymentId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    PurchaseOrderId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    PurchaseOrderItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    PaymentAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentAmountToBe = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ProductId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    PN = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Brand = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    VerificationStatus = table.Column<short>(type: "smallint", nullable: false),
                    VerificationDone = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VerificationToBe = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentId = table.Column<string>(type: "character varying(36)", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financepaymentitem", x => x.FinancePaymentItemId);
                    table.ForeignKey(
                        name: "FK_financepaymentitem_financepayment_FinancePaymentId",
                        column: x => x.FinancePaymentId,
                        principalTable: "financepayment",
                        principalColumn: "FinancePaymentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_financepaymentitem_financepayment_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "financepayment",
                        principalColumn: "FinancePaymentId");
                });

            migrationBuilder.CreateTable(
                name: "financepurchaseinvoiceitem",
                columns: table => new
                {
                    FinancePurchaseInvoiceItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    FinancePurchaseInvoiceId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    StockInId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    StockInCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    PurchaseOrderCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    StockInCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    BillCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    BillQty = table.Column<long>(type: "bigint", nullable: false),
                    BillAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ExcludTaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financepurchaseinvoiceitem", x => x.FinancePurchaseInvoiceItemId);
                    table.ForeignKey(
                        name: "FK_financepurchaseinvoiceitem_financepurchaseinvoice_FinancePu~",
                        column: x => x.FinancePurchaseInvoiceId,
                        principalTable: "financepurchaseinvoice",
                        principalColumn: "FinancePurchaseInvoiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "financereceiptitem",
                columns: table => new
                {
                    FinanceReceiptItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    FinanceReceiptId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    SellOrderId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    SellOrderItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    FinanceSellInvoiceId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    FinanceSellInvoiceItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ReceiptAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReceiptConvertAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StockOutItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ProductId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    PN = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Brand = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    VerificationStatus = table.Column<short>(type: "smallint", nullable: false),
                    ReceiptId = table.Column<string>(type: "character varying(36)", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financereceiptitem", x => x.FinanceReceiptItemId);
                    table.ForeignKey(
                        name: "FK_financereceiptitem_financereceipt_FinanceReceiptId",
                        column: x => x.FinanceReceiptId,
                        principalTable: "financereceipt",
                        principalColumn: "FinanceReceiptId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_financereceiptitem_financereceipt_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "financereceipt",
                        principalColumn: "FinanceReceiptId");
                });

            migrationBuilder.CreateTable(
                name: "sellinvoiceitem",
                columns: table => new
                {
                    SellInvoiceItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    FinanceSellInvoiceId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    InvoiceTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ValueAddedTax = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxFreeTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Qty = table.Column<long>(type: "bigint", nullable: false),
                    StockOutItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Currency = table.Column<byte>(type: "smallint", nullable: false),
                    ReceiveStatus = table.Column<short>(type: "smallint", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sellinvoiceitem", x => x.SellInvoiceItemId);
                    table.ForeignKey(
                        name: "FK_sellinvoiceitem_financesellinvoice_FinanceSellInvoiceId",
                        column: x => x.FinanceSellInvoiceId,
                        principalTable: "financesellinvoice",
                        principalColumn: "FinanceSellInvoiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stockinitem",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    StockInId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    MaterialId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    OrderQty = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    QtyReceived = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LocationId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    BatchNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProductionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsQualified = table.Column<bool>(type: "boolean", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stockinitem", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_stockinitem_stockin_StockInId",
                        column: x => x.StockInId,
                        principalTable: "stockin",
                        principalColumn: "StockInId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stockoutitem",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    StockOutId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    MaterialId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    OrderQty = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PlanQty = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PickQty = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ActualQty = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LocationId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    StockId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    WarehouseId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    BatchNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stockoutitem", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_stockoutitem_stockout_StockOutId",
                        column: x => x.StockOutId,
                        principalTable: "stockout",
                        principalColumn: "StockOutId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vendoraddress",
                columns: table => new
                {
                    AddressId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    VendorId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    AddressType = table.Column<short>(type: "smallint", nullable: false),
                    Country = table.Column<short>(type: "smallint", nullable: true),
                    Province = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    City = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Area = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContactName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendoraddress", x => x.AddressId);
                    table.ForeignKey(
                        name: "FK_vendoraddress_vendorinfo_VendorId",
                        column: x => x.VendorId,
                        principalTable: "vendorinfo",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vendorbankinfo",
                columns: table => new
                {
                    BankId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    VendorId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    BankName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BankAccount = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AccountName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BankBranch = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Currency = table.Column<short>(type: "smallint", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendorbankinfo", x => x.BankId);
                    table.ForeignKey(
                        name: "FK_vendorbankinfo_vendorinfo_VendorId",
                        column: x => x.VendorId,
                        principalTable: "vendorinfo",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vendorcontactinfo",
                columns: table => new
                {
                    ContactId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    VendorId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Department = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Mobile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Tel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QQ = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    WeChat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsMain = table.Column<bool>(type: "boolean", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendorcontactinfo", x => x.ContactId);
                    table.ForeignKey(
                        name: "FK_vendorcontactinfo_vendorinfo_VendorId",
                        column: x => x.VendorId,
                        principalTable: "vendorinfo",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_financepayment_FinancePaymentCode",
                table: "financepayment",
                column: "FinancePaymentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_financepaymentitem_FinancePaymentId",
                table: "financepaymentitem",
                column: "FinancePaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_financepaymentitem_PaymentId",
                table: "financepaymentitem",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_financepurchaseinvoiceitem_FinancePurchaseInvoiceId",
                table: "financepurchaseinvoiceitem",
                column: "FinancePurchaseInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_financereceipt_FinanceReceiptCode",
                table: "financereceipt",
                column: "FinanceReceiptCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_financereceiptitem_FinanceReceiptId",
                table: "financereceiptitem",
                column: "FinanceReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_financereceiptitem_ReceiptId",
                table: "financereceiptitem",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_sellinvoiceitem_FinanceSellInvoiceId",
                table: "sellinvoiceitem",
                column: "FinanceSellInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_stockinitem_StockInId",
                table: "stockinitem",
                column: "StockInId");

            migrationBuilder.CreateIndex(
                name: "IX_stockoutitem_StockOutId",
                table: "stockoutitem",
                column: "StockOutId");

            migrationBuilder.CreateIndex(
                name: "IX_upload_document_BizType_BizId",
                table: "upload_document",
                columns: new[] { "BizType", "BizId" });

            migrationBuilder.CreateIndex(
                name: "IX_upload_document_CreateTime",
                table: "upload_document",
                column: "CreateTime");

            migrationBuilder.CreateIndex(
                name: "IX_vendoraddress_VendorId",
                table: "vendoraddress",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_vendorbankinfo_VendorId",
                table: "vendorbankinfo",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_vendorcontactinfo_VendorId",
                table: "vendorcontactinfo",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_daily_sequence");

            migrationBuilder.DropTable(
                name: "financepaymentitem");

            migrationBuilder.DropTable(
                name: "financepurchaseinvoiceitem");

            migrationBuilder.DropTable(
                name: "financereceiptitem");

            migrationBuilder.DropTable(
                name: "sellinvoiceitem");

            migrationBuilder.DropTable(
                name: "stock");

            migrationBuilder.DropTable(
                name: "stockinitem");

            migrationBuilder.DropTable(
                name: "stockoutitem");

            migrationBuilder.DropTable(
                name: "stockoutrequest");

            migrationBuilder.DropTable(
                name: "upload_document");

            migrationBuilder.DropTable(
                name: "vendoraddress");

            migrationBuilder.DropTable(
                name: "vendorbankinfo");

            migrationBuilder.DropTable(
                name: "vendorcontacthistory");

            migrationBuilder.DropTable(
                name: "vendorcontactinfo");

            migrationBuilder.DropTable(
                name: "financepayment");

            migrationBuilder.DropTable(
                name: "financepurchaseinvoice");

            migrationBuilder.DropTable(
                name: "financereceipt");

            migrationBuilder.DropTable(
                name: "financesellinvoice");

            migrationBuilder.DropTable(
                name: "stockin");

            migrationBuilder.DropTable(
                name: "stockout");

            migrationBuilder.DropTable(
                name: "vendorinfo");
        }
    }
}
