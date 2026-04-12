using CRM.Core.Models;
using CRM.Core.Models.Auth;
using CRM.Core.Models.Component;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Draft;
using CRM.Core.Models.Document;
using CRM.Core.Models.Favorite;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Material;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Quote;
using CRM.Core.Models.Rbac;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Sales;
using CRM.Core.Models.System;
using CRM.Core.Models.Tag;
using CRM.Core.Models.Vendor;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ===== 原有表 =====
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<CustomerInfo> Customers { get; set; } = null!;
        public DbSet<CustomerAddress> CustomerAddresses { get; set; } = null!;
        public DbSet<CustomerContactInfo> CustomerContacts { get; set; } = null!;
        public DbSet<CustomerBankInfo> CustomerBanks { get; set; } = null!;
        public DbSet<CustomerContactHistory> CustomerContactHistories { get; set; } = null!;

        // ===== 供应商相关表 =====
        public DbSet<VendorInfo> Vendors { get; set; } = null!;
        public DbSet<VendorAddress> VendorAddresses { get; set; } = null!;
        public DbSet<VendorContactInfo> VendorContacts { get; set; } = null!;
        public DbSet<VendorBankInfo> VendorBanks { get; set; } = null!;
        public DbSet<VendorContactHistory> VendorContactHistories { get; set; } = null!;

        // ===== 新增系统表 =====
        public DbSet<SysSerialNumber> SerialNumbers { get; set; } = null!;
        public DbSet<SysErrorLog> ErrorLogs { get; set; } = null!;
        public DbSet<DebugRecord> DebugRecords { get; set; } = null!;

        // ===== 物料缓存表 =====
        public DbSet<ComponentCache> ComponentCaches { get; set; } = null!;

        // ===== 需求模块 =====
        public DbSet<RFQ> RFQs { get; set; } = null!;
        public DbSet<RFQItem> RFQItems { get; set; } = null!;

        // ===== 报价模块 =====
        public DbSet<Quote> Quotes { get; set; } = null!;
        public DbSet<QuoteItem> QuoteItems { get; set; } = null!;

        // ===== 销售订单模块 =====
        public DbSet<SellOrder> SellOrders { get; set; } = null!;
        public DbSet<SellOrderItem> SellOrderItems { get; set; } = null!;
        public DbSet<SellOrderItemExtend> SellOrderItemExtends { get; set; } = null!;
        public DbSet<SellOrderExtend> SellOrderExtends { get; set; } = null!;

        // ===== 采购订单模块 =====
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = null!;
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; } = null!;
        public DbSet<PurchaseOrderItemExtend> PurchaseOrderItemExtends { get; set; } = null!;
        public DbSet<PurchaseOrderExtend> PurchaseOrderExtends { get; set; } = null!;

        // ===== 采购申请模块 =====
        public DbSet<PurchaseRequisition> PurchaseRequisitions { get; set; } = null!;

        // ===== 财务模块（收款/付款/进销项发票）=====
        public DbSet<FinanceReceipt> FinanceReceipts { get; set; } = null!;
        public DbSet<FinanceReceiptItem> FinanceReceiptItems { get; set; } = null!;
        public DbSet<FinancePayment> FinancePayments { get; set; } = null!;
        public DbSet<FinancePaymentItem> FinancePaymentItems { get; set; } = null!;
        public DbSet<FinancePurchaseInvoice> FinancePurchaseInvoices { get; set; } = null!;
        public DbSet<FinancePurchaseInvoiceItem> FinancePurchaseInvoiceItems { get; set; } = null!;
        public DbSet<FinanceSellInvoice> FinanceSellInvoices { get; set; } = null!;
        public DbSet<SellInvoiceItem> SellInvoiceItems { get; set; } = null!;
        public DbSet<FinanceExchangeRateSetting> FinanceExchangeRateSettings { get; set; } = null!;
        public DbSet<FinanceExchangeRateChangeLog> FinanceExchangeRateChangeLogs { get; set; } = null!;
        public DbSet<PaymentRequest> PaymentRequests { get; set; } = null!;

        // ===== 物料主数据 =====
        public DbSet<MaterialInfo> Materials { get; set; } = null!;
        public DbSet<MaterialCategory> MaterialCategories { get; set; } = null!;

        // ===== 库存/入库/出库 =====
        public DbSet<StockInfo> Stocks { get; set; } = null!;
        public DbSet<StockIn> StockIns { get; set; } = null!;
        public DbSet<StockInItem> StockInItems { get; set; } = null!;
        public DbSet<StockOut> StockOuts { get; set; } = null!;
        public DbSet<StockOutItem> StockOutItems { get; set; } = null!;
        public DbSet<StockOutRequest> StockOutRequests { get; set; } = null!;
        public DbSet<StockInNotify> StockInNotifies { get; set; } = null!;
        public DbSet<QCInfo> QCInfos { get; set; } = null!;
        public DbSet<QCItem> QCItems { get; set; } = null!;
        public DbSet<WarehouseInfo> Warehouses { get; set; } = null!;
        public DbSet<WarehouseZone> WarehouseZones { get; set; } = null!;
        public DbSet<WarehouseLocation> WarehouseLocations { get; set; } = null!;
        public DbSet<WarehouseShelf> WarehouseShelves { get; set; } = null!;
        public DbSet<InventoryLedger> InventoryLedgers { get; set; } = null!;
        public DbSet<PickingTask> PickingTasks { get; set; } = null!;
        public DbSet<PickingTaskItem> PickingTaskItems { get; set; } = null!;
        public DbSet<InventoryCountPlan> InventoryCountPlans { get; set; } = null!;
        public DbSet<InventoryCountItem> InventoryCountItems { get; set; } = null!;

        // ===== 文档模块 =====
        public DbSet<UploadDocument> UploadDocuments { get; set; } = null!;
        public DbSet<DocumentDailySequence> DocumentDailySequences { get; set; } = null!;

        // ===== 标签系统 =====
        public DbSet<TagDefinition> Tags { get; set; } = null!;
        public DbSet<TagRelation> TagRelations { get; set; } = null!;
        public DbSet<UserTagPreference> UserTagPreferences { get; set; } = null!;
        public DbSet<UserFavorite> UserFavorites { get; set; } = null!;
        public DbSet<BizDraft> BizDrafts { get; set; } = null!;
        public DbSet<RbacDepartment> RbacDepartments { get; set; } = null!;
        public DbSet<RbacRole> RbacRoles { get; set; } = null!;
        public DbSet<RbacPermission> RbacPermissions { get; set; } = null!;
        public DbSet<RbacUserDepartment> RbacUserDepartments { get; set; } = null!;
        public DbSet<RbacUserRole> RbacUserRoles { get; set; } = null!;
        public DbSet<RbacRolePermission> RbacRolePermissions { get; set; } = null!;

        // ===== 微信认证 =====
        public DbSet<WechatLoginTicket> WechatLoginTickets { get; set; } = null!;
        public DbSet<WechatBindRequest> WechatBindRequests { get; set; } = null!;

        // ===== 系统参数 =====
        public DbSet<SysParamGroup> SysParamGroups { get; set; } = null!;
        public DbSet<SysParam> SysParams { get; set; } = null!;
        public DbSet<SysDictItem> SysDictItems { get; set; } = null!;
        public DbSet<ApprovalRecord> ApprovalRecords { get; set; } = null!;
        public DbSet<OrderJourneyLog> OrderJourneyLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== RFQ 需求模块配置 =====
            modelBuilder.Entity<RFQ>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("rfq_id");
                entity.Property(e => e.RfqCode).IsRequired().HasMaxLength(32);
                entity.HasIndex(e => e.RfqCode).IsUnique();
                entity.Property(e => e.Status).HasDefaultValue((short)1);
                entity.HasMany(e => e.Items)
                      .WithOne(i => i.RFQ)
                      .HasForeignKey(i => i.RfqId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RFQItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("item_id");
                entity.Property(e => e.RfqId).IsRequired();
                entity.Property(e => e.Mpn).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CustomerBrand).HasMaxLength(100);
                entity.Property(e => e.Brand).HasMaxLength(100);
                entity.Property(e => e.AssignedPurchaserUserId1).HasMaxLength(36);
                entity.Property(e => e.AssignedPurchaserUserId2).HasMaxLength(36);
                entity.Property(e => e.Quantity).HasColumnType("numeric(18,4)").HasDefaultValue(1m);
                entity.Property(e => e.TargetPrice).HasColumnType("numeric(18,6)");
                entity.HasIndex(e => e.RfqId);
            });

            // ===== 报价模块配置 =====
            modelBuilder.Entity<Quote>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("QuoteId");
                entity.Property(e => e.QuoteCode).IsRequired().HasMaxLength(32);
                entity.HasIndex(e => e.QuoteCode).IsUnique();
                entity.Property(e => e.Status).HasDefaultValue((short)0);
                entity.Property(e => e.QuoteDate).HasDefaultValueSql("NOW()");
                entity.HasMany(e => e.Items)
                      .WithOne(i => i.Quote)
                      .HasForeignKey(i => i.QuoteId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<QuoteItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("QuoteItemId");
                entity.Property(e => e.QuoteId).IsRequired();
                entity.Property(e => e.Quantity).HasColumnType("numeric(18,4)").HasDefaultValue(0m);
                entity.Property(e => e.UnitPrice).HasColumnType("numeric(18,6)").HasDefaultValue(0m);
                entity.Property(e => e.ConvertedPrice).HasColumnType("numeric(18,6)");
                entity.HasIndex(e => e.QuoteId);
            });

            // ===== 销售订单模块配置 =====
            modelBuilder.Entity<SellOrder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("SellOrderId");
                entity.Property(e => e.SellOrderCode).IsRequired().HasMaxLength(32);
                entity.HasIndex(e => e.SellOrderCode).IsUnique();
                entity.Property(e => e.Status)
                    .HasConversion(
                        v => (short)v,
                        v => (SellOrderMainStatus)v)
                    .HasDefaultValue(SellOrderMainStatus.New);
                entity.HasMany(e => e.Items)
                      .WithOne(i => i.SellOrder)
                      .HasForeignKey(i => i.SellOrderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SellOrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("SellOrderItemId");
                entity.Property(e => e.SellOrderId).IsRequired();
                entity.Property(e => e.Qty).HasColumnType("numeric(18,4)").HasDefaultValue(0m);
                entity.Property(e => e.Price).HasColumnType("numeric(18,6)").HasDefaultValue(0m);
                entity.Property(e => e.ConvertPrice).HasColumnType("numeric(18,6)").HasDefaultValue(0m);
                entity.Property(e => e.PurchasedQty).HasColumnType("numeric(18,4)").HasDefaultValue(0m);
                // 与迁移 20260409120000 列名 sell_order_item_code 一致（避免仅 HasMaxLength 时映射回退为属性名）
                entity.Property(e => e.SellOrderItemCode).HasColumnName("sell_order_item_code").HasMaxLength(64);
                entity.HasIndex(e => new { e.SellOrderId, e.SellOrderItemCode }).IsUnique();
                entity.HasIndex(e => e.SellOrderId);
            });

            modelBuilder.Entity<SellOrderExtend>(entity =>
            {
                entity.HasKey(e => e.SellOrderId);
                entity.ToTable("sellorderextend");
                entity.Property(e => e.SellOrderId).HasColumnName("SellOrderId").HasMaxLength(36);
                entity.Property(e => e.LastItemLineSeq).HasDefaultValue(0);
            });

            modelBuilder.Entity<SellOrderItemExtend>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("SellOrderItemId");
                entity.ToTable("sellorderitemextend");
                entity.Property(e => e.QtyAlreadyPurchased).HasColumnType("numeric(18,4)").HasDefaultValue(0m);
                entity.Property(e => e.QtyNotPurchase).HasColumnType("numeric(18,4)").HasDefaultValue(0m);
                entity.Property(e => e.QtyStockOutNotify).HasColumnType("numeric(18,4)").HasDefaultValue(0m);
                entity.Property(e => e.QtyStockOutNotifyNot).HasColumnType("numeric(18,4)").HasDefaultValue(0m);
                entity.Property(e => e.QtyStockOutActual).HasColumnType("numeric(18,4)").HasDefaultValue(0m);
                entity.Property(e => e.InvoiceAmount).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.InvoiceAmountNot).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.InvoiceAmountFinish).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.ReceiptAmount).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.ReceiptAmountNot).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.ReceiptAmountFinish).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.PaymentAmount).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.PaymentAmountDone).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.PaymentAmountToBe).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.PurchaseInvoiceAmount).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.PurchaseInvoiceDone).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.QuoteItemId).HasMaxLength(36);
                entity.Property(e => e.QuoteCost).HasColumnType("numeric(18,6)").HasDefaultValue(0m);
                entity.Property(e => e.QuoteConvertCost).HasColumnType("numeric(18,6)").HasDefaultValue(0m);
                entity.Property(e => e.FxUsdToCnySnapshot).HasColumnType("numeric(18,6)").HasDefaultValue(0m);
                entity.Property(e => e.FxUsdToHkdSnapshot).HasColumnType("numeric(18,6)").HasDefaultValue(0m);
                entity.Property(e => e.FxUsdToEurSnapshot).HasColumnType("numeric(18,6)").HasDefaultValue(0m);
                entity.Property(e => e.SellConvertUsdUnitSnapshot).HasColumnType("numeric(18,6)").HasDefaultValue(0m);
                entity.Property(e => e.SellLineAmountUsdSnapshot).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.QuoteProfitExpected).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.QuoteProfitRateExpected).HasColumnType("numeric(18,6)").HasDefaultValue(0m);
                entity.Property(e => e.ReQuoteProfitExpected).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.ReQuoteProfitRateExpected).HasColumnType("numeric(18,6)").HasDefaultValue(0m);
                entity.Property(e => e.PoCostUsdTotal).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.PurchaseProfitExpected).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.PurchaseProfitRateExpected).HasColumnType("numeric(18,6)").HasDefaultValue(0m);
                entity.Property(e => e.PoCostUsdConfirmed).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.SalesProfitExpected).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.ProfitOutBizUsd).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.ProfitOutRateBiz).HasColumnType("numeric(18,6)").HasDefaultValue(0m);
                entity.Property(e => e.ProfitOutFinUsd).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.ProfitOutRateFin).HasColumnType("numeric(18,6)").HasDefaultValue(0m);
                entity.Property(e => e.PurchaseProgressStatus).HasDefaultValue((short)0);
                entity.Property(e => e.StockInProgressStatus).HasDefaultValue((short)0);
                entity.Property(e => e.StockOutProgressStatus).HasDefaultValue((short)0);
                entity.Property(e => e.PurchasedStock_AvailableQty).HasColumnName("PurchasedStock_AvailableQty").HasDefaultValue(0);
                entity.Property(e => e.ReceiptProgressStatus).HasDefaultValue((short)0);
                entity.Property(e => e.InvoiceProgressStatus).HasDefaultValue((short)0);
            });

            // ===== 采购订单模块配置 =====
            modelBuilder.Entity<PurchaseOrder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("PurchaseOrderId");
                entity.Property(e => e.PurchaseOrderCode).IsRequired().HasMaxLength(32);
                entity.HasIndex(e => e.PurchaseOrderCode).IsUnique();
                entity.Property(e => e.Status).HasDefaultValue((short)0);
                entity.HasMany(e => e.Items)
                      .WithOne(i => i.PurchaseOrder)
                      .HasForeignKey(i => i.PurchaseOrderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PurchaseOrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("PurchaseOrderItemId");
                entity.Property(e => e.PurchaseOrderId).IsRequired();
                entity.Property(e => e.SellOrderItemId).IsRequired(false);
                entity.Property(e => e.Qty).HasColumnType("numeric(18,4)").HasDefaultValue(0m);
                entity.Property(e => e.Cost).HasColumnType("numeric(18,6)").HasDefaultValue(0m);
                entity.Property(e => e.ConvertPrice).HasColumnType("numeric(18,6)").HasDefaultValue(0m);
                entity.Property(e => e.Status).HasDefaultValue((short)1);
                // 与迁移 20260409120000 列名 purchase_order_item_code 一致（否则 SQL 会引用不存在的 "PurchaseOrderItemCode"）
                entity.Property(e => e.PurchaseOrderItemCode).HasColumnName("purchase_order_item_code").HasMaxLength(64);
                entity.HasIndex(e => new { e.PurchaseOrderId, e.PurchaseOrderItemCode }).IsUnique();
                entity.HasIndex(e => e.PurchaseOrderId);
                entity.HasIndex(e => e.SellOrderItemId);
                entity.HasOne(e => e.SellOrderItem)
                      .WithMany()
                      .HasForeignKey(e => e.SellOrderItemId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PurchaseOrderExtend>(entity =>
            {
                entity.HasKey(e => e.PurchaseOrderId);
                entity.ToTable("purchaseorderextend");
                entity.Property(e => e.PurchaseOrderId).HasColumnName("PurchaseOrderId").HasMaxLength(36);
                entity.Property(e => e.LastItemLineSeq).HasDefaultValue(0);
            });

            modelBuilder.Entity<PurchaseOrderItemExtend>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("PurchaseOrderItemId");
                entity.ToTable("purchaseorderitemextend");
                entity.Property(e => e.QtyStockInNotifyNot).HasColumnType("numeric(18,4)").HasDefaultValue(0m);
                entity.Property(e => e.QtyStockInNotifyExpectSum).HasColumnType("numeric(18,4)").HasDefaultValue(0m);
                entity.Property(e => e.QtyReceiveTotal).HasColumnType("numeric(18,4)").HasDefaultValue(0m);
                entity.Property(e => e.PurchaseInvoiceAmount).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.PurchaseInvoiceDone).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.PurchaseInvoiceToBe).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.PaymentAmount).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.PaymentAmountNot).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.PaymentAmountFinish).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.PaymentAmountRequested).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.ReceiptAmount).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.ReceiptAmountNot).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.ReceiptAmountFinish).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.PurchaseProgressStatus).HasDefaultValue((short)0);
                entity.Property(e => e.PurchaseProgressQty).HasColumnType("numeric(18,4)").HasDefaultValue(0m);
                entity.Property(e => e.StockInProgressStatus).HasDefaultValue((short)0);
                entity.Property(e => e.PaymentProgressStatus).HasDefaultValue((short)0);
                entity.Property(e => e.InvoiceProgressStatus).HasDefaultValue((short)0);
            });

            // ===== 软删除全局过滤器 =====
            // 所有查询自动过滤已删除的客户
            modelBuilder.Entity<CustomerInfo>().HasQueryFilter(e => !e.IsDeleted);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(256);
                entity.Property(e => e.Salt).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).HasDefaultValue((short)1);
            });

            // Customer configuration
            modelBuilder.Entity<CustomerInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CustomerCode).IsRequired().HasMaxLength(16);
                entity.Property(e => e.CreditCode).HasMaxLength(50);
                entity.Property(e => e.OfficialName).HasMaxLength(128);
                entity.Property(e => e.Province).HasMaxLength(50);
                entity.Property(e => e.City).HasMaxLength(50);
                entity.Property(e => e.District).HasMaxLength(50);
                entity.Property(e => e.Status).HasDefaultValue((short)1);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            });

            // Customer Address configuration
            modelBuilder.Entity<CustomerAddress>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Address).HasMaxLength(256);
                entity.Property(e => e.ContactName).HasMaxLength(50);
                entity.Property(e => e.ContactPhone).HasMaxLength(20);
            });

            // Customer Contact configuration
            modelBuilder.Entity<CustomerContactInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CName).HasMaxLength(50);
                entity.Property(e => e.Tel).HasMaxLength(30);
                entity.Property(e => e.Mobile).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Title).HasMaxLength(50);
                entity.Property(e => e.Department).HasMaxLength(50);
                entity.Ignore(e => e.Name);
                entity.Ignore(e => e.Phone);
                entity.Ignore(e => e.Position);
                entity.Ignore(e => e.Fax);
            });

            // Customer Bank configuration
            modelBuilder.Entity<CustomerBankInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.BankName).HasMaxLength(100);
                entity.Property(e => e.BankAccount).HasMaxLength(50);
            });

            // Customer Contact History configuration
            modelBuilder.Entity<CustomerContactHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CustomerId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.Type).HasMaxLength(50);
                entity.Property(e => e.Content).HasMaxLength(500);
            });

            // Vendor configuration
            modelBuilder.Entity<VendorInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(16);
                entity.Property(e => e.OfficialName).HasMaxLength(64);
                entity.Property(e => e.NickName).HasMaxLength(64);
                entity.Property(e => e.Website).HasMaxLength(300);
                entity.Property(e => e.PurchaserName).HasMaxLength(64);
                entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            });

            // Vendor Address configuration
            modelBuilder.Entity<VendorAddress>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Address).HasMaxLength(200);
                entity.Property(e => e.ContactName).HasMaxLength(50);
                entity.Property(e => e.ContactPhone).HasMaxLength(20);
            });

            // Vendor Contact configuration
            modelBuilder.Entity<VendorContactInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CName).HasMaxLength(50);
                entity.Property(e => e.EName).HasMaxLength(100);
                entity.Property(e => e.Mobile).HasMaxLength(20);
                entity.Property(e => e.Tel).HasMaxLength(30);
                entity.Property(e => e.Email).HasMaxLength(100);
            });

            // Vendor Bank configuration
            modelBuilder.Entity<VendorBankInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.BankName).HasMaxLength(100);
                entity.Property(e => e.BankAccount).HasMaxLength(50);
                entity.Property(e => e.AccountName).HasMaxLength(50);
                entity.Property(e => e.BankBranch).HasMaxLength(100);
            });

            // Vendor Contact History configuration
            modelBuilder.Entity<VendorContactHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.VendorId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.Type).HasMaxLength(50);
                entity.Property(e => e.Content).HasMaxLength(500);
            });

            // Vendor Contact History configuration
            modelBuilder.Entity<VendorContactHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.VendorId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.Type).HasMaxLength(50);
                entity.Property(e => e.Content).HasMaxLength(500);
            });

            // ===== 流水号管理表配置 =====
            modelBuilder.Entity<SysSerialNumber>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ModuleCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Prefix).IsRequired().HasMaxLength(16);
                entity.HasIndex(e => e.ModuleCode).IsUnique();
                entity.HasIndex(e => e.Prefix).IsUnique();

                // 种子数据：初始化所有业务模块的流水号配置
                // 注意：CreateTime 使用静态时间，避免 EF Core PendingModelChangesWarning
                var seedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                entity.HasData(
                    new SysSerialNumber { Id = 1,  ModuleCode = "Customer",      ModuleName = "客户",     Prefix = "CUS",     SequenceLength = 5, CurrentSequence = -1,   CreateTime = seedTime },
                    new SysSerialNumber { Id = 2,  ModuleCode = "Vendor",        ModuleName = "供应商",   Prefix = "VEN",     SequenceLength = 5, CurrentSequence = -1,   CreateTime = seedTime },
                    new SysSerialNumber { Id = 3,  ModuleCode = "RFQ",           ModuleName = "询价/需求", Prefix = "RFQ",     SequenceLength = 5, CurrentSequence = 2025, CreateTime = seedTime },
                    new SysSerialNumber { Id = 4,  ModuleCode = "Quotation",     ModuleName = "报价",     Prefix = "QUO",     SequenceLength = 5, CurrentSequence = 2025, CreateTime = seedTime },
                    new SysSerialNumber { Id = 5,  ModuleCode = "SalesOrder",    ModuleName = "销售订单", Prefix = "SO",      SequenceLength = 5, CurrentSequence = 2025, CreateTime = seedTime },
                    new SysSerialNumber { Id = 6,  ModuleCode = "PurchaseOrder", ModuleName = "采购订单", Prefix = "PO",      SequenceLength = 5, CurrentSequence = 2025, CreateTime = seedTime },
                    new SysSerialNumber { Id = 7,  ModuleCode = "StockIn",       ModuleName = "入库",     Prefix = "STI",     SequenceLength = 5, CurrentSequence = 2025, CreateTime = seedTime },
                    new SysSerialNumber { Id = 8,  ModuleCode = "StockOut",      ModuleName = "出库",     Prefix = "STO",     SequenceLength = 5, CurrentSequence = 2025, CreateTime = seedTime },
                    new SysSerialNumber { Id = 9,  ModuleCode = "Receipt",       ModuleName = "收款",     Prefix = "REC",     SequenceLength = 5, CurrentSequence = 2025, CreateTime = seedTime },
                    new SysSerialNumber { Id = 11, ModuleCode = "Payment",       ModuleName = "付款",     Prefix = "PAY_DEL", SequenceLength = 5, CurrentSequence = 2025, CreateTime = seedTime },
                    new SysSerialNumber { Id = 12, ModuleCode = "InputInvoice",  ModuleName = "进项发票", Prefix = "INVI",    SequenceLength = 5, CurrentSequence = 2025, CreateTime = seedTime },
                    new SysSerialNumber { Id = 13, ModuleCode = "OutputInvoice", ModuleName = "销项发票", Prefix = "INVO",    SequenceLength = 5, CurrentSequence = 2025, CreateTime = seedTime },
                    new SysSerialNumber { Id = 14, ModuleCode = "Stock",         ModuleName = "库存",     Prefix = "STK",     SequenceLength = 5, CurrentSequence = 2025, CreateTime = seedTime },
                    new SysSerialNumber { Id = 15, ModuleCode = "PurchaseRequisition", ModuleName = "采购申请", Prefix = "POR",     SequenceLength = 5, CurrentSequence = 2025, CreateTime = seedTime },
                    new SysSerialNumber { Id = 16, ModuleCode = "StockOutRequest",     ModuleName = "出库申请", Prefix = "STOR",    SequenceLength = 5, CurrentSequence = 2025, CreateTime = seedTime },
                    new SysSerialNumber { Id = 17, ModuleCode = "PickingTask",         ModuleName = "拣货任务", Prefix = "PAK",     SequenceLength = 5, CurrentSequence = 2025, CreateTime = seedTime },
                    new SysSerialNumber { Id = 18, ModuleCode = "ArrivalNotice",      ModuleName = "到货通知", Prefix = "STIR",    SequenceLength = 5, CurrentSequence = 2025, CreateTime = seedTime },
                    new SysSerialNumber { Id = 19, ModuleCode = "QcRecord",           ModuleName = "质检",     Prefix = "QC",      SequenceLength = 5, CurrentSequence = 2025, CreateTime = seedTime },
                    new SysSerialNumber { Id = 20, ModuleCode = "PaymentRequest",     ModuleName = "请款",     Prefix = "PAYR",    SequenceLength = 5, CurrentSequence = 2025, CreateTime = seedTime },
                    new SysSerialNumber { Id = 21, ModuleCode = "FinancePayment",     ModuleName = "财务付款", Prefix = "PAY",     SequenceLength = 5, CurrentSequence = 2025, CreateTime = seedTime }
                );
            });

            // ===== 错误日志表配置 =====
            modelBuilder.Entity<SysErrorLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ModuleName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ErrorMessage).IsRequired().HasMaxLength(500);
            });

            // ===== Debug 调试表（无认证调试用）=====
            modelBuilder.Entity<DebugRecord>(entity =>
            {
                entity.HasKey(e => e.Name);
                entity.ToTable("debug");
            });

            // ===== 物料缓存表配置 =====
            modelBuilder.Entity<ComponentCache>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Mpn).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Mpn).IsUnique();  // MPN 唯一索引，加速查询
                entity.Property(e => e.ManufacturerName).HasMaxLength(200);
                entity.Property(e => e.ShortDescription).HasMaxLength(500);
                entity.Property(e => e.LifecycleStatus).HasMaxLength(50);
                entity.Property(e => e.PackageType).HasMaxLength(100);
                entity.Property(e => e.DataSource).HasMaxLength(50);
                // JSON 列使用 text 类型，不限制长度
                entity.Property(e => e.SpecsJson).HasColumnType("text");
                entity.Property(e => e.SellersJson).HasColumnType("text");
                entity.Property(e => e.AlternativesJson).HasColumnType("text");
                entity.Property(e => e.ApplicationsJson).HasColumnType("text");
                entity.Property(e => e.PriceTrendJson).HasColumnType("text");
                entity.Property(e => e.NewsJson).HasColumnType("text");
                entity.Property(e => e.Description).HasColumnType("text");
            });

            // ===== 物料主数据表配置 =====
            modelBuilder.Entity<MaterialCategory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("CategoryId").HasMaxLength(36);
                entity.Property(e => e.CategoryCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ParentId).HasMaxLength(36);
                entity.Property(e => e.Remark).HasMaxLength(500);
                entity.HasOne(e => e.Parent)
                    .WithMany(e => e.Children)
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MaterialInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("MaterialId").HasMaxLength(36);
                entity.Property(e => e.MaterialCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.MaterialName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.MaterialModel).HasMaxLength(100);
                entity.Property(e => e.BrandId).HasMaxLength(36);
                entity.Property(e => e.CategoryId).HasMaxLength(36);
                entity.Property(e => e.Unit).HasMaxLength(20);
                entity.Property(e => e.Remark).HasMaxLength(500);
                entity.HasOne(e => e.Category)
                    .WithMany(c => c.Materials)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ===== 库存/入库/出库表配置 =====
            modelBuilder.Entity<StockInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MaterialId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.WarehouseId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.LocationId).HasMaxLength(36);
                entity.Property(e => e.Unit).HasMaxLength(20);
                entity.Property(e => e.BatchNo).HasMaxLength(50);
                // 与 purchaseorderitem / sellorderitem 一致：snake_case（迁移 20260417100000 重建列）
                entity.Property(e => e.PurchaseOrderItemCode).HasColumnName("purchase_order_item_code").HasMaxLength(64);
                entity.Property(e => e.PurchaseOrderItemId).HasColumnName("purchase_order_item_id").HasMaxLength(36);
                entity.Property(e => e.SellOrderItemCode).HasColumnName("sell_order_item_code").HasMaxLength(64);
                entity.Property(e => e.SellOrderItemId).HasColumnName("sell_order_item_id").HasMaxLength(36);
                entity.Property(e => e.PurchasePn).HasColumnName("purchase_pn").HasMaxLength(200);
                entity.Property(e => e.PurchaseBrand).HasColumnName("purchase_brand").HasMaxLength(200);
                entity.Property(e => e.StockType).HasColumnName("Type").HasDefaultValue((short)1);
                entity.Property(e => e.RegionType).HasColumnName("RegionType").HasDefaultValue((short)10);
                entity.Property(e => e.StockCode).HasColumnName("StockCode").HasMaxLength(32);
                entity.Property(e => e.Remark).HasMaxLength(500);
                entity.HasIndex(e => e.StockCode)
                    .IsUnique()
                    .HasDatabaseName("IX_stock_StockCode_unique_not_null")
                    .HasFilter("\"StockCode\" IS NOT NULL");
            });

            modelBuilder.Entity<StockIn>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StockInCode).IsRequired().HasMaxLength(32);
                entity.Property(e => e.PurchaseOrderItemCode).HasColumnName("purchase_order_item_code").HasMaxLength(64);
                entity.Property(e => e.PurchaseOrderItemId).HasColumnName("purchase_order_item_id").HasMaxLength(36);
                entity.Property(e => e.SellOrderItemCode).HasColumnName("sell_order_item_code").HasMaxLength(64);
                entity.Property(e => e.SellOrderItemId).HasColumnName("sell_order_item_id").HasMaxLength(36);
                entity.Property(e => e.SourceCode).HasMaxLength(32);
                entity.Property(e => e.SourceId).HasMaxLength(36);
                entity.Property(e => e.QcCode).HasMaxLength(32);
                entity.Property(e => e.QcId).HasMaxLength(36).HasColumnName("QCID");
                entity.Property(e => e.WarehouseId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.VendorId).HasMaxLength(36);
                entity.Property(e => e.RegionType).HasColumnName("RegionType").HasDefaultValue((short)10);
                entity.Property(e => e.Remark).HasMaxLength(500);
                entity.HasMany(e => e.Items).WithOne(e => e.StockIn).HasForeignKey(e => e.StockInId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StockInItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StockInId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.MaterialId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.LocationId).HasMaxLength(36);
                entity.Property(e => e.BatchNo).HasMaxLength(50);
                entity.Property(e => e.Remark).HasMaxLength(500);
            });

            modelBuilder.Entity<StockOut>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StockOutCode).IsRequired().HasMaxLength(32);
                entity.Property(e => e.SourceCode).HasMaxLength(32);
                entity.Property(e => e.WarehouseId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.CustomerId).HasMaxLength(36);
                entity.Property(e => e.SellOrderItemId).HasMaxLength(36);
                entity.Property(e => e.Remark).HasMaxLength(500);
                entity.Property(e => e.ShipmentMethod).HasMaxLength(64);
                entity.Property(e => e.CourierTrackingNo).HasMaxLength(128);
                entity.Property(e => e.RegionType).HasColumnName("RegionType").HasDefaultValue((short)10);
                entity.HasMany(e => e.Items).WithOne(e => e.StockOut).HasForeignKey(e => e.StockOutId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StockOutItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StockOutId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.MaterialId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.LocationId).HasMaxLength(36);
                entity.Property(e => e.BatchNo).HasMaxLength(50);
                entity.Property(e => e.Remark).HasMaxLength(500);
            });

            modelBuilder.Entity<StockOutRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RequestCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SalesOrderId).HasMaxLength(36);
                entity.Property(e => e.SalesOrderItemId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.MaterialCode).IsRequired().HasMaxLength(200);
                entity.Property(e => e.MaterialName).HasMaxLength(200);
                entity.Property(e => e.CustomerId).HasMaxLength(36);
                entity.Property(e => e.RequestUserId).HasMaxLength(36);
                entity.Property(e => e.Remark).HasMaxLength(500);
                entity.Property(e => e.ShipmentMethod).HasMaxLength(64);
                entity.Property(e => e.RegionType).HasColumnName("RegionType").HasDefaultValue((short)10);
            });

            modelBuilder.Entity<StockInNotify>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NoticeCode).IsRequired().HasMaxLength(32);
                entity.Property(e => e.PurchaseOrderId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.PurchaseOrderCode).IsRequired().HasMaxLength(32);
                entity.Property(e => e.PurchaseOrderItemId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.SellOrderItemId).HasMaxLength(36);
                entity.Property(e => e.VendorId).HasMaxLength(36);
                entity.Property(e => e.VendorName).HasMaxLength(64);
                entity.Property(e => e.PurchaseUserName).HasMaxLength(64);
                entity.Property(e => e.Status).HasDefaultValue((short)10);
                entity.Property(e => e.ExpectedArrivalDate);
                entity.Property(e => e.RegionType).HasColumnName("RegionType").HasDefaultValue((short)10);
                entity.Property(e => e.Pn).HasMaxLength(128);
                entity.Property(e => e.Brand).HasMaxLength(64);
                entity.Property(e => e.ExpectQty).HasDefaultValue(0);
                entity.Property(e => e.ReceiveQty).HasDefaultValue(0);
                entity.Property(e => e.PassedQty).HasDefaultValue(0);
                entity.Property(e => e.Cost).HasColumnType("numeric(18,6)").HasDefaultValue(0m);
                entity.Property(e => e.ExpectTotal).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.ReceiveTotal).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.Ignore(e => e.Items);
            });

            modelBuilder.Entity<QCInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.QcCode).IsRequired().HasMaxLength(32);
                entity.Property(e => e.StockInNotifyId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.StockInNotifyCode).IsRequired().HasMaxLength(32);
                entity.Property(e => e.StockInId).HasMaxLength(36);
                entity.Property(e => e.Status).HasDefaultValue((short)10);
                entity.Property(e => e.StockInStatus).HasDefaultValue((short)1);
                entity.HasMany(e => e.Items)
                    .WithOne(x => x.QcInfo)
                    .HasForeignKey(x => x.QcInfoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<QCItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.QcInfoId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.ArrivalStockInNotifyId).IsRequired().HasMaxLength(36);
            });

            modelBuilder.Entity<WarehouseInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.WarehouseCode).IsRequired().HasMaxLength(32);
                entity.Property(e => e.WarehouseName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Address).HasMaxLength(200);
                entity.Property(e => e.RegionType).HasColumnName("RegionType");
                entity.HasIndex(e => e.WarehouseCode).IsUnique();
            });

            modelBuilder.Entity<WarehouseZone>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.WarehouseId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.ZoneCode).IsRequired().HasMaxLength(32);
                entity.Property(e => e.ZoneName).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => new { e.WarehouseId, e.ZoneCode }).IsUnique();
            });

            modelBuilder.Entity<WarehouseLocation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.ZoneId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.LocationCode).IsRequired().HasMaxLength(32);
                entity.Property(e => e.LocationName).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => new { e.ZoneId, e.LocationCode }).IsUnique();
            });

            modelBuilder.Entity<WarehouseShelf>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.LocationId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.ShelfCode).IsRequired().HasMaxLength(32);
                entity.Property(e => e.ShelfName).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => new { e.LocationId, e.ShelfCode }).IsUnique();
            });

            modelBuilder.Entity<InventoryLedger>(entity =>
            {
                entity.ToTable("stockledger");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.BizType).IsRequired().HasMaxLength(20);
                entity.Property(e => e.BizId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.BizLineId).HasMaxLength(36);
                entity.Property(e => e.MaterialId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.WarehouseId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.LocationId).HasMaxLength(36);
                entity.Property(e => e.BatchNo).HasMaxLength(50);
                entity.Property(e => e.UnitCost).HasColumnType("numeric(18,6)");
                entity.Property(e => e.Amount).HasColumnType("numeric(18,2)");
                // 与 stock / purchaseorderitem 一致：snake_case（迁移 20260417100000）
                entity.Property(e => e.PurchaseOrderItemCode).HasColumnName("purchase_order_item_code").HasMaxLength(64);
                entity.Property(e => e.PurchaseOrderItemId).HasColumnName("purchase_order_item_id").HasMaxLength(36);
                entity.Property(e => e.SellOrderItemCode).HasColumnName("sell_order_item_code").HasMaxLength(64);
                entity.Property(e => e.SellOrderItemId).HasColumnName("sell_order_item_id").HasMaxLength(36);
                entity.Property(e => e.Remark).HasMaxLength(500);
                entity.HasIndex(e => new { e.BizType, e.BizId, e.BizLineId })
                    .IsUnique()
                    .HasDatabaseName("IX_stockledger_BizType_BizId_BizLineId");
            });

            modelBuilder.Entity<PickingTask>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.TaskCode).IsRequired().HasMaxLength(32);
                entity.Property(e => e.StockOutRequestId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.WarehouseId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.OperatorId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.Remark).HasMaxLength(500);
                entity.HasIndex(e => e.TaskCode).IsUnique();
                entity.HasMany(e => e.Items)
                    .WithOne(e => e.PickingTask)
                    .HasForeignKey(e => e.PickingTaskId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PickingTaskItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.PickingTaskId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.MaterialId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.StockId).HasMaxLength(36);
                entity.Property(e => e.BatchNo).HasMaxLength(50);
                entity.Property(e => e.LocationId).HasMaxLength(36);
                entity.Property(e => e.IsStockingSupplement).HasDefaultValue(false);
            });

            modelBuilder.Entity<InventoryCountPlan>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.PlanMonth).IsRequired().HasMaxLength(7);
                entity.Property(e => e.WarehouseId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.CreatorId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.SubmitterId).HasMaxLength(36);
                entity.Property(e => e.Remark).HasMaxLength(500);
                entity.HasIndex(e => new { e.PlanMonth, e.WarehouseId }).IsUnique();
                entity.HasMany(e => e.Items)
                    .WithOne(e => e.Plan)
                    .HasForeignKey(e => e.PlanId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<InventoryCountItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.PlanId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.MaterialId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.LocationId).HasMaxLength(36);
                entity.Property(e => e.BookAmount).HasColumnType("numeric(18,2)");
                entity.Property(e => e.CountAmount).HasColumnType("numeric(18,2)");
                entity.Property(e => e.DiffAmount).HasColumnType("numeric(18,2)");
            });

            // ===== 文档模块 =====
            modelBuilder.Entity<UploadDocument>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.BizType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.BizId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.StoredFileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.RelativePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Remark).HasMaxLength(256);
                entity.Property(e => e.ThumbnailRelativePath).HasMaxLength(500);
                entity.Property(e => e.DeleteUserId).HasMaxLength(36);
                entity.Property(e => e.UploadUserId).HasMaxLength(36);
                entity.HasIndex(e => new { e.BizType, e.BizId });
                entity.HasIndex(e => e.CreateTime);
            });

            modelBuilder.Entity<DocumentDailySequence>(entity =>
            {
                entity.HasKey(e => e.TheDate);
            });

            // ===== 标签定义表配置 =====
            modelBuilder.Entity<TagDefinition>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("TagId");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.HasIndex(e => e.Code).IsUnique(false);
                entity.Property(e => e.Color).HasMaxLength(20);
                entity.Property(e => e.Icon).HasMaxLength(100);
                entity.Property(e => e.Category).HasMaxLength(50);
                entity.Property(e => e.Scope).HasMaxLength(200);
                entity.Property(e => e.Type).HasDefaultValue((short)2);
                entity.Property(e => e.Status).HasDefaultValue((short)1);
                entity.Property(e => e.Visibility).HasDefaultValue((short)3);
            });

            // ===== 标签关联表配置 =====
            modelBuilder.Entity<TagRelation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("RelationId");
                entity.Property(e => e.TagId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.EntityType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.EntityId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.Source).HasMaxLength(20);

                entity.HasIndex(e => new { e.EntityType, e.EntityId });
                entity.HasIndex(e => e.TagId);
                entity.HasIndex(e => new { e.EntityType, e.TagId });
            });

            // ===== 用户标签偏好表配置 =====
            modelBuilder.Entity<UserTagPreference>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.TagId });
                entity.Property(e => e.TagId).HasMaxLength(36);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.UserId, e.IsFavorite });
            });

            // ===== 用户收藏表配置 =====
            modelBuilder.Entity<UserFavorite>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("FavoriteId");
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.EntityType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.EntityId).IsRequired().HasMaxLength(36);
                entity.HasIndex(e => new { e.UserId, e.EntityType, e.EntityId }).IsUnique();
                entity.HasIndex(e => new { e.EntityType, e.EntityId });
            });

            // ===== 通用草稿表配置 =====
            modelBuilder.Entity<BizDraft>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("DraftId");
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.EntityType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DraftName).HasMaxLength(200);
                entity.Property(e => e.PayloadJson).IsRequired().HasColumnType("text");
                entity.Property(e => e.Status).HasDefaultValue((short)0);
                entity.Property(e => e.Remark).HasMaxLength(500);
                entity.Property(e => e.ConvertedEntityId).HasMaxLength(36);
                entity.HasIndex(e => new { e.UserId, e.EntityType, e.Status });
                entity.HasIndex(e => e.CreateTime);
            });

            // ===== 财务模块配置（与历史迁移表结构一致）=====
            modelBuilder.Entity<PaymentRequest>(entity =>
            {
                entity.ToTable("paymentrequest");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("PaymentRequestId").HasMaxLength(36);
                entity.Property(e => e.RequestCode).HasMaxLength(50);
                entity.Property(e => e.Amount).HasColumnType("numeric(18,2)");
            });

            modelBuilder.Entity<FinanceReceipt>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("FinanceReceiptId");
                entity.Property(e => e.FinanceReceiptCode).IsRequired().HasMaxLength(16);
                entity.Property(e => e.ReceiptAmount).HasColumnType("numeric(18,2)");
                entity.HasIndex(e => e.FinanceReceiptCode).IsUnique();
                entity.HasMany(e => e.Items)
                    .WithOne(i => i.Receipt)
                    .HasForeignKey(i => i.FinanceReceiptId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<FinanceReceiptItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("FinanceReceiptItemId");
                entity.Property(e => e.ReceiptAmount).HasColumnType("numeric(18,2)");
                entity.Property(e => e.ReceiptConvertAmount).HasColumnType("numeric(18,2)");
                entity.Property(e => e.VerifiedAmount).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
                entity.HasIndex(e => e.FinanceReceiptId);
            });

            modelBuilder.Entity<FinancePayment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("FinancePaymentId");
                entity.Property(e => e.FinancePaymentCode).IsRequired().HasMaxLength(16);
                entity.Property(e => e.BankSlipNo).HasMaxLength(100);
                entity.Property(e => e.PaymentAmount).HasColumnType("numeric(18,2)");
                entity.Property(e => e.PaymentAmountToBe).HasColumnType("numeric(18,2)");
                entity.Property(e => e.PaymentTotalAmount).HasColumnType("numeric(18,2)");
                entity.HasIndex(e => e.FinancePaymentCode).IsUnique();
                entity.HasMany(e => e.Items)
                    .WithOne(i => i.Payment)
                    .HasForeignKey(i => i.FinancePaymentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<FinancePaymentItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("FinancePaymentItemId");
                entity.Property(e => e.PaymentAmount).HasColumnType("numeric(18,2)");
                entity.Property(e => e.PaymentAmountToBe).HasColumnType("numeric(18,2)");
                entity.Property(e => e.VerificationDone).HasColumnType("numeric(18,2)");
                entity.Property(e => e.VerificationToBe).HasColumnType("numeric(18,2)");
                entity.HasIndex(e => e.FinancePaymentId);
            });

            modelBuilder.Entity<FinancePurchaseInvoice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("FinancePurchaseInvoiceId");
                entity.Property(e => e.InvoiceAmount).HasColumnType("numeric(18,2)");
                entity.Property(e => e.BillAmount).HasColumnType("numeric(18,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("numeric(18,2)");
                entity.Property(e => e.ExcludTaxAmount).HasColumnType("numeric(18,2)");
                entity.HasMany(e => e.Items)
                    .WithOne(i => i.PurchaseInvoice)
                    .HasForeignKey(i => i.FinancePurchaseInvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<FinancePurchaseInvoiceItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("FinancePurchaseInvoiceItemId");
                entity.Property(e => e.StockInCost).HasColumnType("numeric(18,6)");
                entity.Property(e => e.BillCost).HasColumnType("numeric(18,6)");
                entity.Property(e => e.BillAmount).HasColumnType("numeric(18,2)");
                entity.Property(e => e.TaxRate).HasColumnType("numeric(18,4)");
                entity.Property(e => e.TaxAmount).HasColumnType("numeric(18,2)");
                entity.Property(e => e.ExcludTaxAmount).HasColumnType("numeric(18,2)");
                entity.HasIndex(e => e.FinancePurchaseInvoiceId);
            });

            modelBuilder.Entity<FinanceSellInvoice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("FinanceSellInvoiceId");
                entity.Property(e => e.InvoiceTotal).HasColumnType("numeric(18,2)");
                entity.Property(e => e.ReceiveDone).HasColumnType("numeric(18,2)");
                entity.Property(e => e.ReceiveToBe).HasColumnType("numeric(18,2)");
                entity.HasMany(e => e.Items)
                    .WithOne(i => i.SellInvoice)
                    .HasForeignKey(i => i.FinanceSellInvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 数据库主键列名为 SellInvoiceItemId，与实体 [Column] 不一致时以 Fluent 为准
            modelBuilder.Entity<SellInvoiceItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("SellInvoiceItemId");
                entity.Property(e => e.InvoiceTotal).HasColumnType("numeric(18,2)");
                entity.Property(e => e.TaxRate).HasColumnType("numeric(18,4)");
                entity.Property(e => e.ValueAddedTax).HasColumnType("numeric(18,2)");
                entity.Property(e => e.TaxFreeTotal).HasColumnType("numeric(18,2)");
                entity.Property(e => e.Price).HasColumnType("numeric(18,6)");
                entity.HasIndex(e => e.FinanceSellInvoiceId);
            });

            // ===== RBAC 配置 =====
            modelBuilder.Entity<RbacDepartment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("DepartmentId");
                entity.Property(e => e.DepartmentName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ParentId).HasMaxLength(36);
                entity.Property(e => e.Path).HasMaxLength(500);
                entity.HasIndex(e => e.ParentId);
            });

            modelBuilder.Entity<RbacRole>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("RoleId");
                entity.Property(e => e.RoleCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.RoleName).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.RoleCode).IsUnique();
            });

            modelBuilder.Entity<RbacPermission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("PermissionId");
                entity.Property(e => e.PermissionCode).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PermissionName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PermissionType).HasMaxLength(20);
                entity.Property(e => e.Resource).HasMaxLength(200);
                entity.Property(e => e.Action).HasMaxLength(50);
                entity.HasIndex(e => e.PermissionCode).IsUnique();
            });

            modelBuilder.Entity<RbacUserDepartment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("UserDepartmentId");
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.DepartmentId).IsRequired().HasMaxLength(36);
                entity.HasIndex(e => new { e.UserId, e.DepartmentId }).IsUnique();
            });

            modelBuilder.Entity<RbacUserRole>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("UserRoleId");
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.RoleId).IsRequired().HasMaxLength(36);
                entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
            });

            modelBuilder.Entity<RbacRolePermission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("RolePermissionId");
                entity.Property(e => e.RoleId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.PermissionId).IsRequired().HasMaxLength(36);
                entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
            });

            modelBuilder.Entity<SysParamGroup>(entity =>
            {
                entity.ToTable("sysparamgroup");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("GroupId").HasMaxLength(36);
                entity.Property(e => e.GroupCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.GroupName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ParentId).HasMaxLength(36);
                entity.Property(e => e.Description).HasMaxLength(200);
            });

            modelBuilder.Entity<SysParam>(entity =>
            {
                entity.ToTable("sysparam");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("ParamId").HasMaxLength(36);
                entity.Property(e => e.ParamCode).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ParamName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.GroupId).HasMaxLength(36);
                entity.Property(e => e.ValueString).HasMaxLength(500);
                entity.Property(e => e.DefaultValue).HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ValueDecimal).HasColumnType("numeric(18,4)");
                entity.HasIndex(e => e.ParamCode).IsUnique();
            });

            modelBuilder.Entity<SysDictItem>(entity =>
            {
                entity.ToTable("sys_dict_item");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id").HasMaxLength(36);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(64);
                entity.Property(e => e.ItemCode).IsRequired().HasMaxLength(64);
                entity.Property(e => e.NameZh).IsRequired().HasMaxLength(200);
                entity.Property(e => e.NameEn).HasMaxLength(200);
                entity.Property(e => e.SortOrder);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.HasIndex(e => new { e.Category, e.ItemCode }).IsUnique();
                entity.Ignore(e => e.CreateUserId);
                entity.Ignore(e => e.ModifyUserId);
                entity.Ignore(e => e.ModifyTime);
            });

            modelBuilder.Entity<FinanceExchangeRateSetting>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("FinanceExchangeRateSettingId").HasMaxLength(36);
                entity.Property(e => e.UsdToCny).HasColumnType("numeric(12,4)");
                entity.Property(e => e.UsdToHkd).HasColumnType("numeric(12,4)");
                entity.Property(e => e.UsdToEur).HasColumnType("numeric(12,4)");
                entity.Property(e => e.EditorUserId).HasMaxLength(36);
                entity.Property(e => e.EditorUserName).HasMaxLength(100);
                entity.Ignore(e => e.CreateUserId);
                entity.Ignore(e => e.ModifyUserId);
            });

            modelBuilder.Entity<FinanceExchangeRateChangeLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("FinanceExchangeRateChangeLogId").HasMaxLength(36);
                entity.Property(e => e.UsdToCny).HasColumnType("numeric(12,4)");
                entity.Property(e => e.UsdToHkd).HasColumnType("numeric(12,4)");
                entity.Property(e => e.UsdToEur).HasColumnType("numeric(12,4)");
                entity.Property(e => e.ChangeUserId).HasMaxLength(36);
                entity.Property(e => e.ChangeUserName).HasMaxLength(100);
                entity.Property(e => e.ChangeSummary).HasMaxLength(500);
                entity.Ignore(e => e.CreateUserId);
                entity.Ignore(e => e.ModifyUserId);
                entity.Ignore(e => e.ModifyTime);
                entity.HasIndex(e => e.CreateTime);
            });

            modelBuilder.Entity<ApprovalRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id").HasMaxLength(36);
                // 兼容已落地的 approval_record 表：该表不包含 BaseEntity 的 CreateUserId/ModifyUserId 列
                entity.Ignore(e => e.CreateUserId);
                entity.Ignore(e => e.ModifyUserId);
                entity.Property(e => e.BizType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.BusinessId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.DocumentCode).HasMaxLength(64);
                entity.Property(e => e.ItemDescription).HasMaxLength(1000);
                entity.Property(e => e.ActionType).IsRequired().HasMaxLength(20);
                entity.Property(e => e.SubmitRemark).HasMaxLength(500);
                entity.Property(e => e.AuditRemark).HasMaxLength(500);
                entity.Property(e => e.SubmitterUserId).HasMaxLength(36);
                entity.Property(e => e.SubmitterUserName).HasMaxLength(100);
                entity.Property(e => e.ApproverUserId).HasMaxLength(36);
                entity.Property(e => e.ApproverUserName).HasMaxLength(100);
                entity.HasIndex(e => new { e.BizType, e.BusinessId });
                entity.HasIndex(e => e.ActionTime);
            });

            modelBuilder.Entity<OrderJourneyLog>(entity =>
            {
                entity.ToTable("log_orderjourney");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id").HasMaxLength(36);
                entity.Ignore(e => e.CreateUserId);
                entity.Ignore(e => e.ModifyUserId);
                entity.Property(e => e.EntityKind).IsRequired().HasMaxLength(32);
                entity.Property(e => e.EntityId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.ParentEntityKind).HasMaxLength(32);
                entity.Property(e => e.ParentEntityId).HasMaxLength(36);
                entity.Property(e => e.DocumentCode).HasMaxLength(64);
                entity.Property(e => e.LineHint).HasMaxLength(200);
                entity.Property(e => e.EventCode).IsRequired().HasMaxLength(64);
                entity.Property(e => e.EventLabel).HasMaxLength(200);
                entity.Property(e => e.FromState).HasMaxLength(32);
                entity.Property(e => e.ToState).HasMaxLength(32);
                entity.Property(e => e.EventTime);
                entity.Property(e => e.Quantity).HasColumnType("numeric(18,4)");
                entity.Property(e => e.Amount).HasColumnType("numeric(18,6)");
                entity.Property(e => e.Remark).HasMaxLength(500);
                entity.Property(e => e.RelatedEntityKind).HasMaxLength(32);
                entity.Property(e => e.RelatedEntityId).HasMaxLength(36);
                entity.Property(e => e.ActorKind).HasMaxLength(16);
                entity.Property(e => e.ActorUserId).HasMaxLength(36);
                entity.Property(e => e.ActorUserName).HasMaxLength(100);
                entity.Property(e => e.ActorVendorId).HasMaxLength(36);
                entity.Property(e => e.Source).HasMaxLength(64);
                entity.HasIndex(e => new { e.EntityKind, e.EntityId, e.EventTime });
                entity.HasIndex(e => new { e.ParentEntityKind, e.ParentEntityId, e.EventTime });
                entity.HasIndex(e => new { e.EventCode, e.EventTime });
            });
        }
    }
}
