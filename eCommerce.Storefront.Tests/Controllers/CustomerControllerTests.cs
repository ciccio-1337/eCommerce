using System.Security.Claims;
using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.Controllers;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using eCommerce.Storefront.Controllers.ViewModels.CustomerAccount;
using eCommerce.Storefront.Model;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.CustomerService;
using eCommerce.Storefront.Services.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eCommerce.Storefront.Tests.Controllers
{
    [TestClass]
    public class CustomerControllerTests
    {
        private Mock<ICustomerService> _customerServiceMock;
        private Mock<ICookieAuthentication> _cookieAuthenticationMock;
        private Mock<ILocalAuthenticationService> _authenticationServiceMock;
        private Mock<IEmailService> _emailServiceMock;
        private Mock<IConfiguration> _configurationMock;
        private Mock<IWebHostEnvironment> _environmentMock;
        private Mock<ILogger<CustomerController>> _loggerMock;
        private CustomerController _controller;

        [TestInitialize]
        public void Setup()
        {
            _customerServiceMock = new Mock<ICustomerService>();
            _cookieAuthenticationMock = new Mock<ICookieAuthentication>();
            _authenticationServiceMock = new Mock<ILocalAuthenticationService>();
            _emailServiceMock = new Mock<IEmailService>();
            _configurationMock = new Mock<IConfiguration>();
            _environmentMock = new Mock<IWebHostEnvironment>();
            _loggerMock = new Mock<ILogger<CustomerController>>();

            _configurationMock
                .Setup(x => x[It.IsAny<string>()])
                .Returns((string key) => key == "App:PublicBaseUrl" ? "https://store.example.com" : null);

            _customerServiceMock
                .Setup(x => x.GetCustomerAsync(It.IsAny<GetCustomerRequest>()))
                .ReturnsAsync(new GetCustomerResponse { CustomerFound = false });

            _controller = new CustomerController(
                _customerServiceMock.Object,
                _cookieAuthenticationMock.Object,
                _authenticationServiceMock.Object,
                _emailServiceMock.Object,
                _configurationMock.Object,
                _environmentMock.Object,
                _loggerMock.Object);
        }

        [TestMethod]
        public async Task AddDeliveryAddress_WhenAddressIsInvalid_RerendersFormWithIssueMessage()
        {
            var address = new DeliveryAddressView { City = "London" };

            _cookieAuthenticationMock.Setup(x => x.GetAuthenticationToken()).Returns("customer@test.com");
            _customerServiceMock
                .Setup(x => x.AddDeliveryAddressAsync(It.IsAny<DeliveryAddressAddRequest>()))
                .ThrowsAsync(new EntityBaseIsInvalidException("A delivery address must have a name."));

            var result = await _controller.AddDeliveryAddress(address);

            var viewResult = result as ViewResult;

            Assert.IsNotNull(viewResult);
            Assert.AreEqual("A delivery address must have a name.", viewResult.ViewData["IssueMessage"]);

            var model = viewResult.Model as CustomerDeliveryAddressView;

            Assert.IsNotNull(model);
            Assert.AreSame(address, model.Address);
        }

        [TestMethod]
        public async Task EditDeliveryAddress_WhenAddressIsInvalid_RerendersFormWithIssueMessage()
        {
            var address = new DeliveryAddressView { Id = 3, City = "London" };

            _cookieAuthenticationMock.Setup(x => x.GetAuthenticationToken()).Returns("customer@test.com");
            _customerServiceMock
                .Setup(x => x.ModifyDeliveryAddressAsync(It.IsAny<DeliveryAddressModifyRequest>()))
                .ThrowsAsync(new EntityBaseIsInvalidException("A delivery address must have a name."));

            var result = await _controller.EditDeliveryAddress(address);

            var viewResult = result as ViewResult;

            Assert.IsNotNull(viewResult);
            Assert.AreEqual("A delivery address must have a name.", viewResult.ViewData["IssueMessage"]);

            var model = viewResult.Model as CustomerDeliveryAddressView;

            Assert.IsNotNull(model);
            Assert.AreSame(address, model.Address);
        }

        [TestMethod]
        public async Task Detail_WhenEmailChanges_SignsOutAndReturnsEmailChangePendingView()
        {
            var customerView = new CustomerView
            {
                Email = "new@test.com",
                FirstName = "John",
                SecondName = "Doe"
            };

            _cookieAuthenticationMock.Setup(x => x.GetAuthenticationToken()).Returns("old@test.com");
            _customerServiceMock
                .Setup(x => x.ModifyCustomerAsync(It.IsAny<ModifyCustomerRequest>()))
                .ReturnsAsync(new ModifyCustomerResponse
                {
                    Customer = customerView,
                    EmailChanged = true
                });
            _authenticationServiceMock
                .Setup(x => x.GenerateEmailConfirmationTokenAsync("user-1"))
                .ReturnsAsync("confirmation-token");
            _emailServiceMock
                .Setup(x => x.SendMailAsync(It.IsAny<string>(), "new@test.com", It.IsAny<string>(), It.IsAny<string>(), false))
                .Returns(Task.CompletedTask);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, "user-1")], "test"))
                }
            };

            var result = await _controller.Detail(customerView);

            var viewResult = result as ViewResult;

            Assert.IsNotNull(viewResult);
            Assert.AreEqual("~/Views/AccountRegister/EmailChangePending.cshtml", viewResult.ViewName);
            Assert.AreEqual("new@test.com", viewResult.ViewData["Email"]);
            _cookieAuthenticationMock.Verify(x => x.SignOutAsync(), Times.Once);
            _emailServiceMock.Verify(
                x => x.SendMailAsync(
                    It.IsAny<string>(),
                    "new@test.com",
                    It.IsAny<string>(),
                    It.Is<string>(body => body.Contains("https://store.example.com/AccountRegister/ConfirmEmail")),
                    false),
                Times.Once);
        }
    }
}
