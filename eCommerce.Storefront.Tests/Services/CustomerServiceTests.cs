using System.Threading.Tasks;
using eCommerce.Storefront.Model.Customers;
using eCommerce.Storefront.Repository.EntityFrameworkCore;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using eCommerce.Storefront.Services.Implementations;
using eCommerce.Storefront.Services.Messaging.CustomerService;
using eCommerce.Storefront.Services.ViewModels;
using MapsterMapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eCommerce.Storefront.Tests.Services
{
    [TestClass]
    public class CustomerServiceTests
    {
        private Mock<ICustomerRepository> _customerRepositoryMock;
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<IMapper> _mapperMock;
        private CustomerService _customerService;

        [TestInitialize]
        public void Setup()
        {
            _customerRepositoryMock = new Mock<ICustomerRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();

            _customerService = new CustomerService(
                _customerRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object);
        }

        [TestMethod]
        public async Task ModifyCustomerAsync_WhenEmailChanges_SavesEmailAndFlagsChange()
        {
            var customer = CreateCustomer("old@test.com");

            _customerRepositoryMock.Setup(x => x.FindByAsync("old@test.com")).ReturnsAsync(customer);
            _customerRepositoryMock.Setup(x => x.FindByAsync("new@test.com")).ReturnsAsync((Customer)null);
            _mapperMock
                .Setup(x => x.Map<Customer, CustomerView>(It.IsAny<Customer>()))
                .Returns(new CustomerView { Email = "new@test.com" });

            var response = await _customerService.ModifyCustomerAsync(new ModifyCustomerRequest
            {
                CurrentEmail = "old@test.com",
                NewEmail = "new@test.com",
                FirstName = "John",
                SecondName = "Doe"
            });

            Assert.IsTrue(response.EmailChanged);
            _customerRepositoryMock.Verify(x => x.SaveEmailAsync("user-1", "new@test.com"), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
        }

        [TestMethod]
        public async Task ModifyCustomerAsync_WhenEmailIsUnchanged_DoesNotSaveEmail()
        {
            var customer = CreateCustomer("old@test.com");

            _customerRepositoryMock.Setup(x => x.FindByAsync("old@test.com")).ReturnsAsync(customer);
            _mapperMock
                .Setup(x => x.Map<Customer, CustomerView>(It.IsAny<Customer>()))
                .Returns(new CustomerView { Email = "OLD@test.com" });

            var response = await _customerService.ModifyCustomerAsync(new ModifyCustomerRequest
            {
                CurrentEmail = "old@test.com",
                NewEmail = "OLD@test.com",
                FirstName = "John",
                SecondName = "Doe"
            });

            Assert.IsFalse(response.EmailChanged);
            _customerRepositoryMock.Verify(x => x.SaveEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
        }

        [TestMethod]
        public async Task ModifyCustomerAsync_WhenNewEmailBelongsToAnotherAccount_Throws()
        {
            var customer = CreateCustomer("old@test.com");
            var other = CreateCustomer("new@test.com");

            _customerRepositoryMock.Setup(x => x.FindByAsync("old@test.com")).ReturnsAsync(customer);
            _customerRepositoryMock.Setup(x => x.FindByAsync("new@test.com")).ReturnsAsync(other);

            await Assert.ThrowsExactlyAsync<EmailAlreadyInUseException>(() =>
                _customerService.ModifyCustomerAsync(new ModifyCustomerRequest
                {
                    CurrentEmail = "old@test.com",
                    NewEmail = "new@test.com",
                    FirstName = "John",
                    SecondName = "Doe"
                }));

            _customerRepositoryMock.Verify(x => x.SaveEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        private static Customer CreateCustomer(string email) => new()
        {
            UserId = "user-1",
            FirstName = "John",
            SecondName = "Doe",
            Email = email
        };
    }
}
