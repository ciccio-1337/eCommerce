using System;
using System.Reflection;
using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.Controllers;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.OrderService;
using eCommerce.Storefront.Services.ViewModels;
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
        public async Task PaymentCallBack_WhenUnexpectedExceptionOccurs_ReturnsOk()
        {
            _paymentServiceMock.Setup(x => x.GetOrderIdFor(It.IsAny<IFormCollection>())).Returns(123);
            _orderServiceMock
                .Setup(x => x.GetOrderAsync(It.IsAny<GetOrderRequest>()))
                .ThrowsAsync(new InvalidOperationException("boom"));
            var collection = new FormCollection(new System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            var result = await _controller.PaymentCallBack(collection);

            Assert.IsInstanceOfType(result, typeof(OkResult));
        }

        [TestMethod]
        public async Task CreatePaymentFor_WhenOrderHasAlreadyBeenPaid_RedirectsToOrderDetail()
        {
            _cookieAuthMock.Setup(x => x.GetAuthenticationToken()).Returns("customer@test.com");
            _orderServiceMock
                .Setup(x => x.GetOrderAsync(It.IsAny<GetOrderRequest>()))
                .ReturnsAsync(new GetOrderResponse
                {
                    Order = new OrderView { Id = 7, OrderHasBeenPaidFor = true }
                });

            var result = await _controller.CreatePaymentFor(7);

            var redirect = result as RedirectToActionResult;

            Assert.IsNotNull(redirect);
            Assert.AreEqual("Detail", redirect.ActionName);
            Assert.AreEqual("Order", redirect.ControllerName);
            Assert.AreEqual(7L, redirect.RouteValues["orderId"]);
            _paymentServiceMock.Verify(x => x.GeneratePostDataFor(It.IsAny<eCommerce.Storefront.Model.Orders.OrderPaymentRequest>()), Times.Never);
        }

        [TestMethod]
        public void CreatePaymentFor_IsPostOnly()
        {
            var method = typeof(PaymentController).GetMethod(nameof(PaymentController.CreatePaymentFor));

            Assert.IsNotNull(method);
            Assert.IsNotNull(method.GetCustomAttribute<HttpPostAttribute>());
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
