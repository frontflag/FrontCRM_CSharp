using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "financepaymentitem");

            migrationBuilder.DropTable(
                name: "financepurchaseinvoiceitem");

            migrationBuilder.DropTable(
                name: "financereceiptitem");

            migrationBuilder.DropTable(
                name: "sellinvoiceitem");

            migrationBuilder.DropTable(
                name: "financepayment");

            migrationBuilder.DropTable(
                name: "financepurchaseinvoice");

            migrationBuilder.DropTable(
                name: "financereceipt");

            migrationBuilder.DropTable(
                name: "financesellinvoice");
        }
    }
}
