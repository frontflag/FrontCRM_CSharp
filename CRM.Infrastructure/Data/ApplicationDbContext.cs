using CRM.Core.Models;
using CRM.Core.Models.Component;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Document;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Quote;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Sales;
using CRM.Core.Models.System;
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

        // ===== 采购订单模块 =====
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = null!;
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; } = null!;

        // ===== 库存/入库/出库 =====
        public DbSet<StockInfo> Stocks { get; set; } = null!;
        public DbSet<StockIn> StockIns { get; set; } = null!;
        public DbSet<StockInItem> StockInItems { get; set; } = null!;
        public DbSet<StockOut> StockOuts { get; set; } = null!;
        public DbSet<StockOutItem> StockOutItems { get; set; } = null!;
        public DbSet<StockOutRequest> StockOutRequests { get; set; } = null!;

        // ===== 文档模块 =====
        public DbSet<UploadDocument> UploadDocuments { get; set; } = null!;
        public DbSet<DocumentDailySequence> DocumentDailySequences { get; set; } = null!;

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
                entity.Property(e => e.Status).HasDefaultValue((short)0);
                entity.Property(e => e.RfqDate).HasDefaultValueSql("NOW()");
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
                entity.Property(e => e.Quantity).HasColumnType("numeric(18,4)").HasDefaultValue(1m);
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
                entity.Property(e => e.Status).HasDefaultValue((short)0);
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
                entity.Property(e => e.PurchasedQty).HasColumnType("numeric(18,4)").HasDefaultValue(0m);
                entity.HasIndex(e => e.SellOrderId);
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
                entity.Property(e => e.SellOrderItemId).IsRequired();
                entity.Property(e => e.Qty).HasColumnType("numeric(18,4)").HasDefaultValue(0m);
                entity.Property(e => e.Cost).HasColumnType("numeric(18,6)").HasDefaultValue(0m);
                entity.HasIndex(e => e.PurchaseOrderId);
                entity.HasIndex(e => e.SellOrderItemId);
                entity.HasOne(e => e.SellOrderItem)
                      .WithMany()
                      .HasForeignKey(e => e.SellOrderItemId)
                      .OnDelete(DeleteBehavior.Restrict);
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
                entity.Property(e => e.OfficialName).HasMaxLength(128);
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
                entity.Property(e => e.Prefix).IsRequired().HasMaxLength(10);
                entity.HasIndex(e => e.ModuleCode).IsUnique();

                // 种子数据：初始化所有业务模块的流水号配置
                // 注意：CreateTime 使用静态时间，避免 EF Core PendingModelChangesWarning
                var seedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                entity.HasData(
                    new SysSerialNumber { Id = 1,  ModuleCode = "Customer",      ModuleName = "客户",     Prefix = "Cus",  SequenceLength = 4, CurrentSequence = 0, CreateTime = seedTime },
                    new SysSerialNumber { Id = 2,  ModuleCode = "Vendor",        ModuleName = "供应商",   Prefix = "Ven",  SequenceLength = 4, CurrentSequence = 0, CreateTime = seedTime },
                    new SysSerialNumber { Id = 3,  ModuleCode = "Inquiry",       ModuleName = "询价/需求", Prefix = "INQ",  SequenceLength = 4, CurrentSequence = 0, CreateTime = seedTime },
                    new SysSerialNumber { Id = 4,  ModuleCode = "Quotation",     ModuleName = "报价",     Prefix = "QUO",  SequenceLength = 4, CurrentSequence = 0, CreateTime = seedTime },
                    new SysSerialNumber { Id = 5,  ModuleCode = "SalesOrder",    ModuleName = "销售订单", Prefix = "SO",   SequenceLength = 4, CurrentSequence = 0, CreateTime = seedTime },
                    new SysSerialNumber { Id = 6,  ModuleCode = "PurchaseOrder", ModuleName = "采购订单", Prefix = "PO",   SequenceLength = 4, CurrentSequence = 0, CreateTime = seedTime },
                    new SysSerialNumber { Id = 7,  ModuleCode = "StockIn",       ModuleName = "入库",     Prefix = "SIN",  SequenceLength = 4, CurrentSequence = 0, CreateTime = seedTime },
                    new SysSerialNumber { Id = 8,  ModuleCode = "StockOut",      ModuleName = "出库",     Prefix = "SOUT", SequenceLength = 4, CurrentSequence = 0, CreateTime = seedTime },
                    new SysSerialNumber { Id = 9,  ModuleCode = "Inventory",     ModuleName = "库存调整", Prefix = "INV",  SequenceLength = 4, CurrentSequence = 0, CreateTime = seedTime },
                    new SysSerialNumber { Id = 10, ModuleCode = "Receipt",       ModuleName = "收款",     Prefix = "REC",  SequenceLength = 4, CurrentSequence = 0, CreateTime = seedTime },
                    new SysSerialNumber { Id = 11, ModuleCode = "Payment",       ModuleName = "付款",     Prefix = "PAY",  SequenceLength = 4, CurrentSequence = 0, CreateTime = seedTime },
                    new SysSerialNumber { Id = 12, ModuleCode = "InputInvoice",  ModuleName = "进项发票", Prefix = "VINV", SequenceLength = 4, CurrentSequence = 0, CreateTime = seedTime },
                    new SysSerialNumber { Id = 13, ModuleCode = "OutputInvoice", ModuleName = "销项发票", Prefix = "SINV", SequenceLength = 4, CurrentSequence = 0, CreateTime = seedTime }
                );
            });

            // ===== 错误日志表配置 =====
            modelBuilder.Entity<SysErrorLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ModuleName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ErrorMessage).IsRequired().HasMaxLength(500);
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

            // ===== 库存/入库/出库表配置 =====
            modelBuilder.Entity<StockInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MaterialId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.WarehouseId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.LocationId).HasMaxLength(36);
                entity.Property(e => e.Unit).HasMaxLength(20);
                entity.Property(e => e.BatchNo).HasMaxLength(50);
                entity.Property(e => e.Remark).HasMaxLength(500);
            });

            modelBuilder.Entity<StockIn>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StockInCode).IsRequired().HasMaxLength(32);
                entity.Property(e => e.SourceCode).HasMaxLength(32);
                entity.Property(e => e.WarehouseId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.VendorId).HasMaxLength(36);
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
                entity.Property(e => e.Remark).HasMaxLength(500);
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
                entity.Property(e => e.CustomerId).HasMaxLength(36);
                entity.Property(e => e.RequestUserId).HasMaxLength(36);
                entity.Property(e => e.Remark).HasMaxLength(500);
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
        }
    }
}
