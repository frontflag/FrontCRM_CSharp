using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.System;
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

        // ===== 新增系统表 =====
        public DbSet<SysSerialNumber> SerialNumbers { get; set; } = null!;
        public DbSet<SysErrorLog> ErrorLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
        }
    }
}
