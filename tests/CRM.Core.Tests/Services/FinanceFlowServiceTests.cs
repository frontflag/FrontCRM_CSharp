using CRM.Core.Constants;
using CRM.Core.Document;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Company;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Customs;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Models.Vendor;
using CRM.Core.Services;
using NSubstitute;

namespace CRM.Core.Tests.Services
{
    public class FinanceFlowServiceTests
    {
        [Fact]
        public async Task VerifyPaymentItemAsync_ShouldSyncPurchaseOrderFinanceStatus()
        {
            var paymentRepo = Substitute.For<IRepository<FinancePayment>>();
            var payItemRepo = Substitute.For<IRepository<FinancePaymentItem>>();
            var poRepo = Substitute.For<IRepository<PurchaseOrder>>();
            var poItemRepo = Substitute.For<IRepository<PurchaseOrderItem>>();
            var dataPermission = Substitute.For<IDataPermissionService>();
            var serialNumber = Substitute.For<ISerialNumberService>();
            var poExtendSync = Substitute.For<IPurchaseOrderItemExtendSyncService>();
            var vendorRepo = Substitute.For<IRepository<VendorInfo>>();
            var userRepo = Substitute.For<IRepository<User>>();
            var uow = Substitute.For<IUnitOfWork>();

            var payItem = new FinancePaymentItem
            {
                Id = "pi-1",
                PurchaseOrderItemId = "poi-1",
                PaymentAmountToBe = 100m,
                VerificationToBe = 100m,
                VerificationDone = 0m
            };
            var poItem = new PurchaseOrderItem
            {
                Id = "poi-1",
                PurchaseOrderId = "po-1",
                FinancePaymentStatus = 0
            };
            var po = new PurchaseOrder { Id = "po-1", FinanceStatus = 0 };

            payItemRepo.GetByIdAsync("pi-1").Returns(payItem);
            poItemRepo.GetByIdAsync("poi-1").Returns(poItem);
            payItemRepo.GetAllAsync().Returns(new[] { payItem });
            poItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<PurchaseOrderItem, bool>>>())
                .Returns(new[] { poItem });
            poRepo.GetByIdAsync("po-1").Returns(po);

            var svc = CreateFinancePaymentService(
                paymentRepo, payItemRepo, poRepo, poItemRepo, dataPermission, serialNumber, poExtendSync, vendorRepo, userRepo, uow);

            await svc.VerifyPaymentItemAsync("pi-1", 100m);

            await poItemRepo.Received(1).UpdateAsync(Arg.Is<PurchaseOrderItem>(x => x.FinancePaymentStatus == 2));
            await poRepo.Received(1).UpdateAsync(Arg.Is<PurchaseOrder>(x => x.FinanceStatus == 2));
        }

        [Fact]
        public async Task UpdateStatusAsync_ToCompleted_ShouldMarkItemsVerifiedAndSyncPurchaseFinanceStatus()
        {
            var paymentRepo = Substitute.For<IRepository<FinancePayment>>();
            var payItemRepo = Substitute.For<IRepository<FinancePaymentItem>>();
            var poRepo = Substitute.For<IRepository<PurchaseOrder>>();
            var poItemRepo = Substitute.For<IRepository<PurchaseOrderItem>>();
            var dataPermission = Substitute.For<IDataPermissionService>();
            var serialNumber = Substitute.For<ISerialNumberService>();
            var poExtendSync = Substitute.For<IPurchaseOrderItemExtendSyncService>();
            var vendorRepo = Substitute.For<IRepository<VendorInfo>>();
            var userRepo = Substitute.For<IRepository<User>>();
            var uow = Substitute.For<IUnitOfWork>();

            const string paymentId = "fp-1";
            var payItem = new FinancePaymentItem
            {
                Id = "pi-1",
                FinancePaymentId = paymentId,
                PurchaseOrderItemId = "poi-1",
                PaymentAmountToBe = 100m,
                VerificationToBe = 100m,
                VerificationDone = 0m,
                VerificationStatus = 0
            };
            var payment = new FinancePayment
            {
                Id = paymentId,
                Status = 10,
                PaymentAmountToBe = 100m
            };
            var poItem = new PurchaseOrderItem
            {
                Id = "poi-1",
                PurchaseOrderId = "po-1",
                FinancePaymentStatus = 0
            };
            var po = new PurchaseOrder { Id = "po-1", FinanceStatus = 0 };

            paymentRepo.GetByIdAsync(paymentId).Returns(payment);
            payItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<FinancePaymentItem, bool>>>())
                .Returns(new[] { payItem });
            payItemRepo.GetAllAsync().Returns(new[] { payItem });
            poItemRepo.GetByIdAsync("poi-1").Returns(poItem);
            poItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<PurchaseOrderItem, bool>>>())
                .Returns(new[] { poItem });
            poRepo.GetByIdAsync("po-1").Returns(po);

            var svc = CreateFinancePaymentService(
                paymentRepo, payItemRepo, poRepo, poItemRepo, dataPermission, serialNumber, poExtendSync, vendorRepo, userRepo, uow);

            await svc.UpdateStatusAsync(paymentId, 100);

            Assert.Equal(100m, payItem.VerificationDone);
            Assert.Equal(0m, payItem.VerificationToBe);
            Assert.Equal((short)2, payItem.VerificationStatus);
            await poItemRepo.Received(1).UpdateAsync(Arg.Is<PurchaseOrderItem>(x => x.FinancePaymentStatus == 2));
            await poRepo.Received(1).UpdateAsync(Arg.Is<PurchaseOrder>(x => x.FinanceStatus == 2));
            await uow.Received(2).SaveChangesAsync();
        }

        [Fact]
        public async Task ReverseVerificationAsync_WhenStatus100_ResetsItemsAndHeader()
        {
            var paymentRepo = Substitute.For<IRepository<FinancePayment>>();
            var payItemRepo = Substitute.For<IRepository<FinancePaymentItem>>();
            var poRepo = Substitute.For<IRepository<PurchaseOrder>>();
            var poItemRepo = Substitute.For<IRepository<PurchaseOrderItem>>();
            var dataPermission = Substitute.For<IDataPermissionService>();
            var serialNumber = Substitute.For<ISerialNumberService>();
            var poExtendSync = Substitute.For<IPurchaseOrderItemExtendSyncService>();
            var vendorRepo = Substitute.For<IRepository<VendorInfo>>();
            var userRepo = Substitute.For<IRepository<User>>();
            var uow = Substitute.For<IUnitOfWork>();
            var logAppend = Substitute.For<ILogOperationAppendService>();

            const string paymentId = "fp-1";
            var payItem = new FinancePaymentItem
            {
                Id = "pi-1",
                FinancePaymentId = paymentId,
                PurchaseOrderItemId = "poi-1",
                PaymentAmountToBe = 340000m,
                VerificationToBe = 0m,
                VerificationDone = 340000m,
                VerificationStatus = 2,
                PaymentAmount = 340000m
            };
            var payment = new FinancePayment
            {
                Id = paymentId,
                FinancePaymentCode = "PAY0021C",
                Status = 100,
                PaymentAmount = 340000m,
                PaymentTotalAmount = 340000m,
                PaymentAmountToBe = 340000m
            };
            var poItem = new PurchaseOrderItem
            {
                Id = "poi-1",
                PurchaseOrderId = "po-1",
                PurchaseOrderItemCode = "P00022M-1",
                FinancePaymentStatus = 2
            };
            var po = new PurchaseOrder { Id = "po-1", FinanceStatus = 2 };

            paymentRepo.GetByIdAsync(paymentId).Returns(payment);
            payItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<FinancePaymentItem, bool>>>())
                .Returns(new[] { payItem });
            payItemRepo.GetAllAsync().Returns(new[] { payItem });
            poItemRepo.GetByIdAsync("poi-1").Returns(poItem);
            poItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<PurchaseOrderItem, bool>>>())
                .Returns(new[] { poItem });
            poRepo.GetByIdAsync("po-1").Returns(po);

            var svc = CreateFinancePaymentService(
                paymentRepo, payItemRepo, poRepo, poItemRepo, dataPermission, serialNumber, poExtendSync, vendorRepo, userRepo, uow,
                logAppend: logAppend);

            var result = await svc.ReverseVerificationAsync(paymentId, "PAY0021C", "user-1", "admin");

            Assert.Equal((short)10, result.Status);
            Assert.Equal(0m, result.PaymentAmount);
            Assert.Equal(0m, result.PaymentTotalAmount);
            Assert.Equal((short)0, payItem.VerificationStatus);
            Assert.Equal(0m, payItem.VerificationDone);
            Assert.Equal(340000m, payItem.VerificationToBe);
            Assert.Equal(0m, payItem.PaymentAmount);
            await poItemRepo.Received(1).UpdateAsync(Arg.Is<PurchaseOrderItem>(x => x.FinancePaymentStatus == 0));
            await logAppend.Received(1).AppendAsync(
                BusinessLogTypes.FinancePayment,
                paymentId,
                "PAY0021C",
                OperationLogActionTypes.FinancePaymentReverseVerification,
                "user-1",
                "admin",
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ReverseVerificationAsync_WhenNotCompleted_Throws()
        {
            var paymentRepo = Substitute.For<IRepository<FinancePayment>>();
            paymentRepo.GetByIdAsync("fp-1").Returns(new FinancePayment
            {
                Id = "fp-1",
                FinancePaymentCode = "PAY001",
                Status = 10
            });

            var svc = CreateFinancePaymentService(
                paymentRepo,
                Substitute.For<IRepository<FinancePaymentItem>>(),
                Substitute.For<IRepository<PurchaseOrder>>(),
                Substitute.For<IRepository<PurchaseOrderItem>>(),
                Substitute.For<IDataPermissionService>(),
                Substitute.For<ISerialNumberService>(),
                Substitute.For<IPurchaseOrderItemExtendSyncService>(),
                Substitute.For<IRepository<VendorInfo>>(),
                Substitute.For<IRepository<User>>(),
                Substitute.For<IUnitOfWork>());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.ReverseVerificationAsync("fp-1", "PAY001", "user-1", null));
        }

        [Fact]
        public async Task ReverseVerificationAsync_WhenConfirmCodeMismatch_Throws()
        {
            var paymentRepo = Substitute.For<IRepository<FinancePayment>>();
            paymentRepo.GetByIdAsync("fp-1").Returns(new FinancePayment
            {
                Id = "fp-1",
                FinancePaymentCode = "PAY001",
                Status = 100
            });

            var svc = CreateFinancePaymentService(
                paymentRepo,
                Substitute.For<IRepository<FinancePaymentItem>>(),
                Substitute.For<IRepository<PurchaseOrder>>(),
                Substitute.For<IRepository<PurchaseOrderItem>>(),
                Substitute.For<IDataPermissionService>(),
                Substitute.For<ISerialNumberService>(),
                Substitute.For<IPurchaseOrderItemExtendSyncService>(),
                Substitute.For<IRepository<VendorInfo>>(),
                Substitute.For<IRepository<User>>(),
                Substitute.For<IUnitOfWork>());

            await Assert.ThrowsAsync<ArgumentException>(() =>
                svc.ReverseVerificationAsync("fp-1", "WRONG", "user-1", null));
        }

        [Fact]
        public async Task CanForceDeleteFinancePaymentAsync_AfterReverse_Allows()
        {
            var payItemRepo = Substitute.For<IRepository<FinancePaymentItem>>();
            payItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<FinancePaymentItem, bool>>>())
                .Returns(new[]
                {
                    new FinancePaymentItem
                    {
                        Id = "pi-1",
                        FinancePaymentId = "pay-1",
                        VerificationStatus = 0,
                        VerificationDone = 0m
                    }
                });

            var guardWithItems = new ForceDeleteGuardService(
                payItemRepo,
                Substitute.For<IRepository<FinanceReceiptItem>>(),
                Substitute.For<IRepository<FinancePurchaseInvoice>>(),
                Substitute.For<IRepository<FinanceSellInvoice>>(),
                Substitute.For<IRepository<SellInvoiceItem>>(),
                Substitute.For<IRepository<StockOutRequest>>(),
                Substitute.For<IRepository<PackingItem>>(),
                Substitute.For<IRepository<PickingTask>>(),
                Substitute.For<IRepository<StockOut>>(),
                Substitute.For<IRepository<StockOutItem>>(),
                Substitute.For<IRepository<PurchaseOrderItem>>(),
                Substitute.For<IRepository<FinanceReceipt>>(),
                Substitute.For<IRepository<FinanceReceivable>>(),
                Substitute.For<IRepository<Packing>>(),
                Substitute.For<IRepository<CustomsDeclaration>>(),
                Substitute.For<IRepository<FinancePurchaseInvoiceWriteOff>>());

            var result = await guardWithItems.CanForceDeleteFinancePaymentAsync("pay-1");

            Assert.True(result.CanDelete);
        }

        [Fact]
        public async Task UpdateReceiptStatus_InvalidTransition_ShouldThrow()
        {
            var receiptRepo = Substitute.For<IRepository<FinanceReceipt>>();
            var receiptItemRepo = Substitute.For<IRepository<FinanceReceiptItem>>();
            var sellInvoiceRepo = Substitute.For<IRepository<FinanceSellInvoice>>();
            var sellInvoiceItemRepo = Substitute.For<IRepository<SellInvoiceItem>>();
            var sellOrderRepo = Substitute.For<IRepository<SellOrder>>();
            var customerRepo = Substitute.For<IRepository<CustomerInfo>>();
            var userRepo = Substitute.For<IRepository<User>>();
            var dataPermission = Substitute.For<IDataPermissionService>();
            var serialNumber = Substitute.For<ISerialNumberService>();
            var extendSync = Substitute.For<ISellOrderItemExtendSyncService>();
            var uow = Substitute.For<IUnitOfWork>();

            receiptRepo.GetByIdAsync("r-1").Returns(new FinanceReceipt { Id = "r-1", Status = 0 });

            var svc = new FinanceReceiptService(
                receiptRepo, receiptItemRepo, sellInvoiceRepo, sellInvoiceItemRepo, sellOrderRepo, customerRepo, userRepo, dataPermission, serialNumber, extendSync,
                Substitute.For<IForceDeleteGuardService>(),
                Substitute.For<ILogOperationAppendService>(),
                Substitute.For<IFinanceReceiptListQuery>(),
                Substitute.For<IFinanceCustomerAdvanceService>(),
                Substitute.For<IFinanceReceivableService>(),
                Substitute.For<IRepository<FreightForwarderCompany>>(),
                uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateStatusAsync("r-1", 3));
        }

        [Fact]
        public async Task UpdateInvoiceStatus_InvalidTransition_ShouldThrow()
        {
            var invoiceRepo = Substitute.For<IRepository<FinanceSellInvoice>>();
            var itemRepo = Substitute.For<IRepository<SellInvoiceItem>>();
            var dataPermission = Substitute.For<IDataPermissionService>();
            var serialNumber = Substitute.For<ISerialNumberService>();
            var uow = Substitute.For<IUnitOfWork>();
            invoiceRepo.GetByIdAsync("si-1").Returns(new FinanceSellInvoice { Id = "si-1", InvoiceStatus = 1 });

            var svc = new FinanceSellInvoiceService(
                invoiceRepo, itemRepo, dataPermission, serialNumber,
                Substitute.For<IForceDeleteGuardService>(),
                Substitute.For<ILogOperationAppendService>(),
                Substitute.For<IFinanceSellInvoiceListQuery>(),
                uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateInvoiceStatusAsync("si-1", 100));
        }

        [Fact]
        public async Task ReverseVerificationAsync_WhenStatus3_ClearsVerification()
        {
            const string receiptId = "rec-1";
            var receiptRepo = Substitute.For<IRepository<FinanceReceipt>>();
            var receiptItemRepo = Substitute.For<IRepository<FinanceReceiptItem>>();
            var receivableService = Substitute.For<IFinanceReceivableService>();
            var logAppend = Substitute.For<ILogOperationAppendService>();
            var uow = Substitute.For<IUnitOfWork>();

            var receipt = new FinanceReceipt
            {
                Id = receiptId,
                FinanceReceiptCode = "REC00001",
                Status = 3
            };
            var item = new FinanceReceiptItem
            {
                Id = "ri-1",
                FinanceReceiptId = receiptId,
                ReceiptConvertAmount = 10000m,
                VerifiedAmount = 10000m,
                VerificationStatus = 2
            };

            receiptRepo.GetByIdAsync(receiptId).Returns(receipt);
            receiptItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<FinanceReceiptItem, bool>>>())
                .Returns(new[] { item });
            receivableService.GetWriteOffsByReceiptIdAsync(receiptId)
                .Returns(new List<FinanceReceivableWriteOffListItem>
                {
                    new() { Id = "wo-1", Amount = 10000m }
                });
            receivableService.ReverseWriteOffsByReceiptAsync(receiptId, "user-1")
                .Returns(new FinanceReceiptReverseWriteOffResult
                {
                    WriteOffCount = 1,
                    ReceivableCodes = new[] { "ARV001" },
                    StockOutCodes = new[] { "SOU001" }
                });

            var svc = CreateFinanceReceiptService(
                receiptRepo, receiptItemRepo, receivableService, logAppend, uow);

            var result = await svc.ReverseVerificationAsync(receiptId, "REC00001", "user-1", "admin");

            Assert.Equal(3, result.Status);
            await receivableService.Received(1).ReverseWriteOffsByReceiptAsync(receiptId, "user-1");
            await logAppend.Received(1).AppendAsync(
                BusinessLogTypes.FinanceReceipt,
                receiptId,
                "REC00001",
                OperationLogActionTypes.FinanceReceiptReverseVerification,
                "user-1",
                "admin",
                Arg.Is<string>(s => s.Contains("撤销流水 1 笔")));
        }

        [Fact]
        public async Task ReverseVerificationAsync_WhenAdvancePool_Throws()
        {
            var receiptRepo = Substitute.For<IRepository<FinanceReceipt>>();
            var receiptItemRepo = Substitute.For<IRepository<FinanceReceiptItem>>();
            receiptRepo.GetByIdAsync("rec-1").Returns(new FinanceReceipt
            {
                Id = "rec-1",
                FinanceReceiptCode = "REC00002",
                Status = 3
            });
            receiptItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<FinanceReceiptItem, bool>>>())
                .Returns(new[]
                {
                    new FinanceReceiptItem
                    {
                        Id = "ri-1",
                        FinanceReceiptId = "rec-1",
                        AdvancePoolAmount = 100m,
                        VerifiedAmount = 0m
                    }
                });

            var svc = CreateFinanceReceiptService(
                receiptRepo, receiptItemRepo,
                Substitute.For<IFinanceReceivableService>(),
                Substitute.For<ILogOperationAppendService>(),
                Substitute.For<IUnitOfWork>());

            await Assert.ThrowsAsync<ArgumentException>(() =>
                svc.ReverseVerificationAsync("rec-1", "REC00002", "user-1", null));
        }

        [Fact]
        public async Task CanForceDeleteFinanceReceiptAsync_WhenAdvancePool_Denies()
        {
            var receiptItemRepo = Substitute.For<IRepository<FinanceReceiptItem>>();
            receiptItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<FinanceReceiptItem, bool>>>())
                .Returns(new[]
                {
                    new FinanceReceiptItem
                    {
                        Id = "ri-1",
                        FinanceReceiptId = "rec-1",
                        AdvancePoolAmount = 50m,
                        VerifiedAmount = 0m,
                        VerificationStatus = 0
                    }
                });

            var guard = new ForceDeleteGuardService(
                Substitute.For<IRepository<FinancePaymentItem>>(),
                receiptItemRepo,
                Substitute.For<IRepository<FinancePurchaseInvoice>>(),
                Substitute.For<IRepository<FinanceSellInvoice>>(),
                Substitute.For<IRepository<SellInvoiceItem>>(),
                Substitute.For<IRepository<StockOutRequest>>(),
                Substitute.For<IRepository<PackingItem>>(),
                Substitute.For<IRepository<PickingTask>>(),
                Substitute.For<IRepository<StockOut>>(),
                Substitute.For<IRepository<StockOutItem>>(),
                Substitute.For<IRepository<PurchaseOrderItem>>(),
                Substitute.For<IRepository<FinanceReceipt>>(),
                Substitute.For<IRepository<FinanceReceivable>>(),
                Substitute.For<IRepository<Packing>>(),
                Substitute.For<IRepository<CustomsDeclaration>>(),
                Substitute.For<IRepository<FinancePurchaseInvoiceWriteOff>>());

            var result = await guard.CanForceDeleteFinanceReceiptAsync("rec-1");

            Assert.False(result.CanDelete);
            Assert.Contains("预收池", result.Message);
        }

        [Fact]
        public async Task CanForceDeleteFinanceReceiptAsync_AfterReverse_Allows()
        {
            var receiptItemRepo = Substitute.For<IRepository<FinanceReceiptItem>>();
            receiptItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<FinanceReceiptItem, bool>>>())
                .Returns(new[]
                {
                    new FinanceReceiptItem
                    {
                        Id = "ri-1",
                        FinanceReceiptId = "rec-1",
                        VerificationStatus = 0,
                        VerifiedAmount = 0m,
                        AdvancePoolAmount = 0m
                    }
                });

            var guard = new ForceDeleteGuardService(
                Substitute.For<IRepository<FinancePaymentItem>>(),
                receiptItemRepo,
                Substitute.For<IRepository<FinancePurchaseInvoice>>(),
                Substitute.For<IRepository<FinanceSellInvoice>>(),
                Substitute.For<IRepository<SellInvoiceItem>>(),
                Substitute.For<IRepository<StockOutRequest>>(),
                Substitute.For<IRepository<PackingItem>>(),
                Substitute.For<IRepository<PickingTask>>(),
                Substitute.For<IRepository<StockOut>>(),
                Substitute.For<IRepository<StockOutItem>>(),
                Substitute.For<IRepository<PurchaseOrderItem>>(),
                Substitute.For<IRepository<FinanceReceipt>>(),
                Substitute.For<IRepository<FinanceReceivable>>(),
                Substitute.For<IRepository<Packing>>(),
                Substitute.For<IRepository<CustomsDeclaration>>(),
                Substitute.For<IRepository<FinancePurchaseInvoiceWriteOff>>());

            var result = await guard.CanForceDeleteFinanceReceiptAsync("rec-1");

            Assert.True(result.CanDelete);
        }

        private static FinanceReceiptService CreateFinanceReceiptService(
            IRepository<FinanceReceipt> receiptRepo,
            IRepository<FinanceReceiptItem> receiptItemRepo,
            IFinanceReceivableService receivableService,
            ILogOperationAppendService logAppend,
            IUnitOfWork uow)
        {
            return new FinanceReceiptService(
                receiptRepo,
                receiptItemRepo,
                Substitute.For<IRepository<FinanceSellInvoice>>(),
                Substitute.For<IRepository<SellInvoiceItem>>(),
                Substitute.For<IRepository<SellOrder>>(),
                Substitute.For<IRepository<CustomerInfo>>(),
                Substitute.For<IRepository<User>>(),
                Substitute.For<IDataPermissionService>(),
                Substitute.For<ISerialNumberService>(),
                Substitute.For<ISellOrderItemExtendSyncService>(),
                Substitute.For<IForceDeleteGuardService>(),
                logAppend,
                Substitute.For<IFinanceReceiptListQuery>(),
                Substitute.For<IFinanceCustomerAdvanceService>(),
                receivableService,
                Substitute.For<IRepository<FreightForwarderCompany>>(),
                uow);
        }

        private static FinancePaymentService CreateFinancePaymentService(
            IRepository<FinancePayment> paymentRepo,
            IRepository<FinancePaymentItem> payItemRepo,
            IRepository<PurchaseOrder> poRepo,
            IRepository<PurchaseOrderItem> poItemRepo,
            IDataPermissionService dataPermission,
            ISerialNumberService serialNumber,
            IPurchaseOrderItemExtendSyncService poExtendSync,
            IRepository<VendorInfo> vendorRepo,
            IRepository<User> userRepo,
            IUnitOfWork uow,
            IForceDeleteGuardService? forceDeleteGuard = null,
            ILogOperationAppendService? logAppend = null)
        {
            return new FinancePaymentService(
                paymentRepo,
                payItemRepo,
                Substitute.For<IRepository<FinancePaymentBank>>(),
                poRepo,
                poItemRepo,
                dataPermission,
                serialNumber,
                poExtendSync,
                vendorRepo,
                Substitute.For<IRepository<VendorBankInfo>>(),
                Substitute.For<IRepository<CompanyBankInfo>>(),
                userRepo,
                forceDeleteGuard ?? Substitute.For<IForceDeleteGuardService>(),
                logAppend ?? Substitute.For<ILogOperationAppendService>(),
                Substitute.For<IFinancePaymentListQuery>(),
                Substitute.For<IDocumentService>(),
                uow);
        }
    }
}
