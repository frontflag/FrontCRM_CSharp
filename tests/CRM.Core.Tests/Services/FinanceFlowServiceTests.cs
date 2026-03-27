using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
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
                paymentRepo, payItemRepo, poRepo, poItemRepo, dataPermission, serialNumber, uow);

            await svc.VerifyPaymentItemAsync("pi-1", 100m);

            await poItemRepo.Received(1).UpdateAsync(Arg.Is<PurchaseOrderItem>(x => x.FinancePaymentStatus == 2));
            await poRepo.Received(1).UpdateAsync(Arg.Is<PurchaseOrder>(x => x.FinanceStatus == 2));
        }

        [Fact]
        public async Task UpdateReceiptStatus_InvalidTransition_ShouldThrow()
        {
            var receiptRepo = Substitute.For<IRepository<FinanceReceipt>>();
            var receiptItemRepo = Substitute.For<IRepository<FinanceReceiptItem>>();
            var sellInvoiceRepo = Substitute.For<IRepository<FinanceSellInvoice>>();
            var sellInvoiceItemRepo = Substitute.For<IRepository<SellInvoiceItem>>();
            var sellOrderRepo = Substitute.For<IRepository<SellOrder>>();
            var dataPermission = Substitute.For<IDataPermissionService>();
            var serialNumber = Substitute.For<ISerialNumberService>();
            var uow = Substitute.For<IUnitOfWork>();

            receiptRepo.GetByIdAsync("r-1").Returns(new FinanceReceipt { Id = "r-1", Status = 0 });

            var svc = new FinanceReceiptService(
                receiptRepo, receiptItemRepo, sellInvoiceRepo, sellInvoiceItemRepo, sellOrderRepo, dataPermission, serialNumber, uow);

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
