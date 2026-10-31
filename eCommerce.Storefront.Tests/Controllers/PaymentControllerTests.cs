using System;
using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.Controllers;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.OrderService;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eCommerce.Storefront.Tests.Controllers
{
    [TestClass]
    public class PaymentControllerTests
    {
        private Mock<IPaymentService> _paymentServiceMock;
        private Mock<IOrderService> _orderServiceMock;
        private Mock<IMapper> _mapperMock;
        private Mock<ILogger<PaymentController>> _loggerMock;
        private Mock<ICookieAuthentication> _cookieAuthMock;
        private PaymentController _controller;

        [TestInitialize]
        public void Setup()
        {
            _paymentServiceMock = new Mock<IPaymentService>();
            _orderServiceMock = new Mock<IOrderService>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<PaymentController>>();
            _cookieAuthMock = new Mock<ICookieAuthentication>();

            _controller = new PaymentController(
                _paymentServiceMock.Object,
                _orderServiceMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _cookieAuthMock.Object);
        }

        [TestMethod]
        public async Task PaymentCallBack_WithInvalidCustomField_ReturnsOk()
        {
            _paymentServiceMock.Setup(x => x.GetOrderIdFor(It.IsAny<IFormCollection>())).Throws(new FormatException());
            var collection = new FormCollection(new System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            var result = await _controller.PaymentCallBack(collection);

            Assert.IsInstanceOfType(result, typeof(OkResult));
        }

        [TestMethod]
        public async Task PaymentCallBack_WhenOrderNotFound_ReturnsOk()
        {
            _paymentServiceMock.Setup(x => x.GetOrderIdFor(It.IsAny<IFormCollection>())).Returns(123);
            _orderServiceMock.Setup(x => x.GetOrderAsync(It.IsAny<GetOrderRequest>())).ReturnsAsync(new GetOrderResponse { Order = null });
            var collection = new FormCollection(new System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            var result = await _controller.PaymentCallBack(collection);

            Assert.IsInstanceOfType(result, typeof(OkResult));
        }

        [TestMethod]
        public void PaymentComplete_ReturnsView()
        {
            var result = _controller.PaymentComplete();
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public void PaymentCancel_ReturnsView()
        {
            var result = _controller.PaymentCancel();
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }
    }
}
