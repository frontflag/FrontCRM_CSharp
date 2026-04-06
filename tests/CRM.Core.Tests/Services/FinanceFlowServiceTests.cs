using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Finance;
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

            var svc = new FinancePaymentService(
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

            var svc = new FinancePaymentService(
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
        public async Task UpdateReceiptStatus_InvalidTransition_ShouldThrow()
        {
            var receiptRepo = Substitute.For<IRepository<FinanceReceipt>>();
            var receiptItemRepo = Substitute.For<IRepository<FinanceReceiptItem>>();
            var sellInvoiceRepo = Substitute.For<IRepository<FinanceSellInvoice>>();
            var sellInvoiceItemRepo = Substitute.For<IRepository<SellInvoiceItem>>();
            var sellOrderRepo = Substitute.For<IRepository<SellOrder>>();
            var userRepo = Substitute.For<IRepository<User>>();
            var dataPermission = Substitute.For<IDataPermissionService>();
            var serialNumber = Substitute.For<ISerialNumberService>();
            var extendSync = Substitute.For<ISellOrderItemExtendSyncService>();
            var uow = Substitute.For<IUnitOfWork>();

            receiptRepo.GetByIdAsync("r-1").Returns(new FinanceReceipt { Id = "r-1", Status = 0 });

            var svc = new FinanceReceiptService(
                receiptRepo, receiptItemRepo, sellInvoiceRepo, sellInvoiceItemRepo, sellOrderRepo, userRepo, dataPermission, serialNumber, extendSync, uow);

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

            var svc = new FinanceSellInvoiceService(invoiceRepo, itemRepo, dataPermission, serialNumber, uow);

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateInvoiceStatusAsync("si-1", 100));
        }
    }
}
