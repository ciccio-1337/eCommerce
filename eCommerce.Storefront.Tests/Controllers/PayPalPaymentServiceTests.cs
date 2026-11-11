using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.Services.Implementations;
using eCommerce.Storefront.Model.Orders;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eCommerce.Storefront.Tests.Controllers
{
    [TestClass]
    public class PayPalPaymentServiceTests
    {
        private Mock<IConfiguration> _configurationMock;
        private PayPalPaymentService _service;

        [TestInitialize]
        public void Setup()
        {
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(x => x["PayPalBusinessEmail"]).Returns("merchant@test.com");
            _configurationMock.Setup(x => x["PayPalPaymentPostToUrl"]).Returns("https://sandbox.paypal.com/cgi-bin/webscr");

            _service = new PayPalPaymentService(
                new Mock<IHttpContextAccessor>().Object,
                _configurationMock.Object,
                new HttpClient(new StubHttpMessageHandler("VERIFIED")),
                new Mock<ILogger<PayPalPaymentService>>().Object);
        }

        [TestMethod]
        public async Task HandleCallBackAsync_WhenTransactionIdIsMissing_DoesNotMarkPaymentOk()
        {
            var result = await _service.HandleCallBackAsync(
                new OrderPaymentRequest { Id = 1, Total = 10m },
                CreateCollection(transactionId: null));

            Assert.IsFalse(result.PaymentOk);
            Assert.IsNull(result.PaymentToken);
        }

        [TestMethod]
        public async Task HandleCallBackAsync_WhenTransactionIdIsBlank_DoesNotMarkPaymentOk()
        {
            var result = await _service.HandleCallBackAsync(
                new OrderPaymentRequest { Id = 1, Total = 10m },
                CreateCollection(transactionId: "   "));

            Assert.IsFalse(result.PaymentOk);
            Assert.IsNull(result.PaymentToken);
        }

        [TestMethod]
        public async Task HandleCallBackAsync_WhenIpnIsValid_MarksPaymentOk()
        {
            var result = await _service.HandleCallBackAsync(
                new OrderPaymentRequest { Id = 1, Total = 10m },
                CreateCollection(transactionId: "TXN-1"));

            Assert.IsTrue(result.PaymentOk);
            Assert.AreEqual("TXN-1", result.PaymentToken);
            Assert.AreEqual(10m, result.Amount);
            Assert.AreEqual("PayPal", result.PaymentMerchant);
        }

        [TestMethod]
        public async Task HandleCallBackAsync_WhenAmountDoesNotMatch_DoesNotMarkPaymentOk()
        {
            var result = await _service.HandleCallBackAsync(
                new OrderPaymentRequest { Id = 1, Total = 9m },
                CreateCollection(transactionId: "TXN-1", amount: "10.00"));

            Assert.IsFalse(result.PaymentOk);
        }

        private static IFormCollection CreateCollection(
            string transactionId,
            string amount = "10.00",
            string currency = "EUR",
            string status = "Completed")
        {
            return new FormCollection(new Dictionary<string, StringValues>
            {
                ["receiver_email"] = "merchant@test.com",
                ["payment_status"] = status,
                ["mc_currency"] = currency,
                ["mc_gross"] = amount,
                ["txn_id"] = transactionId
            });
        }

        private sealed class StubHttpMessageHandler(string response) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(response)
                });
            }
        }
    }
}
